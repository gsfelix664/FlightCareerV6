using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class FerryRestrictionMonitorTests
    {
        [Fact]
        public void TetoExcedido_5s_Ok()
        {
            var monitor = new FerryRestrictionMonitor();
            monitor.Configure(new FerryPermitContractData
            {
                Restriction = "max_altitude",
                MaxAltitudeFt = 9000.0
            });

            // Decolagem
            monitor.Update(isOnGround: false, altitudeMslFt: 2000.0, flapsPercent: 0.0, simDtSeconds: 1.0);

            // Voando a 9500 ft (> 9000 + 200) por 5 s
            monitor.Update(isOnGround: false, altitudeMslFt: 9500.0, flapsPercent: 0.0, simDtSeconds: 5.0);

            Assert.False(monitor.HasViolation);
            Assert.Equal(5.0, monitor.AltitudeExceedanceSeconds);
        }

        [Fact]
        public void TetoExcedido_15s_Falha()
        {
            var monitor = new FerryRestrictionMonitor();
            monitor.Configure(new FerryPermitContractData
            {
                Restriction = "max_altitude",
                MaxAltitudeFt = 9000.0
            });

            monitor.Update(false, 2000.0, 0.0, 1.0);

            // Voando a 9500 ft por 15 s (> 10 s acumulados)
            monitor.Update(false, 9500.0, 0.0, 15.0);

            Assert.True(monitor.HasViolation);
            Assert.NotNull(monitor.FailureReason);
            Assert.Contains("teto", monitor.FailureReason);
        }

        [Fact]
        public void FlapsDe0Para20Pct_Falha()
        {
            var monitor = new FerryRestrictionMonitor();
            monitor.Configure(new FerryPermitContractData
            {
                Restriction = "flaps_fixed",
                FlapTolerancePct = 5.0
            });

            // Decolagem no solo -> ar com flaps 0%
            monitor.Update(isOnGround: true, 1000.0, flapsPercent: 0.0, simDtSeconds: 1.0);
            monitor.Update(isOnGround: false, 1050.0, flapsPercent: 0.0, simDtSeconds: 1.0);

            // Em voo flaps passam para 20% (> 5% tolerância)
            monitor.Update(isOnGround: false, 3000.0, flapsPercent: 20.0, simDtSeconds: 1.0);

            Assert.True(monitor.HasViolation);
            Assert.NotNull(monitor.FailureReason);
            Assert.Contains("flaps", monitor.FailureReason);
        }

        [Fact]
        public void FlapsVariacaoPequena_3Pct_Ok()
        {
            var monitor = new FerryRestrictionMonitor();
            monitor.Configure(new FerryPermitContractData
            {
                Restriction = "flaps_fixed",
                FlapTolerancePct = 5.0
            });

            // Decolagem com flaps 10%
            monitor.Update(isOnGround: true, 1000.0, flapsPercent: 10.0, simDtSeconds: 1.0);
            monitor.Update(isOnGround: false, 1050.0, flapsPercent: 10.0, simDtSeconds: 1.0);

            // Flaps variam para 13% (diferença de 3% <= 5%)
            monitor.Update(isOnGround: false, 3000.0, flapsPercent: 13.0, simDtSeconds: 1.0);

            Assert.False(monitor.HasViolation);
        }
    }
}
