using System;
using FlightCareer.Domain.Components;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Modelo de probabilidade de pane por taxa de uso para peças sujeitas a queima (ex.: lâmpadas).
    /// </summary>
    public static class UsageFailureModel
    {
        private static readonly HazardSettings DefaultHazard = new();

        /// <summary>
        /// Taxa de falha horária λ(h) baseada na curva do FailureEngine.
        /// Zero acima de 20%; abaixo, cresce de forma suave até 2,0 falhas/hora em 0%.
        /// </summary>
        public static double LambdaPerHour(double healthPct)
        {
            return FailureEngine.HazardPerHour(healthPct, DefaultHazard);
        }

        /// <summary>
        /// Realiza o sorteio determinístico de falha da peça para um intervalo de tempo simulado.
        /// Saúde &lt;= 0 enquanto acesa resulta em falha certa (retorna true).
        /// </summary>
        public static bool Roll(double healthPct, double dtSimHours, IRandomSource rng)
        {
            if (healthPct <= 0.0) return true;
            if (dtSimHours <= 0.0 || rng == null) return false;

            double lambda = LambdaPerHour(healthPct);
            if (lambda <= 0.0) return false;

            double p = 1.0 - Math.Exp(-lambda * dtSimHours);
            if (p <= 0.0) return false;

            return rng.NextDouble() < p;
        }
    }
}
