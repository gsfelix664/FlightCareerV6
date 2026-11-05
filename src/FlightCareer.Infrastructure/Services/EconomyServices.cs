using System;

namespace FlightCareer.Infrastructure.Services
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
                default:
                    return 0;
            }
        }
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
    }

    public static class FuelService
    {
        private const double BASE_PRICE_JETA = 6.50;
        private const double BASE_PRICE_AVGAS = 7.80;

        public static double GetCurrentPrice(string fuelType)
        {
            string type = fuelType.ToUpper();
            double basePrice = type.Contains("JET") ? BASE_PRICE_JETA : BASE_PRICE_AVGAS;

            int seed = DateTime.UtcNow.Year * 1000 + DateTime.UtcNow.DayOfYear;
            Random rng = new Random(seed);
            double fluctuation = 0.95 + (rng.NextDouble() * 0.10);

            return Math.Round(basePrice * fluctuation, 2);
        }
    }
}
