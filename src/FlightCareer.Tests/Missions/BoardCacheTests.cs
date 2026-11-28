using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class BoardCacheTests
    {
        private static (List<Airport> Airports, List<Runway> Runways) CreateSyntheticData()
        {
            var airports = new List<Airport>
            {
                new() { ICAO = "SBGR", Name = "Guarulhos", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731, ElevationFt = 2461 },
                new() { ICAO = "SBKP", Name = "Viracopos", Country = "Brazil", Tier = 2, Latitude = -23.0074, Longitude = -47.1345, ElevationFt = 2170 },
                new() { ICAO = "SBMT", Name = "Campo de Marte", Country = "Brazil", Tier = 2, Latitude = -23.5092, Longitude = -46.6378, ElevationFt = 2369 },
                new() { ICAO = "SDCO", Name = "Sorocaba", Country = "Brazil", Tier = 3, Latitude = -23.4808, Longitude = -47.4897, ElevationFt = 2077 },
                new() { ICAO = "SWCA", Name = "Fazenda Cachoeira", Country = "Brazil", Tier = 4, Latitude = -22.1000, Longitude = -47.0000, ElevationFt = 2100 },
                new() { ICAO = "LFPG", Name = "Charles de Gaulle", Country = "France", Tier = 1, Latitude = 49.0097, Longitude = 2.5479, ElevationFt = 392 },
                new() { ICAO = "LFPO", Name = "Orly", Country = "France", Tier = 2, Latitude = 48.7233, Longitude = 2.3794, ElevationFt = 291 }
            };

            var runways = new List<Runway>
            {
                new() { AirportIdent = "SBGR", LengthFt = 12000, WidthFt = 150, Surface = "Asphalt", IsLighted = true },
                new() { AirportIdent = "SBKP", LengthFt = 10000, WidthFt = 150, Surface = "Asphalt", IsLighted = true },
                new() { AirportIdent = "SBMT", LengthFt = 5000, WidthFt = 100, Surface = "Asphalt", IsLighted = true },
                new() { AirportIdent = "SDCO", LengthFt = 4500, WidthFt = 98, Surface = "Asphalt", IsLighted = false },
                new() { AirportIdent = "SWCA", LengthFt = 2200, WidthFt = 60, Surface = "Grass", IsLighted = false },
                new() { AirportIdent = "LFPG", LengthFt = 13000, WidthFt = 150, Surface = "Asphalt", IsLighted = true },
                new() { AirportIdent = "LFPO", LengthFt = 11000, WidthFt = 150, Surface = "Asphalt", IsLighted = true }
            };

            return (airports, runways);
        }

        [Fact]
        public void BoardCache_MesmaSemente_MesmoQuadro()
        {
            var (airports, runways) = CreateSyntheticData();
            var origin = airports.First(a => a.ICAO == "SBGR");
            var refTime = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
            const int seed = 42;

            // Execução 1: 3 quadros sem reaproveitamento de cache (cache = null)
            var rngWithoutCache = new Random(seed);
            var missionsWithoutCache = new List<Mission>();
            for (int b = 0; b < 3; b++)
            {
                var board = MissionGenerator.GenerateMissionsInMemory(
                    origin,
                    airports,
                    null,
                    rngWithoutCache,
                    refTime,
                    countOverride: 15,
                    allRunways: runways,
                    cache: null);
                missionsWithoutCache.AddRange(board);
            }

            // Execução 2: 3 quadros com o mesmo BoardCache reaproveitado
            var rngWithCache = new Random(seed);
            var cache = new BoardCache(airports, runways);
            var missionsWithCache = new List<Mission>();
            for (int b = 0; b < 3; b++)
            {
                var board = MissionGenerator.GenerateMissionsInMemory(
                    origin,
                    airports,
                    null,
                    rngWithCache,
                    refTime,
                    countOverride: 15,
                    allRunways: runways,
                    cache: cache);
                missionsWithCache.AddRange(board);
            }

            Assert.Equal(missionsWithoutCache.Count, missionsWithCache.Count);
            Assert.True(missionsWithCache.Count > 0);

            for (int i = 0; i < missionsWithoutCache.Count; i++)
            {
                var m1 = missionsWithoutCache[i];
                var m2 = missionsWithCache[i];

                Assert.Equal(m1.SubId, m2.SubId);
                Assert.Equal(m1.OriginICAO, m2.OriginICAO);
                Assert.Equal(m1.DestinationICAO, m2.DestinationICAO);
                Assert.Equal(m1.DistanceNM, m2.DistanceNM);
                Assert.Equal(m1.PassengerCount, m2.PassengerCount);
                Assert.Equal(m1.CargoWeightLbs, m2.CargoWeightLbs);
                Assert.Equal(m1.LuggageWeightLbs, m2.LuggageWeightLbs);
                Assert.Equal(m1.RewardMoney, m2.RewardMoney);
            }
        }

        [Fact]
        public void BoardCache_OrigensDiferentes_NaoVazaCandidatos()
        {
            var (airports, runways) = CreateSyntheticData();
            var origin1 = airports.First(a => a.ICAO == "SBGR");
            var origin2 = airports.First(a => a.ICAO == "LFPG");
            var refTime = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

            // Cenário A: Usando o MESMO objeto BoardCache alternando entre duas origens
            var sharedCache = new BoardCache(airports, runways);

            var rng1 = new Random(101);
            var m1Reused = MissionGenerator.GenerateMissionsInMemory(
                origin1, airports, null, rng1, refTime, countOverride: 15, allRunways: runways, cache: sharedCache);

            var rng2 = new Random(202);
            var m2Reused = MissionGenerator.GenerateMissionsInMemory(
                origin2, airports, null, rng2, refTime, countOverride: 15, allRunways: runways, cache: sharedCache);

            var rng3 = new Random(303);
            var m1SecondReused = MissionGenerator.GenerateMissionsInMemory(
                origin1, airports, null, rng3, refTime, countOverride: 15, allRunways: runways, cache: sharedCache);

            // Cenário B: Usando caches isolados/limpos para cada execução com as mesmas sementes
            var rng1Clean = new Random(101);
            var m1Clean = MissionGenerator.GenerateMissionsInMemory(
                origin1, airports, null, rng1Clean, refTime, countOverride: 15, allRunways: runways, cache: new BoardCache(airports, runways));

            var rng2Clean = new Random(202);
            var m2Clean = MissionGenerator.GenerateMissionsInMemory(
                origin2, airports, null, rng2Clean, refTime, countOverride: 15, allRunways: runways, cache: new BoardCache(airports, runways));

            var rng3Clean = new Random(303);
            var m1SecondClean = MissionGenerator.GenerateMissionsInMemory(
                origin1, airports, null, rng3Clean, refTime, countOverride: 15, allRunways: runways, cache: new BoardCache(airports, runways));

            // Validação: Alternar entre origens não causa nenhuma diferença nas missões geradas
            Assert.Equal(m1Clean.Count, m1Reused.Count);
            for (int i = 0; i < m1Clean.Count; i++)
            {
                Assert.Equal(m1Clean[i].SubId, m1Reused[i].SubId);
                Assert.Equal(m1Clean[i].OriginICAO, m1Reused[i].OriginICAO);
                Assert.Equal(m1Clean[i].DestinationICAO, m1Reused[i].DestinationICAO);
                Assert.Equal(m1Clean[i].DistanceNM, m1Reused[i].DistanceNM);
                Assert.Equal(m1Clean[i].RewardMoney, m1Reused[i].RewardMoney);
            }

            Assert.Equal(m2Clean.Count, m2Reused.Count);
            for (int i = 0; i < m2Clean.Count; i++)
            {
                Assert.Equal(m2Clean[i].SubId, m2Reused[i].SubId);
                Assert.Equal(m2Clean[i].OriginICAO, m2Reused[i].OriginICAO);
                Assert.Equal(m2Clean[i].DestinationICAO, m2Reused[i].DestinationICAO);
                Assert.Equal(m2Clean[i].DistanceNM, m2Reused[i].DistanceNM);
                Assert.Equal(m2Clean[i].RewardMoney, m2Reused[i].RewardMoney);
            }

            Assert.Equal(m1SecondClean.Count, m1SecondReused.Count);
            for (int i = 0; i < m1SecondClean.Count; i++)
            {
                Assert.Equal(m1SecondClean[i].SubId, m1SecondReused[i].SubId);
                Assert.Equal(m1SecondClean[i].OriginICAO, m1SecondReused[i].OriginICAO);
                Assert.Equal(m1SecondClean[i].DestinationICAO, m1SecondReused[i].DestinationICAO);
                Assert.Equal(m1SecondClean[i].DistanceNM, m1SecondReused[i].DistanceNM);
                Assert.Equal(m1SecondClean[i].RewardMoney, m1SecondReused[i].RewardMoney);
            }
        }
    }
}
