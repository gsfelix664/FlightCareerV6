using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;

namespace FlightCareer.Infrastructure.Services
{
    public enum RuleApplicabilityStatus
    {
        Active,
        InactiveMissingLimitOrParam,
        NotApplicableByFeature
    }

    public sealed record RuleApplicabilityResult(
        string RuleId,
        string Description,
        WearRuleKind Kind,
        RuleApplicabilityStatus Status,
        string? Reason
    );

    public sealed record AircraftCoverageSummary(
        string AircraftId,
        string Class,
        IReadOnlyList<string> Features,
        IReadOnlyList<RuleApplicabilityResult> Results
    )
    {
        public int ActiveCount => Results.Count(r => r.Status == RuleApplicabilityStatus.Active);
        public int InactiveMissingCount => Results.Count(r => r.Status == RuleApplicabilityStatus.InactiveMissingLimitOrParam);
        public int NotApplicableCount => Results.Count(r => r.Status == RuleApplicabilityStatus.NotApplicableByFeature);
    }

    /// <summary>
    /// Avalia a aplicabilidade das 47 regras de desgaste do catálogo para cada aeronave,
    /// identificando regras ativas, regras excluídas por feature e regras inativas por limite/parâmetro ausente.
    /// Gera o relatório consolidado de cobertura por avião.
    /// </summary>
    public sealed class RuleCoverageService
    {
        private readonly AircraftLimitCatalogLoader _loader;
        private readonly WearRuleCatalog _wearRuleCatalog;

        public RuleCoverageService(AircraftLimitCatalogLoader? loader = null, WearRuleCatalog? wearRuleCatalog = null)
        {
            _loader = loader ?? new AircraftLimitCatalogLoader();
            _wearRuleCatalog = wearRuleCatalog ?? WearRuleCatalog.LoadDefault();
        }

        public IReadOnlyList<AircraftCoverageSummary> EvaluateAll()
        {
            var resolver = _loader.CreateResolver();
            var profiles = _loader.LoadProfiles().OrderBy(p => p.AircraftId).ToList();
            var summaries = new List<AircraftCoverageSummary>();

            foreach (var profile in profiles)
            {
                var results = new List<RuleApplicabilityResult>();
                var featSet = new HashSet<string>(profile.Features, StringComparer.OrdinalIgnoreCase);

                foreach (var rule in _wearRuleCatalog.Rules)
                {
                    var (status, reason) = EvaluateRule(rule, profile, featSet, resolver);
                    results.Add(new RuleApplicabilityResult(rule.Id, rule.Description, rule.Kind, status, reason));
                }

                summaries.Add(new AircraftCoverageSummary(profile.AircraftId, profile.Class, profile.Features, results));
            }

            return summaries;
        }

