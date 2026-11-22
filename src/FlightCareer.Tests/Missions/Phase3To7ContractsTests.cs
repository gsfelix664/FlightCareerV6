using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Career;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Xp;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class Phase3To7ContractsTests
    {
        [Fact]
        public void CareerPhases_TodosSubtiposDeMissionRulesPossuemFaseDefinida()
        {
            var catalog = CareerPhaseCatalog.Instance;
            var subIdMap = catalog.SubIdToPhase;

            var missing = new List<string>();

            foreach (var kvp in MissionRules.Rules)
            {
                int tier = kvp.Key;
                var tierConfig = kvp.Value;
                foreach (var cat in tierConfig.Categories)
                {
                    foreach (var sub in cat.SubCategories)
                    {
                        if (!subIdMap.ContainsKey(sub.Id))
                        {
                            missing.Add($"Tier {tier} › Categoria {cat.Id} › Subtipo {sub.Id} ({sub.Name})");
                        }
                        else
                        {
                            var phase = catalog.ResolvePhase(sub.Id);
                            Assert.True((int)phase >= 1 && (int)phase <= 7, $"Fase inválida para subtipo {sub.Id}: {phase}");
                        }
                    }
                }
            }

            Assert.Empty(missing);
        }

        [Fact]
        public void Pagamento_FamiliasNovasEEscalaDeReferencia()
        {
            var payTable = MissionPayTable.Shared;

            // 1. Carga Frágil: escala igual à da AOG (valor calibrado em mission_pay.json)
            var famAog = payTable.Resolve("3.2.1");
            Assert.Equal("aog", famAog.Id);
            Assert.True(famAog.Scale > 0);

            var famFragil1 = payTable.Resolve("1.7.2");
            var famFragil2 = payTable.Resolve("2.7.2");
            var famFragil3 = payTable.Resolve("3.7.1");

            Assert.Equal("carga_fragil", famFragil1.Id);
            Assert.Equal("carga_fragil", famFragil2.Id);
            Assert.Equal("carga_fragil", famFragil3.Id);
            Assert.Equal(famAog.Scale, famFragil1.Scale);

            // 2. Carga Global: escala igual à da Linha (valor calibrado em mission_pay.json)
            var famLinha = payTable.Resolve("1.1.1");
            Assert.Equal("linha", famLinha.Id);
            Assert.True(famLinha.Scale > 0);

            var famGlobal = payTable.Resolve("1.7.8");
            Assert.Equal("carga_global", famGlobal.Id);
            Assert.Equal(famLinha.Scale, famGlobal.Scale);

            // 3. Subtipos novos mapeados para famílias existentes
            Assert.Equal("taxi_aereo", payTable.Resolve("1.7.1").Id);
            Assert.Equal("taxi_aereo", payTable.Resolve("2.7.1").Id);
            Assert.Equal("taxi_aereo", payTable.Resolve("1.7.4").Id);
            Assert.Equal("taxi_aereo", payTable.Resolve("2.7.4").Id);

            Assert.Equal("medevac", payTable.Resolve("1.7.3").Id);
            Assert.Equal("medevac", payTable.Resolve("2.7.3").Id);
            Assert.Equal("medevac", payTable.Resolve("3.7.2").Id);

            Assert.Equal("linha", payTable.Resolve("1.7.5").Id);
            Assert.Equal("linha", payTable.Resolve("1.7.6").Id);
            Assert.Equal("linha", payTable.Resolve("1.7.7").Id);
        }

        [Fact]
        public void Xp_MultiplicadoresPorTipoConfigurados()
        {
            var xpRules = XpRules.Default;

            Assert.Equal(1.35, xpRules.TypeMultipliers["carga_fragil"]);
            Assert.Equal(1.35, xpRules.TypeMultipliers["carga_global"]);
            Assert.Equal(1.00, xpRules.TypeMultipliers["1.7.6"]);

            // Resolução via XpCalculator
            Assert.Equal(1.35, XpCalculator.ResolveTypeMultiplier("carga_fragil", xpRules));
            Assert.Equal(1.35, XpCalculator.ResolveTypeMultiplier("carga_global", xpRules));
            Assert.Equal(1.00, XpCalculator.ResolveTypeMultiplier("1.7.6", xpRules));
            Assert.Equal(1.35, XpCalculator.ResolveTypeMultiplier("1.7.2", xpRules)); // Carga frágil
            Assert.Equal(1.35, XpCalculator.ResolveTypeMultiplier("1.7.8", xpRules)); // Carga global
            Assert.Equal(1.25, XpCalculator.ResolveTypeMultiplier("1.7.1", xpRules)); // Fretamento executivo -> taxi_aereo
            Assert.Equal(1.50, XpCalculator.ResolveTypeMultiplier("1.7.3", xpRules)); // MEDEVAC dinâmico -> medevac
            Assert.Equal(1.25, XpCalculator.ResolveTypeMultiplier("1.7.4", xpRules)); // VIP internacional -> taxi_aereo
            Assert.Equal(1.25, XpCalculator.ResolveTypeMultiplier("1.7.5", xpRules)); // Ponte aérea -> linha
            Assert.Equal(1.25, XpCalculator.ResolveTypeMultiplier("1.7.7", xpRules)); // Long-haul -> linha
        }

        [Fact]
        public void Gerador_FretamentoExecutivo_MesmoPais_Tier3Ou4_2a6Pax_100a400NM()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var airports = new List<Airport>
            {
                origin,
                new Airport { ICAO = "SBCF", Country = "Brazil", Tier = 1, Latitude = -19.6244, Longitude = -43.9719 }, // Tier 1 (não deve escolher)
                new Airport { ICAO = "SBRF", Country = "Brazil", Tier = 2, Latitude = -21.1364, Longitude = -47.7744 }, // Tier 2 (não deve escolher)
                new Airport { ICAO = "SDVG", Country = "Brazil", Tier = 3, Latitude = -21.5878, Longitude = -45.4736 }, // Tier 3, ~120 NM (válido)
                new Airport { ICAO = "SWCA", Country = "Brazil", Tier = 4, Latitude = -22.1000, Longitude = -47.0000 }, // Tier 4, ~90 NM (válido)
                new Airport { ICAO = "SAEZ", Country = "Argentina", Tier = 3, Latitude = -34.8222, Longitude = -58.5358 } // Outro país (não deve escolher)
            };

            var sub = MissionRules.Rules[1].Categories.First(c => c.Id == "1.7").SubCategories.First(s => s.Id == "1.7.1");

            for (int seed = 1; seed <= 20; seed++)
            {
                var rnd = new Random(seed);
                var m = MissionGenerator.CreateMissionObjectSync(
                    origin,
                    airports[3], // SDVG
                    150.0,
                    sub,
                    rnd.Next(2, 7),
                    18.0,
                    0.0,
                    rnd,
                    DateTime.UtcNow);

                Assert.NotNull(m);
                Assert.Equal(CareerPhase.Phase3_AirTaxi, m.RequiredPhase);
                Assert.InRange(m.PassengerCount, 2, 6);
                Assert.InRange(m.DistanceNM, 100, 400);
            }

            // Teste com seleção in-memory
            var mGen = MissionGenerator.GenerateMissionsInMemory(origin, airports, null, new Random(42), DateTime.UtcNow, countOverride: 20);
            var fretamentos = mGen.Where(m => m.SubId == "1.7.1").ToList();
            foreach (var m in fretamentos)
            {
                var dest = airports.First(a => a.ICAO == m.DestinationICAO);
                Assert.Equal("Brazil", dest.Country);
                Assert.True(dest.Tier == 3 || dest.Tier == 4, $"Destino {dest.ICAO} deve ser Tier 3 ou 4");
                Assert.InRange(m.PassengerCount, 2, 6);
                Assert.InRange(m.DistanceNM, 100, 400);
                Assert.Equal(CareerPhase.Phase3_AirTaxi, m.RequiredPhase);
            }
        }

        [Fact]
        public void Gerador_CargaFragilExpressa_100a800Lb_80a400NM_Urgente_ExpCurta()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var dest = new Airport { ICAO = "SBDN", Country = "Brazil", Tier = 3, Latitude = -22.1969, Longitude = -49.9261 };
            var sub = MissionRules.Rules[1].Categories.First(c => c.Id == "1.7").SubCategories.First(s => s.Id == "1.7.2");

            var rnd = new Random(101);
            var now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
            var m = MissionGenerator.CreateMissionObjectSync(origin, dest, 200.0, sub, 0, 0, 450.0, rnd, now);

            Assert.NotNull(m);
            Assert.Equal("1.7.2", m.SubId);
            Assert.Equal(0, m.PassengerCount);
            Assert.Equal(450.0, m.CargoWeightLbs);
            Assert.Equal(1.3, m.Urgency);
            Assert.Equal(CareerPhase.Phase3_AirTaxi, m.RequiredPhase);
            Assert.InRange(m.ExpirationTime, now.AddHours(1), now.AddHours(3));
        }

        [Fact]
        public void Gerador_MedevacDinamico_PistaCurtaOuNaoPavimentada_1a2Pax_300a600Lb()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var dest = new Airport { ICAO = "SWCA", Country = "Brazil", Tier = 4, Latitude = -22.1000, Longitude = -47.0000 };
            var sub = MissionRules.Rules[1].Categories.First(c => c.Id == "1.7").SubCategories.First(s => s.Id == "1.7.3");

            var rnd = new Random(202);
            var now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
            var m = MissionGenerator.CreateMissionObjectSync(origin, dest, 120.0, sub, 2, 30.0, 400.0, rnd, now);

            Assert.NotNull(m);
            Assert.Equal("1.7.3", m.SubId);
            Assert.InRange(m.PassengerCount, 1, 2);
            Assert.InRange(m.CargoWeightLbs, 300, 600);
            Assert.Equal(1.5, m.Urgency);
            Assert.Equal(CareerPhase.Phase4_HighPerformanceAndMedevac, m.RequiredPhase);
            Assert.InRange(m.ExpirationTime, now.AddHours(1), now.AddHours(2));
        }

        [Fact]
        public void Gerador_VipInternacionalCurto_SempreOutroPais_300a900NM_2a8Pax()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var airports = new List<Airport>
            {
                origin,
                new Airport { ICAO = "SBPA", Country = "Brazil", Tier = 2, Latitude = -29.9939, Longitude = -51.1711 }, // Mesmo país, 460 NM (não deve escolher)
                new Airport { ICAO = "SGAS", Country = "Paraguay", Tier = 2, Latitude = -25.2400, Longitude = -57.5194 }, // Outro país, ~580 NM (válido)
                new Airport { ICAO = "SUMU", Country = "Uruguay", Tier = 1, Latitude = -34.8384, Longitude = -56.0308 }  // Outro país, ~850 NM (válido)
            };

            var mGen = MissionGenerator.GenerateMissionsInMemory(origin, airports, null, new Random(77), DateTime.UtcNow, countOverride: 30);
            var vips = mGen.Where(m => m.SubId == "1.7.4").ToList();

            foreach (var m in vips)
            {
                var dest = airports.First(a => a.ICAO == m.DestinationICAO);
                Assert.NotEqual("Brazil", dest.Country);
                Assert.InRange(m.DistanceNM, 300, 900);
                Assert.InRange(m.PassengerCount, 2, 8);
                Assert.Equal(CareerPhase.Phase4_HighPerformanceAndMedevac, m.RequiredPhase);
            }
        }

        [Fact]
        public void Gerador_PonteAereaTroncal_SempreHubHubMesmoPais_150a220Pax_150a700NM()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var airports = new List<Airport>
            {
                origin,
                new Airport { ICAO = "SBRJ", Country = "Brazil", Tier = 1, Latitude = -22.9105, Longitude = -43.1631 }, // Hub mesmo país, 190 NM (válido)
                new Airport { ICAO = "SBKP", Country = "Brazil", Tier = 1, Latitude = -23.0074, Longitude = -47.1345 }, // Hub mesmo país, ~45 NM (abaixo de 150 NM)
                new Airport { ICAO = "SBCF", Country = "Brazil", Tier = 1, Latitude = -19.6244, Longitude = -43.9719 }, // Hub mesmo país, 275 NM (válido)
                new Airport { ICAO = "SBPA", Country = "Brazil", Tier = 2, Latitude = -29.9939, Longitude = -51.1711 }, // Tier 2 (não deve escolher)
                new Airport { ICAO = "SAEZ", Country = "Argentina", Tier = 1, Latitude = -34.8222, Longitude = -58.5358 } // Outro país (não deve escolher)
            };

            var mGen = MissionGenerator.GenerateMissionsInMemory(origin, airports, null, new Random(99), DateTime.UtcNow, countOverride: 30);
            var pontes = mGen.Where(m => m.SubId == "1.7.5").ToList();

            foreach (var m in pontes)
            {
                var dest = airports.First(a => a.ICAO == m.DestinationICAO);
                Assert.Equal(1, dest.Tier);
                Assert.Equal("Brazil", dest.Country);
                Assert.InRange(m.DistanceNM, 150, 700);
                Assert.InRange(m.PassengerCount, 150, 220);
                Assert.Equal(CareerPhase.Phase6_DomesticAirline, m.RequiredPhase);
            }
        }

        [Fact]
        public void Gerador_CargueiroNoturno_EntreHubs_20kA60kLb_TextoVooNoturno()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var airports = new List<Airport>
            {
                origin,
                new Airport { ICAO = "SBGL", Country = "Brazil", Tier = 1, Latitude = -22.8089, Longitude = -43.2436 }, // Hub, ~185 NM
                new Airport { ICAO = "SBBR", Country = "Brazil", Tier = 1, Latitude = -15.8692, Longitude = -47.9172 }, // Hub, ~470 NM (válido)
                new Airport { ICAO = "SBSV", Country = "Brazil", Tier = 1, Latitude = -12.9086, Longitude = -38.3225 }  // Hub, ~780 NM (válido)
            };

            var mGen = MissionGenerator.GenerateMissionsInMemory(origin, airports, null, new Random(105), DateTime.UtcNow, countOverride: 30);
            var cargueiros = mGen.Where(m => m.SubId == "1.7.6").ToList();

            foreach (var m in cargueiros)
            {
                var dest = airports.First(a => a.ICAO == m.DestinationICAO);
                Assert.Equal(1, dest.Tier);
                Assert.Equal(0, m.PassengerCount);
                Assert.InRange(m.CargoWeightLbs, 20000, 60000);
                Assert.InRange(m.DistanceNM, 300, 1500);
                Assert.Contains("voo noturno", m.Description, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(CareerPhase.Phase6_DomesticAirline, m.RequiredPhase);
            }
        }

        [Fact]
        public void Gerador_LongHaulInternacional_SempreEntreHubsPaisesDiferentes_Maior2500NM_200a400Pax()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var airports = new List<Airport>
            {
                origin,
                new Airport { ICAO = "SBGL", Country = "Brazil", Tier = 1, Latitude = -22.8089, Longitude = -43.2436 },
                new Airport { ICAO = "KJFK", Country = "United States", Tier = 1, Latitude = 40.6413, Longitude = -73.7781 }, // ~4150 NM (válido)
                new Airport { ICAO = "EGLL", Country = "United Kingdom", Tier = 1, Latitude = 51.4700, Longitude = -0.4543 }   // ~5100 NM (válido)
            };

            var mGen = MissionGenerator.GenerateMissionsInMemory(origin, airports, null, new Random(250), DateTime.UtcNow, countOverride: 30);
            var longHauls = mGen.Where(m => m.SubId == "1.7.7").ToList();

            foreach (var m in longHauls)
            {
                var dest = airports.First(a => a.ICAO == m.DestinationICAO);
                Assert.Equal(1, dest.Tier);
                Assert.NotEqual("Brazil", dest.Country);
                Assert.True(m.DistanceNM > 2500, $"Distância {m.DistanceNM} deve ser superior a 2.500 NM");
                Assert.InRange(m.PassengerCount, 200, 400);
                Assert.Equal(CareerPhase.Phase7_InternationalAirline, m.RequiredPhase);
            }
        }

        [Fact]
        public void Gerador_FretamentoGlobalCargaAltoValor_Maior2000NM_30kA100kLb_Urgente()
        {
            var origin = new Airport { ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731 };
            var airports = new List<Airport>
            {
                origin,
                new Airport { ICAO = "KATL", Country = "United States", Tier = 1, Latitude = 33.6407, Longitude = -84.4277 }, // ~3650 NM
                new Airport { ICAO = "LFPG", Country = "France", Tier = 1, Latitude = 49.0097, Longitude = 2.5479 }          // ~5050 NM
            };

            var mGen = MissionGenerator.GenerateMissionsInMemory(origin, airports, null, new Random(310), DateTime.UtcNow, countOverride: 30);
            var fretamentosGlobais = mGen.Where(m => m.SubId == "1.7.8").ToList();

            foreach (var m in fretamentosGlobais)
            {
                Assert.True(m.DistanceNM > 2000, $"Distância {m.DistanceNM} deve ser superior a 2.000 NM");
                Assert.Equal(0, m.PassengerCount);
                Assert.InRange(m.CargoWeightLbs, 30000, 100000);
                Assert.Equal(1.2, m.Urgency);
                Assert.Equal(CareerPhase.Phase7_InternationalAirline, m.RequiredPhase);
            }
        }

        [Fact]
        public async Task Gerador_ViaDbContext_RespeitaFiltrosDosNovosContratos()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var origin = new Airport { Id = Guid.NewGuid(), ICAO = "SBGR", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731, IsInSimulator = true };
            var destMedevac = new Airport { Id = Guid.NewGuid(), ICAO = "SWCA", Country = "Brazil", Tier = 4, Latitude = -22.1000, Longitude = -47.0000, IsInSimulator = true };
            var destHubSameCountry = new Airport { Id = Guid.NewGuid(), ICAO = "SBRJ", Country = "Brazil", Tier = 1, Latitude = -22.9105, Longitude = -43.1631, IsInSimulator = true };
            var destVipOtherCountry = new Airport { Id = Guid.NewGuid(), ICAO = "SGAS", Country = "Paraguay", Tier = 2, Latitude = -25.2400, Longitude = -57.5194, IsInSimulator = true };
            var destLongHaul = new Airport { Id = Guid.NewGuid(), ICAO = "KJFK", Country = "United States", Tier = 1, Latitude = 40.6413, Longitude = -73.7781, IsInSimulator = true };

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                await db.Airports.AddRangeAsync(origin, destMedevac, destHubSameCountry, destVipOtherCountry, destLongHaul);
                await db.Runways.AddRangeAsync(
                    new Runway { Id = Guid.NewGuid(), AirportIdent = "SBGR", LengthFt = 12000, Surface = "Asphalt" },
                    new Runway { Id = Guid.NewGuid(), AirportIdent = "SWCA", LengthFt = 3200, Surface = "Grass" }, // <= 4000 ft e unpaved
                    new Runway { Id = Guid.NewGuid(), AirportIdent = "SBRJ", LengthFt = 4300, Surface = "Asphalt" },
                    new Runway { Id = Guid.NewGuid(), AirportIdent = "SGAS", LengthFt = 6000, Surface = "Asphalt" },
                    new Runway { Id = Guid.NewGuid(), AirportIdent = "KJFK", LengthFt = 14000, Surface = "Concrete" }
                );
                await db.SaveChangesAsync();

                var gen = new MissionGenerator(db, null, new Random(42));
                var missions = (await gen.GenerateMissionsAsync(origin, DateTime.UtcNow)).ToList();

                Assert.NotEmpty(missions);

                // VIP Internacional (1.7.4) nunca deve ter como destino aeroporto do mesmo país
                var vips = missions.Where(m => m.SubId == "1.7.4").ToList();
                foreach (var v in vips)
                {
                    Assert.Equal("SGAS", v.DestinationICAO);
                }

                // Ponte Aérea (1.7.5) sempre hub do mesmo país
                var pontes = missions.Where(m => m.SubId == "1.7.5").ToList();
                foreach (var p in pontes)
                {
                    Assert.Equal("SBRJ", p.DestinationICAO);
                }

                // Long-Haul (1.7.7) sempre hub de outro país
                var longHauls = missions.Where(m => m.SubId == "1.7.7").ToList();
                foreach (var lh in longHauls)
                {
                    Assert.Equal("KJFK", lh.DestinationICAO);
                }
            }
        }
    }
}
