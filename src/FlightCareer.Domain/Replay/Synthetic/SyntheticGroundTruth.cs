using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Gabarito do voo sintético: metadados exatos do que foi injetado e do comportamento esperado.
    /// </summary>
    /// <param name="InjectedAbuses">Lista de abusos ou anomalias deliberadamente injetados.</param>
    /// <param name="PlannedDurationSeconds">Duração planejada em segundos reais/simulados.</param>
    /// <param name="ExpectedLandingCycles">Número de pousos válidos (não-bounce) planejados.</param>
    /// <param name="IsCancelled">Indica se o voo foi cancelado/interrompido no meio.</param>
    /// <param name="HasDataCorruptions">Indica se há dados corrompidos intencionalmente (NaN, gaps, etc.).</param>
    public sealed record SyntheticGroundTruth(
        IReadOnlyList<SyntheticAbuseRecord> InjectedAbuses,
        double PlannedDurationSeconds,
        int ExpectedLandingCycles,
        bool IsCancelled = false,
        bool HasDataCorruptions = false);
}
