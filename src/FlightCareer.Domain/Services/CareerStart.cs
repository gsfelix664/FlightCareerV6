using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Services
{
    /// <summary>
    /// Configurações padronizadas de início de carreira do piloto e aeronave inicial (C152).
    /// Fonte única de verdade para telas de criação de perfil e simuladores de progressão.
    /// </summary>
    public static class CareerStart
    {
        public const double EasyInitialBalance = 1_000_000_000.0;
        public const double MediumInitialBalance = 100_000.0;
        public const double HardInitialBalance = 10_000.0;

        public const double DefaultStartingAirframeHours = 5000.0;
        public const double DefaultStartingFuelFraction = 0.5;
        public const double DefaultStartingTboRemainingHours = 1800.0;
        public const double DefaultStarting50hHours = 50.0;
        public const double DefaultStarting100hHours = 100.0;

        public static double GetInitialBalance(DifficultyMode difficulty) => difficulty switch
        {
            DifficultyMode.Easy => EasyInitialBalance,
            DifficultyMode.Medium => MediumInitialBalance,
            DifficultyMode.Hard => HardInitialBalance,
            _ => MediumInitialBalance
        };
    }
}
