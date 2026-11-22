using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class GroundAndEngineMetricsTests
    {
        private static SensorFrame CreateBaseFrame(Dictionary<string, double>? overrides = null)
        {
            var values = new Dictionary<string, double>
            {
                ["ctx-sim-on-ground"] = 1.0,
                ["ctx-ground-velocity"] = 0.0,
                ["brake-left-position"] = 0.0,
                ["brake-right-position"] = 0.0,
                ["brake-parking-position"] = 0.0,
                ["af-design-speed-vs0"] = 33.0,
                ["gear-steer-angle-pct"] = 0.0,
                ["gear-is-skidding"] = 0.0,
                ["light-landing-on"] = 0.0,
                ["light-taxi-on"] = 0.0,
                ["light-strobe-on"] = 0.0,
                ["light-nav-on"] = 0.0,
                ["light-beacon-on"] = 0.0,
                ["eng-combustion:1"] = 1.0,
                ["eng-starter:1"] = 0.0,
                ["oil-temperature:1"] = 75.0,
                ["eng-rpm:1"] = 1500.0,
                ["eng-throttle-lever-position:1"] = 20.0,
                ["af-elevator-trim-position"] = 0.0,
                ["af-autopilot-master"] = 0.0,
                ["af-g-force"] = 1.0
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

        [Fact]
        public void Frenagem_forte_do_c152_gera_evento_de_energia()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston, AircraftFeatures.SingleEngine };

            // C152 com Vs0 = 33 kt. GS desacelera de 45 para 5 kt em 8 s com freio 0.8
            // 8 frames de 1 segundo simulado: velocidade cai linearmente de 45 para 5 kt (5 kt por segundo)
            var allEvents = new List<DerivedPartEvent>();

            for (int i = 0; i <= 8; i++)
            {
                double gs = 45.0 - (5.0 * i);
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = gs,
                    ["brake-left-position"] = 0.8 * 32767.0,
                    ["brake-right-position"] = 0.8 * 32767.0,
                    ["brake-parking-position"] = 0.0,
                    ["af-design-speed-vs0"] = 33.0
                });

                var evs = deriver.Update(f, 1.0, features);
                allEvents.AddRange(evs);
            }

            // Agora encerra a ocorrência: solta o freio por 2 segundos simulados a 5 kt
            for (int i = 0; i < 2; i++)
            {
                var fEnd = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 5.0,
                    ["brake-left-position"] = 0.0,
                    ["brake-right-position"] = 0.0,
                    ["af-design-speed-vs0"] = 33.0
                });
                var evs = deriver.Update(fEnd, 1.0, features);
                allEvents.AddRange(evs);
            }

            var brakeEvent = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.BrakeEnergyPctRef);
            // Vso 33 kt; GS 45 -> 5 kt:
            // v_start = 45 * 0.5144 = 23.148 m/s, v_end = 5 * 0.5144 = 2.572 m/s
            // soma Δ(v²/2) = (23.148² - 2.572²) / 2 = (535.83 - 6.615) / 2 = 264.6075
            // refEnergy = (1.3 * 33 * 0.5144)² / 2 = (22.06776)² / 2 = 243.493
            // pctRef = 100 * 264.6075 / 243.493 = 108.67%
            Assert.InRange(brakeEvent.Value, 105.0, 111.0);
        }

        [Fact]
        public void Frenagem_leve_nao_gera_evento()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            var allEvents = new List<DerivedPartEvent>();
            // Freio leve (0.2 < 0.30)
            for (int i = 0; i <= 8; i++)
            {
                double gs = 45.0 - (5.0 * i);
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = gs,
                    ["brake-left-position"] = 0.2 * 32767.0,
                    ["brake-right-position"] = 0.2 * 32767.0,
                    ["af-design-speed-vs0"] = 33.0
                });

                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }

            for (int i = 0; i < 2; i++)
            {
                var fEnd = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 5.0,
                    ["brake-left-position"] = 0.0,
                    ["brake-right-position"] = 0.0,
                    ["af-design-speed-vs0"] = 33.0
                });
                allEvents.AddRange(deriver.Update(fEnd, 1.0, features));
            }

            Assert.DoesNotContain(allEvents, e => e.MetricKey == WearRuleMetrics.BrakeEnergyPctRef);
        }

        [Fact]
        public void Freio_de_estacionamento_nao_conta()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            var allEvents = new List<DerivedPartEvent>();
            // Freio aplicado com freio de mão puxado
            for (int i = 0; i <= 8; i++)
            {
                double gs = 45.0 - (5.0 * i);
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = gs,
                    ["brake-left-position"] = 0.8 * 32767.0,
                    ["brake-right-position"] = 0.8 * 32767.0,
                    ["brake-parking-position"] = 1.0,
                    ["af-design-speed-vs0"] = 33.0
                });

                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }

            Assert.DoesNotContain(allEvents, e => e.MetricKey == WearRuleMetrics.BrakeEnergyPctRef);
        }

        [Fact]
        public void Curva_rapida_no_taxi()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { "noseWheelSteering" };

            var allEvents = new List<DerivedPartEvent>();

            // Curva rápida: GS 20 kt, direção 60% por 4 segundos
            for (int i = 0; i < 4; i++)
            {
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 20.0,
                    ["gear-steer-angle-pct"] = 60.0
                });
                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }

            // Fim da curva: direção volta a 0% por 2 segundos
            for (int i = 0; i < 2; i++)
            {
                var fEnd = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 18.0,
                    ["gear-steer-angle-pct"] = 0.0
                });
                allEvents.AddRange(deriver.Update(fEnd, 1.0, features));
            }

            var turnEv = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.TaxiTurnSpeedKnots);
            Assert.Equal(20.0, turnEv.Value);

            // Agora teste com GS 10 kt: não deve emitir pois < 15 kt
            allEvents.Clear();
            for (int i = 0; i < 4; i++)
            {
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 10.0,
                    ["gear-steer-angle-pct"] = 60.0
                });
                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }
            for (int i = 0; i < 2; i++)
            {
                var fEnd = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 10.0,
                    ["gear-steer-angle-pct"] = 0.0
                });
                allEvents.AddRange(deriver.Update(fEnd, 1.0, features));
            }

            Assert.DoesNotContain(allEvents, e => e.MetricKey == WearRuleMetrics.TaxiTurnSpeedKnots);
        }

        [Fact]
        public void Derrapagem_de_4_s_emite_4()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            var allEvents = new List<DerivedPartEvent>();

            // 4 s derrapando a 25 kt
            for (int i = 0; i < 4; i++)
            {
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 25.0,
                    ["gear-is-skidding"] = 1.0
                });
                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }

            // Fim após 3 s sem derrapar
            for (int i = 0; i < 3; i++)
            {
                var fEnd = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 25.0,
                    ["gear-is-skidding"] = 0.0
                });
                allEvents.AddRange(deriver.Update(fEnd, 1.0, features));
            }

            var skidEv = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.SkidSeconds);
            Assert.Equal(4.0, skidEv.Value);

            // Derrapagem a GS 1 kt -> nada
            allEvents.Clear();
            for (int i = 0; i < 4; i++)
            {
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 1.0, // < 3 kt
                    ["gear-is-skidding"] = 1.0
                });
                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }
            for (int i = 0; i < 3; i++)
            {
                var fEnd = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 1.0,
                    ["ctx-ground-velocity"] = 1.0,
                    ["gear-is-skidding"] = 0.0
                });
                allEvents.AddRange(deriver.Update(fEnd, 1.0, features));
            }
            Assert.DoesNotContain(allEvents, e => e.MetricKey == WearRuleMetrics.SkidSeconds);
        }

        [Fact]
        public void Partida_com_farol_e_strobe_emite_2()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            // 1. Frame com starter ativo, sem combustão, landing e strobe ligados, taxi desligado, beacon e nav ligados
            var f1 = CreateBaseFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["eng-combustion:1"] = 0.0,
                ["light-landing-on"] = 1.0,
                ["light-strobe-on"] = 1.0,
                ["light-taxi-on"] = 0.0,
                ["light-nav-on"] = 1.0,
                ["light-beacon-on"] = 1.0
            });
            deriver.Update(f1, 1.0, features);

            // 2. Combustão inicia (0 -> 1)
            var f2 = CreateBaseFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["eng-combustion:1"] = 1.0,
                ["light-landing-on"] = 1.0,
                ["light-strobe-on"] = 1.0,
                ["light-taxi-on"] = 0.0,
                ["light-nav-on"] = 1.0,
                ["light-beacon-on"] = 1.0
            });
            var evs = deriver.Update(f2, 1.0, features);

            var lightEv = Assert.Single(evs, e => e.MetricKey == WearRuleMetrics.StartLightsOnCount);
            Assert.Equal(2.0, lightEv.Value);

            // Só beacon e nav -> nada
            deriver.Reset();
            var f3 = CreateBaseFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["eng-combustion:1"] = 0.0,
                ["light-landing-on"] = 0.0,
                ["light-strobe-on"] = 0.0,
                ["light-taxi-on"] = 0.0,
                ["light-nav-on"] = 1.0,
                ["light-beacon-on"] = 1.0
            });
            deriver.Update(f3, 1.0, features);

            var f4 = CreateBaseFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["eng-combustion:1"] = 1.0,
                ["light-landing-on"] = 0.0,
                ["light-strobe-on"] = 0.0,
                ["light-taxi-on"] = 0.0,
                ["light-nav-on"] = 1.0,
                ["light-beacon-on"] = 1.0
            });
            var evsNoLights = deriver.Update(f4, 1.0, features);
            Assert.DoesNotContain(evsNoLights, e => e.MetricKey == WearRuleMetrics.StartLightsOnCount);
        }

        [Fact]
        public void Oleo_frio_em_potencia_alta_emite_nos_cruzamentos()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };
            deriver.ConfigureEngine(new EngineLimitsConfig { OilTempMin = 24.0, RpmRedline = 2700.0 });

            var allEvents = new List<DerivedPartEvent>();

            // Potência alta: RPM 2500 (>= 0.90 * 2700 = 2430), óleo a 15 °C (< 24 °C)
            // 7 minutos (420 s simulados)
            for (int i = 0; i < 420; i++)
            {
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["eng-combustion:1"] = 1.0,
                    ["oil-temperature:1"] = 15.0,
                    ["eng-rpm:1"] = 2500.0
                });
                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }

            var emitted = allEvents.Where(e => e.MetricKey == WearRuleMetrics.HighPowerColdOilMinutes).ToList();
            Assert.Equal(3, emitted.Count);
            Assert.Equal(1.0, emitted[0].Value);
            Assert.Equal(3.0, emitted[1].Value);
            Assert.Equal(6.0, emitted[2].Value);

            // Ficar fora da condição por 2 minutos (120 s)
            allEvents.Clear();
            for (int i = 0; i < 120; i++)
            {
                var fOk = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["eng-combustion:1"] = 1.0,
                    ["oil-temperature:1"] = 50.0, // Faixa normal
                    ["eng-rpm:1"] = 1500.0
                });
                deriver.Update(fOk, 1.0, features);
            }

            // Ao entrar novamente por 1 minuto (60 s), deve emitir 1.0 novamente (reset com sucesso)
            for (int i = 0; i < 60; i++)
            {
                var f = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["eng-combustion:1"] = 1.0,
                    ["oil-temperature:1"] = 15.0,
                    ["eng-rpm:1"] = 2500.0
                });
                allEvents.AddRange(deriver.Update(f, 1.0, features));
            }

            var reEmitted = Assert.Single(allEvents, e => e.MetricKey == WearRuleMetrics.HighPowerColdOilMinutes);
            Assert.Equal(1.0, reEmitted.Value);
        }

        [Fact]
        public void Ciclos_de_compensador()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            var allEvents = new List<DerivedPartEvent>();

            // No ar (ctx-sim-on-ground = 0)
            // Simular 9 ciclos: trim anda de 0 para 0.15 e volta para 0
            for (int cycle = 1; cycle <= 9; cycle++)
            {
                // Anda de 0 para 0.15 (+15%)
                var fUp = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 0.0,
                    ["af-elevator-trim-position"] = 0.15
                });
                allEvents.AddRange(deriver.Update(fUp, 1.0, features));

                // Volta para 0 (inverte curso)
                var fDown = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 0.0,
                    ["af-elevator-trim-position"] = 0.0
                });
                allEvents.AddRange(deriver.Update(fDown, 1.0, features));
            }

            var trimEvents = allEvents.Where(e => e.MetricKey == WearRuleMetrics.TrimCycles).ToList();
            Assert.Equal(3, trimEvents.Count);
            Assert.Equal(1.0, trimEvents[0].Value);
            Assert.Equal(4.0, trimEvents[1].Value);
            Assert.Equal(8.0, trimEvents[2].Value);
        }

        [Fact]
        public void Turbulencia_com_piloto_automatico()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Piston };

            var allEvents = new List<DerivedPartEvent>();

            // No ar com AP ligado
            // Simular 50 ciclos de turbulência: G vai para 1.6 (|1.6 - 1| = 0.6 >= 0.5) e rearma em 1.05 (|1.05 - 1| = 0.05 < 0.2)
            for (int i = 1; i <= 50; i++)
            {
                var fHighG = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 0.0,
                    ["af-autopilot-master"] = 1.0,
                    ["af-g-force"] = 1.6
                });
                allEvents.AddRange(deriver.Update(fHighG, 1.0, features));

                var fNormalG = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["ctx-sim-on-ground"] = 0.0,
                    ["af-autopilot-master"] = 1.0,
                    ["af-g-force"] = 1.05
                });
                allEvents.AddRange(deriver.Update(fNormalG, 1.0, features));
            }

            var gEvents = allEvents.Where(e => e.MetricKey == WearRuleMetrics.TurbulenceGExceedanceCycles).ToList();
            Assert.Equal(3, gEvents.Count);
            Assert.Equal(10.0, gEvents[0].Value);
            Assert.Equal(25.0, gEvents[1].Value);
            Assert.Equal(50.0, gEvents[2].Value);
        }

        [Fact]
        public void Transiente_de_torque_em_turboelice()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.Turboprop };

            var allEvents = new List<DerivedPartEvent>();

            // Simular 15 ciclos bruscos de torque: de 20% para 70% (delta 50 pp >= 40) dentro de 1 s
            for (int i = 1; i <= 15; i++)
            {
                var fLow = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["turb-max-torque-percent:1"] = 20.0
                });
                allEvents.AddRange(deriver.Update(fLow, 1.0, features));

                var fHigh = CreateBaseFrame(new Dictionary<string, double>
                {
                    ["turb-max-torque-percent:1"] = 70.0
                });
                allEvents.AddRange(deriver.Update(fHigh, 1.0, features));

                // 2 s de estabilidade para aguardar cooldown
                allEvents.AddRange(deriver.Update(fHigh, 2.5, features));
            }

            var transEvents = allEvents.Where(e => e.MetricKey == WearRuleMetrics.RapidPowerTransientCycles).ToList();
            Assert.Equal(3, transEvents.Count);
            Assert.Equal(3.0, transEvents[0].Value);
            Assert.Equal(8.0, transEvents[1].Value);
            Assert.Equal(15.0, transEvents[2].Value);

            // Sem feature turboprop: não emite nada
            deriver.Reset();
            allEvents.Clear();
            var pistonFeatures = new[] { AircraftFeatures.Piston };
            for (int i = 1; i <= 5; i++)
            {
                var fLow = CreateBaseFrame(new Dictionary<string, double> { ["turb-max-torque-percent:1"] = 20.0 });
                allEvents.AddRange(deriver.Update(fLow, 1.0, pistonFeatures));
                var fHigh = CreateBaseFrame(new Dictionary<string, double> { ["turb-max-torque-percent:1"] = 70.0 });
                allEvents.AddRange(deriver.Update(fHigh, 1.0, pistonFeatures));
                allEvents.AddRange(deriver.Update(fHigh, 2.5, pistonFeatures));
            }
            Assert.DoesNotContain(allEvents, e => e.MetricKey == WearRuleMetrics.RapidPowerTransientCycles);
        }
    }
}
