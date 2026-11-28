using System;
using System.Collections.Generic;
using System.Linq;
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
    public class FlightFinishTotalLossTests
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
        public async Task Fim_de_voo_nao_cobra_combustivel_consumido()
        {
            var (connection, options) = await CreateInMemoryDbAsync();
            await using (connection)
            {
                var pilotId = Guid.NewGuid();
                var aircraftId = Guid.NewGuid();
                var missionId = Guid.NewGuid();
                const double initialBalance = 10_000.0;
                const double missionReward = 1_500.0;

                await using (var db = new AppDbContext(options))
                {
                    var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
                    db.AircraftTypes.Add(type);

                    var aircraft = new PilotAircraft
                    {
                        Id = aircraftId,
                        PilotId = pilotId,
                        AircraftTypeId = type.Id,
                        Prefix = "PT-TST",
                        FuelAmount = 20.0,
                        EngineCondition = 100,
                        TiresCondition = 100,
                        AirframeCondition = 100,
                        AvionicsCondition = 100
                    };

                    var pilot = new Pilot
                    {
                        Id = pilotId,
                        Name = "Piloto Teste",
                        Balance = initialBalance,
                        Hangar = { aircraft }
                    };
                    db.Pilots.Add(pilot);

                    var mission = new Mission
                    {
                        Id = missionId,
                        PilotId = pilotId,
                        IsAccepted = true,
                        OriginICAO = "SBJD",
                        DestinationICAO = "SBBP",
                        RewardMoney = missionReward,
                        Title = "Transporte de Peças"
                    };
                    db.Missions.Add(mission);
                    await db.SaveChangesAsync();
                }

                await using var serviceContext = new AppDbContext(options);
                var service = new FlightService(serviceContext, null!, null!);

                var activeMission = new Mission
                {
                    Id = missionId,
                    PilotId = pilotId,
                    IsAccepted = true,
                    OriginICAO = "SBJD",
                    DestinationICAO = "SBBP",
                    RewardMoney = missionReward,
                    Title = "Transporte de Peças"
                };

                var quality = new FlightQualityService();
                var outcome = await service.ProcessFlightFinishAsync(
                    pilotId,
                    aircraftId,
                    "SBJD",
                    "SBBP",
                    DateTime.Now.AddMinutes(-30),
                    initialFuel: 20.0,
                    finalFuel: 15.0,
                    distanceNM: 25.0,
                    new List<Mission> { activeMission },
                    quality,
                    flightDurationMinutes: 30
                );

                Assert.True(outcome.Success);
                Assert.Equal(0, outcome.FuelCost);
                Assert.True(outcome.FuelUsed > 0, "Consumo em galões deve ser registrado para informação");

                await using var check = new AppDbContext(options);
                var transactions = await check.FinancialTransactions.Where(t => t.PilotId == pilotId).ToListAsync();
                Assert.DoesNotContain(transactions, t => t.Category == "Combustível");

                var updatedPilot = await check.Pilots.FindAsync(pilotId);
                Assert.NotNull(updatedPilot);

                double expectedBalance = initialBalance + outcome.Revenue - outcome.LandingFee;
                Assert.Equal(expectedBalance, updatedPilot.Balance, 2);
            }
        }

        [Fact]
        public async Task Pouso_fora_de_pista_nao_cobra_combustivel()
        {
            var (connection, options) = await CreateInMemoryDbAsync();
            await using (connection)
            {
                var pilotId = Guid.NewGuid();
                var aircraftId = Guid.NewGuid();
                const double initialBalance = 10_000.0;

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
                        FuelAmount = 20.0,
                        EngineCondition = 100,
                        TiresCondition = 100,
                        AirframeCondition = 100,
                        AvionicsCondition = 100
                    };

                    var pilot = new Pilot
                    {
                        Id = pilotId,
                        Name = "Piloto Emergencia",
                        Balance = initialBalance,
                        Hangar = { aircraft }
                    };
                    db.Pilots.Add(pilot);
                    await db.SaveChangesAsync();
                }

                await using var serviceContext = new AppDbContext(options);
                var service = new FlightService(serviceContext, null!, null!);

                var outcome = await service.ProcessFlightFinishAsync(
                    pilotId,
                    aircraftId,
                    "SBJD",
                    "SBBP",
                    DateTime.Now.AddMinutes(-20),
                    initialFuel: 20.0,
                    finalFuel: 16.0,
                    distanceNM: 15.0,
                    new List<Mission>(),
                    new FlightQualityService(),
                    isOffFieldLanding: true,
                    nearestDistance: 4.5,
                    flightDurationMinutes: 20
                );

                Assert.False(outcome.Success);
                Assert.Equal(0, outcome.FuelCost);
                Assert.True(outcome.FuelUsed > 0);

                await using var check = new AppDbContext(options);
                var transactions = await check.FinancialTransactions.Where(t => t.PilotId == pilotId).ToListAsync();
                Assert.DoesNotContain(transactions, t => t.Category == "Combustível");

                var updatedPilot = await check.Pilots.FindAsync(pilotId);
                Assert.NotNull(updatedPilot);
                Assert.Equal(initialBalance, updatedPilot.Balance, 2);
            }
        }

        [Fact]
        public async Task Perda_total_registra_voo_de_acidente_e_devolve_missoes()
        {
            var (connection, options) = await CreateInMemoryDbAsync();
            await using (connection)
            {
                var pilotId = Guid.NewGuid();
                var aircraftId = Guid.NewGuid();
                var mission1Id = Guid.NewGuid();
                var mission2Id = Guid.NewGuid();
                var initialClock = new DateTime(2026, 11, 22, 14, 0, 0, DateTimeKind.Utc);
                const double initialHours = 50.0;
                const double reward1 = 1200.0;
                const double reward2 = 2500.0;

                await using (var db = new AppDbContext(options))
                {
                    var type = new AircraftType { Id = Guid.NewGuid(), Model = "Cessna C152", Type = "Geral", FuelType = "Avgas" };
                    db.AircraftTypes.Add(type);

                    var aircraft = new PilotAircraft
                    {
                        Id = aircraftId,
                        PilotId = pilotId,
                        AircraftTypeId = type.Id,
                        Prefix = "PT-CRS",
                        FuelAmount = 25.0
                    };

                    var pilot = new Pilot
                    {
                        Id = pilotId,
                        Name = "Piloto Acidente",
                        Balance = 5_000.0,
                        TotalHours = initialHours,
                        CareerClockUtc = initialClock,
                        Hangar = { aircraft }
                    };
                    db.Pilots.Add(pilot);

                    var m1 = new Mission
                    {
                        Id = mission1Id,
                        PilotId = pilotId,
                        IsAccepted = true,
                        OriginICAO = "SBJD",
                        DestinationICAO = "SBBP",
                        RewardMoney = reward1,
                        DistanceNM = 25.0,
                        Title = "Carga Expressa"
                    };
                    var m2 = new Mission
                    {
                        Id = mission2Id,
                        PilotId = pilotId,
                        IsAccepted = true,
                        OriginICAO = "SBJD",
                        DestinationICAO = "SBBP",
                        RewardMoney = reward2,
                        DistanceNM = 25.0,
                        Title = "Passageiro VIP"
                    };
                    db.Missions.AddRange(m1, m2);
                    await db.SaveChangesAsync();
                }

                await using var serviceContext = new AppDbContext(options);
                var service = new FlightService(serviceContext, null!, null!);

                var activeMissions = new List<Mission>
                {
                    new() { Id = mission1Id, PilotId = pilotId, IsAccepted = true, OriginICAO = "SBJD", DestinationICAO = "SBBP", RewardMoney = reward1 },
                    new() { Id = mission2Id, PilotId = pilotId, IsAccepted = true, OriginICAO = "SBJD", DestinationICAO = "SBBP", RewardMoney = reward2 }
                };

                const double flightDurationMinutes = 13.0;
                const double distanceNM = 8.5;

                await service.ProcessTotalLossAsync(
                    pilotId,
                    aircraftId,
                    originIcao: "SBJD",
                    crashSiteIcao: "SBBP",
                    flightDurationMinutes: flightDurationMinutes,
                    distanceNM: distanceNM,
                    activeMissions: activeMissions
                );

                await using var check = new AppDbContext(options);

                // 1. Voo no logbook
                var flights = await check.Flights.Where(f => f.PilotId == pilotId).ToListAsync();
                Assert.Single(flights);
                var flight = flights[0];
                Assert.Equal("SBJD", flight.OriginICAO);
                Assert.Equal("SBBP", flight.DestinationICAO);
                Assert.Equal(flightDurationMinutes / 60.0, flight.DurationHours, 4);
                Assert.Equal(distanceNM, flight.DistanceNM, 2);
                Assert.Equal(0, flight.Revenue);
                Assert.Equal(0, flight.XP);
                Assert.Equal("Cessna C152", flight.AircraftModel);

                // 2. Piloto: horas e relógio de carreira
                var updatedPilot = await check.Pilots.FindAsync(pilotId);
                Assert.NotNull(updatedPilot);
                Assert.Equal(initialHours + (flightDurationMinutes / 60.0), updatedPilot.TotalHours, 4);

                var expectedClock = CareerClock.AdvanceFlight(initialClock, flightDurationMinutes / 60.0);
                Assert.Equal(expectedClock, updatedPilot.CareerClockUtc);

                // 3. Sem transações financeiras novas
                var transactions = await check.FinancialTransactions.Where(t => t.PilotId == pilotId).ToListAsync();
                Assert.Empty(transactions);

                // 4. Missões devolvidas ao mercado
                var m1Db = await check.Missions.FindAsync(mission1Id);
                var m2Db = await check.Missions.FindAsync(mission2Id);
                Assert.NotNull(m1Db);
                Assert.NotNull(m2Db);

                Assert.False(m1Db.IsAccepted);
                Assert.Null(m1Db.PilotId);
                Assert.Equal("SBJD", m1Db.OriginICAO);
                Assert.Equal(reward1, m1Db.RewardMoney);
                Assert.True(m1Db.DistanceNM > 0);

                Assert.False(m2Db.IsAccepted);
                Assert.Null(m2Db.PilotId);
                Assert.Equal("SBJD", m2Db.OriginICAO);
                Assert.Equal(reward2, m2Db.RewardMoney);
                Assert.True(m2Db.DistanceNM > 0);

                // Lista em memória também atualizada
                Assert.False(activeMissions[0].IsAccepted);
                Assert.Null(activeMissions[0].PilotId);
                Assert.Equal("SBJD", activeMissions[0].OriginICAO);
            }
        }
    }
}
