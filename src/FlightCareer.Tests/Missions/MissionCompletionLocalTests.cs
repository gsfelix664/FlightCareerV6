using System.Collections.Generic;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionCompletionLocalTests
    {
        [Fact]
        public void Evaluate_MixedWaypointList_AllVisited_CompletesSuccessfully()
        {
            var mission = new Mission
            {
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                OverflyWaypoints = "SBJD; P:Represa de Jundiaí@-23.15,-46.85,2.0,3500; SBSP"
            };

            var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "SBJD",
                "Represa de Jundiaí",
                "SBSP"
            };

            var result = MissionCompletion.Evaluate(mission, "SBJD", visitedWaypoints: visited);
            Assert.True(result.IsCompleted);
        }

        [Fact]
        public void Evaluate_MixedWaypointList_MissingPoint_Fails()
        {
            var mission = new Mission
            {
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                OverflyWaypoints = "SBJD; P:Represa de Jundiaí@-23.15,-46.85,2.0,3500; SBSP"
            };

            var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "SBJD",
                "SBSP"
            };

            var result = MissionCompletion.Evaluate(mission, "SBJD", visitedWaypoints: visited);
            Assert.False(result.IsCompleted);
            Assert.Contains("Represa de Jundiaí", result.Reason);
        }

        [Fact]
        public void Evaluate_Subtype341_LocalInstruction_AllRequirementsMet_Completes()
        {
            var mission = new Mission
            {
                SubId = "3.4.1",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                RequiredTouchAndGoes = 3,
                OverflyWaypoints = "P:Área de instrução NW@-23.05,-47.00,3.0,6000"
            };

            var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "Área de instrução NW"
            };

            var result = MissionCompletion.Evaluate(mission, "SBJD", visitedWaypoints: visited, touchAndGoCount: 3);
            Assert.True(result.IsCompleted);
        }

        [Fact]
        public void Evaluate_Subtype341_MissingTglOrArea_Fails()
        {
            var mission = new Mission
            {
                SubId = "3.4.1",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                RequiredTouchAndGoes = 2,
                OverflyWaypoints = "P:Área de instrução NE@-23.05,-46.80,3.0,6000"
            };

            // Falta TGL
            var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "Área de instrução NE"
            };
            var resNoTgl = MissionCompletion.Evaluate(mission, "SBJD", visitedWaypoints: visited, touchAndGoCount: 1);
            Assert.False(resNoTgl.IsCompleted);
            Assert.Contains("TGL", resNoTgl.Reason);

            // Falta Área
            var visitedEmpty = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var resNoArea = MissionCompletion.Evaluate(mission, "SBJD", visitedWaypoints: visitedEmpty, touchAndGoCount: 2);
            Assert.False(resNoArea.IsCompleted);
            Assert.Contains("Área de instrução NE", resNoArea.Reason);
        }

        [Fact]
        public void Evaluate_Subtype343_Checkride_RequiresAreaAnd2TglAndOrigin()
        {
            var mission = new Mission
            {
                SubId = "3.4.3",
                OriginICAO = "SBJD",
                DestinationICAO = "SBJD",
                RequiredTouchAndGoes = 2,
                PassengerCount = 1,
                OverflyWaypoints = "P:Área de instrução S@-23.25,-46.90,3.0,6000"
            };

            var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "Área de instrução S"
            };

            // Pousou em outro aeroporto
            var resWrongDest = MissionCompletion.Evaluate(mission, "SBSP", visitedWaypoints: visited, touchAndGoCount: 2);
            Assert.False(resWrongDest.IsCompleted);

            // Pousou na origem com 2 TGLs e área visitada
            var resOk = MissionCompletion.Evaluate(mission, "SBJD", visitedWaypoints: visited, touchAndGoCount: 2);
            Assert.True(resOk.IsCompleted);
        }
    }
}
