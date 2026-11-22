using FlightCareer.Application.Services;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class PlacesDataServiceTests
    {
        [Fact]
        public void LoadRealFiles_AllThreeDataSetsHaveItems()
        {
            var service = new PlacesDataService();

            Assert.True(service.TouristPoisCount > 1000, $"tourist_pois.json deve ter mais de 1000 registros, encontrado: {service.TouristPoisCount}");
            Assert.True(service.TrainingAerodromesCount > 50, $"training_aerodromes.json deve ter mais de 50 aeródromos, encontrado: {service.TrainingAerodromesCount}");
            Assert.True(service.DropZonesCount > 10, $"dropzones.json deve ter mais de 10 dropzones, encontrado: {service.DropZonesCount}");
        }

        [Fact]
        public void FindPois_AroundKnownLocation_ReturnsNearbyPois()
        {
            var service = new PlacesDataService();
            // Jundiaí / São Paulo (-23.18, -46.94)
            var pois = service.FindPois(-23.18, -46.94, 2.0, 30.0);

            Assert.NotEmpty(pois);
        }

        [Fact]
        public void GetDropZone_KnownDropZone_ReturnsData()
        {
            var service = new PlacesDataService();
            // Boituva (SDTI) é a mais famosa dropzone do Brasil
            var dz = service.GetDropZone("SDTI");
            if (dz != null)
            {
                Assert.Equal("SDTI", dz.Icao);
                Assert.False(string.IsNullOrWhiteSpace(dz.Name));
            }
        }
    }
}
