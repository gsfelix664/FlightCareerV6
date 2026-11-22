using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Economy
{
    public sealed record DifficultyEconomy(double PayMultiplier, double CostMultiplier, double FailureMultiplier)
    {
        public static DifficultyEconomy For(DifficultyMode mode) => mode switch
        {
            DifficultyMode.Easy => new DifficultyEconomy(1.25, 0.80, 1.00),
            DifficultyMode.Medium => new DifficultyEconomy(1.00, 1.00, 1.00),
            DifficultyMode.Hard => new DifficultyEconomy(0.85, 1.15, 1.25),
            _ => new DifficultyEconomy(1.00, 1.00, 1.00)
        };

        public static string Label(DifficultyMode mode) => mode switch
        {
            DifficultyMode.Easy => "Fácil",
            DifficultyMode.Medium => "Médio",
            DifficultyMode.Hard => "Difícil",
            _ => "Médio"
        };
    }
}
