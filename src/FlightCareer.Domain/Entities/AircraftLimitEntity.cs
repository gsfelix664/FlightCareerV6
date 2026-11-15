using System;
using FlightCareer.Domain.Limits;

namespace FlightCareer.Domain.Entities
{
    /// <summary>
    /// Limite operacional individual de uma aeronave persistido no banco de dados.
    /// </summary>
    public class AircraftLimitEntity
    {
        public Guid Id { get; set; }

        public Guid ProfileId { get; set; }
        public AircraftLimitProfileEntity? Profile { get; set; }

        public string Key { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public LimitSource Source { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
