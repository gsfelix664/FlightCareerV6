using FlightCareer.Domain.Components;

namespace FlightCareer.UI.Models
{
    public static class MarketConditionHelper
    {
        public static string ConditionText(double condition) =>
            HealthWords.For(condition, feminine: false);

        public static string ConditionColor(double condition) => condition switch
        {
            <= 0.0 => "#E74C3C", // Quebrado
            < 20.0 => "#E74C3C", // Crítico
            < 40.0 => "#F1C40F", // Atenção
            < 80.0 => "#A3E635", // Bom
            _ => "#2ECC71"       // Íntegro
        };
    }
}
