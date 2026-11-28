using System;
using FlightCareer.Domain.Xp;
using Xunit;

namespace FlightCareer.Tests.Xp
{
    public class PilotLevelTests
    {
        [Theory]
        [InlineData(-100, 1)]
        [InlineData(0, 1)]
        [InlineData(299, 1)]
        [InlineData(300, 2)]
        [InlineData(7499, 5)]
        [InlineData(7500, 6)]
        [InlineData(43199, 12)]
        [InlineData(43200, 13)]
        [InlineData(119999, 20)]
        [InlineData(120000, 21)]
        [InlineData(235200, 29)]
        [InlineData(388800, 37)]
        [InlineData(580800, 45)]
        [InlineData(720300, 50)]
        public void ForXp_CalculatesCorrectMilestoneLevels(long xp, int expectedLevel)
        {
            int level = PilotLevel.ForXp(xp);
            Assert.Equal(expectedLevel, level);
        }

        [Fact]
        public void ForXp_ExtremeXp_ProgressesWithoutCap()
        {
            int level10M = PilotLevel.ForXp(10_000_000);
            Assert.True(level10M > 50, $"Nível para 10M XP deveria ser > 50 mas foi {level10M}");
            Assert.Equal(183, level10M); // 1 + floor(sqrt(10_000_000 / 300)) = 1 + floor(182.57) = 183
        }

        [Theory]
        [InlineData(1, 0L)]
        [InlineData(2, 300L)]
        [InlineData(6, 7500L)]
        [InlineData(13, 43200L)]
        [InlineData(21, 120000L)]
        [InlineData(29, 235200L)]
        [InlineData(37, 388800L)]
        [InlineData(45, 580800L)]
        [InlineData(50, 720300L)]
        public void XpForLevel_ReturnsExactThreshold(int level, long expectedXp)
        {
            long xp = PilotLevel.XpForLevel(level);
            Assert.Equal(expectedXp, xp);
        }

        [Fact]
        public void Invariant_XpForLevelOfForXp_IsAlwaysLessThanOrEqualToOriginalXp()
        {
            long[] testValues = { 0, 150, 300, 500, 7500, 12345, 43200, 99999, 580800, 1_000_000, 25_000_000 };
            foreach (var xp in testValues)
            {
                int level = PilotLevel.ForXp(xp);
                long reqXp = PilotLevel.XpForLevel(level);
                Assert.True(reqXp <= xp, $"Invariante violada para XP={xp}: Nível={level}, XpForLevel={reqXp} > {xp}");
            }
        }
    }
}
