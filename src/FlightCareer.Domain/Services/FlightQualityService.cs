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

        public void ProcessFrame(SimTelemetry data, bool isPaxFlight)
        {
            if (data.OnGround > 0.5) return;

            // Monitorar G-Force
            if (data.GForce > MaxGForce) MaxGForce = data.GForce;
            if (data.GForce < MinGForce) MinGForce = data.GForce;

            if (data.GForce > 1.5 || data.GForce < 0.5)
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

        public void RecordTouchdown(double velocityBodyY)
        {
            TouchdownFPM = Math.Abs(velocityBodyY) * 60.0;
        }

        public double GetQualityScore()
        {
            double score = 100.0;

            if (TouchdownFPM > 150 && TouchdownFPM <= 300) score -= 10;
            else if (TouchdownFPM > 300 && TouchdownFPM <= 500) score -= 30;
            else if (TouchdownFPM > 500) score -= 60;

            score -= (ComfortViolations * 0.1); 
            score -= (CargoViolations * 0.05);

            return Math.Max(0, score);
        }

        public string GetRating()
        {
            double score = GetQualityScore();
            if (score >= 95) return "EXCELENTE (MANTEIGA)";
            if (score >= 80) return "BOM";
            if (score >= 60) return "ACEITÁVEL";
            if (score >= 40) return "RUIM (RECLAMAÇÕES)";
            return "PÉSSIMO (DANOS)";
        }
    }
}
