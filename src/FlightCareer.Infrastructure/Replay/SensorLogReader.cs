using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlightCareer.Domain.Replay;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.Infrastructure.Replay
{
    /// <summary>
    /// Registro de leitura de sensor com marcador operacional associado.
    /// </summary>
    public sealed record ReplayMarkerRecord(DateTime Utc, double ElapsedSeconds, string Marker);

    /// <summary>
    /// Dados completos carregados a partir de um log de sensores.
    /// </summary>
    public sealed record SensorLogData(
        IReadOnlyList<SensorFrame> Frames,
        IReadOnlyList<ReplayMarkerRecord> Markers);

    /// <summary>
    /// Leitor de arquivos de telemetria/sensores (.csv e .csv.gz) e arquivos de toques/blackbox.
    /// </summary>
    public static class SensorLogReader
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Lê um arquivo de telemetria (.csv ou .csv.gz) e devolve a sequência de SensorFrame.
        /// Trata BOM UTF-8 e cultura invariante.
        /// </summary>
        public static IReadOnlyList<SensorFrame> ReadFrames(string filePath)
        {
            return ReadLog(filePath).Frames;
        }

        /// <summary>
        /// Lê um arquivo de telemetria (.csv ou .csv.gz) e devolve tanto os frames quanto os marcadores encontrados.
        /// </summary>
        public static SensorLogData ReadLog(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Caminho do arquivo não pode ser nulo ou vazio.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Arquivo de log não encontrado: {filePath}");

            using var fileStream = File.OpenRead(filePath);
            using Stream inputStream = filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(fileStream, CompressionMode.Decompress)
                : fileStream;

            using var reader = new StreamReader(inputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return ReadLog(reader);
        }

        /// <summary>
        /// Lê o stream CSV e processa cabeçalho e linhas de dados.
        /// </summary>
        public static SensorLogData ReadLog(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            string? headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return new SensorLogData(Array.Empty<SensorFrame>(), Array.Empty<ReplayMarkerRecord>());
            }

            // Remove eventual BOM se não tiver sido consumido
            if (headerLine.Length > 0 && headerLine[0] == '\uFEFF')
            {
                headerLine = headerLine.Substring(1);
            }

            var headers = ParseCsvLine(headerLine);
            if (headers.Count < 2 || !string.Equals(headers[0], "utc", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Cabeçalho inválido no log CSV. Esperado 'utc,sim_rate,...', recebido: '{headerLine}'");
            }

            int utcIdx = 0;
            int simRateIdx = 1;
            int markerIdx = headers.FindIndex(h => string.Equals(h, "marker", StringComparison.OrdinalIgnoreCase));

            // Chaves de sensores (exclui utc, sim_rate e marker)
            var columnKeys = new string[headers.Count];
            for (int col = 0; col < headers.Count; col++)
            {
                if (col != utcIdx && col != simRateIdx && col != markerIdx)
                {
                    columnKeys[col] = headers[col];
                }
            }

            var frames = new List<SensorFrame>();
            var markers = new List<ReplayMarkerRecord>();
            DateTime? startUtc = null;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cells = ParseCsvLine(line);
                if (cells.Count < 2) continue;

                if (!DateTime.TryParse(cells[utcIdx], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc))
                {
                    continue;
                }

                startUtc ??= utc;
                double elapsedSec = (utc - startUtc.Value).TotalSeconds;

                double simRate = 1.0;
                if (cells.Count > simRateIdx && double.TryParse(cells[simRateIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRate))
                {
                    simRate = parsedRate;
                }

                var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                int limit = Math.Min(cells.Count, columnKeys.Length);

                for (int col = 0; col < limit; col++)
                {
                    string? key = columnKeys[col];
                    if (key == null) continue;

                    string cell = cells[col];
                    if (string.IsNullOrWhiteSpace(cell)) continue;

                    if (double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    {
                        values[key] = val;
                    }
                }

                frames.Add(new SensorFrame(values, utc, simRate));

                if (markerIdx >= 0 && markerIdx < cells.Count)
                {
                    string markerText = cells[markerIdx];
                    if (!string.IsNullOrWhiteSpace(markerText))
                    {
                        markers.Add(new ReplayMarkerRecord(utc, elapsedSec, markerText));
                    }
                }
            }

            return new SensorLogData(frames, markers);
        }

        /// <summary>
        /// Lê toques a partir de um arquivo de blackbox (linhas '# [EVENTO &lt;seg&gt;s] TOQUE: ...').
        /// </summary>
        public static IReadOnlyList<ReplayTouchdown> ReadTouchdownsFromBlackbox(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Caminho do arquivo não pode ser nulo ou vazio.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Arquivo de blackbox não encontrado: {filePath}");

            using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return ReadTouchdownsFromBlackbox(reader);
        }

        /// <summary>
        /// Lê toques a partir de um TextReader contendo o log do blackbox.
        /// </summary>
        public static IReadOnlyList<ReplayTouchdown> ReadTouchdownsFromBlackbox(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var result = new List<ReplayTouchdown>();
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (!line.Contains("TOQUE:", StringComparison.OrdinalIgnoreCase)) continue;

                var fpmMatch = Regex.Match(line, @"TOQUE:\s*([0-9\.\-]+)\s*fpm", RegexOptions.IgnoreCase);
                if (!fpmMatch.Success) continue;

                if (!double.TryParse(fpmMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double fpm))
                {
                    continue;
                }

                double? elapsedSec = null;
                var timeMatch = Regex.Match(line, @"\[EVENTO\s+([0-9\.]+)\s*s\]", RegexOptions.IgnoreCase);
                if (timeMatch.Success && double.TryParse(timeMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double sec))
                {
                    elapsedSec = sec;
                }

                double? pitch = null;
                var pitchMatch = Regex.Match(line, @"nariz\s*([0-9\.\-]+)°", RegexOptions.IgnoreCase);
                if (pitchMatch.Success && double.TryParse(pitchMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double p))
                {
                    pitch = p;
                }

                double? bank = null;
                var bankMatch = Regex.Match(line, @"inclina[çc][ãa]o\s*([0-9\.\-]+)°", RegexOptions.IgnoreCase);
                if (bankMatch.Success && double.TryParse(bankMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
                {
                    bank = b;
                }

                bool isBounce = line.Contains("BOUNCE", StringComparison.OrdinalIgnoreCase);

                result.Add(new ReplayTouchdown(
                    Fpm: fpm,
                    PitchDeg: pitch,
                    BankDeg: bank,
                    LateralFps: null,
                    IsBounce: isBounce,
                    ElapsedSeconds: elapsedSec));
            }

            return result;
        }

        /// <summary>
        /// Lê toques a partir de um arquivo JSON (.touchdowns.json).
        /// </summary>
        public static IReadOnlyList<ReplayTouchdown> ReadTouchdownsFromJson(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Caminho do arquivo não pode ser nulo ou vazio.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Arquivo de toques JSON não encontrado: {filePath}");

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return ReadTouchdownsFromJsonContent(json);
        }

        /// <summary>
        /// Desserializa toques a partir de conteúdo JSON.
        /// </summary>
        public static IReadOnlyList<ReplayTouchdown> ReadTouchdownsFromJsonContent(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<ReplayTouchdown>();

            var items = JsonSerializer.Deserialize<List<ReplayTouchdownDto>>(json, JsonOpts);
            if (items == null || items.Count == 0) return Array.Empty<ReplayTouchdown>();

            var result = new List<ReplayTouchdown>();
            foreach (var item in items)
            {
                result.Add(new ReplayTouchdown(
                    Fpm: item.Fpm,
                    PitchDeg: item.PitchDeg,
                    BankDeg: item.BankDeg,
                    LateralFps: item.LateralFps,
                    IsBounce: item.IsBounce,
                    ElapsedSeconds: item.ElapsedSeconds,
                    Utc: item.Utc));
            }

            return result;
        }

        private static List<string> ParseCsvLine(string line)
        {
            if (!line.Contains('"'))
            {
                return new List<string>(line.Split(','));
            }

            var cells = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    cells.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            cells.Add(sb.ToString());
            return cells;
        }

        private sealed class ReplayTouchdownDto
        {
            public double Fpm { get; set; }
            public double? PitchDeg { get; set; }
            public double? BankDeg { get; set; }
            public double? LateralFps { get; set; }
            public bool IsBounce { get; set; }
            public double? ElapsedSeconds { get; set; }
            public DateTime? Utc { get; set; }
        }
    }
}
