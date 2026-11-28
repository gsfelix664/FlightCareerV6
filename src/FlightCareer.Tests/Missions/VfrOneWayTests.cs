using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class VfrOneWayTests
    {
        [Fact]
        public void Catalogo_NavegacaoVfrIdaExisteEValida()
        {
            var catalog = MissionCatalog.Shared;
            Assert.Equal(70, catalog.Missions.Count);

            var def = catalog.Get("navegacao_vfr_ida");
            Assert.NotNull(def);
            Assert.True(def.Enabled);
            Assert.Equal("vfr_one_way", def.Generator);
            Assert.Equal(10.0, def.BaseByTier["3"]);
            Assert.Equal(3.0, def.BaseByTier["2"]);
            Assert.Equal(6.0, def.BaseByTier["4"]);
            Assert.Equal(0.0, def.BaseByTier["1"]);

            Assert.Equal(2, def.Profiles.Count);
            var iniciante = def.Profiles.FirstOrDefault(p => p.Variant == "iniciante");
            var intermediaria = def.Profiles.FirstOrDefault(p => p.Variant == "intermediaria");
            Assert.NotNull(iniciante);
            Assert.NotNull(intermediaria);

            Assert.Equal(60.0, iniciante.Weight);
            Assert.Equal(3, iniciante.RepeatPool);
            Assert.Null(iniciante.MinPilotLevel);

            Assert.Equal(40.0, intermediaria.Weight);
            Assert.Equal(3, intermediaria.MinPilotLevel);

            // triangle_long em navegacao_vfr
            var vfr = catalog.Get("navegacao_vfr");
            Assert.NotNull(vfr);
            var longProfile = vfr.Profiles.FirstOrDefault(p => p.Variant == "triangle_long");
            Assert.NotNull(longProfile);
            Assert.Equal(3, longProfile.MinPilotLevel);
            Assert.Equal(20.0, longProfile.Weight);
            Assert.Equal(new[] { 150, 300 }, longProfile.RangeNm);
            Assert.Equal(new[] { 40, 130 }, longProfile.LegNm);
        }

        [Fact]
        public void PickProfile_NivelAbaixoDoMinimo_DescartaPerfil()
        {
            var origin = new Airport { ICAO = "SBJD", Country = "Brazil", Tier = 3 };
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var def = new MissionDefinition
            {
                Id = "test_def",
                Profiles = new List<MissionProfileDefinition>
                {
                    new() { Variant = "A", Weight = 50, RangeNm = new[] { 50, 100 } },
                    new() { Variant = "B", Weight = 50, RangeNm = new[] { 50, 150 }, MinPilotLevel = 3 }
                }
            };

            var rnd = new Random(42);

            // Nível 2: 200 sorteios devem ser todos A
            for (int i = 0; i < 200; i++)
            {
                var picked = MissionBoardCore.PickProfile(def, origin, tags, rnd, originRunway: null, pilotLevel: 2);
                Assert.NotNull(picked);
                Assert.Equal("A", picked.Variant);
            }

            // Nível 3: B deve aparecer
            bool foundB = false;
            for (int i = 0; i < 200; i++)
            {
                var picked = MissionBoardCore.PickProfile(def, origin, tags, rnd, originRunway: null, pilotLevel: 3);
                Assert.NotNull(picked);
                if (picked.Variant == "B")
                {
                    foundB = true;
                    break;
                }
            }
            Assert.True(foundB, "Perfil B (nível mínimo 3) não apareceu no sorteio para piloto de nível 3");
        }

        [Fact]
        public void PickProfile_NivelNulo_NaoFiltra()
        {
            var origin = new Airport { ICAO = "SBJD", Country = "Brazil", Tier = 3 };
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var def = new MissionDefinition
            {
                Id = "test_def",
                Profiles = new List<MissionProfileDefinition>
                {
                    new() { Variant = "A", Weight = 50, RangeNm = new[] { 50, 100 } },
                    new() { Variant = "B", Weight = 50, RangeNm = new[] { 50, 150 }, MinPilotLevel = 3 }
                }
            };

            var rnd = new Random(123);
            bool foundB = false;
            for (int i = 0; i < 200; i++)
            {
                var picked = MissionBoardCore.PickProfile(def, origin, tags, rnd, originRunway: null, pilotLevel: null);
                Assert.NotNull(picked);
                if (picked.Variant == "B")
                {
                    foundB = true;
                    break;
                }
            }
            Assert.True(foundB, "Perfil B deveria aparecer quando pilotLevel for null");
        }

        private static AirportCandidate CreateCandidate(string icao, double lat, double lon, int tier, bool hasTower, int runwayLength, string surface, int elev = 2000, string country = "Brazil")
        {
            var airport = new Airport
            {
                ICAO = icao,
                Name = icao,
                Country = country,
                Tier = tier,
                Latitude = lat,
                Longitude = lon,
                ElevationFt = elev,
                HasTower = hasTower
            };
            var runway = new Runway
            {
                AirportIdent = icao,
                LengthFt = runwayLength,
                Surface = surface
            };
            return new AirportCandidate(airport, runway);
        }

        [Fact]
        public void BeginnerPool_PrefereSemTorreEPistaFacil_TresMaisProximos()
        {
            // Origem no equador lat 0, lon 0
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };

            // 1 grau lat ~ 60 NM
            // 52 NM com torre: lat 52/60 ~ 0.86667
            // 55 NM sem torre: lat 55/60 ~ 0.91667
            // 60 NM sem torre: lat 1.0
            // 70 NM sem torre: lat 70/60 ~ 1.16667
            // 80 NM sem torre: lat 80/60 ~ 1.33333
            // 95 NM sem torre: lat 95/60 ~ 1.58333
            var cTower52 = CreateCandidate("TOW1", 52.0 / 60.0, 0.0, tier: 3, hasTower: true, runwayLength: 3000, surface: "ASPH");
            var c55 = CreateCandidate("NOT1", 55.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var c60 = CreateCandidate("NOT2", 60.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var c70 = CreateCandidate("NOT3", 70.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var c80 = CreateCandidate("NOT4", 80.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var c95 = CreateCandidate("NOT5", 95.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");

            var candidates = new List<AirportCandidate> { c95, cTower52, c70, c55, c80, c60 };

            var pool = VfrOneWayDestinations.BeginnerPool(origin, candidates, minNm: 50, maxNm: 100, poolSize: 3);

            Assert.Equal(3, pool.Count);
            // Degrau (a) preferiu sem torre e pista fácil: NOT1, NOT2, NOT3
            Assert.Equal("NOT1", pool[0].Airport.ICAO);
            Assert.Equal("NOT2", pool[1].Airport.ICAO);
            Assert.Equal("NOT3", pool[2].Airport.ICAO);
        }

        [Fact]
        public void BeginnerPool_SemCandidatoFacil_RelaxaEscada()
        {
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };

            // Apenas pistas não pavimentadas (terra/grama) >= 1800 ft
            var c1 = CreateCandidate("DIR1", 60.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 2200, surface: "DIRT");
            var c2 = CreateCandidate("DIR2", 70.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 2500, surface: "GRASS");

            var candidates = new List<AirportCandidate> { c2, c1 };

            var pool = VfrOneWayDestinations.BeginnerPool(origin, candidates, minNm: 50, maxNm: 100, poolSize: 3);

            Assert.Equal(2, pool.Count);
            Assert.Equal("DIR1", pool[0].Airport.ICAO);
            Assert.Equal("DIR2", pool[1].Airport.ICAO);
        }

        [Fact]
        public void BeginnerPool_Deterministico()
        {
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };

            var c1 = CreateCandidate("AIR1", 60.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var c2 = CreateCandidate("AIR2", 70.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var c3 = CreateCandidate("AIR3", 80.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");

            var list1 = new List<AirportCandidate> { c1, c2, c3 };
            var list2 = new List<AirportCandidate> { c3, c1, c2 };

            var pool1 = VfrOneWayDestinations.BeginnerPool(origin, list1, 50, 100, 3);
            var pool2 = VfrOneWayDestinations.BeginnerPool(origin, list2, 50, 100, 3);

            Assert.Equal(pool1.Select(c => c.Airport.ICAO), pool2.Select(c => c.Airport.ICAO));
        }

        [Fact]
        public void BeginnerPool_ForaDoRaioOuOutroPais_Vazio()
        {
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };

            var cPerto = CreateCandidate("NEAR", 40.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH"); // 40 NM
            var cLonge = CreateCandidate("FARR", 120.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH"); // 120 NM
            var cOutroPais = CreateCandidate("EXT1", 60.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH", country: "Argentina");

            var pool = VfrOneWayDestinations.BeginnerPool(origin, new[] { cPerto, cLonge, cOutroPais }, 50, 100, 3);

            Assert.Empty(pool);
        }

        [Fact]
        public void IntermediateWeight_ContaCaracteristicas()
        {
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };

            // Sem nada desafiador: sem torre, pista 4000 ft pavimentada, relevo similar
            var cNormal = CreateCandidate("NORM", 60.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 4000, surface: "ASPH", elev: 1200);
            double wNormal = VfrOneWayDestinations.IntermediateWeight(origin, cNormal);
            Assert.Equal(1.0, wNormal);

            // Apenas torre: 1 + 2 * 1 = 3
            var cTorre = CreateCandidate("TORR", 60.0 / 60.0, 0.0, tier: 3, hasTower: true, runwayLength: 4000, surface: "ASPH", elev: 1200);
            double wTorre = VfrOneWayDestinations.IntermediateWeight(origin, cTorre);
            Assert.Equal(3.0, wTorre);

            // Torre + pista 2800 ft (curta <= 3000) + relevo 2200 ft (desnível 1200 >= 1000): 1 + 2 * 3 = 7
            var cDesafio = CreateCandidate("HARD", 60.0 / 60.0, 0.0, tier: 3, hasTower: true, runwayLength: 2800, surface: "ASPH", elev: 2200);
            double wDesafio = VfrOneWayDestinations.IntermediateWeight(origin, cDesafio);
            Assert.Equal(7.0, wDesafio);
        }

        [Fact]
        public void CreateVfrOneWay_Iniciante_DestinoDoPoolESemVolta()
        {
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };
            var c1 = CreateCandidate("NOT1", 60.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var c2 = CreateCandidate("NOT2", 70.0 / 60.0, 0.0, tier: 3, hasTower: false, runwayLength: 3000, surface: "ASPH");
            var candidates = new List<AirportCandidate> { c1, c2 };

            var def = MissionCatalog.Shared.Get("navegacao_vfr_ida")!;
            var profile = def.Profiles.First(p => p.Variant == "iniciante");

            var rnd = new Random(42);
            var mission = MissionGenerator.CreateVfrOneWayMissionSync(
                origin, def, rnd, DateTime.UtcNow, candidates, profile: profile);

            Assert.NotNull(mission);
            Assert.Equal("ORIG", mission.OriginICAO);
            Assert.NotEqual("ORIG", mission.DestinationICAO);
            Assert.True(mission.DestinationICAO == "NOT1" || mission.DestinationICAO == "NOT2");
            Assert.InRange(mission.DistanceNM, 50, 100);
            Assert.False(string.IsNullOrWhiteSpace(mission.OverflyWaypoints));
            Assert.Contains("Ponto de Reporte VFR", mission.OverflyWaypoints);
            Assert.Null(mission.ContractData); // sem escalas adicionais
        }

        [Fact]
        public void CreateVfrOneWay_SemCandidato_RetornaNull()
        {
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };
            var candidates = new List<AirportCandidate>();

            var def = MissionCatalog.Shared.Get("navegacao_vfr_ida")!;
            var profile = def.Profiles.First(p => p.Variant == "iniciante");

            var mission = MissionGenerator.CreateVfrOneWayMissionSync(
                origin, def, new Random(42), DateTime.UtcNow, candidates, profile: profile);

            Assert.Null(mission);
        }

        [Fact]
        public void GenerateBoard_Nivel1_SemIntermediariaNemTriangleLong()
        {
            var origin = new Airport
            {
                ICAO = "BASE",
                Name = "Base Instrucao",
                Country = "Brazil",
                Tier = 3,
                Latitude = -23.0,
                Longitude = -47.0,
                ElevationFt = 2000
            };
            var originRunway = new Runway { AirportIdent = "BASE", LengthFt = 4000, Surface = "ASPH" };

            // Candidatos com torre e desafiadores, e também simples
            var candidates = new List<AirportCandidate>
            {
                new(origin, originRunway),
                CreateCandidate("DEST1", -23.0 + (60.0 / 60.0), -47.0, tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH"),
                CreateCandidate("DEST2", -23.0 + (70.0 / 60.0), -47.0, tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH"),
                CreateCandidate("DEST3", -23.0 - (60.0 / 60.0), -47.0, tier: 3, hasTower: true, runwayLength: 3500, surface: "ASPH"),
                CreateCandidate("DEST4", -23.0, -47.0 + (60.0 / 60.0), tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH")
            };

            var originTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "instrucao", "aeroclube" };
            var rnd = new Random(777);

            var missions = MissionBoardCore.GenerateBoard(
                origin,
                candidates,
                originTags,
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                MissionCatalog.Shared,
                rnd,
                DateTime.UtcNow,
                targetCount: 150,
                originRunway: originRunway,
                pilotLevel: 1);

            Assert.NotEmpty(missions);

            // Nível 1 não deve sortear intermediária nem triangle_long
            foreach (var m in missions)
            {
                if (m.SubId == "navegacao_vfr_ida")
                {
                    // A intermediária gera título com 'Intermediária' ou descrição com 'desafiador'
                    Assert.DoesNotContain("Intermediária", m.Title);
                    Assert.DoesNotContain("desafiador", m.Description);
                }
                if (m.SubId == "navegacao_vfr")
                {
                    Assert.DoesNotContain("Longa", m.Title);
                }
            }
        }

        [Fact]
        public void GenerateBoard_Nivel3_TemIntermediaria()
        {
            var origin = new Airport
            {
                ICAO = "BASE",
                Name = "Base Instrucao",
                Country = "Brazil",
                Tier = 3,
                Latitude = -23.0,
                Longitude = -47.0,
                ElevationFt = 2000
            };
            var originRunway = new Runway { AirportIdent = "BASE", LengthFt = 4000, Surface = "ASPH" };

            // Candidatos com torre e desafiadores, e também simples
            var candidates = new List<AirportCandidate>
            {
                new(origin, originRunway),
                CreateCandidate("DEST1", -23.0 + (60.0 / 60.0), -47.0, tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH"),
                CreateCandidate("DEST2", -23.0 + (70.0 / 60.0), -47.0, tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH"),
                CreateCandidate("DEST3", -23.0 - (60.0 / 60.0), -47.0, tier: 3, hasTower: true, runwayLength: 2500, surface: "ASPH", elev: 3500),
                CreateCandidate("DEST4", -23.0, -47.0 + (60.0 / 60.0), tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH")
            };

            var originTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "instrucao", "aeroclube" };
            var rnd = new Random(777);

            var missions = MissionBoardCore.GenerateBoard(
                origin,
                candidates,
                originTags,
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                MissionCatalog.Shared,
                rnd,
                DateTime.UtcNow,
                targetCount: 200,
                originRunway: originRunway,
                pilotLevel: 3);

            Assert.NotEmpty(missions);

            // Nível 3 permite intermediária
            bool foundIntermediaria = missions.Any(m => m.SubId == "navegacao_vfr_ida" && (m.Title.Contains("Intermediária") || m.Description.Contains("desafiador")));
            Assert.True(foundIntermediaria, "Deveria encontrar missão intermediária no quadro para piloto de nível 3");
        }

        [Fact]
        public void CreateVfrTriangle_TriangleLong_TotalMin150EDuasParadasCompletas()
        {
            var origin = new Airport { ICAO = "ORIG", Country = "Brazil", Tier = 3, Latitude = 0.0, Longitude = 0.0, ElevationFt = 1000 };

            // Pernas entre 40 e 130 NM, total entre 150 e 300 NM
            // ORIG (0, 0) -> A (1.0, 0) = 60 NM
            // A (1.0, 0) -> B (1.0, 1.0) = 60 NM
            // B (1.0, 1.0) -> ORIG (0, 0) = sqrt(60^2 + 60^2) ~ 84.85 NM
            // Total ~ 205 NM
            var candA = CreateCandidate("STPA", 1.0, 0.0, tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH");
            var candB = CreateCandidate("STPB", 1.0, 1.0, tier: 3, hasTower: false, runwayLength: 3500, surface: "ASPH");

            var def = MissionCatalog.Shared.Get("navegacao_vfr")!;
            var profile = def.Profiles.First(p => p.Variant == "triangle_long");

            var rnd = new Random(42);
            var mission = MissionGenerator.CreateVfrTriangleMissionSync(
                origin, def, rnd, DateTime.UtcNow, new[] { candA, candB }, profile: profile);

            Assert.NotNull(mission);
            Assert.Equal("ORIG", mission.OriginICAO);
            Assert.Equal("ORIG", mission.DestinationICAO);
            Assert.True(mission.DistanceNM >= 150, $"Distância total esperada >= 150 NM, obtido {mission.DistanceNM}");
            Assert.True(mission.Title.Contains("Longa") || mission.Title.Contains("Longo"), $"Título esperado com Longa/Longo, obtido: {mission.Title}");

            Assert.NotNull(mission.ContractData);
            var contract = JsonSerializer.Deserialize<MultiStopContractData>(mission.ContractData);
            Assert.NotNull(contract);
            Assert.Equal(2, contract!.Stops.Count);
            Assert.Equal("full_stop", contract.StopMode);
        }
    }
}
