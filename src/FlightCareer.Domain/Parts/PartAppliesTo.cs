using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Regras de aplicação de uma peça baseadas nas características (features) da aeronave.
    /// { "all": [features], "any": [features], "none": [features] }
    /// </summary>
    public sealed class PartAppliesTo
    {
        public IReadOnlyList<string> All { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Any { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> None { get; init; } = Array.Empty<string>();

        public PartAppliesTo() { }

        public PartAppliesTo(
            IEnumerable<string>? all = null,
            IEnumerable<string>? any = null,
            IEnumerable<string>? none = null)
        {
            All = all?.ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();
            Any = any?.ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();
            None = none?.ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();
        }

        /// <summary>
        /// Avalia se a peça é aplicável a uma aeronave com o conjunto de features fornecido.
        /// </summary>
        public bool Matches(IReadOnlyCollection<string> aircraftFeatures)
        {
            var set = aircraftFeatures as IReadOnlySet<string>
                      ?? new HashSet<string>(aircraftFeatures, StringComparer.OrdinalIgnoreCase);

            // 1. None: não pode conter nenhuma dessas features
            if (None.Count > 0 && None.Any(f => set.Contains(f)))
            {
                return false;
            }

            // 2. All: precisa conter todas essas features
            if (All.Count > 0 && All.Any(f => !set.Contains(f)))
            {
                return false;
            }

            // 3. Any: precisa conter pelo menos uma dessas features (se lista não vazia)
            if (Any.Count > 0 && !Any.Any(f => set.Contains(f)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida se todas as features citadas pertencem ao vocabulário fechado.
        /// </summary>
        public void ValidateVocabulary(string partId)
        {
            foreach (var f in All.Concat(Any).Concat(None))
            {
                if (!AircraftFeatures.All.Contains(f))
                {
                    throw new InvalidOperationException($"Peça '{partId}': feature '{f}' em AppliesTo não existe no vocabulário fechado de AircraftFeatures.");
                }
            }
        }
    }
}
