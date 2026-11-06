using System;

namespace FlightCareer.Domain.Entities
{
    public class Mission
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OriginICAO { get; set; } = string.Empty;
        public string DestinationICAO { get; set; } = string.Empty;
        public double PayloadLbs { get; set; }
        public int PassengerCount { get; set; }
        public double LuggageWeightLbs { get; set; }
        public double CargoWeightLbs { get; set; }
        public double Urgency { get; set; }
        public double RewardMoney { get; set; }
        public int ExperienceGain { get; set; }
        public double DistanceNM { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsAccepted { get; set; }
        public Guid? PilotId { get; set; }

        // Categoria Regulatória Requerida e Tipo da Missão
        public PilotCategory RequiredCategory { get; set; } = PilotCategory.Category1_FlightSchool;

        // Mantido para compatibilidade com código existente
        public CompanyTier RequiredTier
        {
            get => (CompanyTier)(int)RequiredCategory;
            set => RequiredCategory = (PilotCategory)(int)value;
        }
        public string MissionCategoryType { get; set; } = "Instrucao"; // Instrucao, SAE_Foto, SAE_Reboque, SAE_Carga, Taxi_Charter, etc.
    }
}
