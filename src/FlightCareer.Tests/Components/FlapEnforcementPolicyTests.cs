using System;
using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class FlapEnforcementPolicyTests
    {
        private readonly DateTime _now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(0.0, 1.0)]
        [InlineData(1.0, 2.0)]
        public void ShouldWrite_MismatchConfirmedAndThrottleElapsed_WritesEvenWithNativeFailure(double current, double locked)
        {
            var mismatchSince = _now.AddSeconds(-2.0);
            var lastWrite = _now.AddSeconds(-5.0);

            bool result = FlapEnforcementPolicy.ShouldWrite(
                currentIndex: current,
                lockedIndex: locked,
                mismatchSince: mismatchSince,
                lastWrite: lastWrite,
                now: _now);

            Assert.True(result); // decisão do usuário: a pane nativa não silencia a correção (barulho = indicativo)
        }

        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(1.0, 1.0)]
        [InlineData(2.0, 2.05)] // dentro da tolerância de 0.1
        public void ShouldWrite_EqualIndex_DoesNotWrite(double current, double locked)
        {
            var mismatchSince = _now.AddSeconds(-2.0);
            var lastWrite = _now.AddSeconds(-5.0);

            bool result = FlapEnforcementPolicy.ShouldWrite(
                currentIndex: current,
                lockedIndex: locked,
                mismatchSince: mismatchSince,
                lastWrite: lastWrite,
                now: _now);

            Assert.False(result);
        }

        [Fact]
        public void ShouldWrite_MismatchShorterThan500ms_DoesNotWrite()
        {
            // diferente há 0,3 s
            var mismatchSince = _now.AddSeconds(-0.3);
            DateTime? lastWrite = null;

            bool result = FlapEnforcementPolicy.ShouldWrite(
                currentIndex: 0.0,
                lockedIndex: 2.0,
                mismatchSince: mismatchSince,
                lastWrite: lastWrite,
                now: _now);

            Assert.False(result);
        }

        [Fact]
        public void ShouldWrite_Mismatch600msAndLastWrite2sAgo_Writes()
        {
            // há 0,6 s e última escrita há 2 s
            var mismatchSince = _now.AddSeconds(-0.6);
            var lastWrite = _now.AddSeconds(-2.0);

            bool result = FlapEnforcementPolicy.ShouldWrite(
                currentIndex: 0.0,
                lockedIndex: 2.0,
                mismatchSince: mismatchSince,
                lastWrite: lastWrite,
                now: _now);

            Assert.True(result);
        }

        [Fact]
        public void ShouldWrite_LastWriteShorterThan1sAgo_DoesNotWrite()
        {
            // última escrita há 0,5 s (mesmo com mismatch confirmado há 2 s)
            var mismatchSince = _now.AddSeconds(-2.0);
            var lastWrite = _now.AddSeconds(-0.5);

            bool result = FlapEnforcementPolicy.ShouldWrite(
                currentIndex: 0.0,
                lockedIndex: 2.0,
                mismatchSince: mismatchSince,
                lastWrite: lastWrite,
                now: _now);

            Assert.False(result);
        }
    }
}
