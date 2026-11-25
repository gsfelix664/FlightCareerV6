using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class CargoKindCatalogTests
    {
        private static readonly string[] CargoMissions =
        {
            "carga_paletizada",
            "carga_overnight",
            "malotes",
            "carga_valores",
            "ecommerce",
            "carga_jit",
            "cadeia_fria",
            "carga_dgr",
            "carga_sensivel",
            "insumos_agro",
            "suprimentos_remotos",
            "conexao_pista_curta",
            "humanitario",
            "combustivel_tambor",
            "apoio_acampamento"
        };

        [Fact]
        public void LoadDefault_CobreTodasAsMissoesDeCarga()
        {
            var catalog = CargoKindCatalog.Default;
            Assert.NotNull(catalog);

            foreach (var missionId in CargoMissions)
            {
                Assert.True(catalog.HasMission(missionId), $"Catálogo deve conter a missão '{missionId}'");
                var kinds = catalog.Missions[missionId];
                Assert.InRange(kinds.Count, 3, 8);
                Assert.Contains(kinds, k => k.Weight > 0);
            }
        }

        [Fact]
        public void LoadDefault_ClassesDgExistemNaTabela()
        {
            var catalog = CargoKindCatalog.Default;
            var dgCodes = catalog.DangerousGoods.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (missionId, kinds) in catalog.Missions)
            {
                foreach (var kind in kinds)
                {
                    if (kind.Dg != null)
                    {
                        Assert.Contains(kind.Dg, dgCodes);
                    }

                    if (string.Equals(missionId, "carga_dgr", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.NotNull(kind.Dg);
                    }

                    if (string.Equals(missionId, "combustivel_tambor", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Equal("3", kind.Dg);
                    }
                }
            }
        }

        [Fact]
        public void LoadDefault_EtiquetasExistemNosPerfis()
        {
            var catalog = CargoKindCatalog.Default;

            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "places", "airport_profiles.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "places", "airport_profiles.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "places", "airport_profiles.json")
            };
            var path = candidates.FirstOrDefault(File.Exists);
            Assert.NotNull(path);

            var validTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var doc = JsonDocument.Parse(File.ReadAllText(path)))
            {
                if (doc.RootElement.TryGetProperty("airports", out var airportsElem))
                {
                    foreach (var p in airportsElem.EnumerateObject())
                    {
                        if (p.Value.TryGetProperty("t", out var tElem) && tElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in tElem.EnumerateArray())
                            {
                                validTags.Add(item.GetString() ?? "");
                            }
                        }
                    }
                }
            }

            foreach (var kinds in catalog.Missions.Values)
            {
                foreach (var kind in kinds)
                {
                    foreach (var tag in kind.Tags.Keys)
                    {
                        if (tag.StartsWith("safra:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        Assert.Contains(tag, validTags);
                    }
                }
            }
        }

        [Fact]
        public void Pick_MesmaSemente_MesmoResultado()
        {
            var catalog = CargoKindCatalog.Default;
            var originTags = new HashSet<string> { "carga" };
            var destTags = new HashSet<string> { "carga:distribuicao" };
            int seed = 12345;

            var pick1 = catalog.Pick("carga_paletizada", originTags, destTags, seed);
            var pick2 = catalog.Pick("carga_paletizada", originTags, destTags, seed);

            Assert.NotNull(pick1);
            Assert.NotNull(pick2);
            Assert.Equal(pick1.KindId, pick2.KindId);
            Assert.Equal(pick1.Name, pick2.Name);
            Assert.Equal(pick1.Dg, pick2.Dg);
        }

        [Fact]
        public void Pick_SemEtiqueta_UsaSoPesoBase()
        {
            string testJson = @"{
                ""version"": 1,
                ""dangerousGoods"": {},
                ""missions"": {
                    ""teste_missao"": [
                        { ""id"": ""base_zero"", ""weight"": 0.0, ""tags"": { ""etiqueta_rara"": 10.0 }, ""names"": [""carga rara""], ""dg"": null },
                        { ""id"": ""base_positiva"", ""weight"": 1.0, ""tags"": {}, ""names"": [""carga padrao""], ""dg"": null }
                    ]
                }
            }";

            var catalog = CargoKindCatalog.LoadFromJson(testJson);

            // Sem a etiqueta rara, base_zero tem peso 0 e NUNCA pode ser sorteado
            for (int seed = 0; seed < 100; seed++)
            {
                var pick = catalog.Pick("teste_missao", null, null, seed);
                Assert.NotNull(pick);
                Assert.Equal("base_positiva", pick.KindId);
                Assert.Equal("carga padrao", pick.Name);
            }

            // Com a etiqueta rara, base_zero ganha peso 10 vs base_positiva peso 1
            var tags = new HashSet<string> { "etiqueta_rara" };
            int countZero = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var pick = catalog.Pick("teste_missao", tags, null, seed);
                if (pick?.KindId == "base_zero") countZero++;
            }
            Assert.True(countZero > 50, $"Esperava > 50 seleções de base_zero com tag, obteve {countZero}");
        }

        [Fact]
        public void Pick_EtiquetaDoDestino_FavoreceTipo()
        {
            var catalog = CargoKindCatalog.Default;
            var destTags = new HashSet<string> { "carga:mineracao" };
            int mineraçãoCount = 0;

            for (int seed = 0; seed < 200; seed++)
            {
                var pick = catalog.Pick("carga_dgr", null, destTags, seed);
                if (pick?.KindId == "explosivos_mineracao")
                {
                    mineraçãoCount++;
                }
            }

            // Com carga:mineracao (+6) e os demais somando peso base menor, explosivos deve sair > 50%
            Assert.True(mineraçãoCount > 100, $"Esperava > 50% (100 de 200) para mineração, obteve {mineraçãoCount}");
        }

        [Fact]
        public void Pick_MissaoSemEntrada_RetornaNull()
        {
            var catalog = CargoKindCatalog.Default;
            var pick = catalog.Pick("missao_inexistente_xyz", null, null, 42);
            Assert.Null(pick);
        }

        [Fact]
        public void StableSeed_IgualEntreChamadas()
        {
            int seed1 = CargoKindCatalog.StableSeed("SBGR", "SBKP", "carga_dgr");
            int seed2 = CargoKindCatalog.StableSeed("SBGR", "SBKP", "carga_dgr");

            Assert.Equal(seed1, seed2);
            Assert.Equal(-668890095, seed1);
        }

        [Fact]
        public void CheckManifest_DgSomenteCargueiroComPax_Bloqueia()
        {
            var m1 = new Mission
            {
                Title = "Voo DGR Explosivos",
                SubId = "carga_dgr",
                CargoKind = "explosivos_mineracao",
                PassengerCount = 0
            };

            var m2 = new Mission
            {
                Title = "Translado Pax",
                SubId = "transporte_passageiros",
                PassengerCount = 2
            };

            var reason = DangerousGoodsRules.CheckManifest(new[] { m1, m2 });
            Assert.NotNull(reason);
            Assert.Contains("explosivos", reason);
            Assert.Contains("1.1D", reason);
            Assert.Contains("2 a bordo", reason);
        }

        [Fact]
        public void CheckManifest_DgSemPax_Libera()
        {
            var m1 = new Mission
            {
                Title = "Voo DGR Explosivos",
                SubId = "carga_dgr",
                CargoKind = "explosivos_mineracao",
                PassengerCount = 0
            };

            var reason = DangerousGoodsRules.CheckManifest(new[] { m1 });
            Assert.Null(reason);
        }

        [Fact]
        public void CheckManifest_DgPermitidoComPax_Libera()
        {
            // Classe 6.2 (substância infecciosa) tem cargoAircraftOnly: false
            var m1 = new Mission
            {
                Title = "Amostras Biológicas",
                SubId = "carga_dgr",
                CargoKind = "amostras_diagnostico_infecciosas",
                PassengerCount = 0
            };

            var m2 = new Mission
            {
                Title = "Translado Médico",
                SubId = "transporte_passageiros",
                PassengerCount = 3
            };

            var reason = DangerousGoodsRules.CheckManifest(new[] { m1, m2 });
            Assert.Null(reason);
        }

        [Fact]
        public void CheckManifest_SemDg_Libera()
        {
            var m1 = new Mission
            {
                Title = "Carga Paletizada",
                SubId = "carga_paletizada",
                CargoKind = "carga_geral_consol",
                PassengerCount = 0
            };

            var m2 = new Mission
            {
                Title = "Passageiros",
                SubId = "transporte_passageiros",
                PassengerCount = 4
            };

            var reason = DangerousGoodsRules.CheckManifest(new[] { m1, m2 });
            Assert.Null(reason);
        }

        [Fact]
        public void CreateMissionObjectSync_CargaDgr_PreencheCargoEDescricao()
        {
            var origin = new Airport { ICAO = "SBGR", Name = "Guarulhos", Tier = 1, Latitude = -23.4, Longitude = -46.4 };
            var dest = new Airport { ICAO = "SBCF", Name = "Confins", Tier = 1, Latitude = -19.6, Longitude = -43.9 };
            var def = new MissionDefinition { Id = "carga_dgr", Name = "Carga Perigosa", MinExpHours = 2, MaxExpHours = 6, Urgency = 1.0 };
            var destTags = new HashSet<string> { "carga:mineracao" };

            var mission = MissionGenerator.CreateMissionObjectSync(
                origin: origin,
                dest: dest,
                dist: 200,
                sub: def,
                paxCount: 0,
                luggageWeightLbs: 0,
                cargoWeight: 1500,
                random: new Random(42),
                referenceTime: new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc),
                destTags: destTags);

            Assert.NotNull(mission.CargoKind);
            Assert.NotNull(mission.CargoName);
            Assert.Contains(mission.CargoName, mission.Description);
            Assert.Contains("Carga perigosa classe", mission.Description);
        }
    }
}
