using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    /// <summary>Toque e crash medidos pelo addon na leitura rápida (independe do aviso de crash do simulador).</summary>
    public class TouchdownDetectorTests
    {
        private const double Dt = 0.05; // ~20 Hz

        private static FastFlightSample S(double t, bool ground, double vs = 0, double g = 1, double noseUp = 2, double bank = 0, double bodyY = 0, double gs = 60) =>
            new(t, ground, vs, bodyY, g, noseUp, bank, ground ? 50 : 60, gs);

        /// <summary>Voo no ar por 3 s descendo a <paramref name="vs"/>, toque e 1 s no solo.</summary>
        private static List<TouchdownEvent> Approach(TouchdownDetector d, double vs, double noseUp = 3, double bank = 0, double contactG = 1.4, double bodyY = 0)
        {
            var events = new List<TouchdownEvent>();
            double t = 0;
            for (; t < 3.0; t += Dt)
                if (d.Update(S(t, false, vs, 1.0, noseUp, bank, bodyY)) is { } e) events.Add(e);
            for (int i = 0; i < 20; i++, t += Dt)
                if (d.Update(S(t, true, i < 3 ? vs / 3 : 0, i < 3 ? contactG : 1.0, noseUp, bank)) is { } e) events.Add(e);
            return events;
        }

        private static TouchdownDetector New() => new(new LandingSettings());

        [Fact]
        public void NormalLanding_MeasuresDescentBeforeContact()
        {
            var e = Assert.Single(Approach(New(), vs: -250));
            Assert.Equal(TouchdownKind.Normal, e.Kind);
            Assert.Equal(250, e.TouchdownFpm);
            Assert.Equal(1.4, e.PeakG);
            Assert.Equal(string.Empty, e.Reason);
        }

        [Fact]
        public void HardButSurvivable_IsNormalKindWithRealFpm()
        {
            var e = Assert.Single(Approach(New(), vs: -900));
            Assert.Equal(TouchdownKind.Normal, e.Kind); // dano de pouso duro fica com o WearEngine
            Assert.Equal(900, e.TouchdownFpm);
        }

        [Fact]
        public void HighDescent_IsCrash()
        {
            var e = Assert.Single(Approach(New(), vs: -1600));
            Assert.Equal(TouchdownKind.Crash, e.Kind);
            Assert.Contains("razão de descida", e.Reason);
        }

        [Fact]
        public void TodayCrash_C152_NoseUpPancakeWith74DegBank_IsCrash()
        {
            // Voo 20261115-0905: chegou ao chão a ~1.077 fpm, nariz 37° para cima e 74° de inclinação (sim sem dano de colisão)
            var e = Assert.Single(Approach(New(), vs: -1077, noseUp: 37, bank: 74));
            Assert.Equal(TouchdownKind.Crash, e.Kind);
            Assert.Contains("inclinação", e.Reason);
            Assert.Contains("nariz 37° para cima", e.Reason);
        }

        [Fact]
        public void NoseDownContact_IsCrash()
        {
            var e = Assert.Single(Approach(New(), vs: -500, noseUp: -20));
            Assert.Equal(TouchdownKind.Crash, e.Kind);
            Assert.Contains("para baixo", e.Reason);
            Assert.Equal(-20, e.NoseUpDeg);
        }

        [Fact]
        public void HighImpactG_IsCrash()
        {
            var e = Assert.Single(Approach(New(), vs: -700, contactG: 7.0));
            Assert.Equal(TouchdownKind.Crash, e.Kind);
            Assert.Contains("G", e.Reason);
        }

        [Fact]
        public void BodyVerticalSpeed_IsUsedWhenLarger()
        {
            var e = Assert.Single(Approach(New(), vs: -100, bodyY: -10)); // 10 ft/s = 600 fpm
            Assert.Equal(600, e.TouchdownFpm);
        }

        [Fact]
        public void StartOnGround_TaxiNoEvent()
        {
            var d = New();
            for (double t = 0; t < 5; t += Dt) Assert.Null(d.Update(S(t, true, 0)));
        }

        [Fact]
        public void ShortGroundFlicker_IsNotNewContact()
        {
            var d = New();
            var events = Approach(d, vs: -200);
            Assert.Single(events);
            // 0,1 s no ar (sensor pulando na rolagem) e volta ao solo: não é novo toque
            double t = 10;
            Assert.Null(d.Update(S(t, false))); t += Dt;
            Assert.Null(d.Update(S(t, false))); t += Dt;
            for (int i = 0; i < 20; i++, t += Dt) Assert.Null(d.Update(S(t, true)));
        }

        [Fact]
        public void Bounce_ProducesSecondEvent()
        {
            var d = New();
            Assert.Single(Approach(d, vs: -300));
            var second = Approach(d, vs: -500);
            var e = Assert.Single(second);
            Assert.Equal(500, e.TouchdownFpm);
        }

        [Fact]
        public void Trace_CoversTwoSecondsBeforeAndWindowAfter()
        {
            var e = Assert.Single(Approach(New(), vs: -300));
            Assert.True(e.Trace.First().TimeSec <= e.ContactTimeSec - 1.9);
            Assert.True(e.Trace.Last().TimeSec >= e.ContactTimeSec + 0.45);
        }

        [Fact]
        public void Thresholds_ComeFromSettings()
        {
            var d = new TouchdownDetector(new LandingSettings { CrashFpm = 800 });
            Assert.Equal(TouchdownKind.Crash, Assert.Single(Approach(d, vs: -900)).Kind);
        }
    }
}
