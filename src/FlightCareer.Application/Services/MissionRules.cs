using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Missions;

namespace FlightCareer.Application.Services
{
    public class TierConfig
    {
        public List<int> AllowedDestinationTiers { get; set; } = new List<int>();
        public List<CategoryConfig> Categories { get; set; } = new List<CategoryConfig>();
    }

    public class CategoryConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Weight { get; set; }
        public List<SubCategoryConfig> SubCategories { get; set; } = new List<SubCategoryConfig>();
    }

    public class SubCategoryConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Weight { get; set; }
        public List<ProbRange> RangeRules { get; set; } = new List<ProbRange>();
        public List<ProbRange> PaxRules { get; set; } = new List<ProbRange>();
        public List<ProbRange> LbsRules { get; set; } = new List<ProbRange>();
        public int MinExpHours { get; set; }
        public int MaxExpHours { get; set; }
        public double Urgency { get; set; } = 1.0;
        public bool RequiresHardSurface { get; set; }
        public int MinRunwayDest { get; set; }
        public double LuggageWeightKg { get; set; } = 15;
        public List<string> PreferredDestinations { get; set; } = new List<string>();
        public Dictionary<string, string[]> PreferredDestinationsMap { get; set; } = new Dictionary<string, string[]>();

        public MissionDefinition? Definition { get; set; }
        public string Generator => Definition?.Generator ?? "standard";
        public MissionDestinationRule Destination => Definition?.Destination ?? new();
        public FixedTextsDefinition? FixedTexts => Definition?.FixedTexts;
    }

    /// <summary>
    /// Adaptador fino sobre o catálogo de missões (MissionCatalog).
    /// Mantém as estruturas TierConfig, CategoryConfig e SubCategoryConfig para compatibilidade com o gerador.
    /// </summary>
    public static class MissionRules
    {
        public static Dictionary<int, TierConfig> Rules { get; } = new();

        static MissionRules()
        {
            var catalog = MissionCatalog.Shared;
            foreach (var (tier, layout) in catalog.Tiers.OrderBy(kv => kv.Key))
            {
                var tierConfig = new TierConfig
                {
                    AllowedDestinationTiers = new List<int>(layout.AllowedDestinationTiers)
                };

                foreach (var group in layout.Groups)
                {
                    var catConfig = new CategoryConfig
                    {
                        Id = group.Id,
                        Name = group.Name,
                        Weight = group.Weight
                    };

                    foreach (var item in group.Missions)
                    {
                        var def = catalog.Get(item.Id);
                        if (def == null) continue;

                        var sub = new SubCategoryConfig
                        {
                            Id = def.Id,
                            Name = def.Name,
                            Description = def.Description,
                            Weight = item.Weight,
                            MinExpHours = def.MinExpHours,
                            MaxExpHours = def.MaxExpHours,
                            Urgency = def.Urgency,
                            RequiresHardSurface = def.RequiresHardSurface,
                            MinRunwayDest = def.MinRunwayDest,
                            LuggageWeightKg = def.LuggageWeightKg,
                            RangeRules = new List<ProbRange>(def.RangeRules),
                            PaxRules = new List<ProbRange>(def.PaxRules),
                            LbsRules = new List<ProbRange>(def.LbsRules),
                            Definition = def
                        };

                        catConfig.SubCategories.Add(sub);
                    }

                    tierConfig.Categories.Add(catConfig);
                }

                Rules[tier] = tierConfig;
            }
        }
    }
}
