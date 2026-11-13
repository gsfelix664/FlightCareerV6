using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Desgaste e panes de um voo usando o sistema por componentes (substitui o antigo WearAndFailureService).
    ///
    /// A cada tick: crash → desgaste (WearEngine) → panes por desgaste (FailureEngine) → eventos externos
    /// → gelo no carburador → efeitos contínuos no simulador (SimFailureActuator).
    /// Os componentes são a fonte da verdade; o FlightMonitor copia a saúde para as colunas antigas
    /// (LegacyConditionBridge) para o Hangar/HUD/FlightService continuarem funcionando.
    /// </summary>
    public sealed class ComponentFlightSession
    {
        private readonly SimConnectService _sim;
        private readonly ComponentCatalog _catalog;
        private readonly IRandomSource _random;
        private readonly SimFailureActuator _actuator;
        private readonly List<FlightFailureRecord> _history = new();

        private List<AircraftComponent> _components = new();
        private WearEngine? _wear;
        private FailureEngine? _failures;
        private ExternalEventEngine? _external;
        private CarbIcingMonitor _carbIce;
        private bool _crashApplied;
        private bool _oilLeakFromLandingChecked;

        public event Action<FlightFailureRecord>? OnFailure;

        public ComponentFlightSession(SimConnectService sim, ComponentCatalog catalog, IRandomSource? random = null)
        {
            _sim = sim;
            _catalog = catalog;
            _random = random ?? new SystemRandomSource();
            _actuator = new SimFailureActuator(sim, _random);
            _actuator.OnRecord += Emit;
            _carbIce = new CarbIcingMonitor(catalog.CarbIcing);
        }

        public ComponentCatalog Catalog => _catalog;
        public EngineClass EngineClass { get; private set; } = EngineClass.Piston;
        public IReadOnlyList<AircraftComponent> Components => _components;
        public IReadOnlyList<FlightFailureRecord> History => _history;
        public bool HasFailures => _history.Any(r => r.Source != "Excedência");
        public string LatestFailureSummary => _history.LastOrDefault(r => r.Source != "Excedência")?.Title ?? string.Empty;
        public bool IsEngineOnFire => _actuator.IsEngineOnFire;

        /// <summary>Prepara a sessão para uma aeronave (componentes já carregados e reconciliados).</summary>
        public void Begin(List<AircraftComponent> components, AircraftType? type)
        {
            _components = components;
            EngineClass = _catalog.ResolveEngineClass(type);
            (_engineLimits, _airframeLimits) = _catalog.ResolveLimits(type?.Model, EngineClass);
            CreateEngines();
        }

        private EngineLimits? _engineLimits;
        private AirframeLimits? _airframeLimits;

        private void CreateEngines()
        {
            _wear = new WearEngine(_catalog, EngineClass, _engineLimits ?? _catalog.LimitsFor(EngineClass), _airframeLimits ?? _catalog.AirframeLimits);
            _failures = new FailureEngine(_catalog, EngineClass, _random);
            _external = new ExternalEventEngine(_catalog.ExternalEvents, EngineClass, _random);
            _carbIce = new CarbIcingMonitor(_catalog.CarbIcing);
        }

        /// <summary>Zera o estado do voo. Com clearSimState, desfaz no MSFS as panes aplicadas.</summary>
        public void Reset(bool clearSimState = false)
        {
            _history.Clear();
            _actuator.Reset(clearSimState);
            _crashApplied = false;
            _oilLeakFromLandingChecked = false;
            // Recria os motores de desgaste/pane para zerar o estado interno (CHT anterior, starter, panes do voo)
            if (_wear != null) CreateEngines();
        }

        public AircraftComponent? Get(ComponentKind kind) => _components.FirstOrDefault(c => c.Kind == kind);

        public void ClampHealth(ComponentKind kind, double max)
        {
            var c = Get(kind);
            if (c != null) c.Health = Math.Min(c.Health, max);
        }

        public void ZeroAll()
        {
            foreach (var c in _components) c.Health = 0.0;
        }

        public void ProcessTick(SimTelemetry data, double dtHours)
        {
            if (_wear == null || _failures == null || _external == null || _components.Count == 0) return;
            dtHours = Math.Clamp(dtHours, 0.0, 0.005); // Proteção contra saltos (pausa, travamento)
            if (dtHours <= 0) return;

            // 1. Crash real reportado pelo simulador
            bool isRealCrash = _sim.IsCrashed && (!data.IsOnGround || data.GroundSpeed > 45.0 || Math.Abs(data.GForce) > 4.5 || Math.Abs(data.VerticalSpeed) > 2500.0);
            if (isRealCrash)
            {
                if (!_crashApplied)
                {
                    _crashApplied = true;
                    ZeroAll();
                    Emit(new FlightFailureRecord
                    {
                        Title = "ACIDENTE: AERONAVE DESTRUÍDA",
                        Description = "O simulador reportou colisão com impacto real. Todos os componentes foram perdidos.",
                        IsCritical = true,
                        Source = "Operacional"
                    });
                }
                return;
            }

            // 2. Desgaste
            foreach (var ex in _wear.ApplyTick(_components, data, dtHours))
            {
                string limit = ex.Limit != 0 ? $" (limite {ex.Limit:F0})" : string.Empty;
                Emit(new FlightFailureRecord
                {
                    Title = $"EXCEDÊNCIA: {ex.Parameter.ToUpperInvariant()}",
                    Description = $"{ex.Parameter}: {ex.Value:F0}{limit}. Desgaste acelerado em {ComponentName(ex.Kind)} enquanto durar.",
                    IsCritical = false,
                    Kind = ex.Kind,
                    Source = "Excedência"
                });
            }

            // 3. Panes por desgaste (abaixo de 20%)
            foreach (var failure in _failures.Evaluate(_components, data, dtHours))
            {
                ApplyMode(failure.Kind, failure.Mode, data);
            }

            // 4. Eventos externos (independem da saúde)
            foreach (var ev in _external.Evaluate(_components, data, dtHours))
            {
                var o = ev.Outcome;
                var target = Get(o.Kind);
                if (target != null) target.Health = Math.Clamp(target.Health - ev.Damage, 0.0, 100.0);
                _actuator.Apply(o.Effect, o.EffectValue, data);
                Emit(new FlightFailureRecord
                {
                    Title = o.Name.ToUpperInvariant(),
                    Description = $"{o.Description} (dano: {ev.Damage:F0}% em {ComponentName(o.Kind)})",
                    IsCritical = o.Critical,
                    Effect = o.Effect,
                    Kind = o.Kind,
                    Source = "Evento externo"
                });
            }

            // 5. Gelo no carburador (recuperável com carb heat)
            switch (_carbIce.Update(data, dtHours * 3600.0))
            {
                case CarbIcingEvent.IceForming:
                    Emit(new FlightFailureRecord
                    {
                        Title = "GELO NO CARBURADOR",
                        Description = $"Temperatura do carburador em {data.CarbTemperature:F1}°C com baixa potência em ar úmido. Acione o CARB HEAT.",
                        IsCritical = false,
                        Source = "Operacional"
                    });
                    break;
                case CarbIcingEvent.EngineStopped:
                    _actuator.Apply(FailureEffect.EngineFailure, 0, data);
                    Emit(new FlightFailureRecord
                    {
                        Title = "MOTOR APAGOU: GELO NO CARBURADOR",
                        Description = "O gelo bloqueou o carburador. Mantenha o CARB HEAT ligado: após ~30 s o gelo derrete e o motor pode ser repartido.",
                        IsCritical = true,
                        Effect = FailureEffect.EngineFailure,
                        Kind = ComponentKind.Engine,
                        Source = "Operacional",
                        Persistent = false // recuperável com carb heat
                    });
                    break;
                case CarbIcingEvent.Recovered:
                    _actuator.RecoverEngineFailure();
                    Emit(new FlightFailureRecord
                    {
                        Title = "GELO DO CARBURADOR DERRETIDO",
                        Description = "O carb heat derreteu o gelo. Motor liberado para nova partida (mistura rica, bomba ligada, magnetos em BOTH).",
                        IsCritical = false,
                        Source = "Operacional"
                    });
                    break;
            }

            // 6. Efeitos contínuos (fogo, vazamento de óleo, enforcement)
            _actuator.Process(data, dtHours, Get(ComponentKind.Engine), Get(ComponentKind.Oil));
        }

        public void ProcessLanding(double touchdownFpm, double lateralFps, bool wetSurface)
        {
            if (_wear == null || _components.Count == 0) return;

            foreach (var ex in _wear.ApplyLanding(_components, touchdownFpm, lateralFps, wetSurface))
            {
                Emit(new FlightFailureRecord
                {
                    Title = $"EXCEDÊNCIA: {ex.Parameter.ToUpperInvariant()}",
                    Description = $"{ex.Parameter}: {ex.Value:F0} (limite {ex.Limit:F0}). Inspecione {ComponentName(ex.Kind)}.",
                    IsCritical = false,
                    Kind = ex.Kind,
                    Source = "Excedência"
                });
            }

            // Pouso duro pode trincar o cárter / retentor do virabrequim (uma chance por voo)
            var l = _catalog.Landing;
            double fpm = Math.Abs(touchdownFpm);
            if (!_oilLeakFromLandingChecked && fpm >= l.OilLeakMinFpm && Get(ComponentKind.Oil) != null)
            {
                _oilLeakFromLandingChecked = true;
                double chance = Math.Clamp((fpm - l.OilLeakMinFpm) / Math.Max(1.0, 1000.0 - l.OilLeakMinFpm) * l.OilLeakChanceAt1000Fpm, 0.0, 0.85);
                if (_random.NextDouble() < chance)
                {
                    _actuator.Apply(FailureEffect.OilLeak, l.OilLeakRatePerMin, _sim.CurrentData);
                    Emit(new FlightFailureRecord
                    {
                        Title = "VAZAMENTO DE ÓLEO APÓS POUSO DURO",
                        Description = $"O impacto de {fpm:F0} FPM trincou o cárter / retentor do virabrequim. Corte o motor assim que possível.",
                        IsCritical = true,
                        Effect = FailureEffect.OilLeak,
                        Kind = ComponentKind.Oil,
                        Source = "Operacional"
                    });
                }
            }
        }

        private void ApplyMode(ComponentKind kind, FailureModeDefinition mode, SimTelemetry data)
        {
            if (mode.Damage > 0)
            {
                var target = Get(mode.DamageKind ?? kind);
                if (target != null) target.Health = Math.Clamp(target.Health - mode.Damage, 0.0, 100.0);
            }

            _actuator.Apply(mode.Effect, mode.EffectValue, data);
            Emit(new FlightFailureRecord
            {
                Title = $"PANE: {mode.Name.ToUpperInvariant()}",
                Description = $"{mode.Description} ({ComponentName(kind)} em {Get(kind)?.Health ?? 0:F0}%)",
                IsCritical = mode.Critical,
                Effect = mode.Effect,
                Kind = kind,
                Source = "Desgaste"
            });
        }

        private string ComponentName(ComponentKind kind) =>
            _catalog.Components.FirstOrDefault(c => c.Kind == kind)?.Name.ToLowerInvariant() ?? kind.ToString();

        private void Emit(FlightFailureRecord record)
        {
            _history.Add(record);
            OnFailure?.Invoke(record);
        }
    }
}
