using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Economy;
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

        private static string FindAircraftProfilesDirectory()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "aircraft"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "aircraft"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "aircraft")
            };

            return candidates.FirstOrDefault(Directory.Exists)
                ?? throw new DirectoryNotFoundException("Pasta 'Data/aircraft' não encontrada.");
        }

        private static string FindAircraftsCsvPath()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "CSV", "aircrafts.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "CSV", "aircrafts.csv"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "CSV", "aircrafts.csv")
            };

            return candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException("Arquivo 'aircrafts.csv' não encontrado.");
        }

        public static string ResolveAircraftClass(string modelName, string engineClass, int engineCount, IReadOnlyCollection<string> features, double price)
        {
            return AircraftEconomyClass.Resolve(modelName, engineClass, engineCount, features, price);
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
                ("I", new[] { "jet", "retractableGear", "tricycleGear", "pressurized", "passengerCabin", "lavatory", "galley" }, 2, 110_000_000.0),
                ("J", new[] { "antiskid", "apu", "autopilot", "bleedAir", "bleedAntiIce", "fadec", "fireProtection", "fms", "galley", "glassCockpit", "hydraulicGear", "iceDetector", "jet", "lavatory", "multiEngine", "noseWheelSteering", "oxygen", "passengerCabin", "pressurized", "rat", "retractableGear", "slats", "spoilers", "thsTrim", "weatherRadar", "wipers" }, 4, 418_400_000.0)
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
                var simResult = PartEconomySimulator.Simulate(applicableParts, classKey: classKey, engineCount: engines);

                string status = (simResult.CostPerHour >= minCost && simResult.CostPerHour <= maxCost) ? "OK" : "FORA";
                _output.WriteLine($"| {classKey} | {example} | ${simResult.CostPerHour:F2}/h | ${minCost:F0} - ${maxCost:F0}/h | {status} |");

                Assert.InRange(simResult.CostPerHour, minCost, maxCost);
            }
        }

        [Fact]
        public void EconomySimulation_All46CapturedAircraft_FallWithinTargetBands()
        {
            using var doc = LoadEconomyTargets();
            var classesEl = doc.RootElement.GetProperty("classes");

            string profilesDir = FindAircraftProfilesDirectory();
            string csvPath = FindAircraftsCsvPath();

            // 1. Carrega preços do CSV
            var pricesByModel = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(csvPath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(',');
                if (cols.Length >= 3 && double.TryParse(cols[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double p))
                {
                    pricesByModel[cols[0].Trim()] = p;
                }
            }

            // 2. Carrega todos os perfis JSON
            var profileFiles = Directory.GetFiles(profilesDir, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("_"))
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            _output.WriteLine($"\n### Tabela de Balanceamento Econômico de Todas as {profileFiles.Count} Aeronaves Capturadas\n");
            _output.WriteLine("| # | Modelo | Classe | Motores | Valor Novo ($) | Custo/h Simulado | Faixa Alvo ($/h) | Status |");
            _output.WriteLine("|---|---|---|---|---|---|---|---|");

            int idx = 1;
            foreach (var file in profileFiles)
            {
                using var pDoc = JsonDocument.Parse(File.ReadAllText(file));
                var root = pDoc.RootElement;

                string id = root.GetProperty("id").GetString()!;
                string cls = root.GetProperty("class").GetString()!;
                int engines = root.GetProperty("engineCount").GetInt32();
                var features = root.GetProperty("features").EnumerateArray().Select(e => e.GetString()!).ToList();

                // Obter preço novo do CSV (ou fallback)
                double price = pricesByModel.GetValueOrDefault(id, 1_000_000.0);
                if (price <= 0 && root.TryGetProperty("simMatch", out var simMatch) && simMatch.TryGetProperty("title", out var titles))
                {
                    foreach (var t in titles.EnumerateArray())
                    {
                        if (pricesByModel.TryGetValue(t.GetString()!, out price)) break;
                    }
                }
                if (price <= 0) price = 1_000_000.0;

                string classKey = ResolveAircraftClass(id, cls, engines, features, price);
                var target = classesEl.GetProperty(classKey);
                double minCost = target.GetProperty("minCostPerHour").GetDouble();
                double maxCost = target.GetProperty("maxCostPerHour").GetDouble();

                var applicableParts = Catalog.PartsFor(features);
                var simResult = PartEconomySimulator.Simulate(applicableParts, classKey: classKey, engineCount: engines);

                string status = (simResult.CostPerHour >= minCost && simResult.CostPerHour <= maxCost) ? "OK" : "FORA";
                _output.WriteLine($"| {idx++} | {id} | **{classKey}** | {engines}x | ${price:N0} | ${simResult.CostPerHour:F2}/h | ${minCost:F0} - ${maxCost:F0}/h | {status} |");

                Assert.InRange(simResult.CostPerHour, minCost, maxCost);
            }
        }
    }
}
