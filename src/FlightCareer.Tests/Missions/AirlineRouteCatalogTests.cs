using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class AirlineRouteCatalogTests
    {
        [Fact]
        public void LoadFromJson_ParesNosDoisSentidos_HasRouteSimetrico()
        {
            string json = @"{
                ""version"": 1,
                ""routes"": {
                    ""SBGR"": { ""SBSV"": 3 },
                    ""SBSV"": { ""SBGR"": 3 }
                }
            }";
            var catalog = AirlineRouteCatalog.LoadFromJson(json);
            Assert.True(catalog.HasRoute("sbgr", "SBSV"));
            Assert.True(catalog.HasRoute("SBSV", "SBGR"));
            Assert.Equal(3, catalog.Airlines("SBGR", "SBSV"));
            Assert.Equal(3, catalog.Airlines("sbsv", "sbgr"));
            Assert.False(catalog.HasRoute("SBGR", "SBPA"));
            Assert.Equal(0, catalog.Airlines("SBGR", "SBPA"));
            Assert.Equal(1, catalog.PairCount);
        }

        [Fact]
        public void LoadFromJson_JsonInvalido_CatalogoVazio()
        {
            var catalog = AirlineRouteCatalog.LoadFromJson("{x");
            Assert.Equal(0, catalog.PairCount);
            Assert.False(catalog.HasRoute("SBGR", "SBSV"));
            Assert.Equal(0, catalog.Airlines("SBGR", "SBSV"));
        }

        [Fact]
        public void RealRouteMultiplier_SemValorNoCatalogo_Retorna1()
        {
            var m = new MissionDefinition { Id = "teste", Destination = new MissionDestinationRule { RealRouteMultiplier = 0.0 } };
            var routes = new AirlineRouteCatalog(new Dictionary<string, IReadOnlyDictionary<string, int>>
            {
                ["SBGR"] = new Dictionary<string, int> { ["SBSV"] = 2 }
            });
            double mult = MissionWeighting.RealRouteMultiplier(m, routes, "SBGR", "SBSV");
            Assert.Equal(1.0, mult);
        }

        [Fact]
        public void RealRouteMultiplier_RotaReal_RetornaValor()
        {
            var m = new MissionDefinition { Id = "teste", Destination = new MissionDestinationRule { RealRouteMultiplier = 6.0 } };
            var routes = new AirlineRouteCatalog(new Dictionary<string, IReadOnlyDictionary<string, int>>
            {
                ["SBGR"] = new Dictionary<string, int> { ["SBSV"] = 2 }
            });
            double mult = MissionWeighting.RealRouteMultiplier(m, routes, "SBGR", "SBSV");
            Assert.Equal(6.0, mult);
        }

        [Fact]
        public void RealRouteMultiplier_SemRota_Retorna1()
        {
            var m = new MissionDefinition { Id = "teste", Destination = new MissionDestinationRule { RealRouteMultiplier = 6.0 } };
            var routes = new AirlineRouteCatalog(new Dictionary<string, IReadOnlyDictionary<string, int>>
            {
                ["SBGR"] = new Dictionary<string, int> { ["SBSV"] = 2 }
            });
            double mult = MissionWeighting.RealRouteMultiplier(m, routes, "SBGR", "SBPA");
            Assert.Equal(1.0, mult);
        }

        [Fact]
        public void RealRouteMultiplier_CatalogoNulo_Retorna1()
        {
            var m = new MissionDefinition { Id = "teste", Destination = new MissionDestinationRule { RealRouteMultiplier = 6.0 } };
            double mult = MissionWeighting.RealRouteMultiplier(m, null, "SBGR", "SBSV");
            Assert.Equal(1.0, mult);
        }

        [Fact]
        public void DestinationChoiceGroup_SemMultiplicadorExtra_PesosIguaisAoAtual()
        {
            var candidates = Enumerable.Range(1, 10)
                .Select(i => new AirportCandidate(new Airport { ICAO = $"SB0{i}", Tier = 2, Latitude = -20 - i, Longitude = -45 - i }, null))
                .ToList();

            var def = new MissionDefinition
            {
                Id = "teste_equiv",
                Destination = new MissionDestinationRule { Profiles = new Dictionary<string, double> { ["linha"] = 3.0 } }
            };

            var group1 = new DestinationChoiceGroup(def, candidates, _ => new HashSet<string> { "linha" });
            var group2 = new DestinationChoiceGroup(def, candidates, _ => new HashSet<string> { "linha" }, null);

            var rng1 = new Random(12345);
            var rng2 = new Random(12345);

            for (int i = 0; i < 1000; i++)
            {
                var pick1 = group1.Pick(rng1);
                var pick2 = group2.Pick(rng2);
                Assert.Same(pick1, pick2);
            }
        }

        [Fact]
        public void Sorteio_RotaRealMultiplicador6_PreferenciaMedida()
        {
            var candidates = Enumerable.Range(1, 10)
                .Select(i => new AirportCandidate(new Airport { ICAO = $"SB{i:D2}", Tier = 1, Latitude = -20 - i, Longitude = -45 - i }, null))
                .ToList();

            var def = new MissionDefinition
            {
                Id = "troncal_teste",
                Destination = new MissionDestinationRule
                {
                    RealRouteMultiplier = 6.0
                }
            };

            var routes = new AirlineRouteCatalog(new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
            {
                ["SBGR"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SB01"] = 3,
                    ["SB02"] = 2
                }
            });

            var group = new DestinationChoiceGroup(
                def,
                candidates,
                _ => new HashSet<string>(),
                c => MissionWeighting.RealRouteMultiplier(def, routes, "SBGR", c.Airport.ICAO));

            var rng = new Random(42);
            int countRotasReais = 0;
            const int totalRolls = 5000;

            for (int i = 0; i < totalRolls; i++)
            {
                var picked = group.Pick(rng);
                if (picked!.Airport.ICAO == "SB01" || picked.Airport.ICAO == "SB02")
                {
                    countRotasReais++;
                }
            }

            double fraction = (double)countRotasReais / totalRolls;
            Assert.InRange(fraction, 0.52, 0.68);
        }

        [Fact]
        public void DadosReais_AirlineRoutesJson_CarregaEContemRotasConhecidas()
        {
            var catalog = AirlineRouteCatalog.LoadDefault();
            Assert.True(catalog.PairCount > 10000, $"Esperado PairCount > 10000, obtido {catalog.PairCount}");
            Assert.True(catalog.HasRoute("SBGR", "KMIA"));
            Assert.True(catalog.HasRoute("SBGR", "SBSV"));
            Assert.True(catalog.HasRoute("kmia", "sbgr"));
            Assert.True(catalog.HasRoute("sbsv", "sbgr"));
        }

        [Fact]
        public void CreateMissionObjectSync_RotaReal_DescricaoComNota()
        {
            var origin = new Airport { ICAO = "SBGR", Latitude = -23.4, Longitude = -46.4, Tier = 1 };
            var dest = new Airport { ICAO = "SBSV", Latitude = -12.9, Longitude = -38.3, Tier = 2 };
            var def = MissionCatalog.Shared.Missions["troncal_domestico"];
            var mission = MissionGenerator.CreateMissionObjectSync(
                origin, dest, 780.0, def, 150, 2000.0, 1000.0, new Random(42), DateTime.UtcNow, realRoute: true);

            Assert.Contains("Rota regular com demanda consolidada.", mission.Description);
        }

        [Fact]
        public void CreateMissionObjectSync_SemRota_SemNota()
        {
            var origin = new Airport { ICAO = "SBGR", Latitude = -23.4, Longitude = -46.4, Tier = 1 };
            var dest = new Airport { ICAO = "SBSV", Latitude = -12.9, Longitude = -38.3, Tier = 2 };
            var def = MissionCatalog.Shared.Missions["troncal_domestico"];
            var mission = MissionGenerator.CreateMissionObjectSync(
                origin, dest, 780.0, def, 150, 2000.0, 1000.0, new Random(42), DateTime.UtcNow, realRoute: false);

            Assert.DoesNotContain("Rota regular", mission.Description);
            Assert.EndsWith("lbs de carga.", mission.Description);
        }
    }
}
