using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Catálogo geral de peças do FlightCareer V6.
    /// Carrega, valida e filtra as peças aplicáveis a cada configuração de aeronave.
    /// </summary>
    public sealed class PartCatalog
    {
        private static readonly Regex ValidIdPattern = new("^[a-z0-9_]+\\.[a-z0-9_]+$", RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly Dictionary<string, PartDefinition> _partsById;

        public IReadOnlyList<PartDefinition> Parts { get; }

        public PartCatalog(IEnumerable<PartDefinition> parts)
        {
            Parts = parts.ToList();
            _partsById = Parts.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        }

        public PartDefinition? Find(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId)) return null;
            return _partsById.GetValueOrDefault(partId);
        }

        /// <summary>
        /// Retorna todas as peças do catálogo que se aplicam a uma aeronave com as features fornecidas.
        /// </summary>
        public IReadOnlyList<PartDefinition> PartsFor(IReadOnlyCollection<string> features)
        {
            if (features == null) return Array.Empty<PartDefinition>();
            return Parts.Where(p => p.AppliesTo.Matches(features)).ToList();
        }

        /// <summary>
        /// Valida todas as regras do catálogo:
        /// - IDs únicos e formato 'grupo.nome' em minúsculas
        /// - Grupo pertence ao vocabulário fechado PartGroup.All
        /// - AppliesTo utiliza apenas features do vocabulário fechado AircraftFeatures.All
        /// - LifeHours > 0 ou LifeCycles > 0
        /// - LifeSource não vazio
        /// - Sensores existem no registro (se validSensorKeys fornecido)
        /// </summary>
        public void Validate(IReadOnlySet<string>? validSensorKeys = null)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in Parts)
            {
                if (string.IsNullOrWhiteSpace(part.Id))
                {
                    throw new InvalidDataException("Encontrada peça com Id vazio no catálogo.");
                }

                if (!ValidIdPattern.IsMatch(part.Id))
                {
                    throw new InvalidDataException($"Id de peça '{part.Id}' inválido. Deve seguir o padrão 'grupo.nome' em letras minúsculas.");
                }

                if (!seenIds.Add(part.Id))
                {
                    throw new InvalidDataException($"Id de peça duplicado no catálogo: '{part.Id}'.");
                }

                if (string.IsNullOrWhiteSpace(part.Name))
                {
                    throw new InvalidDataException($"Peça '{part.Id}' possui Nome vazio.");
                }

                if (string.IsNullOrWhiteSpace(part.Group) || !PartGroup.All.Contains(part.Group))
                {
                    throw new InvalidDataException($"Peça '{part.Id}' possui Grupo '{part.Group}' inválido ou fora do vocabulário permitido.");
                }

                part.AppliesTo.ValidateVocabulary(part.Id);

                if (part.LifeHours <= 0 && (!part.LifeCycles.HasValue || part.LifeCycles.Value <= 0))
                {
                    throw new InvalidDataException($"Peça '{part.Id}' deve ter LifeHours > 0 ou LifeCycles > 0.");
                }

                if (string.IsNullOrWhiteSpace(part.LifeSource))
                {
                    throw new InvalidDataException($"Peça '{part.Id}' deve ter LifeSource preenchido com a origem da vida útil.");
                }

                if (part.RepairCostPctOfAircraft != null)
                {
                    if (part.RepairCostPctOfAircraft.Count != 2 ||
                        part.RepairCostPctOfAircraft[0] < 0 ||
                        part.RepairCostPctOfAircraft[0] > part.RepairCostPctOfAircraft[1])
                    {
                        throw new InvalidDataException($"Peça '{part.Id}' possui RepairCostPctOfAircraft inválido: deve ter 2 elementos [min, max] com 0 <= min <= max.");
                    }
                }

                if (part.DowntimeHours != null)
                {
                    if (part.DowntimeHours.Count != 2 ||
                        part.DowntimeHours[0] < 0 ||
                        part.DowntimeHours[0] > part.DowntimeHours[1])
                    {
                        throw new InvalidDataException($"Peça '{part.Id}' possui DowntimeHours inválido: deve ter 2 elementos [min, max] com 0 <= min <= max.");
                    }
                }

                if (part.CalendarLifeMonths.HasValue && part.CalendarLifeMonths.Value <= 0)
                {
                    throw new InvalidDataException($"Peça '{part.Id}' possui CalendarLifeMonths inválido: deve ser > 0 quando especificado.");
                }

                if (validSensorKeys != null && part.Sensors != null)
                {
                    foreach (var s in part.Sensors)
                    {
                        if (!validSensorKeys.Contains(s))
                        {
                            throw new InvalidDataException($"Peça '{part.Id}' referencia sensor '{s}' que não existe no registro de sensores.");
                        }
                    }
                }
            }
        }

        public static PartCatalog LoadFromJson(string json)
        {
            var dto = JsonSerializer.Deserialize<PartCatalogDto>(json, JsonOpts)
                ?? throw new InvalidDataException("Arquivo JSON de catálogo de peças vazio ou inválido.");

            var parts = dto.Parts?.Select(d => new PartDefinition(
                d.Id,
                d.Name,
                d.Group,
                new PartAppliesTo(d.AppliesTo?.All, d.AppliesTo?.Any, d.AppliesTo?.None),
                d.LifeHours,
                d.LifeCycles,
                d.VisibleOnWalkaround,
                d.BlocksFlightWhenBroken,
                d.PartPrice,
                d.LaborHours,
                d.Sensors ?? new List<string>(),
                d.LifeSource,
                d.PerEngine,
                d.PriceScalesWithAircraft,
                d.RepairCostPctOfAircraft,
                d.DowntimeHours,
                d.FailureEffect,
                d.CalendarLifeMonths
            )).ToList() ?? new List<PartDefinition>();

            return new PartCatalog(parts);
        }

        public static PartCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "parts_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "parts_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "parts_catalog.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "parts_catalog.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "parts_catalog.json") }.Concat(candidates).ToArray();
            }

            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null)
            {
                throw new FileNotFoundException($"Arquivo 'parts_catalog.json' não encontrado. Buscado em: {string.Join(", ", candidates)}");
            }

            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        private class PartCatalogDto
        {
            public List<PartItemDto>? Parts { get; set; }
        }

        private class PartItemDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Group { get; set; } = string.Empty;
            public AppliesToDto? AppliesTo { get; set; }
            public double LifeHours { get; set; }
            public int? LifeCycles { get; set; }
            public bool VisibleOnWalkaround { get; set; }
            public bool BlocksFlightWhenBroken { get; set; }
            public decimal PartPrice { get; set; }
            public double LaborHours { get; set; }
            public List<string>? Sensors { get; set; }
            public string LifeSource { get; set; } = string.Empty;
            public bool PerEngine { get; set; }
            public bool PriceScalesWithAircraft { get; set; }
            public List<double>? RepairCostPctOfAircraft { get; set; }
            public List<double>? DowntimeHours { get; set; }
            public string? FailureEffect { get; set; }
            public int? CalendarLifeMonths { get; set; }
        }

        private class AppliesToDto
        {
            public List<string>? All { get; set; }
            public List<string>? Any { get; set; }
            public List<string>? None { get; set; }
        }
    }
}
