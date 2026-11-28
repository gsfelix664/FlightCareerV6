using System;
using System.Collections.Generic;
using System.Text.Json;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class ContractMarkerBuilderTests
    {
        [Fact]
        public void Build_341InstructionAnd351Scenic_GeneratesAreaAndPointMarkers()
        {
            var m1 = new Mission
            {
                SubId = "3.4.1",
                OverflyWaypoints = "P:Área de instrução NW@-23.10,-47.10,3.0,6000"
            };

            var m2 = new Mission
            {
                SubId = "3.5.1",
                OverflyWaypoints = "P:Pico do Jaraguá@-23.45,-46.76,1.5,3000"
            };

            var visuals = ContractMarkerBuilder.Build(new[] { m1, m2 });

            Assert.Equal(2, visuals.Markers.Count);
            Assert.Contains(visuals.Markers, m => m.Kind == ContractMarkerKind.Area && m.Label == "Área de instrução NW" && m.RadiusNm == 3.0);
            Assert.Contains(visuals.Markers, m => m.Kind == ContractMarkerKind.Point && m.Label == "Pico do Jaraguá" && m.RadiusNm == 1.5);
            Assert.Null(visuals.SurveyArea);
        }

        [Fact]
        public void Build_362Skydive_GeneratesDropZoneMarker()
        {
            var contract = new SkydiveContractData
            {
                DropZoneLatitude = -23.35,
                DropZoneLongitude = -47.85,
                TargetAltitudeAglFt = 10000,
                MaxDropSpeedKt = 90
            };

            var mission = new Mission
            {
                SubId = "3.6.2",
                ContractData = JsonSerializer.Serialize(contract)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { mission });

            Assert.Single(visuals.Markers);
            var marker = visuals.Markers[0];
            Assert.Equal(ContractMarkerKind.DropZone, marker.Kind);
            Assert.Equal(-23.35, marker.Lat, 3);
            Assert.Equal(-47.85, marker.Lon, 3);
            Assert.Equal(2.0, marker.RadiusNm);
        }

        [Fact]
        public void Build_361Survey_GeneratesSurveyAreaWithCornersAndLegs()
        {
            var contract = new SurveyContractData
            {
                CenterLatitude = -23.0,
                CenterLongitude = -47.0,
                WidthNM = 4.0,
                LengthNM = 2.0,
                OrientationDeg = 90.0,
                RequiredLegs = 3
            };

            var mission = new Mission
            {
                SubId = "3.6.1",
                ContractData = JsonSerializer.Serialize(contract)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { mission });

            Assert.NotNull(visuals.SurveyArea);
            Assert.Equal(4, visuals.SurveyArea!.Corners.Count);
            Assert.Equal(3, visuals.SurveyArea.Legs.Count);

            foreach (var leg in visuals.SurveyArea.Legs)
            {
                Assert.Equal(2, leg.Count);
            }
        }

        [Fact]
        public void Build_Triangulo_GeraEscalasNumeradasELinhaFechada()
        {
            var airportDict = new Dictionary<string, GeoPoint>(StringComparer.OrdinalIgnoreCase)
            {
                ["SDCO"] = new GeoPoint(-23.50, -46.50)
            };

            var multiStop = new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "AAAA", Lat = -23.20, Lon = -46.30 },
                    new() { Icao = "BBBB", Lat = -23.10, Lon = -46.20 }
                }
            };

            string overfly = OverflyWaypointCodec.FormatCoordinatePoint("Ponto de Reporte VFR", -23.40, -46.45);

            var mission = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SDCO",
                OverflyWaypoints = overfly,
                ContractData = JsonSerializer.Serialize(multiStop)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { mission }, icao => airportDict.TryGetValue(icao, out var pt) ? pt : null);

            // Marcadores: "1 · AAAA", "2 · BBBB" + o ponto de reporte
            Assert.Equal(3, visuals.Markers.Count);
            Assert.Contains(visuals.Markers, m => m.Kind == ContractMarkerKind.Stop && m.Label == "1 · AAAA" && m.RadiusNm == 0);
            Assert.Contains(visuals.Markers, m => m.Kind == ContractMarkerKind.Stop && m.Label == "2 · BBBB" && m.RadiusNm == 0);
            Assert.Contains(visuals.Markers, m => m.Kind == ContractMarkerKind.Point && m.Label == "Ponto de Reporte VFR");

            // 1 linha com 5 vértices na ordem origem, reporte, A, B, origem
            Assert.Single(visuals.Routes);
            var route = visuals.Routes[0];
            Assert.Equal(5, route.Points.Count);
            Assert.Equal(-23.50, route.Points[0].Lat, 2);
            Assert.Equal(-46.50, route.Points[0].Lon, 2);
            Assert.Equal(-23.40, route.Points[1].Lat, 2);
            Assert.Equal(-46.45, route.Points[1].Lon, 2);
            Assert.Equal(-23.20, route.Points[2].Lat, 2);
            Assert.Equal(-46.30, route.Points[2].Lon, 2);
            Assert.Equal(-23.10, route.Points[3].Lat, 2);
            Assert.Equal(-46.20, route.Points[3].Lon, 2);
            Assert.Equal(-23.50, route.Points[4].Lat, 2);
            Assert.Equal(-46.50, route.Points[4].Lon, 2);
        }

        [Fact]
        public void Build_PingaPinga_LinhaTerminaNoDestinoResolvido()
        {
            var airportDict = new Dictionary<string, GeoPoint>(StringComparer.OrdinalIgnoreCase)
            {
                ["SDCO"] = new GeoPoint(-23.50, -46.50),
                ["SBSP"] = new GeoPoint(-23.62, -46.65)
            };

            var multiStop = new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "AAAA", Lat = -23.20, Lon = -46.30 },
                    new() { Icao = "BBBB", Lat = -23.10, Lon = -46.20 }
                }
            };

            var mission = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SBSP",
                ContractData = JsonSerializer.Serialize(multiStop)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { mission }, icao => airportDict.TryGetValue(icao, out var pt) ? pt : null);

            Assert.Single(visuals.Routes);
            var route = visuals.Routes[0];
            Assert.Equal(4, route.Points.Count);
            // Último vértice é o destino resolvido (SBSP)
            Assert.Equal(-23.62, route.Points[^1].Lat, 2);
            Assert.Equal(-46.65, route.Points[^1].Lon, 2);
        }

        [Fact]
        public void Build_SemResolver_LinhaSoComEscalas()
        {
            // 2 escalas, sem resolver -> linha com 2 vértices
            var m2 = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SBSP",
                ContractData = JsonSerializer.Serialize(new MultiStopContractData
                {
                    Stops = new List<StopPoint>
                    {
                        new() { Icao = "AAAA", Lat = -23.20, Lon = -46.30 },
                        new() { Icao = "BBBB", Lat = -23.10, Lon = -46.20 }
                    }
                })
            };

            var v2 = ContractMarkerBuilder.Build(new[] { m2 }, null);
            Assert.Single(v2.Routes);
            Assert.Equal(2, v2.Routes[0].Points.Count);
            Assert.Equal(-23.20, v2.Routes[0].Points[0].Lat, 2);
            Assert.Equal(-23.10, v2.Routes[0].Points[1].Lat, 2);

            // com 1 escala e sem overfly -> nenhuma linha
            var m1 = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SBSP",
                ContractData = JsonSerializer.Serialize(new MultiStopContractData
                {
                    Stops = new List<StopPoint>
                    {
                        new() { Icao = "AAAA", Lat = -23.20, Lon = -46.30 }
                    }
                })
            };

            var v1 = ContractMarkerBuilder.Build(new[] { m1 }, null);
            Assert.Empty(v1.Routes);
        }

        [Fact]
        public void Build_EscalaSemCoordenada_PulaEMantemNumero()
        {
            // escalas [0,0], válida -> só "2 · X"
            var multiStop = new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "ZERO", Lat = 0.0, Lon = 0.0 },
                    new() { Icao = "X", Lat = -23.25, Lon = -46.35 }
                }
            };

            var mission = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SDCO",
                ContractData = JsonSerializer.Serialize(multiStop)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { mission });

            Assert.Single(visuals.Markers);
            Assert.Equal("2 · X", visuals.Markers[0].Label);
            Assert.Equal(ContractMarkerKind.Stop, visuals.Markers[0].Kind);
            Assert.Equal(-23.25, visuals.Markers[0].Lat, 2);
            Assert.Equal(-46.35, visuals.Markers[0].Lon, 2);
        }

        [Fact]
        public void Build_Stol_MarcaPistaELinhaIdaEVolta()
        {
            var airportDict = new Dictionary<string, GeoPoint>(StringComparer.OrdinalIgnoreCase)
            {
                ["SDCO"] = new GeoPoint(-23.50, -46.50)
            };

            var stol = new StolContractData
            {
                StripIcao = "FAZ1",
                Lat = -23.35,
                Lon = -46.40,
                ElevationFt = 2500
            };

            var mission = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SDCO",
                ContractData = JsonSerializer.Serialize(stol)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { mission }, icao => airportDict.TryGetValue(icao, out var pt) ? pt : null);

            // Marcador de pista
            Assert.Single(visuals.Markers);
            Assert.Equal("1 · FAZ1", visuals.Markers[0].Label);
            Assert.Equal(ContractMarkerKind.Stop, visuals.Markers[0].Kind);
            Assert.Equal(-23.35, visuals.Markers[0].Lat, 2);

            // 3 vértices: origem, pista, origem
            Assert.Single(visuals.Routes);
            var route = visuals.Routes[0];
            Assert.Equal(3, route.Points.Count);
            Assert.Equal(-23.50, route.Points[0].Lat, 2);
            Assert.Equal(-46.50, route.Points[0].Lon, 2);
            Assert.Equal(-23.35, route.Points[1].Lat, 2);
            Assert.Equal(-46.40, route.Points[1].Lon, 2);
            Assert.Equal(-23.50, route.Points[2].Lat, 2);
            Assert.Equal(-46.50, route.Points[2].Lon, 2);
        }

        [Fact]
        public void Build_MissoesIguais_NaoDuplicaMarcador()
        {
            var multiStop = new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "AAAA", Lat = -23.20, Lon = -46.30 }
                }
            };

            var m1 = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SDCO",
                ContractData = JsonSerializer.Serialize(multiStop)
            };

            var m2 = new Mission
            {
                OriginICAO = "SDCO",
                DestinationICAO = "SDCO",
                ContractData = JsonSerializer.Serialize(multiStop)
            };

            var visuals = ContractMarkerBuilder.Build(new[] { m1, m2 });

            Assert.Single(visuals.Markers);
            Assert.Equal("1 · AAAA", visuals.Markers[0].Label);
        }
    }
}
