using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Limits
{
    /// <summary>
    /// Vocabulário fechado de chaves e unidades para limites de operação das aeronaves.
    /// </summary>
    public static class LimitKeys
    {
        public const string RpmRedline = "rpmRedline";
        public const string RpmMaxRated = "rpmMaxRated";
        public const string RpmNativeOnset = "rpmNativeOnset";
        public const string ChtMax = "chtMax";
        public const string OilTempMax = "oilTempMax";
        public const string OilPressMin = "oilPressMin";
        public const string EgtMax = "egtMax";
        public const string IttMax = "ittMax";
        public const string TorqueMaxPct = "torqueMaxPct";
        public const string N1MaxPct = "n1MaxPct";
        public const string Vne = "vne";
        public const string Vno = "vno";
        public const string Vle = "vle";
        public const string Vlo = "vlo";
        public const string Va = "va";
        public const string Mmo = "mmo";
        public const string Vs0 = "vs0";
        public const string Vs1 = "vs1";
        public const string GLimitPos = "gLimitPos";
        public const string GLimitNeg = "gLimitNeg";
        public const string MaxTouchdownFpm = "maxTouchdownFpm";
        public const string ChtCoolingRateMax = "chtCoolingRateMax";

        // Parâmetros do sistema elétrico da aeronave (F2.4b-CAL)
        public const string ElecSystemVolts = "elecSystemVolts";
        public const string ElecAvionicsCircuitIndex = "elecAvionicsCircuitIndex";
        public const string ElecAvionicsBusAmpsIndex = "elecAvionicsBusAmpsIndex";
        public const string ElecMainBusIndex = "elecMainBusIndex";
        public const string AlternatorRatedAmps = "alternatorRatedAmps";
        public const string ElecDeepDischargeVolts = "elecDeepDischargeVolts";

        /// <summary>
        /// Chave dinâmica que representa o limite estrutural do dente de flap atualmente estendido (Vfe).
        /// Resolvida dinamicamente em tempo de execução no motor de desgaste (F2.3) a partir da posição do manete/superfície
        /// de flaps ('af-flaps-handle-index') mapeada para o limite do dente ('vfe:1'..'vfe:n').
        /// </summary>
        public const string VfeCurrent = "vfe:current";

        private static readonly Dictionary<string, string> FixedUnits = new(StringComparer.Ordinal)
        {
            [RpmRedline] = "rpm",
            [RpmMaxRated] = "rpm",
            [RpmNativeOnset] = "rpm",
            [ChtMax] = "celsius",
            [OilTempMax] = "celsius",
            [OilPressMin] = "psi",
            [EgtMax] = "celsius",
            [IttMax] = "celsius",
            [TorqueMaxPct] = "percent",
            [N1MaxPct] = "percent",
            [Vne] = "knots",
            [Vno] = "knots",
            [Vle] = "knots",
            [Vlo] = "knots",
            [Va] = "knots",
            [Mmo] = "mach",
            [Vs0] = "knots",
            [Vs1] = "knots",
            [GLimitPos] = "g",
            [GLimitNeg] = "g",
            [MaxTouchdownFpm] = "fpm",
            [ChtCoolingRateMax] = "celsius_per_minute",
            [VfeCurrent] = "knots",
            [ElecSystemVolts] = "volts",
            [ElecAvionicsCircuitIndex] = "index",
            [ElecAvionicsBusAmpsIndex] = "index",
            [ElecMainBusIndex] = "index",
            [AlternatorRatedAmps] = "amperes",
            [ElecDeepDischargeVolts] = "volts"
        };

        public static bool IsValid(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            if (FixedUnits.ContainsKey(key))
            {
                return true;
            }

            if (TryParseVfe(key, out _))
            {
                return true;
            }

            return false;
        }

        public static string UnitOf(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Chave não pode ser nula ou vazia.", nameof(key));
            }

            if (FixedUnits.TryGetValue(key, out var unit))
            {
                return unit;
            }

            if (TryParseVfe(key, out _))
            {
                return "knots";
            }

            throw new ArgumentException($"Chave de limite desconhecida: '{key}'.", nameof(key));
        }

        private static bool TryParseVfe(string key, out int flapIndex)
        {
            flapIndex = 0;
            if (key.StartsWith("vfe:", StringComparison.Ordinal))
            {
                var span = key.AsSpan(4);
                if (int.TryParse(span, out int index) && index >= 1 && index <= 10)
                {
                    flapIndex = index;
                    return true;
                }
            }
            return false;
        }
    }
}
