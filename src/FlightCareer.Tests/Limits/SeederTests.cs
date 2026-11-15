using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Limits
{
    public class SeederTests
    {
        [Fact]
        public async Task Seeder_InsertsAndIsIdempotent()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var seeder = new AircraftLimitSeeder(db);
                int inserted = await seeder.SeedLimitsAsync();
                Assert.True(inserted > 0);

                // Execução subsequente: idempotente (nada é inserido ou alterado)
                int secondRun = await seeder.SeedLimitsAsync();
                Assert.Equal(0, secondRun);
            }

            await using (var db = new AppDbContext(options))
            {
                var c152 = await db.AircraftLimitProfiles
                    .Include(p => p.Limits)
                    .FirstOrDefaultAsync(p => p.AircraftTypeModel == "Cessna C152");

                Assert.NotNull(c152);
                Assert.NotEmpty(c152.Limits);
                Assert.Contains(c152.Limits, l => l.Key == "vne" && l.Value == 149.0);
            }
        }

        [Fact]
        public async Task Seeder_UpdatesOnlyWhenDataVersionHigher()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FC_Test_Limits_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string aircraftJson = Path.Combine(tempDir, "cessna-c152.json");
                File.WriteAllText(aircraftJson, """
                {
                  "schemaVersion": 1,
                  "dataVersion": 1,
                  "id": "Cessna C152",
                  "class": "Piston",
                  "features": ["piston", "singleEngine", "fixedGear", "fixedPitchProp", "carbureted"],
                  "simMatch": { "title": ["C152"], "atcModel": ["C152"] },
                  "limits": [
                    { "key": "vne", "value": 149.0, "unit": "knots", "source": "SimVar", "reference": "Ref v1", "notes": null }
                  ]
                }
                """);

                await using var connection = new SqliteConnection("Data Source=:memory:");
                await connection.OpenAsync();
                var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

                await using (var db = new AppDbContext(options))
                {
                    await db.Database.EnsureCreatedAsync();
                    var loader = new AircraftLimitCatalogLoader(tempDir);
                    var seeder = new AircraftLimitSeeder(db, loader);
                    int ins = await seeder.SeedLimitsAsync();
                    Assert.Equal(1, ins);
                }

                // 1. Tentar rodar com versão IGUAL (dataVersion = 1), mas valor modificado: NÃO deve atualizar
                File.WriteAllText(aircraftJson, """
                {
                  "schemaVersion": 1,
                  "dataVersion": 1,
                  "id": "Cessna C152",
                  "class": "Piston",
                  "features": ["piston", "singleEngine", "fixedGear", "fixedPitchProp", "carbureted"],
                  "simMatch": { "title": ["C152"], "atcModel": ["C152"] },
                  "limits": [
                    { "key": "vne", "value": 999.0, "unit": "knots", "source": "SimVar", "reference": "Ref v1", "notes": null }
                  ]
                }
                """);

                await using (var db = new AppDbContext(options))
                {
                    var loader = new AircraftLimitCatalogLoader(tempDir);
                    var seeder = new AircraftLimitSeeder(db, loader);
                    int updated = await seeder.SeedLimitsAsync();
                    Assert.Equal(0, updated);

                    var current = await db.AircraftLimitProfiles.Include(p => p.Limits).SingleAsync();
                    Assert.Equal(149.0, current.Limits.First().Value);
                }

                // 2. Tentar rodar com versão MENOR (dataVersion = 0): loader rejeita
                File.WriteAllText(aircraftJson, """
                {
                  "schemaVersion": 1,
                  "dataVersion": 0,
                  "id": "Cessna C152",
                  "class": "Piston",
                  "simMatch": { "title": ["C152"], "atcModel": ["C152"] },
                  "limits": [
                    { "key": "vne", "value": 888.0, "unit": "knots", "source": "SimVar", "reference": "Ref v0", "notes": null }
                  ]
                }
                """);

                await using (var db = new AppDbContext(options))
                {
                    var loader = new AircraftLimitCatalogLoader(tempDir);
                    var seeder = new AircraftLimitSeeder(db, loader);
                    Assert.Throws<InvalidDataException>(() => loader.LoadProfiles());
                }

                // 3. Atualizar com dataVersion MAIOR (dataVersion = 2): DEVE atualizar
                File.WriteAllText(aircraftJson, """
                {
                  "schemaVersion": 1,
                  "dataVersion": 2,
                  "id": "Cessna C152",
                  "class": "Piston",
                  "features": ["piston", "singleEngine", "fixedGear", "fixedPitchProp", "carbureted"],
                  "simMatch": { "title": ["C152"], "atcModel": ["C152"] },
                  "limits": [
                    { "key": "vne", "value": 155.0, "unit": "knots", "source": "SimVar", "reference": "Ref v2", "notes": null }
                  ]
                }
                """);

                await using (var db = new AppDbContext(options))
                {
                    var loader = new AircraftLimitCatalogLoader(tempDir);
                    var seeder = new AircraftLimitSeeder(db, loader);
                    int updated = await seeder.SeedLimitsAsync();
                    Assert.Equal(1, updated);

                    var current = await db.AircraftLimitProfiles.Include(p => p.Limits).SingleAsync();
                    Assert.Equal(2, current.DataVersion);
                    Assert.Equal(155.0, current.Limits.First().Value);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task Seeder_OldDatabaseGetsTables()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                // Simula um banco legado dropando as tabelas de limites
                await db.Database.ExecuteSqlRawAsync("DROP TABLE AircraftLimits;");
                await db.Database.ExecuteSqlRawAsync("DROP TABLE AircraftLimitProfiles;");
            }

            await using (var db = new AppDbContext(options))
            {
                // EnsureSchemaUpdatedAsync recria as tabelas ausentes
                await db.EnsureSchemaUpdatedAsync();
                await db.EnsureSchemaUpdatedAsync(); // idempotente

                var seeder = new AircraftLimitSeeder(db);
                int count = await seeder.SeedLimitsAsync();
                Assert.True(count > 0);
            }

            await using (var db = new AppDbContext(options))
            {
                var any = await db.AircraftLimitProfiles.AnyAsync();
                Assert.True(any);
            }
        }
    }
}
