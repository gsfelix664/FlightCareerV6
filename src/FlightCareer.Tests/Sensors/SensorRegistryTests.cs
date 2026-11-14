using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Sensors
{
    public class SensorRegistryTests
    {
        [Fact]
        public void Registry_RealFiles_Load_WithoutErrors()
        {
            var context = SensorRegistry.LoadDefault("context_registry.json");
            var sensors = SensorRegistry.LoadDefault("sensor_registry.json");

            Assert.NotNull(context);
            Assert.NotEmpty(context.Sensors);

            Assert.NotNull(sensors);
            Assert.NotEmpty(sensors.Sensors);
        }

        [Fact]
        public void Registry_Keys_AreUniqueAcrossBothFiles()
        {
            var context = SensorRegistry.LoadDefault("context_registry.json");
            var sensors = SensorRegistry.LoadDefault("sensor_registry.json");

            var allKeys = context.Sensors
                .Concat(sensors.Sensors)
                .Select(s => s.Key)
                .ToList();

            var duplicates = allKeys
                .GroupBy(k => k, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.True(duplicates.Count == 0, $"Chaves duplicadas entre context e sensors: {string.Join(", ", duplicates)}");
        }

        [Fact]
        public void Registry_EveryEntry_HasUnitAndGroup()
        {
            var context = SensorRegistry.LoadDefault("context_registry.json");
            var sensors = SensorRegistry.LoadDefault("sensor_registry.json");

            foreach (var s in context.Sensors.Concat(sensors.Sensors))
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Unit), $"Sensor '{s.Key}' possui unidade vazia.");
                Assert.False(string.IsNullOrWhiteSpace(s.Group), $"Sensor '{s.Key}' possui grupo vazio.");
                Assert.Equal(s.Unit.ToLowerInvariant(), s.Unit);
            }
        }

        [Fact]
        public void Registry_ContainsEverySimVarFromInventory()
        {
            var inventoryPath = FindInventoryPath();
            var expectedSimVars = ExtractSimVarsFromInventory(inventoryPath);

            Assert.NotEmpty(expectedSimVars);

            var context = SensorRegistry.LoadDefault("context_registry.json");
            var sensors = SensorRegistry.LoadDefault("sensor_registry.json");

            var registeredSimVars = context.Sensors
                .Concat(sensors.Sensors)
                .Select(s => s.SimVar)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missing = expectedSimVars.Except(registeredSimVars).ToList();

            Assert.True(missing.Count == 0, $"SimVars do inventário ausentes no registro: {string.Join(", ", missing)}");
        }

        [Fact]
        public void Registry_NoSimVarOutsideInventory()
        {
            var inventoryPath = FindInventoryPath();
            var expectedSimVars = ExtractSimVarsFromInventory(inventoryPath);

            var context = SensorRegistry.LoadDefault("context_registry.json");
            var sensors = SensorRegistry.LoadDefault("sensor_registry.json");

            var registeredSimVars = context.Sensors
                .Concat(sensors.Sensors)
                .Select(s => s.SimVar)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var extra = registeredSimVars.Except(expectedSimVars).ToList();

            Assert.True(extra.Count == 0, $"SimVars no registro que não constam no inventário: {string.Join(", ", extra)}");
        }

        [Fact]
        public void Validate_DuplicateKey_Throws()
        {
            const string json = """
            {
              "sensors": [
                { "key": "eng-rpm", "simVar": "GENERAL ENG RPM", "unit": "rpm", "group": "piston", "perEngine": true },
                { "key": "eng-rpm", "simVar": "PROP RPM", "unit": "rpm", "group": "piston", "perEngine": true }
              ]
            }
            """;

            var ex = Assert.Throws<InvalidDataException>(() => SensorRegistry.FromJson(json));
            Assert.Contains("duplicada", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_SimVarWithIndexSuffix_Throws()
        {
            const string json = """
            {
              "sensors": [
                { "key": "eng-rpm", "simVar": "GENERAL ENG RPM:1", "unit": "rpm", "group": "piston", "perEngine": true }
              ]
            }
            """;

            var ex = Assert.Throws<InvalidDataException>(() => SensorRegistry.FromJson(json));
            Assert.Contains(":", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_EmptyUnit_Throws()
        {
            const string json = """
            {
              "sensors": [
                { "key": "eng-rpm", "simVar": "GENERAL ENG RPM", "unit": "", "group": "piston", "perEngine": true }
              ]
            }
            """;

            var ex = Assert.Throws<InvalidDataException>(() => SensorRegistry.FromJson(json));
            Assert.Contains("Unidade", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_InvalidKeyFormat_Throws()
        {
            const string json = """
            {
              "sensors": [
                { "key": "eng_rpm_invalid", "simVar": "GENERAL ENG RPM", "unit": "rpm", "group": "piston", "perEngine": true }
              ]
            }
            """;

            var ex = Assert.Throws<InvalidDataException>(() => SensorRegistry.FromJson(json));
            Assert.Contains("kebab-case", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindInventoryPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "docs", "plans", "sensor-inventory.md");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException("docs/plans/sensor-inventory.md não encontrado subindo a partir de AppContext.BaseDirectory.");
        }

        private static HashSet<string> ExtractSimVarsFromInventory(string inventoryPath)
        {
            var lines = File.ReadAllLines(inventoryPath);
            var simVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int currentSection = 0;
            var sectionHeaderRegex = new Regex(@"^##\s+(\d+)\.", RegexOptions.Compiled);
            var backtickRegex = new Regex(@"`([^`]+)`", RegexOptions.Compiled);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                var secMatch = sectionHeaderRegex.Match(line);
                if (secMatch.Success)
                {
                    currentSection = int.Parse(secMatch.Groups[1].Value);
                    continue;
                }

                if (currentSection < 1 || currentSection > 11)
                    continue;

                if (!line.StartsWith('|') || line.StartsWith("|---") || line.StartsWith("| SimVar"))
                    continue;

                var columns = line.Split('|', StringSplitOptions.TrimEntries);
                if (columns.Length < 3)
                    continue;

                var simVarColumn = columns[1];
                var matches = backtickRegex.Matches(simVarColumn);
                foreach (Match match in matches)
                {
                    var rawSimVar = match.Groups[1].Value;
                    var clean = rawSimVar.Replace(":N", "", StringComparison.Ordinal)
                                         .Replace("✅", "", StringComparison.Ordinal)
                                         .Trim();
                    if (!string.IsNullOrEmpty(clean))
                    {
                        simVars.Add(clean);
                    }
                }
            }

            return simVars;
        }
    }
}
