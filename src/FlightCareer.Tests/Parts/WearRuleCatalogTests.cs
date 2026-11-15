using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class WearRuleCatalogTests
    {
        private static readonly PartCatalog PartsCatalog = PartCatalog.LoadDefault();
        private static readonly WearRuleCatalog Catalog = WearRuleCatalog.LoadDefault();

        private static HashSet<string> LoadValidSensors()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "sensor_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "sensor_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "sensor_registry.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "sensor_registry.json")
            };

            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException("sensor_registry.json não encontrado.");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var el in doc.RootElement.GetProperty("sensors").EnumerateArray())
            {
                set.Add(el.GetProperty("key").GetString()!);
            }
            return set;
        }

        [Fact]
        public void Catalog_IdsUniqueAndValid()
        {
            var validSensors = LoadValidSensors();

            // Valida sem lançar exceções (IDs únicos, LimitKeys, sensores, peças, faixas, não-linearidade, inspeções)
            Catalog.Validate(PartsCatalog, validSensors);

            Assert.NotEmpty(Catalog.Rules);
            Assert.True(Catalog.Rules.Count >= 15, "Deve conter ao menos 15 regras para cobrir os grupos mínimos.");

            var defectCodePattern = new Regex("^[A-Z0-9]+(_[A-Z0-9]+)*$", RegexOptions.Compiled);

            foreach (var rule in Catalog.Rules)
            {
                Assert.False(string.IsNullOrWhiteSpace(rule.Id));
                Assert.False(string.IsNullOrWhiteSpace(rule.Description));
                Assert.False(string.IsNullOrWhiteSpace(rule.Source));
                Assert.NotEmpty(rule.Sensors);
                Assert.NotEmpty(rule.Bands);

                // Validação de LimitKey em regras contínuas
                if (rule.Kind == WearRuleKind.Continuous)
                {
                    Assert.NotNull(rule.LimitKey);
                    Assert.True(LimitKeys.IsValid(rule.LimitKey), $"LimitKey '{rule.LimitKey}' inválido na regra '{rule.Id}'.");
                }
                else if (rule.Kind == WearRuleKind.Event)
                {
                    Assert.False(string.IsNullOrWhiteSpace(rule.Metric), $"Metric não pode ser nulo na regra de evento '{rule.Id}'.");
                }

                // NativeOverstress (se presente: componentId 0–49)
                if (rule.NativeOverstress != null)
                {
                    Assert.InRange(rule.NativeOverstress.ComponentId, 0, 49);
                    Assert.True(rule.NativeOverstress.Threshold > 0);
                }

                // DefectCode de inspeções (se presente)
                if (rule.Inspection != null)
                {
                    Assert.Matches(defectCodePattern, rule.Inspection.DefectCode);
                }

                // Validação das faixas e peças
                for (int i = 0; i < rule.Bands.Count; i++)
                {
                    var band = rule.Bands[i];
                    Assert.False(string.IsNullOrWhiteSpace(band.Name));
                    Assert.NotEmpty(band.Parts);

                    if (band.Inspection != null)
                    {
                        Assert.Matches(defectCodePattern, band.Inspection.DefectCode);
                    }

                    if (i > 0)
                    {
                        Assert.True(band.Threshold > rule.Bands[i - 1].Threshold,
                            $"Regra '{rule.Id}': faixas fora de ordem crescente ({rule.Bands[i - 1].Threshold} >= {band.Threshold}).");
                    }

                    foreach (var p in band.Parts)
                    {
                        var partDef = PartsCatalog.Find(p.PartId);
                        Assert.NotNull(partDef);
                    }
                }
            }
        }

        [Fact]
        public void Coverage_AllNoGoPartsCovered()
        {
            var noGoParts = PartsCatalog.Parts
                .Where(p => p.BlocksFlightWhenBroken)
                .Select(p => p.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var coveredParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in Catalog.Rules)
            {
                foreach (var band in rule.Bands)
                {
                    foreach (var p in band.Parts)
                    {
                        coveredParts.Add(p.PartId);
                    }
                }
            }

            foreach (var noGoId in noGoParts)
            {
                Assert.True(coveredParts.Contains(noGoId), $"Peça NO-GO '{noGoId}' não aparece em nenhuma regra de desgaste.");
            }
        }

        [Fact]
        public void Coverage_EngineOverspeedRuleForEveryPropulsionClass()
        {
            // Pistão
            var pistonRules = Catalog.RulesFor(new[] { AircraftFeatures.Piston, AircraftFeatures.SingleEngine });
            Assert.Contains(pistonRules, r => r.LimitKey == LimitKeys.RpmRedline && r.Kind == WearRuleKind.Continuous);

            // Turboélice
            var turbopropRules = Catalog.RulesFor(new[] { AircraftFeatures.Turboprop, AircraftFeatures.SingleEngine });
            Assert.Contains(turbopropRules, r => (r.LimitKey == LimitKeys.TorqueMaxPct || r.LimitKey == LimitKeys.IttMax || r.LimitKey == LimitKeys.N1MaxPct) && r.Kind == WearRuleKind.Continuous);

            // Jato
            var jetRules = Catalog.RulesFor(new[] { AircraftFeatures.Jet, AircraftFeatures.MultiEngine });
            Assert.Contains(jetRules, r => (r.LimitKey == LimitKeys.N1MaxPct || r.LimitKey == LimitKeys.IttMax) && r.Kind == WearRuleKind.Continuous);
        }

        [Fact]
        public void Filtering_RulesForAircraftMatchesExpectedRules()
        {
            // Cessna C152 features: piston, singleEngine, fixedGear, fixedPitchProp, carbureted
            var c152Features = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted
            };

            var c152Rules = Catalog.RulesFor(c152Features);

            // Deve conter overspeed de RPM de pistão
            Assert.Contains(c152Rules, r => r.Id == "piston_rpm_overspeed");
            // NÃO pode conter ITT de turbina
            Assert.DoesNotContain(c152Rules, r => r.LimitKey == LimitKeys.IttMax);
            // NÃO pode conter torque de turboélice
            Assert.DoesNotContain(c152Rules, r => r.LimitKey == LimitKeys.TorqueMaxPct);
            // NÃO pode conter trem retrátil
            Assert.DoesNotContain(c152Rules, r => r.Id == "gear_overspeed_vle_vlo");

            // TBM 930 features: turboprop, singleEngine, retractableGear, constantSpeedProp, pressurized, hydraulicGear
            var tbmFeatures = new[]
            {
                AircraftFeatures.Turboprop,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.RetractableGear,
                AircraftFeatures.ConstantSpeedProp,
                AircraftFeatures.Pressurized,
                AircraftFeatures.HydraulicGear
            };

            var tbmRules = Catalog.RulesFor(tbmFeatures);

            // Deve conter ITT e torque de turbina
            Assert.Contains(tbmRules, r => r.LimitKey == LimitKeys.IttMax);
            Assert.Contains(tbmRules, r => r.LimitKey == LimitKeys.TorqueMaxPct);
            // Deve conter pressurização e trem retrátil
            Assert.Contains(tbmRules, r => r.Id == "pressurization_cycle_stress");
            Assert.Contains(tbmRules, r => r.Id == "gear_overspeed_vle_vlo");
            // NÃO pode conter CHT de pistão
            Assert.DoesNotContain(tbmRules, r => r.LimitKey == LimitKeys.ChtMax);
            // NÃO pode conter overspeed de RPM de pistão
            Assert.DoesNotContain(tbmRules, r => r.Id == "piston_rpm_overspeed");
        }
    }
}
