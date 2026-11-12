using System;

namespace FlightCareer.Domain.Entities
{
    public class MarketListing
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AircraftTypeId { get; set; }
        public AircraftType? AircraftType { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public double EngineCondition { get; set; } = 100.0;
        public double TiresCondition { get; set; } = 100.0;
        public double AirframeCondition { get; set; } = 100.0;
        public double AvionicsCondition { get; set; } = 100.0;
        public double AirframeHours { get; set; }
        public int YearOfManufacture { get; set; } = 1995;
        public double TboHours { get; set; } = 1800.0;
        public double TsmoHours { get; set; }
        public string StructuralIncidentHistory { get; set; } = string.Empty;
        public double FuelAmount { get; set; }
        public double BaseMarketValue { get; set; }
        public double AskingPrice { get; set; }
        public double OriginalAskingPrice { get; set; }
        public int PriceDropsCount { get; set; }
        public DateTime ListedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastPriceDropAtUtc { get; set; }
        public bool IsSold { get; set; }
        public DateTime? SoldAtUtc { get; set; }
        public bool IsPlayerOwned { get; set; }
        public Guid? PlayerAircraftId { get; set; }
        public Guid? PilotId { get; set; }
        public string CategoryBadge { get; set; } = string.Empty;
    }
}
