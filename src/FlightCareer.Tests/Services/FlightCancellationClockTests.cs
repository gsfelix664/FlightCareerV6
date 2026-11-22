using System;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Services
{
    /// <summary>Cancelar um voo ativo também avança o relógio de carreira (tempo voado + 0,5 h de solo).</summary>
    public class FlightCancellationClockTests
    {
        [Fact]
        public async Task Cancelamento_avanca_o_relogio_de_carreira()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var pilotId = Guid.NewGuid();
            var start = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Comandante Cancela", CareerClockUtc = start });
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                await new FlightService(db, null!, null!).ProcessFlightCancellationAsync(pilotId, 0.75);
            }

            await using (var db = new AppDbContext(options))
            {
                var pilot = await db.Pilots.FindAsync(pilotId);
                Assert.Equal(start.AddHours(0.75 + 0.5), pilot!.CareerClockUtc);
            }
        }
    }
}
