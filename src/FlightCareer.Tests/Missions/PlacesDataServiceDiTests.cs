using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class PlacesDataServiceDiTests
    {
        [Fact]
        public void ResolvidoPeloContainer_CarregaOsDadosDosArquivos()
        {
            // Mesmo registro do App.xaml.cs
            using var provider = new ServiceCollection()
                .AddSingleton<IPlacesDataService, PlacesDataService>()
                .BuildServiceProvider();

            var places = provider.GetRequiredService<IPlacesDataService>();

            // "Aeroclube de Tatuí", a 23 NM de SDCO
            Assert.NotEmpty(places.FindTrainingAreas(-23.4778, -47.4900));
            Assert.True(places.IsTrainingAerodrome("SDCO"));
        }
    }
}
