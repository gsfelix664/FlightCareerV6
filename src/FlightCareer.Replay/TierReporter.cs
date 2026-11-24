using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using FlightCareer.Domain.Airports;
using FlightCareer.Domain.Geo;
using FlightCareer.Domain.Missions;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace FlightCareer.Replay
{
    public sealed class TierReportOptions
    {
        public string? LnmPath { get; set; }
        public string? CsvPath { get; set; }
        public string OutputDirectory { get; set; } = ".";
    }

    public static class TierReporter
    {
        public sealed record CsvItem(string Ident, string Type, bool Scheduled, double? Lat, double? Lon, string? IsoCountry);

        public sealed record CountryResolutionStats(
            int TotalInvalid,
            int ResolvedByBorder,
            int ResolvedByNeighbor,
            int RemainedEmpty,
            List<AirportClassified> ExamplesByBorder
        );

        public sealed record AirportClassified(
            string ICAO,
            string Name,
            string City,
            string Country,
            double Lat,
            double Lon,
            int LongestRunwayFt,
            bool LongestRunwayPaved,
            int OldTier,
            AirportTierScore ScoreDetails
        )
        {
            public int NewTier => ScoreDetails.Tier;
            public int DeltaTier => NewTier - OldTier;
        }

        public static int Run(string[] args)
        {
            var options = new TierReportOptions();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--lnm", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.LnmPath = args[++i];
                }
                else if (args[i].Equals("--csv", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.CsvPath = args[++i];
                }
                else if (args[i].Equals("--out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.OutputDirectory = args[++i];
                }
            }

            options.LnmPath ??= DataSeeder.LocateLittleNavmapDatabase();
            if (string.IsNullOrEmpty(options.LnmPath) || !File.Exists(options.LnmPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[tier-report] Erro: Banco do Little Navmap não encontrado: {options.LnmPath}");
                Console.ResetColor();
                return 1;
            }

            options.CsvPath ??= LocateAirportsCsv();
            if (string.IsNullOrEmpty(options.CsvPath) || !File.Exists(options.CsvPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[tier-report] Erro: airports.csv não encontrado: {options.CsvPath}");
                Console.ResetColor();
                return 1;
            }

            Console.WriteLine("===============================================================================");
            Console.WriteLine("                  FlightCareer V6 — Relatório de Tiers (tier-report)           ");
            Console.WriteLine("===============================================================================");
            Console.WriteLine($"LNM Database: {options.LnmPath}");
            Console.WriteLine($"airports.csv: {options.CsvPath}");
            Console.WriteLine($"Saída:        {options.OutputDirectory}");
            Console.WriteLine();

            // 1. Carregar airports.csv
            Console.WriteLine("Lendo airports.csv...");
            var csvLookup = LoadCsvAirports(options.CsvPath);
            Console.WriteLine($"  {csvLookup.Count} aeródromos indexados do CSV.");

            // 2. Conectar e ler Little Navmap
            Console.WriteLine("Consultando aeródromos e pistas do Little Navmap...");
            var (classifiedAirports, countryStats) = LoadAndClassifyLnm(options.LnmPath, csvLookup);
            Console.WriteLine($"  {classifiedAirports.Count} aeródromos classificados.");
            Console.WriteLine($"  Resolução de países: {countryStats.ResolvedByBorder} por contorno, {countryStats.ResolvedByNeighbor} por vizinho, {countryStats.RemainedEmpty} sem país.");

            // 3. Gerar arquivos do relatório
            Directory.CreateDirectory(options.OutputDirectory);
            string mdPath = Path.Combine(options.OutputDirectory, "tier-report.md");
            string csvOutPath = Path.Combine(options.OutputDirectory, "tier-report.csv");

            Console.WriteLine($"Gravando {mdPath} e {csvOutPath}...");
            WriteMarkdownReport(mdPath, classifiedAirports, countryStats);
            WriteCsvReport(csvOutPath, classifiedAirports);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[tier-report] Relatório concluído com sucesso em {options.OutputDirectory}");
            Console.ResetColor();
            return 0;
        }

        private static string? LocateAirportsCsv()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "CSV", "airports.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "CSV", "airports.csv"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "CSV", "airports.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "CSV", "airports.csv")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path)) return Path.GetFullPath(path);
            }
            return null;
        }

        private static Dictionary<string, CsvItem> LoadCsvAirports(string csvPath)
        {
            var dict = new Dictionary<string, CsvItem>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StreamReader(csvPath, Encoding.UTF8);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var ident = csv.GetField<string>("ident")?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(ident)) continue;

                var type = csv.GetField<string>("type") ?? "";
                bool sched = string.Equals(csv.GetField<string>("scheduled_service"), "yes", StringComparison.OrdinalIgnoreCase);
                double? lat = double.TryParse(csv.GetField<string>("latitude_deg"), NumberStyles.Float, CultureInfo.InvariantCulture, out var la) ? la : null;
                double? lon = double.TryParse(csv.GetField<string>("longitude_deg"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lo) ? lo : null;
                var isoCountry = csv.GetField<string>("iso_country")?.Trim().ToUpperInvariant();

                dict[ident] = new CsvItem(ident, type, sched, lat, lon, isoCountry);
            }
            return dict;
        }

        private static (List<AirportClassified> Airports, CountryResolutionStats Stats) LoadAndClassifyLnm(string lnmPath, Dictionary<string, CsvItem> csvLookup)
        {
            var list = new List<AirportClassified>();
            var rules = AirportTierRules.Default;
            var catalog = AirportProfileCatalog.Shared;

            using var conn = new SqliteConnection($"Data Source={lnmPath};Mode=ReadOnly;");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    COALESCE(ident, icao) as ident,
                    COALESCE(name, 'Aeródromo MSFS') as name,
                    COALESCE(city, '') as city,
                    country,
                    region,
                    laty,
                    lonx,
                    longest_runway_length,
                    longest_runway_surface,
                    num_runway_hard,
                    num_runway_end_ils,
                    num_approach,
                    num_parking_gate,
                    num_jetway,
                    num_parking_cargo,
                    has_tower_object,
                    tower_frequency,
                    is_military
                FROM airport
                WHERE is_closed = 0
                  AND ident IS NOT NULL
                  AND length(ident) >= 3
                  AND (num_runway_hard > 0 OR num_runway_soft > 0)
                  AND longest_runway_length >= 1000;
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ident = reader.GetString(0);
                var name = reader.GetString(1);
                var city = reader.GetString(2);
                var rawCountry = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var rawRegion = reader.IsDBNull(4) ? "" : reader.GetString(4);
                var lat = reader.GetDouble(5);
                var lon = reader.GetDouble(6);
                var longestRunway = reader.IsDBNull(7) ? 0 : Convert.ToInt32(reader.GetValue(7));
                var rwSurface = reader.IsDBNull(8) ? "" : reader.GetString(8);
                var numHard = reader.IsDBNull(9) ? 0 : Convert.ToInt32(reader.GetValue(9));
                var ilsEnds = reader.IsDBNull(10) ? 0 : Convert.ToInt32(reader.GetValue(10));
                var approaches = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader.GetValue(11));
                var gates = reader.IsDBNull(12) ? 0 : Convert.ToInt32(reader.GetValue(12));
                var jetways = reader.IsDBNull(13) ? 0 : Convert.ToInt32(reader.GetValue(13));
                var cargoParking = reader.IsDBNull(14) ? 0 : Convert.ToInt32(reader.GetValue(14));
                var hasTowerObj = reader.IsDBNull(15) ? 0 : Convert.ToInt32(reader.GetValue(15));
                var towerFreq = reader.IsDBNull(16) ? 0.0 : Convert.ToDouble(reader.GetValue(16));
                var isMilitary = !reader.IsDBNull(17) && Convert.ToInt32(reader.GetValue(17)) != 0;

                bool hasTower = hasTowerObj > 0 || towerFreq > 0;
                bool isPaved = rwSurface is "A" or "C" or "B" or "T" ||
                               (numHard > 0 && rwSurface is not ("G" or "D" or "GR" or "S" or "SN" or "CR" or "W" or "Grass" or "Dirt" or "Gravel" or "Sand" or "Snow"));
                string country = ResolveCountry(ident, rawCountry, rawRegion);

                // Conferência com CSV (OurAirports)
                string? csvType = null;
                bool? scheduled = null;

                if (csvLookup.TryGetValue(ident, out var csvItem))
                {
                    if (csvItem.Lat.HasValue && csvItem.Lon.HasValue &&
                        DataSeeder.IsSameAirportPosition(csvItem.Lat.Value, csvItem.Lon.Value, lat, lon))
                    {
                        csvType = csvItem.Type;
                        scheduled = csvItem.Scheduled;
                        if (!string.IsNullOrWhiteSpace(csvItem.IsoCountry))
                        {
                            country = csvItem.IsoCountry;
                        }
                    }
                }

                var tags = catalog.TagsFor(ident);
                var input = new AirportTierInput(
                    tags,
                    IlsEnds: ilsEnds,
                    Approaches: approaches,
                    Gates: gates,
                    Jetways: jetways,
                    CargoParking: cargoParking,
                    HasTower: hasTower,
                    LongestRunwayFt: longestRunway,
                    LongestRunwayPaved: isPaved,
                    CsvType: csvType,
                    ScheduledService: scheduled,
                    IsMilitary: isMilitary
                );

                var scoreDetails = AirportTierClassifier.Score(input, rules);

                // Regra antiga por comprimento e pavimentação de pista
                int oldTier = (longestRunway >= 7500 && isPaved) ? 1 :
                              (longestRunway >= 4500 && isPaved) ? 2 :
                              (isPaved || longestRunway >= 2500) ? 3 : 4;

                list.Add(new AirportClassified(
                    ICAO: ident,
                    Name: name,
                    City: city,
                    Country: country,
                    Lat: lat,
                    Lon: lon,
                    LongestRunwayFt: longestRunway,
                    LongestRunwayPaved: isPaved,
                    OldTier: oldTier,
                    ScoreDetails: scoreDetails
                ));
            }

            // 3. Resolução de países faltantes por contorno e vizinho
            var locator = CountryLocator.Shared;
            var grid = new AirportSpatialGrid();
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (!string.IsNullOrEmpty(a.Country) && locator.KnownIsoCodes.Contains(a.Country))
                {
                    grid.Add(a.Lat, a.Lon, a.Country);
                }
            }

            int totalInvalid = 0;
            int resolvedByBorder = 0;
            int resolvedByNeighbor = 0;
            int remainedEmpty = 0;
            var examplesByBorder = new List<AirportClassified>();

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (string.IsNullOrEmpty(a.Country) || !locator.KnownIsoCodes.Contains(a.Country))
                {
                    totalInvalid++;
                    string? byBorder = locator.Locate(a.Lat, a.Lon);
                    if (byBorder != null)
                    {
                        resolvedByBorder++;
                        var updated = a with { Country = byBorder };
                        list[i] = updated;
                        grid.Add(a.Lat, a.Lon, byBorder);
                        if (examplesByBorder.Count < 10)
                        {
                            examplesByBorder.Add(updated);
                        }
                    }
                    else
                    {
                        string? byNeighbor = grid.FindNearestCountry(a.Lat, a.Lon, 50.0);
                        if (byNeighbor != null)
                        {
                            resolvedByNeighbor++;
                            var updated = a with { Country = byNeighbor };
                            list[i] = updated;
                            grid.Add(a.Lat, a.Lon, byNeighbor);
                        }
                        else
                        {
                            remainedEmpty++;
                            if (!string.IsNullOrEmpty(a.Country))
                            {
                                list[i] = a with { Country = "" };
                            }
                        }
                    }
                }
            }

            var stats = new CountryResolutionStats(totalInvalid, resolvedByBorder, resolvedByNeighbor, remainedEmpty, examplesByBorder);
            return (list, stats);
        }

        private static string ResolveCountry(string ident, string? country, string? region)
        {
            if (ident.StartsWith("SB") || ident.StartsWith("SS") || ident.StartsWith("SW") || ident.StartsWith("SD") || ident.StartsWith("SJ") || ident.StartsWith("SN") || ident.StartsWith("SI")) return "BR";
            if (ident.StartsWith("K") && ident.Length == 4) return "US";
            if ((ident.StartsWith("C") || ident.StartsWith("CY") || ident.StartsWith("CZ")) && ident.Length == 4) return "CA";
            if (ident.StartsWith("MM")) return "MX";
            if (ident.StartsWith("SA")) return "AR";
            if (ident.StartsWith("SC")) return "CL";
            if (ident.StartsWith("SK")) return "CO";
            if (ident.StartsWith("SP")) return "PE";
            if (ident.StartsWith("SU")) return "UY";
            if (ident.StartsWith("SV")) return "VE";
            if (ident.StartsWith("SG")) return "PY";
            if (ident.StartsWith("SL")) return "BO";
            if (ident.StartsWith("EG")) return "GB";
            if (ident.StartsWith("LF")) return "FR";
            if (ident.StartsWith("ED") || ident.StartsWith("ET")) return "DE";
            if (ident.StartsWith("LE")) return "ES";
            if (ident.StartsWith("LP")) return "PT";
            if (ident.StartsWith("LI")) return "IT";
            if (ident.StartsWith("EH")) return "NL";
            if (ident.StartsWith("EB")) return "BE";
            if (ident.StartsWith("RJ") || ident.StartsWith("RO")) return "JP";
            if (ident.StartsWith("Y") && ident.Length == 4) return "AU";
            if (ident.StartsWith("NZ")) return "NZ";
            if (!string.IsNullOrEmpty(country) && country.Length == 2 && country != "SB" && country != "SS" && country != "SW" && country != "SD" && country != "SI" && country != "SN") return country;
            return !string.IsNullOrEmpty(country) ? country : (!string.IsNullOrEmpty(region) ? region : "");
        }

        private static void WriteMarkdownReport(string mdPath, List<AirportClassified> airports, CountryResolutionStats stats)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Relatório de Classificação de Tiers por Movimento");
            sb.AppendLine();
            sb.AppendLine($"Data: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Total de aeródromos analisados: {airports.Count:N0}");
            sb.AppendLine();

            // 1. Tabela de distribuição no Mundo e no Brasil
            sb.AppendLine("## 1. Contagem e % por Tier (Mundo e Brasil)");
            sb.AppendLine();
            sb.AppendLine("| Tier | Mundo (Qtd) | Mundo (%) | Alvo Mundo | Brasil (Qtd) | Brasil (%) |");
            sb.AppendLine("|---|---|---|---|---|---|");

            int totalWorld = airports.Count;
            var brAirports = airports.Where(a => a.Country == "BR").ToList();
            int totalBr = brAirports.Count;

            string[] targets = { "≈ 400–600", "≈ 8–10 %", "≈ 25 %", "resto (≈ 60 %)" };

            for (int t = 1; t <= 4; t++)
            {
                int countWorld = airports.Count(a => a.NewTier == t);
                double pctWorld = totalWorld > 0 ? (countWorld * 100.0 / totalWorld) : 0;

                int countBr = brAirports.Count(a => a.NewTier == t);
                double pctBr = totalBr > 0 ? (countBr * 100.0 / totalBr) : 0;

                sb.AppendLine($"| Tier {t} | {countWorld:N0} | {pctWorld:F1}% | {targets[t - 1]} | {countBr:N0} | {pctBr:F1}% |");
            }

            sb.AppendLine();

            // 2. Casos de referência do plano
            sb.AppendLine("## 2. Casos de Referência do Plano");
            sb.AppendLine();
            sb.AppendLine("| ICAO | Nome | Tier Novo | Tier Antigo | Score Total | Tag Dominante (Pts) | Estrutura (ILS/App/Gates/Cgo/Twr/Rw) | OurAirports (Tipo/Sched) |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");

            var refIcaos = new[] { "SBGR", "SBKP", "SBSP", "SBRJ", "KJFK", "EGLL", "SBMT", "SDCO", "SDAO", "PABE" };
            var byIcao = airports.ToDictionary(a => a.ICAO, StringComparer.OrdinalIgnoreCase);

            foreach (var icao in refIcaos)
            {
                if (byIcao.TryGetValue(icao, out var a))
                {
                    var d = a.ScoreDetails;
                    string tagStr = $"{d.DominantTag ?? "nenhuma"} ({d.TagScore})";
                    string structStr = $"{d.StructureScore} (ILS:{d.IlsScore}, App:{d.ApproachScore}, Gt:{d.GateJetwayScore}, Cgo:{d.CargoParkingScore}, Twr:{d.TowerScore}, Rw:{d.RunwayScore})";
                    string oaStr = $"{d.OurAirportsScore} (Typ:{d.CsvTypeScore}, Sched:{d.ScheduledServiceScore})";
                    sb.AppendLine($"| **{a.ICAO}** | {a.Name} | **{a.NewTier}** | {a.OldTier} | **{d.TotalScore}** | {tagStr} | {structStr} | {oaStr} |");
                }
                else
                {
                    sb.AppendLine($"| **{icao}** | *(não encontrado no LNM)* | - | - | - | - | - | - |");
                }
            }

            sb.AppendLine();

            // 3. 30 Exemplos aleatórios por Tier
            sb.AppendLine("## 3. 30 Exemplos Aleatórios por Tier (Semente Fixa)");
            sb.AppendLine();

            var rng = new Random(42);
            for (int t = 1; t <= 4; t++)
            {
                var tierList = airports.Where(a => a.NewTier == t).OrderBy(_ => rng.Next()).Take(30).ToList();
                sb.AppendLine($"### Tier {t} ({tierList.Count} amostras)");
                sb.AppendLine();
                sb.AppendLine("| ICAO | Nome | País | Score | Pista (ft / Pav) | Tag Dominante |");
                sb.AppendLine("|---|---|---|---|---|---|");

                foreach (var a in tierList)
                {
                    string pvd = a.LongestRunwayPaved ? "Sim" : "Não";
                    sb.AppendLine($"| {a.ICAO} | {a.Name} | {a.Country} | {a.ScoreDetails.TotalScore} | {a.LongestRunwayFt:N0} ft ({pvd}) | {a.ScoreDetails.DominantTag ?? "-"} |");
                }
                sb.AppendLine();
            }

            // 4. 50 Aeroportos que mais mudaram de Tier
            sb.AppendLine("## 4. Top 50 Aeroportos com Maior Mudança de Tier (Novo vs Antigo)");
            sb.AppendLine();
            sb.AppendLine("| ICAO | Nome | País | Antigo → Novo | Δ Tier | Score | Pista (ft / Pav) | Motivo Principal |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");

            var changedAirports = airports
                .OrderByDescending(a => Math.Abs(a.DeltaTier))
                .ThenByDescending(a => a.ScoreDetails.TotalScore)
                .Take(50)
                .ToList();

            foreach (var a in changedAirports)
            {
                string pvd = a.LongestRunwayPaved ? "Pav" : "Terra";
                string motivo = a.ScoreDetails.TagScore >= 50
                    ? $"Etiqueta forte ({a.ScoreDetails.DominantTag}: {a.ScoreDetails.TagScore} pts)"
                    : a.ScoreDetails.StructureScore >= 30
                        ? $"Estrutura no sim ({a.ScoreDetails.StructureScore} pts)"
                        : a.ScoreDetails.OurAirportsScore >= 30
                            ? $"OurAirports ({a.ScoreDetails.OurAirportsScore} pts)"
                            : !a.LongestRunwayPaved ? "Pista não pavimentada (limite max Tier 3/4)" : "Pista longa sem estrutura/movimento";

                string sign = a.DeltaTier > 0 ? $"+{a.DeltaTier}" : $"{a.DeltaTier}";
                sb.AppendLine($"| **{a.ICAO}** | {a.Name} | {a.Country} | {a.OldTier} → **{a.NewTier}** | {sign} | {a.ScoreDetails.TotalScore} | {a.LongestRunwayFt:N0} ft ({pvd}) | {motivo} |");
            }

            sb.AppendLine();

            // 5. Resolução de Países
            sb.AppendLine("## 5. Resolução de Países (Natural Earth e Vizinhos)");
            sb.AppendLine();
            sb.AppendLine($"- Total de aeródromos analisados com país inicial inválido: {stats.TotalInvalid:N0}");
            sb.AppendLine($"- Resolvidos por contorno do Natural Earth: {stats.ResolvedByBorder:N0}");
            sb.AppendLine($"- Resolvidos por vizinho válido mais próximo (<= 50 NM): {stats.ResolvedByNeighbor:N0}");
            sb.AppendLine($"- Aeródromos que ficaram sem país: {stats.RemainedEmpty:N0} (alvo <= 30)");
            sb.AppendLine();
            sb.AppendLine("### 10 Exemplos Resolvidos por Contorno");
            sb.AppendLine();
            sb.AppendLine("| ICAO | Nome | País Atribuído | Lat | Lon |");
            sb.AppendLine("|---|---|---|---|---|");

            foreach (var ex in stats.ExamplesByBorder)
            {
                sb.AppendLine($"| **{ex.ICAO}** | {ex.Name} | **{ex.Country}** | {ex.Lat.ToString("F4", CultureInfo.InvariantCulture)} | {ex.Lon.ToString("F4", CultureInfo.InvariantCulture)} |");
            }

            File.WriteAllText(mdPath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteCsvReport(string csvPath, List<AirportClassified> airports)
        {
            using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
            writer.WriteLine("ICAO,Name,City,Country,Lat,Lon,OldTier,NewTier,DeltaTier,TotalScore,TagScore,StructureScore,OurAirportsScore,IlsScore,ApproachScore,GateJetwayScore,CargoParkingScore,TowerScore,RunwayScore,CsvTypeScore,ScheduledServiceScore,LongestRunwayFt,LongestRunwayPaved,DominantTag");

            foreach (var a in airports)
            {
                var d = a.ScoreDetails;
                string cleanName = a.Name.Replace("\"", "\"\"");
                string cleanCity = a.City.Replace("\"", "\"\"");
                string line = $"{a.ICAO},\"{cleanName}\",\"{cleanCity}\",{a.Country},{a.Lat.ToString(CultureInfo.InvariantCulture)},{a.Lon.ToString(CultureInfo.InvariantCulture)},{a.OldTier},{a.NewTier},{a.DeltaTier},{d.TotalScore},{d.TagScore},{d.StructureScore},{d.OurAirportsScore},{d.IlsScore},{d.ApproachScore},{d.GateJetwayScore},{d.CargoParkingScore},{d.TowerScore},{d.RunwayScore},{d.CsvTypeScore},{d.ScheduledServiceScore},{a.LongestRunwayFt},{(a.LongestRunwayPaved ? 1 : 0)},\"{d.DominantTag ?? ""}\"";
                writer.WriteLine(line);
            }
        }
    }
}
