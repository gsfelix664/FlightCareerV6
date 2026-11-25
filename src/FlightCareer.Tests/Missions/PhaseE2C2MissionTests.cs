using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Application.Services;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class PhaseE2C2MissionTests
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private static Airport CreateTestAirport(string icao = "SBMT", double lat = -23.50, double lon = -46.63, double elev = 2369.0, int tier = 2)
        {
            return new Airport
            {
                ICAO = icao,
                Name = $"Aeroporto {icao}",
                Country = "Brazil",
                Latitude = lat,
                Longitude = lon,
                ElevationFt = (int)elev,
                Tier = tier
            };
        }

        [Fact]
        public void PatrolGenerator_GeraContratoCorretoETextosSemPlaceholders()
        {
            var origin = CreateTestAirport("SBMT", -23.50, -46.63, 2369.0);
            var def = MissionCatalog.Shared.Get("patrulha_vigilancia");
            Assert.NotNull(def);

            var random = new Random(42);
            var mission = MissionGenerator.CreatePatrolMissionSync(
                origin, def!, random, DateTime.UtcNow, 1.0, null, null, null, 1, 0, def!.Profiles[0]);

            Assert.NotNull(mission);
            Assert.InRange(mission!.DistanceNM, 30.0, 200.0);
            Assert.Equal(origin.ICAO, mission.OriginICAO);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);

            // Textos sem {}
            Assert.DoesNotContain("{", mission.Title);
            Assert.DoesNotContain("}", mission.Title);
            Assert.DoesNotContain("{", mission.Description);
            Assert.DoesNotContain("}", mission.Description);

            // Contrato desserializa
            Assert.False(string.IsNullOrWhiteSpace(mission.ContractData));
            var contract = JsonSerializer.Deserialize<AerialWorkContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.Equal("sectors", contract!.Mode);
            Assert.InRange(contract.Points.Count, 3, 5);
            Assert.Equal(1.0, contract.PointRadiusNm);
            Assert.Equal(1500.0, contract.MaxAglFt);
            Assert.Equal(130.0, contract.MaxIasKt);
        }

        [Fact]
        public void LineInspectionGenerator_RespeitaVariacaoDeRumoETextosSemPlaceholders()
        {
            var origin = CreateTestAirport("SBDN", -22.19, -49.92, 2123.0);
            var def = MissionCatalog.Shared.Get("inspecao_linhas");
            Assert.NotNull(def);

            var random = new Random(1234);
            var mission = MissionGenerator.CreateLineInspectionMissionSync(
                origin, def!, random, DateTime.UtcNow, 1.0, null, null, 1, 0, def!.Profiles[0]);

            Assert.NotNull(mission);
            Assert.InRange(mission!.DistanceNM, 40.0, 200.0);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);

            // Textos sem {}
            Assert.DoesNotContain("{", mission.Title);
            Assert.DoesNotContain("}", mission.Title);
            Assert.DoesNotContain("{", mission.Description);
            Assert.DoesNotContain("}", mission.Description);

            var contract = JsonSerializer.Deserialize<AerialWorkContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.Equal("route", contract!.Mode);
            Assert.InRange(contract.Points.Count, 6, 10);
            Assert.Equal(0.5, contract.PointRadiusNm);
            Assert.Equal(1000.0, contract.MaxAglFt);

            // Checar variação de rumo <= +-25 graus entre trechos consecutivos
            for (int i = 0; i < contract.Points.Count - 2; i++)
            {
                var p1 = contract.Points[i];
                var p2 = contract.Points[i + 1];
                var p3 = contract.Points[i + 2];

                double h1 = CalculateHeading(p1.Lat, p1.Lon, p2.Lat, p2.Lon);
                double h2 = CalculateHeading(p2.Lat, p2.Lon, p3.Lat, p3.Lon);
                double diff = Math.Abs((h2 - h1 + 540.0) % 360.0 - 180.0);
                Assert.True(diff <= 25.5, $"Variação de rumo entre {p1.Ident}-{p2.Ident} e {p2.Ident}-{p3.Ident} excedeu 25°: {diff:F1}°");
            }
        }

        [Fact]
        public void TrafficOrbitGenerator_GeraContratoCorretoETextosSemPlaceholders()
        {
            var origin = CreateTestAirport("SBGR", -23.43, -46.47, 2461.0);
            var def = MissionCatalog.Shared.Get("monitoramento_trafego");
            Assert.NotNull(def);

            var random = new Random(777);
            var mission = MissionGenerator.CreateTrafficOrbitMissionSync(
                origin, def!, random, DateTime.UtcNow, 1.0, null, null, null, 2, 30, def!.Profiles[0]);

            Assert.NotNull(mission);
            Assert.InRange(mission!.DistanceNM, 10.0, 40.0);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);

            // Textos sem {}
            Assert.DoesNotContain("{", mission.Title);
            Assert.DoesNotContain("}", mission.Title);
            Assert.DoesNotContain("{", mission.Description);
            Assert.DoesNotContain("}", mission.Description);

            var contract = JsonSerializer.Deserialize<AerialWorkContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.Equal("orbit", contract!.Mode);
            Assert.InRange(contract.OrbitMinutes, 10.0, 20.0);
            Assert.Equal(1.5, contract.OrbitRadiusNm);
            Assert.Equal(1000.0, contract.MinAglFt);
            Assert.Equal(2500.0, contract.MaxAglFt);
        }

        [Fact]
        public void SarGenerator_GeraDuasVariantesEAlvoPertoDaPerna()
        {
            var origin = CreateTestAirport("SBSP", -23.62, -46.65, 2631.0);
            var def = MissionCatalog.Shared.Get("sar");
            Assert.NotNull(def);

            bool sawSquare = false;
            bool sawCreeping = false;

            for (int seed = 1; seed <= 50; seed++)
            {
                var random = new Random(seed);
                var profile = def!.Profiles[seed % def.Profiles.Count];
                var mission = MissionGenerator.CreateSarMissionSync(
                    origin, def, random, DateTime.UtcNow, 1.0, null, null, 2, 0, profile);

                if (mission == null) continue;

                Assert.InRange(mission.DistanceNM, 40.0, 250.0);
                Assert.DoesNotContain("{", mission.Title);
                Assert.DoesNotContain("}", mission.Title);
                Assert.DoesNotContain("{", mission.Description);
                Assert.DoesNotContain("}", mission.Description);

                var contract = JsonSerializer.Deserialize<AerialWorkContractData>(mission.ContractData!);
                Assert.NotNull(contract);
                Assert.Equal("search", contract!.Mode);
                Assert.Equal(0.5, contract.PointRadiusNm);
                Assert.Equal(0.3, contract.TargetRadiusNm);
                Assert.Equal(1500.0, contract.MaxAglFt);
                Assert.Equal(160.0, contract.MaxIasKt);

                if (profile.Variant == "expanding_square") sawSquare = true;
                if (profile.Variant == "creeping_line") sawCreeping = true;

                // Alvo a <= 0.15 NM de uma perna >= 2
                double minDistanceToLeg = double.MaxValue;
                if (profile.Variant == "expanding_square")
                {
                    for (int i = 0; i < contract.Points.Count - 1; i++)
                    {
                        var pA = contract.Points[i];
                        var pB = contract.Points[i + 1];
                        double dist = AerialWorkTracker.CalculateCrossTrackDistance(
                            pA.Lat, pA.Lon, pB.Lat, pB.Lon, contract.TargetLat, contract.TargetLon);
                        if (dist < minDistanceToLeg) minDistanceToLeg = dist;
                    }
                }
                else
                {
                    // Em creeping line, a direção transversal é a reta Datum -> P1
                    double hTrans = CalculateHeading(
                        contract.OrbitCenterLat, contract.OrbitCenterLon,
                        contract.Points[0].Lat, contract.Points[0].Lon);

                    for (int i = 1; i < contract.Points.Count; i++)
                    {
                        var pEnd = contract.Points[i];
                        // Pernas pares (2, 4...) têm rumo hTrans + 180, pernas ímpares têm rumo hTrans
                        double legHeading = ((i + 1) % 2 == 0) ? (hTrans + 180.0) % 360.0 : hTrans;
                        var (pStartLat, pStartLon) = PointFrom(pEnd.Lat, pEnd.Lon, 5.0, (legHeading + 180.0) % 360.0);

                        double dist = AerialWorkTracker.CalculateCrossTrackDistance(
                            pStartLat, pStartLon, pEnd.Lat, pEnd.Lon, contract.TargetLat, contract.TargetLon);
                        if (dist < minDistanceToLeg) minDistanceToLeg = dist;
                    }
                }
                Assert.True(minDistanceToLeg <= 0.16, $"Distância do alvo à perna excedeu 0.15 NM: {minDistanceToLeg:F3} NM");
            }

            Assert.True(sawSquare, "SAR deve gerar expanding_square");
            Assert.True(sawCreeping, "SAR deve gerar creeping_line");
        }

        [Fact]
        public void RoundTripGenerator_GeraMultiStopIdaEVoltaCorreto()
        {
            var origin = CreateTestAirport("SBMT", -23.50, -46.63, 2369.0, 2);
            var dest = CreateTestAirport("SDCO", -22.50, -46.00, 2500.0, 3);
            var def = MissionCatalog.Shared.Get("saude_familia");
            Assert.NotNull(def);

            var random = new Random(999);
            double idaDist = 75.0;
            var mission = MissionGenerator.CreateRoundTripMissionSync(
                origin, dest, idaDist, def!, random, DateTime.UtcNow, 1.0, null, null, 4, 300.0);

            Assert.NotNull(mission);
            Assert.Equal(150.0, mission!.DistanceNM);
            Assert.Equal(origin.ICAO, mission.OriginICAO);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);

            Assert.DoesNotContain("{", mission.Title);
            Assert.DoesNotContain("}", mission.Title);
            Assert.DoesNotContain("{", mission.Description);
            Assert.DoesNotContain("}", mission.Description);

            var contract = JsonSerializer.Deserialize<MultiStopContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.False(contract!.RequireNoRefuel);
            Assert.Equal("full_stop", contract.StopMode);
            Assert.Single(contract.Stops);
            Assert.Equal(dest.ICAO, contract.Stops[0].Icao);
        }

        [Fact]
        public void MissionCompletion_AerialWorkIncompleto_FalhaComMotivo()
        {
            var origin = CreateTestAirport("SBMT");
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "patrulha_vigilancia",
                OriginICAO = origin.ICAO,
                DestinationICAO = origin.ICAO
            };

            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint> { new() { Ident = "S1", Lat = 0, Lon = 0 } },
                RefElevationFt = 1000,
                MaxAglFt = 1500,
                MaxIasKt = 130
            };
            tracker.Configure(data);

            var trackers = new MissionTrackerResults(AerialWork: tracker);
            var result = MissionCompletion.Evaluate(mission, origin.ICAO, trackers: trackers);

            Assert.False(result.IsCompleted);
            Assert.Contains("Setores sem cobertura: S1", result.Reason);
        }

        [Fact]
        public void MissionCompletion_AerialWorkCompleto_Aprova()
        {
            var origin = CreateTestAirport("SBMT");
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "patrulha_vigilancia",
                OriginICAO = origin.ICAO,
                DestinationICAO = origin.ICAO
            };

            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint> { new() { Ident = "S1", Lat = -23.0, Lon = -46.0 } },
                RefElevationFt = 1000,
                MaxAglFt = 1500,
                MaxIasKt = 130
            };
            tracker.Configure(data);
            tracker.Update(false, -23.0, -46.0, 1500.0, 100.0, 1.0);

            Assert.True(tracker.IsCompleted);

            var trackers = new MissionTrackerResults(AerialWork: tracker);
            var result = MissionCompletion.Evaluate(mission, origin.ICAO, trackers: trackers);

            Assert.True(result.IsCompleted);
            Assert.Null(result.Reason);
        }

        [Fact]
        public void MissionCompletion_RoundTripSemEscala_FalhaCitandoIcao()
        {
            var origin = CreateTestAirport("SBMT");
            var dest = CreateTestAirport("SDCO");
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "saude_familia",
                OriginICAO = origin.ICAO,
                DestinationICAO = origin.ICAO
            };

            var multiStop = new MultiStopTracker();
            multiStop.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint> { new() { Icao = dest.ICAO, Lat = dest.Latitude, Lon = dest.Longitude } },
                StopMode = "full_stop"
            }, true);

            var trackers = new MissionTrackerResults(MultiStop: multiStop);
            var result = MissionCompletion.Evaluate(mission, origin.ICAO, trackers: trackers);

            Assert.False(result.IsCompleted);
            Assert.Contains(dest.ICAO, result.Reason);
        }

        [Fact]
        public void ContractProgressFormatter_FormataLinhaCorretaPorModo()
        {
            // 1. sectors incompleto
            var tracker = new AerialWorkTracker();
            tracker.Configure(new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "S1", Lat = 0, Lon = 0 },
                    new() { Ident = "S2", Lat = 0, Lon = 0 },
                    new() { Ident = "S3", Lat = 0, Lon = 0.03 },
                    new() { Ident = "S4", Lat = 0, Lon = 0.5 }
                },
                RefElevationFt = 600,
                MaxAglFt = 1500,
                MaxIasKt = 130
            });
            // Marca S1 e S2
            tracker.Update(false, 0, 0, 1500, 100, 1);
            // Agora aeronave a 1.8 NM de S3 e acima do teto (2200 ft > 2100 ft)
            tracker.Update(false, 0, 0.03 - (1.8 / 60.0), 2200, 100, 1);

            string lineSectors = ContractProgressFormatter.Build(null, new ContractProgressInputs(AerialWork: tracker));
            Assert.Contains("Setores 2/4 · S3 1,8 NM · máx. 2.100 ft: acima", lineSectors);

            // 2. route
            var trackerRoute = new AerialWorkTracker();
            trackerRoute.Configure(new AerialWorkContractData
            {
                Mode = "route",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "L1", Lat = 0, Lon = 0 },
                    new() { Ident = "L2", Lat = 0, Lon = 0.1 },
                    new() { Ident = "L3", Lat = 0, Lon = 0.2 },
                    new() { Ident = "L4", Lat = 0, Lon = 0.3 }
                },
                RefElevationFt = 1000,
                MaxAglFt = 1000
            });
            // Completa L1, L2, L3
            trackerRoute.Update(false, 0, 0, 1500, 100, 1);
            trackerRoute.Update(false, 0, 0.1, 1500, 100, 1);
            trackerRoute.Update(false, 0, 0.2, 1500, 100, 1);
            // Aeronave a 2.4 NM de L4 com desvio 0.3 NM
            // L3 em (0, 0.2), L4 em (0, 0.3). Aeronave em (0.3/60, 0.3 - 2.4/60)
            double latDev = 0.3 / 60.0;
            double lonDev = 0.3 - (2.4 / 60.0);
            trackerRoute.Update(false, latDev, lonDev, 1500, 100, 1);

            string lineRoute = ContractProgressFormatter.Build(null, new ContractProgressInputs(AerialWork: trackerRoute));
            Assert.Contains("Traçado 3/4 · L4 2,4 NM · desvio 0,3 NM", lineRoute);

            // 3. orbit
            var trackerOrbit = new AerialWorkTracker();
            trackerOrbit.Configure(new AerialWorkContractData
            {
                Mode = "orbit",
                OrbitCenterLat = 0,
                OrbitCenterLon = 0,
                OrbitRadiusNm = 1.5,
                OrbitMinutes = 15,
                RefElevationFt = 1000,
                MinAglFt = 1000,
                MaxAglFt = 2500
            });
            // Acumula 6 minutos válidos (360 s) a 1.2 NM do centro
            double lonOrbit = 1.2 / 60.0;
            trackerOrbit.Update(false, 0, lonOrbit, 2000, 100, 360.0);

            string lineOrbit = ContractProgressFormatter.Build(null, new ContractProgressInputs(AerialWork: trackerOrbit));
            Assert.Contains("Órbita 6/15 min · 1,2 NM", lineOrbit);

            // 4. search antes e depois do alvo
            var trackerSearch = new AerialWorkTracker();
            trackerSearch.Configure(new AerialWorkContractData
            {
                Mode = "search",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "P1", Lat = 0, Lon = 0 },
                    new() { Ident = "P2", Lat = 0, Lon = 0.1 },
                    new() { Ident = "P3", Lat = 0, Lon = 0.2 },
                    new() { Ident = "P4", Lat = 0, Lon = 0.3 },
                    new() { Ident = "P5", Lat = 0, Lon = 0.4 },
                    new() { Ident = "P6", Lat = 0, Lon = 0.5 },
                    new() { Ident = "P7", Lat = 0, Lon = 0.6 },
                    new() { Ident = "P8", Lat = 0, Lon = 0.7 }
                },
                TargetLat = 1.0,
                TargetLon = 1.0,
                TargetRadiusNm = 0.3,
                RefElevationFt = 1000,
                MaxAglFt = 1500,
                MaxIasKt = 160
            });
            // Completa P1 e P2 -> perna 3
            trackerSearch.Update(false, 0, 0, 1500, 100, 1);
            trackerSearch.Update(false, 0, 0.1, 1500, 100, 1);

            string lineSearch1 = ContractProgressFormatter.Build(null, new ContractProgressInputs(AerialWork: trackerSearch));
            Assert.Contains("Busca: perna 3/8 · alvo não achado", lineSearch1);

            // Passa pelo alvo
            trackerSearch.Update(false, 1.0, 1.0, 1500, 100, 1);
            string lineSearch2 = ContractProgressFormatter.Build(null, new ContractProgressInputs(AerialWork: trackerSearch));
            Assert.Equal("Alvo encontrado ✓", lineSearch2);
        }

        [Fact]
        public void ContractMarkerBuilder_ContratoSarNaoGeraMarcadorNoAlvo()
        {
            var data = new AerialWorkContractData
            {
                Mode = "search",
                OrbitCenterLat = -23.00,
                OrbitCenterLon = -46.00, // Datum
                Points = new List<WorkPoint>
                {
                    new() { Ident = "P1", Lat = -23.01, Lon = -46.01 },
                    new() { Ident = "P2", Lat = -23.02, Lon = -46.02 }
                },
                TargetLat = -23.015,
                TargetLon = -46.015, // Alvo secreto
                TargetRadiusNm = 0.3,
                TargetName = "aeronave desaparecida"
            };

            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                ContractData = JsonSerializer.Serialize(data)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { mission });

            Assert.NotEmpty(visuals.Markers);
            // Verifica se nenhum marcador possui as coordenadas ou nome do alvo
            foreach (var marker in visuals.Markers)
            {
                Assert.NotEqual("aeronave desaparecida", marker.Label);
                Assert.False(
                    Math.Abs(marker.Lat - data.TargetLat) < 0.0001 &&
                    Math.Abs(marker.Lon - data.TargetLon) < 0.0001,
                    "Marcador do mapa não pode conter a coordenada do alvo SAR!");
            }
        }

        private static double CalculateHeading(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double midLat = (lat1 + lat2) / 2.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0 * Math.Cos(midLat * Math.PI / 180.0);
            double heading = Math.Atan2(dLon, dLat) * 180.0 / Math.PI;
            return (heading + 360.0) % 360.0;
        }

        private static (double Lat, double Lon) PointFrom(double lat, double lon, double distanceNm, double headingDeg)
        {
            double rad = headingDeg * Math.PI / 180.0;
            double dLat = (distanceNm / 60.0) * Math.Cos(rad);
            double newLat = lat + dLat;
            double midLat = (lat + newLat) / 2.0;
            double cosLat = Math.Max(0.01, Math.Cos(midLat * Math.PI / 180.0));
            double dLon = (distanceNm / (60.0 * cosLat)) * Math.Sin(rad);
            return (newLat, lon + dLon);
        }
    }
}
