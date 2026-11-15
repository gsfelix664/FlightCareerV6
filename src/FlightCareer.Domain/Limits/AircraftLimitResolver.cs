using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Limits
{
    /// <summary>
    /// Resolve o perfil de limites de uma aeronave e devolve cada limite com prioridade de fonte
    /// (POH > SimVar > Cfg > NativeOnset > ClassDefault). Lógica pura, sem EF nem SimConnect.
    /// </summary>
    public sealed class AircraftLimitResolver
    {
        private readonly List<AircraftLimitProfile> _profiles;
        private readonly Dictionary<string, IReadOnlyList<AircraftLimit>> _classDefaults;

        public AircraftLimitResolver(
            IEnumerable<AircraftLimitProfile> profiles,
            IReadOnlyDictionary<string, IReadOnlyList<AircraftLimit>> classDefaults)
        {
            _profiles = profiles?.ToList() ?? new List<AircraftLimitProfile>();
            _classDefaults = new Dictionary<string, IReadOnlyList<AircraftLimit>>(StringComparer.OrdinalIgnoreCase);

            if (classDefaults != null)
            {
                foreach (var (cls, limits) in classDefaults)
                {
                    _classDefaults[cls] = limits;
                }
            }
        }

        /// <summary>
        /// Localiza o perfil da aeronave: primeiro se TITLE contém algum TitlePattern (case-insensitive);
        /// em segundo se ATC MODEL contém algum AtcModelPattern (case-insensitive); senão null.
        /// </summary>
        public AircraftLimitProfile? FindProfile(string? title, string? atcModel)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                var matchByTitle = _profiles.FirstOrDefault(p =>
                    p.TitlePatterns.Any(pattern =>
                        !string.IsNullOrWhiteSpace(pattern) &&
                        title.Contains(pattern, StringComparison.OrdinalIgnoreCase)));

                if (matchByTitle != null)
                {
                    return matchByTitle;
                }
            }

            if (!string.IsNullOrWhiteSpace(atcModel))
            {
                var matchByAtc = _profiles.FirstOrDefault(p =>
                    p.AtcModelPatterns.Any(pattern =>
                        !string.IsNullOrWhiteSpace(pattern) &&
                        atcModel.Contains(pattern, StringComparison.OrdinalIgnoreCase)));

                if (matchByAtc != null)
                {
                    return matchByAtc;
                }
            }

            return null;
        }

        /// <summary>
        /// Obtém o limite operacional para a chave indicada:
        /// Se definido no perfil, retorna o de menor LimitSource (maior prioridade).
        /// Caso não exista no perfil, busca no padrão da classe (retornando com Source = ClassDefault).
        /// Se não existir em nenhum, retorna null.
        /// </summary>
        public AircraftLimit? Get(AircraftLimitProfile? profile, string engineClassOrClass, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (profile != null)
            {
                var candidate = profile.Limits
                    .Where(l => string.Equals(l.Key, key, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(l => l.Source)
                    .FirstOrDefault();

                if (candidate != null)
                {
                    return candidate;
                }
            }

            if (!string.IsNullOrWhiteSpace(engineClassOrClass) &&
                _classDefaults.TryGetValue(engineClassOrClass, out var defaultLimits))
            {
                var classDefault = defaultLimits
                    .FirstOrDefault(l => string.Equals(l.Key, key, StringComparison.OrdinalIgnoreCase));

                if (classDefault != null)
                {
                    return classDefault with { Source = LimitSource.ClassDefault };
                }
            }

            return null;
        }
    }
}
