using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using FlightCareer.Domain.Xp;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Xp
{
    public class FlightFinishXpPersistenceTests
    {
        [Fact]
        public async Task ProcessFlightFinishAsync_WithXpInputs_SavesCalculatedXpAndUpdatesPilotLevel()
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
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Piloto XP", ExperiencePoints = 7400 });
                db.AircraftTypes.Add(type);
                db.PilotAircrafts.Add(new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = type.Id,
                    Prefix = "PT-XPT",
                    CurrentAirframeHours = 50.0,
                    FuelAmount = 20.0
                });
                await db.SaveChangesAsync();
            }

            var xpInputs = new XpFlightInputs(
                AirborneMinutes: 40,
                DistanceNm: 60,
                LegsCompleted: 1,
                MissionFamilyId: "instrucao",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 120,
                WorstTouchdownG: 1.15,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: 45,
                EteMinutes: 50,
                IsUrgentMission: false
            );

            var expectedBreakdown = XpCalculator.Compute(xpInputs);
            int expectedXp = expectedBreakdown.Total;

            await using (var db = new AppDbContext(options))
            {
                var service = new FlightService(db, null!, null!);
                var outcome = await service.ProcessFlightFinishAsync(
                    pilotId: pilotId,
                    aircraftId: aircraftId,
                    originIcao: "SBMT",
                    destinationIcao: "SBDN",
                    startTime: DateTime.Now.AddMinutes(-45),
                    initialFuel: 20.0,
                    finalFuel: 12.0,
                    distanceNM: 60.0,
                    activeMissions: new List<Mission>(),
                    qualityService: new FlightQualityService(),
                    flightDurationMinutes: 45.0,
                    xpInputs: xpInputs
                );

                Assert.True(outcome.Success);
                Assert.Equal(expectedXp, outcome.XP);
                Assert.NotNull(outcome.XpBreakdownLines);
                Assert.NotEmpty(outcome.XpBreakdownLines!);
            }

            await using (var db = new AppDbContext(options))
            {
                var pilot = await db.Pilots.FindAsync(pilotId);
                Assert.NotNull(pilot);
                Assert.Equal(7400 + expectedXp, pilot!.ExperiencePoints);
                // 7400 XP era nível 5 (7400 < 7500). Com +expectedXp (> 100 XP), passa de 7500 -> vira nível 6!
                Assert.True(pilot.Level >= 6, $"Nível esperado >= 6 para XP {pilot.ExperiencePoints}, mas foi {pilot.Level}");

                var flight = await db.Flights.FirstOrDefaultAsync(f => f.PilotId == pilotId);
                Assert.NotNull(flight);
                Assert.Equal(expectedXp, flight!.XP);
            }
        }

        [Fact]
        public async Task ProcessFlightFinishAsync_InvalidatedFlight_GivesZeroXp()
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
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Piloto Trapaceiro", ExperiencePoints = 500 });
                db.AircraftTypes.Add(type);
                db.PilotAircrafts.Add(new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = type.Id,
                    Prefix = "PT-BAD",
                    CurrentAirframeHours = 10.0,
                    FuelAmount = 20.0
                });
                await db.SaveChangesAsync();
            }

            var xpInputs = new XpFlightInputs(
                AirborneMinutes: 60,
                DistanceNm: 100,
                LegsCompleted: 1,
                MissionFamilyId: "carga_expressa",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 100,
                WorstTouchdownG: 1.1,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: 60,
                EteMinutes: 60,
                IsUrgentMission: false
            );

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
                    finalFuel: 10.0,
                    distanceNM: 100.0,
                    activeMissions: new List<Mission>(),
                    qualityService: new FlightQualityService(),
                    flightDurationMinutes: 60.0,
                    isFlightInvalidated: true,
                    invalidationReason: "Combustível alterado em voo",
                    xpInputs: xpInputs
                );

                Assert.False(outcome.Success);
                Assert.Equal(0, outcome.XP);
            }

            await using (var db = new AppDbContext(options))
            {
                var pilot = await db.Pilots.FindAsync(pilotId);
                Assert.NotNull(pilot);
                Assert.Equal(500, pilot!.ExperiencePoints); // Inalterado
            }
        }
    }
}
