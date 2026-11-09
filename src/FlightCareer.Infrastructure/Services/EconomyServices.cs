namespace FlightCareer.Infrastructure.Services
{
    // Redirecionamento retrocompatível para a camada de Application
    public static class MaintenanceService
    {
        public static double CalculateRepairCost(double basePrice, string component, double points)
            => FlightCareer.Application.Services.MaintenanceService.CalculateRepairCost(basePrice, component, points);
    }

    public static class InsuranceService
    {
        public static double CalculateMonthlyPremium(double basePrice)
            => FlightCareer.Application.Services.InsuranceService.CalculateMonthlyPremium(basePrice);

        public static double CalculateDeductible(double basePrice)
            => FlightCareer.Application.Services.InsuranceService.CalculateDeductible(basePrice);

        public static double CalculatePayout(double basePrice)
            => FlightCareer.Application.Services.InsuranceService.CalculatePayout(basePrice);
    }

    public static class FuelService
    {
        public static double GetCurrentPrice(string fuelType)
            => FlightCareer.Application.Services.FuelService.GetCurrentPrice(fuelType);
    }
}
