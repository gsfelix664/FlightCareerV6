using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Career;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionGeneratorLocalTests
    {
        private static Airport CreateOriginAirport(string icao = "SBJD", double lat = -23.18, double lon = -46.94)
        {
            return new Airport
            {
                ICAO = icao,
                Name = "Aeroporto Estadual de Jundiaí",
                City = "Jundiaí",
                Country = "BR",
                Tier = 2,
                Latitude = lat,
                Longitude = lon,
                ElevationFt = 2484
            };
        }

        private static SubCategoryConfig GetSubConfig(string id)
        {
            foreach (var t in MissionRules.Rules.Values)
            {
                foreach (var c in t.Categories)
                {
                    var s = c.SubCategories.FirstOrDefault(x => x.Id == id);
                    if (s != null) return s;
                }
            }
            return new SubCategoryConfig { Id = id, Name = "Test Sub" };
        }

        [Fact]
        public void Subtype341_LocalInstruction_GeneratesProperAreaAndTgl()
        {
            var origin = CreateOriginAirport();
            var sub = GetSubConfig("3.4.1");
            var rng = new Random(42);

            var mission = MissionGenerator.CreateLocalInstructionMissionSync(
                origin, sub, rng, DateTime.UtcNow);

            Assert.Equal("3.4.1", mission.SubId);
            Assert.Equal(origin.ICAO, mission.OriginICAO);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);
            Assert.InRange(mission.RequiredTouchAndGoes, 1, 3);
            Assert.False(string.IsNullOrWhiteSpace(mission.OverflyWaypoints));
            Assert.StartsWith("P:Área de instrução", mission.OverflyWaypoints);

            // Confere formatação sem marcadores soltos
            Assert.DoesNotMatch(@"\{[a-zA-Z0-9_]+\}", mission.Title);
            Assert.DoesNotMatch(@"\{[a-zA-Z0-9_]+\}", mission.Description);
        }

        [Fact]
        public void Subtype343_Checkride_RequiresExaminerAndSimulatedFailure()
        {
            var origin = CreateOriginAirport();
            var sub = GetSubConfig("3.4.3");
            var rng = new Random(100);

            var mission = MissionGenerator.CreateCheckrideMissionSync(
                origin, sub, rng, DateTime.UtcNow);

            Assert.Equal("3.4.3", mission.SubId);
            Assert.Equal(origin.ICAO, mission.OriginICAO);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);
            Assert.Equal(1, mission.PassengerCount);
            Assert.Equal(2, mission.RequiredTouchAndGoes);

            var combinedText = (mission.Title + " " + mission.Description).ToLowerInvariant();
            Assert.True(combinedText.Contains("cheque") || combinedText.Contains("examinador") || combinedText.Contains("pane") || combinedText.Contains("simulad"));
            Assert.DoesNotMatch(@"\{[a-zA-Z0-9_]+\}", mission.Title);
            Assert.DoesNotMatch(@"\{[a-zA-Z0-9_]+\}", mission.Description);
        }

        [Fact]
        public void Subtype351_ScenicTour_WithPois_UsesPois_WithoutPois_ReturnsNull()
        {
            var origin = CreateOriginAirport();
            var sub = GetSubConfig("3.5.1");
            var rng = new Random(200);

            // Mock com 3 POIs a ~10, ~15 e ~20 NM da origem (-23.18, -46.94)
            var mockPois = new List<TouristPoiInfo>
            {
                new("poi1", "Parque da Cidade", -23.01, -46.94, "parque", "BR", 80),
                new("poi2", "Serra do Japi", -23.18, -46.67, "parque_nacional", "BR", 90),
                new("poi3", "Represa Municipal", -23.51, -46.94, "lago", "BR", 70)
            };
            var placesWithPois = new PlacesDataService(dropZones: null, trainingAerodromes: null, pois: mockPois);

            var missionWithPois = MissionGenerator.CreateScenicTourMissionSync(
                origin, sub, rng, DateTime.UtcNow, placesService: placesWithPois);

            Assert.NotNull(missionWithPois);
            Assert.Contains("P:", missionWithPois!.OverflyWaypoints);
            var parsed = OverflyWaypointCodec.Parse(missionWithPois.OverflyWaypoints);
            Assert.InRange(parsed.Count, 2, 3);

            // Sem POIs -> retorna null (para fazer fallback aos aeródromos no gerador principal)
            var placesEmpty = new PlacesDataService(dropZones: null, trainingAerodromes: null, pois: Array.Empty<TouristPoiInfo>());
            var missionEmpty = MissionGenerator.CreateScenicTourMissionSync(
                origin, sub, rng, DateTime.UtcNow, placesService: placesEmpty);
            Assert.Null(missionEmpty);
        }

        [Fact]
        public void Subtype361_Survey_WithInfrastructurePoi_CentersOnPoi()
        {
            var origin = CreateOriginAirport();
            var sub = GetSubConfig("3.6.1");
            var rng = new Random(300);

            var mockPois = new List<TouristPoiInfo>
            {
                new("bar1", "Usina Hidrelétrica de Porto Primavera", -22.50, -52.90, "barragem", "BR", 100)
            };
            // Coloca a origem a ~10 NM da barragem (faixa 5 a 25 NM)
            var originNearDam = CreateOriginAirport("SBNP", -22.35, -52.85);
            var places = new PlacesDataService(dropZones: null, trainingAerodromes: null, pois: mockPois);

            var mission = MissionGenerator.CreateSurveyMissionSync(
                originNearDam, sub, rng, DateTime.UtcNow, placesService: places);

            Assert.Equal("3.6.1", mission.SubId);
            Assert.NotNull(mission.ContractData);

            var contract = JsonSerializer.Deserialize<SurveyContractData>(mission.ContractData);
            Assert.NotNull(contract);
            // Deve estar centralizado no POI da barragem
            Assert.Equal(-22.50, contract!.CenterLatitude, 3);
            Assert.Equal(-52.90, contract.CenterLongitude, 3);
            Assert.Contains("barragem", mission.Description.ToLowerInvariant());
        }

        [Fact]
        public void Subtype362_Skydive_WithDropZone_UsesDzCoordinatesAndName()
        {
            var origin = CreateOriginAirport("SDTI", -23.28, -47.67); // Boituva
            var sub = GetSubConfig("3.6.2");
            var rng = new Random(400);

            var mockDzs = new List<DropZoneInfo>
            {
                new("SDTI", "Boituva Parachuting Center", "BR", "Centro Nacional de Paraquedismo", -23.295, -47.685)
            };
            var places = new PlacesDataService(dropZones: mockDzs, trainingAerodromes: null, pois: null);

            var mission = MissionGenerator.CreateSkydiveMissionSync(
                origin, sub, rng, DateTime.UtcNow, placesService: places, engineClass: "turboprop");

            Assert.Equal("3.6.2", mission.SubId);
            Assert.NotNull(mission.ContractData);

            var contract = JsonSerializer.Deserialize<SkydiveContractData>(mission.ContractData);
            Assert.NotNull(contract);
            Assert.Equal(-23.295, contract!.DropZoneLatitude, 3);
            Assert.Equal(-47.685, contract.DropZoneLongitude, 3);
            Assert.InRange(contract.TargetAltitudeAglFt, 12000, 14500); // turboprop: 12k-14k
            Assert.Contains("Centro Nacional de Paraquedismo", mission.Description);
        }

        [Fact]
        public void MissionGenerator_DropZoneWeightSkew_GeneratesRoughly80PercentSkydive()
        {
            var origin = CreateOriginAirport("SDTI", -23.28, -47.67);
            var mockDzs = new List<DropZoneInfo>
            {
                new("SDTI", "Boituva", "BR", "DZ Boituva", -23.28, -47.67)
            };
            var places = new PlacesDataService(dropZones: mockDzs, trainingAerodromes: null, pois: null);

            var allAirports = new List<Airport> { origin };
            var rng = new Random(777);

            int skydiveCount = 0;
            int surveyCount = 0;

            for (int i = 0; i < 500; i++)
            {
                var missions = MissionGenerator.GenerateMissionsInMemory(
                    currentAirport: origin,
                    allAirports: allAirports,
                    hubService: null,
                    random: rng,
                    referenceTime: DateTime.UtcNow,
                    countOverride: 15,
                    placesService: places);

                foreach (var m in missions)
                {
                    if (m.SubId == "3.6.2") skydiveCount++;
                    else if (m.SubId == "3.6.1") surveyCount++;
                }
            }

            int totalSae = skydiveCount + surveyCount;
            Assert.True(totalSae > 0, "Deveria ter gerado contratos SAE (3.6.x)");

            double skydiveRatio = (double)skydiveCount / totalSae;
            // Proporção esperada: 80% de 3.6.2 vs 20% de 3.6.1. Tolerância estatística [0.70, 0.90].
            Assert.InRange(skydiveRatio, 0.70, 0.90);
        }
    }
}
