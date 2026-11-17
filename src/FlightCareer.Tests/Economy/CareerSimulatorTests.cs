using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using FlightCareer.Application.Simulation;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    public class CareerSimulatorTests
    {
        [Fact]
        public void MaintenancePricing_MesmosValoresDaUI()
        {
            // Critério: 850 / 2.500 / 1.450 / 20 % mín. 25.000
            Assert.Equal(850.0, MaintenancePricing.Cost50hInspection);
            Assert.Equal(2500.0, MaintenancePricing.Cost100hInspection);
            Assert.Equal(1450.0, MaintenancePricing.Cost500hInspection);
            Assert.Equal(1200.0, MaintenancePricing.CostToggleConfiguration);

            // Retífica com piso de $25.000
            Assert.Equal(25000.0, MaintenancePricing.CalculateOverhaulCost(50000.0));
            Assert.Equal(25000.0, MaintenancePricing.CalculateOverhaulCost(100000.0));
            Assert.Equal(25000.0, MaintenancePricing.CalculateOverhaulCost(125000.0));

            // Retífica proporcional acima do piso (20%)
            Assert.Equal(30000.0, MaintenancePricing.CalculateOverhaulCost(150000.0));
            Assert.Equal(40000.0, MaintenancePricing.CalculateOverhaulCost(200000.0));
            Assert.Equal(200000.0, MaintenancePricing.CalculateOverhaulCost(1000000.0));
        }

        [Fact]
        public async Task CareerSim_MesmaSemente_MesmoResultado()
        {
            // Critério: 2 bots, 5 dias, --fast, mesma semente -> mesmo resultado
            var (options1, tempDb1) = await CreateTestEnvironmentAsync();
            var (options2, tempDb2) = await CreateTestEnvironmentAsync();

            try
            {
                var simOptions1 = new CareerSimulationOptions
                {
                    AircraftModels = new[] { "Cessna C152" },
                    BotsPerAircraft = 2,
                    Days = 5,
                    HoursPerDay = 2.0,
                    Seed = 42,
                    FastMode = true,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), $"sim_out_1_{Guid.NewGuid():N}")
                };

                var simOptions2 = new CareerSimulationOptions
                {
                    AircraftModels = new[] { "Cessna C152" },
                    BotsPerAircraft = 2,
                    Days = 5,
                    HoursPerDay = 2.0,
                    Seed = 42,
                    FastMode = true,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), $"sim_out_2_{Guid.NewGuid():N}")
                };

                var sim1 = new CareerSimulator(() => new AppDbContext(options1));
                var sim2 = new CareerSimulator(() => new AppDbContext(options2));

                var res1 = await sim1.RunAsync(simOptions1);
                var res2 = await sim2.RunAsync(simOptions2);

                Assert.Equal(res1.Flights.Count, res2.Flights.Count);
                Assert.Equal(res1.Maintenance.Count, res2.Maintenance.Count);
                Assert.Equal(res1.Daily.Count, res2.Daily.Count);

                for (int i = 0; i < res1.Flights.Count; i++)
                {
                    var f1 = res1.Flights[i];
                    var f2 = res2.Flights[i];
                    Assert.Equal(f1.Origin, f2.Origin);
                    Assert.Equal(f1.Destination, f2.Destination);
                    Assert.Equal(f1.Hours, f2.Hours, 2);
                    Assert.Equal(f1.Revenue, f2.Revenue, 2);
                    Assert.Equal(f1.FuelCost, f2.FuelCost, 2);
                    Assert.Equal(f1.TotalFees, f2.TotalFees, 2);
                }

                // Limpa diretórios temporários
                if (Directory.Exists(simOptions1.OutputDirectory)) Directory.Delete(simOptions1.OutputDirectory, true);
                if (Directory.Exists(simOptions2.OutputDirectory)) Directory.Delete(simOptions2.OutputDirectory, true);
            }
            finally
            {
                CleanupDb(tempDb1);
                CleanupDb(tempDb2);
            }
        }

        [Fact]
        public async Task CareerSim_InspecaoVencida_BotFazInspecao()
        {
            // Critério: avião com 100 h vencendo -> linha de inspeção com o preço do jogo ($2.500)
            var (options, tempDb) = await CreateTestEnvironmentAsync();

            try
            {
                var simOptions = new CareerSimulationOptions
                {
                    AircraftModels = new[] { "Cessna C152" },
                    BotsPerAircraft = 1,
                    Days = 60, // 60 dias a 2h/dia = ~120h voadas, suficiente para vencer a de 100h
                    HoursPerDay = 2.0,
                    Seed = 10,
                    FastMode = true,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), $"sim_maint_{Guid.NewGuid():N}")
                };

                var sim = new CareerSimulator(() => new AppDbContext(options));
                var res = await sim.RunAsync(simOptions);

                var insp100 = res.Maintenance.FirstOrDefault(m => m.Service.Contains("100h"));
                Assert.NotNull(insp100);
                Assert.Equal(2500.0, insp100.Cost);
                Assert.Equal(16.0, insp100.DowntimeHours);

                // Também deve ter feito ao menos uma inspeção de 50h ($850)
                var insp50 = res.Maintenance.FirstOrDefault(m => m.Service.Contains("50h"));
                Assert.NotNull(insp50);
                Assert.Equal(850.0, insp50.Cost);

                if (Directory.Exists(simOptions.OutputDirectory)) Directory.Delete(simOptions.OutputDirectory, true);
            }
            finally
            {
                CleanupDb(tempDb);
            }
        }

        [Fact]
        public async Task CareerSim_TaxasDoDestino_IgualGroundServiceCalculator()
        {
            // Critério: taxas cobradas no voo devem ser rigorosamente iguais ao cálculo de GroundServiceCalculator
            var (options, tempDb) = await CreateTestEnvironmentAsync();

            try
            {
                var simOptions = new CareerSimulationOptions
                {
                    AircraftModels = new[] { "Cessna C152" },
                    BotsPerAircraft = 1,
                    Days = 3,
                    HoursPerDay = 2.0,
                    Seed = 99,
                    FastMode = true,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), $"sim_fees_{Guid.NewGuid():N}")
                };

                var sim = new CareerSimulator(() => new AppDbContext(options));
                var res = await sim.RunAsync(simOptions);

                Assert.NotEmpty(res.Flights);
                var f = res.Flights.First();

                double mtow = AircraftWeightSpecs.GetMtow("Cessna C152", null);
                var feeCtx = new FeeContext(f.DestTier, f.Destination, mtow, f.Passengers, hasMission: true, parkedGameHours: 0.0);
                var expectedFees = GroundServiceCalculator.Calculate(feeCtx);

                double expectedLanding = expectedFees.FirstOrDefault(x => x.Code == "LANDING")?.Total ?? 0.0;
                double expectedPax = expectedFees.FirstOrDefault(x => x.Code == "PAX_FEE")?.Total ?? 0.0;
                double expectedHandling = expectedFees.FirstOrDefault(x => x.Code == "HANDLING")?.Total ?? 0.0;
                double expectedNav = expectedFees.FirstOrDefault(x => x.Code == "NAV_TERMINAL")?.Total ?? 0.0;
                double expectedTotal = expectedLanding + expectedPax + expectedHandling + expectedNav;

                Assert.Equal(expectedLanding, f.LandingFee, 2);
                Assert.Equal(expectedPax, f.PassengerFee, 2);
                Assert.Equal(expectedHandling, f.HandlingFee, 2);
                Assert.Equal(expectedNav, f.NavFee, 2);
                Assert.Equal(expectedTotal, f.TotalFees, 2);

                if (Directory.Exists(simOptions.OutputDirectory)) Directory.Delete(simOptions.OutputDirectory, true);
            }
            finally
            {
                CleanupDb(tempDb);
            }
        }

        [Fact]
        public async Task CareerSim_AviaoParado_NaoVoa()
        {
            // Critério: quando há downtime / parada, o avião não realiza voos no dia indisponível
            var (options, tempDb) = await CreateTestEnvironmentAsync();

            try
            {
                var simOptions = new CareerSimulationOptions
                {
                    AircraftModels = new[] { "Cessna C152" },
                    BotsPerAircraft = 1,
                    Days = 60,
                    HoursPerDay = 2.0,
                    Seed = 777,
                    FastMode = true,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), $"sim_down_{Guid.NewGuid():N}")
                };

                var sim = new CareerSimulator(() => new AppDbContext(options));
                var res = await sim.RunAsync(simOptions);

                // Em dias marcados como IsAvailable == false, horas voadas e voos devem ser exatamente 0
                var unavailableDays = res.Daily.Where(d => !d.IsAvailable).ToList();
                foreach (var day in unavailableDays)
                {
                    Assert.Equal(0.0, day.HoursFlown);
                    Assert.Equal(0, day.Flights);
                    Assert.Equal(0.0, day.Revenue);
                }

                if (Directory.Exists(simOptions.OutputDirectory)) Directory.Delete(simOptions.OutputDirectory, true);
            }
            finally
            {
                CleanupDb(tempDb);
            }
        }

        private static async Task<(DbContextOptions<AppDbContext> Options, string TempDb)> CreateTestEnvironmentAsync()
        {
            string tempDb = Path.Combine(Path.GetTempPath(), $"career_test_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={tempDb}")
                .Options;

            using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var c152 = new AircraftType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Model = "Cessna C152",
                    Type = "Monomotor Pistão",
                    FuelType = "Avgas",
                    PurchasePrice = 85000.0,
                    FuelTankGallons = 26.0,
                    FuelConsumptionGPH = 6.0,
                    CruiseSpeedKts = 105,
                    MaxPayloadLbs = 450,
                    MaxPax = 1,
                    RangeNM = 400,
                    MinRunwayLength = 1500
                };
                db.AircraftTypes.Add(c152);

                var apOrigin = new Airport
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ICAO = "SBRP",
                    Name = "Ribeirao Preto",
                    Tier = 2,
                    Latitude = -21.1364,
                    Longitude = -47.7767
                };
                var apDest1 = new Airport
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    ICAO = "SBAU",
                    Name = "Araraquara",
                    Tier = 2,
                    Latitude = -21.8044,
                    Longitude = -48.1403
                };
                var apDest2 = new Airport
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    ICAO = "SBDN",
                    Name = "Presidente Prudente",
                    Tier = 2,
                    Latitude = -22.1753,
                    Longitude = -51.4244
                };
                db.Airports.AddRange(apOrigin, apDest1, apDest2);
                await db.SaveChangesAsync();
            }

            return (options, tempDb);
        }

        private static void CleanupDb(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Ignora falha de exclusão
            }
        }
    }
}
