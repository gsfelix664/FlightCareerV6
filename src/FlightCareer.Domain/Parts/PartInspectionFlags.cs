using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public sealed record PartFlagResult(PartFlag Level, string Reason);

    /// <summary>
    /// Avaliação e sinalização de peças para exibição de alerta no Hangar após inspeção ou walkaround.
    /// Exclamação amarela (faixa amarela) ou vermelha (faixa vermelha ou quebrada), visível até o próximo voo.
    /// </summary>
    public static class PartInspectionFlags
    {
        public const double LowHealthPct = 40.0;
        public const double RedHealthPct = 20.0;
        public const double LifeWarningFraction = 0.9;

        /// <summary>
        /// Avalia a sinalização da peça exclusivamente pela sua faixa de saúde.
        /// </summary>
        public static PartFlagResult Evaluate(AircraftPart part, PartDefinition def, DateTime careerNow, PartBandSettings? bands = null)
        {
            if (part == null || def == null)
            {
                return new PartFlagResult(PartFlag.None, string.Empty);
            }

            bands ??= new PartBandSettings();

            if (part.IsBroken)
            {
                return new PartFlagResult(PartFlag.Broken, "Quebrada");
            }

            if (part.Health < bands.RedBelowPct)
            {
                return new PartFlagResult(PartFlag.Broken, $"Faixa vermelha: {part.Health:F0} %");
            }

            if (part.Health < bands.YellowBelowPct)
            {
                return new PartFlagResult(PartFlag.Suspect, $"Faixa amarela: {part.Health:F0} %");
            }

            return new PartFlagResult(PartFlag.None, string.Empty);
        }

        public static PartFlagResult Evaluate(AircraftPart part, PartDefinition def, DateTime careerNow)
            => Evaluate(part, def, careerNow, new PartBandSettings());

        /// <summary>
        /// Devolve a nota de vida/ciclos/calendário quando vencidos (como informação descritiva para a UI).
        /// </summary>
        public static string? LifeNote(AircraftPart part, PartDefinition def, DateTime careerNow)
        {
            if (part == null || def == null) return null;

            if (def.LifeHours > 0)
            {
                if (part.HoursSinceReplacement >= def.LifeHours)
                {
                    return $"Vida vencida: {part.HoursSinceReplacement:F0} / {def.LifeHours:F0} h";
                }
                if (part.HoursSinceReplacement >= def.LifeHours * LifeWarningFraction)
                {
                    return $"Vida: {part.HoursSinceReplacement:F0} / {def.LifeHours:F0} h";
                }
            }

            if (def.LifeCycles.HasValue && def.LifeCycles.Value > 0)
            {
                if (part.CyclesSinceReplacement >= def.LifeCycles.Value)
                {
                    return $"Ciclos vencidos: {part.CyclesSinceReplacement} / {def.LifeCycles.Value}";
                }
                if (part.CyclesSinceReplacement >= def.LifeCycles.Value * LifeWarningFraction)
                {
                    return $"Ciclos: {part.CyclesSinceReplacement} / {def.LifeCycles.Value}";
                }
            }

            if (def.CalendarLifeMonths.HasValue && def.CalendarLifeMonths.Value > 0)
            {
                double elapsedDays = Math.Max(0.0, (careerNow - part.LastReplacedAtUtc).TotalDays);
                double months = elapsedDays / 30.4375;
                if (months >= def.CalendarLifeMonths.Value)
                {
                    return $"Calendário vencido: {months:F0} / {def.CalendarLifeMonths.Value} meses";
                }
                if (months >= def.CalendarLifeMonths.Value * LifeWarningFraction)
                {
                    return $"Calendário: {months:F0} / {def.CalendarLifeMonths.Value} meses";
                }
            }

            return null;
        }

        /// <summary>
        /// Marca as peças no escopo; limpa a marca das peças do escopo que estão ok. Devolve quantas ficaram marcadas.
        /// </summary>
        public static int ApplyInspection(
            IEnumerable<AircraftPart> parts,
            PartCatalog catalog,
            Func<PartDefinition, bool> inScope,
            PartFlagSource source,
            double airframeHours,
            DateTime careerNow,
            PartBandSettings? bands = null)
        {
            if (parts == null || catalog == null || inScope == null) return 0;

            int flaggedCount = 0;
            foreach (var part in parts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null || !inScope(def)) continue;

                var result = Evaluate(part, def, careerNow, bands);
                part.Flag = result.Level;

                if (result.Level != PartFlag.None)
                {
                    part.FlaggedBy = source;
                    part.FlaggedAtUtc = careerNow;
                    part.FlaggedAirframeHours = airframeHours;
                    flaggedCount++;
                }
                else
                {
                    part.FlaggedBy = PartFlagSource.None;
                    part.FlaggedAtUtc = null;
                    part.FlaggedAirframeHours = null;
                }
            }

            return flaggedCount;
        }

        /// <summary>
        /// Limpa as sinalizações das peças pertencentes aos tipos de componente reparados.
        /// </summary>
        public static void ClearForKinds(
            IEnumerable<AircraftPart> parts,
            PartCatalog catalog,
            IReadOnlySet<ComponentKind> kinds)
        {
            if (parts == null || catalog == null || kinds == null || kinds.Count == 0) return;

            var mapping = PartComponentMapping.CreateDefault();
            foreach (var part in parts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null) continue;

                var kind = mapping.ResolveComponentKind(def.Group);
                if (kind.HasValue && kinds.Contains(kind.Value))
                {
                    part.Flag = PartFlag.None;
                    part.FlaggedBy = PartFlagSource.None;
                    part.FlaggedAtUtc = null;
                    part.FlaggedAirframeHours = null;
                }
            }
        }

        /// <summary>
        /// Indica se a sinalização da peça deve ser visível no Hangar (não expirou com voo posterior).
        /// </summary>
        public static bool IsVisible(AircraftPart part, double currentAirframeHours)
        {
            if (part == null) return false;
            if (part.Flag == PartFlag.None) return false;
            if (!part.FlaggedAirframeHours.HasValue) return false;
            return Math.Abs(part.FlaggedAirframeHours.Value - currentAirframeHours) < 0.01;
        }
    }
}
