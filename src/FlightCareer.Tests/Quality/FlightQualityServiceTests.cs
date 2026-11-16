using System;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using Xunit;

namespace FlightCareer.Tests.Quality
{
    public class FlightQualityServiceTests
    {
        [Fact]
        public void SameManeuver_1HzVs4Hz_SameScore()
        {
            var svc1Hz = new FlightQualityService();
            var svc4Hz = new FlightQualityService();

            var sample = new FlightQualitySample(
                OnGround: false,
                GForce: 1.0,
                BankDeg: 40.0,
                PitchUpDeg: 0.0
            );

            // 60 s a 1 Hz (60 amostras de 1 s)
            for (int i = 0; i < 60; i++)
            {
                svc1Hz.ProcessSample(sample, isPaxFlight: true, simSeconds: 1.0);
            }

            // 60 s a 4 Hz (240 amostras de 0,25 s)
            for (int i = 0; i < 240; i++)
            {
                svc4Hz.ProcessSample(sample, isPaxFlight: true, simSeconds: 0.25);
            }

            Assert.Equal(60.0, svc1Hz.BankSeconds, 2);
            Assert.Equal(60.0, svc4Hz.BankSeconds, 2);
            Assert.Equal(svc1Hz.GetQualityScore(), svc4Hz.GetQualityScore(), 2);
            Assert.Equal(94.0, svc1Hz.GetQualityScore(), 2); // 100 - (60 * 0.1) = 94.0
        }

        [Fact]
        public void SimRate4x_CountsSimulatedSeconds()
        {
            var svc1x = new FlightQualityService();
            var svc4x = new FlightQualityService();

            var sample = new FlightQualitySample(
                OnGround: false,
                GForce: 1.0,
                BankDeg: 40.0,
                PitchUpDeg: 0.0
            );

            // 60 s a 1x (60 amostras de 1 s)
            for (int i = 0; i < 60; i++)
            {
                svc1x.ProcessSample(sample, isPaxFlight: true, simSeconds: 1.0);
            }

            // 15 amostras de 4 s simulados (ex.: 1 s real a 4x sim rate)
            for (int i = 0; i < 15; i++)
            {
                svc4x.ProcessSample(sample, isPaxFlight: true, simSeconds: 4.0);
            }

            Assert.Equal(60.0, svc4x.BankSeconds, 2);
            Assert.Equal(svc1x.GetQualityScore(), svc4x.GetQualityScore(), 2);
        }

        [Fact]
        public void Paused_ZeroSeconds_NoPenalty()
        {
            var svc = new FlightQualityService();
            var sample = new FlightQualitySample(
                OnGround: false,
                GForce: 2.5,
                BankDeg: 50.0,
                PitchUpDeg: 30.0
            );

            // simSeconds <= 0 não conta tempo
            svc.ProcessSample(sample, isPaxFlight: true, simSeconds: 0.0);
            svc.ProcessSample(sample, isPaxFlight: true, simSeconds: -1.0);
            svc.ProcessSample(sample, isPaxFlight: true, simSeconds: double.NaN);

            Assert.Equal(100.0, svc.GetQualityScore());
            Assert.Equal(0.0, svc.GSeconds);
            Assert.Equal(0.0, svc.BankSeconds);
            Assert.Equal(0.0, svc.PitchSeconds);

            // Mas atualiza extremos (não pausado por MotionPaused)
            Assert.Equal(2.5, svc.MaxGForce);
            Assert.Equal(50.0, svc.MaxBank);
            Assert.Equal(30.0, svc.MaxPitch);
        }

        [Fact]
        public void MotionPaused_NoPenalty()
        {
            var svc = new FlightQualityService();
            var sample = new FlightQualitySample(
                OnGround: false,
                GForce: 2.5,
                BankDeg: 50.0,
                PitchUpDeg: 30.0,
                MotionPaused: true
            );

            // MotionPaused ignora contagem e extremos
            svc.ProcessSample(sample, isPaxFlight: true, simSeconds: 10.0);

            Assert.Equal(100.0, svc.GetQualityScore());
            Assert.Equal(0.0, svc.GSeconds);
            Assert.Equal(0.0, svc.BankSeconds);
            Assert.Equal(0.0, svc.PitchSeconds);
            Assert.Equal(1.0, svc.MaxGForce);
            Assert.Equal(0.0, svc.MaxBank);
            Assert.Equal(0.0, svc.MaxPitch);
        }

