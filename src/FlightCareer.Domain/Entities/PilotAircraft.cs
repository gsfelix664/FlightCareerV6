using System;

namespace FlightCareer.Domain.Entities
{
    public class PilotAircraft
    {
        public Guid Id { get; set; }
        public Guid PilotId { get; set; }
        public Pilot? Pilot { get; set; }
        public Guid AircraftTypeId { get; set; }
        public AircraftType? AircraftType { get; set; }
        public DateTime PurchaseDate { get; set; }
        public double CurrentAirframeHours { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public double EngineCondition { get; set; } = 100;
        public double TiresCondition { get; set; } = 100;
        public double AirframeCondition { get; set; } = 100;
        public double AvionicsCondition { get; set; } = 100;
        public int LandingsCount { get; set; }
        public double FuelAmount { get; set; }
        public string CurrentLocationICAO { get; set; } = string.Empty;
        public DateTime? InsuranceExpirationDate { get; set; }

        // Manutenção Regulatória & TBO
        public double EngineTboRemainingHours { get; set; } = 450; // Horas restantes para overhaul do motor
        public double HoursUntilNext50hInspection { get; set; } = 12;  // Troca de óleo (antiga 50h)
        public double HoursUntilNext100hInspection { get; set; } = 25; // Inspeção anual (antiga 100h)
        public double HoursUntilNext500hInspection { get; set; } = 125;  // Revisão de ignição (antiga 500h)
        public double MagnetosCondition { get; set; } = 100;            // Condição dos Magnetos & Ignição (0-100%)
        public double OilCondition { get; set; } = 100; // Saúde do Óleo e Sistema de Lubrificação (0-100%)
        public bool IsInterdicted { get; set; } = false;               // Aeronave interditada para voo
        public string InterdictionReason { get; set; } = string.Empty; // Motivo da interdição (ex: TBO vencido)
        public string Configuration { get; set; } = "Passageiro";     // "Passageiro", "Carga (Sem Assentos)"

        // Barras de saúde individuais por componente (motor, óleo, pneus, freios...)
        public List<AircraftComponent> Components { get; set; } = new();

        // Peças individuais com saúde oculta (F2.1)
        public List<AircraftPart> Parts { get; set; } = new();

        /// <summary>Data UTC em que as peças individuais desta aeronave foram migradas das barras/colunas legadas.</summary>
        public DateTime? PartsMigratedAtUtc { get; set; }
    }
}
