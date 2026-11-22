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
            Assert.Equal(16, service.ScenicAerodromesCount);
        }

        [Fact]
        public void LoadRealFiles_ScenicAerodromes_LoadedWith16Items_SbjrHasPointsAndNote()
        {
            var service = new PlacesDataService();

            Assert.Equal(16, service.ScenicAerodromesCount);
            var sbjr = service.GetScenicAerodrome("SBJR");
            Assert.NotNull(sbjr);
            Assert.True(sbjr!.Points.Count >= 3, $"SBJR deve ter ao menos 3 pontos, encontrado {sbjr.Points.Count}");
            Assert.False(string.IsNullOrWhiteSpace(sbjr.Note), "Nota de SBJR não pode ser vazia.");
            Assert.Contains(sbjr.Points, p => p.Name.Contains("Cristo Redentor"));
        }

        [Fact]
        public void FindPois_AroundKnownLocation_ReturnsNearbyPois()
        {
            var service = new PlacesDataService();
            // Jundiaí / São Paulo (-23.18, -46.94)
            var pois = service.FindPois(-23.18, -46.94, 2.0, 30.0);

            Assert.NotEmpty(pois);
        }

        [Theory]
        [InlineData("SDOI", "Centro Nacional de Paraquedismo")]
        [InlineData("SIZD", "DropZone 47 Clube Escola de Paraquedismo")]
        [InlineData("L65", "Skydive Perris")]
        [InlineData("OMDU", "Skydive Dubai Palm Dropzone")]
        public void Zonas_curadas_tem_nome_e_nota_com_o_codigo_do_simulador(string icao, string name)
        {
            var dz = new PlacesDataService().GetDropZone(icao);

            Assert.NotNull(dz);
            Assert.Equal(name, dz!.Name);
            Assert.False(string.IsNullOrWhiteSpace(dz.Note));
        }
    }
}
