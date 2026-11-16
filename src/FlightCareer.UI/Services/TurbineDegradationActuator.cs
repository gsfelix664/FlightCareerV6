using System;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Gerencia a gravação e conferência de perdas de motor a turbina no simulador de voo.
    /// Escreve quando o alvo muda ou quando a leitura diverge do simulador, respeitando
    /// a frequência de proteção de 5 segundos de relógio por motor.
    /// </summary>
    public sealed class TurbineDegradationActuator : IDisposable
    {
        private readonly ISimConnectService _sim;
        private readonly TurbineDegradationConfig? _config;
        private readonly Func<DateTime> _clock;

        private readonly TurbineLosses[] _lastWritten = new TurbineLosses[4];
        private readonly DateTime[] _lastWriteTime = new DateTime[4];
        private readonly bool[] _hasWritten = new bool[4];
        private readonly TurbineLosses[] _lastTarget = new TurbineLosses[4];

        public static readonly TimeSpan MinWriteInterval = TimeSpan.FromSeconds(5.0);

        public TurbineDegradationActuator(ISimConnectService sim, TurbineDegradationConfig? config, Func<DateTime>? clock = null)
        {
            _sim = sim;
            _config = config;
            _clock = clock ?? (() => DateTime.UtcNow);
            _sim.OnAircraftReloaded += HandleAircraftReloaded;

            for (int i = 0; i < 4; i++)
            {
                _lastWritten[i] = TurbineLosses.None;
                _lastTarget[i] = TurbineLosses.None;
            }
        }

        public void Apply(SimTelemetry data, AircraftComponent? engine, AircraftComponent? hotSection, int engineCount = 1)
        {
            if (_config == null) return;

            // Só atua se for motor a turbina (1 = Jet, 5 = Turboprop)
            bool isTurbine = data.EngineType == 1.0 || data.EngineType == 5.0;
            if (!isTurbine) return;

            double engineHealth = engine?.Health ?? 100.0;
            double hotSectionHealth = hotSection?.Health ?? 100.0;
            var target = TurbineDegradation.Compute(engineHealth, hotSectionHealth, _config);

            int count = Math.Clamp(engineCount, 1, 4);
            DateTime now = _clock();

            for (int i = 1; i <= count; i++)
            {
                int idx = i - 1;
                _lastTarget[idx] = target;

                var lastWritten = _lastWritten[idx];
                var currentSim = _sim.GetTurbineLosses(i);
                TimeSpan elapsed = _lastWriteTime[idx] == default ? TimeSpan.FromDays(1) : now - _lastWriteTime[idx];

                if (TurbineDegradation.ShouldWrite(target, lastWritten, currentSim, elapsed, _config.WriteToleranceStepPct, MinWriteInterval))
                {
                    _sim.SetTurbineLosses(i, target);
                    _lastWritten[idx] = target;
                    _lastWriteTime[idx] = now;
                    if (!target.IsZero)
                    {
                        _hasWritten[idx] = true;
                    }
                }
            }
        }

        public void Reset()
        {
            for (int i = 1; i <= 4; i++)
            {
                int idx = i - 1;
                if (_hasWritten[idx])
                {
                    _sim.SetTurbineLosses(i, TurbineLosses.None);
                    _hasWritten[idx] = false;
                    _lastWritten[idx] = TurbineLosses.None;
                }
                _lastTarget[idx] = TurbineLosses.None;
                _lastWriteTime[idx] = default;
            }
        }

        private void HandleAircraftReloaded()
        {
            // O simulador foi recarregado e zerou os valores. Reaplicar se o addon tinha perdas ativas.
            for (int i = 1; i <= 4; i++)
            {
                int idx = i - 1;
                if (_hasWritten[idx] && _lastTarget[idx] != null && !_lastTarget[idx].IsZero)
                {
                    _sim.SetTurbineLosses(i, _lastTarget[idx]);
                    _lastWritten[idx] = _lastTarget[idx];
                    _lastWriteTime[idx] = _clock();
                }
                else
                {
                    _lastWritten[idx] = TurbineLosses.None;
                }
            }
        }

        public void Dispose()
        {
            _sim.OnAircraftReloaded -= HandleAircraftReloaded;
        }
    }
}
