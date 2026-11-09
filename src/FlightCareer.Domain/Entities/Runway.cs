using System;
using FlightCareer.Domain.Enums;

namespace FlightCareer.Domain.Entities
{
    public class Runway
    {
        public Guid Id { get; set; }
        public string AirportIdent { get; set; } = string.Empty;
        public int LengthFt { get; set; }
        public int WidthFt { get; set; }
        public string Surface { get; set; } = string.Empty;
        public bool IsLighted { get; set; }

        public RunwaySurface SurfaceType => Surface.ToUpperInvariant() switch
        {
            var s when s.Contains("ASP") => RunwaySurface.Asphalt,
            var s when s.Contains("CON") => RunwaySurface.Concrete,
            var s when s.Contains("GRAS") => RunwaySurface.Grass,
            var s when s.Contains("DIRT") => RunwaySurface.Dirt,
            var s when s.Contains("GRAV") => RunwaySurface.Gravel,
            var s when s.Contains("WAT") => RunwaySurface.Water,
            _ => RunwaySurface.Other
        };
    }
}
