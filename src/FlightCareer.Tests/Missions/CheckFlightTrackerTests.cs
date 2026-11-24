using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class CheckFlightTrackerTests
    {
        private static CheckFlightContractData CreateTestData()
        {
            return new CheckFlightContractData
            {
                TestAltitudeFt = 4000.0,
                AltitudeToleranceFt = 300.0,
                PowerHoldSeconds = 10.0,
                HeadingHoldSeconds = 15.0,
                HeadingToleranceDeg = 10.0
            };
        }

        [Fact]
        public void SequenciaCompleta_Pistao_CompletaComSucesso()
        {
            var tracker = new CheckFlightTracker();
            tracker.Configure(CreateTestData());

            // 1. Passo 1: Atingir altitude de teste (4000 ft ± 300 ft)
            tracker.Update(isOnGround: false, altitudeMslFt: 4050.0, planeHeadingDeg: 45.0, engineRpm: 2200.0, maxRpm: 2700.0, engineN1: 0.0, engineType: 0.0, simDtSeconds: 2.0);
            Assert.True(tracker.Step1Reached);
            Assert.False(tracker.Step2PowerChecked);

            // 2. Passo 2: Potência máxima por 10 s (RPM = 2600 >= 0.95 * 2700 = 2565)
            tracker.Update(false, 4050.0, 45.0, 2600.0, 2700.0, 0.0, 0.0, 11.0);
            Assert.True(tracker.Step2PowerChecked);
            Assert.False(tracker.Step3HeadingsChecked);

            // 3. Passo 3: Rumos cardinais (N, E, S, W) por 15 s cada dentro da altitude
            // Rumo N (358° ~ 0°)
            tracker.Update(false, 4000.0, 358.0, 2300.0, 2700.0, 0.0, 0.0, 16.0);
            Assert.True(tracker.HeldN);

            // Rumo E (92°)
            tracker.Update(false, 3950.0, 92.0, 2300.0, 2700.0, 0.0, 0.0, 16.0);
            Assert.True(tracker.HeldE);

            // Rumo S (178°)
            tracker.Update(false, 4010.0, 178.0, 2300.0, 2700.0, 0.0, 0.0, 16.0);
            Assert.True(tracker.HeldS);

            // Rumo W (271°)
            tracker.Update(false, 4005.0, 271.0, 2300.0, 2700.0, 0.0, 0.0, 16.0);
            Assert.True(tracker.HeldW);

            Assert.True(tracker.Step3HeadingsChecked);
            Assert.True(tracker.IsCompleted);
            Assert.Null(tracker.GetFailureReason());
        }

        [Fact]
        public void SequenciaCompleta_Turbina_CompletaComSucesso()
        {
            var tracker = new CheckFlightTracker();
            tracker.Configure(CreateTestData());

            // Passo 1
            tracker.Update(false, 4000.0, 50.0, 0.0, 0.0, 70.0, engineType: 1.0, simDtSeconds: 2.0);
            Assert.True(tracker.Step1Reached);

            // Passo 2: Turbina N1 >= 90% por 10 s
            tracker.Update(false, 4000.0, 50.0, 0.0, 0.0, 95.0, 1.0, 10.5);
            Assert.True(tracker.Step2PowerChecked);

            // Passo 3: Rumos
            tracker.Update(false, 4000.0, 0.0, 0.0, 0.0, 80.0, 1.0, 15.5); // N
            tracker.Update(false, 4000.0, 90.0, 0.0, 0.0, 80.0, 1.0, 15.5); // E
            tracker.Update(false, 4000.0, 180.0, 0.0, 0.0, 80.0, 1.0, 15.5); // S
            tracker.Update(false, 4000.0, 270.0, 0.0, 0.0, 80.0, 1.0, 15.5); // W

            Assert.True(tracker.IsCompleted);
        }

        [Fact]
        public void FaltandoRumoW_FalhaCitandoPasso3()
        {
            var tracker = new CheckFlightTracker();
            tracker.Configure(CreateTestData());

            tracker.Update(false, 4000.0, 0.0, 2600.0, 2700.0, 0.0, 0.0, 2.0); // Passo 1
            tracker.Update(false, 4000.0, 0.0, 2600.0, 2700.0, 0.0, 0.0, 11.0); // Passo 2

            // Passo 3: cumpriu N, E, S, mas NÃO cumpriu W
            tracker.Update(false, 4000.0, 0.0, 2300.0, 2700.0, 0.0, 0.0, 16.0); // N
            tracker.Update(false, 4000.0, 90.0, 2300.0, 2700.0, 0.0, 0.0, 16.0); // E
            tracker.Update(false, 4000.0, 180.0, 2300.0, 2700.0, 0.0, 0.0, 16.0); // S

            Assert.False(tracker.IsCompleted);
            string? reason = tracker.GetFailureReason();
            Assert.NotNull(reason);
            Assert.Contains("Passo 3", reason);
            Assert.Contains("W", reason);
        }

        [Fact]
        public void RumosForaDaFaixaDeAltitude_NaoContam()
        {
            var tracker = new CheckFlightTracker();
            tracker.Configure(CreateTestData());

            tracker.Update(false, 4000.0, 0.0, 2600.0, 2700.0, 0.0, 0.0, 2.0); // Passo 1
            tracker.Update(false, 4000.0, 0.0, 2600.0, 2700.0, 0.0, 0.0, 11.0); // Passo 2

            // Rumo N a 5000 ft (fora de 4000 ± 300 ft)
            tracker.Update(false, 5000.0, 0.0, 2300.0, 2700.0, 0.0, 0.0, 20.0);

            Assert.False(tracker.HeldN);
        }

        [Fact]
        public void TempoComSimulationRate4_Conta4x()
        {
            var tracker = new CheckFlightTracker();
            tracker.Configure(CreateTestData());

            tracker.Update(false, 4000.0, 0.0, 2000.0, 2700.0, 0.0, 0.0, 2.0); // Passo 1

            // 3 ticks de 1 segundo real com SimulationRate = 4 -> simDt = 4.0 s por tick (total 12 s simulados)
            tracker.Update(false, 4000.0, 0.0, 2600.0, 2700.0, 0.0, 0.0, 4.0);
            tracker.Update(false, 4000.0, 0.0, 2600.0, 2700.0, 0.0, 0.0, 4.0);
            tracker.Update(false, 4000.0, 0.0, 2600.0, 2700.0, 0.0, 0.0, 4.0);

            // Total 12 s simulados >= PowerHoldSeconds (10 s)
            Assert.True(tracker.Step2PowerChecked);
        }
    }
}
