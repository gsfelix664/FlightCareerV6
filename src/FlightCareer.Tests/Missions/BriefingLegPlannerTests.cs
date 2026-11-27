using System;
using System.Collections.Generic;
using System.Text.Json;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions;

public class BriefingLegPlannerTests
{
    [Fact]
    public void Plan_TrianguloFullStop_ViraPernasReaisSemTgl()
    {
        // SDCO→SDCO, Stops [SNZG], full_stop, RTG 0 → [SDCO→SNZG ContractStop full_stop, SNZG→SDCO Navigation final]; nenhum TouchAndGo.
        var contractData = JsonSerializer.Serialize(new MultiStopContractData
        {
            Stops = new List<StopPoint>
            {
                new() { Icao = "SNZG", Lat = -21.5, Lon = -46.5, ElevationFt = 2600 }
            },
            StopMode = "full_stop"
        });

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OriginICAO = "SDCO",
            DestinationICAO = "SDCO",
            RequiredTouchAndGoes = 0,
            ContractData = contractData
        };

        var plans = BriefingLegPlanner.Plan(new[] { mission }, "SDCO");

        Assert.Equal(2, plans.Count);

        // Leg 1: SDCO -> SNZG ContractStop
        Assert.Equal("SDCO", plans[0].From);
        Assert.Equal("SNZG", plans[0].To);
        Assert.Equal(BriefingLegKind.ContractStop, plans[0].Kind);
        Assert.Equal("full_stop", plans[0].StopMode);
        Assert.False(plans[0].IsFinalLegOfMission);
        Assert.Equal(-21.5, plans[0].ToLat);
        Assert.Equal(-46.5, plans[0].ToLon);

        // Leg 2: SNZG -> SDCO Navigation
        Assert.Equal("SNZG", plans[1].From);
        Assert.Equal("SDCO", plans[1].To);
        Assert.Equal(BriefingLegKind.Navigation, plans[1].Kind);
        Assert.Equal("full_stop", plans[1].StopMode);
        Assert.True(plans[1].IsFinalLegOfMission);
        Assert.Null(plans[1].ToLat);
        Assert.Null(plans[1].ToLon);