        [Fact]
        public void OnGround_Ignored()
        {
            var svc = new FlightQualityService();
            var sample = new FlightQualitySample(
                OnGround: true,
                GForce: 2.5,
                BankDeg: 45.0,
                PitchUpDeg: 25.0
            );

            svc.ProcessSample(sample, isPaxFlight: true, simSeconds: 10.0);

            Assert.Equal(100.0, svc.GetQualityScore());
            Assert.Equal(0.0, svc.GSeconds);
            Assert.Equal(0.0, svc.BankSeconds);
            Assert.Equal(1.0, svc.MaxGForce);
            Assert.Equal(0.0, svc.MaxBank);
        }

        [Fact]
        public void PitchSign_MsfsNoseUpNegative()
        {
            // MSFS: PLANE PITCH DEGREES é negativo com nariz para cima
            // FromTelemetry converte: PitchUpDeg = -PlanePitch
            var svc = new FlightQualityService();

            // 1. Nariz 25° para cima: MSFS reporta -25° -> PitchUpDeg = +25° (> 20° limite) -> penaliza
            var telemNoseUp = new SimTelemetry { OnGround = 0.0, GForce = 1.0, PlanePitch = -25.0 };
            svc.ProcessSample(FlightQualitySample.FromTelemetry(telemNoseUp), isPaxFlight: true, simSeconds: 10.0);
            Assert.Equal(10.0, svc.PitchSeconds);
            Assert.Equal(99.0, svc.GetQualityScore()); // 100 - (10 * 0.1)

            svc.Reset();

            // 2. Nariz 10° para baixo: MSFS reporta +10° -> PitchUpDeg = -10° (limite para baixo é 15°) -> não penaliza
            var telemNoseDownMild = new SimTelemetry { OnGround = 0.0, GForce = 1.0, PlanePitch = 10.0 };
            svc.ProcessSample(FlightQualitySample.FromTelemetry(telemNoseDownMild), isPaxFlight: true, simSeconds: 10.0);
            Assert.Equal(0.0, svc.PitchSeconds);
            Assert.Equal(100.0, svc.GetQualityScore());

            svc.Reset();

            // 3. Nariz 18° para baixo: MSFS reporta +18° -> PitchUpDeg = -18° (> 15° limite para baixo) -> penaliza
            var telemNoseDownSteep = new SimTelemetry { OnGround = 0.0, GForce = 1.0, PlanePitch = 18.0 };
            svc.ProcessSample(FlightQualitySample.FromTelemetry(telemNoseDownSteep), isPaxFlight: true, simSeconds: 10.0);
            Assert.Equal(10.0, svc.PitchSeconds);
            Assert.Equal(99.0, svc.GetQualityScore());
        }

        [Fact]
        public void CargoFlight_BankAndPitchDoNotCount_GDoes()
        {
            var svc = new FlightQualityService();

            // Amostra com bank 45°, pitch 25° para cima, mas G normal (1.0)
            var sampleAtitude = new FlightQualitySample(
                OnGround: false,
                GForce: 1.0,
                BankDeg: 45.0,
                PitchUpDeg: 25.0
            );

            // Voo de carga (isPaxFlight: false)
            svc.ProcessSample(sampleAtitude, isPaxFlight: false, simSeconds: 30.0);
            Assert.Equal(0.0, svc.BankSeconds);
            Assert.Equal(0.0, svc.PitchSeconds);
            Assert.Equal(100.0, svc.GetQualityScore());

            // Agora excesso de G em carga: G = 2.0 por 20 s (peso carga = 0,05)
            var sampleG = new FlightQualitySample(
                OnGround: false,
                GForce: 2.0,
                BankDeg: 0.0,
                PitchUpDeg: 0.0
            );
            svc.ProcessSample(sampleG, isPaxFlight: false, simSeconds: 20.0);
            Assert.Equal(20.0, svc.GSeconds);
            // Penalidade = 20 * 0.05 = 1.0 ponto -> Score = 99.0
            Assert.Equal(99.0, svc.GetQualityScore());
        }

        [Fact]
        public void Turbulence_WindVertical_ReducesGPenalty()
        {
            var svcTurb = new FlightQualityService();

            // Vento vertical 8 ft/s (acima do limite de 5 ft/s) com G de 2.0 por 40 s
            var sampleTurb = new FlightQualitySample(
                OnGround: false,
                GForce: 2.0,
                BankDeg: 0.0,
                PitchUpDeg: 0.0,
                WindVerticalFps: 8.0
            );

            svcTurb.ProcessSample(sampleTurb, isPaxFlight: true, simSeconds: 40.0);

            // Turbulência reduz para fração 0,25: 40 * 0,25 = 10 s
            Assert.Equal(10.0, svcTurb.GSeconds);
            // Penalidade em pax: 10 * 0,1 = 1,0 ponto -> Score = 99.0
            Assert.Equal(99.0, svcTurb.GetQualityScore());
        }

