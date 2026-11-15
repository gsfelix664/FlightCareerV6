using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class FlapSurfaceLockTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(2, 1)]
        [InlineData(1, -1)]
        public void TargetFraction_PositionsMenorOuIgualAUm_RetornaZero(double index, double positions)
        {
            double result = FlapSurfaceLock.TargetFraction(index, positions);
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void TargetFraction_C152QuatroPosicoes_CalculaCorretamente()
        {
            // C152: 4 posições (UP=0, 10=1, 20=2, 30=3) -> divisor 3
            Assert.Equal(0.0, FlapSurfaceLock.TargetFraction(0, 4));
            Assert.Equal(1.0 / 3.0, FlapSurfaceLock.TargetFraction(1, 4), precision: 4);
            Assert.Equal(2.0 / 3.0, FlapSurfaceLock.TargetFraction(2, 4), precision: 4);
            Assert.Equal(1.0, FlapSurfaceLock.TargetFraction(3, 4));
        }

        [Fact]
        public void TargetFraction_IndiceNegativo_LimitaAZero()
        {
            double result = FlapSurfaceLock.TargetFraction(-1, 4);
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void TargetFraction_IndiceAcimaDoMaximo_LimitaAUm()
        {
            double result = FlapSurfaceLock.TargetFraction(5, 4);
            Assert.Equal(1.0, result);
        }
    }
}
