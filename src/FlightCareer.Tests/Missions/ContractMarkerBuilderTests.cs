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
    }
}
