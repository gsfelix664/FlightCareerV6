using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using FlightCareer.Domain.Sensors.Probe;
using Xunit;

namespace FlightCareer.Tests.Sensors.Probe
{
    public class ProbeDomainTests
    {
        private static string GetRealCsvPath()
        {
            string inBase = Path.Combine(AppContext.BaseDirectory, "Data", "simvar_probe_list.csv");
            if (File.Exists(inBase))
            {
                return inBase;
            }

            // Fallback caso rode da raiz
            string fallback = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "simvar_probe_list.csv");
            if (File.Exists(fallback))
            {
                return Path.GetFullPath(fallback);
            }

            throw new FileNotFoundException("simvar_probe_list.csv não encontrado.");
        }

        [Fact]
        public void Loader_CsvComAspasEComentario_LeTodasAsLinhasValidas()
        {
            string csvContent =
                "# Este é um comentário\n" +
                "simvar,unit,isString,indexKind,source,category,settable\n" +
                "\n" +
                "ENG RPM,rpm,false,engine,doc,\"Motor, Principal\",true\n" +
                "# Outro comentário\n" +
                "TITLE,,true,none,doc,\"Texto \"\"Especial\"\"\",false\n";

            using var reader = new StringReader(csvContent);
            var entries = ProbeListLoader.Load(reader);

            Assert.Equal(2, entries.Count);

            var first = entries[0];
            Assert.Equal("ENG RPM", first.SimVar);
            Assert.Equal("rpm", first.Unit);
            Assert.False(first.IsString);
            Assert.Equal(ProbeIndexKind.Engine, first.IndexKind);
            Assert.Equal("doc", first.Source);
            Assert.Equal("Motor, Principal", first.Category);
            Assert.True(first.Settable);

            var second = entries[1];
            Assert.Equal("TITLE", second.SimVar);
            Assert.Equal(string.Empty, second.Unit);
            Assert.True(second.IsString);
            Assert.Equal(ProbeIndexKind.None, second.IndexKind);
            Assert.Equal("Texto \"Especial\"", second.Category);
            Assert.False(second.Settable);
        }

        [Fact]
        public void Loader_ArquivoReal_CarregaMaisDe1700Entradas()
        {
            string path = GetRealCsvPath();
            var entries = ProbeListLoader.Load(path);

            Assert.True(entries.Count >= 1700, $"Esperado >= 1700 entradas, mas carregou {entries.Count}");

            foreach (var e in entries)
            {
                Assert.False(string.IsNullOrWhiteSpace(e.SimVar), "Nenhum nome de SimVar pode ser vazio.");
                if (!e.IsString)
                {
                    Assert.False(string.IsNullOrWhiteSpace(e.Unit), $"SimVar numérica '{e.SimVar}' não pode ter unidade vazia.");
                }
            }
        }

        [Fact]
        public void Expander_Engine_GeraUmSlotPorMotor()
        {
            var entry = new ProbeEntry("ENG RPM", "rpm", false, ProbeIndexKind.Engine, "doc", "Eng", true);
            var options = new ProbeOptions(MaxEngine: 2);

            var slots = ProbeSlotExpander.Expand(new[] { entry }, options);

            Assert.Equal(2, slots.Count);
            Assert.Equal("ENG RPM:1", slots[0].Name);
            Assert.Equal(1, slots[0].Index);
            Assert.Equal("ENG RPM:2", slots[1].Name);
            Assert.Equal(2, slots[1].Index);
        }

        [Fact]
        public void Expander_Generic_IncluiSemIndiceEZeroAteMax()
        {
            var entry = new ProbeEntry("CAMERA VIEW", "Enum", false, ProbeIndexKind.Generic, "doc", "Cam", false);
            var options = new ProbeOptions(MaxGeneric: 2);

            var slots = ProbeSlotExpander.Expand(new[] { entry }, options);

            Assert.Equal(4, slots.Count);
            Assert.Equal("CAMERA VIEW", slots[0].Name);
            Assert.Null(slots[0].Index);

            Assert.Equal("CAMERA VIEW:0", slots[1].Name);
            Assert.Equal(0, slots[1].Index);

            Assert.Equal("CAMERA VIEW:1", slots[2].Name);
            Assert.Equal(1, slots[2].Index);

            Assert.Equal("CAMERA VIEW:2", slots[3].Name);
            Assert.Equal(2, slots[3].Index);
        }

