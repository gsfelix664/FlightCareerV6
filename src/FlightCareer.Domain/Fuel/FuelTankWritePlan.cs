using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Fuel
{
    public record FuelTank(string SimVar, double UsableCapacityGallons, double CurrentGallons = 0.0);

    public record FuelTankAllocation(string SimVar, double Gallons);

    public class FuelTankWritePlanResult
    {
        public double TargetGallons { get; }
        public double TotalAllocatedGallons { get; }
        public double ExcessGallons { get; }
        public IReadOnlyList<FuelTankAllocation> Allocations { get; }

        public FuelTankWritePlanResult(
            double targetGallons,
            double totalAllocatedGallons,
            double excessGallons,
            IReadOnlyList<FuelTankAllocation> allocations)
        {
            TargetGallons = targetGallons;
            TotalAllocatedGallons = Math.Round(totalAllocatedGallons, 2);
            ExcessGallons = Math.Round(excessGallons, 2);
            Allocations = allocations;
        }
    }

    public static class FuelTankWritePlan
    {
        /// <summary>
        /// Distribui o combustível total desejado proporcionalmente à capacidade útil de cada tanque,
        /// sem exceder a capacidade individual. Arredonda a 0.01 galão.
        /// </summary>
        public static FuelTankWritePlanResult Distribute(double targetGallons, IReadOnlyList<FuelTank>? tanks)
        {
            if (targetGallons < 0) targetGallons = 0.0;

            if (tanks == null || tanks.Count == 0)
            {
                return new FuelTankWritePlanResult(targetGallons, 0.0, targetGallons, Array.Empty<FuelTankAllocation>());
            }

            double totalUsableCapacity = 0.0;
            var validTanks = new List<FuelTank>();

            foreach (var tank in tanks)
            {
                if (tank != null && tank.UsableCapacityGallons > 0 && !string.IsNullOrWhiteSpace(tank.SimVar))
                {
                    totalUsableCapacity += tank.UsableCapacityGallons;
                    validTanks.Add(tank);
                }
            }

            if (totalUsableCapacity <= 0 || validTanks.Count == 0)
            {
                var zeroAllocations = new List<FuelTankAllocation>();
                foreach (var t in tanks)
                {
                    if (t != null && !string.IsNullOrWhiteSpace(t.SimVar))
                    {
                        zeroAllocations.Add(new FuelTankAllocation(t.SimVar, 0.0));
                    }
                }
                return new FuelTankWritePlanResult(targetGallons, 0.0, targetGallons, zeroAllocations);
            }

            double fuelToDistribute = Math.Min(targetGallons, totalUsableCapacity);
            double excess = Math.Max(0.0, targetGallons - totalUsableCapacity);

            var allocations = new List<FuelTankAllocation>();
            double sumAllocated = 0.0;

            for (int i = 0; i < validTanks.Count; i++)
            {
                var tank = validTanks[i];
                double fraction = tank.UsableCapacityGallons / totalUsableCapacity;
                double allocated = Math.Round(fuelToDistribute * fraction, 2);

                // Garante que não ultrapassa a capacidade útil por arredondamento
                if (allocated > tank.UsableCapacityGallons)
                {
                    allocated = tank.UsableCapacityGallons;
                }

                allocations.Add(new FuelTankAllocation(tank.SimVar, allocated));
                sumAllocated += allocated;
            }

            // Ajuste fino de centésimos decorrente do arredondamento proporcional (se houver margem)
            double diff = Math.Round(fuelToDistribute - sumAllocated, 2);
            if (Math.Abs(diff) >= 0.01 && allocations.Count > 0)
            {
                // Tenta ajustar no tanque com mais folga ou maior capacidade
                for (int i = 0; i < allocations.Count; i++)
                {
                    var alloc = allocations[i];
                    var tank = validTanks[i];
                    double adjusted = Math.Round(alloc.Gallons + diff, 2);

                    if (adjusted >= 0 && adjusted <= tank.UsableCapacityGallons)
                    {
                        allocations[i] = new FuelTankAllocation(alloc.SimVar, adjusted);
                        sumAllocated += diff;
                        break;
                    }
                }
            }

            return new FuelTankWritePlanResult(targetGallons, sumAllocated, excess, allocations);
        }
    }
}
