using System;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Services
{
    public class FlightQualityService
    {
        public int ComfortViolations { get; private set; }
        public int CargoViolations { get; private set; }
        public double MaxGForce { get; private set; }
        public double MinGForce { get; private set; } = 1.0;
        public double MaxBank { get; private set; }
        public double MaxPitch { get; private set; }
        public double TouchdownFPM { get; private set; }

        public void Reset()
        {
            ComfortViolations = 0;
            CargoViolations = 0;
            MaxGForce = 1.0;
            MinGForce = 1.0;
            MaxBank = 0;
            MaxPitch = 0;
            TouchdownFPM = 0;
        }

        public void ProcessFrame(SimTelemetry data, bool isPaxFlight)
        {
            if (data.OnGround > 0.5) return;

            // Monitorar G-Force
            if (data.GForce > MaxGForce) MaxGForce = data.GForce;
            if (data.GForce < MinGForce) MinGForce = data.GForce;

            if (data.GForce > 1.6 || data.GForce < 0.4)
            {
                if (isPaxFlight) ComfortViolations++;
                else CargoViolations++;
            }

            // Monitorar Bank (Inclinação Lateral > 30 graus)
            double absBank = Math.Abs(data.PlaneBank);
            if (absBank > MaxBank) MaxBank = absBank;
            if (absBank > 30) ComfortViolations++;

            // Monitorar Pitch (Inclinação Vertical > 20 ou < -15 graus)
            double absPitch = Math.Abs(data.PlanePitch);
            if (absPitch > MaxPitch) MaxPitch = absPitch;
            if (data.PlanePitch > 20 || data.PlanePitch < -15) ComfortViolations++;
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

            score -= (ComfortViolations * 0.1); 
            score -= (CargoViolations * 0.05);

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
