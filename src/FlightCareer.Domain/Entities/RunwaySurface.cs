using System;
using System.Linq;

namespace FlightCareer.Domain.Entities
{
    /// <summary>
    /// Classificação de superfícies de pista para regras de proficiência (XP) e bônus de pagamento (bush/remoto).
    /// </summary>
    public static class RunwaySurface
    {
        private static readonly string[] HardSurfaces = { "ASPH", "CON", "CONC", "HARD", "BIT", "PEM", "ASP" };

        /// <summary>
        /// Retorna true se a descrição da superfície corresponder a um pavimento duro (asfalto, concreto, etc.).
        /// </summary>
        public static bool IsHard(string? surface)
        {
            if (string.IsNullOrWhiteSpace(surface)) return false;
            return HardSurfaces.Any(h => surface.Contains(h, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retorna true se a pista for não pavimentada (terra, grama, cascalho, etc.).
        /// </summary>
        public static bool IsUnpaved(string? surface)
        {
            if (string.IsNullOrWhiteSpace(surface)) return false;
            return !IsHard(surface);
        }
    }
}
