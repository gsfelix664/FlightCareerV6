using System;
using System.Collections.Generic;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Cria as barras de componente de uma aeronave a partir das colunas antigas de PilotAircraft
    /// (EngineCondition, OilCondition, MagnetosCondition, TiresCondition...), preservando o estado atual.
    /// Também usado para aeronaves novas (todas as colunas em 100%).
    /// </summary>
    public static class LegacyComponentMigrator
    {
        public static List<AircraftComponent> CreateComponents(PilotAircraft aircraft, EngineClass engineClass, ComponentCatalog catalog, IRandomSource random)
        {
            var result = new List<AircraftComponent>();

            foreach (var def in catalog.For(engineClass))
            {
                double health = LegacyHealth(aircraft, def.Kind);
                var component = new AircraftComponent
                {
                    Id = Guid.NewGuid(),
                    PilotAircraftId = aircraft.Id,
                    Kind = def.Kind,
                    Health = Math.Clamp(health, 0.0, 100.0)
                };

                double lifeHours = def.LifeHoursFor(engineClass);
                double lifeCycles = def.LifeCyclesFor(engineClass);

                double? knownHours = KnownHoursSinceOverhaul(aircraft, def.Kind, lifeHours);
                if (lifeHours > 0)
                {
                    component.HoursSinceOverhaul = knownHours ?? EstimateUsageFromHealth(component.Health, def.NaturalLossPerHour(engineClass), lifeHours);
                }
                if (lifeCycles > 0)
                {
                    component.CyclesSinceOverhaul = (int)Math.Round(EstimateUsageFromHealth(component.Health, def.NaturalLossPerCycle(engineClass), lifeCycles));
                }

                // Última inspeção conhecida: a de 50 h (componentes ficam com o valor exato naquele momento)
                double hoursSinceInspection = Math.Clamp(50.0 - aircraft.HoursUntilNext50hInspection, 0.0, 50.0);
                component.LastInspectionAirframeHours = Math.Max(0.0, aircraft.CurrentAirframeHours - hoursSinceInspection);
                component.InspectionPrecision = 0.0;
                component.InspectionBias = (random.NextDouble() * 2.0 - 1.0) * catalog.Visibility.MaxBias;

                result.Add(component);
            }

            return result;
        }

        private static double LegacyHealth(PilotAircraft a, ComponentKind kind) => kind switch
        {
            ComponentKind.Engine => a.EngineCondition,
            ComponentKind.HotSection => a.EngineCondition,
            ComponentKind.Oil => a.OilCondition,
            ComponentKind.Ignition => a.MagnetosCondition,
            ComponentKind.Electrical => a.AvionicsCondition,
            ComponentKind.LandingGear => a.AirframeCondition,
            ComponentKind.Tires => a.TiresCondition,
            ComponentKind.Brakes => a.TiresCondition, // Antes pneus e freios eram uma barra só
            ComponentKind.Airframe => a.AirframeCondition,
            _ => 100.0
        };

        private static double? KnownHoursSinceOverhaul(PilotAircraft a, ComponentKind kind, double lifeHours) => kind switch
        {
            ComponentKind.Engine or ComponentKind.HotSection when lifeHours > 0
                => Math.Max(0.0, lifeHours - a.EngineTboRemainingHours),
            ComponentKind.Oil => Math.Clamp(50.0 - a.HoursUntilNext50hInspection, 0.0, 50.0),
            ComponentKind.Ignition => Math.Clamp(500.0 - a.HoursUntilNext500hInspection, 0.0, 500.0),
            ComponentKind.Airframe => Math.Max(0.0, a.CurrentAirframeHours),
            _ => null
        };

        /// <summary>Uso equivalente (horas ou ciclos) que o desgaste natural levaria para chegar à saúde atual.</summary>
        private static double EstimateUsageFromHealth(double health, double lossPerUnit, double life)
        {
            if (lossPerUnit <= 0) return 0.0;
            return Math.Clamp((100.0 - health) / lossPerUnit, 0.0, life);
        }
    }
}
