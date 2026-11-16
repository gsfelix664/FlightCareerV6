using System;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Services
{
    public class FlightQualityService
    {
        private readonly FlightQualityRules _rules;

        public FlightQualityRules Rules => _rules;

        public bool IsPaxFlight { get; private set; } = true;

        public double GSeconds { get; private set; }
        public double BankSeconds { get; private set; }
        public double PitchSeconds { get; private set; }
        public double JerkSeconds { get; private set; }
        public double SlipSeconds { get; private set; }

        public double MaxGForce { get; private set; } = 1.0;
        public double MinGForce { get; private set; } = 1.0;
        public double MaxBank { get; private set; }
        public double MaxPitch { get; private set; }
        public double TouchdownFPM { get; private set; }

        /// <summary>Pontos de conforto perdidos, somados a cada amostra com o peso do tipo de voo daquele momento.</summary>
        public double ComfortPenaltyPoints { get; private set; }

        [Obsolete("Use os acumulados em segundos (GSeconds, BankSeconds, etc.)")]
        public int ComfortViolations => (int)Math.Round(BankSeconds + PitchSeconds + (IsPaxFlight ? GSeconds + JerkSeconds + SlipSeconds : 0));

        [Obsolete("Use os acumulados em segundos (GSeconds, JerkSeconds)")]
        public int CargoViolations => (int)Math.Round(!IsPaxFlight ? GSeconds + JerkSeconds : 0);

        public FlightQualityService(FlightQualityRules? rules = null)
        {
            _rules = rules ?? new FlightQualityRules();
            Reset();
        }

        public void Reset()
        {
            IsPaxFlight = true;
            GSeconds = 0;
            BankSeconds = 0;
            PitchSeconds = 0;
            JerkSeconds = 0;
            SlipSeconds = 0;
            MaxGForce = 1.0;
            MinGForce = 1.0;
            MaxBank = 0;
            MaxPitch = 0;
            TouchdownFPM = 0;
            ComfortPenaltyPoints = 0;
        }

        [Obsolete("Use ProcessSample com FlightQualitySample e segundos simulados")]
        public void ProcessFrame(SimTelemetry data, bool isPaxFlight)
        {
            ProcessSample(FlightQualitySample.FromTelemetry(data), isPaxFlight, 1.0);
        }

        public void ProcessSample(FlightQualitySample sample, bool isPaxFlight, double simSeconds)
        {
            if (sample.OnGround) return;
            if (sample.MotionPaused == true) return;

            // Monitorar Extremos (G, Bank, Pitch)
            if (sample.GForce > MaxGForce) MaxGForce = sample.GForce;
            if (sample.GForce < MinGForce) MinGForce = sample.GForce;

            double absBank = Math.Abs(sample.BankDeg);
            if (absBank > MaxBank) MaxBank = absBank;

            double absPitch = Math.Abs(sample.PitchUpDeg);
            if (absPitch > MaxPitch) MaxPitch = absPitch;

            if (double.IsNaN(simSeconds) || simSeconds <= 0) return;

            IsPaxFlight = isPaxFlight;

            // Turbulência atenua solavanco e excesso de G (não é culpa do piloto)
            bool isTurbulence = sample.WindVerticalFps.HasValue && Math.Abs(sample.WindVerticalFps.Value) >= _rules.WindVerticalThresholdFps;
            double accelSimSeconds = isTurbulence ? simSeconds * _rules.TurbulenceFactor : simSeconds;

            // Monitorar G-Force
            if (sample.GForce > _rules.MaxGForce || sample.GForce < _rules.MinGForce)
            {
                GSeconds += accelSimSeconds;
                ComfortPenaltyPoints += accelSimSeconds * (isPaxFlight ? _rules.PaxGWeight : _rules.CargoGWeight);
            }

            // Monitorar Jerk (solavanco vertical)
            if (sample.LoadFactorYDot.HasValue && Math.Abs(sample.LoadFactorYDot.Value) > _rules.JerkThreshold)
            {
                JerkSeconds += accelSimSeconds;
                ComfortPenaltyPoints += accelSimSeconds * (isPaxFlight ? _rules.PaxJerkWeight : _rules.CargoJerkWeight);
            }

            // Em voos de carga, manobras de atitude e derrapagem não incomodam
            if (isPaxFlight)
            {
                // Monitorar Bank (Inclinação lateral)
                if (absBank > _rules.MaxBankDeg)
                {
                    BankSeconds += simSeconds;
                    ComfortPenaltyPoints += simSeconds * _rules.PaxBankWeight;
                }

                // Monitorar Pitch (Nariz para cima > 20° ou nariz para baixo > 15°)
                if (sample.PitchUpDeg > _rules.PitchUpMaxDeg || sample.PitchUpDeg < -_rules.PitchDownMaxDeg)
                {
                    PitchSeconds += simSeconds;
                    ComfortPenaltyPoints += simSeconds * _rules.PaxPitchWeight;
                }

                // Monitorar Derrapagem (Slip / Beta)
                if (sample.BetaDeg.HasValue && Math.Abs(sample.BetaDeg.Value) > _rules.SlipThresholdDeg)
                {
                    SlipSeconds += simSeconds;
                    ComfortPenaltyPoints += simSeconds * _rules.PaxSlipWeight;
                }
            }
        }

        public void RecordTouchdownFpm(double fpm)
        {
            double safeFpm = Math.Round(Math.Abs(fpm), 0);
            if (safeFpm > TouchdownFPM)
            {
                TouchdownFPM = safeFpm;
            }
        }

        public void RecordTouchdown(double velocityBodyY)
        {
            RecordTouchdownFpm(Math.Abs(velocityBodyY) * 60.0);
        }

        public double GetQualityScore()
        {
            double score = 100.0;

            if (TouchdownFPM > 0)
            {
                if (TouchdownFPM <= 180) { /* Butter - Sem penalidade */ }
                else if (TouchdownFPM <= 350) score -= 5;
                else if (TouchdownFPM <= 500) score -= 15;
                else if (TouchdownFPM <= 750) score -= 35;
                else score -= 60;
            }

            score -= ComfortPenaltyPoints;

            return Math.Max(0, Math.Min(100.0, score));
        }

        public string GetRating()
        {
            if (TouchdownFPM >= 900) return "CRASH / COLAPSO ESTRUTURAL";
            if (TouchdownFPM > 600) return "POUSO DURO (MANUTENÇÃO)";
            double score = GetQualityScore();
            if (score >= 90) return "EXCELENTE (MANTEIGA)";
            if (score >= 75) return "BOM";
            if (score >= 60) return "ACEITÁVEL";
            if (score >= 40) return "RUIM (RECLAMAÇÕES)";
            return "PÉSSIMO (DESCONFORTO)";
        }
    }
}
