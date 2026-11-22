using System;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    public class DifficultRunwayRewardTests
    {
        [Fact]
        public void CalculateReward_DestTier2_WithGrassRunway_Receives25PctBushBonus()
        {
            var grassRunway = new Runway { Surface = "GRASS", LengthFt = 3500 };
            var rnd = new Random(12345);
            var rndCopy = new Random(12345);

            double rewardGrass = MissionGenerator.CalculateReward(
                distance: 100, payload: 400, urgency: 1.0, subId: "fallback", paxCount: 0,
                originTier: 3, destTier: 2, destRunway: grassRunway, random: rnd);

            // Sem bônus (tier 2 com pista normal pavimentada)
            var pavedRunway = new Runway { Surface = "ASPHALT", LengthFt = 5000 };
            double rewardPaved = MissionGenerator.CalculateReward(
                distance: 100, payload: 400, urgency: 1.0, subId: "fallback", paxCount: 0,
                originTier: 3, destTier: 2, destRunway: pavedRunway, random: rndCopy);

            // Deve ser 25% maior (tolerância para arredondamento em centavos)
            Assert.Equal(rewardPaved * 1.25, rewardGrass, 0.02);
        }

        [Fact]
        public void CalculateReward_DestTier4_WithLongAsphaltRunway_ReceivesNoBushBonus()
        {
            var longAsphalt = new Runway { Surface = "ASPHALT", LengthFt = 6000 };
            var rnd = new Random(12345);

            double reward = MissionGenerator.CalculateReward(
                distance: 100, payload: 400, urgency: 1.0, subId: "fallback", paxCount: 0,
                originTier: 3, destTier: 4, destRunway: longAsphalt, random: rnd);

            var rndCopy = new Random(12345);
            // Comparando com tier 4 sem bônus (isDifficultRunway = false)
            double rewardExplicitNoBonus = MissionGenerator.CalculateReward(
                distance: 100, payload: 400, urgency: 1.0, subId: "fallback", paxCount: 0,
                originTier: 3, destTier: 4, random: rndCopy, isDifficultRunway: false);

            Assert.Equal(rewardExplicitNoBonus, reward);
        }

        [Fact]
        public void CalculateReward_WithoutRunwayData_FallsBackToTier4Rule()
        {
            var rnd1 = new Random(42);
            var rnd2 = new Random(42);

            // Sem dado de pista, destTier 4 recebe 1.25
            double rewardTier4 = MissionGenerator.CalculateReward(
                distance: 100, payload: 400, urgency: 1.0, subId: "fallback", paxCount: 0,
                originTier: 3, destTier: 4, random: rnd1, isDifficultRunway: null);

            // Sem dado de pista, destTier 3 não recebe 1.25
            double rewardTier3 = MissionGenerator.CalculateReward(
                distance: 100, payload: 400, urgency: 1.0, subId: "fallback", paxCount: 0,
                originTier: 3, destTier: 3, random: rnd2, isDifficultRunway: null);

            Assert.Equal(rewardTier3 * 1.25, rewardTier4, 0.02);
        }
    }
}
