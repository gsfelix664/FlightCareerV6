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

        private static FastFlightSample S(double t, bool ground, double vs = 0, double g = 1, double noseUp = 2, double bank = 0, double bodyY = 0, double gs = 60, double normalFps = double.NaN) =>
            new(t, ground, vs, bodyY, g, noseUp, bank, ground ? 50 : 60, gs, normalFps);

        /// <summary>Voo no ar por 3 s descendo a <paramref name="vs"/>, toque e 1 s no solo.</summary>
        private static List<TouchdownEvent> Approach(TouchdownDetector d, double vs, double noseUp = 3, double bank = 0, double contactG = 1.4, double bodyY = 0, double normalBefore = double.NaN, double normalAfter = double.NaN)
        {
            var events = new List<TouchdownEvent>();
            double t = 0;
            for (; t < 3.0; t += Dt)
                if (d.Update(S(t, false, vs, 1.0, noseUp, bank, bodyY, normalFps: normalBefore)) is { } e) events.Add(e);
            for (int i = 0; i < 20; i++, t += Dt)
                if (d.Update(S(t, true, i < 3 ? vs / 3 : 0, i < 3 ? contactG : 1.0, noseUp, bank, normalFps: normalAfter)) is { } e) events.Add(e);
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
        public void Pouso_suave_de_nariz_alto_usa_a_velocidade_normal_do_sim()
        {
            // Aproximação com VS caindo de −60 a 0 fpm, BodyVerticalFps = −10,6 e nariz +6° nas amostras antes do contato;
            // TouchdownNormalFps = 0,30 antes do contato (toque anterior) e 1,666 depois -> TouchdownFpm ≈ 100 (±1), Kind Normal.
            var d = New();
            var events = new List<TouchdownEvent>();
            double t = 0;
            for (; t < 3.0; t += Dt)
            {
                double vs = t >= 2.0 ? -60.0 * (3.0 - t) : -60.0;
                if (d.Update(S(t, false, vs: vs, g: 1.0, noseUp: 6.0, bank: 0, bodyY: -10.6, normalFps: 0.30)) is { } e)
                    events.Add(e);
            }
            for (int i = 0; i < 20; i++, t += Dt)
            {
                if (d.Update(S(t, true, vs: 0, g: 1.09, noseUp: 6.0, bank: 0, normalFps: 1.666)) is { } e)
                    events.Add(e);
            }
            var ev = Assert.Single(events);
            Assert.Equal(TouchdownKind.Normal, ev.Kind);
            Assert.InRange(ev.TouchdownFpm, 99, 101);
        }

        [Fact]
        public void Sem_velocidade_normal_usa_vertical_speed_e_ignora_body_y()
        {
            // TouchdownNormalFps = NaN em tudo; VS mínimo −250 fpm antes do contato; BodyVerticalFps = −12 -> TouchdownFpm = 250 (não 720)
            var e = Assert.Single(Approach(New(), vs: -250, bodyY: -12, normalBefore: double.NaN, normalAfter: double.NaN));
            Assert.Equal(250, e.TouchdownFpm);
        }

        [Fact]
        public void Velocidade_normal_sem_mudar_no_contato_usa_vertical_speed()
        {
            // TouchdownNormalFps constante 1,5 antes e depois (o sim não atualizou) -> usa VS (-300 fpm)
            var e = Assert.Single(Approach(New(), vs: -300, normalBefore: 1.5, normalAfter: 1.5));
            Assert.Equal(300, e.TouchdownFpm);
        }

        [Fact]
        public void Pouso_duro_pela_velocidade_normal()
        {
            // TouchdownNormalFps muda para 12,5 ft/s (750 fpm) depois do contato, VS −700 -> TouchdownFpm = 750
            var e = Assert.Single(Approach(New(), vs: -700, normalBefore: 0.0, normalAfter: 12.5));
            Assert.Equal(750, e.TouchdownFpm);
        }

        [Fact]
        public void Crash_pela_velocidade_normal()
        {
            // Valor normal acima de CrashFpm/60 -> Kind Crash com o motivo de razão de descida (1600 fpm / 60 = 26.666 ft/s)
            var e = Assert.Single(Approach(New(), vs: -300, normalBefore: 0.0, normalAfter: 1600.0 / 60.0));
            Assert.Equal(TouchdownKind.Crash, e.Kind);
            Assert.Contains("razão de descida", e.Reason);
            Assert.Equal(1600, e.TouchdownFpm);
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