        private static (RuleApplicabilityStatus Status, string? Reason) EvaluateRule(
            WearRule rule,
            AircraftLimitProfile profile,
            HashSet<string> featSet,
            AircraftLimitResolver resolver)
        {
            // 1. Verificação de Features (AppliesTo)
            if (!rule.AppliesTo.Matches(featSet))
            {
                var reasons = new List<string>();
                if (rule.AppliesTo.All != null && rule.AppliesTo.All.Count > 0)
                {
                    var missing = rule.AppliesTo.All.Where(f => !featSet.Contains(f)).ToList();
                    if (missing.Count > 0)
                        reasons.Add($"requer feature(s) obrigatória(s) [{string.Join(", ", missing)}]");
                }
                if (rule.AppliesTo.Any != null && rule.AppliesTo.Any.Count > 0)
                {
                    if (!rule.AppliesTo.Any.Any(f => featSet.Contains(f)))
                        reasons.Add($"requer ao menos uma feature dentre [{string.Join(", ", rule.AppliesTo.Any)}]");
                }
                if (rule.AppliesTo.None != null && rule.AppliesTo.None.Count > 0)
                {
                    var forbidden = rule.AppliesTo.None.Where(f => featSet.Contains(f)).ToList();
                    if (forbidden.Count > 0)
                        reasons.Add($"incompatível com feature(s) [{string.Join(", ", forbidden)}]");
                }

                string reason = reasons.Count > 0 ? string.Join("; ", reasons) : "filtros de feature da regra não atendidos";
                return (RuleApplicabilityStatus.NotApplicableByFeature, reason);
            }

            // 2. Verificação de Limites Operacionais para Regras Contínuas
            if (rule.Kind == WearRuleKind.Continuous)
            {
                if (string.Equals(rule.LimitKey, LimitKeys.VfeCurrent, StringComparison.OrdinalIgnoreCase))
                {
                    if (featSet.Contains("noFlaps") || featSet.Contains(AircraftFeatures.NoFlaps))
                    {
                        return (RuleApplicabilityStatus.NotApplicableByFeature, "aeronave sem flaps (feature 'noFlaps')");
                    }

                    // Verifica se existe algum limite Vfe configurado
                    bool hasAnyVfe = profile.Limits.Any(l => l.Key.StartsWith("vfe", StringComparison.OrdinalIgnoreCase));
                    if (!hasAnyVfe)
                    {
                        return (RuleApplicabilityStatus.InactiveMissingLimitOrParam, "nenhum limite Vfe configurado para os dentes de flap");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(rule.LimitKey))
                {
                    var limit = resolver.Get(profile, profile.Class, rule.LimitKey);
                    if (limit == null || double.IsNaN(limit.Value) || double.IsInfinity(limit.Value) || limit.Value == 0.0)
                    {
                        return (RuleApplicabilityStatus.InactiveMissingLimitOrParam, $"limite operacional '{rule.LimitKey}' ausente no perfil e nos padrões de classe");
                    }
                }
            }

            // 3. Verificação de Parâmetros de Catálogo para Regras de Evento
            if (rule.Kind == WearRuleKind.Event)
            {
                if (string.Equals(rule.Metric, WearRuleMetrics.AvionicsMasterStartSpikes, StringComparison.OrdinalIgnoreCase))
                {
                    var lim = resolver.Get(profile, profile.Class, LimitKeys.ElecAvionicsCircuitIndex);
                    if (lim == null || lim.Value <= 0)
                    {
                        return (RuleApplicabilityStatus.InactiveMissingLimitOrParam, $"parâmetro '{LimitKeys.ElecAvionicsCircuitIndex}' ausente no catálogo da aeronave");
                    }
                }
                else if (string.Equals(rule.Metric, WearRuleMetrics.BatteryDeepDischargeMinutes, StringComparison.OrdinalIgnoreCase))
                {
                    var busLim = resolver.Get(profile, profile.Class, LimitKeys.ElecMainBusIndex);
                    var voltLim = resolver.Get(profile, profile.Class, LimitKeys.ElecSystemVolts);
                    if (busLim == null || busLim.Value <= 0 || voltLim == null || voltLim.Value <= 0)
                    {
                        return (RuleApplicabilityStatus.InactiveMissingLimitOrParam, $"parâmetro '{LimitKeys.ElecMainBusIndex}' ou '{LimitKeys.ElecSystemVolts}' ausente");
                    }
                }
                else if (string.Equals(rule.Metric, WearRuleMetrics.ElectricalOverloadMinutes, StringComparison.OrdinalIgnoreCase))
                {
                    var ampsLim = resolver.Get(profile, profile.Class, LimitKeys.AlternatorRatedAmps);
                    var busLim = resolver.Get(profile, profile.Class, LimitKeys.ElecMainBusIndex);
                    var voltLim = resolver.Get(profile, profile.Class, LimitKeys.ElecSystemVolts);
                    if (ampsLim == null || ampsLim.Value <= 0 || busLim == null || busLim.Value <= 0 || voltLim == null || voltLim.Value <= 0)
                    {
                        return (RuleApplicabilityStatus.InactiveMissingLimitOrParam, $"parâmetro '{LimitKeys.AlternatorRatedAmps}', '{LimitKeys.ElecMainBusIndex}' ou '{LimitKeys.ElecSystemVolts}' ausente");
                    }
                }
            }

            return (RuleApplicabilityStatus.Active, null);
        }

        public string GenerateMarkdownReport()
        {
            var summaries = EvaluateAll();
            var sb = new StringBuilder();

            sb.AppendLine("# Relatório de Cobertura das Regras de Desgaste por Aeronave");
            sb.AppendLine();
            sb.AppendLine("> Relatório gerado automaticamente cobrindo os 46 modelos do catálogo contra as 47 regras de desgaste.");
            sb.AppendLine();

            // Resumo no topo por classe
            sb.AppendLine("## 1. Resumo por Classe");
            sb.AppendLine();
            sb.AppendLine("| Classe | Aeronaves | Média Ativas | Média Inativas (Falta Parâm.) | Média Não Aplicáveis (Feature) |");
            sb.AppendLine("|---|---|---|---|---|");

            var byClass = summaries.GroupBy(s => s.Class).OrderBy(g => g.Key);
            foreach (var g in byClass)
            {
                int count = g.Count();
                double avgActive = g.Average(s => s.ActiveCount);
                double avgInactive = g.Average(s => s.InactiveMissingCount);
                double avgNotApp = g.Average(s => s.NotApplicableCount);
                sb.AppendLine($"| **{g.Key}** | {count} | {avgActive:F1} | {avgInactive:F1} | {avgNotApp:F1} |");
            }

            int totalAircraft = summaries.Count;
            double grandAvgActive = summaries.Average(s => s.ActiveCount);
            double grandAvgInactive = summaries.Average(s => s.InactiveMissingCount);
            double grandAvgNotApp = summaries.Average(s => s.NotApplicableCount);
            sb.AppendLine($"| **Total Geral** | **{totalAircraft}** | **{grandAvgActive:F1}** | **{grandAvgInactive:F1}** | **{grandAvgNotApp:F1}** |");
            sb.AppendLine();

            // Tabela comparativa geral
            sb.AppendLine("## 2. Visão Geral dos 46 Modelos");
            sb.AppendLine();
            sb.AppendLine("| Modelo | Classe | Ativas | Inativas (Falta Parâm.) | Não Aplicáveis (Feature) | Total Regras |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var s in summaries)
            {
                sb.AppendLine($"| `{s.AircraftId}` | {s.Class} | {s.ActiveCount} | {s.InactiveMissingCount} | {s.NotApplicableCount} | {s.Results.Count} |");
            }
            sb.AppendLine();

            // Detalhes por aeronave
            sb.AppendLine("## 3. Detalhamento por Aeronave");
            sb.AppendLine();

            foreach (var s in summaries)
            {
                sb.AppendLine($"### `{s.AircraftId}` ({s.Class})");
                sb.AppendLine();
                sb.AppendLine($"- **Features:** `{string.Join(", ", s.Features)}`");
                sb.AppendLine($"- **Regras Ativas:** {s.ActiveCount} | **Inativas (Falta Parâm.):** {s.InactiveMissingCount} | **Não Aplicáveis (Feature):** {s.NotApplicableCount}");
                sb.AppendLine();

                var active = s.Results.Where(r => r.Status == RuleApplicabilityStatus.Active).ToList();
                var inactive = s.Results.Where(r => r.Status == RuleApplicabilityStatus.InactiveMissingLimitOrParam).ToList();
                var notApp = s.Results.Where(r => r.Status == RuleApplicabilityStatus.NotApplicableByFeature).ToList();

                if (inactive.Count > 0)
                {
                    sb.AppendLine("#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):");
                    foreach (var r in inactive)
                    {
                        sb.AppendLine($"- **`{r.RuleId}`** ({r.Kind}): {r.Reason}");
                    }
                    sb.AppendLine();
                }

                if (active.Count > 0)
                {
                    sb.AppendLine("#### ✅ Regras Ativas:");
                    sb.AppendLine(string.Join(", ", active.Select(r => $"`{r.RuleId}`")));
                    sb.AppendLine();
                }

                if (notApp.Count > 0)
                {
                    sb.AppendLine("<details>");
                    sb.AppendLine($"<summary>Regras não aplicáveis por feature ({notApp.Count})</summary>");
                    sb.AppendLine();
                    foreach (var r in notApp)
                    {
                        sb.AppendLine($"- `{r.RuleId}`: {r.Reason}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                    sb.AppendLine();
                }

                sb.AppendLine("---");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void SaveReport(string outputPath)
        {
            string markdown = GenerateMarkdownReport();
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(outputPath, markdown, Encoding.UTF8);
        }
    }
}
