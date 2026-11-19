using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static FlightCareer.Tests.Components.TestData;

namespace FlightCareer.Tests.Components
{
    public class ComponentPersistenceTests
    {
        [Fact]
        public async Task Banco_antigo_recebe_tabela_de_componentes_e_backfill_das_aeronaves()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var pistonId = Guid.NewGuid();
            var turbopropId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                // Simula um banco criado antes desta versão (sem a tabela nova)
                await db.Database.ExecuteSqlRawAsync("DROP TABLE AircraftComponents;");

                var c172 = new AircraftType { Id = Guid.NewGuid(), Model = "C172 Skyhawk", Type = "Geral", FuelType = "Avgas" };
                var pc12 = new AircraftType { Id = Guid.NewGuid(), Model = "PC-12 NGX", Type = "Geral", FuelType = "Jet A-1" };
                db.AircraftTypes.AddRange(c172, pc12);
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Teste" });
                await db.SaveChangesAsync();

                // Insere as aeronaves via SQL para não depender da tabela ausente no rastreamento do EF
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO PilotAircrafts (Id, PilotId, AircraftTypeId, PurchaseDate, CurrentAirframeHours, Prefix, EngineCondition, TiresCondition, AirframeCondition, AvionicsCondition, LandingsCount, FuelAmount, CurrentLocationICAO, EngineTboRemainingHours, HoursUntilNext50hInspection, HoursUntilNext100hInspection, HoursUntilNext500hInspection, MagnetosCondition, OilCondition, IsInterdicted, InterdictionReason, Configuration) " +
                    "VALUES ({0}, {1}, {2}, '2026-01-01', 1200, 'PP-AAA', 70, 60, 90, 85, 400, 30, 'SBSP', 1000, 10, 60, 300, 75, 45, 0, '', 'Passageiro')",
                    pistonId, pilotId, c172.Id);
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO PilotAircrafts (Id, PilotId, AircraftTypeId, PurchaseDate, CurrentAirframeHours, Prefix, EngineCondition, TiresCondition, AirframeCondition, AvionicsCondition, LandingsCount, FuelAmount, CurrentLocationICAO, EngineTboRemainingHours, HoursUntilNext50hInspection, HoursUntilNext100hInspection, HoursUntilNext500hInspection, MagnetosCondition, OilCondition, IsInterdicted, InterdictionReason, Configuration) " +
                    "VALUES ({0}, {1}, {2}, '2026-01-01', 3000, 'PP-BBB', 95, 90, 98, 99, 900, 200, 'SBRJ', 3000, 40, 90, 500, 100, 90, 0, '', 'Passageiro')",
                    turbopropId, pilotId, pc12.Id);
            }

            await using (var db = new AppDbContext(options))
            {
                await db.EnsureSchemaUpdatedAsync();

                var provisioner = new AircraftComponentProvisioner(Catalog, new FixedRandom(0.5));
                Assert.Equal(18, await provisioner.EnsureComponentsAsync(db));
                Assert.Equal(0, await provisioner.EnsureComponentsAsync(db)); // idempotente
            }

            await using (var db = new AppDbContext(options))
            {
                var piston = await db.PilotAircrafts.Include(a => a.Components).SingleAsync(a => a.Id == pistonId);
                var turboprop = await db.PilotAircrafts.Include(a => a.Components).SingleAsync(a => a.Id == turbopropId);

                Assert.Equal(9, piston.Components.Count);
                Assert.Contains(piston.Components, c => c.Kind == ComponentKind.Ignition && c.Health == 75.0);
                Assert.Contains(piston.Components, c => c.Kind == ComponentKind.Oil && c.Health == 45.0);
                Assert.Contains(turboprop.Components, c => c.Kind == ComponentKind.HotSection && c.Health == 95.0);
                Assert.DoesNotContain(turboprop.Components, c => c.Kind == ComponentKind.Ignition);
            }
        }

        [Fact]
        public async Task Banco_antigo_recebe_tabela_de_panes_persistentes_com_o_mesmo_mapeamento_do_EF()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var aircraftId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                await db.Database.ExecuteSqlRawAsync("DROP TABLE AircraftDefects;");
            }

            await using (var db = new AppDbContext(options))
            {
                await db.EnsureSchemaUpdatedAsync();
                await db.EnsureSchemaUpdatedAsync(); // idempotente
                db.AircraftDefects.Add(new AircraftDefect
                {
                    Id = Guid.NewGuid(), PilotAircraftId = aircraftId, Code = "PITOT", Title = "Tubo de pitot obstruído",
                    Description = "", Cause = "teste", FlightPhase = "CRUZEIRO", IsGrounding = false, RepairCost = 250,
                    DetectedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var open = await db.AircraftDefects.Where(d => d.PilotAircraftId == aircraftId && !d.IsResolved).ToListAsync();
                Assert.Single(open);
                Assert.Equal("PITOT", open[0].Code);
                Assert.Null(open[0].ResolvedAtUtc);
            }
        }

        [Fact]
        public async Task Banco_antigo_sem_FlaggedAirframeHours_recebe_coluna_e_grava_peca()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            var partId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                // Recria a tabela AircraftParts sem a coluna FlaggedAirframeHours para simular banco antigo
                await db.Database.ExecuteSqlRawAsync("DROP TABLE AircraftParts;");
                await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE AircraftParts (
                    Id TEXT NOT NULL PRIMARY KEY,
                    PilotAircraftId TEXT NOT NULL,
                    PartId TEXT NOT NULL,
                    EngineIndex INTEGER NOT NULL DEFAULT 0,
                    Health REAL NOT NULL,
                    HoursSinceReplacement REAL NOT NULL,
                    CyclesSinceReplacement INTEGER NOT NULL,
                    LastReplacedAtUtc TEXT NOT NULL,
                    LastReplacedAirframeHours REAL NOT NULL,
                    Flag INTEGER NOT NULL,
                    FlaggedBy INTEGER NOT NULL,
                    FlaggedAtUtc TEXT NULL,
                    IsBroken INTEGER NOT NULL,
                    BrokenAtUtc TEXT NULL,
                    LastShopServiceAtUtc TEXT NULL,
                    LastShopServiceAirframeHours REAL NULL,
                    LastShopServiceCycles INTEGER NULL
                );");

                var c172 = new AircraftType { Id = Guid.NewGuid(), Model = "C172 Skyhawk", Type = "Geral", FuelType = "Avgas" };
                db.AircraftTypes.Add(c172);
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Teste" });
                await db.SaveChangesAsync();

                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO PilotAircrafts (Id, PilotId, AircraftTypeId, PurchaseDate, CurrentAirframeHours, Prefix, EngineCondition, TiresCondition, AirframeCondition, AvionicsCondition, LandingsCount, FuelAmount, CurrentLocationICAO, EngineTboRemainingHours, HoursUntilNext50hInspection, HoursUntilNext100hInspection, HoursUntilNext500hInspection, MagnetosCondition, OilCondition, IsInterdicted, InterdictionReason, Configuration) " +
                    "VALUES ({0}, {1}, {2}, '2026-01-01', 1200, 'PP-AAA', 70, 60, 90, 85, 400, 30, 'SBSP', 1000, 10, 60, 300, 75, 45, 0, '', 'Passageiro')",
                    aircraftId, pilotId, c172.Id);
            }

            await using (var db = new AppDbContext(options))
            {
                // Aplica migração que adiciona FlaggedAirframeHours
                await db.EnsureSchemaUpdatedAsync();

                db.AircraftParts.Add(new AircraftPart
                {
                    Id = partId,
                    PilotAircraftId = aircraftId,
                    PartId = "engine.cylinders",
                    EngineIndex = 0,
                    Health = 85.0,
                    HoursSinceReplacement = 90.0,
                    CyclesSinceReplacement = 40,
                    LastReplacedAtUtc = DateTime.UtcNow,
                    LastReplacedAirframeHours = 1110.0,
                    Flag = PartFlag.Suspect,
                    FlaggedBy = PartFlagSource.Inspection,
                    FlaggedAtUtc = DateTime.UtcNow,
                    FlaggedAirframeHours = 1200.0,
                    IsBroken = false
                });

                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var part = await db.AircraftParts.FirstOrDefaultAsync(p => p.Id == partId);
                Assert.NotNull(part);
                Assert.Equal(PartFlag.Suspect, part.Flag);
                Assert.Equal(PartFlagSource.Inspection, part.FlaggedBy);
                Assert.Equal(1200.0, part.FlaggedAirframeHours);
            }
        }

        [Fact]
        public async Task Banco_antigo_recebe_coluna_CareerClockUtc_na_tabela_Pilots()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var pilotId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                // Simula tabela de pilotos em banco antigo sem a coluna CareerClockUtc
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Pilots DROP COLUMN CareerClockUtc;");

                // Insere piloto legado sem relógio de carreira
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Pilots (Id, Name, CompanyName, InitialBaseICAO, CurrentAirportICAO, Balance, Difficulty, TotalHours, ExperiencePoints, Rank, PilotCategory, HasPPLicense, HasCPLicense, HasIFRRating, HasMultiEngineRating, HasATPLicense, HasCFIInstructor) " +
                    "VALUES ({0}, 'Piloto Legado', 'Voe Bem', 'SBMT', 'SBMT', 50000, 0, 100, 500, 'Private Pilot', 1, 1, 0, 0, 0, 0, 0)",
                    pilotId);
            }

            await using (var db = new AppDbContext(options))
            {
                // Aplica EnsureSchemaUpdatedAsync, que adiciona a coluna e inicializa com timestamp UTC
                await db.EnsureSchemaUpdatedAsync();
                await db.EnsureSchemaUpdatedAsync(); // Idempotente

                var pilot = await db.Pilots.FirstOrDefaultAsync(p => p.Id == pilotId);
                Assert.NotNull(pilot);
                Assert.True(pilot.CareerClockUtc > DateTime.MinValue, "CareerClockUtc deve ser preenchido pela migração.");

                // Testa persistência e atualização normal
                var advancedClock = pilot.CareerClockUtc.AddHours(12.0);
                pilot.CareerClockUtc = advancedClock;
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var pilot = await db.Pilots.FirstOrDefaultAsync(p => p.Id == pilotId);
                Assert.NotNull(pilot);
                Assert.True(pilot.CareerClockUtc > DateTime.MinValue);
            }
        }
    }
}
