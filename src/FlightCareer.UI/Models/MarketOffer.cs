using FlightCareer.Domain.Entities;
using System;

namespace FlightCareer.UI.Models
{
    public class MarketOffer
    {
        public AircraftType BaseType { get; set; } = null!;
        public string Prefix { get; set; } = string.Empty;
        
        // Estados (0 a 100)
        public double EngineCondition { get; set; }
        public double TiresCondition { get; set; }
        public double AirframeCondition { get; set; }
        public double AirframeHours { get; set; }
        public double FuelAmount { get; set; }
        
        public double AverageCondition => (EngineCondition + TiresCondition + AirframeCondition) / 3.0;
        
        // Preço baseado no estado (ex: uma aeronave 100% custa o preço cheio, 70% custa proporcional)
        public double SalePrice { get; set; }

        public string GeneralState => AverageCondition switch
        {
            >= 95 => "EXCELENTE",
            >= 80 => "BOM",
            >= 60 => "REGULAR",
            _ => "RESTAURAÇÃO"
        };

        public string StateColor => AverageCondition switch
        {
            >= 95 => "#2ECC71",
            >= 80 => "#3498DB",
            >= 60 => "#F1C40F",
            _ => "#E74C3C"
        };
    }
}
