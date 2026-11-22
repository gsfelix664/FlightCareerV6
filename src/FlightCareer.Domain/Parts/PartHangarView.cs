using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public enum PartKnownBand
    {
        Unknown,
        Green,
        Yellow,
        Red,
        Broken
    }

    public enum PartDisplayState
    {
        Green,
        Yellow,
        Red,
        Broken,
        Uncertain
    }

    public sealed record PartDisplayInfo(
        PartDisplayState State,
        PartKnownBand LastKnownBand,
        double? HoursSinceSeen);

    public sealed record PartHangarRow(
        AircraftPart Part,
        PartDefinition Def,
        string DisplayName,
        double? KnownHealth,
        string HealthText,
        string SeenText,
        PartKnownBand Band,
        string? LifeNote,
        PartDisplayInfo Display);

    public static class PartHangarView
    {
        public static string DisplayName(AircraftPart part, PartDefinition def, int engineCount)
        {
            if (part == null || def == null) return string.Empty;
            return def.PerEngine && engineCount > 1 && part.EngineIndex > 0
                ? $"{def.Name} (motor {part.EngineIndex})"
                : def.Name;
        }

        public static PartDisplayInfo DisplayState(
            AircraftPart part,
            double airframeHours,
            double growthPerHour,
            PartBandSettings? bands = null)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            bands ??= new PartBandSettings();

            if (part.IsBroken)
            {
                return new PartDisplayInfo(PartDisplayState.Broken, PartKnownBand.Broken,
                    part.KnownAtAirframeHours.HasValue ? Math.Max(0.0, airframeHours - part.KnownAtAirframeHours.Value) : null);
            }

            if (!part.KnownHealth.HasValue)
            {
                return new PartDisplayInfo(PartDisplayState.Uncertain, PartKnownBand.Unknown, null);
            }

            double known = part.KnownHealth.Value;
            double hoursSince = part.KnownAtAirframeHours.HasValue
                ? Math.Max(0.0, airframeHours - part.KnownAtAirframeHours.Value)
                : 0.0;

            PartKnownBand knownBand;
            if (known < bands.RedBelowPct)
            {
                knownBand = PartKnownBand.Red;
            }
            else if (known < bands.YellowBelowPct)
            {
                knownBand = PartKnownBand.Yellow;
            }
            else
            {
                knownBand = PartKnownBand.Green;
            }

            // Vermelho nunca vira incerto
            if (knownBand == PartKnownBand.Red)
            {
                return new PartDisplayInfo(PartDisplayState.Red, knownBand, hoursSince);
            }

            double lowerBound = knownBand == PartKnownBand.Green ? bands.YellowBelowPct : bands.RedBelowPct;
            double worstPossible = known - growthPerHour * hoursSince;

            if (worstPossible < lowerBound)
            {
                return new PartDisplayInfo(PartDisplayState.Uncertain, knownBand, hoursSince);
            }

            PartDisplayState state = knownBand == PartKnownBand.Green ? PartDisplayState.Green : PartDisplayState.Yellow;
            return new PartDisplayInfo(state, knownBand, hoursSince);
        }

        public static (PartDisplayState Worst, IReadOnlyDictionary<PartDisplayState, int> Counts)
            Summarize(IEnumerable<PartDisplayInfo> items)
        {
            var counts = new Dictionary<PartDisplayState, int>
            {
                [PartDisplayState.Green] = 0,
                [PartDisplayState.Yellow] = 0,
                [PartDisplayState.Red] = 0,
                [PartDisplayState.Broken] = 0,
                [PartDisplayState.Uncertain] = 0
            };

            var worst = PartDisplayState.Green;
            bool hasItems = false;

            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item == null) continue;
                    hasItems = true;
                    counts[item.State]++;

                    if (Priority(item.State) < Priority(worst))
                    {
                        worst = item.State;
                    }
                }
            }

            if (!hasItems)
            {
                worst = PartDisplayState.Uncertain;
            }

            return (worst, counts);

            static int Priority(PartDisplayState s) => s switch
            {
                PartDisplayState.Broken => 0,
                PartDisplayState.Red => 1,
                PartDisplayState.Yellow => 2,
                PartDisplayState.Uncertain => 3,
                PartDisplayState.Green => 4,
                _ => 5
            };
        }

        public static (string Text, bool IsOverdue) GroupLifeInfo(ComponentKind kind, PilotAircraft? aircraft, EngineClass engineClass)
        {
            if (aircraft == null) return (string.Empty, false);

            switch (kind)
            {
                case ComponentKind.Engine:
                {
                    double remaining = Math.Round(aircraft.EngineTboRemainingHours, 1);
                    double total = MaintenancePricing.TboHoursFor(engineClass);
                    if (remaining <= 0)
                    {
                        double overdue = Math.Abs(remaining);
                        return ($"TBO vencido há {overdue:F0} h", true);
                    }
                    return ($"TBO: {remaining:F0} h restantes (de {total:F0})", false);
                }
                case ComponentKind.Oil:
                {
                    double remaining = Math.Round(aircraft.HoursUntilNext50hInspection, 1);
                    double total = MaintenancePricing.OilChangeIntervalHours;
                    if (remaining <= 0)
                    {
                        double overdue = Math.Abs(remaining);
                        return ($"Troca de óleo vencida há {overdue:F0} h", true);
                    }
                    return ($"Troca de óleo em {remaining:F0} h (de {total:F0})", false);
                }
                case ComponentKind.Ignition:
                {
                    if (engineClass != EngineClass.Piston) return (string.Empty, false);
                    double remaining = Math.Round(aircraft.HoursUntilNext500hInspection, 1);
                    double total = MaintenancePricing.IgnitionRevisionIntervalHours;
                    if (remaining <= 0)
                    {
                        double overdue = Math.Abs(remaining);
                        return ($"Revisão de ignição vencida há {overdue:F0} h", true);
                    }
                    return ($"Revisão de ignição em {remaining:F0} h (de {total:F0})", false);
                }
                default:
                    return (string.Empty, false);
            }
        }

        public static string GroupLifeText(ComponentKind kind, PilotAircraft? aircraft, EngineClass engineClass)
        {
            return GroupLifeInfo(kind, aircraft, engineClass).Text;
        }

        public static PartHangarRow Build(
            AircraftPart part,
            PartDefinition def,
            int engineCount,
            double airframeHours,
            DateTime careerNow,
            PartBandSettings? bands = null,
            double growthPerHour = 0.4)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (def == null) throw new ArgumentNullException(nameof(def));

            bands ??= new PartBandSettings();

            string displayName = DisplayName(part, def, engineCount);
            double? knownHealth = part.KnownHealth;

            string healthText = knownHealth.HasValue
                ? $"{knownHealth.Value:F0} %"
                : "—";

            string seenText;
            if (!part.KnownAtAirframeHours.HasValue)
            {
                seenText = "nunca inspecionada";
            }
            else
            {
                double diff = Math.Max(0.0, airframeHours - part.KnownAtAirframeHours.Value);
                seenText = diff < 0.05
                    ? "visto agora"
                    : $"visto há {diff:F0} h";
            }

            PartKnownBand band;
            if (part.IsBroken)
            {
                band = PartKnownBand.Broken;
            }
            else if (!knownHealth.HasValue)
            {
                band = PartKnownBand.Unknown;
            }
            else if (knownHealth.Value < bands.RedBelowPct)
            {
                band = PartKnownBand.Red;
            }
            else if (knownHealth.Value < bands.YellowBelowPct)
            {
                band = PartKnownBand.Yellow;
            }
            else
            {
                band = PartKnownBand.Green;
            }

            string? lifeNote = PartInspectionFlags.LifeNote(part, def, careerNow);
            var display = DisplayState(part, airframeHours, growthPerHour, bands);

            return new PartHangarRow(
                part,
                def,
                displayName,
                knownHealth,
                healthText,
                seenText,
                band,
                lifeNote,
                display);
        }

        private static int BandPriority(PartKnownBand band) => band switch
        {
            PartKnownBand.Broken => 0,
            PartKnownBand.Red => 1,
            PartKnownBand.Yellow => 2,
            PartKnownBand.Unknown => 3,
            PartKnownBand.Green => 4,
            _ => 5
        };

        /// <summary>
        /// Ordem: Broken, Red, Yellow, Unknown, Green; dentro da faixa, menor KnownHealth primeiro; depois nome.
        /// </summary>
        public static IReadOnlyList<PartHangarRow> BuildList(
            IEnumerable<AircraftPart> parts,
            PartCatalog catalog,
            int engineCount,
            double airframeHours,
            DateTime careerNow,
            PartBandSettings? bands = null,
            double growthPerHour = 0.4)
        {
            if (parts == null || catalog == null) return Array.Empty<PartHangarRow>();
            bands ??= new PartBandSettings();

            var rows = new List<PartHangarRow>();
            foreach (var part in parts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null) continue;
                rows.Add(Build(part, def, engineCount, airframeHours, careerNow, bands, growthPerHour));
            }

            return rows
                .OrderBy(r => BandPriority(r.Band))
                .ThenBy(r => r.KnownHealth ?? double.MaxValue)
                .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
