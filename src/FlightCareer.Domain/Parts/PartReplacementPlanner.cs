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
        /// <summary>
        /// Seleciona as peças recomendadas para substituição (quebradas e em faixa vermelha &lt; 20%).
        /// Usado pelo botão de reparar/reparar tudo.
        /// </summary>
        public static IReadOnlyList<AircraftPart> SelectRecommended(
            IEnumerable<AircraftPart> parts,
            PartCatalog catalog,
            PartBandSettings? bands = null)
        {
            if (parts == null) return Array.Empty<AircraftPart>();
            bands ??= new PartBandSettings();

            var result = new List<AircraftPart>();
            foreach (var part in parts)
            {
                if (part.IsBroken || part.Health < bands.RedBelowPct)
                {
                    result.Add(part);
                }
            }
            return result;
        }

        /// <summary>
        /// Precifica exatamente as peças recebidas com base no multiplicador econômico de classe da aeronave.
        /// </summary>
        public static PartReplacementPlan Plan(
            IEnumerable<AircraftPart> selectedParts,
            PartCatalog catalog,
            double classMultiplier,
            DateTime careerNow,
            double laborRatePerHour = PartPricing.DefaultLaborRatePerHour,
            IRandomSource? random = null,
            PartBandSettings? bands = null)
        {
            if (selectedParts == null || catalog == null)
            {
                return new PartReplacementPlan(Array.Empty<PartReplacementItem>(), 0.0, 0.0);
            }

            bands ??= new PartBandSettings();
            var items = new List<PartReplacementItem>();

            foreach (var part in selectedParts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null) continue;

                string reason;
                if (part.IsBroken)
                {
                    reason = "Quebrada";
                }
                else if (part.Health < bands.RedBelowPct)
                {
                    reason = $"Faixa vermelha: {part.Health:F0} %";
                }
                else if (part.Health < bands.YellowBelowPct)
                {
                    reason = $"Faixa amarela: {part.Health:F0} %";
                }
                else
                {
                    reason = $"Saúde {part.Health:F0} %";
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
        /// Gera o plano de substituição para as peças recebidas com base na chave de classe econômica da aeronave (A a J).
        /// </summary>
        public static PartReplacementPlan Plan(
            IEnumerable<AircraftPart> selectedParts,
            PartCatalog catalog,
            string? classKey,
            DateTime careerNow,
            double laborRatePerHour = PartPricing.DefaultLaborRatePerHour,
            IRandomSource? random = null,
            PartBandSettings? bands = null)
        {
            double mult = !string.IsNullOrWhiteSpace(classKey)
                ? AircraftEconomyClass.GetPartPriceMultiplier(classKey)
                : 1.0;
            return Plan(selectedParts, catalog, mult, careerNow, laborRatePerHour, random, bands);
        }

        /// <summary>
        /// Aplica a substituição das peças de um plano: saúde 100 %, IsBroken = false, horas/ciclos zerados,
        /// LastReplaced* atualizados e marcas de suspeita e achados limpos.
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

            part.FoundRedAtAnnualAirframeHours = null;
            part.KnownHealth = 100.0;
            part.KnownAtAirframeHours = airframeHours;
        }
    }
}
