using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Serviços de manutenção sobre os componentes (Hangar): orçamento e reparo por componente,
    /// inspeções (que revelam a saúde exata) e walkaround.
    /// </summary>
    public static class ComponentMaintenance
    {
        /// <summary>Orçamento para levar o componente até targetHealth (padrão 100%).</summary>
        public static double RepairCost(ComponentDefinition def, AircraftComponent component, double aircraftPurchasePrice, double targetHealth = 100.0)
        {
            double deficit = Math.Max(0.0, Math.Min(100.0, targetHealth) - component.Health);
            if (deficit <= 0.05) return 0.0;
            double fullCost = Math.Max(def.MinFullRepairCost, aircraftPurchasePrice * def.RepairCostFactor);
            return Math.Round(fullCost * deficit / 100.0, 2);
        }

        /// <summary>
        /// Repara o componente. Em 100% conta como revisado (horas e ciclos zerados).
        /// O mecânico sempre conhece o resultado: a saúde passa a ser exata.
        /// </summary>
        public static void Repair(AircraftComponent component, double airframeHours, IRandomSource random, VisibilitySettings visibility, double targetHealth = 100.0)
        {
            component.Health = Math.Clamp(Math.Max(component.Health, targetHealth), 0.0, 100.0);
            if (component.Health >= 99.9)
            {
                component.Health = 100.0;
                component.HoursSinceOverhaul = 0;
                component.CyclesSinceOverhaul = 0;
            }
            HealthVisibility.RecordFullInspection(component, airframeHours, random, visibility);
        }

        /// <summary>Inspeção completa dos componentes informados (revela a saúde exata, sem reparar).</summary>
        public static void Inspect(IEnumerable<AircraftComponent> components, double airframeHours, IRandomSource random, VisibilitySettings visibility)
        {
            foreach (var c in components)
                HealthVisibility.RecordFullInspection(c, airframeHours, random, visibility);
        }

        /// <summary>Walkaround do pré-voo: estima os componentes visíveis por fora. Retorna quantos foram estreitados.</summary>
        public static int Walkaround(IEnumerable<AircraftComponent> components, ComponentCatalog catalog, double airframeHours, IRandomSource random)
        {
            int count = 0;
            foreach (var c in components)
            {
                var def = catalog.Components.FirstOrDefault(d => d.Kind == c.Kind);
                if (def != null && HealthVisibility.RecordWalkaround(c, def, airframeHours, random, catalog.Visibility)) count++;
            }
            return count;
        }

        /// <summary>Uso restante até a próxima revisão do componente (horas ou ciclos; negativo = vencido).</summary>
        public static (double Remaining, double Life, bool IsCycles) RemainingLife(ComponentDefinition def, AircraftComponent component, EngineClass engineClass)
        {
            double lifeCycles = def.LifeCyclesFor(engineClass);
            if (lifeCycles > 0) return (lifeCycles - component.CyclesSinceOverhaul, lifeCycles, true);
            double lifeHours = def.LifeHoursFor(engineClass);
            return (lifeHours - component.HoursSinceOverhaul, lifeHours, false);
        }
    }

    public enum AircraftStatusLevel
    {
        Ready = 0,          // Todos os componentes acima da faixa de sintomas
        MaintenanceDue = 1, // Inspeção ou revisão vencendo/vencida
        Attention = 2,      // Algum componente na faixa de sintomas (20–30%)
        FailureRisk = 3,    // Algum componente abaixo de 20%: risco de pane
        Interdicted = 4,    // Impedida de voar (inspeção obrigatória / TBO)
        Destroyed = 5       // Perda total
    }

    public sealed record AircraftStatus(AircraftStatusLevel Level, string Text, string Detail);

    /// <summary>
    /// Status da aeronave como o jogador o percebe: usa o centro da faixa exibida (saúde oculta),
    /// não o valor real, para não revelar o que ainda não foi inspecionado.
    /// </summary>
    public static class AircraftStatusEvaluator
    {
        public static AircraftStatus Evaluate(PilotAircraft aircraft, IReadOnlyList<AircraftComponent> components, ComponentCatalog catalog, EngineClass engineClass)
        {
            var engine = components.FirstOrDefault(c => c.Kind == ComponentKind.Engine);
            var airframe = components.FirstOrDefault(c => c.Kind == ComponentKind.Airframe);
            if (engine != null && airframe != null && engine.Health <= 0 && airframe.Health <= 0)
                return new AircraftStatus(AircraftStatusLevel.Destroyed, "PERDA TOTAL", "A aeronave foi destruída. Acione o seguro ou descarte-a.");

            if (aircraft.IsInterdicted)
                return new AircraftStatus(AircraftStatusLevel.Interdicted, "INTERDITADA", aircraft.InterdictionReason);

            var shown = components
                .Select(c => (Component: c, Def: catalog.Components.FirstOrDefault(d => d.Kind == c.Kind), Range: HealthVisibility.GetDisplayedRange(c, aircraft.CurrentAirframeHours, catalog.Visibility)))
                .Where(x => x.Def != null && x.Def.AppliesTo.Contains(engineClass))
                .OrderBy(x => x.Range.Mid)
                .ToList();

            var worst = shown.FirstOrDefault();
            if (worst.Def != null && worst.Range.Mid < catalog.Hazard.Threshold)
                return new AircraftStatus(AircraftStatusLevel.FailureRisk, "RISCO DE PANE", $"{worst.Def.Name} em {worst.Range}: chance de pane a cada hora de voo.");

            if (worst.Def != null && worst.Range.Mid < catalog.SymptomThreshold)
                return new AircraftStatus(AircraftStatusLevel.Attention, "ATENÇÃO", $"{worst.Def.Name} em {worst.Range}: componente perto do limite.");

            var due = new List<string>();
            if (aircraft.HoursUntilNext50hInspection <= 5) due.Add("inspeção de 50 h");
            if (aircraft.HoursUntilNext100hInspection <= 10) due.Add("inspeção de 100 h");
            if (engineClass == EngineClass.Piston && aircraft.HoursUntilNext500hInspection <= 10) due.Add("revisão de 500 h");
            if (aircraft.EngineTboRemainingHours <= 50) due.Add("TBO do motor");
            foreach (var x in shown)
            {
                var (remaining, life, isCycles) = ComponentMaintenance.RemainingLife(x.Def!, x.Component, engineClass);
                if (life > 0 && remaining <= 0) due.Add($"{x.Def!.Name.ToLowerInvariant()} (vida útil vencida)");
            }
            if (due.Count > 0)
                return new AircraftStatus(AircraftStatusLevel.MaintenanceDue, "MANUTENÇÃO PRÓXIMA", "Vencendo: " + string.Join(", ", due) + ".");

            return new AircraftStatus(AircraftStatusLevel.Ready, "PRONTA PARA VOO", "Todos os componentes dentro dos limites.");
        }
    }
}
