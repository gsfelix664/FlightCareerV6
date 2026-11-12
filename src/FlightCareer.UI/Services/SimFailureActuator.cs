using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.UI.Services
{
    /// <summary>Pane/evento comunicado ao piloto (alerta + blackbox).</summary>
    public sealed class FlightFailureRecord
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public bool IsCritical { get; init; }
        public FailureEffect Effect { get; init; } = FailureEffect.None;
        public ComponentKind? Kind { get; init; }
        public string Source { get; init; } = string.Empty; // Desgaste, Evento externo, Operacional, Excedência
        public DateTime Timestamp { get; init; } = DateTime.Now;
    }

    /// <summary>
    /// Traduz efeitos de pane (FailureEffect) em ações no MSFS e mantém os efeitos contínuos:
    /// - Eventos toggle do SDK (motor, elétrico, vácuo, pitot, estática, freios, hidráulico).
    /// - Fogo progressivo (ENG ON FIRE:1): extinto com corte de combustível + bomba por 25 s; destrói o motor em 90 s.
    /// - Vazamento de óleo progressivo (GENERAL ENG OIL LEAKED PERCENT:1).
    /// - Enforcement de chaves sem evento nativo (magneto, alternador, flaps).
    /// </summary>
    public sealed class SimFailureActuator
    {
        private readonly SimConnectService _sim;
        private readonly IRandomSource _random;
        private readonly HashSet<FailureEffect> _active = new();

        // Fogo
        private bool _fireActive;
        private bool _engineDestroyedByFire;
        private double _fireBurnSeconds;
        private double _fireCoolingSeconds;

        // Óleo
        private double _oilLeakedPercent;
        private double _oilLeakRatePerMin;
        private double _lastTransmittedLeak;

        // Enforcement
        private double _lockedFlapsHandle;
        private DateTime _lastEnforcement = DateTime.MinValue;

        public event Action<FlightFailureRecord>? OnRecord;

        public SimFailureActuator(SimConnectService sim, IRandomSource random)
        {
            _sim = sim;
            _random = random;
        }

        public bool IsEngineOnFire => _fireActive;
        public double OilLeakedPercent => _oilLeakedPercent;
        public bool IsActive(FailureEffect effect) => _active.Contains(effect);

        public void Reset(bool clearSimState)
        {
            if (clearSimState)
            {
                _sim.ClearSimFailures();
                if (_fireActive || _sim.CurrentData.IsEngineOnFire) _sim.SetEngineFire(false);
                if (_oilLeakedPercent > 0 || _sim.CurrentData.OilLeakedPercent > 0) _sim.SetOilLeakedPercent(0.0);
            }

            _active.Clear();
            _fireActive = false;
            _engineDestroyedByFire = false;
            _fireBurnSeconds = 0;
            _fireCoolingSeconds = 0;
            _oilLeakedPercent = 0;
            _oilLeakRatePerMin = 0;
            _lastTransmittedLeak = 0;
            _lockedFlapsHandle = 0;
            _lastEnforcement = DateTime.MinValue;
        }

        public void Apply(FailureEffect effect, double value, SimTelemetry data)
        {
            if (effect == FailureEffect.None) return;
            _active.Add(effect);

            switch (effect)
            {
                case FailureEffect.EngineFailure:
                    _sim.SetFailure("TOGGLE_ENGINE1_FAILURE");
                    break;
                case FailureEffect.EngineFire:
                    if (!_fireActive && !_engineDestroyedByFire)
                    {
                        _fireActive = true;
                        _fireBurnSeconds = 0;
                        _fireCoolingSeconds = 0;
                        _sim.SetEngineFire(true);
                    }
                    break;
                case FailureEffect.OilLeak:
                    _oilLeakRatePerMin = Math.Max(_oilLeakRatePerMin, value > 0 ? value : 1.5);
                    _oilLeakedPercent = Math.Max(_oilLeakedPercent, 5.0);
                    TransmitOilLeak(force: true);
                    break;
                case FailureEffect.ElectricalFailure:
                    _sim.SetFailure("TOGGLE_ELECTRICAL_FAILURE");
                    break;
                case FailureEffect.VacuumFailure:
                    _sim.SetFailure("TOGGLE_VACUUM_FAILURE");
                    break;
                case FailureEffect.PitotBlockage:
                    _sim.SetFailure("TOGGLE_PITOT_BLOCKAGE");
                    break;
                case FailureEffect.StaticBlockage:
                    _sim.SetFailure("TOGGLE_STATIC_PORT_BLOCKAGE");
                    break;
                case FailureEffect.BrakeFailureOneSide:
                    _sim.SetFailure(_random.NextDouble() < 0.5 ? "TOGGLE_LEFT_BRAKE_FAILURE" : "TOGGLE_RIGHT_BRAKE_FAILURE");
                    break;
                case FailureEffect.BrakeFailureTotal:
                    _sim.SetFailure("TOGGLE_TOTAL_BRAKE_FAILURE");
                    break;
                case FailureEffect.HydraulicFailure:
                    _sim.SetFailure("TOGGLE_HYDRAULIC_FAILURE");
                    break;
                case FailureEffect.FlapJam:
                    _lockedFlapsHandle = NormalizeFlaps(data.FlapsHandlePercent);
                    break;
                // MagnetoLeft/MagnetoRight/AlternatorOff: mantidos em Process (enforcement)
            }
        }

        /// <summary>Recupera o motor apagado por gelo no carburador.</summary>
        public void RecoverEngineFailure()
        {
            _active.Remove(FailureEffect.EngineFailure);
            _sim.ClearFailure("TOGGLE_ENGINE1_FAILURE");
        }

        /// <summary>Efeitos contínuos a cada tick.</summary>
        public void Process(SimTelemetry data, double dtHours, AircraftComponent? engine, AircraftComponent? oil)
        {
            ProcessFire(data, dtHours, engine);
            ProcessOilLeak(data, dtHours, oil);
            Enforce(data);
        }

        private void ProcessFire(SimTelemetry data, double dtHours, AircraftComponent? engine)
        {
            if (!_fireActive || _engineDestroyedByFire) return;
            double dt = dtHours * 3600.0;
            bool isTurbine = data.EngineType == 1.0 || data.EngineType == 5.0;
            bool fuelCutOff = (isTurbine ? data.ConditionLever < 5.0 : data.MixturePercent < 5.0) && data.FuelPumpSwitch <= 0.5;

            if (fuelCutOff)
            {
                _fireCoolingSeconds += dt;
                if (engine != null) engine.Health = Math.Max(1.0, engine.Health - 0.15 * dt);

                if (_fireCoolingSeconds >= 25.0)
                {
                    _fireActive = false;
                    _sim.SetEngineFire(false);
                    OnRecord?.Invoke(new FlightFailureRecord
                    {
                        Title = "INCÊNDIO EXTINTO (CORTE DE COMBUSTÍVEL)",
                        Description = $"O corte de combustível e o vento relativo extinguiram o fogo. Motor preservado em {engine?.Health ?? 0:F1}%. Não tente repartir.",
                        IsCritical = false,
                        Effect = FailureEffect.EngineFire,
                        Kind = ComponentKind.Engine,
                        Source = "Operacional"
                    });
                }
                return;
            }

            _fireCoolingSeconds = Math.Max(0, _fireCoolingSeconds - dt * 0.5);
            _fireBurnSeconds += dt;
            if (engine != null) engine.Health = Math.Max(0.0, engine.Health - 0.80 * dt);
            if (!data.IsEngineOnFire) _sim.SetEngineFire(true);

            if (_fireBurnSeconds >= 90.0)
            {
                _fireActive = false;
                _engineDestroyedByFire = true;
                if (engine != null) engine.Health = 0.0;
                _sim.SetEngineFire(false);
                _active.Add(FailureEffect.EngineFailure);
                _sim.SetFailure("TOGGLE_ENGINE1_FAILURE");
                OnRecord?.Invoke(new FlightFailureRecord
                {
                    Title = "MOTOR DESTRUÍDO PELO FOGO",
                    Description = "O incêndio não foi contido em 90 segundos (combustível não cortado). O motor foi destruído e o fogo se extinguiu após consumir o compartimento.",
                    IsCritical = true,
                    Effect = FailureEffect.EngineFailure,
                    Kind = ComponentKind.Engine,
                    Source = "Operacional"
                });
            }
        }

        private void ProcessOilLeak(SimTelemetry data, double dtHours, AircraftComponent? oil)
        {
            if (data.OilLeakedPercent > _oilLeakedPercent && _oilLeakRatePerMin > 0)
                _oilLeakedPercent = data.OilLeakedPercent;

            if (_oilLeakRatePerMin <= 0) return;

            if (data.IsEngineRunning)
                _oilLeakedPercent = Math.Min(100.0, _oilLeakedPercent + _oilLeakRatePerMin * dtHours * 60.0);

            TransmitOilLeak(force: false);

            // Óleo que vazou é óleo que falta: a barra de óleo acompanha o nível
            if (oil != null) oil.Health = Math.Min(oil.Health, Math.Max(0.0, 100.0 - _oilLeakedPercent));
        }

        private void TransmitOilLeak(bool force)
        {
            if (_oilLeakedPercent <= 0) return;
            if (force || Math.Abs(_oilLeakedPercent - _lastTransmittedLeak) >= 0.2 || _oilLeakedPercent >= 100.0)
            {
                _sim.SetOilLeakedPercent(_oilLeakedPercent);
                _lastTransmittedLeak = _oilLeakedPercent;
            }
        }

        private void Enforce(SimTelemetry data)
        {
            if ((DateTime.Now - _lastEnforcement).TotalSeconds < 2.0) return;

            bool leftFailed = _active.Contains(FailureEffect.MagnetoLeft);
            bool rightFailed = _active.Contains(FailureEffect.MagnetoRight);
            if ((leftFailed || rightFailed) && data.IsEngineRunning && data.StarterActive <= 0.5)
            {
                bool failedOn = leftFailed ? data.LeftMagneto > 0.5 : data.RightMagneto > 0.5;
                bool goodOn = leftFailed ? data.RightMagneto > 0.5 : data.LeftMagneto > 0.5;
                if (failedOn && goodOn)
                {
                    _sim.SendEvent(leftFailed ? "MAGNETO1_RIGHT" : "MAGNETO1_LEFT");
                    _lastEnforcement = DateTime.Now;
                    return;
                }
                if (failedOn)
                {
                    _sim.SendEvent("MAGNETO1_OFF");
                    _lastEnforcement = DateTime.Now;
                    return;
                }
            }

            if (_active.Contains(FailureEffect.AlternatorOff) && data.MasterAlternator > 0.5)
            {
                _sim.SendEvent("TOGGLE_ALTERNATOR1");
                _lastEnforcement = DateTime.Now;
                return;
            }

            if (_active.Contains(FailureEffect.FlapJam) && Math.Abs(NormalizeFlaps(data.FlapsHandlePercent) - _lockedFlapsHandle) > 0.02)
            {
                _sim.SendEvent("FLAPS_SET", (uint)Math.Round(Math.Clamp(_lockedFlapsHandle, 0.0, 1.0) * 16383.0));
                _lastEnforcement = DateTime.Now;
            }
        }

        private static double NormalizeFlaps(double handle) => handle > 1.0 ? handle / 100.0 : handle;
    }
}
