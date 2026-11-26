using System;
using System.Collections.Generic;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class ContractProgressFormatterTests
    {
        [Fact]
        public void Build_SemMissoesNemRastreadores_RetornaVazio()
        {
            var result = ContractProgressFormatter.Build(null, new ContractProgressInputs());
            Assert.Equal(string.Empty, result);

            var resultEmptyMissions = ContractProgressFormatter.Build(new List<Mission>(), new ContractProgressInputs());
            Assert.Equal(string.Empty, resultEmptyMissions);
        }

        [Fact]
        public void Build_Tgl_MostraContagem()
        {
            var missions = new List<Mission>
            {
                new() { RequiredTouchAndGoes = 2 }
            };

            var res1 = ContractProgressFormatter.Build(missions, new ContractProgressInputs(TouchAndGoCount: 1));
            Assert.Equal("TGL 1/2", res1);

            var res2 = ContractProgressFormatter.Build(missions, new ContractProgressInputs(TouchAndGoCount: 2));
            Assert.Equal("TGL 2/2 ✓", res2);
        }

        [Fact]
        public void Build_Sobrevoo_AbaixoDaFaixa()
        {
            var tracker = new OverflyTracker();
            var pt = new OverflyPoint("AREA", 0.0, 0.0, ElevationFt: 1000.0, RadiusNm: 3.0, MaxAglFt: 3500.0, MinAglFt: 2500.0);
            tracker.ConfigureWaypoints(new[] { pt });

            tracker.Update(0.0, 2.0 / 60.0, 2700.0);

            var result = ContractProgressFormatter.Build(null, new ContractProgressInputs(Overfly: tracker));
            Assert.Equal("Pontos 0/1 · AREA 2,0 NM · faixa 3.500–4.500 ft: abaixo", result);
        }

        [Fact]
        public void Build_Sobrevoo_NaFaixa_Visitado()
        {
            var tracker = new OverflyTracker();
            var pt = new OverflyPoint("AREA", 0.0, 0.0, ElevationFt: 1000.0, RadiusNm: 3.0, MaxAglFt: 3500.0, MinAglFt: 2500.0);
            tracker.ConfigureWaypoints(new[] { pt });

            tracker.Update(0.0, 0.0, 4000.0);

            var result = ContractProgressFormatter.Build(null, new ContractProgressInputs(Overfly: tracker));
            Assert.Equal("Pontos 1/1 ✓", result);
        }

        [Fact]
        public void Build_Sobrevoo_AcimaDaFaixa()
        {
            var tracker = new OverflyTracker();
            var pt = new OverflyPoint("AREA", 0.0, 0.0, ElevationFt: 1000.0, RadiusNm: 3.0, MaxAglFt: 3500.0, MinAglFt: 2500.0);
            tracker.ConfigureWaypoints(new[] { pt });

            tracker.Update(0.0, 4.0 / 60.0, 5100.0);

            var result = ContractProgressFormatter.Build(null, new ContractProgressInputs(Overfly: tracker));
            Assert.EndsWith("acima", result);
        }

        [Fact]
        public void EvaluateAltitude_RespeitaTolerancia()
        {
            var ptFaixa = new OverflyPoint("P1", 0.0, 0.0, ElevationFt: 1000.0, RadiusNm: 2.0, MaxAglFt: 3500.0, MinAglFt: 2500.0);
            // MinAgl > 0: Elev + Min - Tol = 1000 + 2500 - 100 = 3400
            //             Elev + Max + Tol = 1000 + 3500 + 100 = 4600
            Assert.Equal(OverflyAltitudeState.InBand, OverflyTracker.EvaluateAltitude(ptFaixa, 3400.0));
            Assert.Equal(OverflyAltitudeState.Below, OverflyTracker.EvaluateAltitude(ptFaixa, 3399.0));
            Assert.Equal(OverflyAltitudeState.InBand, OverflyTracker.EvaluateAltitude(ptFaixa, 4600.0));
            Assert.Equal(OverflyAltitudeState.Above, OverflyTracker.EvaluateAltitude(ptFaixa, 4601.0));

            var ptTeto = new OverflyPoint("P2", 0.0, 0.0, ElevationFt: 1000.0, RadiusNm: 2.0, MaxAglFt: 3000.0, MinAglFt: 0.0);
            // MinAgl == 0: Elev + Max = 1000 + 3000 = 4000
            Assert.Equal(OverflyAltitudeState.InBand, OverflyTracker.EvaluateAltitude(ptTeto, 4000.0));
            Assert.Equal(OverflyAltitudeState.Above, OverflyTracker.EvaluateAltitude(ptTeto, 4001.0));
        }

        [Fact]
        public void Build_Escalas_MostraProxima()
        {
            var tracker = new MultiStopTracker();
            tracker.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SBDN", Lat = -22.1969, Lon = -49.9261, ElevationFt = 1476 },
                    new() { Icao = "SDCO", Lat = -23.4808, Lon = -47.4897, ElevationFt = 2077 },
                    new() { Icao = "SBMT", Lat = -23.5092, Lon = -46.6378, ElevationFt = 2369 }
                }
            }, initialIsOnGround: true);

            var result = ContractProgressFormatter.Build(null, new ContractProgressInputs(MultiStop: tracker));
            Assert.Equal("Escala 0/3 · próxima SBDN", result);
        }

        [Fact]
        public void Build_VooDeTeste_Passo1()
        {
            var tracker = new CheckFlightTracker();
            tracker.Configure(new CheckFlightContractData
            {
                TestAltitudeFt = 5000.0,
                PowerHoldSeconds = 10.0,
                HeadingHoldSeconds = 15.0
            });

            var result = ContractProgressFormatter.Build(null, new ContractProgressInputs(CheckFlight: tracker));
            Assert.Equal("Passo 1/3 · subir a 5.000 ft", result);
        }

        [Fact]
        public void Build_Translado_Restricao()
        {
            var monitor = new FerryRestrictionMonitor();
            monitor.Configure(new FerryPermitContractData
            {
                Restriction = "max_altitude",
                MaxAltitudeFt = 8000.0
            });

            var result = ContractProgressFormatter.Build(null, new ContractProgressInputs(Ferry: monitor));
            Assert.Equal("Restrição: máx. 8.000 ft", result);
        }

        [Fact]
        public void Build_LimitaTresLinhas()
        {
            var missions = new List<Mission> { new() { RequiredTouchAndGoes = 2 } };

            var overfly = new OverflyTracker();
            overfly.ConfigureWaypoints(new[] { new OverflyPoint("WP1", 0, 0, 1000) });

            var multiStop = new MultiStopTracker();
            multiStop.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint> { new() { Icao = "SBDN" } }
            });

            var checkFlight = new CheckFlightTracker();
            checkFlight.Configure(new CheckFlightContractData { TestAltitudeFt = 5000 });

            var inputs = new ContractProgressInputs(
                TouchAndGoCount: 1,
                Overfly: overfly,
                MultiStop: multiStop,
                CheckFlight: checkFlight);

            var result = ContractProgressFormatter.Build(missions, inputs);
            var lines = result.Split('\n');
            Assert.Equal(3, lines.Length);
            Assert.StartsWith("TGL 1/2", lines[0]);
            Assert.StartsWith("Pontos", lines[1]);
            Assert.StartsWith("Escala", lines[2]);
        }

        [Fact]
        public void BuildLines_TglIncompleto_Pending()
        {
            var missions = new List<Mission> { new() { RequiredTouchAndGoes = 2 } };
            var lines = ContractProgressFormatter.BuildLines(missions, new ContractProgressInputs(TouchAndGoCount: 1));
            Assert.Single(lines);
            Assert.Equal("TGL 1/2", lines[0].Text);
            Assert.Equal(ContractObjectiveState.Pending, lines[0].State);
        }

        [Fact]
        public void BuildLines_TglCompleto_DoneSemMarcador()
        {
            var missions = new List<Mission> { new() { RequiredTouchAndGoes = 2 } };
            var lines = ContractProgressFormatter.BuildLines(missions, new ContractProgressInputs(TouchAndGoCount: 2));
            Assert.Single(lines);
            Assert.Equal("TGL 2/2", lines[0].Text);
            Assert.Equal(ContractObjectiveState.Done, lines[0].State);
        }

        [Fact]
        public void BuildLines_EscalaReabasteceu_Failed()
        {
            var tracker = new MultiStopTracker();
            var data = new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SBDN", Lat = -22.1969, Lon = -49.9261 }
                }
            };
            tracker.Configure(data, initialIsOnGround: true);
            tracker.Update(isOnGround: false, -23.0, -47.0, 100.0, 50.0, 10.0);
            tracker.Update(isOnGround: false, -22.5, -48.0, 100.0, 35.0, 10.0);
            tracker.Update(isOnGround: true, -22.1969, -49.9261, 1.0, 41.0, 6.0);

            var lines = ContractProgressFormatter.BuildLines(null, new ContractProgressInputs(MultiStop: tracker));
            Assert.Single(lines);
            Assert.Equal("Reabasteceu entre escalas", lines[0].Text);
            Assert.Equal(ContractObjectiveState.Failed, lines[0].State);
        }

        [Fact]
        public void BuildLines_MaisDeTresLinhas_RetornaTodas()
        {
            var missions = new List<Mission> { new() { RequiredTouchAndGoes = 2 } };

            var overfly = new OverflyTracker();
            overfly.ConfigureWaypoints(new[] { new OverflyPoint("WP1", 0, 0, 1000) });

            var multiStop = new MultiStopTracker();
            multiStop.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint> { new() { Icao = "SBDN" } }
            });

            var checkFlight = new CheckFlightTracker();
            checkFlight.Configure(new CheckFlightContractData { TestAltitudeFt = 5000 });

            var inputs = new ContractProgressInputs(
                TouchAndGoCount: 1,
                Overfly: overfly,
                MultiStop: multiStop,
                CheckFlight: checkFlight);

            var lines = ContractProgressFormatter.BuildLines(missions, inputs);
            Assert.Equal(4, lines.Count);
        }

        [Fact]
        public void Build_MaisDeTresLinhas_LimitaATres()
        {
            var missions = new List<Mission> { new() { RequiredTouchAndGoes = 2 } };

            var overfly = new OverflyTracker();
            overfly.ConfigureWaypoints(new[] { new OverflyPoint("WP1", 0, 0, 1000) });

            var multiStop = new MultiStopTracker();
            multiStop.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint> { new() { Icao = "SBDN" } }
            });

            var checkFlight = new CheckFlightTracker();
            checkFlight.Configure(new CheckFlightContractData { TestAltitudeFt = 5000 });

            var inputs = new ContractProgressInputs(
                TouchAndGoCount: 1,
                Overfly: overfly,
                MultiStop: multiStop,
                CheckFlight: checkFlight);

            var result = ContractProgressFormatter.Build(missions, inputs);
            var lines = result.Split('\n');
            Assert.Equal(3, lines.Length);
        }

        [Fact]
        public void FormatHud_RecolocaMarcadores()
        {
            var lines = new List<ContractObjectiveLine>
            {
                new("A", ContractObjectiveState.Done),
                new("B", ContractObjectiveState.Failed),
                new("C", ContractObjectiveState.Pending)
            };

            var formatted = ContractProgressFormatter.FormatHud(lines);
            Assert.Equal("A ✓\n✖ B\nC", formatted);
        }
    }
}
