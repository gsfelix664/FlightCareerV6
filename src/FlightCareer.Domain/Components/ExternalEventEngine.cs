using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    /// <summary>Resultado de um evento externo: dano em um componente e, opcionalmente, um efeito no simulador.</summary>
    public sealed record ExternalEvent(string Source, ExternalOutcome Outcome, double Damage);

    /// <summary>
    /// Eventos externos que independem da saúde dos componentes (sempre ativos):
    /// - Bird strike: em voo, abaixo da altitude limite, com taxa ∝ (IAS/100)².
    /// - FOD: corrida no solo com potência alta (mais provável em pista molhada/contaminada).
    /// - Falha pós-manutenção: componente recém-revisado (poucas horas de uso desde o serviço).
    /// Cada tipo acontece no máximo uma vez por voo.
    /// </summary>
    public sealed class ExternalEventEngine
    {
        public const string BirdStrike = "BirdStrike";
        public const string Fod = "FOD";
        public const string PostMaintenance = "PostMaintenance";

        private readonly ExternalEventSettings _settings;
        private readonly EngineClass _engineClass;
        private readonly IRandomSource _random;
        private readonly HashSet<string> _happened = new();

        public ExternalEventEngine(ExternalEventSettings settings, EngineClass engineClass, IRandomSource random)
        {
            _settings = settings;
            _engineClass = engineClass;
            _random = random;
        }

        public void Reset() => _happened.Clear();

        public IReadOnlyList<ExternalEvent> Evaluate(IReadOnlyList<AircraftComponent> components, SimTelemetry data, double dtHours)
        {
            var events = new List<ExternalEvent>();
            if (dtHours <= 0) return events;

            // Bird strike
            var bird = _settings.BirdStrike;
            if (!data.IsOnGround && data.Altitude <= bird.MaxAltitudeFt && data.Airspeed >= bird.MinAirspeedKts)
            {
                double lambda = bird.LambdaPerHourAt100Kts * Math.Pow(data.Airspeed / 100.0, 2);
                TryFire(BirdStrike, lambda, dtHours, bird.Outcomes, components, events);
            }

            // FOD na corrida de decolagem / arremetida no solo
            var fod = _settings.Fod;
            if (data.IsOnGround && data.GroundSpeed > fod.MinGroundSpeedKts && data.ThrottlePercent > fod.MinThrottlePercent)
            {
                double lambda = fod.LambdaPerHour * (data.IsWetOrContaminatedSurface ? fod.WetMultiplier : 1.0);
                TryFire(Fod, lambda, dtHours, fod.Outcomes, components, events);
            }

            // Falha de montagem logo após manutenção: só resultados cujo componente foi revisado há pouco e está em uso
            var pm = _settings.PostMaintenance;
            var eligible = pm.Outcomes.Where(o =>
            {
                var c = components.FirstOrDefault(x => x.Kind == o.Kind);
                if (c == null || !ComponentUsage.IsInUse(o.Kind, data)) return false;
                // Componentes por ciclos não acumulam horas: "recém-revisado" = poucos pousos desde o serviço
                return ComponentUsage.IsCycleDriven(o.Kind)
                    ? c.CyclesSinceOverhaul < pm.WindowCycles
                    : c.HoursSinceOverhaul < pm.WindowHours;
            }).ToList();
            if (eligible.Count > 0)
                TryFire(PostMaintenance, pm.LambdaPerHour, dtHours, eligible, components, events);

            return events;
        }

        private void TryFire(string source, double lambdaPerHour, double dtHours, List<ExternalOutcome> outcomes,
            IReadOnlyList<AircraftComponent> components, List<ExternalEvent> events)
        {
            if (_happened.Contains(source) || lambdaPerHour <= 0) return;

            var candidates = outcomes
                .Where(o => o.Weight > 0)
                .Where(o => o.AppliesTo == null || o.AppliesTo.Count == 0 || o.AppliesTo.Contains(_engineClass))
                .Where(o => components.Any(c => c.Kind == o.Kind))
                .ToList();
            if (candidates.Count == 0) return;

            double p = 1.0 - Math.Exp(-lambdaPerHour * dtHours);
            if (_random.NextDouble() >= p) return;

            var outcome = PickByWeight(candidates);
            double damage = outcome.DamageMin + _random.NextDouble() * Math.Max(0.0, outcome.DamageMax - outcome.DamageMin);

            _happened.Add(source);
            events.Add(new ExternalEvent(source, outcome, damage));
        }

        private ExternalOutcome PickByWeight(List<ExternalOutcome> candidates)
        {
            double roll = _random.NextDouble() * candidates.Sum(c => c.Weight);
            foreach (var c in candidates)
            {
                roll -= c.Weight;
                if (roll < 0) return c;
            }
            return candidates[^1];
        }
    }

    public enum CarbIcingEvent
    {
        None,
        IceForming,   // Aviso: gelo formando, perda de RPM
        EngineStopped,
        Recovered
    }

    /// <summary>
    /// Gelo no carburador (aeronaves com carb heat). Diferente do sistema legado, a pane é recuperável:
    /// gelo por StopSeconds sem carb heat apaga o motor; carb heat ligado por RecoverySeconds depois
    /// disso derrete o gelo e libera o motor para nova partida.
    /// </summary>
    public sealed class CarbIcingMonitor
    {
        private readonly CarbIcingSettings _s;
        private double _iceSeconds;
        private double _recoverySeconds;
        private bool _warned;

        public CarbIcingMonitor(CarbIcingSettings settings) => _s = settings;

        public bool EngineStoppedByIce { get; private set; }

        public void Reset()
        {
            _iceSeconds = 0;
            _recoverySeconds = 0;
            _warned = false;
            EngineStoppedByIce = false;
        }

        public CarbIcingEvent Update(SimTelemetry data, double dtSeconds)
        {
            if (dtSeconds <= 0 || data.CarbHeatAvailable <= 0.5) return CarbIcingEvent.None;

            if (EngineStoppedByIce)
            {
                _recoverySeconds = data.IsCarbHeatOn ? _recoverySeconds + dtSeconds : 0.0;
                if (_recoverySeconds >= _s.RecoverySeconds)
                {
                    Reset();
                    return CarbIcingEvent.Recovered;
                }
                return CarbIcingEvent.None;
            }

            if (data.IsOnGround || !data.IsEngineRunning)
            {
                _iceSeconds = Math.Max(0, _iceSeconds - dtSeconds);
                return CarbIcingEvent.None;
            }

            bool humid = data.IsPrecipitating ||
                         (data.AmbientTemperature >= _s.MinAmbientTemperature && data.AmbientTemperature <= _s.MaxAmbientTemperature);
            bool icing = data.CarbTemperature <= _s.MaxCarbTemperature && data.ThrottlePercent < _s.MaxThrottlePercent && humid;

            if (icing && !data.IsCarbHeatOn)
            {
                _iceSeconds += dtSeconds;
                if (_iceSeconds >= _s.StopSeconds)
                {
                    EngineStoppedByIce = true;
                    _recoverySeconds = 0;
                    return CarbIcingEvent.EngineStopped;
                }
                if (!_warned && _iceSeconds >= _s.WarningSeconds)
                {
                    _warned = true;
                    return CarbIcingEvent.IceForming;
                }
            }
            else
            {
                // Carb heat ou potência/temperatura restauradas derretem o gelo 2× mais rápido
                _iceSeconds = Math.Max(0, _iceSeconds - dtSeconds * 2.0);
                if (_iceSeconds <= 0) _warned = false;
            }

            return CarbIcingEvent.None;
        }
    }
}
