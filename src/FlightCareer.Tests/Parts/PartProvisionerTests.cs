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
        public async Task Provisioner_MultiEngine_CreatesPerEngineCopies_DA62_Baron_C152_747()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var c152Id = Guid.NewGuid();
            var baronId = Guid.NewGuid();
            var da62Id = Guid.NewGuid();
            var b748Id = Guid.NewGuid();

            var c152TypeId = Guid.NewGuid();
            var baronTypeId = Guid.NewGuid();
            var da62TypeId = Guid.NewGuid();
            var b748TypeId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var pilot = new Pilot { Id = pilotId, Name = "Piloto Frotas" };
                db.Pilots.Add(pilot);

                // Types
                db.AircraftTypes.AddRange(
                    new AircraftType { Id = c152TypeId, Model = "Cessna C152", Type = "Geral", FuelType = "Avgas" },
                    new AircraftType { Id = baronTypeId, Model = "Beechcraft Baron G58", Type = "Geral", FuelType = "Avgas" },
                    new AircraftType { Id = da62TypeId, Model = "Diamond DA62", Type = "Geral", FuelType = "JetA" },
                    new AircraftType { Id = b748TypeId, Model = "747-8i", Type = "Comercial", FuelType = "JetA" }
                );

                // Aircrafts
                db.PilotAircrafts.AddRange(
                    new PilotAircraft { Id = c152Id, PilotId = pilotId, AircraftTypeId = c152TypeId, Prefix = "PT-152" },
                    new PilotAircraft { Id = baronId, PilotId = pilotId, AircraftTypeId = baronTypeId, Prefix = "PT-G58" },
                    new PilotAircraft { Id = da62Id, PilotId = pilotId, AircraftTypeId = da62TypeId, Prefix = "PT-D62" },
                    new PilotAircraft { Id = b748Id, PilotId = pilotId, AircraftTypeId = b748TypeId, Prefix = "PR-748" }
                );

                // Profiles
                db.AircraftLimitProfiles.AddRange(
                    new AircraftLimitProfileEntity
                    {
                        Id = Guid.NewGuid(),
                        AircraftTypeModel = "Cessna C152",
                        Class = "Piston",
                        DataVersion = 1,
                        SimMatchJson = "{}",
                        EngineCount = 1,
                        FeaturesJson = JsonSerializer.Serialize(new[]
                        {
                            AircraftFeatures.Piston,
                            AircraftFeatures.SingleEngine,
                            AircraftFeatures.FixedGear,
                            AircraftFeatures.FixedPitchProp,
                            AircraftFeatures.Carbureted
                        })
                    },
                    new AircraftLimitProfileEntity
                    {
                        Id = Guid.NewGuid(),
                        AircraftTypeModel = "Beechcraft Baron G58",
                        Class = "Piston",
                        DataVersion = 1,
                        SimMatchJson = "{}",
                        EngineCount = 2,
                        FeaturesJson = JsonSerializer.Serialize(new[]
                        {
                            AircraftFeatures.Piston,
                            AircraftFeatures.MultiEngine,
                            AircraftFeatures.RetractableGear,
                            AircraftFeatures.ConstantSpeedProp,
                            AircraftFeatures.FuelInjected
                        })
                    },
                    new AircraftLimitProfileEntity
                    {
                        Id = Guid.NewGuid(),
                        AircraftTypeModel = "Diamond DA62",
                        Class = "Piston",
                        DataVersion = 1,
                        SimMatchJson = "{}",
                        EngineCount = 2,
                        FeaturesJson = JsonSerializer.Serialize(new[]
                        {
                            AircraftFeatures.Piston,
                            AircraftFeatures.Diesel,
                            AircraftFeatures.MultiEngine,
                            AircraftFeatures.RetractableGear,
                            AircraftFeatures.ConstantSpeedProp,
                            AircraftFeatures.FuelInjected,
                            AircraftFeatures.Turbocharged
                        })
                    },
                    new AircraftLimitProfileEntity
                    {
                        Id = Guid.NewGuid(),
                        AircraftTypeModel = "747-8i",
                        Class = "Jet",
                        DataVersion = 1,
                        SimMatchJson = "{}",
                        EngineCount = 4,
                        FeaturesJson = JsonSerializer.Serialize(new[]
                        {
                            AircraftFeatures.Jet,
                            AircraftFeatures.MultiEngine,
                            AircraftFeatures.RetractableGear,
                            AircraftFeatures.Pressurized,
                            AircraftFeatures.Apu,
                            AircraftFeatures.HydraulicGear
                        })
                    }
                );

                await db.SaveChangesAsync();
            }

            var provisioner = new AircraftPartProvisioner(Catalog);

            await using (var db = new AppDbContext(options))
            {
                int created = await provisioner.EnsurePartsAsync(db);
                Assert.True(created > 0);

                var allAircraft = await db.PilotAircrafts
                    .Include(a => a.AircraftType)
                    .Include(a => a.Parts)
                    .ToListAsync();

                // 1. C152 (1 motor)
                var c152 = allAircraft.Single(a => a.Id == c152Id);
                var c152Magnetos = c152.Parts.Where(p => p.PartId == "ignition.magneto_left").ToList();
                Assert.Single(c152Magnetos);
                Assert.Equal(1, c152Magnetos[0].EngineIndex);

                var c152Airframe = c152.Parts.Where(p => p.PartId == "airframe.wing_structure").ToList();
                Assert.Single(c152Airframe);
                Assert.Equal(0, c152Airframe[0].EngineIndex);

                // 2. Baron G58 (2 motores pistão a gasolina)
                var baron = allAircraft.Single(a => a.Id == baronId);
                var baronMagnetos = baron.Parts.Where(p => p.PartId == "ignition.magneto_left").OrderBy(p => p.EngineIndex).ToList();
                Assert.Equal(2, baronMagnetos.Count);
                Assert.Equal(1, baronMagnetos[0].EngineIndex);
                Assert.Equal(2, baronMagnetos[1].EngineIndex);

                var baronCylinders = baron.Parts.Where(p => p.PartId == "engine.cylinders").OrderBy(p => p.EngineIndex).ToList();
                Assert.Equal(2, baronCylinders.Count);
                Assert.Equal(1, baronCylinders[0].EngineIndex);
                Assert.Equal(2, baronCylinders[1].EngineIndex);

                // 3. Diamond DA62 (2 motores diesel)
                var da62 = allAircraft.Single(a => a.Id == da62Id);
                var da62GlowPlugs = da62.Parts.Where(p => p.PartId == "ignition.glow_plugs").OrderBy(p => p.EngineIndex).ToList();
                Assert.Equal(2, da62GlowPlugs.Count);
                Assert.Equal(1, da62GlowPlugs[0].EngineIndex);
                Assert.Equal(2, da62GlowPlugs[1].EngineIndex);

                var da62Cylinders = da62.Parts.Where(p => p.PartId == "engine.cylinders").OrderBy(p => p.EngineIndex).ToList();
                Assert.Equal(2, da62Cylinders.Count);
                Assert.Equal(1, da62Cylinders[0].EngineIndex);
                Assert.Equal(2, da62Cylinders[1].EngineIndex);

                // DA62 não usa magnetos (diesel)
                Assert.DoesNotContain(da62.Parts, p => p.PartId == "ignition.magneto_left");

                // 4. Boeing 747-8i (4 motores turbofan)
                var b748 = allAircraft.Single(a => a.Id == b748Id);
                var b748Combustors = b748.Parts.Where(p => p.PartId == "hotsection.combustor").OrderBy(p => p.EngineIndex).ToList();
                Assert.Equal(4, b748Combustors.Count);
                Assert.Equal(new[] { 1, 2, 3, 4 }, b748Combustors.Select(p => p.EngineIndex));

                var b748Fans = b748.Parts.Where(p => p.PartId == "engine.fan").OrderBy(p => p.EngineIndex).ToList();
                Assert.Equal(4, b748Fans.Count);
                Assert.Equal(new[] { 1, 2, 3, 4 }, b748Fans.Select(p => p.EngineIndex));

                var b748Reversers = b748.Parts.Where(p => p.PartId == "engine.thrust_reverser").OrderBy(p => p.EngineIndex).ToList();
                Assert.Equal(4, b748Reversers.Count);
                Assert.Equal(new[] { 1, 2, 3, 4 }, b748Reversers.Select(p => p.EngineIndex));

                var b748Airframe = b748.Parts.Where(p => p.PartId == "airframe.wing_structure").ToList();
                Assert.Single(b748Airframe);
                Assert.Equal(0, b748Airframe[0].EngineIndex);
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

                // Verifica se a tabela AircraftParts existe e aceita inserções com EngineIndex (incluindo motores 1 e 2 da mesma peça)
                var partEngine1 = new AircraftPart
                {
                    Id = Guid.NewGuid(),
                    PilotAircraftId = aircraftId,
                    PartId = "oil.lubricant",
                    EngineIndex = 1,
                    Health = 100.0,
                    HoursSinceReplacement = 0.0,
                    CyclesSinceReplacement = 0,
                    LastReplacedAtUtc = DateTime.UtcNow,
                    LastReplacedAirframeHours = 0.0,
                    Flag = PartFlag.None,
                    FlaggedBy = PartFlagSource.None,
                    IsBroken = false
                };

                var partEngine2 = new AircraftPart
                {
                    Id = Guid.NewGuid(),
                    PilotAircraftId = aircraftId,
                    PartId = "oil.lubricant",
                    EngineIndex = 2,
                    Health = 95.0,
                    HoursSinceReplacement = 5.0,
                    CyclesSinceReplacement = 2,
                    LastReplacedAtUtc = DateTime.UtcNow,
                    LastReplacedAirframeHours = 5.0,
                    Flag = PartFlag.None,
                    FlaggedBy = PartFlagSource.None,
                    IsBroken = false
                };

                db.AircraftParts.AddRange(partEngine1, partEngine2);
                await db.SaveChangesAsync();

                var savedParts = await db.AircraftParts
                    .Where(p => p.PilotAircraftId == aircraftId && p.PartId == "oil.lubricant")
                    .OrderBy(p => p.EngineIndex)
                    .ToListAsync();

                Assert.Equal(2, savedParts.Count);
                Assert.Equal(1, savedParts[0].EngineIndex);
                Assert.Equal(2, savedParts[1].EngineIndex);
                Assert.Equal(100.0, savedParts[0].Health);
                Assert.Equal(95.0, savedParts[1].Health);
            }
        }

        [Fact]
        public async Task Provisioner_RemovePecasQueNaoSeAplicam()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var cubAircraftId = Guid.NewGuid();
            var c152AircraftId = Guid.NewGuid();
            var cubTypeId = Guid.NewGuid();
            var c152TypeId = Guid.NewGuid();

            var cubBatteryPartId = Guid.NewGuid();
            var cubFlapsPartId = Guid.NewGuid();
            var c152BatteryPartId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var pilot = new Pilot { Id = pilotId, Name = "Piloto Teste" };
                db.Pilots.Add(pilot);

                var cubType = new AircraftType { Id = cubTypeId, Model = "Piper J-3 Cub", Type = "Geral", FuelType = "Avgas" };
                var c152Type = new AircraftType { Id = c152TypeId, Model = "Cessna C152", Type = "Geral", FuelType = "Avgas" };
                db.AircraftTypes.AddRange(cubType, c152Type);

                var cubAircraft = new PilotAircraft { Id = cubAircraftId, PilotId = pilotId, AircraftTypeId = cubTypeId, Prefix = "PT-CUB" };
                var c152Aircraft = new PilotAircraft { Id = c152AircraftId, PilotId = pilotId, AircraftTypeId = c152TypeId, Prefix = "PT-152" };
                db.PilotAircrafts.AddRange(cubAircraft, c152Aircraft);

                var cubProfile = new AircraftLimitProfileEntity
                {
                    Id = Guid.NewGuid(),
                    AircraftTypeModel = "Piper J-3 Cub",
                    Class = "Piston",
                    DataVersion = 1,
                    SimMatchJson = "{}",
                    FeaturesJson = JsonSerializer.Serialize(new[]
                    {
                        AircraftFeatures.Piston,
                        AircraftFeatures.SingleEngine,
                        AircraftFeatures.FixedGear,
                        AircraftFeatures.Tailwheel,
                        AircraftFeatures.FixedPitchProp,
                        AircraftFeatures.Carbureted,
                        AircraftFeatures.NoElectrical,
                        AircraftFeatures.NoFlaps
                    })
                };

                var c152Profile = new AircraftLimitProfileEntity
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
                db.AircraftLimitProfiles.AddRange(cubProfile, c152Profile);

                // Peças do J-3 que NÃO devem se aplicar
                db.AircraftParts.Add(new AircraftPart
                {
                    Id = cubBatteryPartId,
                    PilotAircraftId = cubAircraftId,
                    PartId = "electrical.battery",
                    Health = 90.0
                });
                db.AircraftParts.Add(new AircraftPart
                {
                    Id = cubFlapsPartId,
                    PilotAircraftId = cubAircraftId,
                    PartId = "airframe.flaps",
                    Health = 85.0
                });

                // Peça aplicável do C152
                db.AircraftParts.Add(new AircraftPart
                {
                    Id = c152BatteryPartId,
                    PilotAircraftId = c152AircraftId,
                    PartId = "electrical.battery",
                    Health = 77.0
                });

                await db.SaveChangesAsync();
            }

            var provisioner = new AircraftPartProvisioner(Catalog);

            await using (var db = new AppDbContext(options))
            {
                await provisioner.EnsurePartsAsync(db);

                // J-3: bateria e flaps foram removidas
                var cubParts = await db.AircraftParts.Where(p => p.PilotAircraftId == cubAircraftId).ToListAsync();
                Assert.DoesNotContain(cubParts, p => p.PartId == "electrical.battery");
                Assert.DoesNotContain(cubParts, p => p.PartId == "airframe.flaps");
                Assert.DoesNotContain(cubParts, p => p.Id == cubBatteryPartId);
                Assert.DoesNotContain(cubParts, p => p.Id == cubFlapsPartId);

                // C152: bateria preservada com sua saúde original
                var c152Battery = await db.AircraftParts.SingleOrDefaultAsync(p => p.Id == c152BatteryPartId);
                Assert.NotNull(c152Battery);
                Assert.Equal(77.0, c152Battery.Health);
            }
        }

        [Fact]
        public async Task Provisioner_RemovePecaForaDoCatalogo()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            var typeId = Guid.NewGuid();
            var obsoletePartId1 = Guid.NewGuid();
            var obsoletePartId2 = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var pilot = new Pilot { Id = pilotId, Name = "Piloto Teste" };
                db.Pilots.Add(pilot);

                var c152Type = new AircraftType { Id = typeId, Model = "Cessna C152", Type = "Geral", FuelType = "Avgas" };
                db.AircraftTypes.Add(c152Type);

                var aircraft = new PilotAircraft { Id = aircraftId, PilotId = pilotId, AircraftTypeId = typeId, Prefix = "PT-152" };
                db.PilotAircrafts.Add(aircraft);

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

                // electrical.external_lighting (removida do catálogo)
                db.AircraftParts.Add(new AircraftPart
                {
                    Id = obsoletePartId1,
                    PilotAircraftId = aircraftId,
                    PartId = "electrical.external_lighting",
                    Health = 50.0
                });

                // Peça inexistente arbitrária
                db.AircraftParts.Add(new AircraftPart
                {
                    Id = obsoletePartId2,
                    PilotAircraftId = aircraftId,
                    PartId = "custom.non_existent_part",
                    Health = 50.0
                });

                await db.SaveChangesAsync();
            }

            var provisioner = new AircraftPartProvisioner(Catalog);

            await using (var db = new AppDbContext(options))
            {
                await provisioner.EnsurePartsAsync(db);

                var parts = await db.AircraftParts.Where(p => p.PilotAircraftId == aircraftId).ToListAsync();
                Assert.DoesNotContain(parts, p => p.PartId == "electrical.external_lighting");
                Assert.DoesNotContain(parts, p => p.PartId == "custom.non_existent_part");
                Assert.DoesNotContain(parts, p => p.Id == obsoletePartId1);
                Assert.DoesNotContain(parts, p => p.Id == obsoletePartId2);
            }
        }
    }
}
