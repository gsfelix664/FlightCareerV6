using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class ElectricalRuleDeriverTests
    {
        private static SensorFrame CreateFrame(
            double simRate = 1.0,
            double starter = 0.0,
            double avionicsCircuit = 0.0,
            double masterBattery = 1.0,
            double busVoltage = 28.0,
            double totalLoadAmps = 20.0,
            double combustion = 1.0,
            double alternator = 1.0)
        {
            var readings = new Dictionary<string, double>
            {
                ["ctx-simulation-rate"] = simRate,
                ["eng-starter:1"] = starter,
                ["eng-combustion:1"] = combustion,
                ["elec-master-alternator:1"] = alternator,
                ["elec-circuit-on-3"] = avionicsCircuit,
                ["elec-master-battery"] = masterBattery,
                ["elec-bus-voltage-1"] = busVoltage,
                ["elec-total-load-amps"] = totalLoadAmps
            };

            return new SensorFrame(
                Values: readings,
                ReceivedUtc: DateTime.UtcNow,
                SimulationRate: simRate
            );
        }

        private static ElectricalConfig CreateC172Config()
        {
            return new ElectricalConfig
            {
                SystemVolts = 28.0,
                AvionicsCircuitIndex = 3,
                AvionicsBusAmpsIndex = 4,
                MainBusIndex = 1,
                AlternatorRatedAmps = 60.0,
                DeepDischargeVolts = 22.0,
                BatteryDeepDischargeThresholds = new[] { 10.0, 25.0, 45.0 },
                ElectricalOverloadThresholds = new[] { 5.0, 15.0, 30.0 }
            };
        }

        private static readonly IReadOnlyList<string> Features = new[] { "piston", "singleEngine" };

        [Fact]
        public void Deriver_PartidaComAvionicsLigado_EmiteContadorCrescente()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // 1. Starter off
            var f1 = CreateFrame(starter: 0.0, avionicsCircuit: 1.0);
            deriver.Update(f1, 1.0, Features);

            // 2. Starter engata (borda 0 -> 1) com aviônicos em 1 -> evento 1
            var f2 = CreateFrame(starter: 1.0, avionicsCircuit: 1.0);
            var events1 = deriver.Update(f2, 1.0, Features);

            var spike1 = events1.FirstOrDefault(e => e.MetricKey == "avionicsMasterStartSpikes");
            Assert.NotNull(spike1);
            Assert.Equal(1.0, spike1.Value);

            // 3. Starter desengata
            var f3 = CreateFrame(starter: 0.0, avionicsCircuit: 1.0);
            deriver.Update(f3, 1.0, Features);

            // 4. Nova partida (segunda borda 0 -> 1) com aviônicos em 1 -> evento 2
            var f4 = CreateFrame(starter: 1.0, avionicsCircuit: 1.0);
            var events2 = deriver.Update(f4, 1.0, Features);

            var spike2 = events2.FirstOrDefault(e => e.MetricKey == "avionicsMasterStartSpikes");
            Assert.NotNull(spike2);
            Assert.Equal(2.0, spike2.Value);
        }

        [Fact]
        public void Deriver_PartidaComAvionicsDesligado_NaoEmite()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            var f1 = CreateFrame(starter: 0.0, avionicsCircuit: 0.0);
            deriver.Update(f1, 1.0, Features);

            var f2 = CreateFrame(starter: 1.0, avionicsCircuit: 0.0);
            var events = deriver.Update(f2, 1.0, Features);

            Assert.DoesNotContain(events, e => e.MetricKey == "avionicsMasterStartSpikes");
        }

        [Fact]
        public void Deriver_SemIndiceNoCatalogo_RegraDesligada()
        {
            var deriver = new PartEventDeriver();
            // Sem AvionicsCircuitIndex
            var cfg = new ElectricalConfig
            {
                SystemVolts = 28.0,
                AvionicsCircuitIndex = null,
                AvionicsBusAmpsIndex = null,
                MainBusIndex = 1,
                AlternatorRatedAmps = 60.0,
                DeepDischargeVolts = 22.0,
                BatteryDeepDischargeThresholds = new[] { 10.0, 25.0, 45.0 },
                ElectricalOverloadThresholds = new[] { 5.0, 15.0, 30.0 }
            };
            deriver.ConfigureElectrical(cfg);

            var f1 = CreateFrame(starter: 0.0, avionicsCircuit: 1.0);
            deriver.Update(f1, 1.0, Features);

            var f2 = CreateFrame(starter: 1.0, avionicsCircuit: 1.0);
            var events = deriver.Update(f2, 1.0, Features);

            Assert.DoesNotContain(events, e => e.MetricKey == "avionicsMasterStartSpikes");
        }

        [Fact]
        public void Deriver_DiagnosticoDeRegraDesligada_SaiUmaVezPorVoo()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(new ElectricalConfig()); // nenhum parâmetro elétrico no catálogo

            deriver.Update(CreateFrame(starter: 0.0, avionicsCircuit: 1.0), 1.0, Features);
            var first = deriver.DrainDiagnostics();
            deriver.Update(CreateFrame(starter: 1.0, avionicsCircuit: 1.0), 1.0, Features);
            deriver.Update(CreateFrame(starter: 0.0, avionicsCircuit: 1.0), 1.0, Features);
            var later = deriver.DrainDiagnostics();

            Assert.NotEmpty(first);
            Assert.DoesNotContain(later, m => first.Contains(m));
        }

        [Fact]
        public void Deriver_DescargaProfunda_CruzaFaixasUmaVezCada()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // Tensão em 21 V (< 22 V), simRate 2.0 (tempo simulado passa 2x mais rápido)
            // 46 minutos simulados = 2760 s simulados. A cada tick de 1 s real (2 s simulados), chamamos 1380 vezes.
            var emittedEvents = new List<DerivedPartEvent>();

            for (int i = 0; i < 1400; i++)
            {
                var frame = CreateFrame(simRate: 2.0, busVoltage: 21.0);
                var evts = deriver.Update(frame, dtSimSeconds: 2.0, Features);
                emittedEvents.AddRange(evts.Where(e => e.MetricKey == "batteryDeepDischargeMinutes"));
            }

            // Deve emitir faixas 10, 25 e 45, exatamente uma vez cada
            Assert.Equal(3, emittedEvents.Count);
            Assert.Equal(10.0, emittedEvents[0].Value);
            Assert.Equal(25.0, emittedEvents[1].Value);
            Assert.Equal(45.0, emittedEvents[2].Value);
        }

        [Fact]
        public void Deriver_DescargaProfunda_AlternadorCarregando_NaoAcumula()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // Tensão em 28 V (normal) por 60 min simulados
            var emittedEvents = new List<DerivedPartEvent>();
            for (int i = 0; i < 1800; i++)
            {
                var frame = CreateFrame(simRate: 2.0, busVoltage: 28.0);
                var evts = deriver.Update(frame, dtSimSeconds: 2.0, Features);
                emittedEvents.AddRange(evts.Where(e => e.MetricKey == "batteryDeepDischargeMinutes"));
            }

            Assert.Empty(emittedEvents);
        }

        [Fact]
        public void Deriver_DescargaProfunda_InterrupcaoCurta_NaoZera()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // 9 minutos de descarga profunda (540 s)
            for (int i = 0; i < 540; i++)
            {
                var frame = CreateFrame(simRate: 1.0, busVoltage: 21.0);
                deriver.Update(frame, 1.0, Features);
            }

            // Interrupção curta de 5 segundos com tensão normal (28 V)
            for (int i = 0; i < 5; i++)
            {
                var frame = CreateFrame(simRate: 1.0, busVoltage: 28.0);
                deriver.Update(frame, 1.0, Features);
            }

            // Mais 2 minutos de descarga profunda (120 s). Total de descarga: 11 min -> deve cruzar threshold 10!
            var emitted = new List<DerivedPartEvent>();
            for (int i = 0; i < 120; i++)
            {
                var frame = CreateFrame(simRate: 1.0, busVoltage: 21.0);
                var evts = deriver.Update(frame, 1.0, Features);
                emitted.AddRange(evts.Where(e => e.MetricKey == "batteryDeepDischargeMinutes"));
            }

            Assert.Single(emitted);
            Assert.Equal(10.0, emitted[0].Value);
        }

        [Fact]
        public void Deriver_DescargaProfunda_InterrupcaoLonga_Zera()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // 9 minutos de descarga profunda (540 s)
            for (int i = 0; i < 540; i++)
            {
                var frame = CreateFrame(simRate: 1.0, busVoltage: 21.0);
                deriver.Update(frame, 1.0, Features);
            }

            // Interrupção longa de 15 segundos (>= 10 s) com tensão normal
            for (int i = 0; i < 15; i++)
            {
                var frame = CreateFrame(simRate: 1.0, busVoltage: 28.0);
                deriver.Update(frame, 1.0, Features);
            }

            // Mais 2 minutos de descarga profunda (120 s). Como zerou, acumulou apenas 2 min (não atinge 10 min)
            var emitted = new List<DerivedPartEvent>();
            for (int i = 0; i < 120; i++)
            {
                var frame = CreateFrame(simRate: 1.0, busVoltage: 21.0);
                var evts = deriver.Update(frame, 1.0, Features);
                emitted.AddRange(evts.Where(e => e.MetricKey == "batteryDeepDischargeMinutes"));
            }

            Assert.Empty(emitted);
        }

        [Fact]
        public void Deriver_Sobrecarga_IgnoraStarter()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // 170 A (pico de partida do C172), tensão 24 V, starter engatado por 6 minutos
            var emitted = new List<DerivedPartEvent>();
            for (int i = 0; i < 360; i++)
            {
                var frame = CreateFrame(simRate: 1.0, starter: 1.0, busVoltage: 24.0, totalLoadAmps: 170.0);
                var evts = deriver.Update(frame, 1.0, Features);
                emitted.AddRange(evts.Where(e => e.MetricKey == "electricalOverloadMinutes"));
            }

            Assert.Empty(emitted);
        }

        [Fact]
        public void Overload_BarramentoEstavel_NaoConta()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // 75 A (> 60 A rated), mas barramento estável em 28.0 V (>= 0.95 * 28 = 26.6 V):
            // Alternador está suprindo sem sag de sobrecarga prejudicial -> não deve emitir sobrecarga
            var emitted = new List<DerivedPartEvent>();
            for (int i = 0; i < 360; i++)
            {
                var frame = CreateFrame(simRate: 1.0, starter: 0.0, busVoltage: 28.0, totalLoadAmps: 75.0, combustion: 1.0, alternator: 1.0);
                var evts = deriver.Update(frame, 1.0, Features);
                emitted.AddRange(evts.Where(e => e.MetricKey == "electricalOverloadMinutes"));
            }

            Assert.Empty(emitted);
        }

        [Fact]
        public void Overload_TensaoCaida_ComCargaAlta_Conta()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // 75 A (> 60 A rated), barramento com sag para 25.5 V (faixa [0.88 * 28, 0.95 * 28) = [24.64, 26.6)):
            // Alternador ativo e motor ligado sofrendo sobrecarga real -> cruza threshold de 5 min aos 300 s
            var emitted = new List<DerivedPartEvent>();
            for (int i = 0; i < 360; i++)
            {
                var frame = CreateFrame(simRate: 1.0, starter: 0.0, busVoltage: 25.5, totalLoadAmps: 75.0, combustion: 1.0, alternator: 1.0);
                var evts = deriver.Update(frame, 1.0, Features);
                emitted.AddRange(evts.Where(e => e.MetricKey == "electricalOverloadMinutes"));
            }

            Assert.Single(emitted);
            Assert.Equal(5.0, emitted[0].Value);
        }

        [Fact]
        public void Overload_AlternadorEmPane_BateriaSozinha_NaoConta()
        {
            var deriver = new PartEventDeriver();
            deriver.ConfigureElectrical(CreateC172Config());

            // 75 A (> 60 A), mas tensão colapsada em 23.5 V (< 0.88 * 28 = 24.64 V):
            // Alternador fora da barra/desarmado e bateria fornecendo sozinha -> não conta sobrecarga de alternador
            var emitted = new List<DerivedPartEvent>();
            for (int i = 0; i < 360; i++)
            {
                var frame = CreateFrame(simRate: 1.0, starter: 0.0, busVoltage: 23.5, totalLoadAmps: 75.0, combustion: 1.0, alternator: 1.0);
                var evts = deriver.Update(frame, 1.0, Features);
                emitted.AddRange(evts.Where(e => e.MetricKey == "electricalOverloadMinutes"));
            }

            Assert.Empty(emitted);
        }

        [Fact]
        public void Deriver_Sobrecarga_SemAlternatorRatedAmps_RegraDesligada()
        {
            var deriver = new PartEventDeriver();
            var cfg = new ElectricalConfig
            {
                SystemVolts = 28.0,
                AvionicsCircuitIndex = 3,
                AvionicsBusAmpsIndex = 4,
                MainBusIndex = 1,
                AlternatorRatedAmps = null, // Ausente
                DeepDischargeVolts = 22.0,
                BatteryDeepDischargeThresholds = new[] { 10.0, 25.0, 45.0 },
                ElectricalOverloadThresholds = new[] { 5.0, 15.0, 30.0 }
            };
            deriver.ConfigureElectrical(cfg);

            var emitted = new List<DerivedPartEvent>();
            for (int i = 0; i < 360; i++)
            {
                var frame = CreateFrame(simRate: 1.0, starter: 0.0, busVoltage: 28.0, totalLoadAmps: 100.0);
                var evts = deriver.Update(frame, 1.0, Features);
                emitted.AddRange(evts.Where(e => e.MetricKey == "electricalOverloadMinutes"));
            }

            Assert.Empty(emitted);
        }
    }
}
