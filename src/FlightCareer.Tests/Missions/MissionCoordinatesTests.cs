using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions;

public class MissionCoordinatesTests
{
    [Fact]
    public void FormatDegreesMinutes_SulOeste()
    {
        string result = MissionCoordinates.FormatDegreesMinutes(-23.50833, -46.63667);
        Assert.Equal("S23°30,5' W046°38,2'", result);
    }

    [Fact]
    public void FormatDegreesMinutes_NorteLeste()
    {
        string result = MissionCoordinates.FormatDegreesMinutes(5.5, 100.25);
        Assert.Equal("N05°30,0' E100°15,0'", result);
    }

    [Fact]
    public void FormatDegreesMinutes_ArredondaParaOGrauSeguinte()
    {
        string result = MissionCoordinates.FormatDegreesMinutes(-22.99999, -46.99999);
        Assert.Equal("S23°00,0' W047°00,0'", result);
    }

    [Fact]
    public void For_PontoDeReporteEEscalas_OrdemERotulos()
    {
        var stopsData = new MultiStopContractData
        {
            Stops = new()
            {
                new StopPoint { Icao = "SDSM", Lat = -23.2, Lon = -46.6 },
                new StopPoint { Icao = "SDXX", Lat = -23.3, Lon = -46.7 }
            }
        };

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OverflyWaypoints = "P:Represa@-23.1,-46.5",
            ContractData = JsonSerializer.Serialize(stopsData)
        };

        var points = MissionCoordinates.For(mission);

        Assert.Equal(3, points.Count);
        Assert.Equal("Represa", points[0].Label);
        Assert.Equal(-23.1, points[0].Lat);
        Assert.Equal(-46.5, points[0].Lon);

        Assert.Equal("1 · SDSM", points[1].Label);
        Assert.Equal(-23.2, points[1].Lat);
        Assert.Equal(-46.6, points[1].Lon);

        Assert.Equal("2 · SDXX", points[2].Label);
        Assert.Equal(-23.3, points[2].Lat);
        Assert.Equal(-46.7, points[2].Lon);
    }

    [Fact]
    public void For_EscalaSemCoordenada_PuladaMantendoNumeracao()
    {
        var stopsData = new MultiStopContractData
        {
            Stops = new()
            {
                new StopPoint { Icao = "SD00", Lat = 0, Lon = 0 },
                new StopPoint { Icao = "SDSM", Lat = -23.2, Lon = -46.6 }
            }
        };

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            ContractData = JsonSerializer.Serialize(stopsData)
        };

        var points = MissionCoordinates.For(mission);

        Assert.Single(points);
        Assert.Equal("2 · SDSM", points[0].Label);
        Assert.Equal(-23.2, points[0].Lat);
        Assert.Equal(-46.6, points[0].Lon);
    }

    [Fact]
    public void For_Stol_DevolvePista()
    {
        var stol = new StolContractData
        {
            StripIcao = "SJ99",
            Lat = -22.5,
            Lon = -45.8
        };

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            ContractData = JsonSerializer.Serialize(stol)
        };

        var points = MissionCoordinates.For(mission);

        Assert.Single(points);
        Assert.Equal("1 · SJ99", points[0].Label);
        Assert.Equal(-22.5, points[0].Lat);
        Assert.Equal(-45.8, points[0].Lon);
    }

    [Fact]
    public void For_IcaoEmOverfly_Ignorado()
    {
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OverflyWaypoints = "SBJD;SDCO;P:Represa@-23.1,-46.5"
        };

        var points = MissionCoordinates.For(mission);

        Assert.Single(points);
        Assert.Equal("Represa", points[0].Label);
        Assert.Equal(-23.1, points[0].Lat);
        Assert.Equal(-46.5, points[0].Lon);
    }

    [Fact]
    public void For_JsonInvalido_ListaVazia()
    {
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            ContractData = "{ invalid json content! }"
        };

        var points = MissionCoordinates.For(mission);

        Assert.Empty(points);
    }

    [Fact]
    public void For_Null_ListaVazia()
    {
        var points = MissionCoordinates.For(null);
        Assert.Empty(points);
    }

    [Fact]
    public void CopyText_ComCulturaPtBr_MantemPontoDecimal()
    {
        var prevCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");
            var point = new MissionCoordinatePoint("Ponto Teste", -23.50833, -46.63667);

            Assert.Equal("-23.50833, -46.63667", point.CopyText);
            Assert.Equal("S23°30,5' W046°38,2'", point.Display);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = prevCulture;
        }
    }
}
