using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    public sealed record ComponentFailure(ComponentKind Kind, FailureModeDefinition Mode, double HealthAtFailure);

    /// <summary>
    /// Panes por desgaste. Acima do limiar (20%) o risco é zero; abaixo, a taxa cresce de forma suave:
    ///
    ///   λ(h) = λ20 × (λ0 / λ20) ^ ((20 − h) / 20)   [falhas por hora de uso]
    ///
    /// Um único sorteio por componente por intervalo (p = 1 − e^(−λ·dt)); se der pane,
    /// um segundo sorteio escolhe o modo de falha pelos pesos do catálogo.
    /// Cada componente falha no máximo uma vez por voo.
    /// </summary>
    public sealed class FailureEngine
    {
        private readonly ComponentCatalog _catalog;
        private readonly EngineClass _engineClass;
        private readonly IRandomSource _random;
        private readonly HashSet<ComponentKind> _failedThisFlight = new();

        public FailureEngine(ComponentCatalog catalog, EngineClass engineClass, IRandomSource random)
        {
            _catalog = catalog;
            _engineClass = engineClass;
            _random = random;
        }

        public IReadOnlyCollection<ComponentKind> FailedThisFlight => _failedThisFlight;

        public void Reset() => _failedThisFlight.Clear();

        /// <summary>Taxa de pane em falhas por hora de uso para uma saúde (0-100).</summary>
        public static double HazardPerHour(double health, HazardSettings settings, double scale = 1.0)
        {
            if (health >= settings.Threshold) return 0.0;
            double h = Math.Clamp(health, 0.0, settings.Threshold);
            double fraction = (settings.Threshold - h) / settings.Threshold; // 0 no limiar, 1 em 0%
            double ratio = settings.LambdaAtZero / settings.LambdaAtThreshold;
            return settings.LambdaAtThreshold * Math.Pow(ratio, fraction) * scale;
        }

        /// <summary>Probabilidade de pane num intervalo de uso de dtHours.</summary>
        public static double FailureProbability(double health, double dtHours, HazardSettings settings, double scale = 1.0)
        {
            double lambda = HazardPerHour(health, settings, scale);
            return lambda <= 0 || dtHours <= 0 ? 0.0 : 1.0 - Math.Exp(-lambda * dtHours);
        }

        /// <summary>Avalia panes por desgaste num intervalo de telemetria.</summary>
        public IReadOnlyList<ComponentFailure> Evaluate(IEnumerable<AircraftComponent> components, SimTelemetry data, double dtHours)
        {
            return Evaluate(components, dtHours, c => ComponentUsage.IsInUse(c.Kind, data));
        }

        /// <summary>Versão sem telemetria: todos os componentes informados são considerados em uso.</summary>
        public IReadOnlyList<ComponentFailure> Evaluate(IEnumerable<AircraftComponent> components, double dtHours, Func<AircraftComponent, bool>? isInUse = null)
        {
            var failures = new List<ComponentFailure>();
            if (dtHours <= 0) return failures;

            foreach (var component in components)
            {
                if (_failedThisFlight.Contains(component.Kind)) continue;
                if (isInUse != null && !isInUse(component)) continue;

                var def = _catalog.Components.FirstOrDefault(d => d.Kind == component.Kind);
                if (def == null || !def.AppliesTo.Contains(_engineClass)) continue;

                double p = FailureProbability(component.Health, dtHours, _catalog.Hazard, def.HazardScale);
                if (p <= 0 || _random.NextDouble() >= p) continue;

                var mode = PickMode(def);
                if (mode == null) continue;

                _failedThisFlight.Add(component.Kind);
                failures.Add(new ComponentFailure(component.Kind, mode, component.Health));
            }

            return failures;
        }

        private FailureModeDefinition? PickMode(ComponentDefinition def)
        {
            var modes = def.ModesFor(_engineClass).Where(m => m.Weight > 0).ToList();
            if (modes.Count == 0) return null;

            double total = modes.Sum(m => m.Weight);
            double roll = _random.NextDouble() * total;
            foreach (var mode in modes)
            {
                roll -= mode.Weight;
                if (roll < 0) return mode;
            }
            return modes[^1];
        }
    }
}
