using System;
using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class TurbineDegradationTests
    {
        private readonly TurbineDegradationConfig _defaultConfig = new()
        {
            StartHealthPct = 60.0,
            MaxN1LossPct = 5.0,
            MaxFuelEfficiencyLossPct = 10.0,
            MaxIttCoolingLossPct = 15.0,
            MaxThrustEfficiencyLossPct = 8.0,
            WriteToleranceStepPct = 0.5
        };

        [Fact]
        public void Compute_SaudeAcimaDoInicio_SemPerda()
        {
            var losses = TurbineDegradation.Compute(80.0, 90.0, _defaultConfig);
            Assert.Equal(TurbineLosses.None, losses);
        }

        [Fact]
        public void Compute_SaudeZero_PerdaMaxima()
        {
            var losses = TurbineDegradation.Compute(0.0, 0.0, _defaultConfig);
            Assert.Equal(5.0, losses.N1LossPct);
            Assert.Equal(10.0, losses.FuelEfficiencyLossPct);
            Assert.Equal(15.0, losses.IttCoolingLossPct);
            Assert.Equal(8.0, losses.ThrustEfficiencyLossPct);
        }

        [Fact]
        public void Compute_MeioDoCaminho_Linear()
        {
            // StartHealth 60, hot 30 -> f(hot) = 0.5 -> 15 * 0.5 = 7.5
            var losses = TurbineDegradation.Compute(60.0, 30.0, _defaultConfig);
            Assert.Equal(0.0, losses.N1LossPct);
            Assert.Equal(7.5, losses.IttCoolingLossPct);
            Assert.Equal(0.0, losses.ThrustEfficiencyLossPct);
            // Fuel usa o menor (30) -> f = 0.5 -> 10 * 0.5 = 5.0
            Assert.Equal(5.0, losses.FuelEfficiencyLossPct);
        }

        [Fact]
        public void Compute_FuelUsaOMenorDosDois()
        {
            // engine 60 (f=0), hot 0 (f=1.0) -> Fuel usa min(60, 0) = 0 -> FuelEff = 10, N1 = 0
            var losses = TurbineDegradation.Compute(60.0, 0.0, _defaultConfig);
            Assert.Equal(0.0, losses.N1LossPct);
            Assert.Equal(10.0, losses.FuelEfficiencyLossPct);
            Assert.Equal(15.0, losses.IttCoolingLossPct);
            Assert.Equal(0.0, losses.ThrustEfficiencyLossPct);
        }

        [Fact]
        public void Compute_Arredonda_NoPassoDeEscrita()
        {
            var cfg = new TurbineDegradationConfig
            {
                StartHealthPct = 100.0,
                MaxN1LossPct = 10.0,
                MaxFuelEfficiencyLossPct = 10.0,
                MaxIttCoolingLossPct = 10.0,
                MaxThrustEfficiencyLossPct = 10.0,
                WriteToleranceStepPct = 0.5
            };

            // health 73 -> f = 0.27 -> 10 * 0.27 = 2.7 -> múltiplo de 0.5 mais próximo é 2.5
            var losses = TurbineDegradation.Compute(73.0, 73.0, cfg);
            Assert.Equal(2.5, losses.N1LossPct);
            Assert.Equal(2.5, losses.FuelEfficiencyLossPct);

            // health 72 -> f = 0.28 -> 10 * 0.28 = 2.8 -> múltiplo de 0.5 mais próximo é 3.0
            losses = TurbineDegradation.Compute(72.0, 72.0, cfg);
            Assert.Equal(3.0, losses.N1LossPct);
        }

        [Fact]
        public void ShouldWrite_AlvoIgualELeituraIgual_NaoEscreve()
        {
            var target = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var lastWritten = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var simValues = new TurbineLosses(5.0, 10.0, 15.0, 8.0);

            bool write = TurbineDegradation.ShouldWrite(
                target,
                lastWritten,
                simValues,
                TimeSpan.FromSeconds(10),
                0.5,
                TimeSpan.FromSeconds(5));

            Assert.False(write);
        }

        [Fact]
        public void ShouldWrite_LeituraZeroComAlvoMaiorQueZero_Escreve()
        {
            var target = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var lastWritten = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var simValues = TurbineLosses.None; // Sim zerou

            bool write = TurbineDegradation.ShouldWrite(
                target,
                lastWritten,
                simValues,
                TimeSpan.FromSeconds(10),
                0.5,
                TimeSpan.FromSeconds(5));

            Assert.True(write);
        }

        [Fact]
        public void ShouldWrite_MenosDe5Segundos_NaoEscreve()
        {
            var target = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var lastWritten = TurbineLosses.None;
            var simValues = TurbineLosses.None;

            bool write = TurbineDegradation.ShouldWrite(
                target,
                lastWritten,
                simValues,
                TimeSpan.FromSeconds(3), // menos de 5 s
                0.5,
                TimeSpan.FromSeconds(5));

            Assert.False(write);
        }
    }
}
