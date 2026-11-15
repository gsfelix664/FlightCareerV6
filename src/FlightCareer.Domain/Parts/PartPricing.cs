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
        /// Retorna o teto de preço por peça física com base no porte da aeronave (F2.3).
        /// Impede que peças de jatos comerciais atinjam valores astronômicos (ex: pneus a $100k+),
        /// mantendo a ordem de grandeza real de custo direto de manutenção (US$ 1.000 - 2.500/h para narrowbodies).
        /// </summary>
        public static double GetClassPartPriceCap(double aircraftValue)
        {
            if (aircraftValue <= 2_000_000.0) return 30_000.0;       // Pistão (Classes A, B, C, D, E)
            if (aircraftValue <= 12_000_000.0) return 80_000.0;      // Turboélice e Jato Executivo (Classes F, G, H)
            if (aircraftValue <= 150_000_000.0) return 55_000.0;     // Linha aérea Narrowbody (Classe I: A320, 737)
            return 110_000.0;                                        // Linha aérea Widebody / Cargueiro pesado (Classe J: A330, 747, 787)
        }

        /// <summary>
        /// Calcula o preço unitário da peça física:
        /// - PartPrice fixo quando PriceScalesWithAircraft == false.
        /// - max(PartPrice, min(valor da aeronave × repairCostPctOfAircraft sorteado na faixa, teto da classe)) quando escala.
        /// </summary>
        public static double CalculatePartPrice(
            PartDefinition def,
            double aircraftValue,
            IRandomSource? random = null,
            double? maxCap = null)
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
                double cap = maxCap ?? GetClassPartPriceCap(aircraftValue);
                double finalPrice = Math.Max(basePrice, Math.Min(scaledCost, cap));

                return Math.Round(finalPrice, 2);
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
            double laborRatePerHour = DefaultLaborRatePerHour,
            double? maxCap = null)
        {
            double partPrice = CalculatePartPrice(def, aircraftValue, random, maxCap);
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
