using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class TrainingAreasTests
    {
        private static Airport CreateTatuiAirport()
        {
            return new Airport
            {
                ICAO = "SDTI",
                Name = "Aeroclube de Tatuí",
                City = "Tatuí",
                Country = "BR",
                Tier = 3,
                Latitude = -23.33,
                Longitude = -47.88,
                ElevationFt = 2073
            };
        }

        private static MissionDefinition GetSubConfig(string id)
        {
            return MissionCatalog.Shared.Get(id)
                ?? MissionCatalog.Shared.Missions.Values.FirstOrDefault(m => m.LegacyCodes.Contains(id, StringComparer.OrdinalIgnoreCase))
                ?? new MissionDefinition { Id = id, Name = "Test Sub" };
        }

        [Fact]
        public void TryResolveRealAreaBand_SfcFloorMslCeil_ConvertsWithElevation()
        {
            // área piso 500 SFC, teto 4.000 MSL, elevação 1.000 → min 1.500, max 3.000, true
            var area = new TrainingAreaInfo(
                "SBR595", "Capivari", "instrucao", -25.196, -48.989, 8.8, 500, "SFC", 4000, "MSL");

            bool ok = MissionGenerator.TryResolveRealAreaBand(area, 1000, out int minAgl, out int maxAgl);

            Assert.True(ok);
            Assert.Equal(1500, minAgl);
            Assert.Equal(3000, maxAgl);
        }

        [Fact]
        public void TryResolveRealAreaBand_NarrowBand_ReturnsFalse()
        {
            // piso 0 SFC, teto 1.500 MSL, elevação 100 → false
            var area = new TrainingAreaInfo(
                "TEST", "Área Estreita", "instrucao", -20.0, -40.0, 5.0, 0, "SFC", 1500, "MSL");

            bool ok = MissionGenerator.TryResolveRealAreaBand(area, 100, out int minAgl, out int maxAgl);

            Assert.False(ok);
            Assert.Equal(0, minAgl);
            Assert.Equal(0, maxAgl);
        }

        [Fact]
        public void TryResolveRealAreaBand_FlightLevelCeil_CapsAt1500AboveMin()
        {
            // piso 0 SFC, teto 20.000 MSL, elevação 2.000 → 1.500 a 3.000
            var area = new TrainingAreaInfo(
                "SBR806", "Aeroclube de Tatuí", "instrucao", -23.34887, -47.88198, 3.4, 0, "SFC", 20000, "MSL");

            bool ok = MissionGenerator.TryResolveRealAreaBand(area, 2000, out int minAgl, out int maxAgl);

            Assert.True(ok);
            Assert.Equal(1500, minAgl);
            Assert.Equal(3000, maxAgl);
        }

        [Fact]
        public void FindTrainingAreas_FiltersByDistanceAndKind()
        {
            // 3 áreas de teste (uma a 10 NM instrução, uma a 40 NM instrução, uma acrobacia a 5 NM) → só a primeira
            double d10Lat = 10.0 / 60.0;
            double d40Lat = 40.0 / 60.0;
            double d5Lat = 5.0 / 60.0;

            var a1 = new TrainingAreaInfo("AREA1", "Instrução Perto", "instrucao", d10Lat, 0.0, 5.0, 0, "SFC", 5000, "MSL");
            var a2 = new TrainingAreaInfo("AREA2", "Instrução Longe", "instrucao", d40Lat, 0.0, 5.0, 0, "SFC", 5000, "MSL");
            var a3 = new TrainingAreaInfo("AREA3", "Acrobacia Perto", "acrobacia", d5Lat, 0.0, 2.0, 0, "SFC", 5000, "MSL");

            var service = new PlacesDataService(null, null, null, null, new[] { a1, a2, a3 });
            var result = service.FindTrainingAreas(0.0, 0.0, 25.0, "instrucao");

            Assert.Single(result);
            Assert.Equal("AREA1", result[0].Id);
        }

        [Fact]
        public void LocalInstruction_WithRealArea_UsesAreaCenterNameAndBand()
        {
            var origin = CreateTatuiAirport();
            var sub = GetSubConfig("instrucao_local");
            var area = new TrainingAreaInfo(
                "SBR806", "Aeroclube de Tatuí", "instrucao", -23.34887, -47.88198, 3.4, 0, "SFC", 20000, "MSL");
            var service = new PlacesDataService(null, null, null, null, new[] { area });

            var mission = MissionGenerator.CreateLocalInstructionMissionSync(
                origin, sub, new Random(42), DateTime.UtcNow, placesService: service);

            Assert.StartsWith("P:Área SBR806 Aeroclube de Tatuí", mission.OverflyWaypoints);
            Assert.Contains("-23.34887", mission.OverflyWaypoints);
            Assert.Contains("-47.88198", mission.OverflyWaypoints);
            Assert.Contains("Aeroclube de Tatuí", mission.Description);
        }

        [Fact]
        public void LocalInstruction_WithoutRealArea_SameAsBefore()
        {
            var origin = CreateTatuiAirport();
            var sub = GetSubConfig("instrucao_local");
            var emptyService = new PlacesDataService(null, null, null, null, Array.Empty<TrainingAreaInfo>());

            var mNull = MissionGenerator.CreateLocalInstructionMissionSync(
                origin, sub, new Random(999), DateTime.UtcNow, placesService: null);

            var mEmpty = MissionGenerator.CreateLocalInstructionMissionSync(
                origin, sub, new Random(999), DateTime.UtcNow, placesService: emptyService);

            Assert.Equal(mNull.OverflyWaypoints, mEmpty.OverflyWaypoints);
            Assert.Equal(mNull.Title, mEmpty.Title);
            Assert.Equal(mNull.Description, mEmpty.Description);
            Assert.Equal(mNull.RequiredTouchAndGoes, mEmpty.RequiredTouchAndGoes);
            Assert.Equal(mNull.DistanceNM, mEmpty.DistanceNM);
        }

        [Fact]
        public void Checkride_WithRealArea_UsesArea()
        {
            var origin = CreateTatuiAirport();
            var sub = new MissionDefinition { Id = "checkride", Name = "Voo de Cheque" };
            var area = new TrainingAreaInfo(
                "SBR806", "Aeroclube de Tatuí", "instrucao", -23.34887, -47.88198, 3.4, 0, "SFC", 20000, "MSL");
            var service = new PlacesDataService(null, null, null, null, new[] { area });

            var mission = MissionGenerator.CreateCheckrideMissionSync(
                origin, sub, new Random(42), DateTime.UtcNow, placesService: service);

            Assert.StartsWith("P:Área SBR806 Aeroclube de Tatuí", mission.OverflyWaypoints);
            Assert.Contains("-23.34887", mission.OverflyWaypoints);
            Assert.Contains("-47.88198", mission.OverflyWaypoints);
            Assert.Contains("Aeroclube de Tatuí", mission.Description);
        }

        [Fact]
        public void TrainingAreasJson_LoadsAndHasExpectedIds()
        {
            var service = new PlacesDataService();
            var areas = service.FindTrainingAreas(-23.33, -47.88, 25.0);

            Assert.Contains(areas, a => a.Id == "SBR806");
            Assert.DoesNotContain(areas, a => string.Equals(a.Kind, "acrobacia", StringComparison.OrdinalIgnoreCase));
        }
    }
}
