using System;
using System.Collections.Generic;
using System.Globalization;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Sensors
{
    public class SensorCsvFormatterTests
    {
        [Fact]
        public void Header_HasUtcSimRateKeysAndMarker()
        {
            var keys = new[] { "eng-rpm:1", "eng-cht:1", "oil-press:1" };
            string header = SensorCsvFormatter.Header(keys);

            Assert.Equal("utc,sim_rate,eng-rpm:1,eng-cht:1,oil-press:1,marker", header);
        }

        [Fact]
        public void Header_EmptyKeys_HasUtcSimRateAndMarker()
        {
            string header = SensorCsvFormatter.Header(Array.Empty<string>());
            Assert.Equal("utc,sim_rate,marker", header);
        }

        [Fact]
        public void Row_UsesInvariantCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                // Força cultura brasileira com vírgula como separador decimal
                CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

                var utc = new DateTime(2026, 11, 14, 12, 30, 45, 123, DateTimeKind.Utc);
                var values = new Dictionary<string, double>
                {
                    { "eng-rpm:1", 2450.75 },
                    { "eng-cht:1", 185.5 }
                };
                var frame = new SensorFrame(values, utc, SimulationRate: 2.5);
                var keys = new[] { "eng-rpm:1", "eng-cht:1" };

                string row = SensorCsvFormatter.Row(utc, frame, keys, marker: "normal");

                // Confere que usa ponto decimal e formato ISO 8601
                Assert.Contains("2.5", row);
                Assert.Contains("2450.75", row);
                Assert.Contains("185.5", row);
                Assert.DoesNotContain("2,5", row);
                Assert.DoesNotContain("2450,75", row);
                Assert.DoesNotContain("185,5", row);
                Assert.StartsWith("2026-11-14T12:30:45.1230000Z,2.5,2450.75,185.5,normal", row);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Fact]
        public void Row_MissingKey_IsEmptyCell()
        {
            var utc = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);
            var values = new Dictionary<string, double>
            {
                { "eng-rpm:1", 2300.0 }
                // "eng-cht:1" ausente
            };
            var frame = new SensorFrame(values, utc, SimulationRate: 1.0);
            var keys = new[] { "eng-rpm:1", "eng-cht:1" };

            string row = SensorCsvFormatter.Row(utc, frame, keys, marker: null);

            // Célula eng-cht:1 deve ser vazia: 2300,,
            Assert.EndsWith(",2300,,", row);
        }

        [Fact]
        public void Row_NaN_IsEmptyCell()
        {
            var utc = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);
            var values = new Dictionary<string, double>
            {
                { "eng-rpm:1", double.NaN },
                { "eng-cht:1", double.PositiveInfinity },
                { "oil-press:1", 55.0 }
            };
            var frame = new SensorFrame(values, utc, SimulationRate: 1.0);
            var keys = new[] { "eng-rpm:1", "eng-cht:1", "oil-press:1" };

            string row = SensorCsvFormatter.Row(utc, frame, keys, marker: null);

            // eng-rpm:1 (NaN) e eng-cht:1 (Infinity) vazios
            Assert.Contains(",,,55,", row);
        }

        [Fact]
        public void Row_MarkerWithCommaAndQuotes_IsEscaped()
        {
            var utc = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);
            var frame = new SensorFrame(new Dictionary<string, double>(), utc, SimulationRate: 1.0);
            var keys = Array.Empty<string>();

            string marker = "Evento \"crítico\", com vírgula e \n quebra";
            string row = SensorCsvFormatter.Row(utc, frame, keys, marker);

            string expectedEscaped = "\"Evento \"\"crítico\"\", com vírgula e \n quebra\"";
            Assert.EndsWith("," + expectedEscaped, row);
        }

        [Fact]
        public void SafeFileName_RemovesInvalidChars_AndLimitsLength()
        {
            string raw = "Cessna 152 \"Asobo\" / v2:1";
            string safe = SensorCsvFormatter.SafeFileName(raw);

            // Não pode conter ", /, :
            Assert.DoesNotContain("\"", safe);
            Assert.DoesNotContain("/", safe);
            Assert.DoesNotContain(":", safe);
            Assert.DoesNotContain(" ", safe);

            // Título com mais de 200 caracteres deve ser truncado em exatamente 60
            string longTitle = new string('A', 200);
            string safeLong = SensorCsvFormatter.SafeFileName(longTitle);
            Assert.Equal(60, safeLong.Length);

            // Título nulo, vazio ou somente inválidos deve retornar UNKNOWN
            Assert.Equal("UNKNOWN", SensorCsvFormatter.SafeFileName(null));
            Assert.Equal("UNKNOWN", SensorCsvFormatter.SafeFileName(""));
            Assert.Equal("UNKNOWN", SensorCsvFormatter.SafeFileName("   "));
            Assert.Equal("UNKNOWN", SensorCsvFormatter.SafeFileName(":::\"\"///"));
        }
    }
}
