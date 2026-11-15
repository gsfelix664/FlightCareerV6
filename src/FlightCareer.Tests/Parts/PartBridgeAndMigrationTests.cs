using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using FlightCareer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartBridgeAndMigrationTests
    {
        private static readonly PartCatalog Catalog = PartCatalog.LoadDefault();

        [Fact]
        public void Bridge_AggregatesPartsIntoComponents_UsingWeightedMinimum()
        {
            var aircraft = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                EngineCondition = 100.0,
                TiresCondition = 100.0,
                AirframeCondition = 100.0,
                AvionicsCondition = 100.0
            };

            var compEngine = new AircraftComponent { Id = Guid.NewGuid(), Kind = ComponentKind.Engine, Health = 100.0 };
            var compTires = new AircraftComponent { Id = Guid.NewGuid(), Kind = ComponentKind.Tires, Health = 100.0 };
            var components = new List<AircraftComponent> { compEngine, compTires };

            // Peças do motor: uma a 60% e outra a 90%
            // Mínimo = 60, Média = 75. Mínimo ponderado (0.7 * 60 + 0.3 * 75) = 42 + 22.5 = 64.5%
            var part1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 60.0 };
            var part2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 90.0 };

            // Peças dos pneus: 80% e 80% -> 80%
            var partTire1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 80.0 };
            var partTire2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.nose", Health = 80.0 };

            var parts = new List<AircraftPart> { part1, part2, partTire1, partTire2 };

            int updated = PartComponentBridge.SyncComponentsFromParts(aircraft, components, parts, Catalog);

            Assert.Equal(2, updated);
            Assert.Equal(64.5, compEngine.Health, 1);
            Assert.Equal(80.0, compTires.Health, 1);

            // Verifica que atualizou também as colunas antigas da aeronave
            Assert.Equal(64.5, aircraft.EngineCondition, 1);
            Assert.Equal(80.0, aircraft.TiresCondition, 1);
        }

        [Fact]
        public void Bridge_HasParts_ReturnsTrueOnlyWhenPartsArePresent()
        {
            var planeWithoutParts = new PilotAircraft { Id = Guid.NewGuid(), Parts = new List<AircraftPart>() };
            var planeWithParts = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                Parts = new List<AircraftPart> { new() { Id = Guid.NewGuid(), PartId = "engine.cylinders" } }
            };

            Assert.False(PartComponentBridge.HasParts(planeWithoutParts));
            Assert.True(PartComponentBridge.HasParts(planeWithParts));
        }

        [Fact]
        public void Pricing_ScalesWithAircraftValue_WhenMarked_RespectingMinimumFloor()
        {
            // Peça sem escala (preço fixo)
            var fixedDef = new PartDefinition(
                "ignition.spark_plugs", "Velas", PartGroup.Ignicao, new PartAppliesTo(),
                500, null, true, true, 200m, 1.0, Array.Empty<string>(), "Manual",
                priceScalesWithAircraft: false);

            double priceFixedSmall = PartPricing.CalculatePartPrice(fixedDef, 50_000.0);
            double priceFixedBig = PartPricing.CalculatePartPrice(fixedDef, 25_000_000.0);
            Assert.Equal(200.0, priceFixedSmall);
            Assert.Equal(200.0, priceFixedBig);

            // Peça que escala: 0.4% a 0.8% do valor da aeronave, com piso em $1500
            var scalingDef = new PartDefinition(
                "avionics.pfd_display", "PFD", PartGroup.Avionicos, new PartAppliesTo(),
                6000, null, false, true, 1500m, 3.0, Array.Empty<string>(), "Manual",
                priceScalesWithAircraft: true,
                repairCostPctOfAircraft: new[] { 0.40, 0.80 });

            // Em aeronave barata ($100.000): 0.4% = $400, 0.8% = $800. Piso de $1500 vence!
            double priceSmall = PartPricing.CalculatePartPrice(scalingDef, 100_000.0);
            Assert.Equal(1500.0, priceSmall);

            // Em jato caro ($10.000.000): 0.4% = $40.000, 0.8% = $80.000. Escala vence!
            double priceBig = PartPricing.CalculatePartPrice(scalingDef, 10_000_000.0);
            Assert.InRange(priceBig, 40_000.0, 80_000.0);

            // Mão de obra e downtime
            double laborCost = PartPricing.CalculateLaborCost(scalingDef, 100.0);
            Assert.Equal(300.0, laborCost); // 3h * $100
        }

        [Fact]
        public void Migration_ProvisionPurchasedAircraft_NewIs100_UsedIsCoherentWithConditions()
        {
            var migrator = new PartMigrationService(Catalog);

            var newPlane = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                CurrentAirframeHours = 0.0,
                EngineCondition = 100.0,
                AirframeCondition = 100.0
            };
            migrator.ProvisionPurchasedAircraft(newPlane, new[] { "piston", "tricycleGear" }, 1);

            Assert.NotEmpty(newPlane.Parts);
            Assert.All(newPlane.Parts, p => Assert.Equal(100.0, p.Health));
            Assert.All(newPlane.Parts, p => Assert.Null(p.LastShopServiceAtUtc));
            Assert.NotNull(newPlane.PartsMigratedAtUtc);

            var usedPlane = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                CurrentAirframeHours = 850.0,
                EngineCondition = 62.0,
                AirframeCondition = 70.0,
                TiresCondition = 65.0,
                AvionicsCondition = 68.0
            };
            migrator.ProvisionPurchasedAircraft(usedPlane, new[] { "piston", "tricycleGear" }, 1, 62.0, 65.0, 70.0, 68.0);

            Assert.NotEmpty(usedPlane.Parts);
            var cylinders = usedPlane.Parts.FirstOrDefault(p => p.PartId == "engine.cylinders");
            Assert.NotNull(cylinders);
            // Deve estar próximo da condição anunciada do motor (62% +-3%)
            Assert.InRange(cylinders.Health, 58.0, 66.0);
            Assert.True(cylinders.HoursSinceReplacement > 0);
            Assert.Null(cylinders.LastShopServiceAtUtc); // Não abre janela pós-manutenção
        }

        [Fact]
        public async System.Threading.Tasks.Task Migration_Database_RemovesObsoleteUntouchedParts_AndHeritsHealth_Idempotent()
        {
            await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<FlightCareer.Infrastructure.Persistence.AppDbContext>()
                .UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();

            await using (var db = new FlightCareer.Infrastructure.Persistence.AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                await db.EnsureSchemaUpdatedAsync();

                var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
                db.AircraftTypes.Add(type);

                var pilot = new Pilot { Id = pilotId, Name = "Piloto Teste" };
                db.Pilots.Add(pilot);

                var profile = new AircraftLimitProfileEntity
                {
                    Id = Guid.NewGuid(),
                    AircraftTypeModel = "C152",
                    Class = "A",
                    DataVersion = 1,
                    EngineCount = 1,
                    FeaturesJson = "[\"piston\", \"fixedPitchProp\", \"fixedGear\", \"tricycleGear\", \"carbureted\", \"naturallyAspirated\", \"unpressurized\"]",
                    SimMatchJson = "{\"TitlePatterns\":[\"C152\"],\"AtcModelPatterns\":[\"C152\"]}"
                };
                db.AircraftLimitProfiles.Add(profile);

                var aircraft = new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = type.Id,
                    Prefix = "PT-MIG",
                    CurrentAirframeHours = 500.0,
                    EngineCondition = 60.0,
                    AirframeCondition = 75.0,
                    TiresCondition = 70.0,
                    AvionicsCondition = 80.0
                };
                db.PilotAircrafts.Add(aircraft);

                // Adiciona uma peça obsoleta intocada (engine.wastegate_controller)
                db.AircraftParts.Add(new AircraftPart
                {
                    Id = Guid.NewGuid(),
                    PilotAircraftId = aircraftId,
                    PartId = "engine.wastegate_controller",
                    Health = 100.0,
                    HoursSinceReplacement = 0.0
                });

                // Adiciona uma peça obsoleta com uso prévio (deve ser mantida)
                db.AircraftParts.Add(new AircraftPart
                {
                    Id = Guid.NewGuid(),
                    PilotAircraftId = aircraftId,
                    PartId = "hydraulic.reservoir_lines",
                    Health = 80.0,
                    HoursSinceReplacement = 50.0
                });

                await db.SaveChangesAsync();
            }

            var migrator = new PartMigrationService(Catalog);

            // 1. Executa migração
            await using (var db = new FlightCareer.Infrastructure.Persistence.AppDbContext(options))
            {
                var plane = await db.PilotAircrafts
                    .Include(a => a.AircraftType)
                    .Include(a => a.Parts)
                    .Include(a => a.Components)
                    .FirstAsync(a => a.Id == aircraftId);

                var summary = await migrator.MigrateAircraftAsync(db, plane);

                Assert.Equal(1, summary.AircraftMigrated);
                Assert.Equal(1, summary.ObsoletePartsRemoved); // wastegate virgem removida
                Assert.NotNull(plane.PartsMigratedAtUtc);

                // Verifica que wastegate virgem sumiu
                Assert.DoesNotContain(plane.Parts, p => p.PartId == "engine.wastegate_controller");
                // Verifica que peça com uso foi mantida
                Assert.Contains(plane.Parts, p => p.PartId == "hydraulic.reservoir_lines");

                // Verifica que cilindros herdou do motor (60% +-3%)
                var cyl = plane.Parts.First(p => p.PartId == "engine.cylinders");
                Assert.InRange(cyl.Health, 56.0, 64.0);
            }

            // 2. Segunda execução: idempotente, nada deve mudar
            await using (var db = new FlightCareer.Infrastructure.Persistence.AppDbContext(options))
            {
                var plane = await db.PilotAircrafts
                    .Include(a => a.AircraftType)
                    .Include(a => a.Parts)
                    .Include(a => a.Components)
                    .FirstAsync(a => a.Id == aircraftId);

                var secondSummary = await migrator.MigrateAircraftAsync(db, plane);
                Assert.Equal(0, secondSummary.AircraftMigrated);
            }
        }
    }
}
