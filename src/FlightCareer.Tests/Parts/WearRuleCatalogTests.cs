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

            // Também carrega context_registry.json para suportar sensores de contexto (ex: ctx-ground-velocity)
            string[] contextCandidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "context_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "context_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "context_registry.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "context_registry.json")
            };
            var ctxPath = contextCandidates.FirstOrDefault(File.Exists);
            if (ctxPath != null)
            {
                using var ctxDoc = JsonDocument.Parse(File.ReadAllText(ctxPath));
                foreach (var el in ctxDoc.RootElement.GetProperty("sensors").EnumerateArray())
                {
                    set.Add(el.GetProperty("key").GetString()!);
                }
            }

            foreach (var dk in PartFlightTracker.DerivedSensorKeys)
            {
                set.Add(dk);
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
            Assert.True(Catalog.Rules.Count >= 40, "Deve conter ao menos 40 regras após a expansão F2.1b (Rodada 2).");

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
        public void LimitKeys_VfeCurrent_IsValidAndHasKnotsUnit()
        {
            Assert.True(LimitKeys.IsValid(LimitKeys.VfeCurrent));
            Assert.Equal("knots", LimitKeys.UnitOf(LimitKeys.VfeCurrent));
        }

        [Fact]
        public void ContinuousRules_HaveCoherentLimitKeyAndSensors()
        {
            var continuousRules = Catalog.Rules.Where(r => r.Kind == WearRuleKind.Continuous).ToList();
            Assert.NotEmpty(continuousRules);

            foreach (var rule in continuousRules)
            {
                Assert.NotNull(rule.LimitKey);

                // Nenhuma regra contínua usa vne ou vno fora de velocidade/estrutura de célula
                if (rule.LimitKey == LimitKeys.Vne || rule.LimitKey == LimitKeys.Vno || rule.LimitKey == LimitKeys.Mmo)
                {
                    Assert.Contains("airspeed", rule.Id);
                    Assert.Contains(rule.Sensors, s => s == "af-airspeed-indicated");
                }

                // Coerência sensor x limite
                if (rule.LimitKey == LimitKeys.RpmRedline || rule.LimitKey == LimitKeys.RpmMaxRated)
                {
                    Assert.Contains(rule.Sensors, s => s.Contains("rpm"));
                }
                else if (rule.LimitKey == LimitKeys.ChtMax || rule.LimitKey == LimitKeys.ChtCoolingRateMax)
                {
                    Assert.Contains(rule.Sensors, s => s.Contains("cht"));
                }
                else if (rule.LimitKey == LimitKeys.OilTempMax)
                {
                    Assert.Contains(rule.Sensors, s => s.Contains("oil-temp"));
                }
                else if (rule.LimitKey == LimitKeys.OilPressMin)
                {
                    Assert.Contains(rule.Sensors, s => s.Contains("oil-press"));
                }
                else if (rule.LimitKey == LimitKeys.IttMax)
                {
                    Assert.Contains(rule.Sensors, s => s.Contains("itt"));
                }
                else if (rule.LimitKey == LimitKeys.TorqueMaxPct)
                {
                    Assert.Contains(rule.Sensors, s => s.Contains("torque"));
                }
                else if (rule.LimitKey == LimitKeys.N1MaxPct)
                {
                    Assert.Contains(rule.Sensors, s => s.Contains("n1"));
                }
                else if (rule.LimitKey == LimitKeys.VfeCurrent)
                {
                    Assert.Contains("af-airspeed-indicated", rule.Sensors);
                    Assert.Contains("af-flaps-handle-index", rule.Sensors);
                }
                else if (rule.LimitKey == LimitKeys.Vle)
                {
                    Assert.Contains("af-airspeed-indicated", rule.Sensors);
                    Assert.Contains(rule.Sensors, s => s.StartsWith("gear-"));
                }
                else if (rule.LimitKey == LimitKeys.Vlo)
                {
                    Assert.Contains("af-airspeed-indicated", rule.Sensors);
                    Assert.Contains(rule.Sensors, s => s == "gear-handle-position" || s == "gear-position");
                }
                else if (rule.LimitKey == LimitKeys.GLimitPos || rule.LimitKey == LimitKeys.GLimitNeg)
                {
                    Assert.Contains("af-g-force", rule.Sensors);
                }
                else if (rule.LimitKey == LimitKeys.Va)
                {
                    Assert.Contains("af-airspeed-indicated", rule.Sensors);
                }
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
            Assert.DoesNotContain(c152Rules, r => r.Id == "gear_extended_overspeed_vle");
            Assert.DoesNotContain(c152Rules, r => r.Id == "gear_operation_overspeed_vlo");

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
            // Deve conter pressurização e trem retrátil (tanto Vle estendido quanto Vlo em trânsito)
            Assert.Contains(tbmRules, r => r.Id == "pressurization_cycle_stress");
            Assert.Contains(tbmRules, r => r.Id == "gear_extended_overspeed_vle");
            Assert.Contains(tbmRules, r => r.Id == "gear_operation_overspeed_vlo");
            // NÃO pode conter CHT de pistão
            Assert.DoesNotContain(tbmRules, r => r.LimitKey == LimitKeys.ChtMax);
            // NÃO pode conter overspeed de RPM de pistão
            Assert.DoesNotContain(tbmRules, r => r.Id == "piston_rpm_overspeed");
        }

        [Fact]
        public void NewRules_F21b_AreValidAndCoverKeyMechanics()
        {
            // Regra de comando acima de Va
            var vaRule = Catalog.Rules.FirstOrDefault(r => r.Id == "control_surface_abrupt_above_va");
            Assert.NotNull(vaRule);
            Assert.Equal(WearRuleKind.Continuous, vaRule.Kind);
            Assert.Equal(LimitKeys.Va, vaRule.LimitKey);
            Assert.Contains("af-airspeed-indicated", vaRule.Sensors);
            Assert.Contains("af-elevator-position", vaRule.Sensors);

            // Regra de tailstrike no pouso
            var tailstrike = Catalog.Rules.FirstOrDefault(r => r.Id == "touchdown_tailstrike");
            Assert.NotNull(tailstrike);
            Assert.Equal(WearRuleKind.Event, tailstrike.Kind);
            Assert.Equal("touchdownPitchDegrees", tailstrike.Metric);
            Assert.Contains(tailstrike.Bands, b => b.Inspection != null && b.Inspection.DefectCode == "TAILSTRIKE_INSPECTION");

            // Regra de wingtip strike
            var wingtip = Catalog.Rules.FirstOrDefault(r => r.Id == "touchdown_wingtip_strike");
            Assert.NotNull(wingtip);
            Assert.Equal(WearRuleKind.Event, wingtip.Kind);
            Assert.Equal("touchdownBankDegrees", wingtip.Metric);
            Assert.Contains(wingtip.Bands, b => b.Inspection != null && b.Inspection.DefectCode == "WINGTIP_STRIKE_INSPECTION");

            // Regra de speedbrake em alta velocidade
            var speedbrake = Catalog.Rules.FirstOrDefault(r => r.Id == "speedbrake_airspeed_vno_overspeed");
            Assert.NotNull(speedbrake);
            Assert.Equal(WearRuleKind.Continuous, speedbrake.Kind);
            Assert.Equal(LimitKeys.Vno, speedbrake.LimitKey);

            // Regra de ciclos de trim
            var trim = Catalog.Rules.FirstOrDefault(r => r.Id == "trim_actuator_cycle_stress");
            Assert.NotNull(trim);
            Assert.Equal(WearRuleKind.Event, trim.Kind);
            Assert.Equal("trimCycles", trim.Metric);

            // Regra de frenagem de alta energia
            var brake = Catalog.Rules.FirstOrDefault(r => r.Id == "brake_high_energy_antiskid");
            Assert.NotNull(brake);
            Assert.Equal(WearRuleKind.Event, brake.Kind);
            Assert.Equal("brakeEnergyPctRef", brake.Metric);
        }

        [Fact]
        public void EventRules_AllMetricsBelongToClosedList_WearRuleMetrics()
        {
            var eventRules = Catalog.Rules.Where(r => r.Kind == WearRuleKind.Event).ToList();
            Assert.NotEmpty(eventRules);

            foreach (var rule in eventRules)
            {
                Assert.NotNull(rule.Metric);
                Assert.True(WearRuleMetrics.All.Contains(rule.Metric),
                    $"Regra de evento '{rule.Id}' possui métrica '{rule.Metric}' fora do vocabulário fechado WearRuleMetrics.All.");
            }
        }

        [Fact]
        public void PistonRpm_SubidaAPlenoNoArcoVerde_SemPenalidade()
        {
            // Decisão do usuário (2026-11-21): até a linha vermelha (arco verde do POH) não há desgaste acelerado;
            // atenção a partir da vermelha e alerta a +4 %
            var rule = Catalog.Find("piston_rpm_overspeed");
            Assert.NotNull(rule);
            Assert.Equal(1.00, rule!.Bands[0].Threshold, 6);
            Assert.Equal(1.04, rule.Bands[1].Threshold, 6);
        }

        [Fact]
        public void WingtipStrikeInspection_RecognizesIsGroundingFromJson()
        {
            var catalog = WearRuleCatalog.LoadDefault();
            var rule = catalog.Find("touchdown_wingtip_strike");

            Assert.NotNull(rule);
            var wingtipInspections = rule!.Bands
                .Select(b => b.Inspection)
                .Where(i => i != null && i.DefectCode == "WINGTIP_STRIKE_INSPECTION")
                .ToList();

            Assert.NotEmpty(wingtipInspections);
            Assert.Contains(wingtipInspections, i => i!.Grounding);
        }
    }
}
