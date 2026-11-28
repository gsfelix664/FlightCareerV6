using System;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Economy
{
    /// <summary>
    /// Avaliação de valor de mercado base de aeronaves novas e usadas.
    /// Cálculo puro sem dependência de banco de dados ou estado volátil.
    /// </summary>
    public static class AircraftValuation
    {
        public static double CalculateBaseMarketValue(
            AircraftType? type,
            double engineCond,
            double tiresCond,
            double airframeCond,
            double avionicsCond,
            int year,
            double airframeHours)
        {
            if (type == null) return 0.0;
            return CalculateBaseMarketValue(type.PurchasePrice, engineCond, tiresCond, airframeCond, avionicsCond, year, airframeHours);
        }

        public static double CalculateBaseMarketValue(
            double purchasePrice,
            double engineCond,
            double tiresCond,
            double airframeCond,
            double avionicsCond,
            int year,
            double airframeHours)
        {
            if (purchasePrice <= 0.0) return 0.0;

            double cMotor = Math.Clamp(engineCond, 0.0, 100.0) / 100.0;
            double cAvionics = Math.Clamp(avionicsCond, 0.0, 100.0) / 100.0;
            double cEstrutura = ((Math.Clamp(airframeCond, 0.0, 100.0) + Math.Clamp(tiresCond, 0.0, 100.0)) / 2.0) / 100.0;

            int currentYear = DateTime.UtcNow.Year;
            int safeYear = year > 1900 ? year : 1995;
            double fAno = Math.Clamp(1.0 - ((currentYear - safeYear) * 0.005), 0.70, 1.05);

            double weightedCondition = (0.45 * cMotor) + (0.30 * cAvionics) + (0.25 * cEstrutura);
            double baseValue = purchasePrice * fAno * weightedCondition;

            // Bônus Overhaul Zero Hora
            if (engineCond >= 95.0)
            {
                baseValue *= 1.12; // +12%
            }

            // Penalidade AOG / Sucata
            if (engineCond < 25.0 || tiresCond < 25.0 || airframeCond < 25.0 || avionicsCond < 25.0)
            {
                baseValue *= 0.75; // -25%
            }

            return Math.Round(Math.Max(5000.0, baseValue), 2);
        }
    }
}
