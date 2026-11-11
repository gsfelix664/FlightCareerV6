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
        public double AvionicsCondition { get; set; } = 100;
        public double AirframeHours { get; set; }
        public double FuelAmount { get; set; }

        public int YearOfManufacture { get; set; } = 1995;
        public double TsmoHours { get; set; } = 450;
        public double TboHours { get; set; } = 1800;
        public string StructuralIncidentHistory { get; set; } = "Sem histórico de incidentes estruturais";
        public string CategoryBadge { get; set; } = "Monomotor Pistão";

        public double AverageCondition => (EngineCondition + TiresCondition + AirframeCondition + AvionicsCondition) / 4.0;
        public double EstimatedPostBuyCost => Math.Round(Math.Max(0, (100 - AverageCondition) * 120.0), 2);
        
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
