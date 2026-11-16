using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Replay;
using FlightCareer.Domain.Replay.Synthetic;
using FlightCareer.Domain.Sensors;
using FlightCareer.Infrastructure.Replay;
using FlightCareer.Infrastructure.Services;

namespace FlightCareer.Replay
{
    public static class Program
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                return 0;
            }

            if (args[0].Equals("fuzz", StringComparison.OrdinalIgnoreCase))
            {
                return RunFuzz(args.Skip(1).ToArray());
            }

            string? csvPath = null;
            string? aircraftArg = null;
            string? blackboxPath = null;
            string? touchdownsPath = null;
            bool jsonOutput = false;
            bool fromStart = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("--aircraft", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    aircraftArg = args[++i];
                }
                else if (arg.Equals("--blackbox", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    blackboxPath = args[++i];
                }
                else if (arg.Equals("--touchdowns", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    touchdownsPath = args[++i];
                }
                else if (arg.Equals("--from-start", StringComparison.OrdinalIgnoreCase))
                {
                    fromStart = true;
                }
                else if (arg.Equals("--json", StringComparison.OrdinalIgnoreCase))
                {
                    jsonOutput = true;
                }
                else if (!arg.StartsWith("-", StringComparison.Ordinal) && csvPath == null)
                {
                    csvPath = arg;
                }
            }

            if (string.IsNullOrWhiteSpace(csvPath))
            {
                Console.Error.WriteLine("Erro: Caminho do arquivo de telemetria (.csv ou .csv.gz) não informado.");
                PrintHelp();
                return 1;
            }

            if (!File.Exists(csvPath))
            {
                Console.Error.WriteLine($"Erro: Arquivo CSV não encontrado: {csvPath}");
                return 1;
            }

            try
            {
                // Carrega catálogos
                var partsCatalog = PartCatalog.LoadDefault();
                var wearRuleCatalog = WearRuleCatalog.LoadDefault();
                var limitLoader = new AircraftLimitCatalogLoader();
                var resolver = limitLoader.CreateResolver();
                var allProfiles = limitLoader.LoadProfiles();

                // Resolve o perfil da aeronave
                AircraftLimitProfile? profile = ResolveProfile(csvPath, aircraftArg, resolver, allProfiles);
                if (profile == null)
                {
                    Console.Error.WriteLine("Erro: Não foi possível determinar o perfil de limites da aeronave.");
                    Console.Error.WriteLine("Use --aircraft <modelo> especificando um dos modelos abaixo:");
                    foreach (var p in allProfiles.OrderBy(p => p.AircraftId))
                    {
                        Console.Error.WriteLine($"  - {p.AircraftId} (Classe: {p.Class})");
                    }
                    return 1;
                }

                // Lê os dados de log
                var logData = SensorLogReader.ReadLog(csvPath);
                var frames = logData.Frames;

                if (frames.Count == 0)
                {
                    Console.Error.WriteLine("Aviso: O arquivo CSV não contém quadros de sensores válidos.");
                }

                // Carrega toques se especificado ou se houver .touchdowns.json adjacente
                IReadOnlyList<ReplayTouchdown> touchdowns = Array.Empty<ReplayTouchdown>();
                if (!string.IsNullOrWhiteSpace(touchdownsPath))
                {
                    touchdowns = SensorLogReader.ReadTouchdownsFromJson(touchdownsPath);
                }
                else if (!string.IsNullOrWhiteSpace(blackboxPath))
                {
                    touchdowns = SensorLogReader.ReadTouchdownsFromBlackbox(blackboxPath);
                }
                else
                {
                    string candidateTd = TryFindAdjacentTouchdownsFile(csvPath);
                    if (File.Exists(candidateTd))
                    {
                        touchdowns = SensorLogReader.ReadTouchdownsFromJson(candidateTd);
                    }
                }

                // Executa o replay
                var activationMode = fromStart ? ReplayActivationMode.FromStart : ReplayActivationMode.GameLike;
                var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
                var result = replayer.Run(
                    frames,
                    touchdowns,
                    profile,
                    profile.Features,
                    profile.EngineCount,
                    activationMode: activationMode);

                if (jsonOutput)
                {
                    var jsonObj = new
                    {
                        aircraft = new
                        {
                            id = profile.AircraftId,
                            @class = profile.Class,
                            engineCount = profile.EngineCount,
                            features = profile.Features
                        },
                        totalSimHours = result.TotalSimHours,
                        totalTicks = result.TotalTicks,
                        elapsedWallTime = result.ElapsedWallTime.ToString(),
                        parts = result.Parts,
                        exceedances = result.Exceedances,
                        inspections = result.Inspections,
                        derivedEvents = result.DerivedEvents,
                        diagnostics = result.Diagnostics
                    };
                    Console.WriteLine(JsonSerializer.Serialize(jsonObj, JsonOpts));
                }
                else
                {
                    PrintHumanReadableResult(csvPath, profile, result, logData.Markers.Count);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro durante a execução do replay: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 2;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Uso: FlightCareer.Replay <csv|csv.gz> [opções]");
            Console.WriteLine("     FlightCareer.Replay fuzz [--runs <n>] [--aircraft <modelo|all>] [--seed <n>] [--fast]");
            Console.WriteLine();
            Console.WriteLine("Opções de Replay:");
            Console.WriteLine("  --aircraft <modelo>      Modelo ou Id da aeronave (ex.: 'Cessna C152', 'C172SP G1000')");
            Console.WriteLine("  --blackbox <arquivo>     Caminho do log do blackbox para leitura dos toques");
            Console.WriteLine("  --touchdowns <json>      Caminho de arquivo JSON com dados de toques");
            Console.WriteLine("  --from-start             Desgasta desde o início do arquivo (ignora espera de ativação)");
            Console.WriteLine("  --json                   Emite a saída no formato JSON estruturado");
            Console.WriteLine("  -h, --help               Exibe esta mensagem de ajuda");
            Console.WriteLine();
            Console.WriteLine("Opções do Fuzz:");
            Console.WriteLine("  --aircraft <modelo|all>  Modelo específico ou 'all' para todas as aeronaves do catálogo");
            Console.WriteLine("  --runs <n>               Número de voos sintéticos (padrão 1000 no total; com 'all' padrão 50 por aeronave)");
            Console.WriteLine("  --seed <n>               Semente pseudoaleatória inicial (padrão 42)");
            Console.WriteLine("  --fast                   Modo rápido: valida apenas invariantes (sem voo controle e gabarito)");
            Console.WriteLine("  Nota: o fuzz usa índices elétricos padrão de teste quando a aeronave não os tem no catálogo.");
        }

        private static int RunFuzz(string[] args)
        {
            int? runsArg = null;
            string? aircraftArg = null;
            int? baseSeedArg = null;
            bool fastMode = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("--runs", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int r) && r > 0) runsArg = r;
                }
                else if (arg.Equals("--aircraft", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    aircraftArg = args[++i];
                }
                else if (arg.Equals("--seed", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int s)) baseSeedArg = s;
                }
                else if (arg.Equals("--fast", StringComparison.OrdinalIgnoreCase))
                {
                    fastMode = true;
                }
                else if (arg.Equals("-h", StringComparison.OrdinalIgnoreCase) || arg.Equals("--help", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Uso: FlightCareer.Replay fuzz [--runs <n>] [--aircraft <modelo|all>] [--seed <n>] [--fast]");
                    return 0;
                }
            }

            bool isAll = string.Equals(aircraftArg, "all", StringComparison.OrdinalIgnoreCase);
            int runs = runsArg ?? (isAll ? 50 : 1000);
            int baseSeed = baseSeedArg ?? 42;

            var partsCatalog = PartCatalog.LoadDefault();
            var wearRuleCatalog = WearRuleCatalog.LoadDefault();
            var limitLoader = new AircraftLimitCatalogLoader();
            var resolver = limitLoader.CreateResolver();
            var allProfiles = limitLoader.LoadProfiles();
            var sensorRegistry = SensorRegistry.LoadDefault();

            List<AircraftLimitProfile> targetProfiles = new();
            if (isAll)
            {
                targetProfiles.AddRange(allProfiles.OrderBy(p => p.AircraftId, StringComparer.OrdinalIgnoreCase));
            }
            else if (!string.IsNullOrWhiteSpace(aircraftArg))
            {
                var prof = resolver.FindById(aircraftArg) ?? resolver.FindProfile(aircraftArg, aircraftArg) ??
                           allProfiles.FirstOrDefault(p => p.AircraftId.Contains(aircraftArg, StringComparison.OrdinalIgnoreCase));
                if (prof == null)
                {
                    Console.Error.WriteLine($"Erro: Aeronave '{aircraftArg}' não encontrada no catálogo de limites.");
                    return 1;
                }
                targetProfiles.Add(prof);
            }
            else
            {
                string[] standardModels = { "Cessna C152", "C172SP G1000", "Beechcraft Baron G58", "Asobo TBM 930" };
                foreach (var model in standardModels)
                {
                    var p = resolver.FindById(model) ?? resolver.FindProfile(model, model);
                    if (p != null) targetProfiles.Add(p);
                }

                if (targetProfiles.Count == 0)
                {
                    targetProfiles.AddRange(allProfiles.Take(4));
                }
            }

            // Aplica os índices elétricos padrão de teste aos perfis alvo do fuzz (não afeta o jogo)
            targetProfiles = targetProfiles
                .Select(FuzzTestDefaults.WithTestElectricalIndices)
                .ToList();

            Console.WriteLine("================================================================================");
            Console.WriteLine("           FlightCareer V6 — Fuzz Testing com Voos Sintéticos                    ");
            Console.WriteLine("================================================================================");
            if (isAll)
            {
                Console.WriteLine($"Aeronave alvo      : Todas as aeronaves do catálogo ({targetProfiles.Count} modelos)");
                Console.WriteLine($"Corridas planejadas: {runs} por aeronave ({runs * targetProfiles.Count} voos totais)");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(aircraftArg))
                {
                    Console.WriteLine($"Aeronave alvo      : {targetProfiles[0].AircraftId}");
                }
                else
                {
                    Console.WriteLine($"Aeronave alvo      : Padrão ({targetProfiles.Count} modelos)");
                }
                Console.WriteLine($"Corridas planejadas: {runs}");
            }
            Console.WriteLine($"Semente base       : {baseSeed}");
            Console.WriteLine($"Modo               : {(fastMode ? "Rápido (apenas invariantes)" : "Completo (Invariantes + Gabarito com Voo-Controle)")}");
            Console.WriteLine();

            var generator = new SyntheticFlightGenerator(resolver);
            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var summary = new FuzzRunSummary();

            string failureDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightCareer", "fuzz-failures");

            var sw = System.Diagnostics.Stopwatch.StartNew();

            void ExecuteFlightRun(AircraftLimitProfile profile, int currentSeed, int runIndex)
            {
                var rnd = new SystemRandomSource(currentSeed);
                int abuseCount = (runIndex % 5 == 0) ? 0 : 1 + (runIndex % 3);
                var plan = SyntheticFlightPlan.Random(rnd, profile, abuseCount);

                var scenario = new AircraftScenarioInput(
                    profile,
                    sensorRegistry.Sensors,
                    profile.Features,
                    profile.EngineCount,
                    profile.AircraftId);

                var flight = generator.Generate(scenario, plan, rnd);

                var result = replayer.Run(
                    flight.Frames,
                    flight.Touchdowns,
                    profile,
                    profile.Features,
                    profile.EngineCount,
                    activationMode: ReplayActivationMode.GameLike);

                var violations = new List<string>();
                violations.AddRange(SyntheticFlightOracle.VerifyInvariants(flight, result));

                if (!fastMode && flight.GroundTruth.InjectedAbuses.Count > 0)
                {
                    double controlDuration = flight.GroundTruth.IsCancelled && flight.Frames.Count > 1
                        ? (flight.Frames[^1].ReceivedUtc - flight.Frames[0].ReceivedUtc).TotalSeconds
                        : plan.TotalDurationSeconds;

                    var controlPlan = new SyntheticFlightPlan
                    {
                        TotalDurationSeconds = controlDuration,
                        SimRateSegments = plan.SimRateSegments,
                        Abuses = Array.Empty<SyntheticAbuseRecord>()
                    };

                    var controlFlight = generator.Generate(scenario, controlPlan, new SystemRandomSource(currentSeed));
                    var controlResult = replayer.Run(
                        controlFlight.Frames,
                        controlFlight.Touchdowns,
                        profile,
                        profile.Features,
                        profile.EngineCount,
                        activationMode: ReplayActivationMode.GameLike);

                    violations.AddRange(SyntheticFlightOracle.VerifyInvariants(controlFlight, controlResult));

                    foreach (var abuse in flight.GroundTruth.InjectedAbuses)
                    {
                        var gtViolations = SyntheticFlightOracle.VerifyGroundTruth(controlResult, result, abuse, profile, wearRuleCatalog);
                        violations.AddRange(gtViolations);
                    }
                }

                summary.RecordRun(profile.AircraftId, violations);

                var actualViolations = violations.Where(v => !v.StartsWith("limite ausente:", StringComparison.OrdinalIgnoreCase)).ToList();
                if (actualViolations.Count > 0)
                {
                    try
                    {
                        Directory.CreateDirectory(failureDir);
                        string baseName = $"fuzz_seed_{currentSeed}_{SensorCsvFormatter.SafeFileName(profile.AircraftId)}";

                        string csvFile = Path.Combine(failureDir, $"{baseName}.csv");
                        using (var writer = new StreamWriter(csvFile, false, Encoding.UTF8))
                        {
                            writer.WriteLine(SensorCsvFormatter.Header(flight.SensorKeys));
                            foreach (var frame in flight.Frames)
                            {
                                writer.WriteLine(SensorCsvFormatter.Row(frame.ReceivedUtc, frame, flight.SensorKeys, null));
                            }
                        }

                        string tdFile = Path.Combine(failureDir, $"{baseName}.touchdowns.json");
                        File.WriteAllText(tdFile, JsonSerializer.Serialize(flight.Touchdowns, JsonOpts));

                        string gtFile = Path.Combine(failureDir, $"{baseName}.groundtruth.json");
                        // abuseCount depende do índice da corrida: reproduz com a mesma semente base e a corrida como a última
                        string reproduceCmd = $"dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft \"{profile.AircraftId}\" --seed {currentSeed - runIndex} --runs {runIndex + 1}";
                        var failureInfo = new
                        {
                            seed = currentSeed,
                            aircraft = profile.AircraftId,
                            reproduce = reproduceCmd,
                            groundTruth = flight.GroundTruth,
                            violations = actualViolations
                        };
                        File.WriteAllText(gtFile, JsonSerializer.Serialize(failureInfo, JsonOpts));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Erro ao salvar artefatos de falha para a semente {currentSeed}: {ex.Message}");
                    }
                }
            }

            if (isAll)
            {
                for (int pIdx = 0; pIdx < targetProfiles.Count; pIdx++)
                {
                    var profile = targetProfiles[pIdx];
                    var swProf = System.Diagnostics.Stopwatch.StartNew();
                    for (int run = 0; run < runs; run++)
                    {
                        int currentSeed = baseSeed + run;
                        ExecuteFlightRun(profile, currentSeed, run);
                    }
                    swProf.Stop();
                    var acSummary = summary.GetAircraftSummary(profile.AircraftId);
                    Console.WriteLine($"[{pIdx + 1,2}/{targetProfiles.Count,2}] {profile.AircraftId,-32} | {acSummary?.TotalRuns ?? runs,3} voos | {acSummary?.Passed ?? 0,3} aprovados | {acSummary?.MissingLimits ?? 0,2} lim. ausentes | {acSummary?.Failed ?? 0,2} falhas ({swProf.Elapsed.TotalSeconds:F1}s)");
                }
            }
            else
            {
                for (int run = 0; run < runs; run++)
                {
                    int currentSeed = baseSeed + run;
                    var profile = targetProfiles[run % targetProfiles.Count];
                    ExecuteFlightRun(profile, currentSeed, run);

                    if ((run + 1) % 100 == 0 || run + 1 == runs)
                    {
                        Console.Write($"\rProcessados: {run + 1}/{runs} (Aprovados: {summary.TotalPassed}, Limite Ausente: {summary.TotalMissingLimits}, Falhas: {summary.TotalFailed})...");
                    }
                }
                Console.WriteLine();
            }

            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("Resumo por Aeronave:");
            Console.WriteLine(string.Format("{0,-32} | {1,5} | {2,9} | {3,14} | {4,6} | {5}", "AircraftId", "voos", "aprovados", "limite ausente", "falhas", "principal violação"));
            Console.WriteLine(new string('-', 95));

            foreach (var s in summary.GetOrderedAircraftSummaries())
            {
                string name = s.AircraftId.Length > 32 ? s.AircraftId.Substring(0, 32) : s.AircraftId;
                Console.WriteLine(string.Format("{0,-32} | {1,5} | {2,9} | {3,14} | {4,6} | {5}",
                    name,
                    s.TotalRuns,
                    s.Passed,
                    s.MissingLimits,
                    s.Failed,
                    s.TopViolationCategory));
            }

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("Resultado do Fuzz Testing:");
            Console.WriteLine($"  Total de Voos       : {summary.TotalRuns}");
            Console.WriteLine($"  Voos Aprovados      : {summary.TotalPassed} ({(double)summary.TotalPassed / Math.Max(1, summary.TotalRuns):P1})");
            if (summary.TotalMissingLimits > 0)
            {
                Console.WriteLine($"  Voos c/ Limite Aus. : {summary.TotalMissingLimits} ({(double)summary.TotalMissingLimits / Math.Max(1, summary.TotalRuns):P1})");
            }
            Console.WriteLine($"  Voos com Falhas     : {summary.TotalFailed} ({(double)summary.TotalFailed / Math.Max(1, summary.TotalRuns):P1})");
            Console.WriteLine($"  Tempo Total         : {sw.Elapsed.TotalSeconds:F2} s ({summary.TotalRuns / Math.Max(0.001, sw.Elapsed.TotalSeconds):F0} voos/s)");
            Console.WriteLine("--------------------------------------------------------------------------------");

            if (summary.MissingLimitsByType.Count > 0)
            {
                Console.WriteLine("Resumo dos Limites Ausentes no Catálogo:");
                foreach (var (key, count) in summary.MissingLimitsByType.OrderByDescending(kv => kv.Value))
                {
                    Console.WriteLine($"  • {key}: {count} ocorrências");
                }
                Console.WriteLine();
            }

            if (summary.ViolationsByCategory.Count > 0)
            {
                Console.WriteLine("Resumo das Violações Encontradas:");
                foreach (var (type, count) in summary.ViolationsByCategory.OrderByDescending(kv => kv.Value))
                {
                    Console.WriteLine($"  • {type}: {count} ocorrências");
                }
                Console.WriteLine();
                Console.WriteLine($"Artefatos de reprodução salvos em:\n  {failureDir}");
            }
            else
            {
                Console.WriteLine("✅ Nenhuma invariante ou gabarito violado em todas as corridas executadas!");
            }

            Console.WriteLine("================================================================================");
            return summary.TotalFailed == 0 ? 0 : 1;
        }

        private static AircraftLimitProfile? ResolveProfile(
            string csvPath,
            string? aircraftArg,
            AircraftLimitResolver resolver,
            IReadOnlyList<AircraftLimitProfile> allProfiles)
        {
            if (!string.IsNullOrWhiteSpace(aircraftArg))
            {
                // Busca direta por ID
                var byId = resolver.FindById(aircraftArg);
                if (byId != null) return byId;

                // Busca por title / atcModel
                var byMatch = resolver.FindProfile(aircraftArg, aircraftArg);
                if (byMatch != null) return byMatch;

                // Busca parcial case-insensitive
                var partial = allProfiles.FirstOrDefault(p =>
                    p.AircraftId.Contains(aircraftArg, StringComparison.OrdinalIgnoreCase) ||
                    p.TitlePatterns.Any(t => t.Contains(aircraftArg, StringComparison.OrdinalIgnoreCase)));
                if (partial != null) return partial;

                return null;
            }

            // Tenta deduzir a partir do arquivo -context.json
            string contextPath = TryFindContextJson(csvPath);
            if (File.Exists(contextPath))
            {
                try
                {
                    string json = File.ReadAllText(contextPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    string? title = null;
                    if (root.TryGetProperty("Title", out var tProp) && tProp.ValueKind == JsonValueKind.String)
                    {
                        title = tProp.GetString();
                    }

                    string? atcModel = null;
                    if (root.TryGetProperty("Strings", out var stringsProp) && stringsProp.ValueKind == JsonValueKind.Object)
                    {
                        if (string.IsNullOrWhiteSpace(title) && stringsProp.TryGetProperty("ctx-title", out var ctxTitleProp))
                        {
                            title = ctxTitleProp.GetString();
                        }
                        if (stringsProp.TryGetProperty("ctx-atc-model", out var atcProp))
                        {
                            atcModel = atcProp.GetString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(atcModel))
                    {
                        var match = resolver.FindProfile(title, atcModel);
                        if (match != null) return match;

                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            var byId = resolver.FindById(title);
                            if (byId != null) return byId;
                        }
                    }
                }
                catch
                {
                    // Ignora falha de parse do contexto
                }
            }

            // Tenta inferir pelo nome do arquivo CSV (ex.: 20261115-1747-Cessna_C152.csv)
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                fileName = Path.GetFileNameWithoutExtension(fileName);
            }

            var candidateProfile = allProfiles.FirstOrDefault(p =>
                fileName.Contains(p.AircraftId.Replace(" ", "_"), StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains(p.AircraftId, StringComparison.OrdinalIgnoreCase));

            return candidateProfile;
        }

        private static string TryFindContextJson(string csvPath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(csvPath)) ?? ".";
            string name = Path.GetFileName(csvPath);

            if (name.EndsWith(".csv.gz", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 7);
            else if (name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            string directContext = Path.Combine(dir, $"{name}-context.json");
            if (File.Exists(directContext)) return directContext;

            // Busca na pasta sensor-logs do usuário
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLogs = Path.Combine(localAppData, "FlightCareer", "sensor-logs", $"{name}-context.json");
            if (File.Exists(appDataLogs)) return appDataLogs;

            return directContext;
        }

        private static string TryFindAdjacentTouchdownsFile(string csvPath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(csvPath)) ?? ".";
            string name = Path.GetFileName(csvPath);

            if (name.EndsWith(".csv.gz", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 7);
            else if (name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            return Path.Combine(dir, $"{name}.touchdowns.json");
        }

        private static void PrintHumanReadableResult(
            string csvPath,
            AircraftLimitProfile profile,
            FlightReplayResult result,
            int markerCount)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("                  FlightCareer V6 — Relatório de Replay de Voo                  ");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Arquivo de Entrada : {Path.GetFileName(csvPath)}");
            Console.WriteLine($"Aeronave           : {profile.AircraftId} (Classe: {profile.Class}, Motores: {profile.EngineCount})");
            Console.WriteLine($"Features Técnicas  : {string.Join(", ", profile.Features)}");
            Console.WriteLine($"Quadros Processados: {result.TotalTicks:N0} frames ({markerCount} marcadores)");
            Console.WriteLine($"Tempo Real Gravado : {result.ElapsedWallTime:hh\\:mm\\:ss}");
            Console.WriteLine($"Tempo Simulado     : {result.TotalSimHours:F4} h ({(result.TotalSimHours * 60.0):F1} min)");
            Console.WriteLine();

            // Top 15 peças
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("Top Peças por Desgaste (Saúde e Ciclos)");
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($"{"Peça",-30} {"Motor",-6} {"Inicial",-8} {"Final",-8} {"Perda %",-9} {"Horas",-9} {"Ciclos",-6}");

            var topParts = result.Parts
                .OrderByDescending(p => p.HealthLoss)
                .ThenBy(p => p.FinalHealth)
                .Take(15)
                .ToList();

            foreach (var p in topParts)
            {
                string lossStr = p.HealthLoss > 0.001 ? $"-{p.HealthLoss:F2}%" : "0.00%";
                Console.WriteLine($"{p.PartId,-30} {p.EngineIndex,5} {p.InitialHealth,7:F1}% {p.FinalHealth,7:F1}% {lossStr,9} {p.HoursSinceReplacement,7:F3}h {p.CyclesSinceReplacement,6}");
            }
            Console.WriteLine();

            // Excedências
            Console.WriteLine("--------------------------------------------------------------------------------");
            var consolidatedExceedances = result.Exceedances
                .GroupBy(e => (e.RuleId, e.TargetId))
                .Select(g => new
                {
                    RuleId = g.Key.RuleId,
                    TargetId = g.Key.TargetId,
                    Peak = g.Max(x => x.PeakValue),
                    Limit = g.First().LimitValue,
                    MaxDuration = g.Max(x => x.DurationSeconds),
                    TotalDamage = g.Sum(x => x.TotalDamagePct)
                })
                .OrderByDescending(x => x.TotalDamage)
                .ThenBy(x => x.RuleId)
                .ToList();

            Console.WriteLine($"Excedências de Limites Operacionais Consolidadas ({consolidatedExceedances.Count})");
            Console.WriteLine("--------------------------------------------------------------------------------");
            if (consolidatedExceedances.Count == 0)
            {
                Console.WriteLine("  Nenhuma excedência de limite operacional registrada.");
            }
            else
            {
                Console.WriteLine($"{"Regra",-24} {"Alvo",-24} {"Pico",-10} {"Limite",-10} {"Duração",-9} {"Perda %",-8}");
                foreach (var exc in consolidatedExceedances)
                {
                    string lossStr = exc.TotalDamage > 0 ? $"-{exc.TotalDamage:F4}%" : "0.00%";
                    Console.WriteLine($"{exc.RuleId,-24} {exc.TargetId,-24} {exc.Peak,9:F1} {exc.Limit,9:F1} {exc.MaxDuration,7:F1}s {lossStr,8}");
                }
            }
            Console.WriteLine();

            // Inspeções
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($"Inspeções Requeridas por Operação / Impacto ({result.Inspections.Count})");
            Console.WriteLine("--------------------------------------------------------------------------------");
            if (result.Inspections.Count == 0)
            {
                Console.WriteLine("  Nenhuma inspeção por estresse ou impacto gerada.");
            }
            else
            {
                foreach (var insp in result.Inspections)
                {
                    string groundStr = insp.IsGrounding ? " [NO-GO]" : "";
                    Console.WriteLine($"  [{insp.DefectCode}]{groundStr} {insp.TargetPartId}: {insp.Description}");
                }
            }
            Console.WriteLine();

            // Eventos Derivados
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($"Eventos Operacionais Derivados ({result.DerivedEvents.Count})");
            Console.WriteLine("--------------------------------------------------------------------------------");
            if (result.DerivedEvents.Count == 0)
            {
                Console.WriteLine("  Nenhum evento operacional derivado registrado.");
            }
            else
            {
                foreach (var ev in result.DerivedEvents)
                {
                    string engStr = ev.EngineIndex.HasValue ? $" (Motor {ev.EngineIndex.Value})" : "";
                    Console.WriteLine($"  +{ev.ElapsedSeconds,7:F1}s | {ev.Utc:HH:mm:ss} UTC | {ev.MetricKey}{engStr}: {ev.Value:F1}");
                }
            }
            Console.WriteLine();

            // Diagnósticos
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($"Mensagens de Diagnóstico do Voo ({result.Diagnostics.Count})");
            Console.WriteLine("--------------------------------------------------------------------------------");
            if (result.Diagnostics.Count == 0)
            {
                Console.WriteLine("  Nenhum diagnóstico emitido.");
            }
            else
            {
                foreach (var diag in result.Diagnostics)
                {
                    Console.WriteLine($"  • {diag}");
                }
            }
            Console.WriteLine("================================================================================");
        }
    }
}
