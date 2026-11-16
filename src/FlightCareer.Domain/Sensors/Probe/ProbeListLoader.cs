using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FlightCareer.Domain.Sensors.Probe
{
    public static class ProbeListLoader
    {
        public static IReadOnlyList<ProbeEntry> Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Caminho do arquivo não pode ser vazio.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Arquivo de lista não encontrado: {filePath}", filePath);
            }

            using var reader = new StreamReader(filePath, Encoding.UTF8);
            return Load(reader);
        }

        public static IReadOnlyList<ProbeEntry> Load(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var entries = new List<ProbeEntry>();
            string? line;
            bool isFirstLine = true;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (trimmed.StartsWith('#'))
                {
                    continue;
                }

                var fields = ParseCsvLine(line);
                if (fields.Count == 0)
                {
                    continue;
                }

                // Cabeçalho
                if (isFirstLine && string.Equals(fields[0].Trim(), "simvar", StringComparison.OrdinalIgnoreCase))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;

                if (fields.Count < 4)
                {
                    continue;
                }

                string simvar = fields[0].Trim();
                if (string.IsNullOrWhiteSpace(simvar))
                {
                    continue;
                }

                string unit = fields.Count > 1 ? fields[1].Trim() : string.Empty;
                bool isString = fields.Count > 2 && bool.TryParse(fields[2].Trim(), out bool bString) && bString;

                if (!isString && string.IsNullOrWhiteSpace(unit))
                {
                    unit = "number";
                }

                ProbeIndexKind indexKind = ProbeIndexKind.None;
                if (fields.Count > 3)
                {
                    string kindStr = fields[3].Trim();
                    if (Enum.TryParse<ProbeIndexKind>(kindStr, true, out var parsedKind))
                    {
                        indexKind = parsedKind;
                    }
                }

                string source = fields.Count > 4 ? fields[4].Trim() : string.Empty;
                string category = fields.Count > 5 ? fields[5].Trim() : string.Empty;

                bool? settable = null;
                if (fields.Count > 6)
                {
                    string settableStr = fields[6].Trim();
                    if (!string.IsNullOrEmpty(settableStr) && bool.TryParse(settableStr, out bool parsedSettable))
                    {
                        settable = parsedSettable;
                    }
                }

                entries.Add(new ProbeEntry(simvar, unit, isString, indexKind, source, category, settable));
            }

            return entries;
        }

        public static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (insideQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++; // Pula aspas escapada
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        insideQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }

            fields.Add(currentField.ToString());
            return fields;
        }
    }
}
