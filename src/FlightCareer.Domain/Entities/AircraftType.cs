using System;

namespace FlightCareer.Domain.Entities
{
    public class AircraftType
    {
        public Guid Id { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double PurchasePrice { get; set; }
        public double FuelTankGallons { get; set; }
        public int RangeNM { get; set; }
        public int MaxPayloadLbs { get; set; }
        public int CruiseSpeedKts { get; set; }
        public int MaxPax { get; set; }
        public double FuelConsumptionGPH { get; set; }
        public int MinRunwayLength { get; set; }
        public int ServiceCeiling { get; set; }
        public string FuelType { get; set; } = string.Empty;
    }
}
