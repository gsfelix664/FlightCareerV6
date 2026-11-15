using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class FlapSurfaceLockTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 0)]
        [InlineData(1, -1)]
        public void TargetFraction_NoPositions_ReturnsZero(double index, double positions) =>
            Assert.Equal(0.0, FlapSurfaceLock.TargetFraction(index, positions));

        [Fact]
        public void TargetFraction_C152_ThreeExtendedDetents()
        {
            // C152 no MSFS 2024: FLAPS NUM HANDLE POSITIONS = 3, índices 0..3
            Assert.Equal(0.0, FlapSurfaceLock.TargetFraction(0, 3));
            Assert.Equal(1.0 / 3.0, FlapSurfaceLock.TargetFraction(1, 3), precision: 4);
            Assert.Equal(2.0 / 3.0, FlapSurfaceLock.TargetFraction(2, 3), precision: 4);
            Assert.Equal(1.0, FlapSurfaceLock.TargetFraction(3, 3));
        }

        [Fact]
        public void TargetFraction_IsClamped()
        {
            Assert.Equal(0.0, FlapSurfaceLock.TargetFraction(-1, 3));
            Assert.Equal(1.0, FlapSurfaceLock.TargetFraction(5, 3));
        }
    }
}
