using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Definição estática de uma peça no catálogo geral.
    /// </summary>
    public sealed class PartDefinition
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Group { get; init; } = string.Empty;
        public PartAppliesTo AppliesTo { get; init; } = new();
        public double LifeHours { get; init; }
        public int? LifeCycles { get; init; }
        public bool VisibleOnWalkaround { get; init; }
        public bool BlocksFlightWhenBroken { get; init; }
        public decimal PartPrice { get; init; }
        public double LaborHours { get; init; }
        public IReadOnlyList<string> Sensors { get; init; } = Array.Empty<string>();
        public string LifeSource { get; init; } = string.Empty;
        public bool PerEngine { get; init; }
        public bool PriceScalesWithAircraft { get; init; }
        public IReadOnlyList<double>? RepairCostPctOfAircraft { get; init; }
        public IReadOnlyList<double>? DowntimeHours { get; init; }
        public string? FailureEffect { get; init; }

        public PartDefinition() { }

        public PartDefinition(
            string id,
            string name,
            string group,
            PartAppliesTo appliesTo,
            double lifeHours,
            int? lifeCycles,
            bool visibleOnWalkaround,
            bool blocksFlightWhenBroken,
            decimal partPrice,
            double laborHours,
            IReadOnlyList<string> sensors,
            string lifeSource,
            bool perEngine = false,
            bool priceScalesWithAircraft = false,
            IReadOnlyList<double>? repairCostPctOfAircraft = null,
            IReadOnlyList<double>? downtimeHours = null,
            string? failureEffect = null)
        {
            Id = id;
            Name = name;
            Group = group;
            AppliesTo = appliesTo ?? new PartAppliesTo();
            LifeHours = lifeHours;
            LifeCycles = lifeCycles;
            VisibleOnWalkaround = visibleOnWalkaround;
            BlocksFlightWhenBroken = blocksFlightWhenBroken;
            PartPrice = partPrice;
            LaborHours = laborHours;
            Sensors = sensors ?? Array.Empty<string>();
            LifeSource = lifeSource;
            PerEngine = perEngine;
            PriceScalesWithAircraft = priceScalesWithAircraft;
            RepairCostPctOfAircraft = repairCostPctOfAircraft;
            DowntimeHours = downtimeHours;
            FailureEffect = failureEffect;
        }
    }
}
