using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.Domain.Replay
{
    /// <summary>
    /// Representa um toque com o solo a ser injetado durante o replay.
    /// </summary>
    public sealed record ReplayTouchdown(
        double Fpm,
        double? PitchDeg = null,
        double? BankDeg = null,
        double? LateralFps = null,
        bool IsBounce = false,
        double? ElapsedSeconds = null,
        DateTime? Utc = null);

    /// <summary>
    /// Modo de ativação do desgaste e tempo no replay.
    /// </summary>
    public enum ReplayActivationMode
    {
        /// <summary>
        /// Imita o início do FlightMonitor no jogo: só começa a desgastar e contar horas
        /// após 10 segundos contínuos em tempo real com motor em combustão e freio de estacionamento solto (PB &lt; 0.5).
        /// </summary>
        GameLike,

        /// <summary>
        /// Desgasta e acumula tempo desde a primeira linha do CSV (comportamento inicial).
        /// </summary>
        FromStart
    }

    /// <summary>
    /// Resumo do desgaste e ciclo de uma peça ao término do replay.
    /// </summary>
    public sealed record ReplayPartItem(
        string PartId,
        int EngineIndex,
        double InitialHealth,
        double FinalHealth,
        double HoursSinceReplacement,
        int CyclesSinceReplacement,
        bool IsBroken)
    {
        public double HealthLoss => Math.Max(0.0, InitialHealth - FinalHealth);
    }

    /// <summary>
    /// Evento operacional derivado com carimbo de data/hora no voo.
    /// </summary>
    public sealed record ReplayDerivedEventItem(
        DateTime Utc,
        double ElapsedSeconds,
        string MetricKey,
        double Value,
        int? EngineIndex);

    /// <summary>
    /// Resultado consolidado da execução de um replay de voo.
    /// </summary>
    public sealed record FlightReplayResult(
        IReadOnlyList<ReplayPartItem> Parts,
        IReadOnlyList<PartWearExceedance> Exceedances,
        IReadOnlyList<PartWearInspectionEvent> Inspections,
        IReadOnlyList<ReplayDerivedEventItem> DerivedEvents,
        IReadOnlyList<string> Diagnostics,
        double TotalSimHours,
        int TotalTicks,
        TimeSpan ElapsedWallTime);

    /// <summary>
    /// Motor determinístico para replay de voos a partir de streams de telemetria/sensores.
    /// Lógica pura de Domain, sem I/O, sem EF e sem SimConnect.
    /// </summary>
    public sealed class FlightReplayer
    {
        private readonly PartCatalog _partsCatalog;
        private readonly WearRuleCatalog _wearRuleCatalog;
        private readonly AircraftLimitResolver _resolver;

        /// <summary>
        /// Ponto de teste para testes de mutação / fuzz: desativa a resolução de sensores com sufixo :n.
        /// </summary>
        public bool DisablePerEngineSensorResolution { get; set; }

        public FlightReplayer(
            PartCatalog partsCatalog,
            WearRuleCatalog wearRuleCatalog,
            AircraftLimitResolver resolver)
        {
            _partsCatalog = partsCatalog ?? throw new ArgumentNullException(nameof(partsCatalog));
            _wearRuleCatalog = wearRuleCatalog ?? throw new ArgumentNullException(nameof(wearRuleCatalog));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Executa o replay do voo tick a tick.
        /// </summary>
        public FlightReplayResult Run(
            IReadOnlyList<SensorFrame> frames,
            IReadOnlyList<ReplayTouchdown>? touchdowns = null,
            AircraftLimitProfile? profile = null,
            IReadOnlyList<string>? features = null,
            int? engineCount = null,
            IReadOnlyList<AircraftPart>? initialParts = null,
            ReplayActivationMode activationMode = ReplayActivationMode.GameLike)
        {
            var effectiveFeatures = features?.ToList() ?? profile?.Features?.ToList() ?? new List<string>();
            int effectiveEngineCount = engineCount ?? profile?.EngineCount ?? 1;
            if (effectiveEngineCount <= 0) effectiveEngineCount = 1;

            List<AircraftPart> parts;
            if (initialParts != null)
            {
                parts = initialParts.Select(p => new AircraftPart
                {
                    Id = p.Id,
                    PilotAircraftId = p.PilotAircraftId,
                    PartId = p.PartId,
                    EngineIndex = p.EngineIndex,
                    Health = p.Health,
                    HoursSinceReplacement = p.HoursSinceReplacement,
                    CyclesSinceReplacement = p.CyclesSinceReplacement,
                    LastReplacedAtUtc = p.LastReplacedAtUtc,
                    LastReplacedAirframeHours = p.LastReplacedAirframeHours,
                    Flag = p.Flag,
                    FlaggedBy = p.FlaggedBy,
                    FlaggedAtUtc = p.FlaggedAtUtc,
                    IsBroken = p.IsBroken,
                    BrokenAtUtc = p.BrokenAtUtc
                }).ToList();
            }
            else
            {
                parts = new List<AircraftPart>();
                var applicable = _partsCatalog.PartsFor(effectiveFeatures);
                foreach (var def in applicable)
                {
                    if (def.PerEngine)
                    {
                        for (int eng = 1; eng <= effectiveEngineCount; eng++)
                        {
                            parts.Add(new AircraftPart
                            {
                                Id = Guid.NewGuid(),
                                PartId = def.Id,
                                EngineIndex = eng,
                                Health = 100.0,
                                HoursSinceReplacement = 0.0,
                                CyclesSinceReplacement = 0,
                                IsBroken = false
                            });
                        }
                    }
                    else
                    {
                        parts.Add(new AircraftPart
                        {
                            Id = Guid.NewGuid(),
                            PartId = def.Id,
                            EngineIndex = 0,
                            Health = 100.0,
                            HoursSinceReplacement = 0.0,
                            CyclesSinceReplacement = 0,
                            IsBroken = false
                        });
                    }
                }
            }

            var initialHealthMap = parts.ToDictionary(p => (p.PartId, p.EngineIndex), p => p.Health);

            var tracker = new PartFlightTracker(_partsCatalog, _wearRuleCatalog, _resolver)
            {
                DisablePerEngineSensorResolution = DisablePerEngineSensorResolution
            };
            tracker.Begin(parts, profile, effectiveFeatures);

            if (frames == null || frames.Count == 0)
            {
                var emptyParts = parts.Select(p => new ReplayPartItem(
                    p.PartId,
                    p.EngineIndex,
                    initialHealthMap.GetValueOrDefault((p.PartId, p.EngineIndex), 100.0),
                    p.Health,
                    p.HoursSinceReplacement,
                    p.CyclesSinceReplacement,
                    p.IsBroken
                )).ToList();

                return new FlightReplayResult(
                    emptyParts,
                    Array.Empty<PartWearExceedance>(),
                    Array.Empty<PartWearInspectionEvent>(),
                    Array.Empty<ReplayDerivedEventItem>(),
                    Array.Empty<string>(),
                    0.0,
                    0,
                    TimeSpan.Zero);
            }

            DateTime startUtc = frames[0].ReceivedUtc;
            DateTime prevUtc = frames[0].ReceivedUtc;
            double totalSimHours = 0.0;

            var exceedances = new List<PartWearExceedance>();
            var inspections = new List<PartWearInspectionEvent>();
            var derivedEvents = new List<ReplayDerivedEventItem>();
            var diagnostics = new List<string>();
            var diagnosticSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var pendingTouchdowns = (touchdowns ?? Array.Empty<ReplayTouchdown>())
                .OrderBy(t => t.ElapsedSeconds ?? (t.Utc.HasValue ? (t.Utc.Value - startUtc).TotalSeconds : double.MaxValue))
                .ToList();
            int nextTouchdownIndex = 0;

            bool isFlightActive = activationMode == ReplayActivationMode.FromStart;
            double accumulatedActivationRealSec = 0.0;

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                double elapsedSeconds = (frame.ReceivedUtc - startUtc).TotalSeconds;

                double wallSec = i > 0 ? (frame.ReceivedUtc - prevUtc).TotalSeconds : 0.0;
                double dtSimHours = i > 0 ? SimTimeStep.ToSimHours(wallSec, frame.SimulationRate) : 0.0;
                prevUtc = frame.ReceivedUtc;

                if (!isFlightActive)
                {
                    bool isCombustion = false;
                    for (int eng = 1; eng <= effectiveEngineCount; eng++)
                    {
                        if (frame.Values.TryGetValue($"eng-combustion:{eng}", out double cVal) ||
                            (eng == 1 && frame.Values.TryGetValue("eng-combustion", out cVal)))
                        {
                            if (cVal > 0.5)
                            {
                                isCombustion = true;
                                break;
                            }
                        }
                    }

                    double parkingBrake = 1.0;
                    if (frame.Values.TryGetValue("brake-parking-position", out double pb) ||
                        frame.Values.TryGetValue("brake-parking", out pb))
                    {
                        parkingBrake = pb;
                    }

                    if (isCombustion && parkingBrake < 0.5)
                    {
                        accumulatedActivationRealSec += wallSec;
                        if (accumulatedActivationRealSec >= 10.0)
                        {
                            isFlightActive = true;
                        }
                    }
                    else
                    {
                        accumulatedActivationRealSec = 0.0;
                    }

                    if (!isFlightActive)
                    {
                        if (dtSimHours > 0)
                        {
                            var preflightResult = tracker.ObservePreflight(frame, dtSimHours, frame.ReceivedUtc);
                            if (preflightResult.Inspections.Count > 0)
                            {
                                inspections.AddRange(preflightResult.Inspections);
                            }

                            if (preflightResult.DiagnosticMessages.Count > 0)
                            {
                                foreach (var diag in preflightResult.DiagnosticMessages)
                                {
                                    if (diagnosticSet.Add(diag))
                                    {
                                        diagnostics.Add(diag);
                                    }
                                }
                            }

                            if (tracker.LastDerivedEvents.Count > 0)
                            {
                                foreach (var ev in tracker.LastDerivedEvents)
                                {
                                    derivedEvents.Add(new ReplayDerivedEventItem(
                                        frame.ReceivedUtc,
                                        elapsedSeconds,
                                        ev.MetricKey,
                                        ev.Value,
                                        ev.EngineIndex));
                                }
                            }
                        }

                        continue;
                    }
                }

                totalSimHours += dtSimHours;

                // Processa toques agendados para este instante ou anterior
                while (nextTouchdownIndex < pendingTouchdowns.Count)
                {
                    var td = pendingTouchdowns[nextTouchdownIndex];
                    double tdSec = td.ElapsedSeconds ?? (td.Utc.HasValue ? (td.Utc.Value - startUtc).TotalSeconds : double.MaxValue);

                    if (tdSec <= elapsedSeconds || double.IsNaN(tdSec))
                    {
                        var tdRes = tracker.ProcessTouchdown(td.Fpm, td.PitchDeg, td.BankDeg, td.LateralFps, td.IsBounce);
                        if (tdRes.Inspections.Count > 0)
                        {
                            inspections.AddRange(tdRes.Inspections);
                        }
                        nextTouchdownIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                var tickResult = tracker.ProcessTick(frame, dtSimHours, frame.ReceivedUtc);

                if (tickResult.Exceedances.Count > 0)
                {
                    exceedances.AddRange(tickResult.Exceedances);
                }

                if (tickResult.Inspections.Count > 0)
                {
                    inspections.AddRange(tickResult.Inspections);
                }

                if (tickResult.DiagnosticMessages.Count > 0)
                {
                    foreach (var diag in tickResult.DiagnosticMessages)
                    {
                        if (diagnosticSet.Add(diag))
                        {
                            diagnostics.Add(diag);
                        }
                    }
                }

                if (tracker.LastDerivedEvents.Count > 0)
                {
                    foreach (var ev in tracker.LastDerivedEvents)
                    {
                        derivedEvents.Add(new ReplayDerivedEventItem(
                            frame.ReceivedUtc,
                            elapsedSeconds,
                            ev.MetricKey,
                            ev.Value,
                            ev.EngineIndex));
                    }
                }
            }

            // Se houver toques residuais não disparados (ex.: sem tempo definido), dispara agora
            while (nextTouchdownIndex < pendingTouchdowns.Count)
            {
                var td = pendingTouchdowns[nextTouchdownIndex++];
                var tdRes = tracker.ProcessTouchdown(td.Fpm, td.PitchDeg, td.BankDeg, td.LateralFps, td.IsBounce);
                if (tdRes.Inspections.Count > 0)
                {
                    inspections.AddRange(tdRes.Inspections);
                }
            }

            TimeSpan elapsedWall = frames[^1].ReceivedUtc - startUtc;

            var finalPartItems = parts.Select(p => new ReplayPartItem(
                p.PartId,
                p.EngineIndex,
                initialHealthMap.GetValueOrDefault((p.PartId, p.EngineIndex), 100.0),
                p.Health,
                p.HoursSinceReplacement,
                p.CyclesSinceReplacement,
                p.IsBroken
            )).ToList();

            return new FlightReplayResult(
                finalPartItems,
                exceedances,
                inspections,
                derivedEvents,
                diagnostics,
                totalSimHours,
                frames.Count,
                elapsedWall);
        }
    }
}
