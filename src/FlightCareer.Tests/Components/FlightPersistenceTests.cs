using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Components
{
    /// <summary>
    /// O FlightService é singleton e guarda um contexto do banco por toda a sessão. Mudanças feitas
    /// entre dois voos por outras telas (Hangar, mercado) precisam ser vistas no segundo voo.
    /// </summary>
    public class FlightPersistenceTests
    {
        [Fact]
        public async Task Segundo_voo_ve_inspecao_e_gastos_feitos_entre_os_voos()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                var type = new AircraftType { Id = Guid.NewGuid(), Model = "C172SP G1000", Type = "Geral", FuelType = "Avgas", PurchasePrice = 400_000 };
                setup.AircraftTypes.Add(type);
                setup.Pilots.Add(new Pilot
                {
                    Id = pilotId, Name = "Teste", Balance = 10_000,
                    Hangar = { new PilotAircraft { Id = aircraftId, AircraftTypeId = type.Id, Prefix = "PT-AAA", FuelAmount = 40, HoursUntilNext50hInspection = 50, HoursUntilNext100hInspection = 100 } }
                });
                await setup.SaveChangesAsync();
            }

            await using var serviceContext = new AppDbContext(options);
            var service = new FlightService(serviceContext, null!, null!);

            // Voo 1 (1 h): 50 → 49 h
            await service.ProcessFlightFinishAsync(pilotId, aircraftId, "SBSP", "SBRJ", DateTime.Now, 40, 30, 100, new List<Mission>(), new FlightQualityService(), flightDurationMinutes: 60);

            // Entre os voos, o Hangar (outro contexto) faz a inspeção de 50 h e gasta 2.000
            await using (var hangar = new AppDbContext(options))
            {
                var a = await hangar.PilotAircrafts.FindAsync(aircraftId);
                var p = await hangar.Pilots.FindAsync(pilotId);
                a!.HoursUntilNext50hInspection = 50;
                p!.Balance -= 2_000;
                await hangar.SaveChangesAsync();
            }

            double balanceBefore;
            await using (var read = new AppDbContext(options))
                balanceBefore = (await read.Pilots.FindAsync(pilotId))!.Balance;

            // Voo 2 (1 h): deve partir de 50 h e do saldo já descontado
            await service.ProcessFlightFinishAsync(pilotId, aircraftId, "SBRJ", "SBSP", DateTime.Now, 30, 20, 100, new List<Mission>(), new FlightQualityService(), flightDurationMinutes: 60);

            await using var check = new AppDbContext(options);
            var aircraft = await check.PilotAircrafts.FindAsync(aircraftId);
            var pilot = await check.Pilots.FindAsync(pilotId);

            Assert.Equal(49, aircraft!.HoursUntilNext50hInspection, 3);
            Assert.True(pilot!.Balance < balanceBefore, "o custo do voo 2 é descontado do saldo atual");
            Assert.True(pilot.Balance <= balanceBefore - 1, "o gasto de 2.000 do Hangar não pode ser desfeito pelo FlightService");
        }

        [Fact]
        public async Task FlightService_AutoStartUsed_ReducesXpAndMentionsInMessage()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilot1Id = Guid.NewGuid();
            var pilot2Id = Guid.NewGuid();
            var aircraft1Id = Guid.NewGuid();
            var aircraft2Id = Guid.NewGuid();

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                var type = new AircraftType { Id = Guid.NewGuid(), Model = "C172SP G1000", Type = "Geral", FuelType = "Avgas", PurchasePrice = 400_000 };
                setup.AircraftTypes.Add(type);
                setup.Pilots.Add(new Pilot
                {
                    Id = pilot1Id, Name = "Piloto Manual", Balance = 10_000, ExperiencePoints = 0,
                    Hangar = { new PilotAircraft { Id = aircraft1Id, AircraftTypeId = type.Id, Prefix = "PT-MAN", FuelAmount = 40 } }
                });
                setup.Pilots.Add(new Pilot
                {
                    Id = pilot2Id, Name = "Piloto AutoStart", Balance = 10_000, ExperiencePoints = 0,
                    Hangar = { new PilotAircraft { Id = aircraft2Id, AircraftTypeId = type.Id, Prefix = "PT-AUT", FuelAmount = 40 } }
                });
                await setup.SaveChangesAsync();
            }

            // Voo sem auto-start
            await using var serviceContext1 = new AppDbContext(options);
            var service1 = new FlightService(serviceContext1, null!, null!);
            var outcomeNormal = await service1.ProcessFlightFinishAsync(
                pilot1Id, aircraft1Id, "SBSP", "SBRJ", DateTime.Now, 40, 30, 100,
                new List<Mission>(), new FlightQualityService(), flightDurationMinutes: 60, autoStartUsed: false);

            // Voo idêntico com auto-start
            await using var serviceContext2 = new AppDbContext(options);
            var service2 = new FlightService(serviceContext2, null!, null!);
            var outcomeAutoStart = await service2.ProcessFlightFinishAsync(
                pilot2Id, aircraft2Id, "SBSP", "SBRJ", DateTime.Now, 40, 30, 100,
                new List<Mission>(), new FlightQualityService(), flightDurationMinutes: 60, autoStartUsed: true);

            int expectedXp = ProcedurePenalties.ApplyAutoStartPenalty(outcomeNormal.XP, true);
            Assert.True(outcomeNormal.XP > 0, "o voo normal deve render XP");
            Assert.Equal(expectedXp, outcomeAutoStart.XP);
            Assert.True(outcomeAutoStart.XP < outcomeNormal.XP, "auto-start deve ter menos XP que o normal");
            Assert.Contains(" | Partida automática usada (−10% XP)", outcomeAutoStart.Message);
            Assert.DoesNotContain("Partida automática usada", outcomeNormal.Message);

            // Confere persistência no banco
            await using var verifyContext = new AppDbContext(options);
            var pilotAuto = await verifyContext.Pilots.FindAsync(pilot2Id);
            var flightAuto = await verifyContext.Flights.FirstOrDefaultAsync(f => f.PilotId == pilot2Id);

            Assert.NotNull(pilotAuto);
            Assert.NotNull(flightAuto);
            Assert.Equal(expectedXp, pilotAuto.ExperiencePoints);
            Assert.Equal(expectedXp, flightAuto.XP);
        }
    }
}
