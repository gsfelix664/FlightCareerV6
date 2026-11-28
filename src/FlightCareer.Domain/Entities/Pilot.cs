using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Entities
{
    public enum DifficultyMode
    {
        Easy,
        Medium,
        Hard
    }

    public class Pilot
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string InitialBaseICAO { get; set; } = string.Empty;
        public string CurrentAirportICAO { get; set; } = string.Empty;
        public double Balance { get; set; }
        public DifficultyMode Difficulty { get; set; } = DifficultyMode.Medium;
        public double TotalHours { get; set; }
        public int ExperiencePoints { get; set; }
        public int Level => FlightCareer.Domain.Xp.PilotLevel.ForXp(ExperiencePoints);
        public string Rank { get; set; } = "Student Pilot";
        public Guid? ActiveAircraftId { get; set; }

        /// <summary>
        /// Relógio simulado de carreira UTC. Avança somente com tempo voado e paradas de oficina no Hangar,
        /// nunca pelo relógio do computador, para não penalizar jogadores inativos na vida real.
        /// </summary>
        public DateTime CareerClockUtc { get; set; } = DateTime.UtcNow;

        // Progressão da Carreira do Piloto (Fases 1 a 7)
        public CareerPhase CareerPhase { get; set; } = CareerPhase.Phase1_FlightSchool;

        // Coluna mantida no banco para compatibilidade com schema legado
        [Obsolete("Utilize CareerPhase")]
        public PilotCategory PilotCategory { get; set; } = PilotCategory.Category1_FlightSchool;

        // Mantido para compatibilidade transitória
        [Obsolete("Utilize CareerPhase")]
        public CompanyTier CompanyTier
        {
            get => (CompanyTier)(int)CareerPhase;
            set => CareerPhase = (CareerPhase)(int)value;
        }

        // Licenças e Habilitações Aeronáuticas
        public bool HasPPLicense { get; set; } = true;          // Piloto Privado (Inicial)
        public bool HasCPLicense { get; set; } = false;         // Piloto Comercial (PC)
        public bool HasIFRRating { get; set; } = false;         // Voo por Instrumentos (IFR)
        public bool HasMultiEngineRating { get; set; } = false; // Multi-motor (MLTE)
        public bool HasATPLicense { get; set; } = false;        // Linha Aérea (PLA)
        public bool HasCFIInstructor { get; set; } = false;     // Instrutor de Voo (INVA)
        
        public ICollection<Flight> Flights { get; set; } = new List<Flight>();
        public ICollection<PilotAircraft> Hangar { get; set; } = new List<PilotAircraft>();

        public void AddFlight(Flight flight)
        {
            Flights.Add(flight);
            TotalHours += flight.DurationHours;
            UpdateRank();
        }

        private void UpdateRank()
        {
            if (TotalHours >= 1500)
            {
                Rank = "Airline Transport Pilot";
                HasATPLicense = true;
                HasCPLicense = true;
                HasIFRRating = true;
                HasMultiEngineRating = true;
            }
            else if (TotalHours >= 250)
            {
                Rank = "Commercial Pilot";
                HasCPLicense = true;
                HasIFRRating = true;
            }
            else if (TotalHours >= 40)
            {
                Rank = "Private Pilot";
                HasPPLicense = true;
            }
        }
    }
}
