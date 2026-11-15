using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FlightCareer.Domain.Limits;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Catálogo de regras de desgaste por estresse e eventos operacionais.
    /// Carrega, valida e filtra as regras aplicáveis às características de cada aeronave.
    /// </summary>
    public sealed class WearRuleCatalog
    {
        private static readonly Regex ValidDefectCodePattern = new("^[A-Z0-9]+(_[A-Z0-9]+)*$", RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly Dictionary<string, WearRule> _rulesById;

        public IReadOnlyList<WearRule> Rules { get; }

        public WearRuleCatalog(IEnumerable<WearRule> rules)
        {
            Rules = rules.ToList();
            _rulesById = Rules.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        }

        public WearRule? Find(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId)) return null;
            return _rulesById.GetValueOrDefault(ruleId);
        }

        /// <summary>
        /// Retorna todas as regras que se aplicam a uma aeronave com o conjunto de features fornecido.
        /// </summary>
        public IReadOnlyList<WearRule> RulesFor(IReadOnlyCollection<string> features)
        {
            if (features == null) return Array.Empty<WearRule>();
            return Rules.Where(r => r.AppliesTo.Matches(features)).ToList();
        }

        /// <summary>
        /// Valida todas as regras do catálogo:
        /// - IDs únicos e não vazios
        /// - LimitKey existe no LimitKeys (para regras contínuas)
        /// - Metric não vazio (para regras de evento)
        /// - Sensores existem no registro (se validSensorKeys fornecido)
        /// - Peças citadas existem no catálogo de peças (se partCatalog fornecido)
        /// - Faixas em ordem crescente e multiplicadores/perdas não decrescentes (com progressão não linear)
        /// - AppliesTo utiliza apenas features do vocabulário fechado
        /// - Regra com NativeOverstress possui ComponentId entre 0 e 49
        /// - DefectCode de inspeções no formato MAIUSCULAS_COM_UNDERSCORE
        /// </summary>
        public void Validate(PartCatalog? partCatalog = null, IReadOnlySet<string>? validSensorKeys = null)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Id))
                {
                    throw new InvalidDataException("Encontrada regra de desgaste com Id vazio no catálogo.");
                }

                if (!seenIds.Add(rule.Id))
                {
                    throw new InvalidDataException($"Id de regra duplicado no catálogo: '{rule.Id}'.");
                }

                if (string.IsNullOrWhiteSpace(rule.Description))
                {
                    throw new InvalidDataException($"Regra '{rule.Id}' possui Descrição vazia.");
                }

                if (string.IsNullOrWhiteSpace(rule.Source))
                {
                    throw new InvalidDataException($"Regra '{rule.Id}' deve ter Source preenchido com a justificativa/fonte.");
                }

                if (rule.Sensors == null || rule.Sensors.Count == 0)
                {
                    throw new InvalidDataException($"Regra '{rule.Id}' deve referenciar ao menos um sensor.");
                }

                if (validSensorKeys != null)
                {
                    foreach (var s in rule.Sensors)
                    {
                        if (!validSensorKeys.Contains(s))
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}' referencia sensor '{s}' inexistente no registro de sensores.");
                        }
                    }
                }

                rule.AppliesTo.ValidateVocabulary(rule.Id);

                if (rule.Kind == WearRuleKind.Continuous)
                {
                    if (string.IsNullOrWhiteSpace(rule.LimitKey))
                    {
                        throw new InvalidDataException($"Regra contínua '{rule.Id}' deve especificar um LimitKey.");
                    }

                    if (!LimitKeys.IsValid(rule.LimitKey))
                    {
                        throw new InvalidDataException($"Regra '{rule.Id}' especifica LimitKey inválido: '{rule.LimitKey}'.");
                    }
                }
                else if (rule.Kind == WearRuleKind.Event)
                {
                    if (string.IsNullOrWhiteSpace(rule.Metric))
                    {
                        throw new InvalidDataException($"Regra de evento '{rule.Id}' deve especificar uma Metric.");
                    }
                }

                if (rule.NativeOverstress != null)
                {
                    if (rule.NativeOverstress.ComponentId < 0 || rule.NativeOverstress.ComponentId > 49)
                    {
                        throw new InvalidDataException($"Regra '{rule.Id}' tem NativeOverstress com ComponentId {rule.NativeOverstress.ComponentId} fora da faixa válida (0–49).");
                    }

                    if (rule.NativeOverstress.Threshold <= 0)
                    {
                        throw new InvalidDataException($"Regra '{rule.Id}' tem NativeOverstress com Threshold inválido: {rule.NativeOverstress.Threshold}.");
                    }
                }

                ValidateInspection(rule.Id, rule.Inspection);

                if (rule.Bands == null || rule.Bands.Count == 0)
                {
                    throw new InvalidDataException($"Regra '{rule.Id}' deve ter ao menos uma faixa configurada.");
                }

                // 1. Faixas em ordem crescente de Threshold
                for (int i = 0; i < rule.Bands.Count - 1; i++)
                {
                    if (rule.Bands[i].Threshold >= rule.Bands[i + 1].Threshold)
                    {
                        throw new InvalidDataException($"Regra '{rule.Id}': faixas fora de ordem crescente de limiar ({rule.Bands[i].Threshold} >= {rule.Bands[i + 1].Threshold}).");
                    }
                }

                // 2. Validação das peças e não-linearidade de multiplicadores e perdas
                var partProgression = new Dictionary<string, List<(double Multiplier, double LossPerHourPct, double DamagePerEventPct)>>();

                for (int bIndex = 0; bIndex < rule.Bands.Count; bIndex++)
                {
                    var band = rule.Bands[bIndex];
                    if (band.Parts == null || band.Parts.Count == 0)
                    {
                        throw new InvalidDataException($"Regra '{rule.Id}', faixa '{band.Name}': deve listar ao menos uma peça afetada.");
                    }

                    ValidateInspection(rule.Id, band.Inspection);

                    foreach (var p in band.Parts)
                    {
                        if (string.IsNullOrWhiteSpace(p.PartId))
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', faixa '{band.Name}': peça com Id vazio.");
                        }

                        if (partCatalog != null && partCatalog.Find(p.PartId) == null)
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', faixa '{band.Name}': peça '{p.PartId}' não existe no catálogo de peças.");
                        }

                        if (!partProgression.TryGetValue(p.PartId, out var list))
                        {
                            list = new List<(double, double, double)>();
                            partProgression[p.PartId] = list;
                        }

                        list.Add((p.Multiplier, p.LossPerHourPct, p.DamagePerEventPct));
                    }
                }

                // Verifica progressão não decrescente e aceleração não linear (o aumento entre faixas não diminui)
                foreach (var (partId, values) in partProgression)
                {
                    if (values.Count < 2) continue;

                    // Multiplier
                    double? prevDeltaMult = null;
                    for (int i = 0; i < values.Count - 1; i++)
                    {
                        double delta = values[i + 1].Multiplier - values[i].Multiplier;
                        if (delta < -1e-6)
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', peça '{partId}': multiplicador decresce entre faixas ({values[i].Multiplier} -> {values[i + 1].Multiplier}).");
                        }
                        if (prevDeltaMult.HasValue && delta < prevDeltaMult.Value - 1e-6)
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', peça '{partId}': progressão de multiplicador não é não-linear crescente (aceleração diminui: delta {delta} < {prevDeltaMult.Value}).");
                        }
                        prevDeltaMult = delta;
                    }

                    // LossPerHourPct
                    double? prevDeltaLoss = null;
                    for (int i = 0; i < values.Count - 1; i++)
                    {
                        double delta = values[i + 1].LossPerHourPct - values[i].LossPerHourPct;
                        if (delta < -1e-6)
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', peça '{partId}': perda por hora decresce entre faixas ({values[i].LossPerHourPct} -> {values[i + 1].LossPerHourPct}).");
                        }
                        if (prevDeltaLoss.HasValue && delta < prevDeltaLoss.Value - 1e-6)
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', peça '{partId}': progressão de perda por hora não é não-linear crescente (aceleração diminui: delta {delta} < {prevDeltaLoss.Value}).");
                        }
                        prevDeltaLoss = delta;
                    }

                    // DamagePerEventPct
                    double? prevDeltaDmg = null;
                    for (int i = 0; i < values.Count - 1; i++)
                    {
                        double delta = values[i + 1].DamagePerEventPct - values[i].DamagePerEventPct;
                        if (delta < -1e-6)
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', peça '{partId}': dano por evento decresce entre faixas ({values[i].DamagePerEventPct} -> {values[i + 1].DamagePerEventPct}).");
                        }
                        if (prevDeltaDmg.HasValue && delta < prevDeltaDmg.Value - 1e-6)
                        {
                            throw new InvalidDataException($"Regra '{rule.Id}', peça '{partId}': progressão de dano por evento não é não-linear crescente (aceleração diminui: delta {delta} < {prevDeltaDmg.Value}).");
                        }
                        prevDeltaDmg = delta;
                    }
                }
            }
        }

        private static void ValidateInspection(string ruleId, WearRuleInspection? inspection)
        {
            if (inspection == null) return;

            if (string.IsNullOrWhiteSpace(inspection.DefectCode))
            {
                throw new InvalidDataException($"Regra '{ruleId}': inspeção possui DefectCode vazio.");
            }

            if (!ValidDefectCodePattern.IsMatch(inspection.DefectCode))
            {
                throw new InvalidDataException($"Regra '{ruleId}': inspeção possui DefectCode inválido '{inspection.DefectCode}'. Deve estar em MAIUSCULAS_COM_UNDERSCORE.");
            }
        }

        public static WearRuleCatalog LoadFromJson(string json)
        {
            var dto = JsonSerializer.Deserialize<WearRuleCatalogDto>(json, JsonOpts)
                ?? throw new InvalidDataException("Arquivo JSON de catálogo de regras de desgaste vazio ou inválido.");

            var rules = dto.Rules?.Select(r => new WearRule(
                r.Id,
                r.Description,
                r.Kind,
                new PartAppliesTo(r.AppliesTo?.All, r.AppliesTo?.Any, r.AppliesTo?.None),
                r.Sensors ?? new List<string>(),
                r.LimitKey,
                r.Direction,
                r.Metric,
                r.Bands?.Select(b => new WearRuleBand(
                    b.Name,
                    b.Threshold,
                    b.Parts?.Select(p => new WearRulePart(
                        p.PartId,
                        p.Multiplier ?? 1.0,
                        p.LossPerHourPct ?? 0.0,
                        p.DamagePerEventPct ?? 0.0
                    )).ToList() ?? new List<WearRulePart>(),
                    b.Inspection != null ? new WearRuleInspection(
                        b.Inspection.DefectCode,
                        b.Inspection.Grounding,
                        b.Inspection.MinSeconds,
                        b.Inspection.MinExcessPct,
                        b.Inspection.Description ?? string.Empty
                    ) : null
                )).ToList() ?? new List<WearRuleBand>(),
                r.Inspection != null ? new WearRuleInspection(
                    r.Inspection.DefectCode,
                    r.Inspection.Grounding,
                    r.Inspection.MinSeconds,
                    r.Inspection.MinExcessPct,
                    r.Inspection.Description ?? string.Empty
                ) : null,
                r.NativeOverstress != null ? new NativeOverstress(
                    r.NativeOverstress.ComponentId,
                    r.NativeOverstress.Threshold
                ) : null,
                r.Source
            )).ToList() ?? new List<WearRule>();

            return new WearRuleCatalog(rules);
        }

        public static WearRuleCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "wear_rules.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "wear_rules.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "wear_rules.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "wear_rules.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "wear_rules.json") }.Concat(candidates).ToArray();
            }

            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null)
            {
                throw new FileNotFoundException($"Arquivo 'wear_rules.json' não encontrado. Buscado em: {string.Join(", ", candidates)}");
            }

            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        private class WearRuleCatalogDto
        {
            public List<WearRuleDto>? Rules { get; set; }
        }

        private class WearRuleDto
        {
            public string Id { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public WearRuleKind Kind { get; set; } = WearRuleKind.Continuous;
            public AppliesToDto? AppliesTo { get; set; }
            public List<string>? Sensors { get; set; }
            public string? LimitKey { get; set; }
            public LimitDirection Direction { get; set; } = LimitDirection.Above;
            public string? Metric { get; set; }
            public List<WearRuleBandDto>? Bands { get; set; }
            public WearRuleInspectionDto? Inspection { get; set; }
            public NativeOverstressDto? NativeOverstress { get; set; }
            public string Source { get; set; } = string.Empty;
        }

        private class WearRuleBandDto
        {
            public string Name { get; set; } = string.Empty;
            public double Threshold { get; set; }
            public List<WearRulePartDto>? Parts { get; set; }
            public WearRuleInspectionDto? Inspection { get; set; }
        }

        private class WearRulePartDto
        {
            public string PartId { get; set; } = string.Empty;
            public double? Multiplier { get; set; }
            public double? LossPerHourPct { get; set; }
            public double? DamagePerEventPct { get; set; }
        }

        private class WearRuleInspectionDto
        {
            public string DefectCode { get; set; } = string.Empty;
            public bool Grounding { get; set; }
            public double? MinSeconds { get; set; }
            public double? MinExcessPct { get; set; }
            public string? Description { get; set; }
        }

        private class NativeOverstressDto
        {
            public int ComponentId { get; set; }
            public double Threshold { get; set; }
        }

        private class AppliesToDto
        {
            public List<string>? All { get; set; }
            public List<string>? Any { get; set; }
            public List<string>? None { get; set; }
        }
    }
}
