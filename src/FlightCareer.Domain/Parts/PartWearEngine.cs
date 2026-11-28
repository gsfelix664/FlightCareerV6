using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FlightCareer.Domain.Components;
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
        private readonly IRandomSource _random;
        private readonly Dictionary<string, double> _exceedanceDurations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _exceedancePeaks = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _triggeredInspections = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, bool> _engineWasCombustion = new();
        private readonly Dictionary<int, double> _engineCombustionTimeSeconds = new();
        private readonly Dictionary<string, bool> _previousSensorActive = new(StringComparer.OrdinalIgnoreCase);
        private readonly PartBandSettings _bandSettings;

        /// <summary>
        /// Ponto de teste para testes de mutação / fuzz: desativa a resolução de sensores com sufixo :n (ex.: chave:1).
        /// </summary>
        public bool DisablePerEngineSensorResolution { get; set; }

        public PartWearEngine(PartCatalog partCatalog, WearRuleCatalog wearRuleCatalog, IRandomSource? random = null, PartBandSettings? bandSettings = null)
        {
            _partCatalog = partCatalog ?? throw new ArgumentNullException(nameof(partCatalog));
            _wearRuleCatalog = wearRuleCatalog ?? throw new ArgumentNullException(nameof(wearRuleCatalog));
            _random = random ?? new FlightCareer.Domain.Components.SystemRandomSource();
            _bandSettings = bandSettings ?? new PartBandSettings();
        }

        public PartBandSettings BandSettings => _bandSettings;

        public void ResetExceedanceTracking()
        {
            _exceedanceDurations.Clear();
            _exceedancePeaks.Clear();
            _triggeredInspections.Clear();
            _engineWasCombustion.Clear();
            _engineCombustionTimeSeconds.Clear();
            _previousSensorActive.Clear();
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

            // Mapeamento temporário de estresse acumulado por (PartId, EngineIndex) neste tick:
            // (PartId, EngineIndex) -> (MaxMultiplier, SumLossPerHourPct, ActiveRules)
            var stressByPart = new Dictionary<(string PartId, int EngineIndex), (double MaxMultiplier, double SumLossPct, List<string> Rules)>();

            int engineCount = DetectEngineCount(frame);

            // Rastreamento de combustão e tempo estabilizado por motor (tempo simulado)
            for (int eng = 1; eng <= engineCount; eng++)
            {
                bool isComb = false;
                if (frame.Values.TryGetValue($"eng-combustion:{eng}", out double cVal) ||
                    (eng == 1 && frame.Values.TryGetValue("eng-combustion", out cVal)))
                {
                    isComb = cVal > 0.5;
                }

                bool prevComb = _engineWasCombustion.GetValueOrDefault(eng, false);
                if (isComb)
                {
                    if (!prevComb)
                    {
                        _engineCombustionTimeSeconds[eng] = dtSeconds;
                    }
                    else
                    {
                        _engineCombustionTimeSeconds[eng] = _engineCombustionTimeSeconds.GetValueOrDefault(eng, 0.0) + dtSeconds;
                    }
                }
                else
                {
                    _engineCombustionTimeSeconds[eng] = 0.0;
                }
                _engineWasCombustion[eng] = isComb;
            }

            // 1. Avalia regras contínuas de estresse
            foreach (var rule in _wearRuleCatalog.Rules)
            {
                if (rule.Kind != WearRuleKind.Continuous) continue;
                if (!rule.AppliesTo.Matches(featureSet)) continue;

                // Determina se a regra é avaliada por motor ou global
                bool isPerEngineRule = false;
                bool sensorsAvailable = true;

                foreach (var sensorKey in rule.Sensors)
                {
                    if (!DisablePerEngineSensorResolution && frame.Values.ContainsKey($"{sensorKey}:1"))
                    {
                        // Sensor possui índice por motor no frame (prioridade para avaliar por motor se disponível, ex: eng-cht-cooling-rate decorado por PartFlightTracker)
                        isPerEngineRule = true;
                    }
                    else if (frame.Values.ContainsKey(sensorKey))
                    {
                        // Sensor global com chave exata disponível
                    }
                    else
                    {
                        sensorsAvailable = false;
                        diagnostics.Add($"[PartWearEngine] Sensor '{sensorKey}' (nem '{sensorKey}:1') ausente/NaN para regra '{rule.Id}'. Regra ignorada.");
                    }
                }

                if (!sensorsAvailable) continue;

                // Resolução do limite operacional aplicável
                var limitInfo = ResolveLimit(rule, frame, resolver, profile);
                if (limitInfo == null || double.IsNaN(limitInfo.Value) || double.IsInfinity(limitInfo.Value) || limitInfo.Value == 0.0)
                {
                    // Sem limite resolvido ou limite zero/inválido: regra não pode avaliar estresse
                    continue;
                }

                if (limitInfo.Value < 0.0 && rule.Direction == LimitDirection.Above)
                {
                    diagnostics.Add($"[PartWearEngine] Regra '{rule.Id}' possui direção Above com limite negativo ({limitInfo.Value}). Regra ignorada.");
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
                    string rateKey1 = $"nw-overstress-rate-{rule.NativeOverstress.ComponentId}";
                    string rateKey2 = $"nw-damage-rate-{rule.NativeOverstress.ComponentId}";
                    string levelKey = $"nw-level-{rule.NativeOverstress.ComponentId}";

                    double nativeRate = 0;
                    bool hasRate = frame.Values.TryGetValue(rateKey1, out nativeRate) || frame.Values.TryGetValue(rateKey2, out nativeRate);

                    if (hasRate)
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

                // Lista de índices de motor a avaliar para esta regra (1..engineCount para regras por motor, ou null para global)
                var engineIndices = isPerEngineRule
                    ? Enumerable.Range(1, engineCount).Select(i => (int?)i).ToList()
                    : new List<int?> { null };

                // Rastreio do pior estresse sofrido entre os motores para peças com EngineIndex == 0 (compartilhadas)
                Dictionary<string, (double WorstMultiplier, double WorstLossPct)>? worstStressForSharedParts =
                    isPerEngineRule ? new Dictionary<string, (double WorstMultiplier, double WorstLossPct)>(StringComparer.OrdinalIgnoreCase) : null;

                foreach (var engIdx in engineIndices)
                {
                    string trackingKey = engIdx.HasValue ? $"{rule.Id}#{engIdx.Value}" : rule.Id;

                    if (rule.RequiresEngineRunning)
                    {
                        bool engineReady = false;
                        if (engIdx.HasValue)
                        {
                            bool running = _engineWasCombustion.GetValueOrDefault(engIdx.Value, false);
                            double time = _engineCombustionTimeSeconds.GetValueOrDefault(engIdx.Value, 0.0);
                            engineReady = running && time >= rule.EngineRunningDelaySeconds;
                        }
                        else
                        {
                            for (int e = 1; e <= engineCount; e++)
                            {
                                if (_engineWasCombustion.GetValueOrDefault(e, false) &&
                                    _engineCombustionTimeSeconds.GetValueOrDefault(e, 0.0) >= rule.EngineRunningDelaySeconds)
                                {
                                    engineReady = true;
                                    break;
                                }
                            }
                        }

                        if (!engineReady)
                        {
                            _exceedanceDurations.Remove(trackingKey);
                            _exceedancePeaks.Remove(trackingKey);
                            continue;
                        }
                    }

                    double primarySensorValue = double.NaN;
                    bool engineSensorsValid = true;

                    foreach (var sensorKey in rule.Sensors)
                    {
                        double val;
                        bool found = false;

                        if (engIdx.HasValue)
                        {
                            if (frame.Values.TryGetValue($"{sensorKey}:{engIdx.Value}", out val) && !double.IsNaN(val) && !double.IsInfinity(val))
                            {
                                found = true;
                            }
                            else if (frame.Values.TryGetValue(sensorKey, out val) && !double.IsNaN(val) && !double.IsInfinity(val))
                            {
                                found = true;
                            }
                        }
                        else
                        {
                            if (frame.Values.TryGetValue(sensorKey, out val) && !double.IsNaN(val) && !double.IsInfinity(val))
                            {
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            engineSensorsValid = false;
                            break;
                        }

                        if (double.IsNaN(primarySensorValue))
                        {
                            primarySensorValue = val;
                        }
                    }

                    if (!engineSensorsValid) continue;

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

                        double excessPct = limitVal != 0.0 ? Math.Abs((measuredVal - limitVal) / limitVal) * 100.0 : 0.0;

                        // Aplica multiplicador e perda percentual nas peças da faixa
                        foreach (var bandPart in activeBand.Parts)
                        {
                            if (engIdx.HasValue)
                            {
                                // Aplica diretamente para a instância da peça daquele motor
                                var partKey = (bandPart.PartId.ToLowerInvariant(), engIdx.Value);
                                if (!stressByPart.TryGetValue(partKey, out var currentStress))
                                {
                                    currentStress = (1.0, 0.0, new List<string>());
                                }

                                double newMaxMult = Math.Max(currentStress.MaxMultiplier, bandPart.Multiplier);
                                double newLossPct = currentStress.SumLossPct + bandPart.LossPerHourPct;
                                currentStress.Rules.Add(rule.Id);
                                stressByPart[partKey] = (newMaxMult, newLossPct, currentStress.Rules);

                                // Para peças com EngineIndex == 0 (não são por motor), rastreia o pior motor
                                if (worstStressForSharedParts != null)
                                {
                                    var currentWorst = worstStressForSharedParts.TryGetValue(bandPart.PartId, out var existingWorst)
                                        ? existingWorst
                                        : (WorstMultiplier: 1.0, WorstLossPct: 0.0);

                                    worstStressForSharedParts[bandPart.PartId] = (
                                        WorstMultiplier: Math.Max(currentWorst.WorstMultiplier, bandPart.Multiplier),
                                        WorstLossPct: Math.Max(currentWorst.WorstLossPct, bandPart.LossPerHourPct)
                                    );
                                }
                            }
                            else
                            {
                                // Regra global: aplica a todas as instâncias da peça instaladas
                                foreach (var p in parts.Where(x => x.PartId.Equals(bandPart.PartId, StringComparison.OrdinalIgnoreCase)))
                                {
                                    var partKey = (p.PartId.ToLowerInvariant(), p.EngineIndex);
                                    if (!stressByPart.TryGetValue(partKey, out var currentStress))
                                    {
                                        currentStress = (1.0, 0.0, new List<string>());
                                    }

                                    double newMaxMult = Math.Max(currentStress.MaxMultiplier, bandPart.Multiplier);
                                    double newLossPct = currentStress.SumLossPct + bandPart.LossPerHourPct;
                                    currentStress.Rules.Add(rule.Id);
                                    stressByPart[partKey] = (newMaxMult, newLossPct, currentStress.Rules);
                                }
                            }

                            // Registra evento de excedência para blackbox apenas se houver efeito ou inspeção
                            bool hasEffect = (bandPart.Multiplier > 1.0 || bandPart.LossPerHourPct > 0.0) || activeBand.Inspection != null;
                            if (hasEffect)
                            {
                                double damageInTick = (bandPart.LossPerHourPct * dtSimHours);
                                string sourceStr = nativeOverstressActive
                                    ? ((rule.Direction == LimitDirection.Above ? measuredVal >= limitVal : measuredVal <= limitVal) ? "Simulador+Addon" : "Simulador")
                                    : "Addon";

                                string targetId = engIdx.HasValue ? $"{bandPart.PartId}:{engIdx.Value}" : bandPart.PartId;
                                string notes = engIdx.HasValue
                                    ? $"Faixa '{activeBand.Name}' atingida (Motor {engIdx.Value})"
                                    : $"Faixa '{activeBand.Name}' atingida";

                                exceedances.Add(new PartWearExceedance(
                                    rule.Id,
                                    targetId,
                                    sourceStr,
                                    currentPeak,
                                    limitVal,
                                    excessPct,
                                    currentDur,
                                    damageInTick,
                                    notes));
                            }
                        }

                        // Avalia disparo de inspeção da faixa (se configurada)
                        if (activeBand.Inspection != null)
                        {
                            var insp = activeBand.Inspection;
                            bool minSecMet = !insp.MinSeconds.HasValue || currentDur >= insp.MinSeconds.Value;
                            bool minExcessMet = !insp.MinExcessPct.HasValue || excessPct >= insp.MinExcessPct.Value;

                            string inspKey = engIdx.HasValue
                                ? $"{rule.Id}#{engIdx.Value}:{insp.DefectCode}"
                                : $"{rule.Id}:{insp.DefectCode}";

                            if (minSecMet && minExcessMet && !_triggeredInspections.Contains(inspKey))
                            {
                                _triggeredInspections.Add(inspKey);
                                string targetPart = activeBand.Parts.FirstOrDefault()?.PartId ?? rule.Id;
                                if (engIdx.HasValue) targetPart = $"{targetPart}:{engIdx.Value}";

                                inspections.Add(new PartWearInspectionEvent(
                                    insp.DefectCode,
                                    rule.Id,
                                    targetPart,
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

                // Aplica o estresse do pior motor nas peças com EngineIndex == 0 (compartilhadas)
                if (worstStressForSharedParts != null)
                {
                    foreach (var (partId, (worstMultiplier, worstLossPct)) in worstStressForSharedParts)
                    {
                        if (worstMultiplier > 1.0 || worstLossPct > 0)
                        {
                            var sharedKey = (partId.ToLowerInvariant(), 0);
                            if (!stressByPart.TryGetValue(sharedKey, out var currentSharedStress))
                            {
                                currentSharedStress = (1.0, 0.0, new List<string>());
                            }

                            double newMaxMult = Math.Max(currentSharedStress.MaxMultiplier, worstMultiplier);
                            double newLossPct = currentSharedStress.SumLossPct + worstLossPct;
                            currentSharedStress.Rules.Add(rule.Id);
                            stressByPart[sharedKey] = (newMaxMult, newLossPct, currentSharedStress.Rules);
                        }
                    }
                }
            }

            // 2. Aplica o desgaste nas peças individuais (normal + estresse + uso)
            foreach (var part in parts)
            {
                if (part.IsBroken) continue;

                var def = _partCatalog.Get(part.PartId);
                if (def == null) continue;

                // Peças condicionadas a sensor de uso (ex.: lâmpadas com usageSensor)
                if (!string.IsNullOrWhiteSpace(def.UsageSensor))
                {
                    bool hasSensor = frame.Values.TryGetValue(def.UsageSensor, out double sensorVal);
                    bool isUsageActive = hasSensor && sensorVal >= 0.5;

                    if (hasSensor)
                    {
                        var cycleKey = $"{part.PartId}:{part.EngineIndex}";
                        if (_previousSensorActive.TryGetValue(cycleKey, out bool wasActive))
                        {
                            if (!wasActive && isUsageActive && def.DamagePerOnCyclePct > 0)
                            {
                                part.Health = Math.Clamp(part.Health - def.DamagePerOnCyclePct, 0.0, 100.0);
                            }
                        }
                        _previousSensorActive[cycleKey] = isUsageActive;
                    }

                    if (!isUsageActive)
                    {
                        // Sensor ausente ou desligado: sem desgaste natural, sem horas acesas, sem sorteio
                        continue;
                    }

                    // Luz acesa: desgasta por horas de uso
                    double lightRatePerHour = def.LifeHours > 0 ? (70.0 / def.LifeHours) : 0.0;
                    lightRatePerHour *= PartHealthBands.LowHealthWearMultiplier(part.Health, _bandSettings);
                    part.HoursSinceReplacement += dtSimHours;

                    double lightLossPct = lightRatePerHour * dtSimHours;
                    part.Health = Math.Clamp(part.Health - lightLossPct, 0.0, 100.0);

                    // Pane por taxa horária enquanto acesa
                    if (def.FailsByUsageRate)
                    {
                        if (UsageFailureModel.Roll(part.Health, dtSimHours, _random))
                        {
                            part.IsBroken = true;
                            part.Health = 0.0;
                            diagnostics.Add($"Lâmpada queimada: {def.Name}");
                        }
                    }

                    continue;
                }

                // Desgaste natural normal (100 -> ~30% ao longo de LifeHours)
                // Taxa horária normal = 70.0 / LifeHours
                double normalRatePerHour = def.LifeHours > 0 ? (70.0 / def.LifeHours) : 0.0;
                normalRatePerHour *= PartHealthBands.LowHealthWearMultiplier(part.Health, _bandSettings);
                part.HoursSinceReplacement += dtSimHours;

                double maxMultiplier = 1.0;
                double sumLossPerHourPct = 0.0;

                var partKey = (part.PartId.ToLowerInvariant(), part.EngineIndex);
                if (stressByPart.TryGetValue(partKey, out var stress))
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

            var partsByPartId = parts
                .GroupBy(p => p.PartId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

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
                    if (partsByPartId.TryGetValue(bandPart.PartId, out var targetList))
                    {
                        foreach (var part in targetList)
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
        /// Ciclo de pouso (toque que não é bounce): aplica as regras de <see cref="WearRuleMetrics.LandingCycles"/> e,
        /// em toda peça com vida em ciclos (pneus, freios, rodas…), conta +1 ciclo e aplica o desgaste natural por ciclo
        /// (100 → ~30 % ao longo de LifeCycles, × multiplicador da faixa amarela), como o simulador de carreira.
        /// Peças que também têm vida em horas já envelhecem por hora: aqui só contam o ciclo.
        /// Antes só as peças citadas nas regras contavam ciclo e nenhuma tinha desgaste natural por pouso.
        /// </summary>
        public PartWearEventResult ApplyLandingCycle(IReadOnlyList<AircraftPart> parts, IReadOnlyList<string> features)
        {
            if (parts == null || parts.Count == 0)
            {
                return new PartWearEventResult(Array.Empty<PartWearInspectionEvent>(), Array.Empty<string>(), 0.0);
            }

            var cyclesBefore = parts.ToDictionary(p => p, p => p.CyclesSinceReplacement, ReferenceEqualityComparer.Instance);
            var ruleResult = ApplyEvent(parts, WearRuleMetrics.LandingCycles, 1.0, features);

            var damaged = new List<string>(ruleResult.DamagedPartIds);
            double totalDamage = ruleResult.TotalDamageApplied;

            foreach (var part in parts)
            {
                if (part.IsBroken) continue;
                var def = _partCatalog.Get(part.PartId);
                if (def == null || !def.LifeCycles.HasValue || def.LifeCycles.Value <= 0) continue;

                if (part.CyclesSinceReplacement == cyclesBefore[part]) part.CyclesSinceReplacement++;

                if (def.LifeHours > 0) continue;
                double wear = (70.0 / def.LifeCycles.Value) * PartHealthBands.LowHealthWearMultiplier(part.Health, _bandSettings);
                if (wear <= 0) continue;
                part.Health = Math.Clamp(part.Health - wear, 0.0, 100.0);
                damaged.Add(part.PartId);
                totalDamage += wear;
            }

            return new PartWearEventResult(ruleResult.Inspections, damaged.Distinct().ToList(), totalDamage);
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
            if (double.IsNaN(limit) || double.IsInfinity(limit) || limit == 0.0 || (limit < 0.0 && rule.Direction == LimitDirection.Above))
            {
                return null;
            }

            if (rule.Direction == LimitDirection.Above)
            {
                // Ordena por threshold crescente (ex.: caution 0.97, warning 1.00, severe 1.08)
                // A última banda cujo threshold foi superado é a faixa mais severa ativa
                return rule.Bands
                    .OrderBy(b => limit * b.Threshold)
                    .LastOrDefault(b => value >= (limit * b.Threshold));
            }
            else
            {
                // Ordena por corte físico decrescente (ex.: caution 20, warning 16, severe 10; ou caution -1.34, warning -1.52, severe -1.75)
                // A última banda cujo corte foi cruzado para baixo é a mais severa
                return rule.Bands
                    .OrderByDescending(b => limit * b.Threshold)
                    .LastOrDefault(b => value <= (limit * b.Threshold));
            }
        }

        internal static int DetectEngineCount(SensorFrame frame)
        {
            int maxEngine = 1;
            foreach (var key in frame.Values.Keys)
            {
                int colonIdx = key.LastIndexOf(':');
                if (colonIdx >= 0 && colonIdx < key.Length - 1)
                {
                    if (int.TryParse(key.AsSpan(colonIdx + 1), out int n) && n > maxEngine)
                    {
                        maxEngine = n;
                    }
                }
            }
            return maxEngine;
        }
    }
}
