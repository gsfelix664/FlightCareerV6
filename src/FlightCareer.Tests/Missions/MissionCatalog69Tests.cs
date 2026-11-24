using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionCatalog69Tests
    {
        private static readonly string[] Old59Ids = new[]
        {
            "linha_domestica_hub", "linha_regional_internacional", "linha_long_haul",
            "carga_heavy_nacional", "carga_heavy_internacional", "carga_heavy_long_haul",
            "executiva_business", "logistica_apoio_crew_parts", "translado_posicionamento_ferry",
            "diplomatico_militar", "alimentacao_hub", "regional_direto",
            "agro_legacy", "agro_tech", "agro_investidores",
            "ecommerce_varejo", "agro_pecas_insumos", "malotes_documentos",
            "manutencao_regional", "governo_local_estadual",
            "instrucao_local", "navegacao_cross_country", "voo_de_cheque",
            "executivo_regional", "familia_alto_padrao", "equipes_urgentes",
            "aog_agro_suporte", "agro_insumos_biotecnologia", "agro_suprimento_campanha",
            "remocao_urgencia", "transferencia_eletiva", "transporte_orgaos",
            "abastecimento_rancho", "transporte_patrao", "troca_equipe",
            "apoio_batedor", "translado_piloto_agricola", "insumos_urgentes_agro",
            "medivac_bush", "aog_maquina_campo",
            "aerolevantamento", "paraquedismo",
            "voo_panoramico", "circuito_tgl", "translado_basico",
            "fretamento_executivo", "carga_fragil", "medevac_dinamico",
            "vip_internacional_curto", "ponte_aerea_troncal", "cargueiro_noturno",
            "long_haul_internacional", "fretamento_global_carga_alto_valor",
            "fretamento_executivo_regional", "carga_fragil_regional", "medevac_regional",
            "vip_internacional_regional", "carga_fragil_municipal", "medevac_municipal"
        };

        [Fact]
        public void MissionCatalog_Carrega69Tipos_IdsUnicosEDadosValidos()
        {
            var catalog = MissionCatalog.Shared;
            Assert.Equal(69, catalog.Missions.Count);

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int disabledCount = 0;

            foreach (var m in catalog.Missions.Values)
            {
                Assert.True(ids.Add(m.Id), $"ID duplicado: {m.Id}");

                // baseByTier 1–4
                Assert.True(m.BaseByTier.ContainsKey("1"), $"{m.Id} sem baseByTier 1");
                Assert.True(m.BaseByTier.ContainsKey("2"), $"{m.Id} sem baseByTier 2");
                Assert.True(m.BaseByTier.ContainsKey("3"), $"{m.Id} sem baseByTier 3");
                Assert.True(m.BaseByTier.ContainsKey("4"), $"{m.Id} sem baseByTier 4");

                // >= 1 perfil
                Assert.NotEmpty(m.Profiles);
                foreach (var p in m.Profiles)
                {
                    Assert.True(p.Weight > 0, $"{m.Id} perfil com peso <= 0");
                    Assert.True(p.RangeNm[0] <= p.RangeNm[1], $"{m.Id} range min > max");
                    Assert.True(p.Pax[0] <= p.Pax[1], $"{m.Id} pax min > max");
                    Assert.True(p.CargoLbs[0] <= p.CargoLbs[1], $"{m.Id} cargo min > max");
                }

                if (m.LuggagePerPersonKg != null)
                {
                    Assert.True(m.LuggagePerPersonKg[0] <= m.LuggagePerPersonKg[1], $"{m.Id} luggage min > max");
                }

                if (m.Crew != null && m.Crew.LuggageKg.Length > 1)
                {
                    Assert.True(m.Crew.LuggageKg[0] <= m.Crew.LuggageKg[1], $"{m.Id} crew luggage min > max");
                }

                if (!m.Enabled)
                {
                    disabledCount++;
                }
            }

            Assert.Equal(17, disabledCount);
        }

        [Fact]
        public void MissionCatalog_LegacyIds_CobremTodosOsIdsAntigos()
        {
            var catalog = MissionCatalog.Shared;

            foreach (var oldId in Old59Ids)
            {
                var def = catalog.Get(oldId);
                Assert.NotNull(def);
                Assert.True(
                    string.Equals(def.Id, oldId, StringComparison.OrdinalIgnoreCase) ||
                    def.LegacyCodes.Any(l => string.Equals(l, oldId, StringComparison.OrdinalIgnoreCase)),
                    $"ID antigo '{oldId}' não coberto pelo novo catálogo.");
            }
        }

        [Fact]
        public void AirportProfileCatalog_CasamentoDeEtiqueta()
        {
            var testDict = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["TEST"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "carga:pereciveis", "saude:resgate", "polo_aeromedico" }
            };

            var cat = new AirportProfileCatalog(testDict);

            var tags = cat.TagsFor("TEST");
            Assert.Contains("carga:pereciveis", tags);

            // Casamento: chave sem ':' casa prefixo ('carga' casa 'carga:pereciveis')
            Assert.True(AirportProfileCatalog.MatchesAny("carga", tags));
            // Casamento exato
            Assert.True(AirportProfileCatalog.MatchesAny("carga:pereciveis", tags));
            // Chave com ':' não casa se não for exata
            Assert.False(AirportProfileCatalog.MatchesAny("carga:noturno", tags));
            // Chave sem ':' não casa categoria diferente
            Assert.False(AirportProfileCatalog.MatchesAny("militar", tags));

            // Aeroporto desconhecido = conjunto vazio
            var unknownTags = cat.TagsFor("UNKNOWN");
            Assert.Empty(unknownTags);
            Assert.False(AirportProfileCatalog.MatchesAny("carga", unknownTags));
        }

        [Fact]
        public void MissionWeighting_OriginWeight()
        {
            var catalog = MissionCatalog.Shared;

            // 1. paraquedismo tier 3 = 3, com zona_salto (x10) = 30
            var skydive = catalog.Get("paraquedismo")!;
            var emptyTags = new HashSet<string>();
            double wSkyBase = MissionWeighting.OriginWeight(skydive, 3, emptyTags);
            Assert.Equal(3.0, wSkyBase);

            var saltoTags = new HashSet<string> { "zona_salto" };
            double wSkySalto = MissionWeighting.OriginWeight(skydive, 3, saltoTags);
            Assert.Equal(30.0, wSkySalto);

            // 2. panoramico tier 3 (base 6) com turistico_curado:base (8) e turistico_poi (3) = 6 * 12 = 72 (teto x12)
            var pano = catalog.Get("panoramico")!;
            var panoTags = new HashSet<string> { "turistico_curado:base", "turistico_poi" };
            double wPano = MissionWeighting.OriginWeight(pano, 3, panoTags);
            Assert.Equal(72.0, wPano);

            // 3. Base puramente militar (militar sem militar_misto): civilAtMilitaryBase == true sofre x0.2
            var civilMission = catalog.Get("executivo_corporativo")!;
            var pureMilTags = new HashSet<string> { "militar" };
            double wCivilMil = MissionWeighting.OriginWeight(civilMission, 1, pureMilTags);
            double wCivilBase = MissionWeighting.OriginWeight(civilMission, 1, emptyTags);
            Assert.Equal(wCivilBase * 0.2, wCivilMil, 2);

            // 4. Base militar mista: sem penalidade de 0.2
            var mixedMilTags = new HashSet<string> { "militar", "militar_misto" };
            double wCivilMixed = MissionWeighting.OriginWeight(civilMission, 1, mixedMilTags);
            Assert.Equal(wCivilBase, wCivilMixed, 2);

            // 5. Missão desabilitada: peso = 0
            var disabled = catalog.Missions.Values.First(m => !m.Enabled);
            double wDisabled = MissionWeighting.OriginWeight(disabled, 1, emptyTags);
            Assert.Equal(0.0, wDisabled);
        }

        [Fact]
        public void MissionWeighting_DestinationWeight_NuncaZero()
        {
            var catalog = MissionCatalog.Shared;
            var overnight = catalog.Get("carga_overnight")!;

            // Sem etiquetas no destino -> peso padrão = 1.0
            double wNoTag = MissionWeighting.DestinationWeight(overnight, new HashSet<string>());
            Assert.Equal(1.0, wNoTag);

            // Com etiquetas correspondentes -> multiplica os pesos casados
            var destTags = new HashSet<string> { "carga", "alimentador" };
            double wMatch = MissionWeighting.DestinationWeight(overnight, destTags);
            // carga (3) * alimentador (3) = 9
            Assert.Equal(9.0, wMatch);
            Assert.True(wMatch >= 1.0);
        }

        [Fact]
        public void MissionBoardCore_DestinoPreferencial_NaoExclusivo()
        {
            // 1 candidato com peso 5, 9 candidatos com peso 1
            var origin = new Airport { ICAO = "SBGR", Tier = 1, Latitude = -23.4, Longitude = -46.4, Country = "BR" };
            var prefDest = new Airport { ICAO = "SBKP", Tier = 2, Latitude = -23.0, Longitude = -47.1, Country = "BR" };
            var prefCandidate = new AirportCandidate(prefDest, null);

            var otherCandidates = Enumerable.Range(1, 9)
                .Select(i => new AirportCandidate(new Airport
                {
                    ICAO = $"SB0{i}",
                    Tier = 2,
                    Latitude = -23.0 - (i * 0.05),
                    Longitude = -47.0 - (i * 0.05),
                    Country = "BR"
                }, null))
                .ToList();

            var allCandidates = new List<AirportCandidate> { prefCandidate };
            allCandidates.AddRange(otherCandidates);

            var mission = new MissionDefinition
            {
                Id = "teste_pref",
                Name = "Teste Pref",
                Destination = new MissionDestinationRule
                {
                    Rule = "standard",
                    Tiers = new List<int> { 2 },
                    Profiles = new Dictionary<string, double> { ["preferencial"] = 5.0 }
                }
            };

            var profile = new MissionProfileDefinition
            {
                Weight = 100,
                RangeNm = new int[] { 10, 500 },
                Pax = new int[] { 0, 0 },
                CargoLbs = new double[] { 0, 0 }
            };

            var getTags = new Func<string, IReadOnlySet<string>>(icao =>
                icao == "SBKP"
                    ? new HashSet<string> { "preferencial" }
                    : new HashSet<string>());

            var rng = new Random(42);
            int prefChosen = 0;
            var chosenCounts = new Dictionary<string, int>();

            for (int i = 0; i < 10000; i++)
            {
                var chosen = MissionBoardCore.PickDestination(
                    mission, allCandidates, getTags, rng);

                Assert.NotNull(chosen);
                if (chosen!.Airport.ICAO == "SBKP") prefChosen++;

                chosenCounts[chosen.Airport.ICAO] = chosenCounts.GetValueOrDefault(chosen.Airport.ICAO) + 1;
            }

            // Ponderação: 5 / (5 + 9*1) = 5/14 ~= 35.7%
            double ratio = (double)prefChosen / 10000;
            Assert.InRange(ratio, 0.30, 0.42);

            // NENHUM dos 10 destinos teve contagem zero (ponderado, nunca exclusivo)
            Assert.Equal(10, chosenCounts.Count);
            foreach (var kvp in chosenCounts)
            {
                Assert.True(kvp.Value > 100, $"Destino {kvp.Key} foi escolhido apenas {kvp.Value} vezes");
            }
        }

        [Fact]
        public void MissionBoardCore_PerfilCorrelacionado()
        {
            var catalog = MissionCatalog.Shared;
            var paletizada = catalog.Get("carga_paletizada")!;

            var origin = new Airport { ICAO = "SBGR", Tier = 1, Latitude = -23.4, Longitude = -46.4, Country = "BR" };
            var rwy = new Runway { AirportIdent = "DEST", LengthFt = 10000, Surface = "ASPH" };
            var candidates = new List<AirportCandidate>
            {
                new(new Airport { ICAO = "SBBW", Tier = 1, Latitude = -15.8, Longitude = -52.3, Country = "BR" }, rwy), // ~550 NM
                new(new Airport { ICAO = "SBEG", Tier = 1, Latitude = -3.0, Longitude = -60.0, Country = "BR" }, rwy),  // ~1450 NM
                new(new Airport { ICAO = "KJFK", Tier = 1, Latitude = 40.6, Longitude = -73.8, Country = "US" }, rwy)   // ~4150 NM
            };

            var rng = new Random(1234);

            for (int i = 0; i < 500; i++)
            {
                var mission = MissionBoardCore.CreateSingleMission(
                    origin, paletizada, candidates, new HashSet<string>(), _ => new HashSet<string>(),
                    rng, DateTime.UtcNow);

                Assert.NotNull(mission);

                // Perfil 1: 300–1600 NM, 20.000–45.000 lbs
                // Perfil 2: 600–2600 NM, 45.000–100.000 lbs
                // Perfil 3: 3500–7200 NM, 100.000–148.000 lbs
                // Nunca deve misturar carga de 100k+ com distância de 400 NM!
                if (mission!.CargoWeightLbs > 100000)
                {
                    Assert.InRange(mission.DistanceNM, 3400, 7300);
                }
                else if (mission.CargoWeightLbs < 45000)
                {
                    Assert.InRange(mission.DistanceNM, 290, 1610);
                }
            }
        }

        [Fact]
        public void MissionBoardCore_BagagemPorPessoa()
        {
            var catalog = MissionCatalog.Shared;

            // 1. Instrução local: pax = 1 (aluno), tripulação = 1 (instrutor 2-6 kg). Sorteio individual (2+2) a (6+6) kg
            var instrucao = catalog.Get("instrucao_local")!;
            var rng = new Random(789);

            double lugInstrucao = MissionBoardCore.CalculateLuggageLbs(instrucao, 1, rng);
            // 4 a 12 kg * 2.20462 ~= 8.8 a 26.5 lbs
            Assert.InRange(lugInstrucao, 8.0, 28.0);

            // 2. Carga pura sem tripulação: bagagem = 0
            var overnight = catalog.Get("carga_overnight")!;
            double lugCargo = MissionBoardCore.CalculateLuggageLbs(overnight, 0, rng);
            Assert.Equal(0.0, lugCargo);

            // 3. Voo com 20 passageiros: distribuição individual de bagagem
            var troncal = catalog.Get("troncal_domestico")!;
            var bagagensIndividuais = new List<double>();
            for (int i = 0; i < 30; i++)
            {
                bagagensIndividuais.Add(MissionBoardCore.CalculateLuggageLbs(troncal, 20, rng));
            }

            // Não devem ser todos valores idênticos
            int distinct = bagagensIndividuais.Distinct().Count();
            Assert.True(distinct > 10, "Amostragem individual de bagagem deve produzir variação entre voos.");
        }

        [Fact]
        public void MissionBoardCore_CaminhoJogoEBotsIguais()
        {
            var origin = new Airport { ICAO = "SBGR", Tier = 1, Latitude = -23.4356, Longitude = -46.4731, Country = "BR" };
            var allAirports = new List<Airport>
            {
                origin,
                new() { ICAO = "SBKP", Tier = 2, Latitude = -23.0074, Longitude = -47.1345, Country = "BR" },
                new() { ICAO = "SBCF", Tier = 1, Latitude = -19.6244, Longitude = -43.9719, Country = "BR" },
                new() { ICAO = "SBGL", Tier = 1, Latitude = -22.8089, Longitude = -43.2436, Country = "BR" }
            };

            var refTime = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

            var run1 = MissionGenerator.GenerateMissionsInMemory(
                origin, allAirports, null, new Random(555), refTime, countOverride: 10);

            var run2 = MissionGenerator.GenerateMissionsInMemory(
                origin, allAirports, null, new Random(555), refTime, countOverride: 10);

            Assert.Equal(run1.Count, run2.Count);
            for (int i = 0; i < run1.Count; i++)
            {
                Assert.Equal(run1[i].SubId, run2[i].SubId);
                Assert.Equal(run1[i].DestinationICAO, run2[i].DestinationICAO);
                Assert.Equal(run1[i].PassengerCount, run2[i].PassengerCount);
                Assert.Equal(run1[i].CargoWeightLbs, run2[i].CargoWeightLbs);
                Assert.Equal(run1[i].LuggageWeightLbs, run2[i].LuggageWeightLbs);
                Assert.Equal(run1[i].RewardMoney, run2[i].RewardMoney);
            }
        }

        [Fact]
        public void MissionBoardCore_TierUmGeraLinhaECarga()
        {
            var origin = new Airport { ICAO = "SBGR", Tier = 1, Latitude = -23.4356, Longitude = -46.4731, Country = "BR" };
            var allAirports = new List<Airport>
            {
                origin,
                new() { ICAO = "SBGL", Tier = 1, Latitude = -22.8089, Longitude = -43.2436, Country = "BR" },
                new() { ICAO = "SBCF", Tier = 1, Latitude = -19.6244, Longitude = -43.9719, Country = "BR" },
                new() { ICAO = "SBKP", Tier = 2, Latitude = -23.0074, Longitude = -47.1345, Country = "BR" },
                new() { ICAO = "SBBR", Tier = 1, Latitude = -15.8692, Longitude = -47.9172, Country = "BR" },
                new() { ICAO = "SBEG", Tier = 1, Latitude = -3.0386, Longitude = -60.0497, Country = "BR" },
                new() { ICAO = "SAEZ", Tier = 1, Latitude = -34.8222, Longitude = -58.5358, Country = "AR" },
                new() { ICAO = "KJFK", Tier = 1, Latitude = 40.6398, Longitude = -73.7789, Country = "US" }
            };

            var rng = new Random(777);
            var refTime = DateTime.UtcNow;

            int countLinhaCarga = 0;
            int totalMissions = 0;

            var linhaCargaIds = new HashSet<string>
            {
                "troncal_domestico", "ponte_aerea", "regional_internacional", "longo_curso", "carga_paletizada"
            };

            var disabledIds = MissionCatalog.Shared.Missions.Values
                .Where(m => !m.Enabled)
                .Select(m => m.Id)
                .ToHashSet();

            for (int i = 0; i < 100; i++)
            {
                var missions = MissionGenerator.GenerateMissionsInMemory(
                    origin, allAirports, null, rng, refTime, countOverride: 20);

                foreach (var m in missions)
                {
                    totalMissions++;
                    Assert.DoesNotContain(m.SubId, disabledIds);

                    if (linhaCargaIds.Contains(m.SubId))
                    {
                        countLinhaCarga++;
                    }
                }
            }

            Assert.Equal(2000, totalMissions);
            double ratio = (double)countLinhaCarga / totalMissions;
            // Em Tier 1 (com etiqueta de linha/mega_hub em SBGR), linha + cargueiro deve ser a grande maioria
            Assert.True(ratio > 0.40, $"Esperava > 40% de linha e carga pesada em Tier 1, obteve {ratio:P1}");
        }
    }
}
