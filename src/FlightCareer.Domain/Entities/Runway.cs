using System;

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
    }
}
