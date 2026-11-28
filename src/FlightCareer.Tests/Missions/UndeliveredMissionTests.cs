using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    /// <summary>
    /// Revisão FASES-2A: só volta ao mercado o contrato que pousou no destino sem cumprir a exigência (TGL/pontos).
    /// Pouso intermediário (multi-leg) mantém a missão aceita, com a origem atualizada.
    /// </summary>
    public class UndeliveredMissionTests
    {
        private static async Task<(DbContextOptions<AppDbContext> options, Guid pilotId, Guid aircraftId)> SetupAsync(SqliteConnection connection, Mission mission)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            await using var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
            db.Pilots.Add(new Pilot { Id = pilotId, Name = "Comandante Multi" });
            db.AircraftTypes.Add(type);
            db.PilotAircrafts.Add(new PilotAircraft { Id = aircraftId, PilotId = pilotId, AircraftTypeId = type.Id, Prefix = "PT-MLG", FuelAmount = 20.0 });
            mission.PilotId = pilotId;
            mission.IsAccepted = true;
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
            return (options, pilotId, aircraftId);
        }

        private static Task<Application.DTOs.FlightOutcome> FinishAsync(AppDbContext db, Guid pilotId, Guid aircraftId, string landedAt, Mission mission, int tgl) =>
            new FlightService(db, null!, null!).ProcessFlightFinishAsync(
                pilotId: pilotId, aircraftId: aircraftId, originIcao: "SBAA", destinationIcao: landedAt,
                startTime: DateTime.Now.AddHours(-1), initialFuel: 20.0, finalFuel: 15.0, distanceNM: 50.0,
                activeMissions: new List<Mission> { mission }, qualityService: new FlightQualityService(),
                flightDurationMinutes: 40.0, visitedWaypoints: new HashSet<string>(), touchAndGoCount: tgl);

        [Fact]
        public async Task Pouso_intermediario_mantem_a_missao_aceita()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var mission = new Mission { Id = Guid.NewGuid(), Title = "Carga", OriginICAO = "SBAA", DestinationICAO = "SBCC", RewardMoney = 1000, SubId = "2.3.1" };
            var (options, pilotId, aircraftId) = await SetupAsync(connection, mission);

            await using (var db = new AppDbContext(options))
            {
                var m = await db.Missions.AsNoTracking().FirstAsync(x => x.Id == mission.Id);
                await FinishAsync(db, pilotId, aircraftId, "SBBB", m, 0);
            }
            await using (var db = new AppDbContext(options))
            {
                var m = await db.Missions.FirstAsync(x => x.Id == mission.Id);
                Assert.True(m.IsAccepted);
                Assert.Equal(pilotId, m.PilotId);
                Assert.Equal("SBBB", m.OriginICAO);
            }
        }

        [Fact]
        public async Task Circuito_incompleto_no_destino_volta_ao_mercado_com_o_mesmo_valor()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var mission = new Mission { Id = Guid.NewGuid(), Title = "Circuito", OriginICAO = "SBAA", DestinationICAO = "SBAA", RewardMoney = 700, SubId = "3.5.2", RequiredTouchAndGoes = 4 };
            var (options, pilotId, aircraftId) = await SetupAsync(connection, mission);

            await using (var db = new AppDbContext(options))
            {
                var m = await db.Missions.AsNoTracking().FirstAsync(x => x.Id == mission.Id);
                await FinishAsync(db, pilotId, aircraftId, "SBAA", m, 2);
            }
            await using (var db = new AppDbContext(options))
            {
                var m = await db.Missions.FirstAsync(x => x.Id == mission.Id);
                Assert.False(m.IsAccepted);
                Assert.Null(m.PilotId);
                Assert.Equal(700, m.RewardMoney);
            }
        }
    }
}
