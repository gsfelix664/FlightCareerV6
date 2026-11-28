using System;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class StolLandingTrackerTests
    {
        private static StolContractData CreateTestData()
        {
            return new StolContractData
            {
                StripIcao = "FAZ1",
                Lat = -22.5000,
                Lon = -48.0000,
                ElevationFt = 1500.0,
                RunwayLengthFt = 1000.0,
                RequiredLandings = 3,
                MaxStopDistanceFt = 600.0,
                MinApproachAngleDeg = 4.0,
                CrosswindBonusKt = 8.0
            };
        }

        [Fact]
        public void ParadaCurta_A250FtComAngulo5Deg_ContaPousoValido()
        {
            var tracker = new StolLandingTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // 1. Decola e aproxima nos últimos 300 ft AGL (Elevation 1500 -> MSL 1700 ft)
            // GS = 50 kt (50 * 101.2686 = 5063.4 ft/min), VS = -450 fpm -> atan(450 / 5063.4) = 5.08°
            tracker.Update(isOnGround: false, -22.5000, -48.0000, 1700.0, groundSpeedKt: 50.0, verticalSpeedFpm: -450.0, crosswindKt: 4.0, simDtSeconds: 2.0);

            // 2. Toque na pista
            tracker.Update(isOnGround: true, -22.5000, -48.0000, 1500.0, groundSpeedKt: 35.0, verticalSpeedFpm: -100.0, crosswindKt: 4.0, simDtSeconds: 1.0);

            // 3. Parada completa em ~250 ft do ponto do toque (250 ft = 0.0411 NM = ~0.000685° lat)
            double stopLat = -22.5000 + (250.0 / 6076.11549 / 60.0);
            tracker.Update(isOnGround: true, stopLat, -48.0000, 1500.0, groundSpeedKt: 1.0, verticalSpeedFpm: 0.0, crosswindKt: 4.0, simDtSeconds: 5.5);

            Assert.Equal(1, tracker.ValidLandingsCount);
            Assert.False(tracker.HadCrosswindBonus);
        }

        [Fact]
        public void ParadaLonga_A1200Ft_NaoConta()
        {
            var tracker = new StolLandingTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // Aproximação a 5°
            tracker.Update(isOnGround: false, -22.5000, -48.0000, 1700.0, 50.0, -450.0, 3.0, 2.0);

            // Toque
            tracker.Update(isOnGround: true, -22.5000, -48.0000, 1500.0, 40.0, -100.0, 3.0, 1.0);

            // Parada em 1200 ft (> MaxStopDistanceFt = 600 ft)
            double stopLat = -22.5000 + (1200.0 / 6076.11549 / 60.0);
            tracker.Update(isOnGround: true, stopLat, -48.0000, 1500.0, 1.0, 0.0, 3.0, 5.5);

            Assert.Equal(0, tracker.ValidLandingsCount);
        }

        [Fact]
        public void AnguloBaixo_A3Deg_NaoConta()
        {
            var tracker = new StolLandingTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // GS = 70 kt (7090 ft/min), VS = -350 fpm -> atan(350/7090) = 2.82° (< 4.0°)
            tracker.Update(isOnGround: false, -22.5000, -48.0000, 1700.0, 70.0, -350.0, 3.0, 2.0);

            // Toque
            tracker.Update(isOnGround: true, -22.5000, -48.0000, 1500.0, 35.0, -50.0, 3.0, 1.0);

            // Parada a 200 ft
            double stopLat = -22.5000 + (200.0 / 6076.11549 / 60.0);
            tracker.Update(isOnGround: true, stopLat, -48.0000, 1500.0, 1.0, 0.0, 3.0, 5.5);

            Assert.Equal(0, tracker.ValidLandingsCount);
        }

        [Fact]
        public void TresValidos_MaisPousoNaOrigem_CompletaComSucesso()
        {
            var tracker = new StolLandingTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            for (int i = 0; i < 3; i++)
            {
                // Aproximação íngreme a ~5°
                tracker.Update(false, -22.5000, -48.0000, 1700.0, 50.0, -450.0, 5.0, 2.0);
                // Toque
                tracker.Update(true, -22.5000, -48.0000, 1500.0, 30.0, -100.0, 5.0, 1.0);
                // Parada a 250 ft
                double stopLat = -22.5000 + (250.0 / 6076.11549 / 60.0);
                tracker.Update(true, stopLat, -48.0000, 1500.0, 1.0, 0.0, 5.0, 5.5);

                if (i < 2)
                {
                    // Decola novamente (ar >= 5 s)
                    tracker.Update(false, -22.5000, -48.0000, 2200.0, 70.0, 500.0, 5.0, 6.0);
                }
            }

            Assert.Equal(3, tracker.ValidLandingsCount);
            Assert.True(tracker.IsCompleted);

            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "instrucao_stol",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                ContractData = System.Text.Json.JsonSerializer.Serialize(data)
            };

            var res = MissionCompletion.Evaluate(mission, "SBJD", trackers: new MissionTrackerResults(Stol: tracker));
            Assert.True(res.IsCompleted);
        }

        [Fact]
        public void TravesForte_10Kt_GeraWarning()
        {
            var tracker = new StolLandingTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // Aproximação com através de 10 kt (>= 8.0 kt)
            tracker.Update(false, -22.5000, -48.0000, 1700.0, 50.0, -450.0, 10.0, 2.0);
            tracker.Update(true, -22.5000, -48.0000, 1500.0, 30.0, -100.0, 10.0, 1.0);
            double stopLat = -22.5000 + (250.0 / 6076.11549 / 60.0);
            tracker.Update(true, stopLat, -48.0000, 1500.0, 1.0, 0.0, 10.0, 5.5);

            Assert.Equal(1, tracker.ValidLandingsCount);
            Assert.True(tracker.HadCrosswindBonus);

            var data1Landing = CreateTestData();
            data1Landing.RequiredLandings = 1;
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "instrucao_stol",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                ContractData = System.Text.Json.JsonSerializer.Serialize(data1Landing)
            };

            var res = MissionCompletion.Evaluate(mission, "SBJD", trackers: new MissionTrackerResults(Stol: tracker));
            Assert.True(res.IsCompleted);
            Assert.NotNull(res.Warning);
            Assert.Contains("vento de través", res.Warning);
        }
    }
}
