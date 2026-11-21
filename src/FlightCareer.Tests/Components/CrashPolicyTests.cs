using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class CrashPolicyTests
    {
        [Fact]
        public void Decide_MonitoringAndFlightActiveAndNotHandled_ReturnsTotalLoss()
        {
            var decision = CrashPolicy.Decide(isMonitoring: true, isFlightActive: true, alreadyHandled: false);
            Assert.Equal(CrashDecision.TotalLoss, decision);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Decide_AlreadyHandled_ReturnsIgnore(bool isMonitoring, bool isFlightActive)
        {
            var decision = CrashPolicy.Decide(isMonitoring, isFlightActive, alreadyHandled: true);
            Assert.Equal(CrashDecision.Ignore, decision);
        }

        [Fact]
        public void Decide_MonitoringWithoutActiveFlight_ReturnsAlertOnly()
        {
            var decision = CrashPolicy.Decide(isMonitoring: true, isFlightActive: false, alreadyHandled: false);
            Assert.Equal(CrashDecision.AlertOnly, decision);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Decide_NotMonitoring_ReturnsAlertOnly(bool isFlightActive)
        {
            var decision = CrashPolicy.Decide(isMonitoring: false, isFlightActive: isFlightActive, alreadyHandled: false);
            Assert.Equal(CrashDecision.AlertOnly, decision);
        }
    }
}
