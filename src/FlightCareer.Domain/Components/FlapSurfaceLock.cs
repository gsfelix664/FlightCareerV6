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
        /// <paramref name="positions"/> = FLAPS NUM HANDLE POSITIONS, que no MSFS 2024 conta só os dentes estendidos
        /// (capturas de 2026-11-15: C152 = 3 com índices 0..3; DA40 = 2; 737 = 8). Por isso fração = índice / posições.
        /// Se positions &lt;= 0, retorna 0.0. O resultado é limitado ao intervalo [0.0, 1.0].
        /// </summary>
        public static double TargetFraction(double index, double positions)
        {
            if (positions <= 0.0) return 0.0;
            double fraction = index / positions;
            return Math.Clamp(fraction, 0.0, 1.0);
        }
    }
}
