using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartProvisionerTests
    {
        private static readonly PartCatalog Catalog = PartCatalog.LoadDefault();

        [Fact]
        public async Task Provisioner_CreatesPartsAt100_Idempotent()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            var typeId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var c152Type = new AircraftType
                {
                    Id = typeId,
                    Model = "Cessna C152",
                    Type = "Geral",
                    FuelType = "Avgas"
                };
                db.AircraftTypes.Add(c152Type);

                var pilot = new Pilot { Id = pilotId, Name = "Piloto Teste" };
                db.Pilots.Add(pilot);

                var pilotAircraft = new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = typeId,
                    Prefix = "PT-TES",
                    PurchaseDate = DateTime.UtcNow,
                    CurrentAirframeHours = 250.0
                };
                db.PilotAircrafts.Add(pilotAircraft);

                var profile = new AircraftLimitProfileEntity
                {
                    Id = Guid.NewGuid(),
                    AircraftTypeModel = "Cessna C152",
                    Class = "Piston",
                    DataVersion = 1,
                    SimMatchJson = "{}",
                    FeaturesJson = JsonSerializer.Serialize(new[]
                    {
                        AircraftFeatures.Piston,
                        AircraftFeatures.SingleEngine,
                        AircraftFeatures.FixedGear,
                        AircraftFeatures.FixedPitchProp,
                        AircraftFeatures.Carbureted
                    })
                };
                db.AircraftLimitProfiles.Add(profile);

                await db.SaveChangesAsync();
            }

            var provisioner = new AircraftPartProvisioner(Catalog);

            // 1ª execução: cria todas as peças a 100%
            await using (var db = new AppDbContext(options))
            {
                int created = await provisioner.EnsurePartsAsync(db);
                Assert.True(created > 0);

                var aircraft = await db.PilotAircrafts
                    .Include(a => a.Parts)
                    .SingleAsync(a => a.Id == aircraftId);

                Assert.Equal(created, aircraft.Parts.Count);

                foreach (var part in aircraft.Parts)
                {
                    Assert.Equal(100.0, part.Health);
                    Assert.Equal(0.0, part.HoursSinceReplacement);
                    Assert.Equal(0, part.CyclesSinceReplacement);
                    Assert.Equal(250.0, part.LastReplacedAirframeHours);
                    Assert.Equal(PartFlag.None, part.Flag);
                    Assert.Equal(PartFlagSource.None, part.FlaggedBy);
                    Assert.False(part.IsBroken);
                    Assert.Null(part.BrokenAtUtc);
                }
            }

            // 2ª execução: idempotente (0 criadas)
            await using (var db = new AppDbContext(options))
            {
                int created = await provisioner.EnsurePartsAsync(db);
                Assert.Equal(0, created);
            }
        }

        [Fact]
        public async Task Provisioner_AircraftWithoutProfile_Skips()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            var typeId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var unknownType = new AircraftType
                {
                    Id = typeId,
                    Model = "Aviao Desconhecido Sem Perfil",
                    Type = "Geral",
                    FuelType = "Avgas"
                };
                db.AircraftTypes.Add(unknownType);

                var pilot = new Pilot { Id = pilotId, Name = "Piloto" };
                db.Pilots.Add(pilot);

                var pilotAircraft = new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = typeId,
                    Prefix = "PT-UNK"
                };
                db.PilotAircrafts.Add(pilotAircraft);

                await db.SaveChangesAsync();
            }

            var provisioner = new AircraftPartProvisioner(Catalog);

            await using (var db = new AppDbContext(options))
            {
                int created = await provisioner.EnsurePartsAsync(db);
                Assert.Equal(0, created);

                var parts = await db.AircraftParts.Where(p => p.PilotAircraftId == aircraftId).ToListAsync();
                Assert.Empty(parts);
            }
        }

        [Fact]
        public async Task Provisioner_NeverRemovesExistingParts()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            var typeId = Guid.NewGuid();
            var customPartId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var c152Type = new AircraftType
                {
                    Id = typeId,
                    Model = "Cessna C152",
                    Type = "Geral",
                    FuelType = "Avgas"
                };
                db.AircraftTypes.Add(c152Type);

                var pilot = new Pilot { Id = pilotId, Name = "Piloto" };
                db.Pilots.Add(pilot);

                var pilotAircraft = new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = typeId,
                    Prefix = "PT-AAA"
                };
                db.PilotAircrafts.Add(pilotAircraft);

                var profile = new AircraftLimitProfileEntity
                {
                    Id = Guid.NewGuid(),
                    AircraftTypeModel = "Cessna C152",
                    Class = "Piston",
                    DataVersion = 1,
                    SimMatchJson = "{}",
                    FeaturesJson = JsonSerializer.Serialize(new[]
                    {
                        AircraftFeatures.Piston,
                        AircraftFeatures.SingleEngine,
                        AircraftFeatures.FixedGear,
                        AircraftFeatures.FixedPitchProp,
                        AircraftFeatures.Carbureted
                    })
                };
                db.AircraftLimitProfiles.Add(profile);

                // Peça pré-existente com desgaste e flag
                var existingCustomPart = new AircraftPart
                {
                    Id = customPartId,
                    PilotAircraftId = aircraftId,
                    PartId = "engine.cylinders",
                    Health = 42.0,
                    HoursSinceReplacement = 850.0,
                    Flag = PartFlag.Suspect,
                    FlaggedBy = PartFlagSource.Walkaround
                };
                db.AircraftParts.Add(existingCustomPart);

                await db.SaveChangesAsync();
            }

            var provisioner = new AircraftPartProvisioner(Catalog);

            await using (var db = new AppDbContext(options))
            {
                await provisioner.EnsurePartsAsync(db);

                var cylinderPart = await db.AircraftParts.SingleAsync(p => p.Id == customPartId);
                Assert.Equal(42.0, cylinderPart.Health);
                Assert.Equal(850.0, cylinderPart.HoursSinceReplacement);
                Assert.Equal(PartFlag.Suspect, cylinderPart.Flag);
            }
        }

        [Fact]
        public async Task Schema_OldDatabaseGetsAircraftPartsAndFeaturesColumn()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            var typeId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var c152Type = new AircraftType
                {
                    Id = typeId,
                    Model = "Cessna C152",
                    Type = "Geral",
                    FuelType = "Avgas"
                };
                db.AircraftTypes.Add(c152Type);

                var pilot = new Pilot { Id = pilotId, Name = "Piloto" };
                db.Pilots.Add(pilot);

                var pilotAircraft = new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = typeId,
                    Prefix = "PT-OLD"
                };
                db.PilotAircrafts.Add(pilotAircraft);
                await db.SaveChangesAsync();

                // Simula um banco antigo sem a tabela AircraftParts e sem a coluna FeaturesJson
                await db.Database.ExecuteSqlRawAsync("DROP TABLE AircraftParts;");
            }

            await using (var db = new AppDbContext(options))
            {
                // EnsureSchemaUpdatedAsync deve recriar a tabela e a coluna idempotentemente
                await db.EnsureSchemaUpdatedAsync();

                // Verifica se a tabela AircraftParts existe e aceita inserções
                var part = new AircraftPart
                {
                    Id = Guid.NewGuid(),
                    PilotAircraftId = aircraftId,
                    PartId = "oil.lubricant",
                    Health = 100.0,
                    HoursSinceReplacement = 0.0,
                    CyclesSinceReplacement = 0,
                    LastReplacedAtUtc = DateTime.UtcNow,
                    LastReplacedAirframeHours = 0.0,
                    Flag = PartFlag.None,
                    FlaggedBy = PartFlagSource.None,
                    IsBroken = false
                };

                // Testa inserção na tabela recém-criada via EF Core
                db.AircraftParts.Add(part);
                await db.SaveChangesAsync();

                var saved = await db.AircraftParts.SingleAsync(p => p.Id == part.Id);
                Assert.Equal("oil.lubricant", saved.PartId);
                Assert.Equal(100.0, saved.Health);
            }
        }
    }
}
