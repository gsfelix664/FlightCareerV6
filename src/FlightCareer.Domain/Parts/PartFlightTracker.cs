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
    /// Rastreia e calcula o estado de desgaste, estresse e panes das peças durante o voo.
    /// Lógica pura de Domain (testável sem UI, sem SimConnect e sem EF).
    /// </summary>
    public sealed class PartFlightTracker
    {
        public static readonly IReadOnlySet<string> DerivedSensorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eng-cht-cooling-rate"
        };

        private readonly PartCatalog _partsCatalog;
        private readonly WearRuleCatalog _wearRuleCatalog;
        private readonly AircraftLimitResolver _resolver;
        private readonly PartWearEngine _wearEngine;
        private readonly PartEventDeriver _eventDeriver = new();
        private readonly PartComponentMapping _mapping = PartComponentMapping.CreateDefault();

        private readonly List<AircraftPart> _parts = new();
        private readonly HashSet<string> _reportedDiagnostics = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, List<(double SimTime, double Cht)>> _chtHistoryByEngine = new();
        private double _accumulatedSimSeconds;
        private AircraftLimitProfile? _profile;
        private List<string> _features = new();
        private bool _pausedReported;

        public PartFlightTracker(
            PartCatalog partsCatalog,
            WearRuleCatalog wearRuleCatalog,
            AircraftLimitResolver resolver)
        {
            _partsCatalog = partsCatalog ?? throw new ArgumentNullException(nameof(partsCatalog));
            _wearRuleCatalog = wearRuleCatalog ?? throw new ArgumentNullException(nameof(wearRuleCatalog));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _wearEngine = new PartWearEngine(_partsCatalog, _wearRuleCatalog);
        }

        public bool IsActive => _parts.Count > 0;
        public bool HasPreflightChanges { get; private set; }
        public IReadOnlyList<AircraftPart> Parts => _parts;

        /// <summary>
        /// Ponto de teste para testes de mutação / fuzz: desativa a resolução de sensores com sufixo :n no motor.
        /// </summary>
        public bool DisablePerEngineSensorResolution
        {
            get => _wearEngine.DisablePerEngineSensorResolution;
            set => _wearEngine.DisablePerEngineSensorResolution = value;
        }
        public IReadOnlyList<DerivedPartEvent> LastDerivedEvents { get; private set; } = Array.Empty<DerivedPartEvent>();

        public void Begin(IEnumerable<AircraftPart> copies, AircraftLimitProfile? profile, IReadOnlyList<string>? features)
        {
            _parts.Clear();
            if (copies != null) _parts.AddRange(copies);

            _profile = profile;
            _features = features?.ToList() ?? new List<string>();
            _wearEngine.ResetExceedanceTracking();
            _eventDeriver.Reset();
            _eventDeriver.ConfigureElectrical(BuildElectricalConfig(profile));
            _pausedReported = false;
            _reportedDiagnostics.Clear();
            _accumulatedSimSeconds = 0.0;
            _chtHistoryByEngine.Clear();
            HasPreflightChanges = false;
            LastDerivedEvents = Array.Empty<DerivedPartEvent>();
        }

        private ElectricalConfig BuildElectricalConfig(AircraftLimitProfile? profile)
        {
            if (profile == null) return new ElectricalConfig();

            string cls = profile.Class ?? string.Empty;
            var voltsLimit = _resolver.Get(profile, cls, LimitKeys.ElecSystemVolts);
            var avionicsCircuitLimit = _resolver.Get(profile, cls, LimitKeys.ElecAvionicsCircuitIndex);
            var avionicsBusAmpsLimit = _resolver.Get(profile, cls, LimitKeys.ElecAvionicsBusAmpsIndex);
            var mainBusLimit = _resolver.Get(profile, cls, LimitKeys.ElecMainBusIndex);
            var ratedAmpsLimit = _resolver.Get(profile, cls, LimitKeys.AlternatorRatedAmps);
            var deepDischargeLimit = _resolver.Get(profile, cls, LimitKeys.ElecDeepDischargeVolts);

            var deepDischargeRule = _wearRuleCatalog.Rules.FirstOrDefault(r => r.Metric == WearRuleMetrics.BatteryDeepDischargeMinutes);
            var overloadRule = _wearRuleCatalog.Rules.FirstOrDefault(r => r.Metric == WearRuleMetrics.ElectricalOverloadMinutes);

            return new ElectricalConfig
            {
                SystemVolts = voltsLimit?.Value,
                AvionicsCircuitIndex = avionicsCircuitLimit != null ? (int)Math.Round(avionicsCircuitLimit.Value) : null,
                AvionicsBusAmpsIndex = avionicsBusAmpsLimit != null ? (int)Math.Round(avionicsBusAmpsLimit.Value) : null,
                MainBusIndex = mainBusLimit != null ? (int)Math.Round(mainBusLimit.Value) : null,
                AlternatorRatedAmps = ratedAmpsLimit?.Value,
                DeepDischargeVolts = deepDischargeLimit?.Value ?? (voltsLimit != null ? voltsLimit.Value * (22.0 / 28.0) : null),
                BatteryDeepDischargeThresholds = deepDischargeRule?.Bands.Select(b => b.Threshold).OrderBy(t => t).ToList() ?? new List<double> { 10.0, 25.0, 45.0 },
                ElectricalOverloadThresholds = overloadRule?.Bands.Select(b => b.Threshold).OrderBy(t => t).ToList() ?? new List<double> { 5.0, 15.0, 30.0 }
            };
        }

        public void Reset()
        {
            _parts.Clear();
            _profile = null;
            _features.Clear();
            _wearEngine.ResetExceedanceTracking();
            _eventDeriver.Reset();
            _pausedReported = false;
            _reportedDiagnostics.Clear();
            _accumulatedSimSeconds = 0.0;
            _chtHistoryByEngine.Clear();
            HasPreflightChanges = false;
            LastDerivedEvents = Array.Empty<DerivedPartEvent>();
        }

        public PartWearTickResult ProcessTick(SensorFrame? frame, double dtSimHours, DateTime nowUtc)
        {
            LastDerivedEvents = Array.Empty<DerivedPartEvent>();
            if (!IsActive || dtSimHours <= 0)
            {
                return new PartWearTickResult(
                    Array.Empty<PartWearExceedance>(),
                    Array.Empty<PartWearInspectionEvent>(),
                    Array.Empty<string>());
            }

            // Decisão 6: Frame velho (>5s) não desgasta; notifica uma vez por voo
            if (frame == null || (nowUtc - frame.ReceivedUtc).TotalSeconds > 5.0)
            {
                if (!_pausedReported)
                {
                    _pausedReported = true;
                    const string msg = "sensores sem dados; desgaste por peça pausado";
                    Debug.WriteLine($"[PartFlightTracker] {msg}");
                    return new PartWearTickResult(
                        Array.Empty<PartWearExceedance>(),
                        Array.Empty<PartWearInspectionEvent>(),
                        new[] { msg });
                }

                return new PartWearTickResult(
                    Array.Empty<PartWearExceedance>(),
                    Array.Empty<PartWearInspectionEvent>(),
                    Array.Empty<string>());
            }

            _pausedReported = false;

            double dtSimSeconds = dtSimHours * 3600.0;
            _accumulatedSimSeconds += dtSimSeconds;

            var effectiveFrame = InjectDerivedMetrics(frame, dtSimSeconds);

            // 1. Desgaste contínuo e faixas de estresse
            var tickResult = _wearEngine.ApplyTick(_parts, effectiveFrame, dtSimHours, _features, _resolver, _profile);

            var exceedances = new List<PartWearExceedance>(tickResult.Exceedances);
            var inspections = new List<PartWearInspectionEvent>(tickResult.Inspections);
            var diagnostics = new List<string>();

            foreach (var diag in tickResult.DiagnosticMessages)
            {
                if (_reportedDiagnostics.Add(diag))
                {
                    diagnostics.Add(diag);
                }
            }

            // 2. Eventos operacionais derivados
            var derivedEvents = _eventDeriver.Update(effectiveFrame, dtSimSeconds, _features);
            LastDerivedEvents = derivedEvents;

            foreach (var ev in derivedEvents)
            {
                IReadOnlyList<AircraftPart> targetParts = _parts;
                if (ev.EngineIndex.HasValue)
                {
                    targetParts = _parts.Where(p => p.EngineIndex == 0 || p.EngineIndex == ev.EngineIndex.Value).ToList();
                }

                var evResult = _wearEngine.ApplyEvent(targetParts, ev.MetricKey, ev.Value, _features);
                if (evResult.Inspections.Count > 0)
                {
                    inspections.AddRange(evResult.Inspections);
                }
            }

            // Drena: cada diagnóstico sai uma vez por voo (antes era copiado a cada tick para o blackbox)
            foreach (var diag in _eventDeriver.DrainDiagnostics())
            {
                if (_reportedDiagnostics.Add(diag))
                {
                    diagnostics.Add(diag);
                }
            }

            return new PartWearTickResult(exceedances, inspections, diagnostics);
        }

        public PartWearTickResult ObservePreflight(SensorFrame? frame, double dtSimHours, DateTime nowUtc)
        {
            LastDerivedEvents = Array.Empty<DerivedPartEvent>();
            if (!IsActive || dtSimHours <= 0)
            {
                return new PartWearTickResult(
                    Array.Empty<PartWearExceedance>(),
                    Array.Empty<PartWearInspectionEvent>(),
                    Array.Empty<string>());
            }

            // Tratamento de frame velho (>5s)
            if (frame == null || (nowUtc - frame.ReceivedUtc).TotalSeconds > 5.0)
            {
                if (!_pausedReported)
                {
                    _pausedReported = true;
                    const string msg = "sensores sem dados; desgaste por peça pausado";
                    Debug.WriteLine($"[PartFlightTracker] {msg}");
                    return new PartWearTickResult(
                        Array.Empty<PartWearExceedance>(),
                        Array.Empty<PartWearInspectionEvent>(),
                        new[] { msg });
                }

                return new PartWearTickResult(
                    Array.Empty<PartWearExceedance>(),
                    Array.Empty<PartWearInspectionEvent>(),
                    Array.Empty<string>());
            }

            _pausedReported = false;

            double dtSimSeconds = dtSimHours * 3600.0;

            // Roda exclusivamente os eventos operacionais derivados (sem ApplyTick, sem horas, sem regras contínuas)
            var derivedEvents = _eventDeriver.Update(frame, dtSimSeconds, _features);
            LastDerivedEvents = derivedEvents;

            var inspections = new List<PartWearInspectionEvent>();
            var diagnostics = new List<string>();

            double healthBefore = _parts.Sum(p => p.Health);
            int cyclesBefore = _parts.Sum(p => p.CyclesSinceReplacement);

            foreach (var ev in derivedEvents)
            {
                IReadOnlyList<AircraftPart> targetParts = _parts;
                if (ev.EngineIndex.HasValue)
                {
                    targetParts = _parts.Where(p => p.EngineIndex == 0 || p.EngineIndex == ev.EngineIndex.Value).ToList();
                }

                var evResult = _wearEngine.ApplyEvent(targetParts, ev.MetricKey, ev.Value, _features);
                if (evResult.Inspections.Count > 0)
                {
                    inspections.AddRange(evResult.Inspections);
                }
            }

            double healthAfter = _parts.Sum(p => p.Health);
            int cyclesAfter = _parts.Sum(p => p.CyclesSinceReplacement);

            if (healthAfter < healthBefore - 1e-6 || cyclesAfter > cyclesBefore || inspections.Count > 0)
            {
                HasPreflightChanges = true;
            }

            // Drena diagnósticos únicos
            foreach (var diag in _eventDeriver.DrainDiagnostics())
            {
                if (_reportedDiagnostics.Add(diag))
                {
                    diagnostics.Add(diag);
                }
            }

            return new PartWearTickResult(
                Array.Empty<PartWearExceedance>(),
                inspections,
                diagnostics);
        }

        public PartWearEventResult ProcessTouchdown(
            double fpm,
            double? pitchDeg,
            double? bankDeg,
            double? lateralFps,
            bool isBounce)
        {
            if (!IsActive)
            {
                return new PartWearEventResult(Array.Empty<PartWearInspectionEvent>(), Array.Empty<string>(), 0.0);
            }

            var inspections = new List<PartWearInspectionEvent>();
            var damaged = new List<string>();
            double totalDamage = 0.0;

            void Aggregate(PartWearEventResult res)
            {
                if (res.Inspections.Count > 0) inspections.AddRange(res.Inspections);
                if (res.DamagedPartIds.Count > 0) damaged.AddRange(res.DamagedPartIds);
                totalDamage += res.TotalDamageApplied;
            }

            // Cada impacto conta para touchdownFpm (inclusive bounce)
            Aggregate(_wearEngine.ApplyEvent(_parts, WearRuleMetrics.TouchdownFpm, Math.Abs(fpm), _features));

            if (pitchDeg.HasValue)
            {
                Aggregate(_wearEngine.ApplyEvent(_parts, WearRuleMetrics.TouchdownPitchDegrees, Math.Abs(pitchDeg.Value), _features));
            }

            if (bankDeg.HasValue)
            {
                Aggregate(_wearEngine.ApplyEvent(_parts, WearRuleMetrics.TouchdownBankDegrees, Math.Abs(bankDeg.Value), _features));
            }

            if (lateralFps.HasValue)
            {
                Aggregate(_wearEngine.ApplyEvent(_parts, WearRuleMetrics.TouchdownLateralFps, Math.Abs(lateralFps.Value), _features));
            }

            // landingCycles conta somente se não for bounce
            if (!isBounce)
            {
                Aggregate(_wearEngine.ApplyEvent(_parts, WearRuleMetrics.LandingCycles, 1.0, _features));
            }

            return new PartWearEventResult(inspections, damaged.Distinct().ToList(), totalDamage);
        }

        public int ApplyNativeFailed(int nativeWearId, ComponentKind? fallbackKind)
        {
            if (!IsActive) return 0;

            // Decisão 3: união das parts de regras com nativeOverstress == id
            var targetPartIds = _wearRuleCatalog.Rules
                .Where(r => r.NativeOverstress != null && r.NativeOverstress.ComponentId == nativeWearId)
                .SelectMany(r => r.Bands)
                .SelectMany(b => b.Parts)
                .Select(p => p.PartId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var installedTargets = _parts
                .Where(p => targetPartIds.Contains(p.PartId))
                .ToList();

            if (installedTargets.Count > 0)
            {
                int brokenCount = 0;
                foreach (var part in installedTargets)
                {
                    if (_wearEngine.ApplyNativeFailed(part)) brokenCount++;
                }
                return brokenCount;
            }

            // Fallback: menor saúde do grupo mapeado por fallbackKind
            if (fallbackKind.HasValue)
            {
                var groupParts = GetPartsByComponentKind(fallbackKind.Value).ToList();
                if (groupParts.Count > 0)
                {
                    var lowest = groupParts
                        .OrderBy(p => p.Health)
                        .ThenBy(p => p.PartId, StringComparer.Ordinal)
                        .First();

                    return _wearEngine.ApplyNativeFailed(lowest) ? 1 : 0;
                }
            }

            Debug.WriteLine($"[PartFlightTracker] Pane nativa ID {nativeWearId} sem peça-alvo instalada.");
            return 0;
        }

        public void DamageGroup(ComponentKind kind, double amount)
        {
            if (!IsActive || amount <= 0) return;

            foreach (var part in GetPartsByComponentKind(kind))
            {
                if (part.IsBroken) continue;
                part.Health = Math.Clamp(part.Health - amount, 0.0, 100.0);
            }
        }

        public void ClampGroup(ComponentKind kind, double max)
        {
            if (!IsActive) return;

            foreach (var part in GetPartsByComponentKind(kind))
            {
                part.Health = Math.Min(part.Health, max);
            }
        }

        public void ZeroAll()
        {
            foreach (var part in _parts)
            {
                part.Health = 0.0;
            }
        }

        public void SyncComponents(PilotAircraft aircraft, IEnumerable<AircraftComponent> components)
        {
            if (!IsActive) return;
            PartComponentBridge.SyncComponentsFromParts(aircraft, components, _parts, _partsCatalog, _mapping);
        }

        private IEnumerable<AircraftPart> GetPartsByComponentKind(ComponentKind kind)
        {
            foreach (var part in _parts)
            {
                var def = _partsCatalog.Get(part.PartId);
                if (def == null) continue;

                var mappedKind = _mapping.ResolveComponentKind(def.Group);
                if (mappedKind == kind)
                {
                    yield return part;
                }
            }
        }

        private SensorFrame InjectDerivedMetrics(SensorFrame frame, double dtSimSeconds)
        {
            var dict = new Dictionary<string, double>(frame.Values, StringComparer.OrdinalIgnoreCase);

            for (int eng = 1; eng <= 4; eng++)
            {
                bool isComb = false;
                if (frame.Values.TryGetValue($"eng-combustion:{eng}", out double cVal) ||
                    (eng == 1 && frame.Values.TryGetValue("eng-combustion", out cVal)))
                {
                    isComb = cVal > 0.5;
                }

                double coolingRate = 0.0;

                if (isComb)
                {
                    bool hasCht = frame.Values.TryGetValue($"eng-cht:{eng}", out double cht) ||
                                  (eng == 1 && frame.Values.TryGetValue("eng-cht", out cht));

                    if (hasCht && !double.IsNaN(cht) && !double.IsInfinity(cht))
                    {
                        if (!_chtHistoryByEngine.TryGetValue(eng, out var history))
                        {
                            history = new List<(double SimTime, double Cht)>();
                            _chtHistoryByEngine[eng] = history;
                        }

                        history.Add((_accumulatedSimSeconds, cht));

                        // Mantém a janela móvel de 60 segundos simulados
                        while (history.Count >= 2 && history[1].SimTime <= _accumulatedSimSeconds - 60.0)
                        {
                            history.RemoveAt(0);
                        }

                        var oldest = history[0];
                        double deltaSec = _accumulatedSimSeconds - oldest.SimTime;
                        if (deltaSec >= 1.0)
                        {
                            // Taxa de resfriamento em °C/min (positivo = esfriando)
                            coolingRate = ((oldest.Cht - cht) * 60.0) / deltaSec;
                        }
                    }
                    else
                    {
                        _chtHistoryByEngine.Remove(eng);
                    }
                }
                else
                {
                    _chtHistoryByEngine.Remove(eng);
                }

                dict[$"eng-cht-cooling-rate:{eng}"] = coolingRate;
                if (eng == 1)
                {
                    dict["eng-cht-cooling-rate"] = coolingRate;
                }
            }

            return new SensorFrame(dict, frame.ReceivedUtc, frame.SimulationRate);
        }
    }
}
