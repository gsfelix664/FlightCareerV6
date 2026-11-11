using FlightCareer.Domain.Entities;
using System;

namespace FlightCareer.UI.Models
{
    public class PlayerAircraftSaleItem
    {
        public PilotAircraft Aircraft { get; set; } = null!;
        public string Model => Aircraft.AircraftType?.Model ?? "N/A";
        public string Prefix => Aircraft.Prefix;
        public double AirframeHours => Aircraft.CurrentAirframeHours;
        public double EngineCondition => Aircraft.EngineCondition;
        public double TiresCondition => Aircraft.TiresCondition;
        public double AirframeCondition => Aircraft.AirframeCondition;
        public double AvionicsCondition => Aircraft.AvionicsCondition;
        public double AverageCondition => (EngineCondition + TiresCondition + AirframeCondition + AvionicsCondition) / 4.0;
        
        public double BasePurchasePrice => Aircraft?.AircraftType?.PurchasePrice ?? 0;
        
        // Avaliação Bluebook: 90% do valor proporcional à condição geral
        public double GrossValue => Math.Round(BasePurchasePrice * (AverageCondition / 100.0) * 0.9, 2);
        public double BluebookValue => GrossValue;
        public string BaseLocation => string.IsNullOrWhiteSpace(Aircraft?.CurrentLocationICAO) ? "Desconhecida" : Aircraft!.CurrentLocationICAO;
        public int YearOfManufacture => Aircraft != null && Aircraft.PurchaseDate != default && Aircraft.PurchaseDate.Year > 1900 ? Aircraft.PurchaseDate.Year : 2018;
        
        // Taxa de corretagem padrão da corretora aeronáutica (8%)
        public double BrokerFee => Math.Round(GrossValue * 0.08, 2);
        
        // Valor líquido creditado na conta do piloto
        public double NetValue => Math.Round(GrossValue - BrokerFee, 2);

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
