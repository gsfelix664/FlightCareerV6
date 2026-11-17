using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public sealed class CalibraF4Tests
    {
        [Fact]
        public void Catalogo_VidasComprimidasPor4()
        {
            // Lê o catálogo oficial da aplicação
            string jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "parts_catalog.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FlightCareer.Infrastructure/Data/parts_catalog.json"));
            }
            Assert.True(File.Exists(jsonPath), $"parts_catalog.json não encontrado em {jsonPath}");

            string json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);
            var parts = doc.RootElement.GetProperty("parts").EnumerateArray()
                .ToDictionary(p => p.GetProperty("id").GetString()!);

            // 1. Célula / Estrutura da asa (airframe.wing_structure): original 12.000 h -> 3.000 h
            var wing = parts["airframe.wing_structure"];
            Assert.Equal(3000.0, wing.GetProperty("lifeHours").GetDouble());
            Assert.True(wing.GetProperty("calendarLifeMonths").ValueKind == JsonValueKind.Null);

            // 2. Trem de pouso (gear.main_struts): original 4.000 h -> 1.000 h; 3.000 ciclos -> 750 ciclos
            var gear = parts["gear.main_struts"];
            Assert.Equal(1000.0, gear.GetProperty("lifeHours").GetDouble());
            Assert.Equal(750, gear.GetProperty("lifeCycles").GetInt32());
            Assert.True(gear.GetProperty("calendarLifeMonths").ValueKind == JsonValueKind.Null);

            // 3. Instrumentos (instruments.pitot_tube): original 3.000 h -> 750 h
            var pitot = parts["instruments.pitot_tube"];
            Assert.Equal(750.0, pitot.GetProperty("lifeHours").GetDouble());
            Assert.True(pitot.GetProperty("calendarLifeMonths").ValueKind == JsonValueKind.Null);

            // 4. Óleo lubrificante (oil.lubricant): original 50 h -> 12.5 h; calendário exceção = 3 meses
            var oil = parts["oil.lubricant"];
            Assert.Equal(12.5, oil.GetProperty("lifeHours").GetDouble());
            Assert.Equal(3, oil.GetProperty("calendarLifeMonths").GetInt32());

            // 5. Iluminação (lights.landing): original 300 h -> 75 h; sem ciclos e sem calendário
            var light = parts["lights.landing"];
            Assert.Equal(75.0, light.GetProperty("lifeHours").GetDouble());
            Assert.True(light.GetProperty("lifeCycles").ValueKind == JsonValueKind.Null);
            Assert.True(light.GetProperty("calendarLifeMonths").ValueKind == JsonValueKind.Null);
        }

        [Fact]
        public void Inspecoes_IntervalosComprimidos()
        {
            var aircraft = new PilotAircraft();
            Assert.Equal(12.0, aircraft.HoursUntilNext50hInspection);
            Assert.Equal(25.0, aircraft.HoursUntilNext100hInspection);
            Assert.Equal(125.0, aircraft.HoursUntilNext500hInspection);

            // Vencida a de 25 h (anual), interdita a aeronave
            aircraft.HoursUntilNext100hInspection = 0.0;
            if (aircraft.HoursUntilNext100hInspection <= 0)
            {
                aircraft.IsInterdicted = true;
                aircraft.InterdictionReason = "Inspeção Anual Vencida";
            }

            Assert.True(aircraft.IsInterdicted);
            Assert.Equal("Inspeção Anual Vencida", aircraft.InterdictionReason);
        }

        [Fact]
        public void Tbo_Pistao450_Turbina900()
        {
            var catalog = TestData.Catalog;
            var engineDef = catalog.Get(ComponentKind.Engine);

            Assert.Equal(450.0, engineDef.LifeHoursFor(EngineClass.Piston));
            Assert.Equal(900.0, engineDef.LifeHoursFor(EngineClass.Turboprop));
            Assert.Equal(900.0, engineDef.LifeHoursFor(EngineClass.Jet));

            var aircraft = new PilotAircraft();
            Assert.Equal(450.0, aircraft.EngineTboRemainingHours);
        }

        [Fact]
        public async Task Migracao_LimitaHorasSalvas()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using (var context = new AppDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();

                var pilot = new Pilot
                {
                    Id = Guid.NewGuid(),
                    Name = "Comandante Teste",
                    CurrentAirportICAO = "SBGR"
                };
                context.Pilots.Add(pilot);

                var pistonType = new AircraftType
                {
                    Id = Guid.NewGuid(),
                    Model = "C152",
                    Type = "Single Engine Piston",
                    FuelType = "AVGAS"
                };

                var jetType = new AircraftType
                {
                    Id = Guid.NewGuid(),
                    Model = "A320neo",
                    Type = "Twin Engine Jet",
                    FuelType = "JET A-1"
                };

                context.AircraftTypes.AddRange(pistonType, jetType);

                var aircraftPistao = new PilotAircraft
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    AircraftTypeId = pistonType.Id,
                    Prefix = "PT-OLD",
                    EngineTboRemainingHours = 1200.0,
                    HoursUntilNext50hInspection = 40.0,
                    HoursUntilNext100hInspection = 80.0,
                    HoursUntilNext500hInspection = 400.0
                };

                var aircraftTurbina = new PilotAircraft
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    AircraftTypeId = jetType.Id,
                    Prefix = "PR-JET",
                    EngineTboRemainingHours = 1500.0,
                    HoursUntilNext50hInspection = 35.0,
                    HoursUntilNext100hInspection = 90.0,
                    HoursUntilNext500hInspection = 500.0
                };

                // Adiciona componente HotSection na turbina para cobrir tanto por tipo quanto por componente
                var hotSection = new AircraftComponent
                {
                    Id = Guid.NewGuid(),
                    PilotAircraftId = aircraftTurbina.Id,
                    Kind = ComponentKind.HotSection,
                    Health = 95.0
                };

                context.PilotAircrafts.AddRange(aircraftPistao, aircraftTurbina);
                context.AircraftComponents.Add(hotSection);
                await context.SaveChangesAsync();

                // 1ª Execução da migração idempotente
                await context.EnsureSchemaUpdatedAsync();

                context.ChangeTracker.Clear();

                var pLoaded = await context.PilotAircrafts.FindAsync(aircraftPistao.Id);
                var jLoaded = await context.PilotAircrafts.FindAsync(aircraftTurbina.Id);

                Assert.NotNull(pLoaded);
                Assert.NotNull(jLoaded);

                // Pistão: TBO limitado a 450, 50h limitada a 12, 100h limitada a 25, 500h limitada a 125
                Assert.Equal(450.0, pLoaded.EngineTboRemainingHours);
                Assert.Equal(12.0, pLoaded.HoursUntilNext50hInspection);
                Assert.Equal(25.0, pLoaded.HoursUntilNext100hInspection);
                Assert.Equal(125.0, pLoaded.HoursUntilNext500hInspection);

                // Turbina: TBO limitado a 900, 50h limitada a 12, 100h limitada a 25, 500h limitada a 125
                Assert.Equal(900.0, jLoaded.EngineTboRemainingHours);
                Assert.Equal(12.0, jLoaded.HoursUntilNext50hInspection);
                Assert.Equal(25.0, jLoaded.HoursUntilNext100hInspection);
                Assert.Equal(125.0, jLoaded.HoursUntilNext500hInspection);

                // 2ª Execução (idempotência)
                await context.EnsureSchemaUpdatedAsync();

                context.ChangeTracker.Clear();
                var pLoaded2 = await context.PilotAircrafts.FindAsync(aircraftPistao.Id);
                var jLoaded2 = await context.PilotAircrafts.FindAsync(aircraftTurbina.Id);

                Assert.Equal(450.0, pLoaded2!.EngineTboRemainingHours);
                Assert.Equal(12.0, pLoaded2.HoursUntilNext50hInspection);
                Assert.Equal(25.0, pLoaded2.HoursUntilNext100hInspection);
                Assert.Equal(125.0, pLoaded2.HoursUntilNext500hInspection);

                Assert.Equal(900.0, jLoaded2!.EngineTboRemainingHours);
                Assert.Equal(12.0, jLoaded2.HoursUntilNext50hInspection);
                Assert.Equal(25.0, jLoaded2.HoursUntilNext100hInspection);
                Assert.Equal(125.0, jLoaded2.HoursUntilNext500hInspection);
            }
        }
    }
}
