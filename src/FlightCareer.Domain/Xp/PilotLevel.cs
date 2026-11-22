using System;

namespace FlightCareer.Domain.Xp
{
    /// <summary>
    /// Progressão de níveis do piloto baseada na curva quadrática:
    /// XP acumulado do nível L = 300 × (L − 1)².
    /// Sem nível máximo (progressão contínua).
    /// </summary>
    public static class PilotLevel
    {
        public static int ForXp(long xp)
        {
            if (xp <= 0) return 1;
            int level = 1 + (int)Math.Floor(Math.Sqrt((double)xp / 300.0));
            return Math.Max(1, level);
        }

        public static long XpForLevel(int level)
        {
            if (level <= 1) return 0L;
            long lMinus1 = level - 1;
            return 300L * lMinus1 * lMinus1;
        }
    }
}
