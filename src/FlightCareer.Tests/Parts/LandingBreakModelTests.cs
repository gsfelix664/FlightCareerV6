using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class LandingBreakModelTests
    {
        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly double _value;
            public FixedRandomSource(double value) => _value = value;
            public double NextDouble() => _value;
        }

        private static readonly LandingSettings DefaultSettings = new();

        private static AircraftPart Part(string id, double health = 100.0, bool isBroken = false) =>
            new() { Id = Guid.NewGuid(), PartId = id, Health = health, IsBroken = isBroken };

        [Theory]
        [InlineData(599.0, 0.0)]
        [InlineData(600.0, 0.01)]
        [InlineData(700.0, 0.026)]
        [InlineData(850.0, 0.15)]
        [InlineData(875.0, 0.225)]
        [InlineData(950.0, 0.30)]
        public void GearBaseChance_ValoresDaCurva_CorrespondemAEspecificacao(double fpm, double esperado)
        {
            double atual = LandingBreakModel.GearBaseChance(fpm, DefaultSettings);
            Assert.Equal(esperado, atual, 6);
        }

        [Fact]
        public void ChanceFor_Pneu700FpmEmPistaSemInclinacaoSaude100_Retorna0039()
        {
            var tire = Part("tires.main", 100.0);
            double chance = LandingBreakModel.ChanceFor(
                tire, isTire: true, fpm: 700.0, bankDeg: null, offRunway: false, DefaultSettings);

            Assert.Equal(0.039, chance, 6);
        }

        [Fact]
        public void ChanceFor_Trem733FpmInclinadoForaDePistaSaude975_Retorna00962()
        {
            var gear = Part("gear.main_struts", 97.5);
            double chance = LandingBreakModel.ChanceFor(
                gear, isTire: false, fpm: 733.0, bankDeg: 18.5, offRunway: true, DefaultSettings);

            Assert.Equal(0.0962, chance, 4);
        }

        [Fact]
        public void ChanceFor_ExcedeTeto_LimitaEm06()
        {
            var tire = Part("tires.main", 0.0);
            double chance = LandingBreakModel.ChanceFor(
                tire, isTire: true, fpm: 900.0, bankDeg: 20.0, offRunway: true, DefaultSettings);

            Assert.Equal(0.6, chance, 6);
        }

        [Fact]
        public void Roll_RandomZero_QuebraUmTremPrincipalEUmPneuPrincipal()
        {
            var parts = new List<AircraftPart>
            {
                Part("gear.main_struts"),
                Part("gear.nose_strut"),
                Part("tires.main"),
                Part("tires.nose")
            };

            var breaks = LandingBreakModel.Roll(parts, 700.0, bankDeg: null, offRunway: false, DefaultSettings, new FixedRandomSource(0.0));

            Assert.Equal(2, breaks.Count);
            Assert.Equal("gear.main_struts", breaks[0].Part.PartId);
            Assert.Equal("tires.main", breaks[1].Part.PartId);
        }

        [Fact]
        public void Roll_Random099_NaoQuebraNada()
        {
            var parts = new List<AircraftPart>
            {
                Part("gear.main_struts"),
                Part("tires.main")
            };

            var breaks = LandingBreakModel.Roll(parts, 900.0, bankDeg: 20.0, offRunway: true, DefaultSettings, new FixedRandomSource(0.99));

            Assert.Empty(breaks);
        }

        [Fact]
        public void Roll_PecaPrincipalJaQuebrada_SelecionaNarizComoCandidata()
        {
            var parts = new List<AircraftPart>
            {
                Part("gear.main_struts", health: 0.0, isBroken: true),
                Part("gear.nose_strut"),
                Part("tires.main", health: 0.0, isBroken: true),
                Part("tires.nose")
            };

            var breaks = LandingBreakModel.Roll(parts, 700.0, bankDeg: 12.0, offRunway: true, DefaultSettings, new FixedRandomSource(0.0));

            Assert.Equal(2, breaks.Count);
            Assert.Equal("gear.nose_strut", breaks[0].Part.PartId);
            Assert.Equal("tires.nose", breaks[1].Part.PartId);
            Assert.Contains("inclinação", breaks[0].Reason);
            Assert.Contains("fora de pista", breaks[0].Reason);
        }
    }
}
