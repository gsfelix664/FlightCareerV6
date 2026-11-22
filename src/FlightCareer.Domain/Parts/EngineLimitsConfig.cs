using System;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Limites operacionais de motor utilizados pelo <see cref="PartEventDeriver"/> para derivação de eventos.
    /// </summary>
    public sealed record EngineLimitsConfig
    {
        /// <summary>
        /// Rotação máxima da faixa vermelha (RPM) para motor a pistão.
        /// </summary>
        public double? RpmRedline { get; init; }

        /// <summary>
        /// Temperatura mínima de operação do óleo (°C).
        /// </summary>
        public double? OilTempMin { get; init; }
    }
}
