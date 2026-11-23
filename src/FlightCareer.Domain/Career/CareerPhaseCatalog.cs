using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Career
{
    public sealed class PhaseDefinition
    {
        [JsonPropertyName("phase")]
        public int Phase { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("shortName")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("entryLevel")]
        public int EntryLevel { get; set; }

        [JsonPropertyName("minHours")]
        public double MinHours { get; set; }

        [JsonPropertyName("promotionCost")]
        public double PromotionCost { get; set; }

        [JsonPropertyName("licenseDisplay")]
        public string LicenseDisplay { get; set; } = string.Empty;

        [JsonPropertyName("badgeColor")]
        public string BadgeColor { get; set; } = "#3B82F6";

        [JsonPropertyName("operationalDescription")]
        public string OperationalDescription { get; set; } = string.Empty;
    }

    public sealed class CareerPhaseCatalogData
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("phases")]
        public List<PhaseDefinition> Phases { get; set; } = new();

        [JsonPropertyName("subIdToPhase")]
        public Dictionary<string, int> SubIdToPhase { get; set; } = new();
    }

    public sealed class CareerPhaseCatalog
    {
        private static readonly Lazy<CareerPhaseCatalog> _default = new(() => LoadDefault());
        public static CareerPhaseCatalog Instance => _default.Value;

        private readonly Dictionary<CareerPhase, PhaseDefinition> _phases;
        private readonly Dictionary<string, CareerPhase> _subIdToPhase;

        public IReadOnlyDictionary<CareerPhase, PhaseDefinition> Phases => _phases;
        public IReadOnlyDictionary<string, CareerPhase> SubIdToPhase => _subIdToPhase;

        public CareerPhaseCatalog(IEnumerable<PhaseDefinition> phases, IDictionary<string, int> subIdMap)
        {
            _phases = new Dictionary<CareerPhase, PhaseDefinition>();
            foreach (var p in phases)
            {
                if (Enum.IsDefined(typeof(CareerPhase), p.Phase))
                {
                    _phases[(CareerPhase)p.Phase] = p;
                }
            }

            _subIdToPhase = new Dictionary<string, CareerPhase>(StringComparer.OrdinalIgnoreCase);
            if (subIdMap != null)
            {
                foreach (var kvp in subIdMap)
                {
                    if (Enum.IsDefined(typeof(CareerPhase), kvp.Value))
                    {
                        _subIdToPhase[kvp.Key] = (CareerPhase)kvp.Value;
                    }
                }
            }
            foreach (var m in Missions.MissionCatalog.Shared.Missions.Values)
            {
                if (Enum.IsDefined(typeof(CareerPhase), m.Phase))
                {
                    _subIdToPhase[m.Id] = (CareerPhase)m.Phase;
                    foreach (var legacy in m.LegacyCodes)
                    {
                        _subIdToPhase[legacy] = (CareerPhase)m.Phase;
                    }
                }
            }
        }

        public PhaseDefinition GetPhase(CareerPhase phase)
        {
            if (_phases.TryGetValue(phase, out var def))
            {
                return def;
            }

            return GetDefaultPhaseDefinition(phase);
        }

        public CareerPhase ResolvePhase(string? subId, CareerPhase fallback = CareerPhase.Phase1_FlightSchool)
        {
            if (string.IsNullOrWhiteSpace(subId))
                return fallback;

            var def = Missions.MissionCatalog.Shared.Get(subId);
            if (def != null && Enum.IsDefined(typeof(CareerPhase), def.Phase))
            {
                return (CareerPhase)def.Phase;
            }

            if (_subIdToPhase.TryGetValue(subId, out var phase))
                return phase;

            return fallback;
        }

        /// <summary>
        /// Converte a categoria legada (1 a 5) para a fase de carreira correspondente:
        /// Regra 5: 1→1, 2→2, 3→3, 4→5, 5→6.
        /// </summary>
        public static CareerPhase ConvertLegacyCategory(int legacyCategory)
        {
            return legacyCategory switch
            {
                1 => CareerPhase.Phase1_FlightSchool,
                2 => CareerPhase.Phase2_SAE,
                3 => CareerPhase.Phase3_AirTaxi,
                4 => CareerPhase.Phase5_Commuter,
                5 => CareerPhase.Phase6_DomesticAirline,
                _ => CareerPhase.Phase1_FlightSchool
            };
        }

        public static CareerPhaseCatalog LoadDefault(string? directory = null)
        {
            string[] candidates = !string.IsNullOrEmpty(directory)
                ? new[] { Path.Combine(directory, "career_phases.json") }
                : new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "Data", "career_phases.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "career_phases.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "career_phases.json"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "career_phases.json")
                };

            foreach (var path in candidates)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var json = File.ReadAllText(path);
                        var data = JsonSerializer.Deserialize<CareerPhaseCatalogData>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (data != null && data.Phases.Count > 0)
                        {
                            return new CareerPhaseCatalog(data.Phases, data.SubIdToPhase);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CareerPhaseCatalog] Erro ao carregar '{path}': {ex.Message}. Usando padrões embutidos.");
                }
            }

            Debug.WriteLine("[CareerPhaseCatalog] Arquivo 'career_phases.json' não encontrado. Usando padrões embutidos.");
            return CreateDefaultCatalog();
        }

        public static CareerPhaseCatalog CreateDefaultCatalog()
        {
            var phases = new List<PhaseDefinition>
            {
                GetDefaultPhaseDefinition(CareerPhase.Phase1_FlightSchool),
                GetDefaultPhaseDefinition(CareerPhase.Phase2_SAE),
                GetDefaultPhaseDefinition(CareerPhase.Phase3_AirTaxi),
                GetDefaultPhaseDefinition(CareerPhase.Phase4_HighPerformanceAndMedevac),
                GetDefaultPhaseDefinition(CareerPhase.Phase5_Commuter),
                GetDefaultPhaseDefinition(CareerPhase.Phase6_DomesticAirline),
                GetDefaultPhaseDefinition(CareerPhase.Phase7_InternationalAirline)
            };

            return new CareerPhaseCatalog(phases, new Dictionary<string, int>());
        }

        private static PhaseDefinition GetDefaultPhaseDefinition(CareerPhase phase)
        {
            return phase switch
            {
                CareerPhase.Phase1_FlightSchool => new PhaseDefinition
                {
                    Phase = 1,
                    Name = "Escola de aviação / aeroclube",
                    ShortName = "Fase 1: Escola / Aeroclube",
                    EntryLevel = 1,
                    MinHours = 0.0,
                    PromotionCost = 0.0,
                    LicenseDisplay = "Piloto Privado (PPL)",
                    BadgeColor = "#3B82F6",
                    OperationalDescription = "Formação inicial, voos de instrução, navegações e cheques de aeroclube em aeronaves monomotoras leves."
                },
                CareerPhase.Phase2_SAE => new PhaseDefinition
                {
                    Phase = 2,
                    Name = "Serviços aéreos especializados (SAE)",
                    ShortName = "Fase 2: SAE & Carga Leve",
                    EntryLevel = 6,
                    MinHours = 40.0,
                    PromotionCost = 35000.0,
                    LicenseDisplay = "Piloto Comercial (CPL)",
                    BadgeColor = "#10B981",
                    OperationalDescription = "Trabalho aéreo remunerado, logística de fazenda/sede, apoio agrícola e desafios em pistas não pavimentadas."
                },
                CareerPhase.Phase3_AirTaxi => new PhaseDefinition
                {
                    Phase = 3,
                    Name = "Táxi aéreo e carga leve",
                    ShortName = "Fase 3: Táxi Aéreo & Charter",
                    EntryLevel = 13,
                    MinHours = 150.0,
                    PromotionCost = 120000.0,
                    LicenseDisplay = "IFR e Multimotor (MLTE)",
                    BadgeColor = "#F59E0B",
                    OperationalDescription = "Fretamento executivo regional, transporte de passageiros sob demanda, cargas expressas e peças AOG."
                },
                CareerPhase.Phase4_HighPerformanceAndMedevac => new PhaseDefinition
                {
                    Phase = 4,
                    Name = "Executiva de alta performance e MEDEVAC",
                    ShortName = "Fase 4: Executiva & MEDEVAC",
                    EntryLevel = 21,
                    MinHours = 300.0,
                    PromotionCost = 300000.0,
                    LicenseDisplay = "Type Rating Turboélice/Jato Leve e RVSM",
                    BadgeColor = "#06B6D4",
                    OperationalDescription = "Operações aeromédicas críticas, remoção de urgência, transporte VIP corporativo e voos diplomáticos em alta altitude."
                },
                CareerPhase.Phase5_Commuter => new PhaseDefinition
                {
                    Phase = 5,
                    Name = "Regional (commuter)",
                    ShortName = "Fase 5: Linha Regional",
                    EntryLevel = 29,
                    MinHours = 500.0,
                    PromotionCost = 500000.0,
                    LicenseDisplay = "ATPL — Fase Regional",
                    BadgeColor = "#8B5CF6",
                    OperationalDescription = "Linhas regionais regulares, conexões entre hubs e cidades do interior em aeronaves turboélice de grande porte e jatos regionais."
                },
                CareerPhase.Phase6_DomesticAirline => new PhaseDefinition
                {
                    Phase = 6,
                    Name = "Linha aérea doméstica (narrowbody)",
                    ShortName = "Fase 6: Linha Doméstica",
                    EntryLevel = 37,
                    MinHours = 1000.0,
                    PromotionCost = 1200000.0,
                    LicenseDisplay = "ATPL + Type Rating",
                    BadgeColor = "#EC4899",
                    OperationalDescription = "Rotas troncais domésticas de alta densidade, pontes aéreas e voos cargueiros pesados em aeronaves narrowbody (A320/B737)."
                },
                CareerPhase.Phase7_InternationalAirline => new PhaseDefinition
                {
                    Phase = 7,
                    Name = "Companhia internacional (widebody)",
                    ShortName = "Fase 7: Internacional / Long Haul",
                    EntryLevel = 45,
                    MinHours = 1500.0,
                    PromotionCost = 2000000.0,
                    LicenseDisplay = "Type Rating Widebody + ETOPS",
                    BadgeColor = "#E11D48",
                    OperationalDescription = "Travessias intercontinentais, operações ETOPS de longo alcance e malha global em aeronaves widebody (B777/B787/A330)."
                },
                _ => new PhaseDefinition
                {
                    Phase = (int)phase,
                    Name = $"Fase {(int)phase}",
                    ShortName = $"Fase {(int)phase}",
                    EntryLevel = 1,
                    BadgeColor = "#6B7280"
                }
            };
        }
    }
}
