using System;
using System.Collections.Generic;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class VfrTriangleTests
    {
        [Fact]
        public void SemPontoDeReporte_FalhaSobrevoo()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "navegacao_vfr",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                OverflyWaypoints = "P:Ponto de Reporte VFR@-22.8,-47.1,2.0,3000"
            };

            var multiTracker = new MultiStopTracker();
            multiTracker.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SDCO", Lat = -23.4808, Lon = -47.4897, ElevationFt = 2077 },
                    new() { Icao = "SBDN", Lat = -22.1969, Lon = -49.9261, ElevationFt = 1476 }
                }
            });
            // Cumpre as escalas no tracker
            multiTracker.Update(false, -23.1, -47.0, 100, 40, 10);
            multiTracker.Update(true, -23.4808, -47.4897, 1.0, 35, 6); // SDCO
            multiTracker.Update(false, -23.4808, -47.4897, 100, 34, 6);
            multiTracker.Update(true, -22.1969, -49.9261, 1.0, 30, 6); // SBDN

            var trackerResults = new MissionTrackerResults(
                VisitedWaypoints: new HashSet<string>(), // Ponto NÃO visitado
                MultiStop: multiTracker
            );

            var result = MissionCompletion.Evaluate(mission, "SBJD", trackers: trackerResults);

            Assert.False(result.IsCompleted);
            Assert.Contains("Sobrevoo incompleto", result.Reason);
        }

        [Fact]
        public void Completo_ComPontoDeReporteEParadas_Sucesso()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "navegacao_vfr",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                OverflyWaypoints = "P:Ponto de Reporte VFR@-22.8,-47.1,2.0,3000"
            };

            var multiTracker = new MultiStopTracker();
            multiTracker.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SDCO", Lat = -23.4808, Lon = -47.4897, ElevationFt = 2077 },
                    new() { Icao = "SBDN", Lat = -22.1969, Lon = -49.9261, ElevationFt = 1476 }
                }
            });
            // Cumpre escalas
            multiTracker.Update(false, -23.1, -47.0, 100, 40, 10);
            multiTracker.Update(true, -23.4808, -47.4897, 1.0, 35, 6); // SDCO
            multiTracker.Update(false, -23.4808, -47.4897, 100, 34, 6);
            multiTracker.Update(true, -22.1969, -49.9261, 1.0, 30, 6); // SBDN

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ponto de Reporte VFR" };
            var trackerResults = new MissionTrackerResults(
                VisitedWaypoints: visited,
                MultiStop: multiTracker
            );

            var result = MissionCompletion.Evaluate(mission, "SBJD", trackers: trackerResults);

            Assert.True(result.IsCompleted);
            Assert.Null(result.Reason);
        }

        [Fact]
        public void IdaEVolta_SemEscalaA_FalhaCitandoA()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "navegacao_vfr",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                OverflyWaypoints = "P:Ponto de Reporte VFR@-22.8,-47.1,2.0,3000"
            };

            var multiTracker = new MultiStopTracker();
            multiTracker.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SBSJ", Lat = -23.2289, Lon = -45.8625, ElevationFt = 2120 }
                },
                StopMode = "full_stop"
            });

            // Decola da origem e pousa direto de volta na origem sem visitar SBSJ
            multiTracker.Update(isOnGround: false, -23.1822, -46.9431, 100, 40, 10);
            multiTracker.Update(isOnGround: true, -23.1822, -46.9431, 1.0, 35, 6);

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ponto de Reporte VFR" };
            var trackerResults = new MissionTrackerResults(
                VisitedWaypoints: visited,
                MultiStop: multiTracker
            );

            var result = MissionCompletion.Evaluate(mission, "SBJD", trackers: trackerResults);

            Assert.False(result.IsCompleted);
            Assert.NotNull(result.Reason);
            Assert.Contains("SBSJ", result.Reason);
        }

        [Fact]
        public void Gerador_NQuadrosComSementeFixa_Gera3VariantesComInvariantes()
        {
            var origin = new Airport
            {
                ICAO = "SBJD",
                Name = "Jundiaí",
                Country = "Brazil",
                Tier = 2,
                Latitude = -23.1822,
                Longitude = -46.9431,
                ElevationFt = 2484
            };

            var candidates = new List<AirportCandidate>
            {
                new(origin, new Runway { AirportIdent = "SBJD", LengthFt = 4500, Surface = "ASPH" }),
                new(new Airport { ICAO = "SDCO", Name = "Sorocaba", Country = "Brazil", Tier = 3, Latitude = -23.4808, Longitude = -47.4897, ElevationFt = 2077 }, new Runway { AirportIdent = "SDCO", LengthFt = 5000, Surface = "ASPH" }),
                new(new Airport { ICAO = "SDAI", Name = "Americana", Country = "Brazil", Tier = 3, Latitude = -22.7631, Longitude = -47.2650, ElevationFt = 1800 }, new Runway { AirportIdent = "SDAI", LengthFt = 3600, Surface = "ASPH" }),
                new(new Airport { ICAO = "SDBP", Name = "Bragança", Country = "Brazil", Tier = 3, Latitude = -22.9778, Longitude = -46.5381, ElevationFt = 2800 }, new Runway { AirportIdent = "SDBP", LengthFt = 4000, Surface = "ASPH" }),
                new(new Airport { ICAO = "SBSJ", Name = "São José", Country = "Brazil", Tier = 3, Latitude = -23.2289, Longitude = -45.8625, ElevationFt = 2120 }, new Runway { AirportIdent = "SBSJ", LengthFt = 6000, Surface = "ASPH" })
            };

            var def = MissionCatalog.Shared.Get("navegacao_vfr")!;
            Assert.NotNull(def);
            Assert.Equal(4, def.Profiles.Count);

            var seenVariants = new HashSet<string>();

            // Gera com N sementes fixas para cobrir as 3 variantes
            for (int seed = 1; seed <= 50; seed++)
            {
                var rnd = new Random(seed);
                var mission = MissionBoardCore.CreateSingleMission(
                    origin,
                    def,
                    candidates,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "instrucao", "aeroclube" },
                    _ => new HashSet<string>(),
                    rnd,
                    DateTime.Now);

                if (mission == null) continue;

                // Todas voltam à origem e têm 1 pax
                Assert.Equal(origin.ICAO, mission.DestinationICAO);
                Assert.Equal(origin.ICAO, mission.OriginICAO);
                Assert.Equal(1, mission.PassengerCount);

                // Ponto de reporte P:
                Assert.NotNull(mission.OverflyWaypoints);
                Assert.StartsWith("P:", mission.OverflyWaypoints);

                // Textos sem chaves residuais {}
                Assert.DoesNotContain("{", mission.Title);
                Assert.DoesNotContain("}", mission.Title);
                Assert.DoesNotContain("{", mission.Description);
                Assert.DoesNotContain("}", mission.Description);

                var contract = System.Text.Json.JsonSerializer.Deserialize<MultiStopContractData>(mission.ContractData!);
                Assert.NotNull(contract);

                if (contract!.Stops.Count == 1)
                {
                    // out_and_back
                    seenVariants.Add("out_and_back");
                    Assert.Equal("full_stop", contract.StopMode);
                    Assert.True(mission.DistanceNM >= 80.0 && mission.DistanceNM <= 240.0, $"Distância out_and_back fora de faixa: {mission.DistanceNM}");

                    // Etapa única (origem -> A): distância = metade do total
                    double legDist = mission.DistanceNM / 2.0;
                    Assert.True(legDist >= 40.0 && legDist <= 120.0, $"Etapa out_and_back fora da faixa 40-120: {legDist}");
                }
                else if (contract.Stops.Count == 2)
                {
                    if (contract.StopMode == "touch_and_go")
                    {
                        // triangle_tgl
                        seenVariants.Add("triangle_tgl");
                        Assert.True(mission.DistanceNM >= 75.0 && mission.DistanceNM <= 200.0, $"Distância triangle_tgl fora de faixa: {mission.DistanceNM}");
                    }
                    else
                    {
                        Assert.Equal("full_stop", contract.StopMode);
                        if (mission.Title.Contains("Longa"))
                        {
                            seenVariants.Add("triangle_long");
                            Assert.True(mission.DistanceNM >= 150.0 && mission.DistanceNM <= 300.0, $"Distância triangle_long fora de faixa: {mission.DistanceNM}");
                        }
                        else
                        {
                            seenVariants.Add("triangle");
                            Assert.True(mission.DistanceNM >= 80.0 && mission.DistanceNM <= 250.0, $"Distância triangle fora de faixa: {mission.DistanceNM}");
                        }
                    }
                }
            }

            // As 3 variantes devem ter sido geradas
            Assert.Contains("triangle", seenVariants);
            Assert.Contains("out_and_back", seenVariants);
            Assert.Contains("triangle_tgl", seenVariants);
        }
    }
}
