using FlightCareer.Domain.Entities;
using System;

namespace FlightCareer.UI.Models
{
    public class MarketOffer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
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
        public double BaseMarketValue { get; set; }
        public double OriginalAskingPrice { get; set; }
        public int PriceDropsCount { get; set; }
        public bool IsSold { get; set; }
        public DateTime? SoldAtUtc { get; set; }
        public bool IsPlayerOwned { get; set; }
        public Guid? PlayerAircraftId { get; set; }

        // Indicadores do Mercado Vivo e Flipping
        public double DealRatio => BaseMarketValue > 0 && SalePrice > 0 ? (BaseMarketValue / SalePrice) : 1.0;
        public bool HasPriceDrop => PriceDropsCount > 0 && OriginalAskingPrice > SalePrice;
        public double PriceDropPercentage => OriginalAskingPrice > 0 ? Math.Round(((OriginalAskingPrice - SalePrice) / OriginalAskingPrice) * 100.0, 1) : 0;

        public string DealBadge => DealRatio switch
        {
            >= 1.15 => "BARBADA",
            >= 1.05 => "OPORTUNIDADE",
            <= 0.85 => "ÁGIO",
            _ => "PREÇO JUSTO"
        };

        public string DealBadgeColor => DealRatio switch
        {
            >= 1.15 => "#00E676",
            >= 1.05 => "#00E5FF",
            <= 0.85 => "#E74C3C",
            _ => "#8B9BAE"
        };

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
