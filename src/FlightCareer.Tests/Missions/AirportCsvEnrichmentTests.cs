using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    /// <summary>O airports.csv só completa cidade/país/elevação quando o código é o mesmo aeródromo do simulador.</summary>
    public class AirportCsvEnrichmentTests
    {
        [Fact]
        public void IsSameAirportPosition_CodigoReaproveitado_NaoBate()
        {
            // SBOK: airports.csv = Fazenda São Vicente (Lucas do Rio Verde/MT); MSFS 2024 = Amontada (CE)
            Assert.False(DataSeeder.IsSameAirportPosition(-13.290168, -55.975493, -3.090261, -39.660091));
        }

        [Fact]
        public void IsSameAirportPosition_MesmoAerodromo_Bate()
        {
            // mesmo aeródromo com pequena diferença de coordenada entre as bases (~2 NM)
            Assert.True(DataSeeder.IsSameAirportPosition(-23.4356, -46.4731, -23.4320, -46.4400));
        }

        [Fact]
        public void IsSameAirportPosition_LimiteDeDezMilhas()
        {
            Assert.True(DataSeeder.IsSameAirportPosition(0, 0, 9.9 / 60.0, 0));
            Assert.False(DataSeeder.IsSameAirportPosition(0, 0, 10.5 / 60.0, 0));
        }
    }
}
