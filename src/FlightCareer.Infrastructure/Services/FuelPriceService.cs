using System;

namespace FlightCareer.Infrastructure.Services
{
    public static class FuelPriceService
    {
        // Data base para o início da economia do sistema
        private static readonly DateTime ProjectStartDate = new DateTime(2024, 1, 1);
        
        // Preços base em USD por GALÃO (Calibrados para o mercado real de aviação)
        private const double BaseAvgasPrice = 6.80; // Avgas 100LL
        private const double BaseJetAPrice = 5.40;  // Jet A-1

        public static double GetCurrentPrice(string fuelType, int airportTier = 3)
        {
            double basePrice = fuelType.ToUpper().Contains("JET") ? BaseJetAPrice : BaseAvgasPrice;
            
            // 1. Calcular Inflação (Dobrar em 10 anos = 3650 dias)
            double daysPassed = (DateTime.Today - ProjectStartDate).TotalDays;
            double inflationFactor = Math.Pow(2, daysPassed / 3650.0);
            
            double inflatedPrice = basePrice * inflationFactor;

            // 2. Variação Diária (+/- 3%) usando a data como semente
            var rng = new Random(DateTime.Today.Year * 1000 + DateTime.Today.DayOfYear);
            double dailyVariation = 0.97 + (rng.NextDouble() * 0.06);

            // 3. Diferencial Logístico por Porte do Aeródromo
            double tierFactor = airportTier switch
            {
                1 => 0.94, // Hubs internacionais (economia de escala em grandes tanques)
                2 => 0.98, // Regionais médios
                3 => 1.00, // Municipais e executivos (preço padrão FBO)
                4 => 1.22, // Pistas remotas / rurais (custo de frete rodoviário e tamboramento)
                _ => 1.00
            };

            return Math.Round(inflatedPrice * dailyVariation * tierFactor, 2);
        }
    }
}
