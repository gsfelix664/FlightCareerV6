using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlightCareer.Domain.Sensors.Probe;
using Microsoft.FlightSimulator.SimConnect;

namespace FlightCareer.SimVarProbe
{
    public sealed class ProbeSessionConfiguration
    {
        public string ListPath { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public int? DurationSeconds { get; set; }
        public int MaxEngine { get; set; } = 4;
        public int MaxContact { get; set; } = 5;
        public int MaxGeneric { get; set; } = 4;
        public string? Filter { get; set; }
        public bool ProbeOnly { get; set; }
        public int ProbeTimeoutSeconds { get; set; } = 15;
        public int BlockSize { get; set; } = 200;
        public int? IntervalFrames { get; set; }
    }

    public enum ProbeSimConnectId : int { }

    public sealed class ProbeSession : IDisposable
    {
        private const int TitleDefId = 999;
        private const int TitleReqId = 999;

        private const int PhaseABaseDefId = 10000;
        private const int PhaseABaseReqId = 10000;

        private const int PhaseBBlockBaseDefId = 50000;
        private const int PhaseBBlockBaseReqId = 50000;

        private const int PhaseBStringBaseDefId = 60000;
        private const int PhaseBStringBaseReqId = 60000;

        private readonly ProbeSessionConfiguration _config;
        private readonly AutoResetEvent _waitHandle = new(false);
        private readonly CancellationTokenSource _cts = new();

        private SimConnect? _simConnect;
        private Thread? _messageLoopThread;
        private volatile bool _connected;
        private volatile bool _running = true;

        private IReadOnlyList<ProbeSlot> _allSlots = Array.Empty<ProbeSlot>();
        private readonly Dictionary<string, ProbeValidationResult> _validationResults = new(StringComparer.Ordinal);
        private readonly ProbeStatsAccumulator _statsAccumulator = new();

        // Mapeamentos da Fase A
        private readonly ConcurrentDictionary<uint, ProbeSlot> _phaseASendIdToSlot = new();
        private readonly ConcurrentDictionary<int, ProbeSlot> _phaseAReqIdToSlot = new();
        private readonly ConcurrentDictionary<string, bool> _phaseABatchCompletedSlots = new(StringComparer.Ordinal);

        // Mapeamentos da Fase B
        private readonly ConcurrentDictionary<uint, ProbeSlot> _phaseBSendIdToSlot = new();
        private readonly ConcurrentDictionary<int, ProbeBlock> _phaseBReqIdToBlock = new();
        private readonly ConcurrentDictionary<int, ProbeSlot> _phaseBReqIdToStringSlot = new();
        private readonly ConcurrentDictionary<string, double> _currentNumericValues = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _lastStringValues = new(StringComparer.Ordinal);
        private readonly ConcurrentBag<ProbeSlot> _phaseBRebuildQueue = new();

        private string _aircraftTitle = "MSFS";
        private double _currentSimRate = 1.0;
        private int _markerCounter = 0;
        private string? _activeMarker = null;
        private readonly object _markerLock = new();

        private string _sessionOutputDir = string.Empty;
        private StreamWriter? _valuesCsvWriter;
        private StreamWriter? _stringsCsvWriter;
        private readonly object _fileWriteLock = new();
        private DateTime _sessionStartTime;

