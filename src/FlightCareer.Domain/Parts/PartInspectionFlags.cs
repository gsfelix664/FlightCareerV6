using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public sealed record PartFlagResult(PartFlag Level, string Reason);

    /// <summary>
    /// Avaliação e sinalização de peças para exibição de alerta no Hangar após inspeção ou walkaround.
    /// Exclamação amarela (vida/desgaste avançado) ou vermelha (quebrada), visível até o próximo voo.
    /// </summary>
    public static class PartInspectionFlags
    {
        public const double LifeWarningFraction = 0.9;   // ≥ 90 % da vida (h, ciclos ou meses)
        public const double LowHealthPct = 40.0;          // saúde abaixo disso = amarelo

        public static PartFlagResult Evaluate(AircraftPart part, PartDefinition def, DateTime careerNow)
        {
            if (part == null || def == null)
            {
                return new PartFlagResult(PartFlag.None, string.Empty);
            }

            // Vermelho: IsBroken -> Broken, "Quebrada"
            if (part.IsBroken)
            {
                return new PartFlagResult(PartFlag.Broken, "Quebrada");
            }

            // Amarelo (Suspect), primeiro motivo que bater:
            // 1. Horas >= 90% de LifeHours
            if (def.LifeHours > 0 && part.HoursSinceReplacement >= def.LifeHours * LifeWarningFraction)
            {
                string reason = part.HoursSinceReplacement >= def.LifeHours
                    ? $"Vida vencida: {part.HoursSinceReplacement:F0} / {def.LifeHours:F0} h"
                    : $"Vida: {part.HoursSinceReplacement:F0} / {def.LifeHours:F0} h";
                return new PartFlagResult(PartFlag.Suspect, reason);
            }

            // 2. Ciclos >= 90% de LifeCycles
            if (def.LifeCycles.HasValue && def.LifeCycles.Value > 0 && part.CyclesSinceReplacement >= def.LifeCycles.Value * LifeWarningFraction)
            {
                string reason = part.CyclesSinceReplacement >= def.LifeCycles.Value
                    ? $"Ciclos vencidos: {part.CyclesSinceReplacement} / {def.LifeCycles.Value}"
                    : $"Ciclos: {part.CyclesSinceReplacement} / {def.LifeCycles.Value}";
                return new PartFlagResult(PartFlag.Suspect, reason);
            }

            // 3. Meses desde LastReplacedAtUtc >= 90% de CalendarLifeMonths (no relógio de carreira)
            if (def.CalendarLifeMonths.HasValue && def.CalendarLifeMonths.Value > 0)
            {
                double elapsedDays = Math.Max(0.0, (careerNow - part.LastReplacedAtUtc).TotalDays);
                double months = elapsedDays / 30.4375;
                if (months >= def.CalendarLifeMonths.Value * LifeWarningFraction)
                {
                    string reason = months >= def.CalendarLifeMonths.Value
                        ? $"Calendário vencido: {months:F0} / {def.CalendarLifeMonths.Value} meses"
                        : $"Calendário: {months:F0} / {def.CalendarLifeMonths.Value} meses";
                    return new PartFlagResult(PartFlag.Suspect, reason);
                }
            }

            // 4. Saúde abaixo de 40%
            if (part.Health < LowHealthPct)
            {
                return new PartFlagResult(PartFlag.Suspect, "Desgaste avançado");
            }

            return new PartFlagResult(PartFlag.None, string.Empty);
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
            DateTime careerNow)
        {
            if (parts == null || catalog == null || inScope == null) return 0;

            int flaggedCount = 0;
            foreach (var part in parts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null || !inScope(def)) continue;

                var result = Evaluate(part, def, careerNow);
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
