using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Item elegível para substituição individual na oficina do Hangar.
    /// </summary>
    public sealed record PartReplacementItem(
        AircraftPart Part,
        PartDefinition Def,
        double PartPrice,
        double LaborCost,
        double DowntimeHours,
        string Reason);

    /// <summary>
    /// Plano consolidado de substituição de peças para um componente ou serviço do Hangar.
    /// </summary>
    public sealed record PartReplacementPlan(
        IReadOnlyList<PartReplacementItem> Items,
        double TotalCost,
        double DowntimeHours);

    /// <summary>
    /// Planejador de substituição de peças físicas inteiras no Hangar a preço cheio (REPARO-PECAS).
    /// Na aviação real não se troca 5 % de freio: troca-se a peça física inteira (100 %) pagando preço integral + mão de obra.
    /// </summary>
    public static class PartReplacementPlanner
    {
        public const double ReplaceBelowHealthPct = 80.0;

        /// <summary>
        /// Gera o plano de substituição para peças com base no multiplicador econômico de classe da aeronave.
        /// Peça entra no plano se: IsBroken, Health &lt; 80, ou PartInspectionFlags.Evaluate(...).Level != None.
        /// </summary>
        public static PartReplacementPlan Plan(
            IEnumerable<AircraftPart> parts,
            PartCatalog catalog,
            double classMultiplier,
            DateTime careerNow,
            double laborRatePerHour = PartPricing.DefaultLaborRatePerHour,
            IRandomSource? random = null)
        {
            if (parts == null || catalog == null)
            {
                return new PartReplacementPlan(Array.Empty<PartReplacementItem>(), 0.0, 0.0);
            }

            var items = new List<PartReplacementItem>();

            foreach (var part in parts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null) continue;

                var eval = PartInspectionFlags.Evaluate(part, def, careerNow);

                bool needsReplacement = part.IsBroken
                    || part.Health < ReplaceBelowHealthPct
                    || eval.Level != PartFlag.None
                    || part.Flag != PartFlag.None;

                if (!needsReplacement) continue;

                string reason;
                if (part.IsBroken || eval.Level == PartFlag.Broken || part.Flag == PartFlag.Broken)
                {
                    reason = "Quebrada";
                }
                else if (eval.Level != PartFlag.None && eval.Reason != "Desgaste avançado" && !string.IsNullOrWhiteSpace(eval.Reason))
                {
                    reason = eval.Reason;
                }
                else if (part.Health < ReplaceBelowHealthPct)
                {
                    reason = $"Saúde {part.Health:F0} %";
                }
                else
                {
                    reason = !string.IsNullOrWhiteSpace(eval.Reason) ? eval.Reason : "Atenção recomendada";
                }

                double partPrice = PartPricing.CalculatePartPrice(def, classMultiplier);
                double laborCost = PartPricing.CalculateLaborCost(def, laborRatePerHour);
                double downtime = PartPricing.CalculateDowntimeHours(def, random);

                items.Add(new PartReplacementItem(part, def, partPrice, laborCost, downtime, reason));
            }

            double totalCost = Math.Round(items.Sum(i => i.PartPrice + i.LaborCost), 2);
            double maxDowntime = items.Count > 0 ? items.Max(i => i.DowntimeHours) : 0.0;

            return new PartReplacementPlan(items, totalCost, maxDowntime);
        }

        /// <summary>
        /// Gera o plano de substituição para peças com base na chave de classe econômica da aeronave (A a J).
        /// </summary>
        public static PartReplacementPlan Plan(
            IEnumerable<AircraftPart> parts,
            PartCatalog catalog,
            string? classKey,
            DateTime careerNow,
            double laborRatePerHour = PartPricing.DefaultLaborRatePerHour,
            IRandomSource? random = null)
        {
            double mult = !string.IsNullOrWhiteSpace(classKey)
                ? AircraftEconomyClass.GetPartPriceMultiplier(classKey)
                : 1.0;
            return Plan(parts, catalog, mult, careerNow, laborRatePerHour, random);
        }

        /// <summary>
        /// Aplica a substituição das peças de um plano: saúde 100 %, IsBroken = false, horas/ciclos zerados,
        /// LastReplaced* atualizados e marcas de suspeita limpas.
        /// </summary>
        public static void Apply(PartReplacementPlan plan, double airframeHours, DateTime careerNow)
        {
            if (plan == null || plan.Items == null) return;

            foreach (var item in plan.Items)
            {
                ApplyToPart(item.Part, airframeHours, careerNow);
            }
        }

        /// <summary>
        /// Aplica a substituição a uma lista de peças físicas.
        /// </summary>
        public static void ApplyToParts(IEnumerable<AircraftPart> parts, double airframeHours, DateTime careerNow)
        {
            if (parts == null) return;
            foreach (var p in parts)
            {
                ApplyToPart(p, airframeHours, careerNow);
            }
        }

        /// <summary>
        /// Aplica a substituição física a uma peça individual (restaura a nova de fábrica).
        /// </summary>
        public static void ApplyToPart(AircraftPart part, double airframeHours, DateTime careerNow)
        {
            if (part == null) return;

            part.Health = 100.0;
            part.IsBroken = false;
            part.BrokenAtUtc = null;
            part.HoursSinceReplacement = 0.0;
            part.CyclesSinceReplacement = 0;
            part.LastReplacedAirframeHours = airframeHours;
            part.LastReplacedAtUtc = careerNow;
            part.Flag = PartFlag.None;
            part.FlaggedBy = PartFlagSource.None;
            part.FlaggedAtUtc = null;
            part.FlaggedAirframeHours = null;
            part.LastShopServiceAtUtc = careerNow;
            part.LastShopServiceAirframeHours = airframeHours;
            part.LastShopServiceCycles = null;
        }
    }
}
