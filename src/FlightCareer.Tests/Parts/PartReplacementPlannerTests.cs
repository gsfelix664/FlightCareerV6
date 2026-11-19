using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartReplacementPlannerTests
    {
        private readonly PartCatalog _catalog = PartCatalog.LoadDefault();

        [Fact]
        public void Planner_Freio98_SemPecaGasta_EmDia()
        {
            var brakePad = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "brakes.pads_discs",
                Health = 98.0,
                HoursSinceReplacement = 10.0,
                CyclesSinceReplacement = 15,
                IsBroken = false
            };

            var plan = PartReplacementPlanner.Plan(new[] { brakePad }, _catalog, "A", DateTime.UtcNow);

            Assert.Empty(plan.Items);
            Assert.Equal(0.0, plan.TotalCost);
            Assert.Equal(0.0, plan.DowntimeHours);
        }

        [Fact]
        public void Planner_PecaAbaixo80_TrocaPrecoCheio()
        {
            var brakePad = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "brakes.pads_discs",
                Health = 75.0, // < 80%
                HoursSinceReplacement = 50.0,
                IsBroken = false
            };

            var def = _catalog.Get("brakes.pads_discs")!;
            var plan = PartReplacementPlanner.Plan(new[] { brakePad }, _catalog, "A", DateTime.UtcNow);

            Assert.Single(plan.Items);
            var item = plan.Items[0];
            Assert.Contains("Saúde 75 %", item.Reason);

            double expectedPartPrice = PartPricing.CalculatePartPrice(def, "A");
            double expectedLaborCost = PartPricing.CalculateLaborCost(def, 100.0);
            double expectedTotal = Math.Round(expectedPartPrice + expectedLaborCost, 2);

            Assert.Equal(expectedPartPrice, item.PartPrice);
            Assert.Equal(expectedLaborCost, item.LaborCost);
            Assert.Equal(expectedTotal, plan.TotalCost);
            // Confirma que não cobrou proporcional (25%), e sim preço cheio
            Assert.True(plan.TotalCost > expectedPartPrice * 0.3);
        }

        [Fact]
        public void Planner_PecaVidaVencida_Troca()
        {
            var def = _catalog.Get("oil.lubricant")!;
            var oil = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "oil.lubricant",
                Health = 95.0, // saúde alta, mas horas no limite de vida
                HoursSinceReplacement = def.LifeHours,
                IsBroken = false
            };

            var plan = PartReplacementPlanner.Plan(new[] { oil }, _catalog, "A", DateTime.UtcNow);

            Assert.Single(plan.Items);
            Assert.Contains("Vida", plan.Items[0].Reason);
        }

        [Fact]
        public void Planner_PecaQuebrada_Troca()
        {
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "brakes.pads_discs",
                Health = 0.0,
                IsBroken = true
            };

            var plan = PartReplacementPlanner.Plan(new[] { part }, _catalog, "A", DateTime.UtcNow);

            Assert.Single(plan.Items);
            Assert.Equal("Quebrada", plan.Items[0].Reason);
        }

        [Fact]
        public void Planner_PorMotor_ContaInstancias()
        {
            var def = _catalog.Get("engine.cylinders")!;
            Assert.True(def.PerEngine);

            var eng1Cylinders = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                EngineIndex = 1,
                Health = 70.0
            };
            var eng2Cylinders = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                EngineIndex = 2,
                Health = 65.0
            };

            var plan = PartReplacementPlanner.Plan(new[] { eng1Cylinders, eng2Cylinders }, _catalog, "B", DateTime.UtcNow);

            Assert.Equal(2, plan.Items.Count);
            double singleCost = Math.Round(PartPricing.CalculatePartPrice(def, "B") + PartPricing.CalculateLaborCost(def), 2);
            Assert.Equal(singleCost * 2, plan.TotalCost);
        }

        [Fact]
        public void Planner_ParadaEhAMaior()
        {
            var defPad = _catalog.Get("brakes.pads_discs")!;
            var defTire = _catalog.Get("tires.main")!;

            var pad = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 60.0 };
            var tire = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 60.0 };

            var plan = PartReplacementPlanner.Plan(new[] { pad, tire }, _catalog, "A", DateTime.UtcNow);

            Assert.Equal(2, plan.Items.Count);
            double maxExpected = Math.Max(
                PartPricing.CalculateDowntimeHours(defPad),
                PartPricing.CalculateDowntimeHours(defTire));
            Assert.Equal(maxExpected, plan.DowntimeHours);
        }

        [Fact]
        public void Apply_ZeraHorasECiclosESaude100()
        {
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "brakes.pads_discs",
                Health = 55.0,
                HoursSinceReplacement = 200.0,
                CyclesSinceReplacement = 150,
                IsBroken = true,
                BrokenAtUtc = DateTime.UtcNow.AddDays(-2),
                Flag = PartFlag.Broken,
                FlaggedBy = PartFlagSource.Inspection,
                FlaggedAtUtc = DateTime.UtcNow.AddDays(-1),
                FlaggedAirframeHours = 450.0
            };

            var plan = PartReplacementPlanner.Plan(new[] { part }, _catalog, "A", DateTime.UtcNow);
            var now = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

            PartReplacementPlanner.Apply(plan, airframeHours: 500.0, careerNow: now);

            Assert.Equal(100.0, part.Health);
            Assert.False(part.IsBroken);
            Assert.Null(part.BrokenAtUtc);
            Assert.Equal(0.0, part.HoursSinceReplacement);
            Assert.Equal(0, part.CyclesSinceReplacement);
            Assert.Equal(500.0, part.LastReplacedAirframeHours);
            Assert.Equal(now, part.LastReplacedAtUtc);
            Assert.Equal(PartFlag.None, part.Flag);
            Assert.Equal(PartFlagSource.None, part.FlaggedBy);
            Assert.Null(part.FlaggedAtUtc);
            Assert.Null(part.FlaggedAirframeHours);
            Assert.Equal(now, part.LastShopServiceAtUtc);
            Assert.Equal(500.0, part.LastShopServiceAirframeHours);
        }

        [Fact]
        public void Hangar_BarraIgualAgregadoAposTroca()
        {
            var aircraft = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                Prefix = "PT-TST",
                CurrentAirframeHours = 200.0
            };

            var pad = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PilotAircraftId = aircraft.Id,
                PartId = "brakes.pads_discs",
                Health = 70.0 // gasta
            };
            var cyl = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PilotAircraftId = aircraft.Id,
                PartId = "brakes.master_cylinders",
                Health = 98.0 // boa
            };
            var parts = new List<AircraftPart> { pad, cyl };
            aircraft.Parts = parts;

            var compBrakes = new AircraftComponent
            {
                Id = Guid.NewGuid(),
                PilotAircraftId = aircraft.Id,
                Kind = ComponentKind.Brakes,
                Health = 70.0
            };
            var components = new List<AircraftComponent> { compBrakes };
            aircraft.Components = components;

            // Gera plano de troca para os freios
            var plan = PartReplacementPlanner.Plan(parts, _catalog, "A", DateTime.UtcNow);
            Assert.Single(plan.Items); // apenas pastilhas/discos entram
            Assert.Equal("brakes.pads_discs", plan.Items[0].Part.PartId);

            // Aplica troca
            var now = DateTime.UtcNow;
            PartReplacementPlanner.Apply(plan, aircraft.CurrentAirframeHours, now);
            Assert.Equal(100.0, pad.Health);
            Assert.Equal(98.0, cyl.Health);

            // Sincroniza barras
            PartComponentBridge.SyncComponentsFromParts(aircraft, components, parts, _catalog);

            double expectedAggregate = PartComponentBridge.AggregateHealth(parts, _catalog);
            Assert.Equal(expectedAggregate, compBrakes.Health);

            // Reconciliação no início do voo seguinte NÃO pode conceder modificações gratuitas
            int modified = PartComponentBridge.ReconcilePartsFromComponents(components, parts, _catalog);
            Assert.Equal(0, modified);
        }

        [Fact]
        public void Overhaul_TrocaPecasDoMotor()
        {
            var aircraft = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                Prefix = "PT-OVH",
                CurrentAirframeHours = 1000.0
            };

            var cyl = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PilotAircraftId = aircraft.Id,
                PartId = "engine.cylinders",
                Health = 50.0,
                HoursSinceReplacement = 1000.0
            };
            var oil = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PilotAircraftId = aircraft.Id,
                PartId = "oil.lubricant",
                Health = 40.0,
                HoursSinceReplacement = 25.0
            };
            var brake = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PilotAircraftId = aircraft.Id,
                PartId = "brakes.pads_discs",
                Health = 85.0,
                HoursSinceReplacement = 100.0
            };

            var parts = new List<AircraftPart> { cyl, oil, brake };
            aircraft.Parts = parts;

            var compEngine = new AircraftComponent { Id = Guid.NewGuid(), Kind = ComponentKind.Engine, Health = 50.0 };
            var compOil = new AircraftComponent { Id = Guid.NewGuid(), Kind = ComponentKind.Oil, Health = 40.0 };
            var compBrakes = new AircraftComponent { Id = Guid.NewGuid(), Kind = ComponentKind.Brakes, Health = 85.0 };
            var components = new List<AircraftComponent> { compEngine, compOil, compBrakes };
            aircraft.Components = components;

            var mapping = PartComponentMapping.CreateDefault();
            var overhaulParts = parts.Where(p =>
            {
                var def = _catalog.Get(p.PartId);
                if (def == null) return false;
                var kind = mapping.ResolveComponentKind(def.Group);
                return kind is ComponentKind.Engine or ComponentKind.HotSection or ComponentKind.Oil;
            }).ToList();

            var now = DateTime.UtcNow;
            PartReplacementPlanner.ApplyToParts(overhaulParts, aircraft.CurrentAirframeHours, now);
            PartComponentBridge.SyncComponentsFromParts(aircraft, components, parts, _catalog);

            // Cilindros e óleo foram restaurados
            Assert.Equal(100.0, cyl.Health);
            Assert.Equal(0.0, cyl.HoursSinceReplacement);
            Assert.Equal(100.0, oil.Health);
            Assert.Equal(0.0, oil.HoursSinceReplacement);
            Assert.Equal(100.0, compEngine.Health);
            Assert.Equal(100.0, compOil.Health);

            // Freios NÃO foram tocados pelo overhaul
            Assert.Equal(85.0, brake.Health);
            Assert.Equal(100.0, brake.HoursSinceReplacement);
        }
    }
}
