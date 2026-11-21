using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    /// <summary>Faixa de saúde exibida ao jogador. Min == Max quando o valor é conhecido com exatidão.</summary>
    public readonly record struct HealthRange(double Min, double Max)
    {
        public bool IsExact => Max - Min < 0.05;
        public double Mid => (Min + Max) / 2.0;
        public override string ToString() => IsExact ? $"{Min:F0}%" : $"{Min:F0}–{Max:F0}%";
    }

    /// <summary>
    /// Saúde oculta: o valor real fica no banco e o jogador vê uma faixa que se abre com as horas
    /// desde a última inspeção. A faixa sempre contém o valor real (o centro é deslocado por um viés
    /// estável sorteado na inspeção, menor que a meia-largura).
    /// </summary>
    public static class HealthVisibility
    {
        public static double HalfWidth(AircraftComponent component, double currentAirframeHours, VisibilitySettings settings)
        {
            double hoursSince = Math.Max(0.0, currentAirframeHours - component.LastInspectionAirframeHours);
            return Math.Min(settings.MaxHalfWidth, component.InspectionPrecision + settings.GrowthPerHour * hoursSince);
        }

        public static HealthRange GetDisplayedRange(AircraftComponent component, double currentAirframeHours, VisibilitySettings settings)
        {
            double halfWidth = HalfWidth(component, currentAirframeHours, settings);
            if (halfWidth <= 0.0) return new HealthRange(component.Health, component.Health);

            double bias = Math.Clamp(component.InspectionBias, -settings.MaxBias, settings.MaxBias);
            double center = component.Health + bias * halfWidth;
            return new HealthRange(
                Math.Clamp(center - halfWidth, 0.0, 100.0),
                Math.Clamp(center + halfWidth, 0.0, 100.0));
        }

        /// <summary>Inspeção completa (50 h / 100 h / pré-compra): revela o valor exato.</summary>
        public static void RecordFullInspection(AircraftComponent component, double airframeHours, IRandomSource random, VisibilitySettings settings)
            => Record(component, airframeHours, 0.0, random, settings);

        /// <summary>
        /// Walkaround do pré-voo: estreita a faixa dos componentes visíveis por fora.
        /// Retorna false se o componente não é visível no walkaround ou se a faixa atual já é mais estreita.
        /// </summary>
        public static bool RecordWalkaround(AircraftComponent component, ComponentDefinition definition, double airframeHours, IRandomSource random, VisibilitySettings settings)
        {
            if (!definition.WalkaroundVisible) return false;
            if (HalfWidth(component, airframeHours, settings) <= settings.WalkaroundHalfWidth) return false;
            Record(component, airframeHours, settings.WalkaroundHalfWidth, random, settings);
            return true;
        }

        private static void Record(AircraftComponent component, double airframeHours, double precision, IRandomSource random, VisibilitySettings settings)
        {
            component.LastInspectionAirframeHours = airframeHours;
            component.InspectionPrecision = precision;
            component.InspectionBias = (random.NextDouble() * 2.0 - 1.0) * settings.MaxBias;
        }
    }

    /// <summary>
    /// Barras gerais do hangar:
    /// - Aeronavegabilidade = pior componente crítico (decide interdição e alertas).
    /// - Condição geral = média ponderada pelos pesos do catálogo (decide valor de mercado).
    /// Os valores "Displayed" usam as faixas ocultas que o jogador vê.
    /// </summary>
    public sealed record AircraftHealthSummary(
        double Airworthiness,
        double Condition,
        HealthRange DisplayedAirworthiness,
        HealthRange DisplayedCondition,
        ComponentKind? WorstCriticalComponent)
    {
        public static AircraftHealthSummary Compute(IEnumerable<AircraftComponent> components, ComponentCatalog catalog, EngineClass engineClass, double currentAirframeHours)
        {
            var items = components
                .Select(c => (Component: c, Def: catalog.Components.FirstOrDefault(d => d.Kind == c.Kind)))
                .Where(x => x.Def != null && x.Def.AppliesTo.Contains(engineClass))
                .Select(x => (x.Component, Def: x.Def!, Range: HealthVisibility.GetDisplayedRange(x.Component, currentAirframeHours, catalog.Visibility)))
                .ToList();

            if (items.Count == 0)
                return new AircraftHealthSummary(100, 100, new HealthRange(100, 100), new HealthRange(100, 100), null);

            var critical = items.Where(x => x.Def.Critical).ToList();
            if (critical.Count == 0) critical = items;

            var worst = critical.OrderBy(x => x.Component.Health).First();
            double airworthiness = worst.Component.Health;
            var displayedAirworthiness = new HealthRange(critical.Min(x => x.Range.Min), critical.Min(x => x.Range.Max));

            static double W(ComponentDefinition d) => d.Weight > 0 ? d.Weight : 1.0;
            double totalWeight = items.Sum(x => W(x.Def));

            double condition = items.Sum(x => x.Component.Health * W(x.Def)) / totalWeight;
            var displayedCondition = new HealthRange(
                items.Sum(x => x.Range.Min * W(x.Def)) / totalWeight,
                items.Sum(x => x.Range.Max * W(x.Def)) / totalWeight);

            return new AircraftHealthSummary(airworthiness, condition, displayedAirworthiness, displayedCondition, worst.Component.Kind);
        }
    }
}
