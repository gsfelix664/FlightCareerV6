using System.Collections.Generic;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Voo sintético procedural completo contendo a sequência de frames de telemetria, toques e gabarito.
    /// </summary>
    public sealed record SyntheticFlight(
        IReadOnlyList<SensorFrame> Frames,
        IReadOnlyList<ReplayTouchdown> Touchdowns,
        SyntheticGroundTruth GroundTruth,
        AircraftLimitProfile Profile,
        IReadOnlyList<string> SensorKeys);
}
