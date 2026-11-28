using FlightCareer.Domain.Services;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class ProcedurePenaltiesTests
    {
        [Fact]
        public void ApplyAutoStartPenalty_100_Retorna90()
        {
            int result = ProcedurePenalties.ApplyAutoStartPenalty(100, autoStartUsed: true);
            Assert.Equal(90, result);
        }

        [Fact]
        public void ApplyAutoStartPenalty_0_Retorna0()
        {
            int result = ProcedurePenalties.ApplyAutoStartPenalty(0, autoStartUsed: true);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ApplyAutoStartPenalty_NaoUsado_RetornaIgual()
        {
            int result1 = ProcedurePenalties.ApplyAutoStartPenalty(100, autoStartUsed: false);
            int result2 = ProcedurePenalties.ApplyAutoStartPenalty(7, autoStartUsed: false);

            Assert.Equal(100, result1);
            Assert.Equal(7, result2);
        }

        [Fact]
        public void ApplyAutoStartPenalty_7_Retorna6()
        {
            int result = ProcedurePenalties.ApplyAutoStartPenalty(7, autoStartUsed: true);
            Assert.Equal(6, result);
        }

        [Fact]
        public void ApplyAutoStartPenalty_Negativo_RetornaZero()
        {
            int result = ProcedurePenalties.ApplyAutoStartPenalty(-10, autoStartUsed: true);
            Assert.Equal(0, result);
        }
    }
}
