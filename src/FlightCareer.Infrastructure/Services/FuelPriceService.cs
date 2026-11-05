using System;

namespace FlightCareer.Infrastructure.Services
{
    public static class FuelPriceService
    {
        // Data base para o início da economia do sistema
        private static readonly DateTime ProjectStartDate = new DateTime(2024, 1, 1);
        
        // Preços base em USD por GALÃO
        private const double BaseAvgasPrice = 5.50; 
        private const double BaseJetAPrice = 4.80; 

        public static double GetCurrentPrice(string fuelType)
        {
            double basePrice = fuelType.ToUpper().Contains("JET") ? BaseJetAPrice : BaseAvgasPrice;
            
            // 1. Calcular Inflação (Dobrar em 10 anos = 3650 dias)
            double daysPassed = (DateTime.Today - ProjectStartDate).TotalDays;
            double inflationFactor = Math.Pow(2, daysPassed / 3650.0);
            
            double inflatedPrice = basePrice * inflationFactor;

            // 2. Variação Diária (+/- 3%) usando a data como semente
            var rng = new Random(DateTime.Today.Year * 1000 + DateTime.Today.DayOfYear);
            double dailyVariation = 0.97 + (rng.NextDouble() * 0.06);

            return Math.Round(inflatedPrice * dailyVariation, 2);
        }
    }
}
