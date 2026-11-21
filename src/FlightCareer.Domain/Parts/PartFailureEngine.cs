using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public sealed record PartFailure(
        AircraftPart Part,
        ComponentKind Kind,
        FailureModeDefinition Mode,
        double HealthAtFailure);

    public sealed class PartFailureEngine
    {
        private readonly ComponentCatalog _catalog;
        private readonly PartCatalog _parts;
        private readonly EngineClass _engineClass;
        private readonly IRandomSource _random;
        private readonly PartComponentMapping _mapping;
        private readonly HashSet<ComponentKind> _failedThisFlight = new();

        public PartFailureEngine(
            ComponentCatalog catalog,
            PartCatalog parts,
            EngineClass engineClass,
            IRandomSource random,
            PartComponentMapping? mapping = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
            _engineClass = engineClass;
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _mapping = mapping ?? PartComponentMapping.CreateDefault();
        }

        public IReadOnlyCollection<ComponentKind> FailedThisFlight => _failedThisFlight;

        public void Reset() => _failedThisFlight.Clear();

        public IReadOnlyList<PartFailure> Evaluate(
            IEnumerable<AircraftPart> parts,
            double dtHours,
            Func<ComponentKind, bool> isInUse,
            IReadOnlyCollection<ComponentKind> alreadyFailed)
        {
            var failures = new List<PartFailure>();
            if (parts == null || dtHours <= 0) return failures;

            foreach (var part in parts)
            {
                if (part.IsBroken) continue;

                var def = _parts.Get(part.PartId);
                if (def == null || def.MinorPart) continue;

                var kind = _mapping.ResolveComponentKind(def.Group);
                if (!kind.HasValue) continue;

                if (alreadyFailed != null && alreadyFailed.Contains(kind.Value)) continue;
                if (_failedThisFlight.Contains(kind.Value)) continue;
                if (isInUse != null && !isInUse(kind.Value)) continue;

                var compDef = _catalog.Components.FirstOrDefault(c => c.Kind == kind.Value);
                if (compDef == null || !compDef.AppliesTo.Contains(_engineClass)) continue;

                double lambda = FailureEngine.HazardPerHour(part.Health, _catalog.Hazard, compDef.HazardScale);
                double p = lambda <= 0.0 ? 0.0 : 1.0 - Math.Exp(-lambda * dtHours);

                if (p <= 0.0 || _random.NextDouble() >= p) continue;

                var mode = FailureEngine.PickMode(compDef, _engineClass, _random);
                if (mode == null) continue;

                _failedThisFlight.Add(kind.Value);
                failures.Add(new PartFailure(part, kind.Value, mode, part.Health));
            }

            return failures;
        }
    }
}
