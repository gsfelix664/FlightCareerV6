namespace FlightCareer.Infrastructure.Services
{
    // Redirecionamento retrocompatível para a camada de Application
    public static class FuelPriceService
    {
        public static double GetCurrentPrice(string fuelType, int airportTier = 3)
            => FlightCareer.Application.Services.FuelPriceService.GetCurrentPrice(fuelType, airportTier);
    }
}
