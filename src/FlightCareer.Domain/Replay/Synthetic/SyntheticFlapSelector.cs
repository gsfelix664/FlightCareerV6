using System;
using System.Linq;
using FlightCareer.Domain.Limits;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Resultado da seleção de dente de flap e respectivo Vfe para voos sintéticos.
    /// </summary>
    public readonly record struct SyntheticFlapSelection(int FlapIndex, double Vfe);

    /// <summary>
    /// Seleciona o dente de flap apropriado para aproximação e abusos de sobrevelocidade de flap (FlapOverSpeed),
    /// respeitando os dentes de flap efetivamente configurados no perfil da aeronave.
    /// </summary>
    public static class SyntheticFlapSelector
    {
        public static SyntheticFlapSelection Select(AircraftLimitProfile profile, Func<string, double, double> resolveLimit)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (resolveLimit == null) throw new ArgumentNullException(nameof(resolveLimit));

            // 1. Se o perfil tem vfe:2 -> dente 2 (comportamento padrão)
            if (HasProfileLimit(profile, "vfe:2"))
            {
                return new SyntheticFlapSelection(2, resolveLimit("vfe:2", 85.0));
            }

            // 2. Senão, o menor dente n >= 2 com vfe:n no perfil
            int minN = int.MaxValue;
            if (profile.Limits != null)
            {
                foreach (var lim in profile.Limits)
                {
                    if (lim.Value > 0 && TryParseVfeIndex(lim.Key, out int n) && n >= 2)
                    {
                        if (n < minN)
                        {
                            minN = n;
                        }
                    }
                }
            }

            if (minN != int.MaxValue)
            {
                return new SyntheticFlapSelection(minN, resolveLimit($"vfe:{minN}", 85.0));
            }

            // 3. Senão, se tem vfe:1 -> dente 1
            if (HasProfileLimit(profile, "vfe:1"))
            {
                return new SyntheticFlapSelection(1, resolveLimit("vfe:1", 110.0));
            }

            // 4. Senão (só vfe genérico ou nada) -> dente 2 com ResolveLimit("vfe", 85)
            return new SyntheticFlapSelection(2, resolveLimit("vfe", 85.0));
        }

        private static bool HasProfileLimit(AircraftLimitProfile profile, string key)
        {
            if (profile.Limits == null) return false;
            return profile.Limits.Any(l => string.Equals(l.Key, key, StringComparison.OrdinalIgnoreCase) && l.Value > 0);
        }

        private static bool TryParseVfeIndex(string key, out int index)
        {
            index = 0;
            if (key.StartsWith("vfe:", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(key.AsSpan(4), out index);
            }
            return false;
        }
    }
}
