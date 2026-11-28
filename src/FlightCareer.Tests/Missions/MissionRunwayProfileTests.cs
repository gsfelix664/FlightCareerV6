using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionRunwayProfileTests
    {
        [Fact]
        public void MissionBoardCore_OrigemPistaCurta_SemPerfilWidebody()
        {
            // Origem SBMT (5.120 ft, pavimentada)
            var originSbmt = new Airport
            {
                ICAO = "SBMT",
                Name = "Campo de Marte",
                Country = "Brazil",
                Tier = 2,
                Latitude = -23.5092,
                Longitude = -46.6378,
                ElevationFt = 2369
            };
            var sbmtRunway = new Runway
            {
                AirportIdent = "SBMT",
                LengthFt = 5120,
                Surface = "Asphalt",
                IsLighted = true
            };

            var paletizadaDef = MissionCatalog.Shared.Missions["carga_paletizada"];
            var ponteAereaDef = MissionCatalog.Shared.Missions["ponte_aerea"];
            var originTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "executivo", "mro", "carga_auto" };

            var rng = new Random(42);

            // 1. Em 2.000 sorteios de perfil para carga_paletizada em SBMT, nenhum perfil sorteado pode ser widebody (8.000 ft)
            for (int i = 0; i < 2000; i++)
            {
                var profile = MissionBoardCore.PickProfile(paletizadaDef, originSbmt, originTags, rng, sbmtRunway);
                Assert.NotNull(profile);
                Assert.True(profile!.MinRunwayFt <= 5120, $"Perfil sorteado ({profile.MinRunwayFt} ft) excede a pista da origem (5120 ft)!");
                Assert.Equal(4300, profile.MinRunwayFt); // Apenas o perfil narrowbody cabe em SBMT
            }

            // 2. Se a pista da origem for < 4.300 ft (ex.: 4.000 ft pavimentada), ponte_aerea (4.300 ft) sai do sorteio
            var pistaCurtaRunway = new Runway
            {
                AirportIdent = "SBMT",
                LengthFt = 4000,
                Surface = "Asphalt",
                IsLighted = true
            };

            var candidateDest = new Airport
            {
                ICAO = "SBRJ",
                Name = "Santos Dumont",
                Country = "Brazil",
                Tier = 1,
                Latitude = -22.9105,
                Longitude = -43.1631,
                ElevationFt = 9
            };
            var candidates = new List<AirportCandidate>
            {
                new(candidateDest, new Runway { AirportIdent = "SBRJ", LengthFt = 4338, Surface = "Asphalt", IsLighted = true })
            };

            for (int i = 0; i < 2000; i++)
            {
                var board = MissionBoardCore.GenerateBoard(
                    originSbmt,
                    candidates,
                    originTags,
                    _ => new HashSet<string>(),
                    MissionCatalog.Shared,
                    rng,
                    DateTime.UtcNow,
                    targetCount: 10,
                    originRunway: pistaCurtaRunway
                );

                // Nenhuma missão de ponte_aerea pode ser gerada
                Assert.DoesNotContain(board, m => m.SubId == "ponte_aerea");
            }
        }

        [Fact]
        public void MissionBoardCore_DestinoRespeitaPistaDoPerfil()
        {
            var origin = new Airport
            {
                ICAO = "SBGR",
                Name = "Guarulhos",
                Country = "Brazil",
                Tier = 1,
                Latitude = -23.4356,
                Longitude = -46.4731
            };
            var originRunway = new Runway
            {
                AirportIdent = "SBGR",
                LengthFt = 12000,
                Surface = "Asphalt",
                IsLighted = true
            };

            // Perfil que exige 4.300 ft pavimentada (narrowbody, ex: ponte_aerea ou troncal P1)
            var profile = new MissionProfileDefinition
            {
                Weight = 100,
                RangeNm = new[] { 100, 300 },
                Pax = new[] { 100, 150 },
                CargoLbs = new double[] { 0, 0 },
                BigAirport = true,
                MinRunwayFt = 4300,
                Paved = true
            };

            var def = new MissionDefinition
            {
                Id = "teste_narrowbody",
                Name = "Teste Narrowbody",
                Profiles = new List<MissionProfileDefinition> { profile },
                Destination = new MissionDestinationRule { Runway = "any", Country = "same" }
            };

            // Candidatos com várias pistas:
            // 1. Destino A: 3.000 ft asfaltada (curta demais)
            // 2. Destino B: 5.000 ft de terra (não pavimentada)
            // 3. Destino C: sem pista conhecida
            // 4. Destino D: 4.500 ft asfaltada (válida!)
            // 5. Destino E: 6.000 ft concreto (válida!)
            var destCurto = new Airport { ICAO = "SDUB", Name = "Ubatuba", Country = "Brazil", Tier = 4, Latitude = -23.4419, Longitude = -45.0747 };
            var destTerra = new Airport { ICAO = "SSKW", Name = "Fazenda", Country = "Brazil", Tier = 4, Latitude = -23.5, Longitude = -45.5 };
            var destSemPista = new Airport { ICAO = "SDXX", Name = "Sem Pista", Country = "Brazil", Tier = 3, Latitude = -23.6, Longitude = -45.6 };
            var destValido1 = new Airport { ICAO = "SBRJ", Name = "Santos Dumont", Country = "Brazil", Tier = 1, Latitude = -22.9105, Longitude = -43.1631 };
            var destValido2 = new Airport { ICAO = "SBSP", Name = "Congonhas", Country = "Brazil", Tier = 1, Latitude = -23.6261, Longitude = -46.6564 };

            var candidates = new List<AirportCandidate>
            {
                new(destCurto, new Runway { AirportIdent = "SDUB", LengthFt = 3000, Surface = "Asphalt" }),
                new(destTerra, new Runway { AirportIdent = "SSKW", LengthFt = 5000, Surface = "Dirt" }),
                new(destSemPista, null),
                new(destValido1, new Runway { AirportIdent = "SBRJ", LengthFt = 4500, Surface = "Asphalt" }),
                new(destValido2, new Runway { AirportIdent = "SBSP", LengthFt = 6000, Surface = "Concrete" })
            };

            var rng = new Random(1234);

            for (int i = 0; i < 200; i++)
            {
                var mission = MissionBoardCore.CreateSingleMission(
                    origin,
                    def,
                    candidates,
                    new HashSet<string>(),
                    _ => new HashSet<string>(),
                    rng,
                    DateTime.UtcNow,
                    originRunway: originRunway
                );

                Assert.NotNull(mission);
                Assert.DoesNotContain(mission!.DestinationICAO, new[] { "SDUB", "SSKW", "SDXX" });
                Assert.Contains(mission.DestinationICAO, new[] { "SBRJ", "SBSP" });
            }
        }

        [Fact]
        public void MissionCatalog_TodosOsPerfisTemPistaMinima()
        {
            var allowedZeroRunwayMissionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "instrucao_local",
                "circuito_tgl",
                "navegacao_vfr",
                "navegacao_vfr_ida",
                "instrucao_ifr",
                "adaptacao_noturna",
                "cheque",
                "instrucao_stol",
                "panoramico",
                "demo_flight",
                "reboque_planador",
                "check_flight"
            };

            var catalog = MissionCatalog.Shared;
            Assert.NotEmpty(catalog.Missions);

            foreach (var mission in catalog.Missions.Values)
            {
                Assert.NotEmpty(mission.Profiles);
                bool isAllowedZero = allowedZeroRunwayMissionIds.Contains(mission.Id);

                for (int i = 0; i < mission.Profiles.Count; i++)
                {
                    var profile = mission.Profiles[i];
                    if (profile.MinRunwayFt == 0)
                    {
                        Assert.True(isAllowedZero, $"Missão '{mission.Id}' perfil #{i + 1} possui MinRunwayFt = 0, mas não é do grupo permitido (instrução/local/volta à origem).");
                    }
                    else
                    {
                        Assert.True(profile.MinRunwayFt >= 1000, $"Missão '{mission.Id}' perfil #{i + 1} possui MinRunwayFt = {profile.MinRunwayFt}, que é inválido (< 1000 ft).");
                    }
                }
            }
        }

        [Fact]
        public void MissionBoardCore_LinhaAerea_ExigeEtiquetaLinhaNaOrigemEDestino()
        {
            var originSbmt = new Airport
            {
                ICAO = "SBMT",
                Name = "Campo de Marte",
                Country = "Brazil",
                Tier = 2,
                Latitude = -23.5092,
                Longitude = -46.6378,
                ElevationFt = 2369
            };
            var sbmtRunway = new Runway
            {
                AirportIdent = "SBMT",
                LengthFt = 5120,
                Surface = "Asphalt",
                IsLighted = true
            };

            var destSbrj = new Airport
            {
                ICAO = "SBRJ",
                Name = "Santos Dumont",
                Country = "Brazil",
                Tier = 1,
                Latitude = -22.9105,
                Longitude = -43.1631,
                ElevationFt = 9
            };
            var sbrjRunway = new Runway { AirportIdent = "SBRJ", LengthFt = 4338, Surface = "Asphalt", IsLighted = true };

            var destSbco = new Airport
            {
                ICAO = "SDCO",
                Name = "Sorocaba",
                Country = "Brazil",
                Tier = 3,
                Latitude = -23.4808,
                Longitude = -47.4897,
                ElevationFt = 2077
            };
            var sbcoRunway = new Runway { AirportIdent = "SDCO", LengthFt = 4800, Surface = "Asphalt", IsLighted = true };

            var candidates = new List<AirportCandidate>
            {
                new(destSbrj, sbrjRunway),
                new(destSbco, sbcoRunway)
            };

            var sbmtTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "executivo", "mro" };
            Func<string, IReadOnlySet<string>> getAirportTags = icao =>
            {
                if (icao == "SBRJ") return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "linha", "linha:ponte_aerea" };
                if (icao == "SBMT") return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "executivo", "mro" };
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "executivo" };
            };

            var rng = new Random(42);
            bool generatedSubRegional = false;

            // 1. SBMT (sem linha/linha_auto) NUNCA gera ponte_aerea nem troncal_domestico em 2.000 sorteios
            for (int i = 0; i < 2000; i++)
            {
                var board = MissionBoardCore.GenerateBoard(
                    originSbmt,
                    candidates,
                    sbmtTags,
                    getAirportTags,
                    MissionCatalog.Shared,
                    rng,
                    DateTime.UtcNow,
                    targetCount: 15,
                    originRunway: sbmtRunway
                );

                Assert.DoesNotContain(board, m => m.SubId is "ponte_aerea" or "troncal_domestico");

                if (board.Any(m => m.SubId == "sub_regional"))
                {
                    generatedSubRegional = true;
                }
            }

            // 2. sub_regional continua saindo de aeroporto sem etiqueta
            Assert.True(generatedSubRegional, "Esperava que sub_regional fosse gerado saindo de SBMT (sem etiqueta de linha).");

            // 3. Origem com linha (SBGR) gerando ponte_aerea/troncal nunca escolhe destino sem etiqueta (SDCO)
            var originSbgr = new Airport
            {
                ICAO = "SBGR",
                Name = "Guarulhos",
                Country = "Brazil",
                Tier = 1,
                Latitude = -23.4356,
                Longitude = -46.4731,
                ElevationFt = 2461
            };
            var sbgrRunway = new Runway { AirportIdent = "SBGR", LengthFt = 12000, Surface = "Asphalt", IsLighted = true };
            var sbgrTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "linha", "linha:mega_hub" };

            for (int i = 0; i < 500; i++)
            {
                var board = MissionBoardCore.GenerateBoard(
                    originSbgr,
                    candidates,
                    sbgrTags,
                    getAirportTags,
                    MissionCatalog.Shared,
                    rng,
                    DateTime.UtcNow,
                    targetCount: 15,
                    originRunway: sbgrRunway
                );

                foreach (var m in board.Where(m => m.SubId is "ponte_aerea" or "troncal_domestico"))
                {
                    // Destino SEMPRE SBRJ (que tem etiqueta 'linha'), nunca SDCO (que não tem)
                    Assert.Equal("SBRJ", m.DestinationICAO);
                }
            }
        }
    }
}
