using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace FlightCareer.Domain.Sensors.Probe
{
    public static class ProbeCsvWriter
    {
        public static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        public static string EscapeField(string? field)
        {
            if (field == null)
            {
                return string.Empty;
            }

            bool needsQuotes = field.Contains(',') ||
                               field.Contains('"') ||
                               field.Contains('\n') ||
                               field.Contains('\r');

            if (!needsQuotes)
            {
                return field;
            }

            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        public static string FormatDouble(double value)
        {
            return value.ToString("G", CultureInfo.InvariantCulture);
        }

        public static string FormatCsvRow(IEnumerable<string?> fields)
        {
            var sb = new StringBuilder();
            bool first = true;

            foreach (var f in fields)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                first = false;
                sb.Append(EscapeField(f));
            }

            return sb.ToString();
        }

        public static void WriteAllLines(string filePath, IEnumerable<string> lines)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Caminho não pode ser vazio.", nameof(filePath));
            }

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var writer = new StreamWriter(filePath, append: false, Utf8WithBom);
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }
    }
}
