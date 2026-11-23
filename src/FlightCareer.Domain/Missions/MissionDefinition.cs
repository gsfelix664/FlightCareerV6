using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    public sealed class MissionDestinationRule
    {
        public string Rule { get; set; } = "standard";
        public string? PreferredHubs { get; set; }
    }

    public sealed class FixedTextsDefinition
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class MissionDefinition
    {
        public string Id { get; set; } = string.Empty;
        public List<string> LegacyCodes { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MissionBoardKind Board { get; set; } = MissionBoardKind.Geral;
        public bool Vip { get; set; }
        public int Phase { get; set; } = 1;
        public string PayFamily { get; set; } = string.Empty;
        public string XpType { get; set; } = string.Empty;
        public string Generator { get; set; } = "standard";
        public MissionDestinationRule Destination { get; set; } = new();
        public FixedTextsDefinition? FixedTexts { get; set; }
        public int MinExpHours { get; set; }
        public int MaxExpHours { get; set; }
        public double Urgency { get; set; } = 1.0;
        public bool RequiresHardSurface { get; set; }
        public int MinRunwayDest { get; set; }
        public double LuggageWeightKg { get; set; } = 15.0;
        public List<ProbRange> RangeRules { get; set; } = new();
        public List<ProbRange> PaxRules { get; set; } = new();
        public List<ProbRange> LbsRules { get; set; } = new();

        // Opcionais reservados para a etapa 2 (lidos se existirem, sem efeito agora)
        public List<string>? Rules { get; set; }
        public List<string>? HudSteps { get; set; }
        public List<string>? Variants { get; set; }
        public Dictionary<string, object>? BaseByTier { get; set; }
        public Dictionary<string, object>? OriginProfiles { get; set; }
        public Dictionary<string, object>? DestinationProfiles { get; set; }
    }

    public sealed class MissionGroupItemDefinition
    {
        public string Id { get; set; } = string.Empty;
        public double Weight { get; set; }
    }

    public sealed class MissionGroupDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Weight { get; set; }
        public List<MissionGroupItemDefinition> Missions { get; set; } = new();
    }

    public sealed class TierLayoutDefinition
    {
        public List<int> AllowedDestinationTiers { get; set; } = new();
        public List<MissionGroupDefinition> Groups { get; set; } = new();
    }

    public sealed class AddGroupWeightDefinition
    {
        public string GroupId { get; set; } = string.Empty;
        public double Weight { get; set; }
    }

    public sealed class SetMissionWeightDefinition
    {
        public string GroupId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public double Weight { get; set; }
    }

    public sealed class AirportAdjustmentDefinition
    {
        public string Condition { get; set; } = string.Empty;
        public string? ImportGroupIfMissing { get; set; }
        public double ImportWeight { get; set; }
        public List<AddGroupWeightDefinition>? AddGroupWeights { get; set; }
        public List<SetMissionWeightDefinition>? SetMissionWeights { get; set; }
        public List<SetMissionWeightDefinition>? SetMissionWeightsIfFalse { get; set; }
    }
}
