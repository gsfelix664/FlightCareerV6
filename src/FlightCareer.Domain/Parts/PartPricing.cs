using System;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;

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
    /// Precificação e tempo de manutenção de peças individuais (F2.3 e CALIBRA-F4b).
    /// Preço base multiplicado pelo multiplicador da classe econômica quando escala com a aeronave.
    /// </summary>
    public static class PartPricing
    {
        public const double DefaultLaborRatePerHour = 100.0;

        /// <summary>
        /// Retorna o teto de preço por peça física com base no porte da aeronave (F2.3 - Legado mantido para referência).
        /// </summary>
        [Obsolete("Legado F2.3: substituído por multiplicador por classe econômica em CALIBRA-F4b.")]
        public static double GetClassPartPriceCap(double aircraftValue)
        {
            if (aircraftValue <= 2_000_000.0) return 30_000.0;       // Pistão (Classes A, B, C, D, E)
            if (aircraftValue <= 12_000_000.0) return 80_000.0;      // Turboélice e Jato Executivo (Classes F, G, H)
            if (aircraftValue <= 150_000_000.0) return 55_000.0;     // Linha aérea Narrowbody (Classe I: A320, 737)
            return 110_000.0;                                        // Linha aérea Widebody / Cargueiro pesado (Classe J: A330, 747, 787)
        }

        /// <summary>
        /// Calcula o preço unitário da peça física (CALIBRA-F4b):
        /// - PartPrice fixo quando PriceScalesWithAircraft == false.
        /// - PartPrice × multiplicador da classe quando PriceScalesWithAircraft == true.
        /// </summary>
        public static double CalculatePartPrice(
            PartDefinition def,
            double classMultiplier = 1.0)
        {
            if (def == null) return 0.0;
            double basePrice = (double)def.PartPrice;

            if (!def.PriceScalesWithAircraft)
            {
                return Math.Round(basePrice, 2);
            }

            double mult = classMultiplier > 0 ? classMultiplier : 1.0;
            return Math.Round(basePrice * mult, 2);
        }

        /// <summary>
        /// Calcula o preço unitário da peça física resolvendo o multiplicador a partir da chave da classe (A a J).
        /// </summary>
        public static double CalculatePartPrice(
            PartDefinition def,
            string? classKey)
        {
            double mult = !string.IsNullOrWhiteSpace(classKey)
                ? AircraftEconomyClass.GetPartPriceMultiplier(classKey)
                : 1.0;
            return CalculatePartPrice(def, mult);
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
        /// Gera o detalhamento financeiro completo para substituição da peça a partir do multiplicador da classe.
        /// </summary>
        public static PartCostBreakdown CalculateBreakdown(
            PartDefinition def,
            double classMultiplier = 1.0,
            IRandomSource? random = null,
            double laborRatePerHour = DefaultLaborRatePerHour)
        {
            double partPrice = CalculatePartPrice(def, classMultiplier);
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

        /// <summary>
        /// Gera o detalhamento financeiro completo para substituição da peça a partir da chave da classe (A a J).
        /// </summary>
        public static PartCostBreakdown CalculateBreakdown(
            PartDefinition def,
            string? classKey,
            IRandomSource? random = null,
            double laborRatePerHour = DefaultLaborRatePerHour)
        {
            double mult = !string.IsNullOrWhiteSpace(classKey)
                ? AircraftEconomyClass.GetPartPriceMultiplier(classKey)
                : 1.0;
            return CalculateBreakdown(def, mult, random, laborRatePerHour);
        }
    }
}