        [Fact]
        public void Expander_WearCoverMulti_ContagensFixas()
        {
            var wear = new ProbeEntry("WEAR DAMAGE", "percent", false, ProbeIndexKind.Wear, "doc", "Wear", false);
            var cover = new ProbeEntry("COVER OPEN", "Bool", false, ProbeIndexKind.Cover, "doc", "Cover", true);
            var multi = new ProbeEntry("MULTI INDEX VAR", "number", false, ProbeIndexKind.Multi, "doc", "Multi", false);

            var slots = ProbeSlotExpander.Expand(new[] { wear, cover, multi });

            var wearSlots = slots.Where(s => s.Entry.IndexKind == ProbeIndexKind.Wear).ToList();
            Assert.Equal(50, wearSlots.Count);
            Assert.Equal("WEAR DAMAGE:0", wearSlots.First().Name);
            Assert.Equal("WEAR DAMAGE:49", wearSlots.Last().Name);

            var coverSlots = slots.Where(s => s.Entry.IndexKind == ProbeIndexKind.Cover).ToList();
            Assert.Equal(8, coverSlots.Count);
            Assert.Equal("COVER OPEN:0", coverSlots.First().Name);
            Assert.Equal("COVER OPEN:7", coverSlots.Last().Name);

            var multiSlots = slots.Where(s => s.Entry.IndexKind == ProbeIndexKind.Multi).ToList();
            Assert.Single(multiSlots);
            Assert.Equal("MULTI INDEX VAR", multiSlots[0].Name);
            Assert.Null(multiSlots[0].Index);
        }

        [Fact]
        public void Expander_Filtro_ManteSoOsQueCasam()
        {
            var e1 = new ProbeEntry("GENERAL ENG RPM", "rpm", false, ProbeIndexKind.Engine, "doc", "Eng", false);
            var e2 = new ProbeEntry("AIRSPEED INDICATED", "knots", false, ProbeIndexKind.None, "doc", "Speed", false);
            var options = new ProbeOptions(MaxEngine: 2, Filter: "^AIRSPEED");

            var slots = ProbeSlotExpander.Expand(new[] { e1, e2 }, options);

            Assert.Single(slots);
            Assert.Equal("AIRSPEED INDICATED", slots[0].Name);
        }

