using System;
using System.Collections.Generic;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class AerialWorkTrackerTests
    {
        [Fact]
        public void Sectores_VisitadosForaDeOrdem_ConcluiComSucesso()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "S1", Lat = -23.00, Lon = -46.00 },
                    new() { Ident = "S2", Lat = -23.05, Lon = -46.05 },
                    new() { Ident = "S3", Lat = -23.10, Lon = -46.10 }
                },
                PointRadiusNm = 1.0,
                RefElevationFt = 2000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1500.0,
                MaxIasKt = 130.0
            };
            tracker.Configure(data);

            // Visitar S3 primeiro
            tracker.Update(false, -23.10, -46.10, 2500.0, 110.0, 1.0);
            Assert.False(tracker.IsCompleted);
            Assert.Equal(1, tracker.CompletedPoints);

            // Visitar S1
            tracker.Update(false, -23.00, -46.00, 2800.0, 120.0, 1.0);
            Assert.False(tracker.IsCompleted);
            Assert.Equal(2, tracker.CompletedPoints);

            // Visitar S2
            tracker.Update(false, -23.05, -46.05, 3000.0, 125.0, 1.0);
            Assert.True(tracker.IsCompleted);
            Assert.Equal(3, tracker.CompletedPoints);
            Assert.Null(tracker.FailureReason);
        }

        [Fact]
        public void Sectores_AcimaDoTeto_NaoConta()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "S1", Lat = -23.00, Lon = -46.00 }
                },
                PointRadiusNm = 1.0,
                RefElevationFt = 2000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1500.0, // teto = 3500 MSL
                MaxIasKt = 130.0
            };
            tracker.Configure(data);

            // 3600 ft está acima do teto (3500 ft)
            tracker.Update(false, -23.00, -46.00, 3600.0, 110.0, 1.0);
            Assert.False(tracker.IsCompleted);
            Assert.Equal(0, tracker.CompletedPoints);
            Assert.Equal(AerialWorkAltitudeState.Above, tracker.AltitudeState);
        }

        [Fact]
        public void Sectores_IasAcimaDoLimite_NaoConta()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "S1", Lat = -23.00, Lon = -46.00 }
                },
                PointRadiusNm = 1.0,
                RefElevationFt = 2000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1500.0,
                MaxIasKt = 130.0
            };
            tracker.Configure(data);

            // IAS 135 kt ultrapassa limite de 130 kt
            tracker.Update(false, -23.00, -46.00, 3000.0, 135.0, 1.0);
            Assert.False(tracker.IsCompleted);
            Assert.Equal(0, tracker.CompletedPoints);
            Assert.True(tracker.IsIasExceeded);
        }

        [Fact]
        public void Sectores_NoSolo_NaoConta()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "S1", Lat = -23.00, Lon = -46.00 }
                },
                PointRadiusNm = 1.0,
                RefElevationFt = 2000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1500.0,
                MaxIasKt = 130.0
            };
            tracker.Configure(data);

            tracker.Update(true, -23.00, -46.00, 2000.0, 50.0, 1.0);
            Assert.False(tracker.IsCompleted);
            Assert.Equal(0, tracker.CompletedPoints);
        }

        [Fact]
        public void Sectores_FalhaListaSetoresFaltantes()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "sectors",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "S1", Lat = -23.00, Lon = -46.00 },
                    new() { Ident = "S2", Lat = -23.05, Lon = -46.05 },
                    new() { Ident = "S3", Lat = -23.10, Lon = -46.10 },
                    new() { Ident = "S4", Lat = -23.15, Lon = -46.15 }
                },
                PointRadiusNm = 1.0,
                RefElevationFt = 600.0,
                MinAglFt = 0.0,
                MaxAglFt = 1500.0,
                MaxIasKt = 130.0
            };
            tracker.Configure(data);

            // Visitar S1 e S3
            tracker.Update(false, -23.00, -46.00, 1500.0, 110.0, 1.0);
            tracker.Update(false, -23.10, -46.10, 1500.0, 110.0, 1.0);

            Assert.False(tracker.IsCompleted);
            Assert.NotNull(tracker.FailureReason);
            Assert.Contains("Setores sem cobertura: S2, S4", tracker.FailureReason);
            Assert.Contains("voe a até 2.100 ft e 130 kt", tracker.FailureReason);
        }

        [Fact]
        public void Route_PontosNaOrdem_ConcluiComSucesso()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "route",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "L1", Lat = -23.00, Lon = -46.00 },
                    new() { Ident = "L2", Lat = -23.00, Lon = -46.10 },
                    new() { Ident = "L3", Lat = -23.00, Lon = -46.20 }
                },
                PointRadiusNm = 0.5,
                RefElevationFt = 1000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1000.0,
                MaxIasKt = 0.0
            };
            tracker.Configure(data);

            // Visitar L1
            tracker.Update(false, -23.00, -46.00, 1500.0, 90.0, 1.0);
            Assert.Equal(1, tracker.CompletedPoints);

            // Visitar L2
            tracker.Update(false, -23.00, -46.10, 1500.0, 90.0, 1.0);
            Assert.Equal(2, tracker.CompletedPoints);

            // Visitar L3
            tracker.Update(false, -23.00, -46.20, 1500.0, 90.0, 1.0);
            Assert.True(tracker.IsCompleted);
            Assert.Equal(3, tracker.CompletedPoints);
            Assert.Null(tracker.FailureReason);
        }

        [Fact]
        public void Route_PassarPeloL3AntesDoL2_NaoContaAteL2()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "route",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "L1", Lat = -23.00, Lon = -46.00 },
                    new() { Ident = "L2", Lat = -23.00, Lon = -46.10 },
                    new() { Ident = "L3", Lat = -23.00, Lon = -46.20 }
                },
                PointRadiusNm = 0.5,
                RefElevationFt = 1000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1000.0,
                MaxIasKt = 0.0
            };
            tracker.Configure(data);

            // Visitar L1
            tracker.Update(false, -23.00, -46.00, 1500.0, 90.0, 1.0);
            Assert.Equal(1, tracker.CompletedPoints);

            // Passar sobre L3 antes de L2
            tracker.Update(false, -23.00, -46.20, 1500.0, 90.0, 1.0);
            Assert.Equal(1, tracker.CompletedPoints);
            Assert.False(tracker.IsCompleted);
            Assert.Equal("L2", tracker.NextPointIdent);

            // Agora visita L2
            tracker.Update(false, -23.00, -46.10, 1500.0, 90.0, 1.0);
            Assert.Equal(2, tracker.CompletedPoints);
            Assert.Equal("L3", tracker.NextPointIdent);

            // E finalmente L3
            tracker.Update(false, -23.00, -46.20, 1500.0, 90.0, 1.0);
            Assert.True(tracker.IsCompleted);
        }

        [Fact]
        public void Route_DesvioLateralCalculado_PontoA04NmDaReta()
        {
            // Ponto A em (0, 0), Ponto B em (0, 10 NM a leste)
            // Aeronave em (0.4 NM ao norte, 5 NM a leste) -> desvio deve ser 0.4 +- 0.05 NM
            double latA = 0.0;
            double lonA = 0.0;
            double latB = 0.0;
            double lonB = 10.0 / 60.0; // 10 NM a leste

            double latC = 0.4 / 60.0; // 0.4 NM ao norte
            double lonC = 5.0 / 60.0; // 5 NM a leste

            double desvio = AerialWorkTracker.CalculateCrossTrackDistance(latA, lonA, latB, lonB, latC, lonC);
            Assert.InRange(desvio, 0.35, 0.45);
        }

        [Fact]
        public void Orbit_TempoSimuladoValido_ConcluiComSucesso()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "orbit",
                OrbitCenterLat = -23.00,
                OrbitCenterLon = -46.00,
                OrbitRadiusNm = 1.5,
                OrbitMinutes = 15.0,
                RefElevationFt = 1000.0,
                MinAglFt = 1000.0,
                MaxAglFt = 2500.0,
                MaxIasKt = 0.0
            };
            tracker.Configure(data);

            // 15 min exigidos = 900 s simulados válidos.
            // 225 pacotes com simDt = 4.0 s (simDt 1 s × rate 4 = 4.0 s por tick)
            for (int i = 0; i < 225; i++)
            {
                tracker.Update(false, -23.00, -46.01, 2500.0, 100.0, 4.0);
            }

            Assert.True(tracker.IsCompleted);
            Assert.Equal(900.0, tracker.OrbitSecondsAccumulated);
            Assert.Null(tracker.FailureReason);
        }

        [Fact]
        public void Orbit_AmostrasForaDoRaio_NaoSomam()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "orbit",
                OrbitCenterLat = -23.00,
                OrbitCenterLon = -46.00,
                OrbitRadiusNm = 1.5,
                OrbitMinutes = 15.0,
                RefElevationFt = 1000.0,
                MinAglFt = 1000.0,
                MaxAglFt = 2500.0,
                MaxIasKt = 0.0
            };
            tracker.Configure(data);

            // Ponto a ~3 NM do centro (fora de 1.5 NM)
            for (int i = 0; i < 50; i++)
            {
                tracker.Update(false, -23.05, -46.00, 2500.0, 100.0, 1.0);
            }

            Assert.Equal(0.0, tracker.OrbitSecondsAccumulated);
            Assert.False(tracker.IsCompleted);
            Assert.Equal("Órbita de 0 de 15 min", tracker.FailureReason);
        }

        [Fact]
        public void Orbit_AmostrasForaDaFaixa_NaoSomam()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "orbit",
                OrbitCenterLat = -23.00,
                OrbitCenterLon = -46.00,
                OrbitRadiusNm = 1.5,
                OrbitMinutes = 15.0,
                RefElevationFt = 1000.0,
                MinAglFt = 1000.0, // min = 2000 MSL
                MaxAglFt = 2500.0, // max = 3500 MSL
                MaxIasKt = 0.0
            };
            tracker.Configure(data);

            // Altitude 1800 MSL está abaixo da faixa (min = 2000 MSL)
            tracker.Update(false, -23.00, -46.00, 1800.0, 100.0, 60.0);
            Assert.Equal(0.0, tracker.OrbitSecondsAccumulated);

            // Altitude 3600 MSL está acima da faixa (max = 3500 MSL)
            tracker.Update(false, -23.00, -46.00, 3600.0, 100.0, 60.0);
            Assert.Equal(0.0, tracker.OrbitSecondsAccumulated);
            Assert.False(tracker.IsCompleted);
        }

        [Fact]
        public void Search_PassarA02NmDoAlvo_AchadoECompleto()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "search",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "P1", Lat = -23.00, Lon = -46.00 },
                    new() { Ident = "P2", Lat = -23.02, Lon = -46.00 }
                },
                PointRadiusNm = 0.5,
                TargetLat = -23.01,
                TargetLon = -46.00,
                TargetRadiusNm = 0.3,
                TargetName = "aeronave desaparecida",
                RefElevationFt = 1000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1500.0,
                MaxIasKt = 160.0
            };
            tracker.Configure(data);

            // Passar a ~0.2 NM do alvo
            double distNm = 0.2;
            double latNear = -23.01 + (distNm / 60.0);
            tracker.Update(false, latNear, -46.00, 2000.0, 120.0, 1.0);

            Assert.True(tracker.TargetFound);
            Assert.True(tracker.IsCompleted);
            Assert.Null(tracker.FailureReason);
        }

        [Fact]
        public void Search_PassarA05NmDoAlvo_NaoAchaEFalhaCitaPerna()
        {
            var tracker = new AerialWorkTracker();
            var data = new AerialWorkContractData
            {
                Mode = "search",
                Points = new List<WorkPoint>
                {
                    new() { Ident = "P1", Lat = -23.00, Lon = -46.00 },
                    new() { Ident = "P2", Lat = -23.02, Lon = -46.00 },
                    new() { Ident = "P3", Lat = -23.04, Lon = -46.00 }
                },
                PointRadiusNm = 0.5,
                TargetLat = -23.01,
                TargetLon = -46.00,
                TargetRadiusNm = 0.3,
                TargetName = "aeronave desaparecida",
                RefElevationFt = 1000.0,
                MinAglFt = 0.0,
                MaxAglFt = 1500.0,
                MaxIasKt = 160.0
            };
            tracker.Configure(data);

            // Passar P1
            tracker.Update(false, -23.00, -46.00, 2000.0, 120.0, 1.0);
            Assert.Equal(1, tracker.CompletedPoints);

            // Passar a 0.5 NM do alvo (fora de 0.3 NM)
            double latFar = -23.01 + (0.5 / 60.0);
            tracker.Update(false, latFar, -46.00, 2000.0, 120.0, 1.0);

            Assert.False(tracker.TargetFound);
            Assert.False(tracker.IsCompleted);
            Assert.Contains("Alvo não encontrado (perna 2 de 3 do padrão)", tracker.FailureReason);
        }
    }
}
