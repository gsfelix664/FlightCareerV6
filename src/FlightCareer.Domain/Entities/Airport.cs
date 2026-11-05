using System;

namespace FlightCareer.Domain.Entities
{
    public class Airport
    {
        public Guid Id { get; set; }
        public string ICAO { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Tier { get; set; }
    }
}
