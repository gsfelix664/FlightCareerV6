using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Economy;

namespace FlightCareer.Domain.Parts
{
    public sealed record EconomySimulationResult(
        double SimulatedHours,
        double TotalCost,
        double CostPerHour,
        int TotalReplacements,
        IReadOnlyDictionary<string, double> CostByPartGroup);

    /// <summary>
    /// Simulador puro da economia de manutenção e peças (F2.3 e CALIBRA-F4b).
    /// Simula N horas de operação típica por aeronave/classe e calcula o custo de manutenção por hora.
    /// </summary>
    public static class PartEconomySimulator
    {
        public const double DefaultSimulatedHours = 1000.0;
        public const double DefaultReplacementThresholdHealth = 40.0;
        public const double TypicalStressMultiplier = 1.15; // Reflete transientes operacionais típicos (partidas, subidas, pousos)

        /// <summary>
        /// Simula o ciclo de desgaste e substituição de peças em N horas de operação típica usando o multiplicador da classe econômica.
        /// </summary>
        public static EconomySimulationResult Simulate(
            IEnumerable<PartDefinition> applicableParts,
            double classMultiplier = 1.0,
            int engineCount = 1,
            double simulatedHours = DefaultSimulatedHours,
            double replacementThresholdHealth = DefaultReplacementThresholdHealth,
            double laborRatePerHour = PartPricing.DefaultLaborRatePerHour)
        {
            if (applicableParts == null) throw new ArgumentNullException(nameof(applicableParts));
            if (simulatedHours <= 0) simulatedHours = DefaultSimulatedHours;

            double healthSpan = Math.Clamp(100.0 - replacementThresholdHealth, 10.0, 100.0);
            double totalCost = 0.0;
            int totalReplacements = 0;
            var costByGroup = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            int engines = Math.Max(1, engineCount);

            foreach (var def in applicableParts)
            {
                if (def.LifeHours <= 0) continue;

                int instances = def.PerEngine ? engines : 1;

                // Vida útil efetiva até atingir o limiar de troca sob perfil típico
                // A vida nominal (70% de perda) é LifeHours. Para perder healthSpan (ex: 60%), leva:
                double hoursPerLife = def.LifeHours * (healthSpan / 70.0) / TypicalStressMultiplier;
                if (hoursPerLife <= 0.1) hoursPerLife = 1.0;

                double replacementsCount = (simulatedHours / hoursPerLife) * instances;
                double costPerUnit = PartPricing.CalculatePartPrice(def, classMultiplier) +
                                     PartPricing.CalculateLaborCost(def, laborRatePerHour);

                double partTotalCost = replacementsCount * costPerUnit;
                totalCost += partTotalCost;
                totalReplacements += (int)Math.Ceiling(replacementsCount);

                string grp = def.Group ?? "Outros";
                costByGroup[grp] = costByGroup.GetValueOrDefault(grp, 0.0) + partTotalCost;
            }

            double costPerHour = Math.Round(totalCost / simulatedHours, 2);

            return new EconomySimulationResult(
                simulatedHours,
                Math.Round(totalCost, 2),
                costPerHour,
                totalReplacements,
                costByGroup);
        }

        /// <summary>
        /// Simula o ciclo de desgaste e substituição de peças resolvendo o multiplicador a partir da chave da classe (A a J).
        /// </summary>
        public static EconomySimulationResult Simulate(
            IEnumerable<PartDefinition> applicableParts,
            string? classKey,
            int engineCount = 1,
            double simulatedHours = DefaultSimulatedHours,
            double replacementThresholdHealth = DefaultReplacementThresholdHealth,
            double laborRatePerHour = PartPricing.DefaultLaborRatePerHour)
        {
            double mult = !string.IsNullOrWhiteSpace(classKey)
                ? AircraftEconomyClass.GetPartPriceMultiplier(classKey)
                : 1.0;
            return Simulate(applicableParts, mult, engineCount, simulatedHours, replacementThresholdHealth, laborRatePerHour);
        }
    }
}
