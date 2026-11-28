using FlightCareer.Domain.Xp;
using Xunit;

namespace FlightCareer.Tests.Xp
{
    public class XpDeadlineTests
    {
        [Fact]
        public void Compute_NormalMission_UsesEteWithBufferAndRatios()
        {
            // 100 NM a 100 kt = 60 min + 15 de folga = 75 min
            var d = XpDeadline.Compute(100, 100, isUrgent: false, isLocalFlight: false);

            Assert.NotNull(d);
            Assert.Equal(75.0, d!.ReferenceEteMinutes, 3);
            Assert.Equal(67.5, d.OnTimeLimitMinutes, 3);
            Assert.Equal(86.25, d.AcceptableLimitMinutes, 3);
            Assert.Equal(30.0, d.OnTimeBonusXp);
            Assert.Equal(15.0, d.AcceptableBonusXp);
        }

        [Fact]
        public void Compute_UrgentMission_UsesUrgentBonus()
        {
            var d = XpDeadline.Compute(100, 100, isUrgent: true, isLocalFlight: false);
            Assert.Equal(60.0, d!.OnTimeBonusXp);
        }

        [Fact]
        public void Compute_LimitMatchesXpCalculatorThreshold()
        {
            // Voar exatamente no limite exibido deve render o bônus "Prazo Cumprido" no cálculo real
            var d = XpDeadline.Compute(100, 100, isUrgent: false, isLocalFlight: false)!;
            double ratio = d.OnTimeLimitMinutes / d.ReferenceEteMinutes;
            Assert.True(ratio <= XpRules.Default.Bonuses.OnTimeRatioMax + 1e-9);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(50, true)]
        public void Compute_LocalOrZeroDistance_ReturnsNull(double nm, bool local)
        {
            Assert.Null(XpDeadline.Compute(nm, 100, false, local));
        }

        [Theory]
        [InlineData(67.5, "1h 07m")]
        [InlineData(42.9, "42 min")]
        public void FormatMinutes_FloorsToMinute(double min, string expected)
        {
            Assert.Equal(expected, XpDeadline.FormatMinutes(min));
        }
    }
}
