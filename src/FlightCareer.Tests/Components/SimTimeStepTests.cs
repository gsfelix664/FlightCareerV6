using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class SimTimeStepTests
    {
        [Theory]
        [InlineData(5.0, 1.0, 5.0)]
        [InlineData(5.0, 4.0, 20.0)]
        [InlineData(5.0, 8.0, 40.0)]
        [InlineData(5.0, 16.0, 80.0)]
        [InlineData(1.0, 64.0, 64.0)]
        public void Acelerada_nao_perde_tempo_simulado(double wall, double rate, double expectedSimSeconds)
        {
            Assert.Equal(expectedSimSeconds, SimTimeStep.ToSimSeconds(wall, rate), 6);
        }

        [Fact]
        public void Pausa_longa_e_limitada_no_tempo_real_antes_do_sim_rate()
        {
            // 5 min parado a 4×: conta só 10 s reais = 40 s simulados
            Assert.Equal(40.0, SimTimeStep.ToSimSeconds(300.0, 4.0), 6);
        }

        [Theory]
        [InlineData(0.0, 1.0)]
        [InlineData(-3.0, 1.0)]
        [InlineData(double.NaN, 1.0)]
        public void Intervalo_invalido_vale_zero(double wall, double rate)
        {
            Assert.Equal(0.0, SimTimeStep.ToSimSeconds(wall, rate));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-2.0)]
        [InlineData(double.NaN)]
        public void Sim_rate_invalido_vale_1x(double rate)
        {
            Assert.Equal(5.0, SimTimeStep.ToSimSeconds(5.0, rate), 6);
        }

        [Fact]
        public void Sim_rate_absurdo_e_limitado()
        {
            Assert.Equal(5.0 * SimTimeStep.MaxSimRate, SimTimeStep.ToSimSeconds(5.0, 10_000.0), 6);
        }

        [Fact]
        public void Horas_e_segundos_coerentes_e_passo_maximo_cabe_no_limite_da_sessao()
        {
            Assert.Equal(40.0 / 3600.0, SimTimeStep.ToSimHours(5.0, 8.0), 9);
            Assert.True(SimTimeStep.ToSimHours(1_000.0, 1_000.0) <= SimTimeStep.MaxSimHoursPerTick + 1e-12);
        }

        [Fact]
        public void Desgaste_a_16x_igual_a_16_vezes_o_desgaste_a_1x()
        {
            // Regressão: com o limite antigo de 0,005 h aplicado depois do sim rate, 16× desgastava ~22% do devido
            double at1x = SimTimeStep.ToSimHours(5.0, 1.0);
            double at16x = SimTimeStep.ToSimHours(5.0, 16.0);
            Assert.Equal(16.0 * at1x, at16x, 9);
            Assert.True(at16x > 0.005);
        }
    }
}
