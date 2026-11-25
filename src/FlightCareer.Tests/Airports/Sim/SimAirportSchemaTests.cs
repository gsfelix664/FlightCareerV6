using System;
using System.Threading.Tasks;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Airports.Sim
{
    public class SimAirportSchemaTests
    {
        [Fact]
        public async Task Schema_BancoAntigo_GanhaColunasETabelas()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var airportId = Guid.NewGuid();

            // 1. Cria esquema antigo (tabela Airports sem as colunas Source, SimRegion, SimFingerprint, SimCheckedUtc)
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Airports (
                        Id TEXT PRIMARY KEY COLLATE NOCASE,
                        ICAO TEXT NOT NULL,
                        Name TEXT NOT NULL DEFAULT '',
                        City TEXT NOT NULL DEFAULT '',
                        Country TEXT NOT NULL DEFAULT '',
                        Latitude REAL NOT NULL DEFAULT 0,
                        Longitude REAL NOT NULL DEFAULT 0,
                        ElevationFt INTEGER NOT NULL DEFAULT 0,
                        Tier INTEGER NOT NULL DEFAULT 1,
                        IsInSimulator INTEGER NOT NULL DEFAULT 0,
                        MsfsIdent TEXT NULL
                    );
                    INSERT INTO Airports (Id, ICAO, Name, City, Country, Latitude, Longitude, ElevationFt, Tier, IsInSimulator, MsfsIdent)
                    VALUES (@id, 'SBSP', 'Congonhas', 'São Paulo', 'BR', -23.6261, -46.6564, 802, 2, 1, 'SBSP');
                ";
                var p = cmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = airportId.ToString();
                cmd.Parameters.Add(p);
                await cmd.ExecuteNonQueryAsync();
            }

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            // 2. Executa a migração EnsureSchemaUpdatedAsync
            await using (var db = new AppDbContext(options))
            {
                await db.EnsureSchemaUpdatedAsync();
            }

            // 3. Testa se o EF Core consegue ler e gravar com o novo esquema
            await using (var db = new AppDbContext(options))
            {
                var sbsp = await db.Airports.FirstOrDefaultAsync(a => a.ICAO == "SBSP");
                Assert.NotNull(sbsp);
                Assert.Equal("", sbsp!.Source);

                sbsp.Source = "Sim";
                sbsp.SimRegion = "SB";
                sbsp.SimFingerprint = "fp-sbsp";
                sbsp.SimCheckedUtc = DateTime.UtcNow;

                db.SimAirportQueue.Add(new SimAirportQueueItem
                {
                    Ident = "SBGR",
                    Region = "SB",
                    Latitude = -23.4356,
                    Longitude = -46.4731,
                    AltitudeM = 750,
                    Fingerprint = "fp-sbgr",
                    Priority = 1,
                    Attempts = 0,
                    EnqueuedUtc = DateTime.UtcNow
                });

                db.SimSyncStates.Add(new SimSyncState
                {
                    Id = 1,
                    SimVersion = "1.37.19.0",
                    ListDigest = "digest-test",
                    LastListUtc = DateTime.UtcNow,
                    ListCount = 100
                });

                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var sbsp = await db.Airports.FirstOrDefaultAsync(a => a.ICAO == "SBSP");
                Assert.NotNull(sbsp);
                Assert.Equal("Sim", sbsp!.Source);
                Assert.Equal("SB", sbsp.SimRegion);
                Assert.Equal("fp-sbsp", sbsp.SimFingerprint);

                var queueItem = await db.SimAirportQueue.FirstOrDefaultAsync(q => q.Ident == "SBGR");
                Assert.NotNull(queueItem);
                Assert.Equal(1, queueItem!.Priority);

                var state = await db.SimSyncStates.FirstOrDefaultAsync(s => s.Id == 1);
                Assert.NotNull(state);
                Assert.Equal("1.37.19.0", state!.SimVersion);
            }
        }
    }
}
