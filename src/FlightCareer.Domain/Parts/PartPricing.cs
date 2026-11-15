using System;
using FlightCareer.Domain.Components;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Detalhamento financeiro da substituição/reparo de uma peça.
    /// </summary>
    public sealed record PartCostBreakdown(
        string PartId,
        double PartPrice,
        double LaborHours,
        double LaborCost,
        double TotalCost,
        double DowntimeHours);

    /// <summary>
    /// Precificação e tempo de manutenção de peças individuais (F2.3 - Decisão 4).
    /// </summary>
    public static class PartPricing
    {
        public const double DefaultLaborRatePerHour = 100.0;

        /// <summary>
        /// Calcula o preço unitário da peça física:
        /// - PartPrice fixo quando PriceScalesWithAircraft == false.
        /// - max(PartPrice, valor da aeronave × repairCostPctOfAircraft sorteado na faixa) quando escala.
        /// </summary>
        public static double CalculatePartPrice(PartDefinition def, double aircraftValue, IRandomSource? random = null)
        {
            if (def == null) return 0.0;
            double basePrice = (double)def.PartPrice;

            if (!def.PriceScalesWithAircraft)
            {
                return Math.Round(basePrice, 2);
            }

            if (def.RepairCostPctOfAircraft != null && def.RepairCostPctOfAircraft.Count >= 2 && aircraftValue > 0)
            {
                double minPct = def.RepairCostPctOfAircraft[0];
                double maxPct = def.RepairCostPctOfAircraft[1];
                double roll = random != null ? random.NextDouble() : 0.5;
                double pct = minPct + (roll * (maxPct - minPct));

                double scaledCost = aircraftValue * (pct / 100.0);
                return Math.Round(Math.Max(basePrice, scaledCost), 2);
            }

            return Math.Round(basePrice, 2);
        }

        /// <summary>
        /// Calcula o custo de mão de obra para a substituição da peça.
        /// </summary>
        public static double CalculateLaborCost(PartDefinition def, double laborRatePerHour = DefaultLaborRatePerHour)
        {
            if (def == null || def.LaborHours <= 0) return 0.0;
            return Math.Round(def.LaborHours * Math.Max(0.0, laborRatePerHour), 2);
        }

        /// <summary>
        /// Calcula as horas de indisponibilidade (downtime) na oficina para a troca da peça.
        /// </summary>
        public static double CalculateDowntimeHours(PartDefinition def, IRandomSource? random = null)
        {
            if (def == null) return 0.0;

            if (def.DowntimeHours != null && def.DowntimeHours.Count >= 2)
            {
                double minH = def.DowntimeHours[0];
                double maxH = def.DowntimeHours[1];
                double roll = random != null ? random.NextDouble() : 0.5;
                return Math.Round(minH + (roll * (maxH - minH)), 1);
            }

            if (def.DowntimeHours != null && def.DowntimeHours.Count == 1)
            {
                return Math.Round(def.DowntimeHours[0], 1);
            }

            return Math.Round(def.LaborHours, 1);
        }

        /// <summary>
        /// Gera o detalhamento financeiro completo para substituição da peça.
        /// </summary>
        public static PartCostBreakdown CalculateBreakdown(
            PartDefinition def,
            double aircraftValue,
            IRandomSource? random = null,
            double laborRatePerHour = DefaultLaborRatePerHour)
        {
            double partPrice = CalculatePartPrice(def, aircraftValue, random);
            double laborCost = CalculateLaborCost(def, laborRatePerHour);
            double total = Math.Round(partPrice + laborCost, 2);
            double downtime = CalculateDowntimeHours(def, random);

            return new PartCostBreakdown(
                def.Id,
                partPrice,
                def.LaborHours,
                laborCost,
                total,
                downtime);
        }
    }
}
