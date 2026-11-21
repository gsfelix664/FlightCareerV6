using System;
using System.Collections.Generic;
using System.Linq;
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

    public sealed record PartHangarRow(
        AircraftPart Part,
        PartDefinition Def,
        string DisplayName,
        double? KnownHealth,
        string HealthText,
        string SeenText,
        PartKnownBand Band,
        string? LifeNote);

    public static class PartHangarView
    {
        public static string DisplayName(AircraftPart part, PartDefinition def, int engineCount)
        {
            if (part == null || def == null) return string.Empty;
            return def.PerEngine && engineCount > 1 && part.EngineIndex > 0
                ? $"{def.Name} (motor {part.EngineIndex})"
                : def.Name;
        }

        public static PartHangarRow Build(
            AircraftPart part,
            PartDefinition def,
            int engineCount,
            double airframeHours,
            DateTime careerNow,
            PartBandSettings? bands = null)
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

            return new PartHangarRow(
                part,
                def,
                displayName,
                knownHealth,
                healthText,
                seenText,
                band,
                lifeNote);
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
            PartBandSettings? bands = null)
        {
            if (parts == null || catalog == null) return Array.Empty<PartHangarRow>();
            bands ??= new PartBandSettings();

            var rows = new List<PartHangarRow>();
            foreach (var part in parts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null) continue;
                rows.Add(Build(part, def, engineCount, airframeHours, careerNow, bands));
            }

            return rows
                .OrderBy(r => BandPriority(r.Band))
                .ThenBy(r => r.KnownHealth ?? double.MaxValue)
                .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
