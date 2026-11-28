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
    public class FlightServiceOffFieldLandingTests
    {
        private static async Task<(SqliteConnection Connection, DbContextOptions<AppDbContext> Options)> CreateInMemoryDbAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();

            db.Airports.AddRange(
                new Airport { ICAO = "SBJD", Name = "Jundiaí", Latitude = -23.18, Longitude = -46.94, Tier = 1 },
                new Airport { ICAO = "SBBP", Name = "Bragança Paulista", Latitude = -22.98, Longitude = -46.54, Tier = 1 }
            );
            await db.SaveChangesAsync();

            return (connection, options);
        }

        [Fact]
        public async Task Pouso_fora_de_pista_cancela_missoes_da_xp_parcial_avanca_relogio_e_zera_receita()
        {
            var (connection, options) = await CreateInMemoryDbAsync();
            await using (connection)
            {
                var pilotId = Guid.NewGuid();
                var aircraftId = Guid.NewGuid();
                var missionId = Guid.NewGuid();
                var initialClock = new DateTime(2026, 11, 22, 14, 0, 0, DateTimeKind.Utc);

                var activeMission = new Mission
                {
                    Id = missionId,
                    PilotId = pilotId,
                    IsAccepted = true,
                    OriginICAO = "SBJD",
                    DestinationICAO = "SBBP",
                    RewardMoney = 1_500.0,
                    Title = "Frete Regional"
                };

                await using (var db = new AppDbContext(options))
                {
                    var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
                    db.AircraftTypes.Add(type);

                    var aircraft = new PilotAircraft
                    {
                        Id = aircraftId,
                        PilotId = pilotId,
                        AircraftTypeId = type.Id,
                        Prefix = "PT-OFF",
                        FuelAmount = 25.0,
                        EngineCondition = 100.0,
                        TiresCondition = 100.0,
                        AirframeCondition = 100.0,
                        AvionicsCondition = 100.0
                    };

                    var pilot = new Pilot
                    {
                        Id = pilotId,
                        Name = "Piloto Pouso Forçado",
                        Balance = 10_000.0,
                        ExperiencePoints = 100,
                        CareerClockUtc = initialClock,
                        Hangar = { aircraft }
                    };
                    db.Pilots.Add(pilot);
                    db.Missions.Add(new Mission
                    {
                        Id = missionId,
                        PilotId = pilotId,
                        IsAccepted = true,
                        OriginICAO = "SBJD",
                        DestinationICAO = "SBBP",
                        RewardMoney = 1_500.0,
                        Title = "Frete Regional"
                    });
                    await db.SaveChangesAsync();
                }

                await using var serviceContext = new AppDbContext(options);
                var service = new FlightService(serviceContext, null!, null!);

                var outcome = await service.ProcessFlightFinishAsync(
                    pilotId: pilotId,
                    aircraftId: aircraftId,
                    originIcao: "SBJD",
                    destinationIcao: "SBBP",
                    startTime: DateTime.Now.AddMinutes(-60),
                    initialFuel: 25.0,
                    finalFuel: 18.0,
                    distanceNM: 45.2,
                    activeMissions: new List<Mission> { activeMission },
                    qualityService: new FlightQualityService(),
                    isOffFieldLanding: true,
                    nearestDistance: 45.2,
                    flightDurationMinutes: 60.0
                );

                Assert.False(outcome.Success);
                Assert.Equal(0, outcome.RemainingMissions);
                Assert.Equal(0, outcome.CompletedMissions);
                Assert.Equal(0, outcome.Revenue);
                Assert.True(outcome.XP > 0);

                await using var check = new AppDbContext(options);
                Assert.Empty(await check.Missions.ToListAsync());

                var updatedPilot = await check.Pilots.FindAsync(pilotId);
                Assert.NotNull(updatedPilot);
                Assert.True(updatedPilot!.CareerClockUtc > initialClock);
                Assert.Equal(100 + outcome.XP, updatedPilot.ExperiencePoints);
            }
        }

        [Fact]
        public async Task Pouso_fora_de_pista_com_terrainDamageApplied_true_nao_altera_pneus_nem_celula()
        {
            var (connection, options) = await CreateInMemoryDbAsync();
            await using (connection)
            {
                var pilotId = Guid.NewGuid();
                var aircraftId = Guid.NewGuid();

                await using (var db = new AppDbContext(options))
                {
                    var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
                    db.AircraftTypes.Add(type);

                    db.Pilots.Add(new Pilot
                    {
                        Id = pilotId,
                        Name = "Piloto Sem Dano Duplicado",
                        Hangar =
                        {
                            new PilotAircraft
                            {
                                Id = aircraftId,
                                PilotId = pilotId,
                                AircraftTypeId = type.Id,
                                Prefix = "PT-TRN",
                                FuelAmount = 20.0,
                                TiresCondition = 82.0,
                                AirframeCondition = 91.0
                            }
                        }
                    });
                    await db.SaveChangesAsync();
                }

                await using var serviceContext = new AppDbContext(options);
                var service = new FlightService(serviceContext, null!, null!);

                await service.ProcessFlightFinishAsync(
                    pilotId: pilotId,
                    aircraftId: aircraftId,
                    originIcao: "SBJD",
                    destinationIcao: "SBBP",
                    startTime: DateTime.Now.AddMinutes(-30),
                    initialFuel: 20.0,
                    finalFuel: 16.0,
                    distanceNM: 20.0,
                    activeMissions: new List<Mission>(),
                    qualityService: new FlightQualityService(),
                    tiresCondition: 82.0,
                    airframeCondition: 91.0,
                    isOffFieldLanding: true,
                    nearestDistance: 12.0,
                    flightDurationMinutes: 30.0,
                    terrainDamageApplied: true
                );

                await using var check = new AppDbContext(options);
                var updatedAircraft = await check.PilotAircrafts.FindAsync(aircraftId);
                Assert.NotNull(updatedAircraft);
                Assert.Equal(82.0, updatedAircraft!.TiresCondition, 2);
                Assert.Equal(91.0, updatedAircraft.AirframeCondition, 2);
            }
        }

        [Fact]
        public async Task Pouso_fora_de_pista_com_terrainDamageApplied_false_aplica_dano_em_pneus_e_celula()
        {
            var (connection, options) = await CreateInMemoryDbAsync();
            await using (connection)
            {
                var pilotId = Guid.NewGuid();
                var aircraftId = Guid.NewGuid();

                await using (var db = new AppDbContext(options))
                {
                    var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
                    db.AircraftTypes.Add(type);

                    db.Pilots.Add(new Pilot
                    {
                        Id = pilotId,
                        Name = "Piloto Com Dano Terreno",
                        Hangar =
                        {
                            new PilotAircraft
                            {
                                Id = aircraftId,
                                PilotId = pilotId,
                                AircraftTypeId = type.Id,
                                Prefix = "PT-DMG",
                                FuelAmount = 20.0,
                                TiresCondition = 80.0,
                                AirframeCondition = 90.0
                            }
                        }
                    });
                    await db.SaveChangesAsync();
                }

                await using var serviceContext = new AppDbContext(options);
                var service = new FlightService(serviceContext, null!, null!);

                await service.ProcessFlightFinishAsync(
                    pilotId: pilotId,
                    aircraftId: aircraftId,
                    originIcao: "SBJD",
                    destinationIcao: "SBBP",
                    startTime: DateTime.Now.AddMinutes(-30),
                    initialFuel: 20.0,
                    finalFuel: 16.0,
                    distanceNM: 20.0,
                    activeMissions: new List<Mission>(),
                    qualityService: new FlightQualityService(),
                    tiresCondition: 80.0,
                    airframeCondition: 90.0,
                    isOffFieldLanding: true,
                    nearestDistance: 12.0,
                    flightDurationMinutes: 30.0,
                    terrainDamageApplied: false
                );

                await using var check = new AppDbContext(options);
                var updatedAircraft = await check.PilotAircrafts.FindAsync(aircraftId);
                Assert.NotNull(updatedAircraft);
                Assert.Equal(45.0, updatedAircraft!.TiresCondition, 2);
                Assert.Equal(70.0, updatedAircraft.AirframeCondition, 2);
            }
        }
    }
}