        [Fact]
        public void NoWindData_FullPenalty()
        {
            var svcNoWind = new FlightQualityService();

            // Sem dado de vento (null), G de 2.0 por 40 s conta inteiro
            var sampleNoWind = new FlightQualitySample(
                OnGround: false,
                GForce: 2.0,
                BankDeg: 0.0,
                PitchUpDeg: 0.0,
                WindVerticalFps: null
            );

            svcNoWind.ProcessSample(sampleNoWind, isPaxFlight: true, simSeconds: 40.0);

            Assert.Equal(40.0, svcNoWind.GSeconds);
            // Penalidade em pax: 40 * 0,1 = 4,0 pontos -> Score = 96.0
            Assert.Equal(96.0, svcNoWind.GetQualityScore());
        }

        [Fact]
        public void Jerk_And_Slip_CountOnlyWhenProvided()
        {
            var svc = new FlightQualityService();

            // Sem dados opcionais: JerkSeconds e SlipSeconds permanecem 0
            var sampleNormal = new FlightQualitySample(
                OnGround: false,
                GForce: 1.0,
                BankDeg: 0.0,
                PitchUpDeg: 0.0,
                LoadFactorYDot: null,
                BetaDeg: null
            );
            svc.ProcessSample(sampleNormal, isPaxFlight: true, simSeconds: 20.0);
            Assert.Equal(0.0, svc.JerkSeconds);
            Assert.Equal(0.0, svc.SlipSeconds);
            Assert.Equal(100.0, svc.GetQualityScore());

            // Solavanco vertical excessivo (|YDOT| = 3.0 > 2.0) por 10 s (peso pax = 0,05)
            var sampleJerk = new FlightQualitySample(
                OnGround: false,
                GForce: 1.0,
                BankDeg: 0.0,
                PitchUpDeg: 0.0,
                LoadFactorYDot: 3.0
            );
            svc.ProcessSample(sampleJerk, isPaxFlight: true, simSeconds: 10.0);
            Assert.Equal(10.0, svc.JerkSeconds);
            // 100 - (10 * 0.05) = 99.5
            Assert.Equal(99.5, svc.GetQualityScore());

            // Derrapagem excessiva (|beta| = 8.0 > 5.0) por 10 s (peso pax = 0,05)
            var sampleSlip = new FlightQualitySample(
                OnGround: false,
                GForce: 1.0,
                BankDeg: 0.0,
                PitchUpDeg: 0.0,
                BetaDeg: 8.0
            );
            svc.ProcessSample(sampleSlip, isPaxFlight: true, simSeconds: 10.0);
            Assert.Equal(10.0, svc.SlipSeconds);
            // 99.5 - (10 * 0.05) = 99.0
            Assert.Equal(99.0, svc.GetQualityScore());
        }

        [Fact]
        public void Calibration_1Hz_MatchesLegacyScale()
        {
            var svc = new FlightQualityService();

            // 50 s de G = 1.8 a 1 Hz em voo de passageiro (50 amostras de 1 s)
            var sample = new FlightQualitySample(
                OnGround: false,
                GForce: 1.8,
                BankDeg: 0.0,
                PitchUpDeg: 0.0
            );

            for (int i = 0; i < 50; i++)
            {
                svc.ProcessSample(sample, isPaxFlight: true, simSeconds: 1.0);
            }

            Assert.Equal(50.0, svc.GSeconds);
            // Penalidade = 50 * 0.1 = 5.0 -> Score = 95.0 (como o legado com 50 pacotes)
            Assert.Equal(95.0, svc.GetQualityScore());
        }

        [Fact]
        public void LegacyProcessFrame_WorksAsExpected()
        {
            var svc = new FlightQualityService();
            var telem = new SimTelemetry
            {
                OnGround = 0.0,
                GForce = 1.8,
                PlaneBank = 0.0,
                PlanePitch = 0.0
            };

#pragma warning disable CS0618 // Type or member is obsolete
            svc.ProcessFrame(telem, isPaxFlight: true);
            Assert.Equal(1.0, svc.GSeconds);
            Assert.Equal(1, svc.ComfortViolations);
            Assert.Equal(99.9, svc.GetQualityScore(), 1);
#pragma warning restore CS0618
        }
    }
}
