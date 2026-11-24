using System;
using System.Collections.Generic;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class VfrTriangleTests
    {
        [Fact]
        public void SemPontoDeReportagem_FalhaSobrevoo()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "navegacao_vfr",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                OverflyWaypoints = "P:Ponto de Reportagem VFR@-22.8,-47.1,2.0,3000"
            };

            var multiTracker = new MultiStopTracker();
            multiTracker.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SDCO", Lat = -23.4808, Lon = -47.4897, ElevationFt = 2077 },
                    new() { Icao = "SBDN", Lat = -22.1969, Lon = -49.9261, ElevationFt = 1476 }
                }
            });
            // Cumpre as escalas no tracker
            multiTracker.Update(false, -23.1, -47.0, 100, 40, 10);
            multiTracker.Update(true, -23.4808, -47.4897, 1.0, 35, 6); // SDCO
            multiTracker.Update(false, -23.4808, -47.4897, 100, 34, 6);
            multiTracker.Update(true, -22.1969, -49.9261, 1.0, 30, 6); // SBDN

            var trackerResults = new MissionTrackerResults(
                VisitedWaypoints: new HashSet<string>(), // Ponto NÃO visitado
                MultiStop: multiTracker
            );

            var result = MissionCompletion.Evaluate(mission, "SBJD", trackers: trackerResults);

            Assert.False(result.IsCompleted);
            Assert.Contains("Sobrevoo incompleto", result.Reason);
        }

        [Fact]
        public void Completo_ComPontoDeReportagemEParadas_Sucesso()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "navegacao_vfr",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                OverflyWaypoints = "P:Ponto de Reportagem VFR@-22.8,-47.1,2.0,3000"
            };

            var multiTracker = new MultiStopTracker();
            multiTracker.Configure(new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SDCO", Lat = -23.4808, Lon = -47.4897, ElevationFt = 2077 },
                    new() { Icao = "SBDN", Lat = -22.1969, Lon = -49.9261, ElevationFt = 1476 }
                }
            });
            // Cumpre escalas
            multiTracker.Update(false, -23.1, -47.0, 100, 40, 10);
            multiTracker.Update(true, -23.4808, -47.4897, 1.0, 35, 6); // SDCO
            multiTracker.Update(false, -23.4808, -47.4897, 100, 34, 6);
            multiTracker.Update(true, -22.1969, -49.9261, 1.0, 30, 6); // SBDN

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ponto de Reportagem VFR" };
            var trackerResults = new MissionTrackerResults(
                VisitedWaypoints: visited,
                MultiStop: multiTracker
            );

            var result = MissionCompletion.Evaluate(mission, "SBJD", trackers: trackerResults);

            Assert.True(result.IsCompleted);
            Assert.Null(result.Reason);
        }
    }
}