        Assert.DoesNotContain(plans, p => p.Kind == BriefingLegKind.TouchAndGo);
    }

    [Fact]
    public void Plan_TrianguloToqueArremetida_EscalasComStopModeTgl()
    {
        // Stops [A, B], touch_and_go → 3 pernas; as 2 primeiras ContractStop com "touch_and_go"; a última B→SDCO Navigation.
        var contractData = JsonSerializer.Serialize(new MultiStopContractData
        {
            Stops = new List<StopPoint>
            {
                new() { Icao = "SBAA", Lat = -22.0, Lon = -47.0 },
                new() { Icao = "SBBB", Lat = -22.5, Lon = -47.5 }
            },
            StopMode = "touch_and_go"
        });

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OriginICAO = "SDCO",
            DestinationICAO = "SDCO",
            RequiredTouchAndGoes = 0,
            ContractData = contractData
        };

        var plans = BriefingLegPlanner.Plan(new[] { mission }, "SDCO");

        Assert.Equal(3, plans.Count);

        Assert.Equal("SDCO", plans[0].From);
        Assert.Equal("SBAA", plans[0].To);
        Assert.Equal(BriefingLegKind.ContractStop, plans[0].Kind);
        Assert.Equal("touch_and_go", plans[0].StopMode);
        Assert.False(plans[0].IsFinalLegOfMission);

        Assert.Equal("SBAA", plans[1].From);
        Assert.Equal("SBBB", plans[1].To);
        Assert.Equal(BriefingLegKind.ContractStop, plans[1].Kind);
        Assert.Equal("touch_and_go", plans[1].StopMode);
        Assert.False(plans[1].IsFinalLegOfMission);

        Assert.Equal("SBBB", plans[2].From);
        Assert.Equal("SDCO", plans[2].To);
        Assert.Equal(BriefingLegKind.Navigation, plans[2].Kind);
        Assert.Equal("full_stop", plans[2].StopMode);
        Assert.True(plans[2].IsFinalLegOfMission);
    }

    [Fact]
    public void Plan_TglSoComRequiredTouchAndGoes()
    {
        // SDCO→SDCO, RTG 3, sem ContractData → 1 perna TouchAndGo.
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OriginICAO = "SDCO",
            DestinationICAO = "SDCO",
            RequiredTouchAndGoes = 3,
            ContractData = null
        };

        var plans = BriefingLegPlanner.Plan(new[] { mission }, "SDCO");

        var plan = Assert.Single(plans);
        Assert.Equal("SDCO", plan.From);
        Assert.Equal("SDCO", plan.To);
        Assert.Equal(BriefingLegKind.TouchAndGo, plan.Kind);
        Assert.True(plan.IsFinalLegOfMission);
        Assert.Null(plan.ToLat);
        Assert.Null(plan.ToLon);
    }

    [Fact]
    public void Plan_LocalSemTgl_ViraLocal()
    {
        // SDCO→SDCO, RTG 0, categoria "SAE_Foto", sem escalas → 1 perna Local.
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OriginICAO = "SDCO",
            DestinationICAO = "SDCO",
            MissionCategoryType = "SAE_Foto",
            RequiredTouchAndGoes = 0,
            ContractData = null
        };

        var plans = BriefingLegPlanner.Plan(new[] { mission }, "SDCO");

        var plan = Assert.Single(plans);
        Assert.Equal("SDCO", plan.From);
        Assert.Equal("SDCO", plan.To);
        Assert.Equal(BriefingLegKind.Local, plan.Kind);
        Assert.True(plan.IsFinalLegOfMission);
        Assert.Null(plan.ToLat);
        Assert.Null(plan.ToLon);
    }

    [Fact]
    public void Plan_UltimaEscalaIgualAoDestino_NaoDuplica()
    {
        // SBXX→SBYY, Stops [SBYY] → 1 perna SBXX→SBYY Navigation.
        var contractData = JsonSerializer.Serialize(new MultiStopContractData
        {
            Stops = new List<StopPoint>
            {
                new() { Icao = "SBYY", Lat = -23.0, Lon = -46.0 }
            },
            StopMode = "full_stop"
        });

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OriginICAO = "SBXX",
            DestinationICAO = "SBYY",
            ContractData = contractData
        };

        var plans = BriefingLegPlanner.Plan(new[] { mission }, "SBXX");

        var plan = Assert.Single(plans);
        Assert.Equal("SBXX", plan.From);
        Assert.Equal("SBYY", plan.To);
        Assert.Equal(BriefingLegKind.Navigation, plan.Kind);
        Assert.True(plan.IsFinalLegOfMission);
    }

    [Fact]
    public void Plan_MultiStopDestinoDiferente()
    {
        // SBXX→SBZZ, Stops [A, B] → SBXX→A, A→B, B→SBZZ (Navigation final).
        var contractData = JsonSerializer.Serialize(new MultiStopContractData
        {
            Stops = new List<StopPoint>
            {
                new() { Icao = "SBAA", Lat = -20.0, Lon = -45.0 },
                new() { Icao = "SBBB", Lat = -21.0, Lon = -46.0 }
            },
            StopMode = "full_stop"
        });

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OriginICAO = "SBXX",
            DestinationICAO = "SBZZ",
            ContractData = contractData
        };

        var plans = BriefingLegPlanner.Plan(new[] { mission }, "SBXX");

        Assert.Equal(3, plans.Count);
        Assert.Equal("SBXX", plans[0].From);
        Assert.Equal("SBAA", plans[0].To);
        Assert.Equal(BriefingLegKind.ContractStop, plans[0].Kind);
        Assert.False(plans[0].IsFinalLegOfMission);

        Assert.Equal("SBAA", plans[1].From);
        Assert.Equal("SBBB", plans[1].To);
        Assert.Equal(BriefingLegKind.ContractStop, plans[1].Kind);
        Assert.False(plans[1].IsFinalLegOfMission);

        Assert.Equal("SBBB", plans[2].From);
        Assert.Equal("SBZZ", plans[2].To);
        Assert.Equal(BriefingLegKind.Navigation, plans[2].Kind);
        Assert.True(plans[2].IsFinalLegOfMission);
    }

    [Fact]
    public void ReadStops_JsonInvalidoOuOutroContrato_Vazio()
    {
        // "{lixo", JSON de SurveyContractData e null → Count 0, sem exceção.
        var m1 = new Mission { Id = Guid.NewGuid(), ContractData = "{lixo" };
        var m2 = new Mission { Id = Guid.NewGuid(), ContractData = JsonSerializer.Serialize(new SurveyContractData()) };
        var m3 = new Mission { Id = Guid.NewGuid(), ContractData = null };

        Assert.Empty(BriefingLegPlanner.ReadStops(m1));
        Assert.Empty(BriefingLegPlanner.ReadStops(m2));
        Assert.Empty(BriefingLegPlanner.ReadStops(m3));
    }

    [Fact]
    public void Plan_DuasMissoesEncadeadas_MantemOrdem()
    {
        // A→B e B→C (aceitas em ordem inversa) → A→B, B→C.
        var m2 = new Mission { Id = Guid.NewGuid(), OriginICAO = "SBBB", DestinationICAO = "SBCC" };
        var m1 = new Mission { Id = Guid.NewGuid(), OriginICAO = "SBAA", DestinationICAO = "SBBB" };

        var plans = BriefingLegPlanner.Plan(new[] { m2, m1 }, "SBAA");

        Assert.Equal(2, plans.Count);
        Assert.Equal("SBAA", plans[0].From);
        Assert.Equal("SBBB", plans[0].To);
        Assert.Equal("SBBB", plans[1].From);
        Assert.Equal("SBCC", plans[1].To);
    }

    [Fact]
    public void Plan_TglMaisNavegacao_TglPrimeiro()
    {
        // TGL em A (RTG 2) + A→B → [TouchAndGo A→A, Navigation A→B].
        var mTgl = new Mission { Id = Guid.NewGuid(), OriginICAO = "SBAA", DestinationICAO = "SBAA", RequiredTouchAndGoes = 2 };
        var mNav = new Mission { Id = Guid.NewGuid(), OriginICAO = "SBAA", DestinationICAO = "SBBB" };

        var plans = BriefingLegPlanner.Plan(new[] { mNav, mTgl }, "SBAA");

        Assert.Equal(2, plans.Count);
        Assert.Equal("SBAA", plans[0].From);
        Assert.Equal("SBAA", plans[0].To);
        Assert.Equal(BriefingLegKind.TouchAndGo, plans[0].Kind);

        Assert.Equal("SBAA", plans[1].From);
        Assert.Equal("SBBB", plans[1].To);
        Assert.Equal(BriefingLegKind.Navigation, plans[1].Kind);
    }

    [Fact]
    public void Plan_ToLatLonVemDoStopPoint()
    {
        // escala com Lat/Lon → ToLat/ToLon iguais na perna ContractStop.
        var contractData = JsonSerializer.Serialize(new MultiStopContractData
        {
            Stops = new List<StopPoint>
            {
                new() { Icao = "SBAA", Lat = -22.1234, Lon = -47.5678, ElevationFt = 2000 }
            },
            StopMode = "full_stop"
        });

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            OriginICAO = "SDCO",
            DestinationICAO = "SDCO",
            ContractData = contractData
        };

        var plans = BriefingLegPlanner.Plan(new[] { mission }, "SDCO");

        Assert.Equal(2, plans.Count);
        Assert.Equal(BriefingLegKind.ContractStop, plans[0].Kind);
        Assert.Equal(-22.1234, plans[0].ToLat);
        Assert.Equal(-47.5678, plans[0].ToLon);
    }
}
