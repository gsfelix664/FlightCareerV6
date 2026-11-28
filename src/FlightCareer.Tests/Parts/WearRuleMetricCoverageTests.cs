using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class WearRuleMetricCoverageTests
    {
        private static readonly WearRuleCatalog RulesCatalog = WearRuleCatalog.LoadDefault();

        private static HashSet<string> LoadKnownSensors()
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

        private static SensorFrame CreateBaseFrame(Dictionary<string, double>? overrides = null)
        {
            var values = new Dictionary<string, double>
            {
                ["ctx-sim-on-ground"] = 0.0,
                ["sim-on-ground"] = 0.0,
                ["eng-combustion:1"] = 1.0,
                ["eng-starter:1"] = 0.0,
                ["eng-starter-active:1"] = 0.0,
                ["fuel-total-quantity"] = 20.0,
                ["fuel-pump-on"] = 0.0,
                ["hyd-pressure"] = 0.0,
                ["inst-structural-ice-pct"] = 0.0,
                ["inst-pitot-ice-pct"] = 0.0,
                ["inst-structural-deice-switch"] = 0.0,
                ["inst-prop-deice-switch"] = 0.0,
                ["af-pressurization-pressure-differential"] = 0.0,
                ["af-pressurization-cabin-altitude"] = 2000.0
            };

            if (overrides != null)
            {
                foreach (var kvp in overrides)
                {
                    values[kvp.Key] = kvp.Value;
                }
            }

            return new SensorFrame(values, DateTime.UtcNow, 1.0);
        }

        // =========================================================================
        // 1. GARANTIA DE COBERTURA ESTÁTICA DAS REGRAS
        // =========================================================================

        [Fact]
        public void EveryEventRule_HasProducedMetric()
        {
            var eventRules = RulesCatalog.Rules.Where(r => r.Kind == WearRuleKind.Event).ToList();
            Assert.NotEmpty(eventRules);

            var unproduced = new List<string>();
            foreach (var rule in eventRules)
            {
                Assert.False(string.IsNullOrWhiteSpace(rule.Metric), $"Regra '{rule.Id}' é do tipo Event mas não possui Metric definida.");

                if (!PartFlightTracker.ProducedMetrics.Contains(rule.Metric))
                {
                    unproduced.Add($"Regra '{rule.Id}' usa a métrica '{rule.Metric}' sem produtor registrado em PartFlightTracker.ProducedMetrics.");
                }
            }

            Assert.Empty(unproduced);
        }

        [Fact]
        public void AllWearRuleMetricsConstants_AreProducedAndNoneOrphan()
        {
            // Todas as 29 constantes definidas no vocabulário fechado WearRuleMetrics.All
            // devem pertencer ao conjunto PartFlightTracker.ProducedMetrics.
            foreach (var metric in WearRuleMetrics.All)
            {
                Assert.True(PartFlightTracker.ProducedMetrics.Contains(metric),
                    $"Métrica '{metric}' está em WearRuleMetrics.All mas não é produzida pelo código.");
            }

            // E o inverso: nada é produzido que não esteja no vocabulário oficial
            foreach (var produced in PartFlightTracker.ProducedMetrics)
            {
                Assert.True(WearRuleMetrics.All.Contains(produced),
                    $"Métrica produzida '{produced}' não está em WearRuleMetrics.All.");
            }
        }

        [Fact]
        public void EveryContinuousRule_HasKnownSensorsAndValidLimitKey()
        {
            var knownSensors = LoadKnownSensors();
            var continuousRules = RulesCatalog.Rules.Where(r => r.Kind == WearRuleKind.Continuous).ToList();
            Assert.NotEmpty(continuousRules);

            foreach (var rule in continuousRules)
            {
                Assert.False(string.IsNullOrWhiteSpace(rule.LimitKey), $"Regra contínua '{rule.Id}' sem LimitKey.");
                Assert.True(LimitKeys.IsValid(rule.LimitKey), $"Regra contínua '{rule.Id}' com LimitKey inválido '{rule.LimitKey}'.");

                foreach (var sensor in rule.Sensors)
                {
                    string baseSensor = sensor.Split(':')[0];
                    Assert.True(knownSensors.Contains(sensor) || knownSensors.Contains(baseSensor),
                        $"Regra '{rule.Id}' referencia sensor '{sensor}' não registrado em sensor_registry.json nem context_registry.json.");
                }
            }
        }

        // =========================================================================
        // 2. TESTES DAS 8 MÉTRICAS DE SISTEMAS DA METRICAS-B
        // =========================================================================

        [Fact]
        public void IcingExposureMinutes_ThresholdsAndReset()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            // No solo com gelo 20%: NÃO deve disparar
            var groundFrame = CreateBaseFrame(new()
            {
                ["ctx-sim-on-ground"] = 1.0,
                ["inst-structural-ice-pct"] = 20.0
            });
            var groundEvents = deriver.Update(groundFrame, 60.0, features);
            Assert.DoesNotContain(groundEvents, e => e.MetricKey == WearRuleMetrics.IcingExposureMinutes);

            // Sensor parado em 0 no ar: NÃO dispara
            deriver.Reset();
            var zeroFrame = CreateBaseFrame(new()
            {
                ["ctx-sim-on-ground"] = 0.0,
                ["inst-structural-ice-pct"] = 0.0,
                ["inst-pitot-ice-pct"] = 0.0
            });
            var zeroEvents = deriver.Update(zeroFrame, 600.0, features);
            Assert.DoesNotContain(zeroEvents, e => e.MetricKey == WearRuleMetrics.IcingExposureMinutes);

            // No ar com gelo estrutural 6% (>= 5%): acumula e cruza 5 min, 15 min, 30 min
            deriver.Reset();
            var icingFrame = CreateBaseFrame(new()
            {
                ["ctx-sim-on-ground"] = 0.0,
                ["inst-structural-ice-pct"] = 6.0
            });

            var allEvents = new List<DerivedPartEvent>();
            // 4 minutos: ainda não atinge 5 min
            allEvents.AddRange(deriver.Update(icingFrame, 240.0, features));
            Assert.DoesNotContain(allEvents, e => e.MetricKey == WearRuleMetrics.IcingExposureMinutes);

            // Mais 2 minutos (total 6 min): cruza 5 min
            allEvents.AddRange(deriver.Update(icingFrame, 120.0, features));
            var ev5 = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.IcingExposureMinutes);
            Assert.Equal(5.0, ev5.Value);

            // Mais 10 minutos (total 16 min): cruza 15 min
            allEvents.AddRange(deriver.Update(icingFrame, 600.0, features));
            var ev15 = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.IcingExposureMinutes && e.Value == 15.0);
            Assert.NotNull(ev15);

            // Mais 15 minutos (total 31 min): cruza 30 min
            allEvents.AddRange(deriver.Update(icingFrame, 900.0, features));
            var ev30 = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.IcingExposureMinutes && e.Value == 30.0);
            Assert.NotNull(ev30);

            // Sai da condição por 10 min (600 s) sem gelo: reseta o acumulador
            var cleanFrame = CreateBaseFrame(new()
            {
                ["ctx-sim-on-ground"] = 0.0,
                ["inst-structural-ice-pct"] = 0.0,
                ["inst-pitot-ice-pct"] = 0.0
            });
            deriver.Update(cleanFrame, 605.0, features);

            // Volta a ter gelo: deve poder cruzar o limiar de 5 min novamente após novo acúmulo
            var newIcingEvents = deriver.Update(icingFrame, 310.0, features);
            Assert.Contains(newIcingEvents, e => e.MetricKey == WearRuleMetrics.IcingExposureMinutes && e.Value == 5.0);
        }

        [Fact]
        public void DeiceCycles_CountAndThresholds()
        {
            var deriver = new PartEventDeriver();

            // Sem a feature 'deIceBoots', transições de chave NÃO devem disparar nada
            var noBootsFeatures = new[] { AircraftFeatures.Piston };
            for (int i = 0; i < 5; i++)
            {
                deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 0.0 }), 1.0, noBootsFeatures);
                var evs = deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 1.0 }), 1.0, noBootsFeatures);
                Assert.DoesNotContain(evs, e => e.MetricKey == WearRuleMetrics.DeiceCycles);
            }

            // Com a feature 'deIceBoots': 2, 6, 12 ciclos
            deriver.Reset();
            var bootsFeatures = new[] { AircraftFeatures.Turboprop, AircraftFeatures.DeIceBoots };
            var allEvents = new List<DerivedPartEvent>();

            // Ciclo 1 (desligado -> ligado)
            deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 0.0 }), 1.0, bootsFeatures);
            allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 1.0 }), 1.0, bootsFeatures));
            Assert.DoesNotContain(allEvents, e => e.MetricKey == WearRuleMetrics.DeiceCycles);

            // Ciclo 2 -> cruza 2.0
            deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 0.0 }), 1.0, bootsFeatures);
            allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 1.0 }), 1.0, bootsFeatures));
            var ev2 = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.DeiceCycles);
            Assert.Equal(2.0, ev2.Value);

            // Fazer mais 4 ciclos (total 6) -> cruza 6.0
            for (int i = 3; i <= 6; i++)
            {
                deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 0.0 }), 1.0, bootsFeatures);
                allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 1.0 }), 1.0, bootsFeatures));
            }
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.DeiceCycles && e.Value == 6.0);

            // Fazer mais 6 ciclos (total 12) -> cruza 12.0
            for (int i = 7; i <= 12; i++)
            {
                deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 0.0 }), 1.0, bootsFeatures);
                allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["inst-structural-deice-switch"] = 1.0 }), 1.0, bootsFeatures));
            }
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.DeiceCycles && e.Value == 12.0);
        }

        [Fact]
        public void PropDeiceCycles_CountAndThresholds()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Turboprop };
            var allEvents = new List<DerivedPartEvent>();

            // Sensor parado em 0 não gera ciclos
            for (int i = 0; i < 5; i++)
            {
                allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["inst-prop-deice-switch"] = 0.0 }), 1.0, features));
            }
            Assert.Empty(allEvents);

            // Transições 0 -> 1: emite em 2, 6, 12
            for (int i = 1; i <= 2; i++)
            {
                deriver.Update(CreateBaseFrame(new() { ["inst-prop-deice-switch"] = 0.0 }), 1.0, features);
                allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["inst-prop-deice-switch"] = 1.0 }), 1.0, features));
            }
            Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.PropDeiceCycles && e.Value == 2.0);

            for (int i = 3; i <= 6; i++)
            {
                deriver.Update(CreateBaseFrame(new() { ["inst-prop-deice-switch"] = 0.0 }), 1.0, features);
                allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["inst-prop-deice-switch"] = 1.0 }), 1.0, features));
            }
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.PropDeiceCycles && e.Value == 6.0);
        }

        [Fact]
        public void GlowPlugCycles_CountAndDieselFeatureRequired()
        {
            var deriver = new PartEventDeriver();

            // Aeronave não diesel (ex: C152): partidas NÃO geram glowPlugCycles
            var gasolineFeatures = new[] { AircraftFeatures.Piston };
            deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 0.0, ["eng-starter:1"] = 1.0 }), 1.0, gasolineFeatures);
            var gasEvents = deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 1.0, ["eng-starter:1"] = 1.0 }), 1.0, gasolineFeatures);
            Assert.DoesNotContain(gasEvents, e => e.MetricKey == WearRuleMetrics.GlowPlugCycles);

            // Aeronave diesel (ex: DA40NG): partidas geram glowPlugCycles em 1, 3, 5
            deriver.Reset();
            var dieselFeatures = new[] { AircraftFeatures.Piston, AircraftFeatures.Diesel };
            var allEvents = new List<DerivedPartEvent>();

            // Partida 1
            deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 0.0, ["eng-starter:1"] = 1.0 }), 1.0, dieselFeatures);
            allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 1.0, ["eng-starter:1"] = 1.0 }), 1.0, dieselFeatures));
            Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.GlowPlugCycles && e.Value == 1.0);

            // Cortar motor e dar partida 2
            deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 0.0, ["eng-starter:1"] = 0.0 }), 1.0, dieselFeatures);
            deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 0.0, ["eng-starter:1"] = 1.0 }), 1.0, dieselFeatures);
            allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 1.0, ["eng-starter:1"] = 1.0 }), 1.0, dieselFeatures));
            // Não emite no ciclo 2 (faixas são 1, 3, 5)
            Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.GlowPlugCycles);

            // Partida 3 -> cruza 3.0
            deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 0.0, ["eng-starter:1"] = 0.0 }), 1.0, dieselFeatures);
            deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 0.0, ["eng-starter:1"] = 1.0 }), 1.0, dieselFeatures);
            allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["eng-combustion:1"] = 1.0, ["eng-starter:1"] = 1.0 }), 1.0, dieselFeatures));
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.GlowPlugCycles && e.Value == 3.0);
        }

        [Fact]
        public void DryRunPumpMinutes_ThresholdsAndReset()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            // 1. Bomba desligada: NÃO dispara mesmo com motor desligado no solo ou tanque vazio
            var pumpOffFrame = CreateBaseFrame(new()
            {
                ["fuel-pump-on"] = 0.0,
                ["fuel-total-quantity"] = 0.0,
                ["ctx-sim-on-ground"] = 1.0,
                ["eng-combustion:1"] = 0.0
            });
            var evs1 = deriver.Update(pumpOffFrame, 600.0, features);
            Assert.DoesNotContain(evs1, e => e.MetricKey == WearRuleMetrics.DryRunPumpMinutes);

            // 2. Bomba ligada em voo com combustível e motor ligado: normal, NÃO dispara
            deriver.Reset();
            var inFlightNormal = CreateBaseFrame(new()
            {
                ["fuel-pump-on"] = 1.0,
                ["fuel-total-quantity"] = 25.0,
                ["ctx-sim-on-ground"] = 0.0,
                ["eng-combustion:1"] = 1.0
            });
            var evs2 = deriver.Update(inFlightNormal, 600.0, features);
            Assert.DoesNotContain(evs2, e => e.MetricKey == WearRuleMetrics.DryRunPumpMinutes);

            // 3. Bomba ligada no solo com motor desligado: acumula e cruza 2, 5, 10 min
            deriver.Reset();
            var dryGroundFrame = CreateBaseFrame(new()
            {
                ["fuel-pump-on"] = 1.0,
                ["fuel-total-quantity"] = 20.0,
                ["ctx-sim-on-ground"] = 1.0,
                ["eng-combustion:1"] = 0.0
            });

            var allEvents = new List<DerivedPartEvent>();
            allEvents.AddRange(deriver.Update(dryGroundFrame, 130.0, features)); // 2.16 min
            Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.DryRunPumpMinutes && e.Value == 2.0);

            allEvents.AddRange(deriver.Update(dryGroundFrame, 180.0, features)); // total 5.16 min
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.DryRunPumpMinutes && e.Value == 5.0);

            allEvents.AddRange(deriver.Update(dryGroundFrame, 300.0, features)); // total 10.16 min
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.DryRunPumpMinutes && e.Value == 10.0);

            // 4. Desligar bomba por 1 min (60 s): reinicia acumulador
            deriver.Update(pumpOffFrame, 65.0, features);
            var retryEvents = deriver.Update(dryGroundFrame, 130.0, features);
            Assert.Contains(retryEvents, e => e.MetricKey == WearRuleMetrics.DryRunPumpMinutes && e.Value == 2.0);
        }

        [Fact]
        public void LowHydraulicPressureMinutes_ThresholdsAndArming()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.RetractableGear };

            // Avião sem sistema hidráulico (pressão sempre zero): NUNCA arma nem dispara
            var noHydFrame = CreateBaseFrame(new()
            {
                ["hyd-pressure"] = 0.0,
                ["eng-combustion:1"] = 1.0
            });
            var evs1 = deriver.Update(noHydFrame, 600.0, features);
            Assert.DoesNotContain(evs1, e => e.MetricKey == WearRuleMetrics.LowHydraulicPressureMinutes);

            // Pressão sobe para 3000 psi (arma o sistema)
            deriver.Reset();
            var armedFrame = CreateBaseFrame(new()
            {
                ["hyd-pressure"] = 3000.0,
                ["eng-combustion:1"] = 1.0
            });
            deriver.Update(armedFrame, 10.0, features);

            // Com motor em combustão e pressão abaixo de 50% de 3000 (ex: 1200 psi < 1500 psi):
            // acumula e emite em 3, 8, 15 min
            var lowPressFrame = CreateBaseFrame(new()
            {
                ["hyd-pressure"] = 1200.0,
                ["eng-combustion:1"] = 1.0
            });

            var allEvents = new List<DerivedPartEvent>();
            allEvents.AddRange(deriver.Update(lowPressFrame, 190.0, features)); // 3.16 min
            Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.LowHydraulicPressureMinutes && e.Value == 3.0);

            allEvents.AddRange(deriver.Update(lowPressFrame, 310.0, features)); // total 8.33 min
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.LowHydraulicPressureMinutes && e.Value == 8.0);

            allEvents.AddRange(deriver.Update(lowPressFrame, 420.0, features)); // total 15.33 min
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.LowHydraulicPressureMinutes && e.Value == 15.0);

            // Com motor desligado, baixa pressão NÃO conta
            deriver.Reset();
            deriver.Update(armedFrame, 10.0, features);
            var lowEngineOff = CreateBaseFrame(new()
            {
                ["hyd-pressure"] = 500.0,
                ["eng-combustion:1"] = 0.0
            });
            var evsOff = deriver.Update(lowEngineOff, 600.0, features);
            Assert.DoesNotContain(evsOff, e => e.MetricKey == WearRuleMetrics.LowHydraulicPressureMinutes);
        }

        [Fact]
        public void PressurizationCycles_CountAndThresholds()
        {
            var deriver = new PartEventDeriver();

            // Aeronave não pressurizada (ex: C152): NÃO processa
            var unpressurizedFeatures = new[] { AircraftFeatures.Piston };
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 4.0 }), 1.0, unpressurizedFeatures);
            var evs1 = deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 0.1 }), 1.0, unpressurizedFeatures);
            Assert.DoesNotContain(evs1, e => e.MetricKey == WearRuleMetrics.PressurizationCycles);

            // Aeronave pressurizada: sobe acima de 2.0 psi e volta abaixo de 0.5 psi = 1 ciclo
            deriver.Reset();
            var pressFeatures = new[] { AircraftFeatures.Turboprop, AircraftFeatures.Pressurized };
            var allEvents = new List<DerivedPartEvent>();

            // Subida para 1.5 psi (não atinge 2.0) e descida para 0.2: NÃO conta ciclo
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 1.5 }), 1.0, pressFeatures);
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 0.2 }), 1.0, pressFeatures);
            Assert.Empty(allEvents);

            // Ciclo 1: sobe para 5.5 psi e desce para 0.3 psi -> emite em 1.0
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 5.5 }), 1.0, pressFeatures);
            allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 0.3 }), 1.0, pressFeatures));
            Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.PressurizationCycles && e.Value == 1.0);

            // Ciclo 2: sobe e desce -> emite em 2.0
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 6.0 }), 1.0, pressFeatures);
            allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 0.4 }), 1.0, pressFeatures));
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.PressurizationCycles && e.Value == 2.0);

            // Ciclos 3 e 4 -> emite em 4.0
            for (int i = 3; i <= 4; i++)
            {
                deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 6.0 }), 1.0, pressFeatures);
                allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 0.4 }), 1.0, pressFeatures));
            }
            Assert.Contains(allEvents, e => e.MetricKey == WearRuleMetrics.PressurizationCycles && e.Value == 4.0);
        }

        [Fact]
        public void RapidDecompressionDeltaPsf_TriggersOnDropAndEmitsPeak()
        {
            var deriver = new PartEventDeriver();

            // Aeronave não pressurizada: NÃO dispara
            var unpressurizedFeatures = new[] { AircraftFeatures.Piston };
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 6.0 }), 1.0, unpressurizedFeatures);
            var evsUnp = deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 2.0 }), 1.0, unpressurizedFeatures);
            Assert.DoesNotContain(evsUnp, e => e.MetricKey == WearRuleMetrics.RapidDecompressionDeltaPsf);

            // Aeronave pressurizada: descida lenta normal (delta de 0.05 psi/s * 144 = 7.2 psf/s): NÃO dispara
            deriver.Reset();
            var pressFeatures = new[] { AircraftFeatures.Jet, AircraftFeatures.Pressurized };
            for (int i = 0; i < 10; i++)
            {
                double psi = 7.0 - (0.05 * i);
                var evsSlow = deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = psi }), 1.0, pressFeatures);
                Assert.DoesNotContain(evsSlow, e => e.MetricKey == WearRuleMetrics.RapidDecompressionDeltaPsf);
            }

            // Descompressão rápida: diferencial cai de 6.0 para 3.0 psi em 1 s (queda de 3.0 * 144 = 432 psf/s >= 150)
            deriver.Reset();
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 6.0 }), 1.0, pressFeatures);
            deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 3.0 }), 1.0, pressFeatures);

            // A queda estabiliza (< 20 psf/s por 5 s) a 3.0 psi
            var allEvents = new List<DerivedPartEvent>();
            for (int i = 0; i < 6; i++)
            {
                allEvents.AddRange(deriver.Update(CreateBaseFrame(new() { ["af-pressurization-pressure-differential"] = 3.0 }), 1.0, pressFeatures));
            }

            var decompEvent = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.RapidDecompressionDeltaPsf);
            Assert.InRange(decompEvent.Value, 420.0, 440.0);
        }

        // =========================================================================
        // 3. TESTE DE GERAÇÃO DO RELATÓRIO DE COBERTURA DOS 46 MODELOS
        // =========================================================================

        [Fact]
        public void CoverageReport_CanBeGeneratedAndEvaluates46Aircraft()
        {
            var service = new RuleCoverageService();
            var summaries = service.EvaluateAll();

            Assert.Equal(46, summaries.Count);

            // Verifica as 3 classes representadas
            Assert.Equal(19, summaries.Count(s => s.Class == "Piston"));
            Assert.Equal(12, summaries.Count(s => s.Class == "Turboprop"));
            Assert.Equal(15, summaries.Count(s => s.Class == "Jet"));

            // Cada aeronave deve avaliar as 47 regras
            foreach (var s in summaries)
            {
                Assert.Equal(47, s.Results.Count);
                Assert.True(s.ActiveCount > 0, $"Aeronave '{s.AircraftId}' não possui nenhuma regra ativa.");
            }

            // Gera markdown e verifica integridade
            string markdown = service.GenerateMarkdownReport();
            Assert.Contains("# Relatório de Cobertura das Regras de Desgaste por Aeronave", markdown);
            Assert.Contains("## 1. Resumo por Classe", markdown);
            Assert.Contains("Cessna C152", markdown);
            Assert.Contains("Asobo TBM 930", markdown);
            Assert.Contains("747-8i", markdown);

            // Salva no caminho oficial do relatório se solicitado ou se o arquivo não existir
            string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "docs", "plans", "cobertura-regras-por-aviao.md");
            if (!File.Exists(reportPath) || Environment.GetEnvironmentVariable("GENERATE_RULE_COVERAGE_REPORT") == "1")
            {
                service.SaveReport(reportPath);
            }
        }
    }
}
