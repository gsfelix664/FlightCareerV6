using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public sealed record HudGroupHealth(
        string GroupName,
        PartDisplayState State,
        string StatusText)
    {
        public static string GetStatusText(PartDisplayState state) => state switch
        {
            PartDisplayState.Green => "Em ordem",
            PartDisplayState.Worn => "Em uso",
            PartDisplayState.Yellow => "Atenção",
            PartDisplayState.Red => "Risco",
            PartDisplayState.Broken => "Quebrada",
            PartDisplayState.Uncertain => "Incerta",
            _ => "Em ordem"
        };
    }

    /// <summary>
    /// Resumo de saúde para o HUD em voo (overlay), baseado na saúde CONHECIDA das peças (ou faixa exibida dos componentes),
    /// sem vazar a saúde oculta real nem valores percentuais exatos.
    /// </summary>
    public sealed record HudHealthSummary(
        HudGroupHealth Airframe,
        HudGroupHealth Engine,
        HudGroupHealth Tires)
    {
        public static HudHealthSummary Build(
            IReadOnlyList<AircraftPart> parts,
            PartCatalog? partCatalog = null,
            double airframeHours = 0.0,
            double growthPerHour = 0.4,
            PartBandSettings? bands = null,
            PartComponentMapping? mapping = null)
        {
            bands ??= new PartBandSettings();
            mapping ??= PartComponentMapping.CreateDefault();
            partCatalog ??= PartCatalog.LoadDefault();

            var airframeDisplays = new List<PartDisplayInfo>();
            var engineDisplays = new List<PartDisplayInfo>();
            var tiresDisplays = new List<PartDisplayInfo>();

            if (parts != null)
            {
                foreach (var part in parts)
                {
                    if (part == null || string.IsNullOrWhiteSpace(part.PartId)) continue;
                    var def = partCatalog.Get(part.PartId);

                    ComponentKind? kind = def != null ? mapping.ResolveComponentKind(def.Group) : null;
                    if (!kind.HasValue)
                    {
                        // Fallback heurístico para testes unitários com IDs sintéticos
                        string id = part.PartId ?? string.Empty;
                        if (id.StartsWith("engine", StringComparison.OrdinalIgnoreCase) ||
                            id.StartsWith("propeller", StringComparison.OrdinalIgnoreCase) ||
                            id.StartsWith("oil", StringComparison.OrdinalIgnoreCase) ||
                            id.StartsWith("ignition", StringComparison.OrdinalIgnoreCase))
                        {
                            kind = ComponentKind.Engine;
                        }
                        else if (id.StartsWith("tire", StringComparison.OrdinalIgnoreCase) ||
                                 id.StartsWith("wheel", StringComparison.OrdinalIgnoreCase) ||
                                 id.StartsWith("brake", StringComparison.OrdinalIgnoreCase))
                        {
                            kind = ComponentKind.Tires;
                        }
                        else if (id.StartsWith("airframe", StringComparison.OrdinalIgnoreCase) ||
                                 id.StartsWith("fuselage", StringComparison.OrdinalIgnoreCase) ||
                                 id.StartsWith("wing", StringComparison.OrdinalIgnoreCase) ||
                                 id.StartsWith("gear", StringComparison.OrdinalIgnoreCase))
                        {
                            kind = ComponentKind.Airframe;
                        }
                    }

                    if (!kind.HasValue) continue;

                    var display = PartHangarView.DisplayState(part, airframeHours, growthPerHour, bands);

                    switch (kind.Value)
                    {
                        case ComponentKind.Airframe:
                        case ComponentKind.LandingGear:
                            airframeDisplays.Add(display);
                            break;

                        case ComponentKind.Engine:
                        case ComponentKind.HotSection:
                        case ComponentKind.Oil:
                        case ComponentKind.Ignition:
                            engineDisplays.Add(display);
                            break;

                        case ComponentKind.Tires:
                        case ComponentKind.Brakes:
                            tiresDisplays.Add(display);
                            break;
                    }
                }
            }

            var (airframeWorst, _) = PartHangarView.Summarize(airframeDisplays);
            var (engineWorst, _) = PartHangarView.Summarize(engineDisplays);
            var (tiresWorst, _) = PartHangarView.Summarize(tiresDisplays);

            return new HudHealthSummary(
                new HudGroupHealth("Célula", airframeWorst, HudGroupHealth.GetStatusText(airframeWorst)),
                new HudGroupHealth("Motor", engineWorst, HudGroupHealth.GetStatusText(engineWorst)),
                new HudGroupHealth("Pneus", tiresWorst, HudGroupHealth.GetStatusText(tiresWorst))
            );
        }

        public static HudHealthSummary BuildFromComponents(
            IReadOnlyList<AircraftComponent> components,
            double airframeHours,
            VisibilitySettings? visibility = null)
        {
            visibility ??= new VisibilitySettings();

            var airframeStates = new List<PartDisplayState>();
            var engineStates = new List<PartDisplayState>();
            var tiresStates = new List<PartDisplayState>();

            if (components != null)
            {
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    var range = HealthVisibility.GetDisplayedRange(comp, airframeHours, visibility);
                    var state = RangeToState(range);

                    switch (comp.Kind)
                    {
                        case ComponentKind.Airframe:
                        case ComponentKind.LandingGear:
                            airframeStates.Add(state);
                            break;

                        case ComponentKind.Engine:
                        case ComponentKind.HotSection:
                        case ComponentKind.Oil:
                        case ComponentKind.Ignition:
                            engineStates.Add(state);
                            break;

                        case ComponentKind.Tires:
                        case ComponentKind.Brakes:
                            tiresStates.Add(state);
                            break;
                    }
                }
            }

            var airframeWorst = WorstOf(airframeStates);
            var engineWorst = WorstOf(engineStates);
            var tiresWorst = WorstOf(tiresStates);

            return new HudHealthSummary(
                new HudGroupHealth("Célula", airframeWorst, HudGroupHealth.GetStatusText(airframeWorst)),
                new HudGroupHealth("Motor", engineWorst, HudGroupHealth.GetStatusText(engineWorst)),
                new HudGroupHealth("Pneus", tiresWorst, HudGroupHealth.GetStatusText(tiresWorst))
            );
        }

        private static PartDisplayState RangeToState(HealthRange range)
        {
            if (range.Mid < 20.0) return PartDisplayState.Red;
            if (range.Mid < 40.0) return PartDisplayState.Yellow;
            if (!range.IsExact && range.Min < 40.0) return PartDisplayState.Uncertain;
            if (range.Mid < 80.0) return PartDisplayState.Worn;
            return PartDisplayState.Green;
        }

        private static PartDisplayState WorstOf(IReadOnlyList<PartDisplayState> states)
        {
            if (states == null || states.Count == 0) return PartDisplayState.Uncertain;
            var worst = PartDisplayState.Green;
            foreach (var s in states)
            {
                if (Priority(s) < Priority(worst))
                {
                    worst = s;
                }
            }
            return worst;

            static int Priority(PartDisplayState s) => s switch
            {
                PartDisplayState.Broken => 0,
                PartDisplayState.Red => 1,
                PartDisplayState.Yellow => 2,
                PartDisplayState.Uncertain => 3,
                PartDisplayState.Worn => 4,
                PartDisplayState.Green => 5,
                _ => 6
            };
        }
    }
}
