using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Ponte temporária entre os componentes (fonte da verdade) e as colunas antigas de PilotAircraft,
    /// que ainda são lidas e escritas pelo Hangar, mercado, HUD e FlightService (até a fase 3).
    ///
    /// Grupos (coluna antiga = menor saúde do grupo):
    ///   EngineCondition    ↔ Motor, Seção quente
    ///   OilCondition       ↔ Óleo
    ///   MagnetosCondition  ↔ Ignição
    ///   TiresCondition     ↔ Pneus, Freios
    ///   AirframeCondition  ↔ Célula, Trem de pouso
    ///   AvionicsCondition  ↔ Elétrico, Instrumentos
    /// </summary>
    public static class LegacyConditionBridge
    {
        private const double Tolerance = 0.5;

        private static readonly (ComponentKind[] Kinds, Func<PilotAircraft, double> Get, Action<PilotAircraft, double> Set)[] Groups =
        {
            (new[] { ComponentKind.Engine, ComponentKind.HotSection }, a => a.EngineCondition, (a, v) => a.EngineCondition = v),
            (new[] { ComponentKind.Oil }, a => a.OilCondition, (a, v) => a.OilCondition = v),
            (new[] { ComponentKind.Ignition }, a => a.MagnetosCondition, (a, v) => a.MagnetosCondition = v),
            (new[] { ComponentKind.Tires, ComponentKind.Brakes }, a => a.TiresCondition, (a, v) => a.TiresCondition = v),
            (new[] { ComponentKind.Airframe, ComponentKind.LandingGear }, a => a.AirframeCondition, (a, v) => a.AirframeCondition = v),
            (new[] { ComponentKind.Electrical, ComponentKind.Instruments }, a => a.AvionicsCondition, (a, v) => a.AvionicsCondition = v),
        };

        /// <summary>Copia a saúde dos componentes para as colunas antigas (menor saúde de cada grupo).</summary>
        public static void WriteToLegacy(PilotAircraft aircraft, IEnumerable<AircraftComponent> components)
        {
            var list = components.ToList();
            foreach (var (kinds, _, set) in Groups)
            {
                var members = list.Where(c => kinds.Contains(c.Kind)).ToList();
                if (members.Count > 0) set(aircraft, members.Min(c => c.Health));
            }
        }

        /// <summary>
        /// Aplica aos componentes as mudanças feitas fora do voo nas colunas antigas
        /// (reparos/inspeções no Hangar, perda total, crash no pouso):
        /// - Coluna acima do grupo: reparo → componentes abaixo do valor sobem até ele; em 100% o componente
        ///   é considerado revisado (horas e ciclos zerados) e a saúde passa a ser conhecida com exatidão.
        /// - Coluna abaixo do grupo: dano externo → componentes descem até o valor.
        /// Também registra inspeções completas quando as inspeções de 50 h/100 h acabaram de ser feitas.
        /// </summary>
        /// <returns>Quantidade de componentes alterados.</returns>
        public static int ReconcileFromLegacy(PilotAircraft aircraft, IList<AircraftComponent> components, IRandomSource random, VisibilitySettings visibility)
        {
            int changed = 0;
            foreach (var (kinds, get, _) in Groups)
            {
                var members = components.Where(c => kinds.Contains(c.Kind)).ToList();
                if (members.Count == 0) continue;

                double legacy = Math.Clamp(get(aircraft), 0.0, 100.0);
                double groupMin = members.Min(c => c.Health);

                if (legacy > groupMin + Tolerance)
                {
                    foreach (var c in members.Where(c => c.Health < legacy))
                    {
                        c.Health = legacy;
                        if (legacy >= 99.9)
                        {
                            c.HoursSinceOverhaul = 0;
                            c.CyclesSinceOverhaul = 0;
                        }
                        HealthVisibility.RecordFullInspection(c, aircraft.CurrentAirframeHours, random, visibility);
                        changed++;
                    }
                }
                else if (legacy < groupMin - Tolerance)
                {
                    foreach (var c in members.Where(c => c.Health > legacy))
                    {
                        c.Health = legacy;
                        changed++;
                    }
                }
            }

            // Inspeção de 100 h recém-feita: todos os componentes inspecionados. 50 h: motor e óleo.
            bool just100h = aircraft.HoursUntilNext100hInspection >= 99.9;
            bool just50h = aircraft.HoursUntilNext50hInspection >= 49.9;
            foreach (var c in components)
            {
                bool inspected = just100h || (just50h && c.Kind is ComponentKind.Oil or ComponentKind.Engine);
                if (inspected && c.LastInspectionAirframeHours < aircraft.CurrentAirframeHours - 0.01)
                    HealthVisibility.RecordFullInspection(c, aircraft.CurrentAirframeHours, random, visibility);
            }

            return changed;
        }
    }
}
