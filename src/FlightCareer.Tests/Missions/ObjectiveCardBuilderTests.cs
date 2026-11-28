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

        [Fact]
        public void ObjectiveCardBuilder_InstrucaoLocal_CicloTglEPousoFinal()
        {
            var missions = new List<Mission>
            {
                new()
                {
                    Title = "Instrução Local",
                    OriginICAO = "SBSJ",
                    DestinationICAO = "SBSJ",
                    RequiredTouchAndGoes = 1,
                    OverflyWaypoints = "P:AREA@-23.15,-45.80,2.0,3000,1000"
                }
            };

            // 1. Antes de decolar (no solo, sem pouso registrado) -> Pendente
            var preflight = ObjectiveCardBuilder.Build(
                missions,
                new List<ContractObjectiveLine>
                {
                    new("TGL 0/1", ContractObjectiveState.Pending),
                    new("Pontos 0/1", ContractObjectiveState.Pending)
                },
                landedIcao: null,
                trackers: new MissionTrackerResults(
                    VisitedWaypoints: new HashSet<string>(),
                    TouchAndGoCount: 0));
            Assert.Equal(ContractObjectiveState.Pending, preflight.Objectives[2].State);

            // 2. Primeiro toque em SBSJ antes do TGL (área visitada, TGL 0/1) -> Pendente
            var firstTouch = ObjectiveCardBuilder.Build(
                missions,
                new List<ContractObjectiveLine>
                {
                    new("TGL 0/1", ContractObjectiveState.Pending),
                    new("Pontos 1/1", ContractObjectiveState.Done)
                },
                landedIcao: "SBSJ",
                trackers: new MissionTrackerResults(
                    VisitedWaypoints: new HashSet<string> { "AREA" },
                    TouchAndGoCount: 0));
            Assert.Equal(ContractObjectiveState.Pending, firstTouch.Objectives[2].State);

            // 3. No ar após o 1º TGL (TGL 1/1 concluído, ainda em voo) -> Pendente
            var airborneAfterTgl = ObjectiveCardBuilder.Build(
                missions,
                new List<ContractObjectiveLine>
                {
                    new("TGL 1/1", ContractObjectiveState.Done),
                    new("Pontos 1/1", ContractObjectiveState.Done)
                },
                landedIcao: null,
                trackers: new MissionTrackerResults(
                    VisitedWaypoints: new HashSet<string> { "AREA" },
                    TouchAndGoCount: 1));
            Assert.Equal(ContractObjectiveState.Pending, airborneAfterTgl.Objectives[2].State);

            // 4. Pouso final em SBSJ com TGL e área cumpridos (antes de desligar o motor) -> Done
            var finalTouch = ObjectiveCardBuilder.Build(
                missions,
                new List<ContractObjectiveLine>
                {
                    new("TGL 1/1", ContractObjectiveState.Done),
                    new("Pontos 1/1", ContractObjectiveState.Done)
                },
                landedIcao: "SBSJ",
                trackers: new MissionTrackerResults(
                    VisitedWaypoints: new HashSet<string> { "AREA" },
                    TouchAndGoCount: 1));
            Assert.Equal("Pouso final em SBSJ", finalTouch.Objectives[2].Text);
            Assert.Equal(ContractObjectiveState.Done, finalTouch.Objectives[2].State);
        }

        [Fact]
        public void ObjectiveCardBuilder_PousoComTglMasSemAreaVisitada_PermanecePending()
        {
            var missions = new List<Mission>
            {
                new()
                {
                    Title = "Instrução Local",
                    OriginICAO = "SBSJ",
                    DestinationICAO = "SBSJ",
                    RequiredTouchAndGoes = 1,
                    OverflyWaypoints = "P:AREA@-23.15,-45.80,2.0,3000,1000"
                }
            };

            var content = ObjectiveCardBuilder.Build(
                missions,
                null,
                landedIcao: "SBSJ",
                trackers: new MissionTrackerResults(
                    VisitedWaypoints: new HashSet<string>(),
                    TouchAndGoCount: 1));

            Assert.Single(content.Objectives);
            Assert.Equal(ContractObjectiveState.Pending, content.Objectives[0].State);
        }

        [Fact]
        public void ObjectiveCardBuilder_PousouEmAeroportoDiferente_PermanecePending()
        {
            var missions = new List<Mission>
            {
                new() { Title = "Translado", OriginICAO = "SBSJ", DestinationICAO = "SBSJ" }
            };

            var content = ObjectiveCardBuilder.Build(missions, null, landedIcao: "SBKP", trackers: new MissionTrackerResults());

            Assert.Single(content.Objectives);
            Assert.Equal(ContractObjectiveState.Pending, content.Objectives[0].State);
        }

        [Fact]
        public void ObjectiveCardBuilder_MultiplosDestinosDistintos_MarcaApenasDestinoPousado()
        {
            var missions = new List<Mission>
            {
                new() { Title = "Entrega 1", OriginICAO = "SBSJ", DestinationICAO = "SDCO" },
                new() { Title = "Entrega 2", OriginICAO = "SDCO", DestinationICAO = "SBKP" }
            };

            var content = ObjectiveCardBuilder.Build(missions, null, landedIcao: "SDCO", trackers: new MissionTrackerResults());

            Assert.Equal(2, content.Objectives.Count);
            Assert.Equal("Pouso final em SDCO", content.Objectives[0].Text);
            Assert.Equal(ContractObjectiveState.Done, content.Objectives[0].State);
            Assert.Equal("Pouso final em SBKP", content.Objectives[1].Text);
            Assert.Equal(ContractObjectiveState.Pending, content.Objectives[1].State);
        }

        [Fact]
        public void ObjectiveCardBuilder_DuasMissoesMesmoDestino_ExigeTodasCompletas()
        {
            var missions = new List<Mission>
            {
                new() { Title = "Cheque 1", OriginICAO = "SBSJ", DestinationICAO = "SBSJ", RequiredTouchAndGoes = 1 },
                new() { Title = "Cheque 2", OriginICAO = "SBSJ", DestinationICAO = "SBSJ", RequiredTouchAndGoes = 2 }
            };

            var partial = ObjectiveCardBuilder.Build(
                missions,
                null,
                landedIcao: "SBSJ",
                trackers: new MissionTrackerResults(TouchAndGoCount: 1));
            Assert.Single(partial.Objectives);
            Assert.Equal(ContractObjectiveState.Pending, partial.Objectives[0].State);

            var complete = ObjectiveCardBuilder.Build(
                missions,
                null,
                landedIcao: "SBSJ",
                trackers: new MissionTrackerResults(TouchAndGoCount: 2));
            Assert.Single(complete.Objectives);
            Assert.Equal(ContractObjectiveState.Done, complete.Objectives[0].State);
        }

        [Fact]
        public void ObjectiveCardBuilder_MissaoPontoAPontoSimples_MarcaDoneAoPousarNoDestino()
        {
            var missions = new List<Mission>
            {
                new() { Title = "Carga Leve", OriginICAO = "SBSJ", DestinationICAO = "SBSP" }
            };

            var content = ObjectiveCardBuilder.Build(
                missions,
                null,
                landedIcao: "SBSP",
                trackers: new MissionTrackerResults());

            Assert.Single(content.Objectives);
            Assert.Equal("Pouso final em SBSP", content.Objectives[0].Text);
            Assert.Equal(ContractObjectiveState.Done, content.Objectives[0].State);
        }

        [Fact]
        public void ResolveLandedIcao_RespeitaSoloToqueInvalidezERaio()
        {
            var candidates = new List<AirportPosition>
            {
                new("SBSJ", -23.2289, -45.8711),
                new("SBTA", -23.0402, -45.5162)
            };

            // No ar após TGL mesmo com hasTouchedDown=true (janela de 8 s) -> null
            Assert.Null(ObjectiveCardBuilder.ResolveLandedIcao(
                isOnGround: false, hasTouchedDown: true, isFlightInvalidated: false,
                -23.2289, -45.8711, candidates));

            // No solo antes da decolagem (hasTouchedDown=false) -> null
            Assert.Null(ObjectiveCardBuilder.ResolveLandedIcao(
                isOnGround: true, hasTouchedDown: false, isFlightInvalidated: false,
                -23.2289, -45.8711, candidates));

            // Voo invalidado -> null
            Assert.Null(ObjectiveCardBuilder.ResolveLandedIcao(
                isOnGround: true, hasTouchedDown: true, isFlightInvalidated: true,
                -23.2289, -45.8711, candidates));

            // Fora do raio de 3 NM (~6 NM ao sul de SBSJ) -> null
            Assert.Null(ObjectiveCardBuilder.ResolveLandedIcao(
                isOnGround: true, hasTouchedDown: true, isFlightInvalidated: false,
                -23.33, -45.8711, candidates));

            // Dentro do raio em SBSJ -> SBSJ
            Assert.Equal("SBSJ", ObjectiveCardBuilder.ResolveLandedIcao(
                isOnGround: true, hasTouchedDown: true, isFlightInvalidated: false,
                -23.2236, -45.8645, candidates));

            // Mais próximo de outro aeródromo (SBTA) -> SBTA
            Assert.Equal("SBTA", ObjectiveCardBuilder.ResolveLandedIcao(
                isOnGround: true, hasTouchedDown: true, isFlightInvalidated: false,
                -23.0410, -45.5170, candidates));
        }
    }
}
