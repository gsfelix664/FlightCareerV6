using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Parts;
using Xunit;
using Xunit.Abstractions;

namespace FlightCareer.Tests.Parts
{
    public class PartEconomyBalanceTests
    {
        private static readonly PartCatalog Catalog = PartCatalog.LoadDefault();
        private readonly ITestOutputHelper _output;

        public PartEconomyBalanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static JsonDocument LoadEconomyTargets()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "economy_targets.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "economy_targets.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "economy_targets.json")
            };

            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException("economy_targets.json não encontrado.");

            return JsonDocument.Parse(File.ReadAllText(path));
        }

        [Fact]
        public void EconomySimulation_EachClass_FallsWithinTargetBand()
        {
            using var doc = LoadEconomyTargets();
            var classesEl = doc.RootElement.GetProperty("classes");

            var classConfigs = new (string ClassKey, string[] Features, int Engines, double Value)[]
            {
                ("A", new[] { "piston", "fixedPitchProp", "fixedGear", "tricycleGear", "carbureted", "naturallyAspirated", "unpressurized" }, 1, 45_000.0),
                ("B", new[] { "piston", "variablePitchProp", "retractableGear", "tricycleGear", "fuelInjected", "naturallyAspirated", "unpressurized" }, 1, 450_000.0),
                ("C", new[] { "piston", "variablePitchProp", "fixedGear", "tricycleGear", "fuelInjected", "turbocharged", "unpressurized" }, 1, 750_000.0),
                ("D", new[] { "piston", "diesel", "variablePitchProp", "fixedGear", "tricycleGear", "unpressurized" }, 1, 550_000.0),
                ("E", new[] { "piston", "variablePitchProp", "retractableGear", "tricycleGear", "fuelInjected", "naturallyAspirated", "unpressurized" }, 2, 1_200_000.0),
                ("F", new[] { "turboprop", "variablePitchProp", "retractableGear", "tricycleGear", "pressurized", "propDeIce" }, 1, 4_000_000.0),
                ("G", new[] { "turboprop", "variablePitchProp", "retractableGear", "tricycleGear", "pressurized", "propDeIce" }, 2, 7_500_000.0),
                ("H", new[] { "jet", "retractableGear", "tricycleGear", "pressurized" }, 2, 9_000_000.0),
                ("I", new[] { "jet", "retractableGear", "tricycleGear", "pressurized", "passengerCabin", "lavatory", "galley" }, 2, 110_000_000.0)
            };

            _output.WriteLine("| Classe | Exemplo | Custo/h Simulado | Faixa Alvo ($/h) | Status |");
            _output.WriteLine("|---|---|---|---|---|");

            foreach (var (classKey, features, engines, value) in classConfigs)
            {
                var target = classesEl.GetProperty(classKey);
                string className = target.GetProperty("className").GetString()!;
                string example = target.GetProperty("example").GetString()!;
                double minCost = target.GetProperty("minCostPerHour").GetDouble();
                double maxCost = target.GetProperty("maxCostPerHour").GetDouble();

                var applicableParts = Catalog.PartsFor(features);
                var simResult = PartEconomySimulator.Simulate(applicableParts, value, engines, 1000.0);

                string status = (simResult.CostPerHour >= minCost && simResult.CostPerHour <= maxCost) ? "OK" : "FORA";
                _output.WriteLine($"| {classKey} | {example} | ${simResult.CostPerHour:F2}/h | ${minCost:F0} - ${maxCost:F0}/h | {status} |");

                Assert.InRange(simResult.CostPerHour, minCost, maxCost);
            }
        }
    }
}
