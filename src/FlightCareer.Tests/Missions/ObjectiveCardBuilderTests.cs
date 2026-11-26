using System.Collections.Generic;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class ObjectiveCardBuilderTests
    {
        [Fact]
        public void ObjectiveCardBuilder_DestinosRepetidos_UmPousoFinalPorDestino()
        {
            var missions = new List<Mission>
            {
                new() { Title = "Instrução 1", OriginICAO = "SDCO", DestinationICAO = "SDCO" },
                new() { Title = "Instrução 2", OriginICAO = "SDCO", DestinationICAO = "SDCO" },
                new() { Title = "Translado", OriginICAO = "SDCO", DestinationICAO = "SBKP" }
            };

            var trackerLines = new List<ContractObjectiveLine>
            {
                new("TGL 1/2", ContractObjectiveState.Pending)
            };

            var content = ObjectiveCardBuilder.Build(missions, trackerLines);

            Assert.Equal(3, content.Missions.Count);
            Assert.Equal("SDCO → SDCO", content.Missions[0].Route);
            Assert.Equal("SDCO → SDCO", content.Missions[1].Route);
            Assert.Equal("SDCO → SBKP", content.Missions[2].Route);

            Assert.Equal(3, content.Objectives.Count);
            Assert.Equal("TGL 1/2", content.Objectives[0].Text);
            Assert.Equal(ContractObjectiveState.Pending, content.Objectives[0].State);

            Assert.Equal("Pouso final em SDCO", content.Objectives[1].Text);
            Assert.Equal(ContractObjectiveState.Pending, content.Objectives[1].State);

            Assert.Equal("Pouso final em SBKP", content.Objectives[2].Text);
            Assert.Equal(ContractObjectiveState.Pending, content.Objectives[2].State);
        }

        [Fact]
        public void ObjectiveCardBuilder_SemMissoes_Vazio()
        {
            var content1 = ObjectiveCardBuilder.Build(null, null);
            Assert.Empty(content1.Missions);
            Assert.Empty(content1.Objectives);

            var content2 = ObjectiveCardBuilder.Build(new List<Mission>(), new List<ContractObjectiveLine>());
            Assert.Empty(content2.Missions);
            Assert.Empty(content2.Objectives);
        }
    }
}
