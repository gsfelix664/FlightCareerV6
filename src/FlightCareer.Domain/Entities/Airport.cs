using System;
using FlightCareer.Domain.Enums;
using FlightCareer.Domain.ValueObjects;

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
        public int ElevationFt { get; set; }
        public int Tier { get; set; }

        public Coordinates Coordinates => new(Latitude, Longitude);
        public AirportClassification Classification => (AirportClassification)Tier;

        // Integração de Cenário do Simulador (MSFS 2024 via Little Navmap)
        public bool IsInSimulator { get; set; } = false;
        public string? MsfsIdent { get; set; }

        public bool HasMsfsDivergence => IsInSimulator && !string.IsNullOrEmpty(MsfsIdent) && !string.Equals(ICAO, MsfsIdent, StringComparison.OrdinalIgnoreCase);
        public string DisplayIdent => HasMsfsDivergence ? $"{ICAO} (MSFS: {MsfsIdent})" : ICAO;
    }
}
