using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Missions
{
    public sealed class MissionDestinationRule
    {
        public string Rule { get; set; } = "standard";
        public List<int> Tiers { get; set; } = new();
        public string Country { get; set; } = "any"; // any | same | other
        public string Runway { get; set; } = "any";  // any | paved | short_or_unpaved | lighted
        public double? MaxNm { get; set; }
        public Dictionary<string, double> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string>? RequireAnyTag { get; set; }
        /// <summary>Teto do produto dos multiplicadores de destino (padrão 12; missões de saúde usam mais).</summary>
        public double MaxMultiplier { get; set; } = MissionWeighting.DefaultDestinationMaxMultiplier;

        // Compatibilidade legada
        public string? PreferredHubs { get; set; }
    }

    public sealed class MissionProfileDefinition
    {
        public double Weight { get; set; }
        public int[] RangeNm { get; set; } = Array.Empty<int>();
        public int[] Pax { get; set; } = Array.Empty<int>();
        public double[] CargoLbs { get; set; } = Array.Empty<double>();
        public bool BigAirport { get; set; }
        public int MinRunwayFt { get; set; }
        public bool Paved { get; set; }
        public List<string>? RequireAnyTag { get; set; }
        public string? Variant { get; set; }
    }

    public sealed class MissionCrewDefinition
    {
        public int Count { get; set; }
        public double[] LuggageKg { get; set; } = Array.Empty<double>();
    }

    public sealed class FixedTextsDefinition
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class MissionDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public MissionBoardKind Board { get; set; } = MissionBoardKind.Geral;
        public bool Vip { get; set; }
        public int Phase { get; set; } = 1;
        public string PayFamily { get; set; } = string.Empty;
        public string XpType { get; set; } = string.Empty;
        public string Generator { get; set; } = "standard";
        public Dictionary<string, double> BaseByTier { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, double> OriginProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public bool CivilAtMilitaryBase { get; set; } = true;
        public MissionDestinationRule Destination { get; set; } = new();
        public List<MissionProfileDefinition> Profiles { get; set; } = new();
        public double[]? LuggagePerPersonKg { get; set; }
        public MissionCrewDefinition? Crew { get; set; }
        public FixedTextsDefinition? FixedTexts { get; set; }
        public int MinExpHours { get; set; } = 4;
        public int MaxExpHours { get; set; } = 24;
        public double Urgency { get; set; } = 1.0;
        public bool RequiresHardSurface { get; set; }
        public int MinRunwayDest { get; set; }
        public List<string>? RequireAnyTag { get; set; }
        public List<string> LegacyIds { get; set; } = new();

        // Compatibilidade com APIs e testes existentes
        public IReadOnlyList<string> LegacyCodes => LegacyIds;

        public List<ProbRange> RangeRules => Profiles
            .Select(p => new ProbRange(p.Weight, p.RangeNm.Length > 0 ? p.RangeNm[0] : 0, p.RangeNm.Length > 1 ? p.RangeNm[1] : 0))
            .ToList();

        public List<ProbRange> PaxRules => Profiles
            .Select(p => new ProbRange(p.Weight, p.Pax.Length > 0 ? p.Pax[0] : 0, p.Pax.Length > 1 ? p.Pax[1] : 0))
            .ToList();

        public List<ProbRange> LbsRules => Profiles
            .Select(p => new ProbRange(p.Weight, (int)(p.CargoLbs.Length > 0 ? p.CargoLbs[0] : 0), (int)(p.CargoLbs.Length > 1 ? p.CargoLbs[1] : 0)))
            .ToList();

        public double LuggageWeightKg => LuggagePerPersonKg != null && LuggagePerPersonKg.Length > 1
            ? (LuggagePerPersonKg[0] + LuggagePerPersonKg[1]) / 2.0
            : 0.0;
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
