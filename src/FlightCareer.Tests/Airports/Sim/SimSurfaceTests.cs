using FlightCareer.Domain.Airports.Sim;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Airports.Sim
{
    public class SimSurfaceTests
    {
        [Fact]
        public void SimSurface_Codigos_MapeiamNomes()
        {
            // 4 -> Asphalt, IsPaved
            Assert.Equal("Asphalt", SimSurface.ToSurfaceName(4));
            Assert.True(SimSurface.IsPaved(4));
            Assert.False(SimSurface.IsWater(4));

            // 1 -> Grass, não pavimentada
            Assert.Equal("Grass", SimSurface.ToSurfaceName(1));
            Assert.False(SimSurface.IsPaved(1));
            Assert.False(SimSurface.IsWater(1));

            // 27 -> Water
            Assert.Equal("Water", SimSurface.ToSurfaceName(27));
            Assert.True(SimSurface.IsWater(27));
            Assert.False(SimSurface.IsPaved(27));

            // 99 -> Other
            Assert.Equal("Other", SimSurface.ToSurfaceName(99));
            Assert.False(SimSurface.IsPaved(99));
            Assert.False(SimSurface.IsWater(99));

            // Compatibilidade com RunwaySurface
            Assert.True(RunwaySurface.IsHard("Asphalt"));
            Assert.False(RunwaySurface.IsHard("Grass"));
        }
    }
}
