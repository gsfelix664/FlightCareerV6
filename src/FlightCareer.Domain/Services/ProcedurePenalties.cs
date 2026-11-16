using System;

namespace FlightCareer.Domain.Services
{
    public static class ProcedurePenalties
    {
        public const double AutoStartXpPenaltyFraction = 0.10;

        /// <summary>
        /// Aplica penalidade de XP quando partida automática foi utilizada.
        /// Reduz 10% do XP do voo (arredondado para baixo; nunca negativo).
        /// </summary>
        public static int ApplyAutoStartPenalty(int xp, bool autoStartUsed)
        {
            if (!autoStartUsed || xp <= 0) return Math.Max(0, xp);
            int reducedXp = (int)Math.Floor(xp * (1.0 - AutoStartXpPenaltyFraction));
            return Math.Max(0, reducedXp);
        }
    }
}
