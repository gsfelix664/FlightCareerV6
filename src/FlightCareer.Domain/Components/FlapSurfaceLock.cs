using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Lógica pura para cálculo da fração alvo da superfície dos flaps (0.0 a 1.0)
    /// com base no índice da alavanca e quantidade total de posições.
    /// </summary>
    public static class FlapSurfaceLock
    {
        /// <summary>
        /// Calcula a fração alvo da superfície dos flaps (0.0 a 1.0).
        /// Se positions &lt;= 1, retorna 0.0.
        /// O resultado é limitado ao intervalo [0.0, 1.0].
        /// </summary>
        public static double TargetFraction(double index, double positions)
        {
            if (positions <= 1.0) return 0.0;
            double fraction = index / (positions - 1.0);
            return Math.Clamp(fraction, 0.0, 1.0);
        }
    }
}
