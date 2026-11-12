using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static FlightCareer.Tests.Components.TestData;

namespace FlightCareer.Tests.Components
{
    public class ComponentPersistenceTests
    {
        [Fact]
        public async Task Banco_antigo_recebe_tabela_de_componentes_e_backfill_das_aeronaves()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var pistonId = Guid.NewGuid();
            var turbopropId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                // Simula um banco criado antes desta versão (sem a tabela nova)
                await db.Database.ExecuteSqlRawAsync("DROP TABLE AircraftComponents;");

                var c172 = new AircraftType { Id = Guid.NewGuid(), Model = "C172 Skyhawk", Type = "Geral", FuelType = "Avgas" };
                var pc12 = new AircraftType { Id = Guid.NewGuid(), Model = "PC-12 NGX", Type = "Geral", FuelType = "Jet A-1" };
                db.AircraftTypes.AddRange(c172, pc12);
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Teste" });
                await db.SaveChangesAsync();

                // Insere as aeronaves via SQL para não depender da tabela ausente no rastreamento do EF
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO PilotAircrafts (Id, PilotId, AircraftTypeId, PurchaseDate, CurrentAirframeHours, Prefix, EngineCondition, TiresCondition, AirframeCondition, AvionicsCondition, LandingsCount, FuelAmount, CurrentLocationICAO, EngineTboRemainingHours, HoursUntilNext50hInspection, HoursUntilNext100hInspection, HoursUntilNext500hInspection, MagnetosCondition, OilCondition, IsInterdicted, InterdictionReason, Configuration) " +
                    "VALUES ({0}, {1}, {2}, '2026-01-01', 1200, 'PP-AAA', 70, 60, 90, 85, 400, 30, 'SBSP', 1000, 10, 60, 300, 75, 45, 0, '', 'Passageiro')",
                    pistonId, pilotId, c172.Id);
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO PilotAircrafts (Id, PilotId, AircraftTypeId, PurchaseDate, CurrentAirframeHours, Prefix, EngineCondition, TiresCondition, AirframeCondition, AvionicsCondition, LandingsCount, FuelAmount, CurrentLocationICAO, EngineTboRemainingHours, HoursUntilNext50hInspection, HoursUntilNext100hInspection, HoursUntilNext500hInspection, MagnetosCondition, OilCondition, IsInterdicted, InterdictionReason, Configuration) " +
                    "VALUES ({0}, {1}, {2}, '2026-01-01', 3000, 'PP-BBB', 95, 90, 98, 99, 900, 200, 'SBRJ', 3000, 40, 90, 500, 100, 90, 0, '', 'Passageiro')",
                    turbopropId, pilotId, pc12.Id);
            }

            await using (var db = new AppDbContext(options))
            {
                await db.EnsureSchemaUpdatedAsync();

                var provisioner = new AircraftComponentProvisioner(Catalog, new FixedRandom(0.5));
                Assert.Equal(18, await provisioner.EnsureComponentsAsync(db));
                Assert.Equal(0, await provisioner.EnsureComponentsAsync(db)); // idempotente
            }

            await using (var db = new AppDbContext(options))
            {
                var piston = await db.PilotAircrafts.Include(a => a.Components).SingleAsync(a => a.Id == pistonId);
                var turboprop = await db.PilotAircrafts.Include(a => a.Components).SingleAsync(a => a.Id == turbopropId);

                Assert.Equal(9, piston.Components.Count);
                Assert.Contains(piston.Components, c => c.Kind == ComponentKind.Ignition && c.Health == 75.0);
                Assert.Contains(piston.Components, c => c.Kind == ComponentKind.Oil && c.Health == 45.0);
                Assert.Contains(turboprop.Components, c => c.Kind == ComponentKind.HotSection && c.Health == 95.0);
                Assert.DoesNotContain(turboprop.Components, c => c.Kind == ComponentKind.Ignition);
            }
        }
    }
}
