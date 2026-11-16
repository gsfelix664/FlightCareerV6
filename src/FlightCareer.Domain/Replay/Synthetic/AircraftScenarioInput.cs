using System;
using System.Collections.Generic;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Dados de entrada do cenário da aeronave para geração de voo sintético.
    /// </summary>
    public sealed record AircraftScenarioInput(
        AircraftLimitProfile Profile,
        IReadOnlyList<SensorDefinition>? Sensors = null,
        IReadOnlyList<string>? Features = null,
        int? EngineCount = null,
        string? Title = null);
}
