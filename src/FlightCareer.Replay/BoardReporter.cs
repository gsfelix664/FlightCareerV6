using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Replay
{
    public sealed class BoardReportOptions
    {
        public int BoardsCount { get; set; } = 100;
        public int Seed { get; set; } = 7;
        public List<string> Airports { get; set; } = new()
        {
            "SBGR", "KMEM", "SBKP", "SBTF", "SDCO", "SDOI", "SBJR", "SDAO", "PABE", "SBAN"
        };
        public bool Coverage { get; set; }
        public string OutputDirectory { get; set; } = ".";
    }

    public static class BoardReporter
    {
        public static int Run(string[] args)
        {
            var options = new BoardReportOptions();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--boards", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int b)) options.BoardsCount = b;
                }
                else if (args[i].Equals("--seed", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int s)) options.Seed = s;
                }
                else if (args[i].Equals("--airports", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.Airports = args[++i].Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                }
                else if (args[i].Equals("--coverage", StringComparison.OrdinalIgnoreCase))
                {
                    options.Coverage = true;
                }
                else if (args[i].Equals("--out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.OutputDirectory = args[++i];
                }
            }

            Console.WriteLine("===============================================================================");
            Console.WriteLine("                  FlightCareer V6 — Relatório de Quadros (Board Report)        ");
            Console.WriteLine("===============================================================================");
            Console.WriteLine($"Boards por aeroporto: {options.BoardsCount} | Seed: {options.Seed} | Coverage: {options.Coverage}");
            Console.WriteLine($"Aeroportos: {string.Join(", ", options.Airports)}");
            Console.WriteLine($"Diretório de saída: {options.OutputDirectory}");
            Console.WriteLine();

            string tempDbFile = Path.Combine(Path.GetTempPath(), $"board_report_{Guid.NewGuid():N}.db");
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={tempDbFile}")
                .Options;

            List<Airport> allAirports;
            List<Runway> allRunways;

            var seedSw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Console.WriteLine("[BOARD-REPORT] Semeando banco temporário para dados de aeroportos e pistas...");
                using (var setupDb = new AppDbContext(dbOptions))
                {
                    setupDb.Database.EnsureCreated();
                    setupDb.EnsureSchemaUpdatedAsync().GetAwaiter().GetResult();
                    var seeder = new DataSeeder(setupDb);
                    seeder.SeedAsync((msg, pct) => { }).GetAwaiter().GetResult();
                }

                using (var db = new AppDbContext(dbOptions))
                {
                    allAirports = db.Airports.AsNoTracking().ToList();
                    allRunways = db.Runways.AsNoTracking().ToList();
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tempDbFile)) File.Delete(tempDbFile);
                }
                catch { }
            }
            seedSw.Stop();
            Console.WriteLine($"[BOARD-REPORT] Seed concluído em {seedSw.Elapsed.TotalSeconds:F2} s.");

            var airportsByIcao = allAirports.ToDictionary(a => a.ICAO, StringComparer.OrdinalIgnoreCase);
            var catalog = MissionCatalog.Shared;
            var profileCatalog = AirportProfileCatalog.Shared;
            var random = new Random(options.Seed);
            var refTime = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
            var cache = new BoardCache(allAirports, allRunways);

            // Estruturas de agregação
            // 1. % de cada tipo por aeroporto
            var countsByAirportAndType = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var totalMissionsByAirport = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 2. Destinos com perfil preferido
            var prefMatchesByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var prefTotalByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 3. Mín, Média, Máx e distintos
            var statsByType = new Dictionary<string, TypeStats>(StringComparer.OrdinalIgnoreCase);

            // 4. Cobertura de destinos
            var destinationHits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var simulationSw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var icao in options.Airports)
            {
                if (!airportsByIcao.TryGetValue(icao, out var origin))
                {
                    Console.WriteLine($"[AVISO] Aeroporto {icao} não encontrado no banco de seed. Pulando.");
                    continue;
                }

                countsByAirportAndType[icao] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                totalMissionsByAirport[icao] = 0;

                Console.WriteLine($"[BOARD-REPORT] Simulando {options.BoardsCount} quadros para {origin.ICAO} ({origin.Name}, Tier {origin.Tier})...");

                for (int b = 0; b < options.BoardsCount; b++)
                {
                    var missions = MissionGenerator.GenerateMissionsInMemory(
                        origin,
                        allAirports,
                        null,
                        random,
                        refTime,
                        countOverride: null,
                        allRunways: allRunways,
                        cache: cache);

                    foreach (var m in missions)
                    {
                        string typeId = m.SubId ?? "unknown";

                        // 1. Tipo por aeroporto
                        countsByAirportAndType[icao][typeId] = countsByAirportAndType[icao].GetValueOrDefault(typeId) + 1;
                        totalMissionsByAirport[icao]++;

                        // 2. Destino preferencial
                        var def = catalog.Get(typeId);
                        bool isReturn = string.Equals(m.DestinationICAO, m.OriginICAO, StringComparison.OrdinalIgnoreCase);

                        if (!isReturn && def != null && def.Destination?.Profiles != null && def.Destination.Profiles.Count > 0)
                        {
                            prefTotalByType[typeId] = prefTotalByType.GetValueOrDefault(typeId) + 1;
                            var destTags = profileCatalog.TagsFor(m.DestinationICAO);
                            bool matched = def.Destination.Profiles.Keys.Any(k => AirportProfileCatalog.MatchesAny(k, destTags));
                            if (matched)
                            {
                                prefMatchesByType[typeId] = prefMatchesByType.GetValueOrDefault(typeId) + 1;
                            }
                        }

                        // 3. Estatísticas
                        if (!statsByType.TryGetValue(typeId, out var stats))
                        {
                            stats = new TypeStats();
                            statsByType[typeId] = stats;
                        }
                        stats.Record(m.PassengerCount, m.CargoWeightLbs, m.LuggageWeightLbs, m.DistanceNM);

                        // 4. Cobertura
                        if (!isReturn && !string.IsNullOrEmpty(m.DestinationICAO))
                        {
                            destinationHits[m.DestinationICAO] = destinationHits.GetValueOrDefault(m.DestinationICAO) + 1;
                        }
                    }
                }
            }
            simulationSw.Stop();
            Console.WriteLine($"[BOARD-REPORT] Simulação de quadros concluída em {simulationSw.Elapsed.TotalSeconds:F2} s.");

            // Gerar Markdown e CSVs
            Directory.CreateDirectory(options.OutputDirectory);
            string mdPath = Path.Combine(options.OutputDirectory, "board-report.md");
            var sb = new StringBuilder();

            sb.AppendLine("# Relatório de Quadros de Missão (Board Report)");
            sb.AppendLine();
            sb.AppendLine($"**Data:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC · **Semente:** {options.Seed} · **Quadros por Aeroporto:** {options.BoardsCount}");
            sb.AppendLine($"**Origens analisadas:** {string.Join(", ", options.Airports)}");
            sb.AppendLine();

            // Tabela 1: % de cada tipo por aeroporto
            sb.AppendLine("## 1. Distribuição de Tipos por Aeroporto de Origem (%)");
            sb.AppendLine();
            var activeAirports = options.Airports.Where(countsByAirportAndType.ContainsKey).ToList();
            var allTypes = catalog.Missions.Values
                .Where(m => m.Enabled)
                .OrderBy(m => m.Board)
                .ThenBy(m => m.Id)
                .Select(m => m.Id)
                .ToList();

            sb.Append("| Tipo | ");
            foreach (var a in activeAirports) sb.Append($"{a} | ");
            sb.AppendLine();
            sb.Append("|---|");
            foreach (var _ in activeAirports) sb.Append("---|");
            sb.AppendLine();

            var csv1 = new StringBuilder();
            csv1.Append("Tipo," + string.Join(",", activeAirports) + "\n");

            foreach (var tid in allTypes)
            {
                sb.Append($"| `{tid}` | ");
                var row = new List<string> { tid };
                foreach (var a in activeAirports)
                {
                    int total = totalMissionsByAirport[a];
                    int count = countsByAirportAndType[a].GetValueOrDefault(tid);
                    double pct = total > 0 ? (count * 100.0 / total) : 0.0;
                    sb.Append($"{pct:F1}% | ");
                    row.Add(pct.ToString("F2", CultureInfo.InvariantCulture));
                }
                sb.AppendLine();
                csv1.AppendLine(string.Join(",", row));
            }
            File.WriteAllText(Path.Combine(options.OutputDirectory, "board_types_by_airport.csv"), csv1.ToString(), Encoding.UTF8);

            sb.AppendLine();

            // Tabela 2: Destinos que têm a etiqueta preferida
            sb.AppendLine("## 2. Aderência a Destinos Preferenciais por Tipo");
            sb.AppendLine();
            sb.AppendLine("| Tipo de Missão | Voos com Destino Externo | Destinos com Etiqueta Preferida | Aderência (%) |");
            sb.AppendLine("|---|---|---|---|");

            var csv2 = new StringBuilder();
            csv2.AppendLine("Tipo,TotalVoosExternos,ComEtiquetaPreferida,AderenciaPct");

            foreach (var tid in allTypes)
            {
                var def = catalog.Get(tid);
                if (def == null || def.Destination == null || def.Destination.Profiles == null || def.Destination.Profiles.Count == 0)
                {
                    continue; // Sem destino preferencial configurado
                }

                int total = prefTotalByType.GetValueOrDefault(tid);
                int matches = prefMatchesByType.GetValueOrDefault(tid);
                double pct = total > 0 ? (matches * 100.0 / total) : 0.0;

                sb.AppendLine($"| `{tid}` | {total:N0} | {matches:N0} | **{pct:F1}%** |");
                csv2.AppendLine($"{tid},{total},{matches},{pct.ToString("F2", CultureInfo.InvariantCulture)}");
            }
            File.WriteAllText(Path.Combine(options.OutputDirectory, "board_destination_preferred.csv"), csv2.ToString(), Encoding.UTF8);

            sb.AppendLine();

            // Tabela 3: Mín / Média / Máx e Distintos por grandeza
            sb.AppendLine("## 3. Parâmetros Físicos por Tipo (Mín / Média / Máx e Valores Distintos)");
            sb.AppendLine();
            sb.AppendLine("| Tipo | Pax (Mín/Méd/Máx · Dist) | Carga lbs (Mín/Méd/Máx · Dist) | Bagagem lbs (Mín/Méd/Máx · Dist) | Distância NM (Mín/Méd/Máx · Dist) |");
            sb.AppendLine("|---|---|---|---|---|");

            var csv3 = new StringBuilder();
            csv3.AppendLine("Tipo,PaxMin,PaxMed,PaxMax,PaxDist,CargaMin,CargaMed,CargaMax,CargaDist,BagMin,BagMed,BagMax,BagDist,DistMin,DistMed,DistMax,DistDist");

            foreach (var tid in allTypes)
            {
                if (!statsByType.TryGetValue(tid, out var s) || s.Count == 0)
                {
                    continue;
                }

                sb.AppendLine($"| `{tid}` | {s.PaxMin}/{s.PaxAvg:F1}/{s.PaxMax} · ({s.PaxDistinct}) | {s.CargoMin:N0}/{s.CargoAvg:N0}/{s.CargoMax:N0} · ({s.CargoDistinct}) | {s.LuggageMin:N0}/{s.LuggageAvg:N0}/{s.LuggageMax:N0} · ({s.LuggageDistinct}) | {s.DistMin:N0}/{s.DistAvg:N0}/{s.DistMax:N0} · ({s.DistDistinct}) |");
                csv3.AppendLine($"{tid},{s.PaxMin},{s.PaxAvg:F1},{s.PaxMax},{s.PaxDistinct},{s.CargoMin:F0},{s.CargoAvg:F1},{s.CargoMax:F0},{s.CargoDistinct},{s.LuggageMin:F0},{s.LuggageAvg:F1},{s.LuggageMax:F0},{s.LuggageDistinct},{s.DistMin:F0},{s.DistAvg:F1},{s.DistMax:F0},{s.DistDistinct}");
            }
            File.WriteAllText(Path.Combine(options.OutputDirectory, "board_ranges_stats.csv"), csv3.ToString(), Encoding.UTF8);

            sb.AppendLine();

            // Tabela 4: Cobertura de aeroportos (se solicitado)
            if (options.Coverage)
            {
                sb.AppendLine("## 4. Cobertura de Destinos em `airport_profiles.json`");
                sb.AppendLine();

                // Ler todos os aeroportos de airport_profiles.json
                var profileAirports = LoadProfileAirportIcaos();
                int totalProfiles = profileAirports.Count;
                int hitCount = profileAirports.Count(icao => destinationHits.ContainsKey(icao) && destinationHits[icao] > 0);
                int zeroCount = totalProfiles - hitCount;

                sb.AppendLine($"- **Total de Aeroportos no Perfil:** {totalProfiles:N0}");
                sb.AppendLine($"- **Alcançados como Destino:** {hitCount:N0} ({(hitCount * 100.0 / totalProfiles):F1}%)");
                sb.AppendLine($"- **Com 0 Visitas:** {zeroCount:N0} ({(zeroCount * 100.0 / totalProfiles):F1}%)");
                sb.AppendLine();

                var csv4 = new StringBuilder();
                csv4.AppendLine("ICAO,DestinoHits,Motivo");

                int sampleZeros = 0;
                sb.AppendLine("### Amostra de Aeroportos não visitados e Motivo");
                sb.AppendLine("| ICAO | Visitas | Motivo provável |");
                sb.AppendLine("|---|---|---|");

                foreach (var icao in profileAirports.OrderBy(x => x))
                {
                    int hits = destinationHits.GetValueOrDefault(icao);
                    if (hits == 0)
                    {
                        bool exists = airportsByIcao.TryGetValue(icao, out var a);
                        string motivo;
                        if (!exists)
                        {
                            motivo = "não cadastrado no banco de dados ativo";
                        }
                        else
                        {
                            // Verificar se está fora do raio de todas as origens testadas
                            double minDist = options.Airports.Where(airportsByIcao.ContainsKey).Min(o =>
                                MissionGenerator.CalculateDistance(airportsByIcao[o].Latitude, airportsByIcao[o].Longitude, a.Latitude, a.Longitude));
                            if (minDist > 3000)
                            {
                                motivo = $"distância extrema das origens testadas ({minDist:F0} NM)";
                            }
                            else
                            {
                                motivo = "não sorteado na amostragem (probabilístico)";
                            }
                        }

                        csv4.AppendLine($"{icao},{hits},{motivo}");
                        if (sampleZeros < 20)
                        {
                            sampleZeros++;
                            sb.AppendLine($"| `{icao}` | {hits} | {motivo} |");
                        }
                    }
                    else
                    {
                        csv4.AppendLine($"{icao},{hits},ok");
                    }
                }
                File.WriteAllText(Path.Combine(options.OutputDirectory, "board_coverage.csv"), csv4.ToString(), Encoding.UTF8);
            }

            File.WriteAllText(mdPath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"[BOARD-REPORT] Relatório gerado com sucesso em {mdPath}!");

            return 0;
        }

        private static List<string> LoadProfileAirportIcaos()
        {
            var list = new List<string>();
            try
            {
                var cat = AirportProfileCatalog.Shared;
                // Ler JSON bruto para pegar a lista completa de chaves de airports
                string[] candidates =
                {
                    Path.Combine(AppContext.BaseDirectory, "Data", "places", "airport_profiles.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "places", "airport_profiles.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "places", "airport_profiles.json")
                };
                var path = candidates.FirstOrDefault(File.Exists);
                if (path != null)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
                    if (doc.RootElement.TryGetProperty("airports", out var el) && el.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in el.EnumerateObject())
                        {
                            list.Add(prop.Name);
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private sealed class TypeStats
        {
            public int Count { get; private set; }

            public int PaxMin { get; private set; } = int.MaxValue;
            public int PaxMax { get; private set; } = int.MinValue;
            public double PaxSum { get; private set; }
            public double PaxAvg => Count > 0 ? PaxSum / Count : 0;
            private readonly HashSet<int> _paxVals = new();
            public int PaxDistinct => _paxVals.Count;

            public double CargoMin { get; private set; } = double.MaxValue;
            public double CargoMax { get; private set; } = double.MinValue;
            public double CargoSum { get; private set; }
            public double CargoAvg => Count > 0 ? CargoSum / Count : 0;
            private readonly HashSet<double> _cargoVals = new();
            public int CargoDistinct => _cargoVals.Count;

            public double LuggageMin { get; private set; } = double.MaxValue;
            public double LuggageMax { get; private set; } = double.MinValue;
            public double LuggageSum { get; private set; }
            public double LuggageAvg => Count > 0 ? LuggageSum / Count : 0;
            private readonly HashSet<double> _luggageVals = new();
            public int LuggageDistinct => _luggageVals.Count;

            public double DistMin { get; private set; } = double.MaxValue;
            public double DistMax { get; private set; } = double.MinValue;
            public double DistSum { get; private set; }
            public double DistAvg => Count > 0 ? DistSum / Count : 0;
            private readonly HashSet<double> _distVals = new();
            public int DistDistinct => _distVals.Count;

            public void Record(int pax, double cargo, double luggage, double dist)
            {
                Count++;

                if (pax < PaxMin) PaxMin = pax;
                if (pax > PaxMax) PaxMax = pax;
                PaxSum += pax;
                _paxVals.Add(pax);

                if (cargo < CargoMin) CargoMin = cargo;
                if (cargo > CargoMax) CargoMax = cargo;
                CargoSum += cargo;
                _cargoVals.Add(cargo);

                if (luggage < LuggageMin) LuggageMin = luggage;
                if (luggage > LuggageMax) LuggageMax = luggage;
                LuggageSum += luggage;
                _luggageVals.Add(luggage);

                if (dist < DistMin) DistMin = dist;
                if (dist > DistMax) DistMax = dist;
                DistSum += dist;
                _distVals.Add(dist);
            }
        }
    }
}
