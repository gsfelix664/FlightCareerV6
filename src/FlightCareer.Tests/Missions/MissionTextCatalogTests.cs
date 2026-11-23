using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionTextCatalogTests
    {
        [Fact]
        public void LoadDefault_All59SubtypesPresent_EachHasAtLeast3Variants()
        {
            var catalog = MissionTextCatalog.LoadDefault();
            Assert.True(catalog.SubtypesCount >= 59, $"Esperado ao menos 59 subtipos, encontrado {catalog.SubtypesCount}");

            foreach (var kvp in catalog.Templates)
            {
                var subId = kvp.Key;
                var list = kvp.Value;
                Assert.True(list.Count >= 3, $"Subtipo {subId} possui apenas {list.Count} variantes (esperado >= 3).");

                foreach (var variant in list)
                {
                    Assert.False(string.IsNullOrWhiteSpace(variant.Title), $"Subtipo {subId} possui variante com título vazio.");
                    Assert.False(string.IsNullOrWhiteSpace(variant.Description), $"Subtipo {subId} possui variante com descrição vazia.");
                }
            }
        }

        [Fact]
        public void Render_ReplacesPlaceholdersAndCleansUnsetBraces()
        {
            var catalog = MissionTextCatalog.LoadDefault();
            var random = new Random(42);

            var values = new Dictionary<string, string>
            {
                ["origin"] = "SBJD",
                ["dest"] = "SBSP",
                ["pax"] = "4",
                ["cargo_lbs"] = "500",
                ["distance_nm"] = "42",
                ["deadline"] = "2h"
            };

            var (title, desc) = catalog.Render("1.1.1", random, values);

            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.False(string.IsNullOrWhiteSpace(desc));

            // Não deve haver nenhum marcador {exemplo} não resolvido
            Assert.DoesNotMatch(@"\{[a-zA-Z0-9_]+\}", title);
            Assert.DoesNotMatch(@"\{[a-zA-Z0-9_]+\}", desc);

            // Valores fornecidos devem estar presentes no texto renderizado
            Assert.Contains("SBJD", desc + title);
        }

        [Fact]
        public void Render_Deterministic_SameSeedProducesSameText()
        {
            var catalog = MissionTextCatalog.LoadDefault();
            var values = new Dictionary<string, string>
            {
                ["origin"] = "SBJD",
                ["dest"] = "SBJD",
                ["area_dir"] = "Norte",
                ["area_nm"] = "10",
                ["alt_band"] = "3.500 ft AGL",
                ["maneuvers"] = "curvas de 45°",
                ["tgl"] = "2"
            };

            var (title1, desc1) = catalog.Render("3.4.1", new Random(12345), values);
            var (title2, desc2) = catalog.Render("3.4.1", new Random(12345), values);

            Assert.Equal(title1, title2);
            Assert.Equal(desc1, desc2);
        }

        [Fact]
        public void Variantes_turisticas_ficam_so_na_chave_propria()
        {
            var catalog = MissionTextCatalog.LoadDefault();

            Assert.True(catalog.Templates["voo_panoramico-turistico"].Count >= 2);
            Assert.All(catalog.Templates["voo_panoramico"], v =>
                Assert.DoesNotContain("{scenic_", v.Title + v.Description));
        }
    }
}
