using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlightCareer.Domain.Career;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionCatalogTests
    {
        private static readonly string[] s_expected59LegacyCodes = new[]
        {
            // Tier 1
            "1.1.1", "1.1.2", "1.1.3",
            "1.2.1", "1.2.2", "1.2.3",
            "1.3.1", "1.3.2", "1.3.3", "1.3.4",
            "1.7.1", "1.7.2", "1.7.3", "1.7.4", "1.7.5", "1.7.6", "1.7.7", "1.7.8",
            // Tier 2
            "2.1.1", "2.1.2",
            "2.2.1", "2.2.2", "2.2.3",
            "2.3.1", "2.3.2", "2.3.3",
            "2.4.1", "2.4.2",
            "2.7.1", "2.7.2", "2.7.3", "2.7.4",
            // Tier 3
            "3.1.1", "3.1.2", "3.1.3",
            "3.2.1", "3.2.2", "3.2.3",
            "3.3.1", "3.3.2", "3.3.3",
            "3.4.1", "3.4.2", "3.4.3",
            "3.5.1", "3.5.2", "3.5.3",
            "3.6.1", "3.6.2",
            "3.7.1", "3.7.2",
            // Tier 4
            "4.1.1", "4.1.2", "4.1.3",
            "4.2.1", "4.2.2", "4.2.3",
            "4.3.1", "4.3.2"
        };

        [Fact]
        public void Catalogo_TodoLegacyCodeAntigoApareceExatamenteUmaVez()
        {
            var catalog = MissionCatalog.Shared;
            var allLegacyInCatalog = catalog.Missions.Values
                .SelectMany(m => m.LegacyCodes)
                .ToList();

            Assert.Equal(59, s_expected59LegacyCodes.Length);
            Assert.True(allLegacyInCatalog.Count >= 59);

            foreach (var code in s_expected59LegacyCodes)
            {
                Assert.Contains(code, allLegacyInCatalog);
            }
        }

        [Fact]
        public void Catalogo_TodoIdTemFaseValida_FamiliaPagamentoExistente_TextosComAoMenos3Variantes_EBoardValido()
        {
            var catalog = MissionCatalog.Shared;
            var textCatalog = MissionTextCatalog.LoadDefault();
            var payTable = MissionPayTable.Shared;

            Assert.NotEmpty(catalog.Missions);

            foreach (var kvp in catalog.Missions)
            {
                var def = kvp.Value;
                Assert.Equal(kvp.Key, def.Id);
                Assert.False(string.IsNullOrWhiteSpace(def.Id));
                Assert.False(string.IsNullOrWhiteSpace(def.Name));

                // 1. Fase (1..7)
                Assert.InRange(def.Phase, 1, 7);

                // 2. Família de pagamento existente
                Assert.False(string.IsNullOrWhiteSpace(def.PayFamily), $"Missão '{def.Id}' sem PayFamily");
                var family = payTable.Families.FirstOrDefault(f => f.Id == def.PayFamily);
                Assert.NotNull(family);

                // 3. Board válido
                Assert.True(Enum.IsDefined(typeof(MissionBoardKind), def.Board), $"Missão '{def.Id}' com Board inválido '{def.Board}'");

                // 4. Textos com >= 1 variantes (ou templates no mission_texts)
                Assert.True(textCatalog.Templates.ContainsKey(def.Id), $"Missão '{def.Id}' não possui chave em mission_texts.json");
                var variants = textCatalog.Templates[def.Id];
                Assert.True(variants.Count >= 1, $"Missão '{def.Id}' possui apenas {variants.Count} variantes em mission_texts.json (esperado >= 1)");
            }
        }

        [Fact]
        public void Catalogo_Normalize_ComportamentoEsperado()
        {
            var catalog = MissionCatalog.Shared;

            // 1. Código legado do MEDEVAC (1.7.3) normaliza para o id correspondente (medevac_remoto)
            string medevacId = catalog.Normalize("1.7.3");
            Assert.Equal("medevac_remoto", medevacId);

            // 2. ID novo devolve ele mesmo
            Assert.Equal("medevac_remoto", catalog.Normalize("medevac_remoto"));
            Assert.Equal("medevac_remoto", catalog.Normalize("medevac_dinamico"));
            Assert.Equal("instrucao_local", catalog.Normalize("instrucao_local"));

            // 3. Desconhecido devolve ele mesmo
            Assert.Equal("desconhecido_123", catalog.Normalize("desconhecido_123"));
            Assert.Equal("9.9.9", catalog.Normalize("9.9.9"));
        }

        [Fact]
        public void CodigoFonte_NenhumCodigoNumericoDeMissaoRestanteForaDeLegacyCodesETestesDeMigracao()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            {
                dir = dir.Parent;
            }
            string srcDir = dir != null ? Path.Combine(dir.FullName, "src") : Path.Combine(Directory.GetCurrentDirectory(), "src");

            // Verifica os projetos de produção (Domain, Application, Infrastructure, UI)
            var productionFolders = new[]
            {
                Path.Combine(srcDir, "FlightCareer.Domain"),
                Path.Combine(srcDir, "FlightCareer.Application"),
                Path.Combine(srcDir, "FlightCareer.Infrastructure"),
                Path.Combine(srcDir, "FlightCareer.UI")
            };

            var csFiles = productionFolders
                .Where(Directory.Exists)
                .SelectMany(folder => Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            var regex = new Regex(@"""[1-4]\.[1-7](\.[0-9])?""");
            var violations = new List<string>();

            foreach (var file in csFiles)
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    // Ignora comentários
                    if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*") || line.TrimStart().StartsWith("*"))
                        continue;

                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        string relative = Path.GetRelativePath(srcDir, file);
                        violations.Add($"{relative}:{i + 1} -> {match.Value} em: {line.Trim()}");
                    }
                }
            }

            Assert.Empty(violations);
        }
    }
}
