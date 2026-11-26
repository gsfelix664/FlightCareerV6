using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class LandingCycleTrackerTests
    {
        private static LandingCycleTracker Landed(double fpm = 101)
        {
            var tracker = new LandingCycleTracker(8.0);
            tracker.RegisterLanding(fpm);
            return tracker;
        }

        [Fact]
        public void ToqueEArremetida_AbreCicloNovo_SemContarPerna()
        {
            var tracker = Landed();
            Assert.False(tracker.Update(true, 36, 100, out _));
            Assert.False(tracker.Update(false, 60, 105, out _));
            Assert.True(tracker.Update(false, 70, 113, out bool afterFullStop));

            Assert.False(afterFullStop);
            Assert.False(tracker.HasTouchedDown);
            Assert.Equal(0, tracker.TouchdownFpm);
            Assert.Equal(0, tracker.CompletedLegs);
        }

        [Fact]
        public void Quique_MantemOCiclo()
        {
            var tracker = Landed();
            Assert.False(tracker.Update(false, 50, 100, out _));
            Assert.False(tracker.Update(false, 48, 104, out _));
            Assert.False(tracker.Update(true, 45, 105, out _));
            // O tempo no ar recomeça: dois quiques curtos não somam
            Assert.False(tracker.Update(false, 44, 106, out _));
            Assert.False(tracker.Update(false, 42, 112, out _));

            Assert.True(tracker.HasTouchedDown);
        }

        [Fact]
        public void Quique_SoContaSeForMaisForte()
        {
            var tracker = Landed(101);
            Assert.False(tracker.TryRegisterBounce(90));
            Assert.True(tracker.TryRegisterBounce(150));
            Assert.Equal(150, tracker.TouchdownFpm);
        }

        [Fact]
        public void ParadaCompleta_ContaUmaPerna_EPermiteDecolagemNova()
        {
            var tracker = Landed();
            tracker.Update(true, 3, 100, out _);
            tracker.Update(true, 0, 101, out _);
            tracker.Update(false, 60, 200, out _);
            Assert.True(tracker.Update(false, 65, 208, out bool afterFullStop));

            Assert.True(afterFullStop);
            Assert.Equal(1, tracker.CompletedLegs);

            // Segundo pouso, mais suave que o primeiro, é um pouso novo
            tracker.RegisterLanding(80);
            tracker.Update(true, 0, 300, out _);
            Assert.Equal(2, tracker.CompletedLegs);
        }

        [Fact]
        public void SemPouso_NaoFazNada_EResetZeraAsPernas()
        {
            var tracker = new LandingCycleTracker(8.0);
            Assert.False(tracker.Update(false, 70, 100, out _));
            Assert.False(tracker.Update(true, 0, 200, out _));
            Assert.Equal(0, tracker.CompletedLegs);

            tracker.RegisterLanding(100);
            tracker.Update(true, 0, 201, out _);
            tracker.Reset();
            Assert.Equal(0, tracker.CompletedLegs);
            Assert.False(tracker.HasTouchedDown);
        }
    }
}
