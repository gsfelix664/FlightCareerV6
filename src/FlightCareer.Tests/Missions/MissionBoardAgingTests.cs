using System;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionBoardAgingTests
    {
        private static readonly DateTime FixedNow = new(2026, 11, 27, 12, 0, 0);

        [Fact]
        public async Task AdvanceAsync_ContratoComFolga_PerdeValidade()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var id = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Missions.Add(new Mission
                {
                    Id = id,
                    Title = "Frete Regional",
                    OriginICAO = "SBSP",
                    DestinationICAO = "SBRJ",
                    ExpirationTime = FixedNow.AddHours(10),
                    IsAccepted = false
                });
                await db.SaveChangesAsync();

                int expired = await MissionBoardAging.AdvanceAsync(db, 2.0, FixedNow);
                Assert.Equal(0, expired);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var mission = await db.Missions.SingleAsync(m => m.Id == id);
                Assert.Equal(FixedNow.AddHours(8), mission.ExpirationTime);
            }
        }

        [Fact]
        public async Task AdvanceAsync_ContratoCurto_VenceESai()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Missions.Add(new Mission
                {
                    Id = Guid.NewGuid(),
                    Title = "Voo Urgente",
                    OriginICAO = "SBSP",
                    DestinationICAO = "SBKP",
                    ExpirationTime = FixedNow.AddHours(1),
                    IsAccepted = false
                });
                await db.SaveChangesAsync();

                int expired = await MissionBoardAging.AdvanceAsync(db, 2.0, FixedNow);
                Assert.Equal(1, expired);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                Assert.Empty(await db.Missions.ToListAsync());
            }
        }

        [Fact]
        public async Task AdvanceAsync_ContratoAceito_NaoMuda()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var id = Guid.NewGuid();
            var originalExpiration = FixedNow.AddHours(1);

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Missions.Add(new Mission
                {
                    Id = id,
                    Title = "Contrato no Manifesto",
                    OriginICAO = "SBSP",
                    DestinationICAO = "SBRJ",
                    ExpirationTime = originalExpiration,
                    IsAccepted = true
                });
                await db.SaveChangesAsync();

                int expired = await MissionBoardAging.AdvanceAsync(db, 72.0, FixedNow);
                Assert.Equal(0, expired);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var mission = await db.Missions.SingleAsync(m => m.Id == id);
                Assert.True(mission.IsAccepted);
                Assert.Equal(originalExpiration, mission.ExpirationTime);
            }
        }

        [Fact]
        public async Task AdvanceAsync_JaVencido_SaiSemContar()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Missions.Add(new Mission
                {
                    Id = Guid.NewGuid(),
                    Title = "Contrato Expirado Antes",
                    OriginICAO = "SBSP",
                    DestinationICAO = "SBGR",
                    ExpirationTime = FixedNow.AddHours(-1),
                    IsAccepted = false
                });
                await db.SaveChangesAsync();

                int expired = await MissionBoardAging.AdvanceAsync(db, 2.0, FixedNow);
                Assert.Equal(0, expired);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                Assert.Empty(await db.Missions.ToListAsync());
            }
        }

        [Fact]
        public async Task AdvanceAsync_OrigensDiferentes_EnvelhecemJuntas()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var idSbsp = Guid.NewGuid();
            var idSbrj = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Missions.AddRange(
                    new Mission
                    {
                        Id = idSbsp,
                        Title = "Origem SP",
                        OriginICAO = "SBSP",
                        DestinationICAO = "SBKP",
                        ExpirationTime = FixedNow.AddHours(10),
                        IsAccepted = false
                    },
                    new Mission
                    {
                        Id = idSbrj,
                        Title = "Origem RJ",
                        OriginICAO = "SBRJ",
                        DestinationICAO = "SBVT",
                        ExpirationTime = FixedNow.AddHours(1),
                        IsAccepted = false
                    });
                await db.SaveChangesAsync();

                int expired = await MissionBoardAging.AdvanceAsync(db, 2.0, FixedNow);
                Assert.Equal(1, expired);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var remaining = await db.Missions.ToListAsync();
                Assert.Single(remaining);
                Assert.Equal(idSbsp, remaining[0].Id);
                Assert.Equal(FixedNow.AddHours(8), remaining[0].ExpirationTime);
            }
        }

        [Fact]
        public async Task AdvanceAsync_HorasZeroOuNegativas_NadaMuda()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var idValido = Guid.NewGuid();
            var idVencido = Guid.NewGuid();
            var expValido = FixedNow.AddHours(1);
            var expVencido = FixedNow.AddHours(-1);

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Missions.AddRange(
                    new Mission
                    {
                        Id = idValido,
                        Title = "Valido",
                        OriginICAO = "SBSP",
                        DestinationICAO = "SBKP",
                        ExpirationTime = expValido,
                        IsAccepted = false
                    },
                    new Mission
                    {
                        Id = idVencido,
                        Title = "Vencido",
                        OriginICAO = "SBRJ",
                        DestinationICAO = "SBVT",
                        ExpirationTime = expVencido,
                        IsAccepted = false
                    });
                await db.SaveChangesAsync();

                Assert.Equal(0, await MissionBoardAging.AdvanceAsync(db, 0.0, FixedNow));
                Assert.Equal(0, await MissionBoardAging.AdvanceAsync(db, -5.0, FixedNow));
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var missions = await db.Missions.ToDictionaryAsync(m => m.Id);
                Assert.Equal(2, missions.Count);
                Assert.Equal(expValido, missions[idValido].ExpirationTime);
                Assert.Equal(expVencido, missions[idVencido].ExpirationTime);
            }
        }
    }
}