        [Fact]
        public void Expander_NomesUnicos()
        {
            string path = GetRealCsvPath();
            var entries = ProbeListLoader.Load(path);
            var options = new ProbeOptions();

            var slots = ProbeSlotExpander.Expand(entries, options);
            var duplicates = slots
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void Planner_ExcluiRejeitadasEStringsERespeitaTamanho()
        {
            var dummyEntryNum = new ProbeEntry("NUM", "number", false, ProbeIndexKind.None, "doc", "cat", false);
            var dummyEntryStr = new ProbeEntry("STR", "", true, ProbeIndexKind.None, "doc", "cat", false);

            var slots = new List<ProbeSlot>();

            // 445 slots aceitos (total 450 com 3 rejeitados e 2 strings)
            for (int i = 0; i < 445; i++)
            {
                slots.Add(new ProbeSlot($"NUM_{i}", dummyEntryNum, null));
            }

            // 3 rejeitados
            for (int i = 0; i < 3; i++)
            {
                slots.Add(new ProbeSlot($"REJ_{i}", dummyEntryNum, null));
            }

            // 2 strings
            slots.Add(new ProbeSlot("STR_1", dummyEntryStr, null));
            slots.Add(new ProbeSlot("STR_2", dummyEntryStr, null));

            var rejectedSet = new HashSet<string> { "REJ_0", "REJ_1", "REJ_2" };

            var blocks = ProbeBlockPlanner.Plan(slots, rejectedSlotNames: rejectedSet, blockSize: 200);

            Assert.Equal(3, blocks.Count);
            Assert.Equal(200, blocks[0].Slots.Count);
            Assert.Equal(200, blocks[1].Slots.Count);
            Assert.Equal(45, blocks[2].Slots.Count);

            foreach (var b in blocks)
            {
                foreach (var s in b.Slots)
                {
                    Assert.False(s.Entry.IsString, "Blocos numéricos não podem conter strings.");
                    Assert.DoesNotContain(s.Name, rejectedSet);
                }
            }
        }

        [Fact]
        public void Stats_ValoresIguais_AceitaConstante()
        {
            var stats = new ProbeSlotStats("ENG_RPM", isString: false);
            stats.RecordNumeric(2400.0);
            stats.RecordNumeric(2400.0);
            stats.RecordNumeric(2400.0);

            Assert.Equal(3, stats.Samples);
            Assert.Equal(1, stats.DistinctCount);
            Assert.Equal(2400.0, stats.Min);
            Assert.Equal(2400.0, stats.Max);
            Assert.Equal(ProbeStatus.AceitaConstante, stats.EvaluateAcceptedStatus());
        }

        [Fact]
        public void Stats_ValoresDiferentes_AceitaVariando()
        {
            var stats = new ProbeSlotStats("ENG_RPM", isString: false);
            stats.RecordNumeric(2400.0);
            stats.RecordNumeric(2450.0);

            Assert.Equal(2, stats.Samples);
            Assert.Equal(2, stats.DistinctCount);
            Assert.Equal(2400.0, stats.Min);
            Assert.Equal(2450.0, stats.Max);
            Assert.Equal(ProbeStatus.AceitaVariando, stats.EvaluateAcceptedStatus());
        }

        [Fact]
        public void Stats_NaNContaComoNulo()
        {
            var stats = new ProbeSlotStats("VAR_NAN", isString: false);
            stats.RecordNumeric(double.NaN);

            Assert.Equal(0, stats.Samples);
            Assert.Equal(1, stats.Nulls);
            Assert.Null(stats.Min);
            Assert.Null(stats.Max);
            Assert.Equal(ProbeStatus.AceitaSemAmostras, stats.EvaluateAcceptedStatus());
        }

        [Fact]
        public void Stats_SemAmostras_AceitaSemAmostras()
        {
            var stats = new ProbeSlotStats("EMPTY", isString: false);
            Assert.Equal(0, stats.Samples);
            Assert.Equal(ProbeStatus.AceitaSemAmostras, stats.EvaluateAcceptedStatus());
        }

        [Fact]
        public void Report_RejeitadaTemPrioridadeSobreAmostras()
        {
            var entry = new ProbeEntry("BAD_VAR", "number", false, ProbeIndexKind.None, "doc", "cat", false);
            var slot = new ProbeSlot("BAD_VAR", entry, null);

            var stats = new ProbeSlotStats("BAD_VAR", isString: false);
            stats.RecordNumeric(10.0);
            stats.RecordNumeric(20.0);

            var validation = ProbeValidationResult.Rejected("NAME_UNRECOGNIZED");

            var status = ProbeReportBuilder.DetermineStatus(validation, stats);
            Assert.Equal(ProbeStatus.Rejeitada, status);

            var accumulator = new ProbeStatsAccumulator();
            accumulator.RecordNumeric("BAD_VAR", 10.0);
            accumulator.RecordNumeric("BAD_VAR", 20.0);

            var valDict = new Dictionary<string, ProbeValidationResult> { ["BAD_VAR"] = validation };
            var rows = ProbeReportBuilder.BuildRows(new[] { slot }, valDict, accumulator);

            Assert.Single(rows);
            Assert.Equal(ProbeStatus.Rejeitada, rows[0].Status);
            Assert.Equal("NAME_UNRECOGNIZED", rows[0].Exception);
        }

        [Fact]
        public void CsvWriter_EscapaVirgulaAspasEQuebra()
        {
            string fieldWithComma = "Motor, 1";
            string fieldWithQuotes = "Texto \"Citado\"";
            string fieldWithNewline = "Linha1\nLinha2";

            Assert.Equal("\"Motor, 1\"", ProbeCsvWriter.EscapeField(fieldWithComma));
            Assert.Equal("\"Texto \"\"Citado\"\"\"", ProbeCsvWriter.EscapeField(fieldWithQuotes));
            Assert.Equal("\"Linha1\nLinha2\"", ProbeCsvWriter.EscapeField(fieldWithNewline));

            string row = ProbeCsvWriter.FormatCsvRow(new[] { fieldWithComma, fieldWithQuotes, "Normal" });
            Assert.Equal("\"Motor, 1\",\"Texto \"\"Citado\"\"\",Normal", row);
        }

        [Fact]
        public void CsvWriter_UsaCulturaInvariante()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");

                double val = 1234.56;
                string formatted = ProbeCsvWriter.FormatDouble(val);

                Assert.Equal("1234.56", formatted);
                Assert.DoesNotContain(",", formatted);

                string row = ProbeCsvWriter.FormatCsvRow(new[] { formatted, "teste" });
                Assert.Equal("1234.56,teste", row);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }
    }
}
