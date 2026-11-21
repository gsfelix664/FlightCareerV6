using System;
using System.Collections.Generic;
using System.Globalization;
using FlightCareer.Domain.Services;

namespace FlightCareer.Domain.Components
{
    /// <summary>Componente do wear and tear nativo (ID 0–49 do SDK) → pane persistente e barra do addon.</summary>
    public sealed record NativeWearFailureSpec(int ComponentId, string NativeName, string? DefectCode, ComponentKind? Kind);

    /// <summary>
    /// Tabela 2 de <c>docs/plans/sensor-validation/native-wear-mapping.md</c> (aprovada em 2026-11-14):
    /// <c>WEAR AND TEAR IS FAILED:&lt;id&gt;</c> 0 → 1 = pane do simulador (plano › 0.1, regra 5).
    /// A pane do wear and tear não liga <c>ENG FAILED</c> nem <c>GENERAL ENG DAMAGE PERCENT</c> (F1.4c, C152).
    /// </summary>
    public static class NativeWearFailureMap
    {
        /// <summary>Prefixo da chave no <c>SensorFrame</c> (registro de sensores, grupo native-wear).</summary>
        public const string SensorKeyPrefix = "nw-is-failed-";

        private static readonly Dictionary<int, NativeWearFailureSpec> Specs = Build();

        public static NativeWearFailureSpec? Get(int componentId) =>
            Specs.TryGetValue(componentId, out var spec) ? spec : null;

        public static IReadOnlyCollection<NativeWearFailureSpec> All => Specs.Values;

        /// <summary>Início do campo Cause da discrepância gravada por esta detecção.</summary>
        public const string CausePrefix = "WEAR AND TEAR IS FAILED:";

        /// <summary>Texto do Cause: identifica o componente sem usar o Parameter (o Parameter dos flaps é a posição travada).</summary>
        public static string CauseFor(int componentId, string nativeName) =>
            $"{CausePrefix}{componentId.ToString(CultureInfo.InvariantCulture)} passou de 0 para 1 ({nativeName})";

        /// <summary>Cause gravado por <see cref="CauseFor"/> → ID do componente; outro texto → null.</summary>
        public static int? ComponentIdFromCause(string? cause)
        {
            if (string.IsNullOrEmpty(cause) || !cause.StartsWith(CausePrefix, StringComparison.Ordinal)) return null;
            var rest = cause.AsSpan(CausePrefix.Length);
            int len = 0;
            while (len < rest.Length && char.IsAsciiDigit(rest[len])) len++;
            return len > 0 && int.TryParse(rest[..len], NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
        }

        /// <summary>"nw-is-failed-39" → 39; outra chave → null.</summary>
        public static int? ComponentIdFromKey(string key)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith(SensorKeyPrefix, StringComparison.Ordinal)) return null;
            return int.TryParse(key.AsSpan(SensorKeyPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id : null;
        }

        private static Dictionary<int, NativeWearFailureSpec> Build()
        {
            var d = new Dictionary<int, NativeWearFailureSpec>();
            void Add(string code, ComponentKind? kind, string name, params int[] ids)
            {
                foreach (var id in ids) d[id] = new NativeWearFailureSpec(id, name, code, kind);
            }
            void LogOnly(string name, params int[] ids)
            {
                foreach (var id in ids) d[id] = new NativeWearFailureSpec(id, name, null, null);
            }

            Add(DefectCatalog.FuelSystem, null, "FUEL", 0, 1, 2, 3, 4);
            Add(DefectCatalog.FlapDamage, ComponentKind.Airframe, "FLAPS", 5, 6);
            Add(DefectCatalog.FlapLock, ComponentKind.Airframe, "FLAPS CABLE", 7, 8);
            Add(DefectCatalog.FlightControl, ComponentKind.Airframe, "FLIGHT CONTROLS", 9, 10, 11, 12, 13, 14, 19, 20);
            Add(DefectCatalog.Hydraulic, ComponentKind.LandingGear, "HYDRAULIC", 21, 22, 23, 24, 25, 26, 27);
            Add(DefectCatalog.Alternator, ComponentKind.Electrical, "ELECTRICAL ALTERNATOR", 28);
            Add(DefectCatalog.Battery, ComponentKind.Electrical, "ELECTRICAL BATTERY", 29);
            Add(DefectCatalog.Electrical, ComponentKind.Electrical, "ELECTRICAL", 30, 31, 32, 33, 34);
            Add(DefectCatalog.GearDamage, ComponentKind.LandingGear, "LANDING GEAR", 35, 47);
            Add(DefectCatalog.BrakeFailure, ComponentKind.Brakes, "BRAKE", 36);
            Add(DefectCatalog.TireBlowout, ComponentKind.Tires, "TIRE", 37, 48);
            Add(DefectCatalog.TireFlat, ComponentKind.Tires, "TIRE PRESSURE", 38, 49);
            Add(DefectCatalog.EngineFailure, ComponentKind.Engine, "PISTON ENGINE", 39);
            Add(DefectCatalog.EngineFailure, ComponentKind.Engine, "TURBINE ENGINE", 40);
            Add(DefectCatalog.OilLeak, ComponentKind.Oil, "OIL TANK INTEGRITY", 41);
            LogOnly("OIL TANK AMOUNT", 42);
            Add(DefectCatalog.Autopilot, null, "AUTOPILOT", 43, 44, 45);
            LogOnly("GENERAL", 46);
            return d;
        }
    }

    /// <summary>Pane nativa detectada: componente do sim, pane e barra do addon.</summary>
    public sealed record NativeWearFailureEvent(int ComponentId, string NativeName, string? DefectCode, ComponentKind? Kind);

    /// <summary>
    /// Detecta a borda 0 → 1 de <c>WEAR AND TEAR IS FAILED:&lt;id&gt;</c>. A primeira leitura de cada componente é só
    /// referência (componente que já chega quebrado depois de reiniciar o voo não gera pane nova); cada componente
    /// dispara uma vez por voo; voltar a 0 (reinício do voo no sim) não repara nada. Lógica pura, sem SimConnect.
    /// </summary>
    public sealed class NativeWearFailureDetector
    {
        private readonly Dictionary<int, bool> _previous = new();
        private readonly HashSet<int> _fired = new();

        /// <summary>Zera o estado (início de um voo monitorado).</summary>
        public void Reset()
        {
            _previous.Clear();
            _fired.Clear();
        }

        /// <summary>Recebe os valores do frame (chave do registro → valor) e devolve as panes novas.</summary>
        public IReadOnlyList<NativeWearFailureEvent> Update(IReadOnlyDictionary<string, double> values)
        {
            var events = new List<NativeWearFailureEvent>();
            if (values == null) return events;

            foreach (var kvp in values)
            {
                if (NativeWearFailureMap.ComponentIdFromKey(kvp.Key) is not int id) continue;
                if (double.IsNaN(kvp.Value)) continue;

                bool failed = kvp.Value >= 0.5;
                bool hadPrevious = _previous.TryGetValue(id, out bool wasFailed);
                _previous[id] = failed;

                if (!hadPrevious || wasFailed || !failed) continue;
                if (!_fired.Add(id)) continue;

                var spec = NativeWearFailureMap.Get(id);
                events.Add(new NativeWearFailureEvent(id, spec?.NativeName ?? $"ID {id}", spec?.DefectCode, spec?.Kind));
            }

            events.Sort((a, b) => a.ComponentId.CompareTo(b.ComponentId));
            return events;
        }
    }
}
