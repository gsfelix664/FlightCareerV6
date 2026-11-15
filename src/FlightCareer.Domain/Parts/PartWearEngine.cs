using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Registro de excedência para gravação no blackbox e telemetria.
    /// </summary>
    public sealed record PartWearExceedance(
        string RuleId,
        string TargetId,
        string Source, // "Addon", "Simulador" ou "Simulador+Addon"
        double PeakValue,
        double LimitValue,
        double ExcessPct,
        double DurationSeconds,
        double TotalDamagePct,
        string Notes);

    /// <summary>
    /// Evento de inspeção gerado por excedência ou impacto operacional.
    /// </summary>
    public sealed record PartWearInspectionEvent(
        string DefectCode,
        string RuleId,
        string TargetPartId,
        bool IsGrounding,
        string Description);

    /// <summary>
    /// Resultado consolidado do tick de desgaste.
    /// </summary>
    public sealed record PartWearTickResult(
        IReadOnlyList<PartWearExceedance> Exceedances,
        IReadOnlyList<PartWearInspectionEvent> Inspections,
        IReadOnlyList<string> DiagnosticMessages);

    /// <summary>
    /// Resultado de um evento de desgaste discreto (ex.: toque de pouso, ciclo de partida).
    /// </summary>
    public sealed record PartWearEventResult(
        IReadOnlyList<PartWearInspectionEvent> Inspections,
        IReadOnlyList<string> DamagedPartIds,
        double TotalDamageApplied);

    /// <summary>
    /// Motor de desgaste por peça (F2.3).
    /// Calcula desgaste natural (100 -> ~30% ao longo de LifeHours/LifeCycles), estresse por regras
    /// contínuas calibradas pelos limites da aeronave, eventos operacionais e panes nativas.
    /// </summary>
    public sealed class PartWearEngine
    {
        private readonly PartCatalog _partCatalog;
        private readonly WearRuleCatalog _wearRuleCatalog;
        private readonly Dictionary<string, double> _exceedanceDurations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _exceedancePeaks = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _triggeredInspections = new(StringComparer.OrdinalIgnoreCase);

        public PartWearEngine(PartCatalog partCatalog, WearRuleCatalog wearRuleCatalog)
        {
            _partCatalog = partCatalog ?? throw new ArgumentNullException(nameof(partCatalog));
            _wearRuleCatalog = wearRuleCatalog ?? throw new ArgumentNullException(nameof(wearRuleCatalog));
        }

        public void ResetExceedanceTracking()
        {
            _exceedanceDurations.Clear();
            _exceedancePeaks.Clear();
            _triggeredInspections.Clear();
        }

        /// <summary>
        /// Aplica um ciclo contínuo de tempo (tick) sobre todas as peças da aeronave.
        /// </summary>
        public PartWearTickResult ApplyTick(
            IReadOnlyList<AircraftPart> parts,
            SensorFrame frame,
            double dtSimHours,
            IReadOnlyList<string> features,
            AircraftLimitResolver? resolver,
            AircraftLimitProfile? profile)
        {
            var exceedances = new List<PartWearExceedance>();
            var inspections = new List<PartWearInspectionEvent>();
            var diagnostics = new List<string>();

            if (parts == null || parts.Count == 0 || dtSimHours <= 0 || frame == null)
            {
                return new PartWearTickResult(exceedances, inspections, diagnostics);
            }

            double dtSeconds = dtSimHours * 3600.0;
            var featureSet = new HashSet<string>(features ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            // Mapeamento temporário de estresse acumulado por peça neste tick:
            // PartId -> (MaxMultiplier, SumLossPerHourPct, ActiveRules)
            var stressByPart = new Dictionary<string, (double MaxMultiplier, double SumLossPct, List<string> Rules)>(StringComparer.OrdinalIgnoreCase);

            // 1. Avalia regras contínuas de estresse
            foreach (var rule in _wearRuleCatalog.Rules)
            {
                if (rule.Kind != WearRuleKind.Continuous) continue;
                if (!rule.AppliesTo.Matches(featureSet)) continue;

                // Verifica presença dos sensores requeridos
                bool sensorsAvailable = true;
                double primarySensorValue = double.NaN;

                foreach (var sensorKey in rule.Sensors)
                {
                    if (!frame.Values.TryGetValue(sensorKey, out double val) || double.IsNaN(val) || double.IsInfinity(val))
                    {
                        sensorsAvailable = false;
                        diagnostics.Add($"[PartWearEngine] Sensor '{sensorKey}' ausente/NaN para regra '{rule.Id}'. Regra ignorada.");
                        break;
                    }

                    if (double.IsNaN(primarySensorValue))
                    {
                        primarySensorValue = val;
                    }
                }

                if (!sensorsAvailable) continue;

                // Resolução do limite operacional aplicável
                var limitInfo = ResolveLimit(rule, frame, resolver, profile);
                if (limitInfo == null || limitInfo.Value <= 0)
                {
                    // Sem limite resolvido: regra não pode avaliar estresse
                    continue;
                }

                double limitVal = limitInfo.Value;
                bool isClassDefault = limitInfo.Source == LimitSource.ClassDefault;

                // Verifica reserva nativa (Regra 0.1 › 3):
                // Só vale quando o limite daquele componente veio do padrão da classe (ClassDefault)
                bool nativeOverstressActive = false;
                bool simStressDisabled = false;

                if (isClassDefault && rule.NativeOverstress != null)
                {
                    string rateKey = $"nw-damage-rate-{rule.NativeOverstress.ComponentId}";
                    string levelKey = $"nw-level-{rule.NativeOverstress.ComponentId}";

                    if (frame.Values.TryGetValue(rateKey, out double nativeRate))
                    {
                        if (frame.Values.TryGetValue(levelKey, out double nativeLevel))
                        {
                            // Taxa reportada > 0 com nível travado em 1.00 = dano por estresse desativado no sim
                            if (nativeRate > 0.0001 && Math.Abs(nativeLevel - 1.0) < 0.0001)
                            {
                                simStressDisabled = true;
                                diagnostics.Add($"[PartWearEngine] Chave anti-trapaça: Dano por estresse nativo desativado no simulador (taxa={nativeRate:F5}, nível=1.00). Regra '{rule.Id}' ativada em faixa vermelha.");
                            }
                        }

                        if (nativeRate > rule.NativeOverstress.Threshold || simStressDisabled)
                        {
                            nativeOverstressActive = true;
                        }
                    }
                }

                // Identifica a faixa de estresse atingida (se houver)
                WearRuleBand? activeBand = null;
                double measuredVal = primarySensorValue;

                if (nativeOverstressActive)
                {
                    // Ativa a faixa mais severa (vermelha/severe/warning) por reserva nativa
                    activeBand = rule.Bands.LastOrDefault();
                }
                else
                {
                    // Avalia via sensor do addon contra o limite
                    activeBand = EvaluateActiveBand(rule, measuredVal, limitVal);
                }

                string trackingKey = $"{rule.Id}";

                if (activeBand != null)
                {
                    // Acumula tempo de duração e pico na faixa de estresse
                    double prevDur = _exceedanceDurations.GetValueOrDefault(trackingKey, 0.0);
                    double currentDur = prevDur + dtSeconds;
                    _exceedanceDurations[trackingKey] = currentDur;

                    double prevPeak = _exceedancePeaks.GetValueOrDefault(trackingKey, measuredVal);
                    double currentPeak = rule.Direction == LimitDirection.Above
                        ? Math.Max(prevPeak, measuredVal)
                        : Math.Min(prevPeak, measuredVal);
                    _exceedancePeaks[trackingKey] = currentPeak;

                    double excessPct = limitVal > 0 ? Math.Abs((measuredVal - limitVal) / limitVal) * 100.0 : 0.0;

                    // Aplica multiplicador e perda percentual nas peças da faixa
                    foreach (var bandPart in activeBand.Parts)
                    {
                        if (!stressByPart.TryGetValue(bandPart.PartId, out var currentStress))
                        {
                            currentStress = (1.0, 0.0, new List<string>());
                        }

                        double newMaxMult = Math.Max(currentStress.MaxMultiplier, bandPart.Multiplier);
                        double newLossPct = currentStress.SumLossPct + bandPart.LossPerHourPct;
                        currentStress.Rules.Add(rule.Id);

                        stressByPart[bandPart.PartId] = (newMaxMult, newLossPct, currentStress.Rules);

                        // Registra evento de excedência para blackbox
                        double damageInTick = (bandPart.LossPerHourPct * dtSimHours);
                        string sourceStr = nativeOverstressActive
                            ? (measuredVal >= limitVal ? "Simulador+Addon" : "Simulador")
                            : "Addon";

                        exceedances.Add(new PartWearExceedance(
                            rule.Id,
                            bandPart.PartId,
                            sourceStr,
                            currentPeak,
                            limitVal,
                            excessPct,
                            currentDur,
                            damageInTick,
                            $"Faixa '{activeBand.Name}' atingida"));
                    }

                    // Avalia disparo de inspeção da faixa (se configurada)
                    if (activeBand.Inspection != null)
                    {
                        var insp = activeBand.Inspection;
                        bool minSecMet = !insp.MinSeconds.HasValue || currentDur >= insp.MinSeconds.Value;
                        bool minExcessMet = !insp.MinExcessPct.HasValue || excessPct >= insp.MinExcessPct.Value;

                        string inspKey = $"{rule.Id}:{insp.DefectCode}";
                        if (minSecMet && minExcessMet && !_triggeredInspections.Contains(inspKey))
                        {
                            _triggeredInspections.Add(inspKey);
                            inspections.Add(new PartWearInspectionEvent(
                                insp.DefectCode,
                                rule.Id,
                                activeBand.Parts.FirstOrDefault()?.PartId ?? rule.Id,
                                insp.Grounding,
                                insp.Description));
                        }
                    }
                }
                else
                {
                    // Sem excesso: zera acumulador de excedência contínua
                    _exceedanceDurations.Remove(trackingKey);
                    _exceedancePeaks.Remove(trackingKey);
                }
            }

            // 2. Aplica o desgaste nas peças individuais (normal + estresse)
            foreach (var part in parts)
            {
                if (part.IsBroken) continue;

                var def = _partCatalog.Get(part.PartId);
                if (def == null) continue;

                // Desgaste natural normal (100 -> ~30% ao longo de LifeHours)
                // Taxa horária normal = 70.0 / LifeHours
                double normalRatePerHour = def.LifeHours > 0 ? (70.0 / def.LifeHours) : 0.0;
                part.HoursSinceReplacement += dtSimHours;

                double maxMultiplier = 1.0;
                double sumLossPerHourPct = 0.0;

                if (stressByPart.TryGetValue(part.PartId, out var stress))
                {
                    maxMultiplier = stress.MaxMultiplier;
                    sumLossPerHourPct = stress.SumLossPct;
                }

                // Fórmula da spec: estresse = normal * maior multiplicador ativo + soma(lossPerHourPct de regras diferentes)
                double totalRatePerHour = (normalRatePerHour * maxMultiplier) + sumLossPerHourPct;
                double totalLossPct = totalRatePerHour * dtSimHours;

                part.Health = Math.Clamp(part.Health - totalLossPct, 0.0, 100.0);
            }

            return new PartWearTickResult(exceedances, inspections, diagnostics);
        }

        /// <summary>
        /// Aplica um evento operacional discreto (ex.: toque de pouso, ciclo de trem, partida de motor).
        /// </summary>
        public PartWearEventResult ApplyEvent(
            IReadOnlyList<AircraftPart> parts,
            string metricKey,
            double metricValue,
            IReadOnlyList<string> features)
        {
            var inspections = new List<PartWearInspectionEvent>();
            var damagedParts = new List<string>();
            double totalDamageApplied = 0.0;

            if (parts == null || parts.Count == 0 || string.IsNullOrWhiteSpace(metricKey))
            {
                return new PartWearEventResult(inspections, damagedParts, totalDamageApplied);
            }

            var featureSet = new HashSet<string>(features ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var partMap = parts.ToDictionary(p => p.PartId, StringComparer.OrdinalIgnoreCase);

            foreach (var rule in _wearRuleCatalog.Rules)
            {
                if (rule.Kind != WearRuleKind.Event) continue;
                if (!string.Equals(rule.Metric, metricKey, StringComparison.OrdinalIgnoreCase)) continue;
                if (!rule.AppliesTo.Matches(featureSet)) continue;

                // Localiza a faixa atingida pelo valor escalar da métrica
                WearRuleBand? matchedBand = null;

                // Para regras de evento, o Threshold da banda é o valor numérico da métrica (ex.: 500, 700, 1000 fpm)
                // Procura a faixa de maior severidade cujo threshold foi satisfeito
                var candidateBands = rule.Bands
                    .Where(b => rule.Direction == LimitDirection.Above ? metricValue >= b.Threshold : metricValue <= b.Threshold)
                    .OrderBy(b => b.Threshold);

                matchedBand = rule.Direction == LimitDirection.Above
                    ? candidateBands.LastOrDefault()
                    : candidateBands.FirstOrDefault();

                if (matchedBand == null) continue;

                foreach (var bandPart in matchedBand.Parts)
                {
                    if (partMap.TryGetValue(bandPart.PartId, out var part))
                    {
                        if (part.IsBroken) continue;

                        double damage = bandPart.DamagePerEventPct;
                        if (damage > 0)
                        {
                            part.Health = Math.Clamp(part.Health - damage, 0.0, 100.0);
                            damagedParts.Add(part.PartId);
                            totalDamageApplied += damage;
                        }

                        // Se o evento representa ciclo de pouso/vida, incrementa CyclesSinceReplacement
                        if (metricKey.Equals(WearRuleMetrics.LandingCycles, StringComparison.OrdinalIgnoreCase))
                        {
                            part.CyclesSinceReplacement++;
                        }
                    }
                }

                if (matchedBand.Inspection != null)
                {
                    var insp = matchedBand.Inspection;
                    inspections.Add(new PartWearInspectionEvent(
                        insp.DefectCode,
                        rule.Id,
                        matchedBand.Parts.FirstOrDefault()?.PartId ?? rule.Id,
                        insp.Grounding,
                        insp.Description));
                }
            }

            return new PartWearEventResult(inspections, damagedParts, totalDamageApplied);
        }

        /// <summary>
        /// Aplica evento de quebra completa originada pelo simulador nativo (WEAR AND TEAR IS FAILED:id 0->1).
        /// Leva a saúde da peça a 0%, marca IsBroken = true e registra BrokenAtUtc.
        /// Idempotente: um evento por peça por voo (se já quebrada, ignora).
        /// </summary>
        /// <returns>True se a peça quebrou neste evento; false se já estava quebrada.</returns>
        public bool ApplyNativeFailed(AircraftPart part)
        {
            if (part == null) return false;
            if (part.IsBroken) return false; // Idempotente: uma quebra por peça por voo

            part.Health = 0.0;
            part.IsBroken = true;
            part.BrokenAtUtc = DateTime.UtcNow;
            return true;
        }

        private static AircraftLimit? ResolveLimit(
            WearRule rule,
            SensorFrame frame,
            AircraftLimitResolver? resolver,
            AircraftLimitProfile? profile)
        {
            if (string.IsNullOrWhiteSpace(rule.LimitKey)) return null;

            string targetKey = rule.LimitKey;

            // Tratamento especial para vfe:current (chave dinâmica resolvida pelo dente de flap em uso)
            if (targetKey.Equals(LimitKeys.VfeCurrent, StringComparison.OrdinalIgnoreCase))
            {
                if (!frame.Values.TryGetValue("af-flaps-handle-index", out double flapIdx) || flapIdx <= 0.05)
                {
                    // Flap recolhido: limite de flap não se aplica
                    return null;
                }

                int flapInt = (int)Math.Round(flapIdx);
                targetKey = $"vfe:{flapInt}";
            }

            if (resolver != null)
            {
                string aircraftClass = profile?.Class ?? string.Empty;
                var resolved = resolver.Get(profile, aircraftClass, targetKey);
                if (resolved != null) return resolved;

                // Se não achou vfe:n específico, fallback para vfe:1 se disponível
                if (targetKey.StartsWith("vfe:", StringComparison.OrdinalIgnoreCase))
                {
                    return resolver.Get(profile, aircraftClass, "vfe:1");
                }
            }
            else if (profile != null)
            {
                var candidate = profile.Limits.FirstOrDefault(l => l.Key.Equals(targetKey, StringComparison.OrdinalIgnoreCase));
                if (candidate != null) return candidate;

                if (targetKey.StartsWith("vfe:", StringComparison.OrdinalIgnoreCase))
                {
                    return profile.Limits.FirstOrDefault(l => l.Key.Equals("vfe:1", StringComparison.OrdinalIgnoreCase));
                }
            }

            return null;
        }

        private static WearRuleBand? EvaluateActiveBand(WearRule rule, double value, double limit)
        {
            if (limit <= 0) return null;

            if (rule.Direction == LimitDirection.Above)
            {
                // Ordena por threshold crescente (ex.: caution 0.97, warning 1.00, severe 1.08)
                // A última banda cujo threshold foi superado é a faixa mais severa ativa
                return rule.Bands
                    .OrderBy(b => b.Threshold)
                    .LastOrDefault(b => value >= (limit * b.Threshold));
            }
            else
            {
                // Ordena por threshold decrescente (ex.: caution 1.00, warning 0.80, severe 0.50)
                // A última banda cujo threshold foi cruzado para baixo é a mais severa
                return rule.Bands
                    .OrderByDescending(b => b.Threshold)
                    .LastOrDefault(b => value <= (limit * b.Threshold));
            }
        }
    }
}