        public ProbeSession(ProbeSessionConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public int Run()
        {
            _sessionStartTime = DateTime.UtcNow;

            Console.WriteLine("=================================================");
            Console.WriteLine("   FlightCareer SimVarProbe — Sonda de SimVars   ");
            Console.WriteLine("=================================================");
            Console.WriteLine($"Lista: {_config.ListPath}");
            Console.WriteLine($"Saída: {_config.OutputDirectory}");
            if (!string.IsNullOrEmpty(_config.Filter))
            {
                Console.WriteLine($"Filtro: {_config.Filter}");
            }
            Console.WriteLine($"Motores: {_config.MaxEngine} | Contatos: {_config.MaxContact} | Genéricos: {_config.MaxGeneric}");
            Console.WriteLine($"Timeout sondagem: {_config.ProbeTimeoutSeconds} s | Tamanho bloco: {_config.BlockSize}");
            if (_config.DurationSeconds.HasValue)
            {
                Console.WriteLine($"Duração programada: {_config.DurationSeconds.Value} s");
            }
            if (_config.ProbeOnly)
            {
                Console.WriteLine("Modo: Somente Sondagem (--probe-only)");
            }
            Console.WriteLine();

            // 1. Carregar lista e expandir slots
            Console.WriteLine("Carregando lista de SimVars...");
            IReadOnlyList<ProbeEntry> entries;
            try
            {
                entries = ProbeListLoader.Load(_config.ListPath);
                Console.WriteLine($"Entradas carregadas: {entries.Count}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao carregar lista de SimVars: {ex.Message}");
                return 1;
            }

            var probeOptions = new ProbeOptions(
                MaxEngine: _config.MaxEngine,
                MaxContact: _config.MaxContact,
                MaxGeneric: _config.MaxGeneric,
                Filter: _config.Filter,
                BlockSize: _config.BlockSize);

            _allSlots = ProbeSlotExpander.Expand(entries, probeOptions);
            Console.WriteLine($"Slots expandidos para teste: {_allSlots.Count}");
            if (_allSlots.Count == 0)
            {
                Console.WriteLine("Nenhum slot para testar com os filtros atuais.");
                return 0;
            }

            // Inicializa resultados de validação como NoResponse
            foreach (var slot in _allSlots)
            {
                _validationResults[slot.Name] = ProbeValidationResult.NoResponse();
            }

            // 2. Conectar ao SimConnect
            Console.WriteLine("Conectando ao Microsoft Flight Simulator...");
            try
            {
                _simConnect = new SimConnect("FlightCareer SimVarProbe", IntPtr.Zero, 0, _waitHandle, 0);
            }
            catch (COMException ex)
            {
                Console.Error.WriteLine($"Não foi possível conectar ao Microsoft Flight Simulator: {ex.Message} (HRESULT 0x{ex.ErrorCode:X8})");
                Console.Error.WriteLine("Verifique se o simulador está aberto e em voo.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro inesperado na conexão SimConnect: {ex.Message}");
                return 1;
            }

            _connected = true;
            _simConnect.OnRecvOpen += SimConnect_OnRecvOpen;
            _simConnect.OnRecvQuit += (s, e) =>
            {
                Console.WriteLine("\n[SimConnect] O simulador foi encerrado.");
                _connected = false;
                _running = false;
            };
            _simConnect.OnRecvException += SimConnect_OnRecvException;
            _simConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;

            // Inicia thread de mensagens SimConnect
            _messageLoopThread = new Thread(MessageLoop)
            {
                IsBackground = true,
                Name = "SimVarProbe-SimConnect-Loop"
            };
            _messageLoopThread.Start();

            // Requisita TITLE da aeronave
            RequestAircraftTitle();
            Thread.Sleep(200);

            // 3. FASE A — Sondagem
            Console.WriteLine();
            Console.WriteLine("=== FASE A: SONDAGEM E VALIDAÇÃO DOS NOMES ===");
            ExecutePhaseA();

            // Cria diretório da sessão
            SetupOutputDirectory();

            if (_config.ProbeOnly)
            {
                Console.WriteLine("Modo --probe-only ativo. Finalizando após Fase A.");
                FinalizeSession();
                return 0;
            }

            // 4. FASE B — Gravação Contínua
            Console.WriteLine();
            Console.WriteLine("=== FASE B: GRAVAÇÃO CONTÍNUA ===");
            ExecutePhaseB();

            FinalizeSession();
            return 0;
        }

        private void RequestAircraftTitle()
        {
            if (_simConnect == null) return;
            try
            {
                var defId = (ProbeSimConnectId)TitleDefId;
                var reqId = (ProbeSimConnectId)TitleReqId;

                _simConnect.ClearDataDefinition(defId);
                _simConnect.AddToDataDefinition(defId, "TITLE", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simConnect.RegisterDataDefineStruct<ProbeStringData>(defId);
                _simConnect.RequestDataOnSimObject(reqId, defId,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SimVarProbe] Falha ao solicitar TITLE: {ex.Message}");
            }
        }

        private void ExecutePhaseA()
        {
            int batchSize = 200;
            int totalBatches = (_allSlots.Count + batchSize - 1) / batchSize;
            int timeoutMsPerBatch = Math.Max(2000, (_config.ProbeTimeoutSeconds * 1000) / Math.Max(1, totalBatches));

            for (int b = 0; b < totalBatches && _running && _connected; b++)
            {
                int start = b * batchSize;
                int count = Math.Min(batchSize, _allSlots.Count - start);
                var batch = _allSlots.Skip(start).Take(count).ToList();

                _phaseABatchCompletedSlots.Clear();

                // Registra e requisita os slots do lote
                for (int i = 0; i < batch.Count; i++)
                {
                    var slot = batch[i];
                    var defId = (ProbeSimConnectId)(PhaseABaseDefId + i);
                    var reqId = (ProbeSimConnectId)(PhaseABaseReqId + i);

                    try
                    {
                        _simConnect!.ClearDataDefinition(defId);

                        if (slot.Entry.IsString)
                        {
                            _simConnect.AddToDataDefinition(defId, slot.Name, null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                            _simConnect.RegisterDataDefineStruct<ProbeStringData>(defId);
                        }
                        else
                        {
                            _simConnect.AddToDataDefinition(defId, slot.Name, slot.Entry.Unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                            _simConnect.RegisterDataDefineStruct<ProbeSingleFloatData>(defId);
                        }

                        uint sendId = _simConnect.GetLastSentPacketID();
                        if (sendId != 0)
                        {
                            _phaseASendIdToSlot[sendId] = slot;
                        }

                        _phaseAReqIdToSlot[(int)reqId] = slot;

                        _simConnect.RequestDataOnSimObject(reqId, defId,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SimVarProbe] Exceção ao registrar slot {slot.Name}: {ex.Message}");
                        _validationResults[slot.Name] = ProbeValidationResult.Error(ex.Message);
                        _phaseABatchCompletedSlots[slot.Name] = true;
                    }
                }

                // Aguarda respostas do lote
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < timeoutMsPerBatch && _running && _connected)
                {
                    int completed = _phaseABatchCompletedSlots.Count;
                    PrintPhaseAProgress(b + 1, totalBatches, batch.Count - completed);

                    if (completed >= batch.Count)
                    {
                        break;
                    }

                    Thread.Sleep(30);
                }

                // Limpa definições do lote
                for (int i = 0; i < batch.Count; i++)
                {
                    var slot = batch[i];
                    var defId = (ProbeSimConnectId)(PhaseABaseDefId + i);
                    var reqId = (ProbeSimConnectId)(PhaseABaseReqId + i);

                    _phaseAReqIdToSlot.TryRemove((int)reqId, out _);

                    try
                    {
                        _simConnect!.ClearDataDefinition(defId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SimVarProbe] Falha ao limpar definição da Fase A ({slot.Name}): {ex.Message}");
                    }
                }
            }

            Console.WriteLine();
            int totalAccepted = _validationResults.Values.Count(v => v.ReceivedResponse && !v.IsRejected && string.IsNullOrEmpty(v.ExceptionMessage));
            int totalRejected = _validationResults.Values.Count(v => v.IsRejected);
            int totalErrors = _validationResults.Values.Count(v => !v.IsRejected && !string.IsNullOrEmpty(v.ExceptionMessage));
            int totalNoResponse = _validationResults.Values.Count(v => !v.ReceivedResponse);

            Console.WriteLine($"[Fase A Concluída] Total: {_allSlots.Count} | Aceitas: {totalAccepted} | Rejeitadas: {totalRejected} | Erros: {totalErrors} | Sem Resposta: {totalNoResponse}");
        }

        private void PrintPhaseAProgress(int currentBatch, int totalBatches, int pendingInBatch)
        {
            int accepted = _validationResults.Values.Count(v => v.ReceivedResponse && !v.IsRejected && string.IsNullOrEmpty(v.ExceptionMessage));
            int rejected = _validationResults.Values.Count(v => v.IsRejected);
            int errors = _validationResults.Values.Count(v => !v.IsRejected && !string.IsNullOrEmpty(v.ExceptionMessage));

            Console.Write($"\r[Fase A] Lote {currentBatch}/{totalBatches} | Aceitas: {accepted} | Rejeitadas: {rejected} | Erros: {errors} | Pendentes no lote: {pendingInBatch}    ");
        }

        private void SetupOutputDirectory()
        {
            string baseDir = string.IsNullOrWhiteSpace(_config.OutputDirectory)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightCareer", "probe-logs")
                : _config.OutputDirectory;

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            string safeTitle = SanitizeFileName(_aircraftTitle);
            _sessionOutputDir = Path.Combine(baseDir, $"{timestamp}-{safeTitle}");

            if (!Directory.Exists(_sessionOutputDir))
            {
                Directory.CreateDirectory(_sessionOutputDir);
            }

            Console.WriteLine($"Pasta de saída da sessão: {_sessionOutputDir}");
        }

        private void ExecutePhaseB()
        {
            var acceptedNumericSlots = _allSlots
                .Where(s => !s.Entry.IsString)
                .Where(s => _validationResults.TryGetValue(s.Name, out var v) && v.ReceivedResponse && !v.IsRejected && string.IsNullOrEmpty(v.ExceptionMessage))
                .ToList();

            var acceptedStringSlots = _allSlots
                .Where(s => s.Entry.IsString)
                .Where(s => _validationResults.TryGetValue(s.Name, out var v) && v.ReceivedResponse && !v.IsRejected && string.IsNullOrEmpty(v.ExceptionMessage))
                .ToList();

            Console.WriteLine($"Slots numéricos para gravação: {acceptedNumericSlots.Count}");
            Console.WriteLine($"Strings para gravação: {acceptedStringSlots.Count}");

            if (acceptedNumericSlots.Count == 0 && acceptedStringSlots.Count == 0)
            {
                Console.WriteLine("Nenhum slot aceito para gravação na Fase B.");
                return;
            }

            // Abre streams de CSV
            string valuesPath = Path.Combine(_sessionOutputDir, "values.csv");
            string stringsPath = Path.Combine(_sessionOutputDir, "strings.csv");

            _valuesCsvWriter = new StreamWriter(valuesPath, append: false, ProbeCsvWriter.Utf8WithBom);
            _stringsCsvWriter = new StreamWriter(stringsPath, append: false, ProbeCsvWriter.Utf8WithBom);

            // Cabeçalho de values.csv: utc,sim_rate,marker,<slot1>,<slot2>,...
            var valHeader = new List<string> { "utc", "sim_rate", "marker" };
            foreach (var slot in acceptedNumericSlots)
            {
                valHeader.Add(slot.Name);
            }
            _valuesCsvWriter.WriteLine(ProbeCsvWriter.FormatCsvRow(valHeader));
            _valuesCsvWriter.Flush();

            // Cabeçalho de strings.csv: utc,slot,valor
            _stringsCsvWriter.WriteLine("utc,slot,valor");
            _stringsCsvWriter.Flush();

            // Planejamento de blocos numéricos
            // O struct ProbeBlockData tem 200 posições fixas: --block-size só pode reduzir o bloco.
            int blockSize = Math.Clamp(_config.BlockSize, 1, 200);
            var blocks = ProbeBlockPlanner.Plan(acceptedNumericSlots, blockSize: blockSize);
            Console.WriteLine($"Blocos numéricos montados: {blocks.Count} (até {blockSize} variáveis por bloco)");

            // Registra blocos numéricos
            foreach (var block in blocks)
            {
                RegisterNumericBlock(block);
            }

            // Registra strings
            for (int i = 0; i < acceptedStringSlots.Count; i++)
            {
                var strSlot = acceptedStringSlots[i];
                RegisterStringSlot(i, strSlot);
            }

            Console.WriteLine();
            Console.WriteLine("Gravação iniciada a cada 1 s.");
            Console.WriteLine("Controles no teclado:");
            Console.WriteLine("  - Digite 'm' ou 'm <texto>' e pressione Enter para registrar uma marca no log.");
            Console.WriteLine("  - Digite 'q' e pressione Enter ou aperte Ctrl+C para finalizar.");
            Console.WriteLine();

            // Inicia thread de leitura de comandos do teclado
            var inputThread = new Thread(KeyboardLoop)
            {
                IsBackground = true,
                Name = "SimVarProbe-Keyboard"
            };
            inputThread.Start();

            // Loop de gravação em ciclos de 1 segundo
            var phaseBStart = DateTime.UtcNow;
            var nextTick = DateTime.UtcNow.AddSeconds(1);

            while (_running && _connected)
            {
                // Verifica se há reconstruções de blocos pendentes (regra do LICOES: fora do handler)
                ProcessPendingRebuilds();

                var now = DateTime.UtcNow;
                if (now >= nextTick)
                {
                    WriteNumericCycle(acceptedNumericSlots, now);
                    nextTick = now.AddSeconds(1);
                }

                if (_config.DurationSeconds.HasValue)
                {
                    var elapsed = (DateTime.UtcNow - phaseBStart).TotalSeconds;
                    if (elapsed >= _config.DurationSeconds.Value)
                    {
                        Console.WriteLine($"\nDuração limite atingida ({_config.DurationSeconds.Value} s). Encerrando gravação...");
                        _running = false;
                        break;
                    }
                }

                Thread.Sleep(50);
            }
        }

        private void RegisterNumericBlock(ProbeBlock block)
        {
            if (_simConnect == null) return;

            var defId = (ProbeSimConnectId)(PhaseBBlockBaseDefId + block.Index);
            var reqId = (ProbeSimConnectId)(PhaseBBlockBaseReqId + block.Index);

            try
            {
                _simConnect.ClearDataDefinition(defId);
                _phaseBReqIdToBlock[block.Index] = block;

                for (int i = 0; i < block.Slots.Count; i++)
                {
                    var slot = block.Slots[i];
                    _simConnect.AddToDataDefinition(defId, slot.Name, slot.Entry.Unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    uint sendId = _simConnect.GetLastSentPacketID();
                    if (sendId != 0)
                    {
                        _phaseBSendIdToSlot[sendId] = slot;
                    }
                }

                // Preenchimento até 200 com SIMULATION RATE
                int paddingCount = 200 - block.Slots.Count;
                for (int p = 0; p < paddingCount; p++)
                {
                    _simConnect.AddToDataDefinition(defId, "SIMULATION RATE", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                }

                _simConnect.RegisterDataDefineStruct<ProbeBlockData>(defId);

                if (_config.IntervalFrames.HasValue && _config.IntervalFrames.Value > 0)
                {
                    _simConnect.RequestDataOnSimObject(reqId, defId,
                        SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, (uint)_config.IntervalFrames.Value, 0);
                }
                else
                {
                    _simConnect.RequestDataOnSimObject(reqId, defId,
                        SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SimVarProbe] Erro ao registrar bloco numérico {block.Index}: {ex.Message}");
            }
        }

        private void RegisterStringSlot(int index, ProbeSlot slot)
        {
            if (_simConnect == null) return;

            var defId = (ProbeSimConnectId)(PhaseBStringBaseDefId + index);
            var reqId = (ProbeSimConnectId)(PhaseBStringBaseReqId + index);

            try
            {
                _simConnect.ClearDataDefinition(defId);
                _phaseBReqIdToStringSlot[index] = slot;

                _simConnect.AddToDataDefinition(defId, slot.Name, null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simConnect.RegisterDataDefineStruct<ProbeStringData>(defId);

                _simConnect.RequestDataOnSimObject(reqId, defId,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SimVarProbe] Erro ao registrar string {slot.Name}: {ex.Message}");
            }
        }

        private void ProcessPendingRebuilds()
        {
            if (_phaseBRebuildQueue.IsEmpty) return;

            var rejectedSlots = new HashSet<string>(StringComparer.Ordinal);
            while (_phaseBRebuildQueue.TryTake(out var slot))
            {
                rejectedSlots.Add(slot.Name);
            }

            // Para cada bloco que contenha algum dos slots rejeitados, reconstrói
            foreach (var kvp in _phaseBReqIdToBlock.ToList())
            {
                var block = kvp.Value;
                bool hasRejected = block.Slots.Any(s => rejectedSlots.Contains(s.Name));
                if (hasRejected)
                {
                    var survivingSlots = block.Slots.Where(s => !rejectedSlots.Contains(s.Name)).ToList();
                    Console.WriteLine($"[Fase B] Reconstruindo bloco {block.Index} sem {block.Slots.Count - survivingSlots.Count} slot(s) rejeitado(s)...");

                    var newBlock = new ProbeBlock(block.Index, survivingSlots);
                    RegisterNumericBlock(newBlock);
                }
            }
        }

        private void WriteNumericCycle(IReadOnlyList<ProbeSlot> slots, DateTime utcNow)
        {
            if (_valuesCsvWriter == null) return;

            string markerToPrint = string.Empty;
            lock (_markerLock)
            {
                if (_activeMarker != null)
                {
                    markerToPrint = _activeMarker;
                    _activeMarker = null;
                }
            }

            var rowFields = new List<string>(slots.Count + 3)
            {
                utcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                _currentSimRate.ToString("G", CultureInfo.InvariantCulture),
                markerToPrint
            };

            foreach (var slot in slots)
            {
                if (_currentNumericValues.TryGetValue(slot.Name, out double val))
                {
                    rowFields.Add(ProbeCsvWriter.FormatDouble(val));
                }
                else
                {
                    rowFields.Add(string.Empty);
                }
            }

            lock (_fileWriteLock)
            {
                _valuesCsvWriter.WriteLine(ProbeCsvWriter.FormatCsvRow(rowFields));
                _valuesCsvWriter.Flush();
            }
        }

        private void WriteStringChange(ProbeSlot slot, string newValue, DateTime utcNow)
        {
            if (_stringsCsvWriter == null) return;

            lock (_fileWriteLock)
            {
                var fields = new string[]
                {
                    utcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    slot.Name,
                    newValue
                };
                _stringsCsvWriter.WriteLine(ProbeCsvWriter.FormatCsvRow(fields));
                _stringsCsvWriter.Flush();
            }
        }

        private void KeyboardLoop()
        {
            while (_running && _connected)
            {
                try
                {
                    string? line = Console.ReadLine();
                    if (line == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    string trimmed = line.Trim();
                    if (string.Equals(trimmed, "q", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Comando 'q' recebido. Encerrando gravação...");
                        _running = false;
                        break;
                    }

                    if (trimmed.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                    {
                        string comment = trimmed.Length > 1 ? trimmed[1..].Trim() : string.Empty;
                        lock (_markerLock)
                        {
                            _markerCounter++;
                            _activeMarker = string.IsNullOrEmpty(comment)
                                ? $"{_markerCounter}"
                                : $"{_markerCounter}: {comment}";
                            Console.WriteLine($"[Marca #{_markerCounter}] '{_activeMarker}' registrada.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[SimVarProbe] Leitura do teclado encerrada: {ex.Message}");
                    break;
                }
            }
        }

        public void Stop()
        {
            _running = false;
        }

        private void FinalizeSession()
        {
            Console.WriteLine();
            Console.WriteLine("Gerando relatórios finais...");

            var duration = DateTime.UtcNow - _sessionStartTime;

            // Fecha arquivos de gravação da Fase B
            lock (_fileWriteLock)
            {
                _valuesCsvWriter?.Flush();
                _valuesCsvWriter?.Dispose();
                _valuesCsvWriter = null;

                _stringsCsvWriter?.Flush();
                _stringsCsvWriter?.Dispose();
                _stringsCsvWriter = null;
            }

            // Constrói relatório
            var rows = ProbeReportBuilder.BuildRows(_allSlots, _validationResults, _statsAccumulator);

            string reportCsvPath = Path.Combine(_sessionOutputDir, "report.csv");
            string summaryTxtPath = Path.Combine(_sessionOutputDir, "summary.txt");

            string reportContent = ProbeReportBuilder.GenerateReportCsv(rows);
            File.WriteAllText(reportCsvPath, reportContent, ProbeCsvWriter.Utf8WithBom);

            string summaryContent = ProbeReportBuilder.GenerateSummaryText(rows, _aircraftTitle, duration);
            File.WriteAllText(summaryTxtPath, summaryContent, ProbeCsvWriter.Utf8WithBom);

            Console.WriteLine("=================================================");
            Console.WriteLine("            Sessão Concluída com Sucesso         ");
            Console.WriteLine("=================================================");
            Console.WriteLine($"Pasta dos arquivos: {_sessionOutputDir}");
            Console.WriteLine($"  - report.csv:  {reportCsvPath}");
            Console.WriteLine($"  - summary.txt: {summaryTxtPath}");
            if (!_config.ProbeOnly)
            {
                Console.WriteLine($"  - values.csv:  {Path.Combine(_sessionOutputDir, "values.csv")}");
                Console.WriteLine($"  - strings.csv: {Path.Combine(_sessionOutputDir, "strings.csv")}");
            }
            Console.WriteLine();
        }

        private void MessageLoop()
        {
            while (!_cts.IsCancellationRequested && _connected && _simConnect != null)
            {
                try
                {
                    if (_waitHandle.WaitOne(50))
                    {
                        _simConnect.ReceiveMessage();
                    }
                }
                catch (COMException)
                {
                    _connected = false;
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SimVarProbe] Exceção em ReceiveMessage: {ex.Message}");
                }
            }
        }

        #region SimConnect Event Handlers

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Debug.WriteLine("[SimVarProbe] Conectado ao MSFS com sucesso.");
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV_QUIT data)
        {
            Console.WriteLine("\n[SimConnect] O simulador foi encerrado.");
            _connected = false;
            _running = false;
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            var exceptionType = (SIMCONNECT_EXCEPTION)data.dwException;

            // Fase A
            if (_phaseASendIdToSlot.TryGetValue(data.dwSendID, out var phaseASlot))
            {
                if (exceptionType == SIMCONNECT_EXCEPTION.NAME_UNRECOGNIZED)
                {
                    _validationResults[phaseASlot.Name] = ProbeValidationResult.Rejected("NAME_UNRECOGNIZED");
                }
                else
                {
                    _validationResults[phaseASlot.Name] = ProbeValidationResult.Error(exceptionType.ToString());
                }
                _phaseABatchCompletedSlots[phaseASlot.Name] = true;
                return;
            }

            // Fase B
            if (_phaseBSendIdToSlot.TryGetValue(data.dwSendID, out var phaseBSlot))
            {
                Debug.WriteLine($"[SimVarProbe Fase B EXCEPTION] Slot={phaseBSlot.Name}, Exception={exceptionType}, SendID={data.dwSendID}");
                _validationResults[phaseBSlot.Name] = ProbeValidationResult.Rejected(exceptionType.ToString());
                _phaseBRebuildQueue.Add(phaseBSlot);
                return;
            }

            Debug.WriteLine($"[SimVarProbe EXCEPTION] Tipo={exceptionType}, SendID={data.dwSendID}, Index={data.dwIndex}");
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            int reqId = (int)data.dwRequestID;

            // TITLE da aeronave
            if (reqId == TitleReqId)
            {
                if (data.dwData != null && data.dwData.Length > 0 && data.dwData[0] is ProbeStringData strData)
                {
                    if (!string.IsNullOrWhiteSpace(strData.Value))
                    {
                        _aircraftTitle = strData.Value.Trim();
                    }
                }
                return;
            }

            // Respostas da Fase A
            if (_phaseAReqIdToSlot.TryGetValue(reqId, out var slot))
            {
                _validationResults[slot.Name] = ProbeValidationResult.Accepted();
                _phaseABatchCompletedSlots[slot.Name] = true;

                if (slot.Entry.IsString)
                {
                    if (data.dwData != null && data.dwData.Length > 0 && data.dwData[0] is ProbeStringData sData)
                    {
                        _statsAccumulator.RecordString(slot.Name, sData.Value);
                    }
                }
                else
                {
                    if (data.dwData != null && data.dwData.Length > 0 && data.dwData[0] is ProbeSingleFloatData fData)
                    {
                        _statsAccumulator.RecordNumeric(slot.Name, fData.Value);
                    }
                }
                return;
            }

            // Respostas numéricas da Fase B
            if (reqId >= PhaseBBlockBaseReqId && reqId < PhaseBStringBaseReqId)
            {
                int blockIndex = reqId - PhaseBBlockBaseReqId;
                if (_phaseBReqIdToBlock.TryGetValue(blockIndex, out var block))
                {
                    if (data.dwData != null && data.dwData.Length > 0 && data.dwData[0] is ProbeBlockData blockData && blockData.Values != null)
                    {
                        int count = Math.Min(block.Slots.Count, blockData.Values.Length);
                        for (int i = 0; i < count; i++)
                        {
                            var s = block.Slots[i];
                            double val = blockData.Values[i];

                            _currentNumericValues[s.Name] = val;
                            _statsAccumulator.RecordNumeric(s.Name, val);

                            if (string.Equals(s.Name, "SIMULATION RATE", StringComparison.OrdinalIgnoreCase))
                            {
                                _currentSimRate = val;
                            }
                        }
                    }
                }
                return;
            }

            // Respostas de strings da Fase B
            if (reqId >= PhaseBStringBaseReqId)
            {
                int strIndex = reqId - PhaseBStringBaseReqId;
                if (_phaseBReqIdToStringSlot.TryGetValue(strIndex, out var strSlot))
                {
                    if (data.dwData != null && data.dwData.Length > 0 && data.dwData[0] is ProbeStringData strData)
                    {
                        string val = strData.Value ?? string.Empty;
                        _statsAccumulator.RecordString(strSlot.Name, val);

                        bool changed = !_lastStringValues.TryGetValue(strSlot.Name, out var lastVal) || lastVal != val;
                        if (changed)
                        {
                            _lastStringValues[strSlot.Name] = val;
                            WriteStringChange(strSlot, val, DateTime.UtcNow);
                        }
                    }
                }
                return;
            }
        }

        #endregion

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                sb.Append(invalid.Contains(c) || c == ' ' ? '_' : c);
            }
            string result = sb.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(result) ? "MSFS" : result;
        }

        public void Dispose()
        {
            _running = false;
            _cts.Cancel();

            if (_simConnect != null)
            {
                try
                {
                    _simConnect.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SimVarProbe] Falha ao fechar o SimConnect: {ex.Message}");
                }
                _simConnect = null;
            }

            _waitHandle.Dispose();
            _cts.Dispose();
        }
    }
}
