using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Orquestrador de voo para o sistema de peças com saúde oculta.
    /// Thread-safe para recebimento de frames assíncronos do SimConnect e delega
    /// o cálculo de desgaste e eventos para o PartFlightTracker (Domain).
    /// </summary>
    public sealed class PartFlightSession
    {
        private readonly PartFlightTracker _tracker;
        private readonly PartCatalog _partCatalog;
        private readonly ComponentCatalog _componentCatalog;
        private readonly IRandomSource _random;
        private PartFailureEngine? _failureEngine;
        private readonly object _frameLock = new();
        private SensorFrame? _lastFrame;

        /// <summary>Excedências de cada tick (lista vazia = nenhuma faixa ativa), para o agrupador do blackbox.</summary>
        public event Action<IReadOnlyList<PartWearExceedance>>? OnExceedanceTick;
        public event Action<PartWearInspectionEvent>? OnInspection;
        public event Action<string>? OnDiagnostic;

        public PartFlightSession(
            PartCatalog parts,
            WearRuleCatalog rules,
            AircraftLimitResolver resolver,
            ComponentCatalog? componentCatalog = null,
            IRandomSource? random = null)
        {
            _partCatalog = parts;
            _componentCatalog = componentCatalog ?? ComponentCatalog.LoadDefault();
            _random = random ?? new SystemRandomSource();
            _tracker = new PartFlightTracker(parts, rules, resolver, _random, _componentCatalog.PartBands);
        }

        public bool IsActive => _tracker.IsActive;
        public bool HasPreflightChanges => _tracker.HasPreflightChanges;
        public double LatestChtCoolingRate => _tracker.LatestChtCoolingRate;
        public IReadOnlyList<AircraftPart> Parts => _tracker.Parts;
        public double HazardMultiplier { get; set; } = 1.0;

        public void Begin(List<AircraftPart> copies, AircraftLimitProfile? profile, IReadOnlyList<string> features)
        {
            lock (_frameLock)
            {
                _lastFrame = null;
            }
            _tracker.Begin(copies, profile, features);

            var engineClass = profile?.Class?.ToLowerInvariant() switch
            {
                "turboprop" => EngineClass.Turboprop,
                "jet" => EngineClass.Jet,
                _ => EngineClass.Piston
            };
            _failureEngine = new PartFailureEngine(_componentCatalog, _partCatalog, engineClass, _random) { HazardMultiplier = HazardMultiplier };
        }

        public void Reset()
        {
            lock (_frameLock)
            {
                _lastFrame = null;
            }
            _tracker.Reset();
            _failureEngine?.Reset();
        }

        public void OnFrame(SensorFrame frame)
        {
            lock (_frameLock)
            {
                _lastFrame = frame;
            }
        }

        public void ProcessTick(double dtSimHours)
        {
            SensorFrame? frame;
            lock (_frameLock)
            {
                frame = _lastFrame;
            }

            var result = _tracker.ProcessTick(frame, dtSimHours, DateTime.UtcNow);

            OnExceedanceTick?.Invoke(result.Exceedances ?? Array.Empty<PartWearExceedance>());

            if (result.Inspections != null)
            {
                foreach (var insp in result.Inspections)
                {
                    OnInspection?.Invoke(insp);
                }
            }

            if (result.DiagnosticMessages != null)
            {
                foreach (var diag in result.DiagnosticMessages)
                {
                    OnDiagnostic?.Invoke(diag);
                }
            }
        }

        public void ObservePreflight(double dtSimHours)
        {
            SensorFrame? frame;
            lock (_frameLock)
            {
                frame = _lastFrame;
            }

            var result = _tracker.ObservePreflight(frame, dtSimHours, DateTime.UtcNow);

            if (result.Inspections != null)
            {
                foreach (var insp in result.Inspections)
                {
                    OnInspection?.Invoke(insp);
                }
            }

            if (result.DiagnosticMessages != null)
            {
                foreach (var diag in result.DiagnosticMessages)
                {
                    OnDiagnostic?.Invoke(diag);
                }
            }
        }

        public void ProcessTouchdown(double fpm, double? pitchDeg, double? bankDeg, double? lateralFps, bool isBounce)
        {
            var result = _tracker.ProcessTouchdown(fpm, pitchDeg, bankDeg, lateralFps, isBounce);
            if (result.Inspections != null)
            {
                foreach (var insp in result.Inspections)
                {
                    OnInspection?.Invoke(insp);
                }
            }
        }

        public int ApplyNativeFailed(int nativeWearId, ComponentKind? fallbackKind) =>
            _tracker.ApplyNativeFailed(nativeWearId, fallbackKind);

        public void DamageGroup(ComponentKind kind, double amount) =>
            _tracker.DamageGroup(kind, amount);

        public void ClampGroup(ComponentKind kind, double max) =>
            _tracker.ClampGroup(kind, max);

        public void ZeroAll() =>
            _tracker.ZeroAll();

        public void SyncComponents(PilotAircraft aircraft, IEnumerable<AircraftComponent> components) =>
            _tracker.SyncComponents(aircraft, components);

        public Task SaveAsync(AppDbContext context) =>
            PartStateWriter.CopyToDbAsync(context, _tracker.Parts);

        public IReadOnlyList<PartFailure> EvaluateFailures(
            double dtSimHours,
            Func<ComponentKind, bool> isInUse,
            IReadOnlyCollection<ComponentKind> alreadyFailed)
        {
            if (_failureEngine == null || !_tracker.IsActive)
            {
                return Array.Empty<PartFailure>();
            }

            return _failureEngine.Evaluate(_tracker.Parts, dtSimHours, isInUse, alreadyFailed);
        }

        public void BreakPart(Guid partId)
        {
            var part = _tracker.Parts.FirstOrDefault(p => p.Id == partId);
            if (part != null && !part.IsBroken)
            {
                part.Health = 0.0;
                part.IsBroken = true;
                part.BrokenAtUtc = DateTime.UtcNow;
            }
        }
    }
}
