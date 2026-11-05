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
        public int LandingsCount { get; set; }
        public double FuelAmount { get; set; }
        public string CurrentLocationICAO { get; set; } = string.Empty;
        public DateTime? InsuranceExpirationDate { get; set; }

        // Manutenção Regulatória & TBO
        public double EngineTboRemainingHours { get; set; } = 1800; // Horas restantes para overhaul do motor
        public double HoursUntilNext50hInspection { get; set; } = 50;  // Inspeção de 50h
        public double HoursUntilNext100hInspection { get; set; } = 100; // Inspeção de 100h
        public bool IsInterdicted { get; set; } = false;               // Aeronave interditada para voo
        public string InterdictionReason { get; set; } = string.Empty; // Motivo da interdição (ex: TBO vencido)
        public string Configuration { get; set; } = "Passageiro";     // "Passageiro", "Carga (Sem Assentos)"
    }
}
