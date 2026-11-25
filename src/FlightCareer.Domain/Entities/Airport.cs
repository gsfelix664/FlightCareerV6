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

        // Sinais estruturais e classificação de Tier por movimento
        public int IlsEnds { get; set; }
        public int Approaches { get; set; }
        public int Gates { get; set; }
        public int CargoParking { get; set; }
        public bool HasTower { get; set; }
        public int LongestRunwayFt { get; set; }
        public bool LongestRunwayPaved { get; set; }
        public int TierScore { get; set; }
        /// <summary>Base militar segundo o simulador (limita o tier sem etiqueta de linha).</summary>
        public bool IsMilitary { get; set; }

        // Sinais do CSV (OurAirports) e versão das regras aplicadas
        public string? CsvType { get; set; }
        public bool ScheduledService { get; set; }
        public int TierRulesVersion { get; set; }

        // Integração de Cenário do Simulador (MSFS 2024 via Little Navmap e SimConnect AIRSIM)
        public bool IsInSimulator { get; set; } = false;
        public string? MsfsIdent { get; set; }
        public string Source { get; set; } = string.Empty;
        public string? SimRegion { get; set; }
        public string? SimFingerprint { get; set; }
        public DateTime? SimCheckedUtc { get; set; }

        public bool HasMsfsDivergence => IsInSimulator && !string.IsNullOrEmpty(MsfsIdent) && !string.Equals(ICAO, MsfsIdent, StringComparison.OrdinalIgnoreCase);
        public string DisplayIdent => HasMsfsDivergence ? $"{ICAO} (MSFS: {MsfsIdent})" : ICAO;
    }
}
