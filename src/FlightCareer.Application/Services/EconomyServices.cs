using System;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Application.Services
{
    public static class MaintenanceService
    {
        private const double ENGINE_COST_FACTOR = 0.002;
        private const double AIRFRAME_COST_FACTOR = 0.0005;
        private const double TIRES_COST_PER_POINT = 8.0;

        public static double CalculateRepairCost(double basePrice, string component, double points)
        {
            if (points <= 0) return 0;

            double unitCost = 0;
            switch (component.ToUpper())
            {
                case "MOTOR":
                    unitCost = basePrice * ENGINE_COST_FACTOR;
                    if (unitCost < 150) unitCost = 150;
                    return Math.Round(unitCost * points, 2);
                case "FUSELAGEM":
                    unitCost = basePrice * AIRFRAME_COST_FACTOR;
                    if (unitCost < 50) unitCost = 50;
                    return Math.Round(unitCost * points, 2);
                case "PNEUS":
                    return Math.Round(TIRES_COST_PER_POINT * points, 2);
                case "AVIONICOS":
                case "AVIÔNICOS":
                    unitCost = basePrice * 0.001;
                    if (unitCost < 80) unitCost = 80;
                    return Math.Round(unitCost * points, 2);
                default:
                    return 0;
            }
        }
    }

    public static class EconomyServices
    {
        public static double CalculateMonthlyPremium(double basePrice) => InsuranceService.CalculateMonthlyPremium(basePrice);
        public static double CalculateDeductible(double basePrice) => InsuranceService.CalculateDeductible(basePrice);
        public static double CalculatePayout(double basePrice) => InsuranceService.CalculatePayout(basePrice);
    }

    public static class InsuranceService
    {
        private const double MONTHLY_RATE = 0.01; // 1% do valor da aeronave
        private const double DEDUCTIBLE_RATE = 0.05; // 5% de franquia
        private const double PAYOUT_RATE = 0.70; // 70% de cobertura
        private const double MIN_PREMIUM = 250;

        public static double CalculateMonthlyPremium(double basePrice)
        {
            double premium = basePrice * MONTHLY_RATE;
            if (premium < MIN_PREMIUM) premium = MIN_PREMIUM;
            return Math.Round(premium, 2);
        }

        public static double CalculateDeductible(double basePrice)
        {
            return Math.Round(basePrice * DEDUCTIBLE_RATE, 2);
        }

        public static double CalculatePayout(double basePrice)
        {
            return Math.Round(basePrice * PAYOUT_RATE, 2);
        }

        /// <summary>
        /// Verifica se a aeronave possui seguro ativo contra o relógio de carreira informado.
        /// </summary>
        public static bool IsInsured(PilotAircraft aircraft, DateTime careerClock)
        {
            return aircraft.InsuranceExpirationDate.HasValue && aircraft.InsuranceExpirationDate.Value > careerClock;
        }

        /// <summary>
        /// Calcula a nova data de validade de 30 dias de seguro a partir do relógio de carreira.
        /// Se já houver seguro ativo, estende por mais 30 dias a partir da validade atual.
        /// </summary>
        public static DateTime CalculateNewExpirationDate(DateTime? currentExpiration, DateTime careerClock)
        {
            var baseDate = currentExpiration.HasValue && currentExpiration.Value > careerClock
                ? currentExpiration.Value
                : careerClock;
            return baseDate.AddDays(30);
        }
    }

    public static class FuelService
    {
        public static double GetCurrentPrice(string fuelType)
        {
            return FuelPriceService.GetCurrentPrice(fuelType);
        }
    }
}
