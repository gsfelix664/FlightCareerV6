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
    /// Mantém as estruturas TierConfig, CategoryConfig e SubCategoryConfig para compatibilidade.
    /// </summary>
    public static class MissionRules
    {
        public static Dictionary<int, TierConfig> Rules { get; } = new();

        static MissionRules()
        {
            var catalog = MissionCatalog.Shared;

            for (int tier = 1; tier <= 4; tier++)
            {
                var allowedDestTiers = tier switch
                {
                    1 => new List<int> { 1, 2 },
                    2 => new List<int> { 1, 2, 3 },
                    _ => new List<int> { 1, 2, 3, 4 }
                };

                var tierConfig = new TierConfig
                {
                    AllowedDestinationTiers = allowedDestTiers
                };

                string tierStr = tier.ToString();
                var missionsForTier = catalog.Missions.Values
                    .Where(m => m.BaseByTier.TryGetValue(tierStr, out var w) && w > 0)
                    .GroupBy(m => m.Board.ToString());

                foreach (var group in missionsForTier)
                {
                    var catConfig = new CategoryConfig
                    {
                        Id = group.Key.ToLowerInvariant(),
                        Name = group.Key,
                        Weight = 1.0
                    };

                    foreach (var def in group)
                    {
                        def.BaseByTier.TryGetValue(tierStr, out double weight);
                        var sub = new SubCategoryConfig
                        {
                            Id = def.Id,
                            Name = def.Name,
                            Description = def.Description,
                            Weight = weight,
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

                        // Compatibilidade: adicionar também sob os IDs legados
                        foreach (var legacy in def.LegacyCodes)
                        {
                            if (catConfig.SubCategories.All(s => s.Id != legacy))
                            {
                                catConfig.SubCategories.Add(new SubCategoryConfig
                                {
                                    Id = legacy,
                                    Name = def.Name,
                                    Description = def.Description,
                                    Weight = weight,
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
                                });
                            }
                        }
                    }

                    tierConfig.Categories.Add(catConfig);
                }

                // Categorias de compatibilidade para testes legados
                var allSubs = tierConfig.Categories.SelectMany(c => c.SubCategories).ToList();

                var aeroclubeCat = new CategoryConfig { Id = "aeroclube", Name = "Aeroclube", Weight = 1.0 };
                foreach (var legacyId in new[] { "voo_panoramico", "panoramico", "circuito_tgl", "translado_basico", "ferry_posicionamento" })
                {
                    var found = allSubs.FirstOrDefault(s => s.Id == legacyId);
                    if (found != null && aeroclubeCat.SubCategories.All(s => s.Id != found.Id))
                        aeroclubeCat.SubCategories.Add(found);
                }
                if (aeroclubeCat.SubCategories.Count > 0)
                    tierConfig.Categories.Add(aeroclubeCat);

                var contratosCat = new CategoryConfig { Id = $"contratos_carreira_t{tier}", Name = $"Contratos T{tier}", Weight = 1.0 };
                foreach (var legacyId in new[] { "fretamento_executivo", "carga_fragil", "medevac_dinamico", "vip_internacional_curto", "ponte_aerea_troncal" })
                {
                    var found = allSubs.FirstOrDefault(s => s.Id == legacyId) 
                                ?? catalog.Missions.Values.Where(m => m.LegacyCodes.Contains(legacyId)).Select(def => new SubCategoryConfig
                                {
                                    Id = legacyId,
                                    Name = def.Name,
                                    Description = def.Description,
                                    Weight = 1.0,
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
                                }).FirstOrDefault();

                    if (found != null && contratosCat.SubCategories.All(s => s.Id != found.Id))
                        contratosCat.SubCategories.Add(found);
                }
                if (contratosCat.SubCategories.Count > 0)
                    tierConfig.Categories.Add(contratosCat);

                Rules[tier] = tierConfig;
            }
        }
    }
}
