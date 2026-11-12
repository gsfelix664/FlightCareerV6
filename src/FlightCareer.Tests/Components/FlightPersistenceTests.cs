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
    }
}
