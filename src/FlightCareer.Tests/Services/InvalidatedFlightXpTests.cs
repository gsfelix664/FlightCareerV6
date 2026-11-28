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

namespace FlightCareer.Tests.Services
{
    /// <summary>Voo invalidado (slew, congelamento, combustível) não pode somar XP no piloto.</summary>
    public class InvalidatedFlightXpTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Fim_de_voo_so_soma_XP_quando_o_voo_e_valido(bool invalidado)
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Comandante XP", ExperiencePoints = 100 });
                db.AircraftTypes.Add(type);
                db.PilotAircrafts.Add(new PilotAircraft
                {
                    Id = aircraftId, PilotId = pilotId, AircraftTypeId = type.Id, Prefix = "PT-XPI",
                    CurrentAirframeHours = 100.0, FuelAmount = 20.0
                });
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var service = new FlightService(db, null!, null!);
                var outcome = await service.ProcessFlightFinishAsync(
                    pilotId: pilotId,
                    aircraftId: aircraftId,
                    originIcao: "SBMT",
                    destinationIcao: "SBAU",
                    startTime: DateTime.Now.AddHours(-1),
                    initialFuel: 20.0,
                    finalFuel: 15.0,
                    distanceNM: 100.0,
                    activeMissions: new List<Mission>(),
                    qualityService: new FlightQualityService(),
                    flightDurationMinutes: 60.0,
                    isFlightInvalidated: invalidado,
                    invalidationReason: invalidado ? "Slew no ar" : null);

                Assert.Equal(!invalidado, outcome.Success);
                if (invalidado) Assert.Equal(0, outcome.XP);
            }

            await using (var db = new AppDbContext(options))
            {
                var pilot = await db.Pilots.FindAsync(pilotId);
                Assert.NotNull(pilot);
                if (invalidado)
                    Assert.Equal(100, pilot!.ExperiencePoints);
                else
                    Assert.True(pilot!.ExperiencePoints > 100);
            }
        }
    }
}
