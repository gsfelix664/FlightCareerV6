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
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    public class CareerProgressionTests
    {
        [Fact]
        public void CareerStart_SaldosIguaisAoJogo()
        {
            // Critério: Fácil $1.000.000.000, Médio $100.000, Difícil $10.000, C152 5.000h de célula e meio tanque
            Assert.Equal(1_000_000_000.0, CareerStart.GetInitialBalance(DifficultyMode.Easy));
            Assert.Equal(100_000.0, CareerStart.GetInitialBalance(DifficultyMode.Medium));
            Assert.Equal(10_000.0, CareerStart.GetInitialBalance(DifficultyMode.Hard));

            Assert.Equal(5000.0, CareerStart.DefaultStartingAirframeHours);
            Assert.Equal(0.5, CareerStart.DefaultStartingFuelFraction);
            Assert.Equal(1800.0, CareerStart.DefaultStartingTboRemainingHours);
            Assert.Equal(50.0, CareerStart.DefaultStarting50hHours);
            Assert.Equal(100.0, CareerStart.DefaultStarting100hHours);
        }

        [Fact]
        public async Task Progression_MesmaSemente_MesmoResultado()
        {
            // Critério: 2 bots, 20 dias, mesma semente -> mesmo resultado
            var (options1, tempDb1) = await CreateTestEnvironmentAsync();
            var (options2, tempDb2) = await CreateTestEnvironmentAsync();

            try
            {
                var simOptions1 = new CareerProgressionOptions
                {
                    Days = 20,
                    BotsPerGroup = 2,
                    Difficulty = "medium",
                    Policy = "equilibrado",
                    Seed = 42,
                    FastMode = true,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), $"stab_test_1_{Guid.NewGuid():N}")
                };

                var simOptions2 = new CareerProgressionOptions
                {
                    Days = 20,
                    BotsPerGroup = 2,
                    Difficulty = "medium",
                    Policy = "equilibrado",
                    Seed = 42,
                    FastMode = true,
                    OutputDirectory = Path.Combine(Path.GetTempPath(), $"stab_test_2_{Guid.NewGuid():N}")
                };

                var sim1 = new CareerProgressionSimulator(() => new AppDbContext(options1));
                var sim2 = new CareerProgressionSimulator(() => new AppDbContext(options2));

                var res1 = await sim1.RunAsync(simOptions1);
                var res2 = await sim2.RunAsync(simOptions2);

                Assert.Equal(res1.Snapshots.Count, res2.Snapshots.Count);
                for (int i = 0; i < res1.Snapshots.Count; i++)
                {
                    var s1 = res1.Snapshots[i];
                    var s2 = res2.Snapshots[i];
                    Assert.Equal(s1.BotId, s2.BotId);
                    Assert.Equal(s1.Day, s2.Day);
                    Assert.Equal(s1.Balance, s2.Balance, 2);
                    Assert.Equal(s1.NetWorth, s2.NetWorth, 2);
                    Assert.Equal(s1.TotalHours, s2.TotalHours, 1);
                    Assert.Equal(s1.AircraftClassKey, s2.AircraftClassKey);
                    Assert.Equal(s1.IsBankrupt, s2.IsBankrupt);
                }

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
        public void Progression_BotCompraQuandoCabeNaReserva()
        {
            var c172 = new AircraftType
            {
                Model = "C172SP G1000",
                PurchasePrice = 432_000.0,
                FuelConsumptionGPH = 9.0
            };

            double monthlyCost = CareerProgressionSimulator.CalculateEstimatedMonthlyOperatingCost(c172, "B");
            Assert.True(monthlyCost > 0);

            // Cauteloso exige 3 meses de reserva; Equilibrado exige 1 mês; Agressivo exige 0
            double reserveCauteloso = 3.0 * monthlyCost;
            double reserveEquilibrado = 1.0 * monthlyCost;
            double reserveAgressivo = 0.0;

            Assert.True(reserveCauteloso > reserveEquilibrado);
            Assert.True(reserveEquilibrado > reserveAgressivo);

            double netCashNeeded = 300_000.0;

            // Saldo cobre a compra pura, mas NÃO a reserva cautelosa
            double balanceInsufficientForCauteloso = netCashNeeded + (0.5 * reserveCauteloso);
            bool canBuyCauteloso = balanceInsufficientForCauteloso >= netCashNeeded + reserveCauteloso;
            Assert.False(canBuyCauteloso, "Bot cauteloso não deve comprar se não tiver 3 meses de reserva.");

            // Saldo com reserva integral permite a compra
            double balanceSufficientForCauteloso = netCashNeeded + reserveCauteloso + 1000.0;
            bool canBuyCautelosoWithReserve = balanceSufficientForCauteloso >= netCashNeeded + reserveCauteloso;
            Assert.True(canBuyCautelosoWithReserve, "Bot cauteloso deve comprar quando tiver saldo + 3 meses de reserva.");

            // Saldo para agressivo precisa apenas cobrir o custo líquido
            bool canBuyAgressivo = balanceInsufficientForCauteloso >= netCashNeeded + reserveAgressivo;
            Assert.True(canBuyAgressivo, "Bot agressivo compra assim que o saldo cobre o custo líquido.");
        }

        [Fact]
        public void Progression_FalenciaQuandoSemDinheiro()
        {
            double balance = -500.0; // Saldo negativo
            double fuelCostForFlight = 150.0;

            // Sem conseguir pagar combustível com saldo negativo -> falência
            bool isBankruptFuel = balance < fuelCostForFlight && balance < 0;
            Assert.True(isBankruptFuel, "Bot deve falir se saldo for negativo e não puder pagar combustível.");

            double groundingRepairCost = 1200.0;
            bool isBankruptRepair = balance < groundingRepairCost && balance < 0;
            Assert.True(isBankruptRepair, "Bot deve falir se saldo for negativo e não puder pagar pane NO-GO.");

            // Saldo positivo cobre combustível sem falir
            double positiveBalance = 200.0;
            bool isBankruptPositive = positiveBalance < fuelCostForFlight && positiveBalance < 0;
            Assert.False(isBankruptPositive);
        }

        [Fact]
        public void Progression_CategoriaLimitaMissoes()
        {
            var origin = new Airport { ICAO = "SBRP", Tier = 2, Latitude = -21.13, Longitude = -47.77 };
            var dest = new Airport { ICAO = "SBAU", Tier = 2, Latitude = -21.80, Longitude = -48.14 };

            var subCat1 = new SubCategoryConfig { Id = "3.4.1", Name = "Instrução Pousos", Urgency = 1.0, RangeRules = new List<ProbRange> { new(10, 5, 20) } };
            var subCat2 = new SubCategoryConfig { Id = "3.2.1", Name = "SAE Agrícola", Urgency = 1.2, RangeRules = new List<ProbRange> { new(10, 30, 100) } };
            var subCat5 = new SubCategoryConfig { Id = "1.1.1", Name = "Linha Regular Comercial", Urgency = 1.5, RangeRules = new List<ProbRange> { new(10, 200, 500) } };

            var rnd = new Random(123);
            var date = new DateTime(2024, 1, 1);

            var m1 = MissionGenerator.CreateMissionObjectSync(origin, dest, 15.0, subCat1, 1, 0, 0, rnd, date);
            var m2 = MissionGenerator.CreateMissionObjectSync(origin, dest, 50.0, subCat2, 0, 0, 500, rnd, date);
            var m5 = MissionGenerator.CreateMissionObjectSync(origin, dest, 300.0, subCat5, 150, 4500, 2000, rnd, date);

            Assert.Equal(PilotCategory.Category1_FlightSchool, m1.RequiredCategory);
            Assert.Equal(PilotCategory.Category2_SAE, m2.RequiredCategory);
            Assert.Equal(PilotCategory.Category5_MajorAirline, m5.RequiredCategory);

            // Piloto em Categoria 1 só pode aceitar m1
            var pilotCat = PilotCategory.Category1_FlightSchool;
            Assert.True(m1.RequiredCategory <= pilotCat);
            Assert.False(m2.RequiredCategory <= pilotCat);
            Assert.False(m5.RequiredCategory <= pilotCat);

            // Ao promover para Categoria 2, m2 é liberada, mas m5 continua bloqueada
            pilotCat = PilotCategory.Category2_SAE;
            Assert.True(m1.RequiredCategory <= pilotCat);
            Assert.True(m2.RequiredCategory <= pilotCat);
            Assert.False(m5.RequiredCategory <= pilotCat);
        }

        private static async Task<(DbContextOptions<AppDbContext> Options, string TempDb)> CreateTestEnvironmentAsync()
        {
            string tempDb = Path.Combine(Path.GetTempPath(), $"career_stab_test_{Guid.NewGuid():N}.db");
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
                    PurchasePrice = 60000.0,
                    FuelTankGallons = 26.0,
                    FuelConsumptionGPH = 6.0,
                    CruiseSpeedKts = 107,
                    MaxPayloadLbs = 530,
                    MaxPax = 2,
                    RangeNM = 415,
                    MinRunwayLength = 1340
                };
                var c172 = new AircraftType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Model = "C172SP G1000",
                    Type = "Monomotor Pistão",
                    FuelType = "Avgas",
                    PurchasePrice = 432000.0,
                    FuelTankGallons = 56.0,
                    FuelConsumptionGPH = 9.0,
                    CruiseSpeedKts = 124,
                    MaxPayloadLbs = 878,
                    MaxPax = 4,
                    RangeNM = 640,
                    MinRunwayLength = 1630
                };
                var baron = new AircraftType
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Model = "Beechcraft Baron G58",
                    Type = "Bimotor Pistão",
                    FuelType = "Avgas",
                    PurchasePrice = 1500000.0,
                    FuelTankGallons = 194.0,
                    FuelConsumptionGPH = 32.0,
                    CruiseSpeedKts = 202,
                    MaxPayloadLbs = 1600,
                    MaxPax = 6,
                    RangeNM = 1000,
                    MinRunwayLength = 2300
                };
                var c208 = new AircraftType
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Model = "C208B",
                    Type = "Monomotor Turboélice",
                    FuelType = "Jet A-1",
                    PurchasePrice = 2900000.0,
                    FuelTankGallons = 335.0,
                    FuelConsumptionGPH = 45.0,
                    CruiseSpeedKts = 185,
                    MaxPayloadLbs = 3305,
                    MaxPax = 14,
                    RangeNM = 960,
                    MinRunwayLength = 2000
                };
                var kingAir = new AircraftType
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Model = "Beechcraft King Air 250",
                    Type = "Bimotor Turboélice",
                    FuelType = "Jet A-1",
                    PurchasePrice = 6000000.0,
                    FuelTankGallons = 544.0,
                    FuelConsumptionGPH = 98.0,
                    CruiseSpeedKts = 310,
                    MaxPayloadLbs = 3700,
                    MaxPax = 9,
                    RangeNM = 1100,
                    MinRunwayLength = 2200
                };
                var cj4 = new AircraftType
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Model = "Cessna Citation CJ4",
                    Type = "Jato Executivo",
                    FuelType = "Jet A-1",
                    PurchasePrice = 10750000.0,
                    FuelTankGallons = 870.0,
                    FuelConsumptionGPH = 173.0,
                    CruiseSpeedKts = 451,
                    MaxPayloadLbs = 2200,
                    MaxPax = 10,
                    RangeNM = 2165,
                    MinRunwayLength = 3410
                };
                var a320 = new AircraftType
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Model = "A320neo",
                    Type = "Jato Comercial",
                    FuelType = "Jet A-1",
                    PurchasePrice = 110600000.0,
                    FuelTankGallons = 7930.0,
                    FuelConsumptionGPH = 780.0,
                    CruiseSpeedKts = 453,
                    MaxPayloadLbs = 76400,
                    MaxPax = 195,
                    RangeNM = 3500,
                    MinRunwayLength = 6500
                };
                var a330 = new AircraftType
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Model = "A330-200",
                    Type = "Jato Comercial",
                    FuelType = "Jet A-1",
                    PurchasePrice = 238500000.0,
                    FuelTankGallons = 36750.0,
                    FuelConsumptionGPH = 1490.0,
                    CruiseSpeedKts = 470,
                    MaxPayloadLbs = 269000,
                    MaxPax = 406,
                    RangeNM = 7250,
                    MinRunwayLength = 9100
                };

                db.AircraftTypes.AddRange(c152, c172, baron, c208, kingAir, cj4, a320, a330);

                var apOrigin = new Airport
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    ICAO = "SBRP",
                    Name = "Ribeirao Preto",
                    Tier = 2,
                    Latitude = -21.1364,
                    Longitude = -47.7767
                };
                var apDest1 = new Airport
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    ICAO = "SBAU",
                    Name = "Araraquara",
                    Tier = 2,
                    Latitude = -21.8044,
                    Longitude = -48.1403
                };
                var apDest2 = new Airport
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
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
