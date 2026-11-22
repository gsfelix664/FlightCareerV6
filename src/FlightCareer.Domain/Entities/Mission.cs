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

        // Identificadores compatíveis com o Microsoft Flight Simulator (caso haja divergência ICAO)
        public string? OriginMsfsIdent { get; set; }
        public string? DestinationMsfsIdent { get; set; }

        public bool HasOriginDivergence => !string.IsNullOrEmpty(OriginMsfsIdent) && !string.Equals(OriginICAO, OriginMsfsIdent, StringComparison.OrdinalIgnoreCase);
        public bool HasDestinationDivergence => !string.IsNullOrEmpty(DestinationMsfsIdent) && !string.Equals(DestinationICAO, DestinationMsfsIdent, StringComparison.OrdinalIgnoreCase);

        public string OriginDisplayIdent => HasOriginDivergence ? $"{OriginICAO} (MSFS: {OriginMsfsIdent})" : OriginICAO;
        public string DestinationDisplayIdent => HasDestinationDivergence ? $"{DestinationICAO} (MSFS: {DestinationMsfsIdent})" : DestinationICAO;

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

        // Fase Regulatória Requerida da Missão (Fases 1 a 7)
        public CareerPhase RequiredPhase { get; set; } = CareerPhase.Phase1_FlightSchool;

        // Coluna mantida no banco para compatibilidade com schema legado
        [Obsolete("Utilize RequiredPhase")]
        public PilotCategory RequiredCategory { get; set; } = PilotCategory.Category1_FlightSchool;

        // Mantido para compatibilidade transitória
        [Obsolete("Utilize RequiredPhase")]
        public CompanyTier RequiredTier
        {
            get => (CompanyTier)(int)RequiredPhase;
            set => RequiredPhase = (CareerPhase)(int)value;
        }
        public string MissionCategoryType { get; set; } = "Instrucao"; // Instrucao, SAE_Foto, SAE_Reboque, SAE_Carga, Taxi_Charter, etc.
        /// <summary>Subcategoria do gerador (ex.: "3.4.1"): define a família de pagamento em mission_pay.json. Nulo em missões antigas.</summary>
        public string? SubId { get; set; }

        /// <summary>Número obrigatório de toques e arremetidas (TGL) exigidos na missão. 0 = não se aplica.</summary>
        public int RequiredTouchAndGoes { get; set; }

        /// <summary>Identificadores de pontos/aeródromos de sobrevoo obrigatório (ex.: "SBXX;SDYY;SNZZ"). Nulo/vazio = não se aplica.</summary>
        public string? OverflyWaypoints { get; set; }

        /// <summary>Parâmetros específicos do contrato serializados em JSON (geometria de pernas de levantamento ou zona/altitude de salto). Nulo em missões padrão.</summary>
        public string? ContractData { get; set; }
    }
}
