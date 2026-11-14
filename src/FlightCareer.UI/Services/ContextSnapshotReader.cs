using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Leitor sob demanda dos dados estáticos e de contexto da aeronave via SimConnect.
    /// Carrega uma vez o context_registry.json e extrai variáveis numéricas e de texto.
    /// </summary>
    public sealed class ContextSnapshotReader : IDisposable
    {
        public const int NumericDefId = 2000;
        public const int NumericRequestId = 2000;
        public const int StringDefId = 2001;
        public const int StringRequestId = 2001;

        private readonly SimConnectService _simConnectService;
        private readonly SensorFrameReader _frameReader;
        private readonly SensorRegistry _contextRegistry;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private SimConnect? _currentSimConnect;
        private bool _disposed;

        public ContextSnapshotReader(SimConnectService simConnectService, SensorFrameReader frameReader)
        {
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _frameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));

            try
            {
                _contextRegistry = SensorRegistry.LoadDefault("context_registry.json");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextSnapshotReader] Falha ao carregar context_registry.json: {ex.Message}");
                _contextRegistry = new SensorRegistry();
            }

            _simConnectService.OnSimConnectReady += HandleSimConnectReady;
            _simConnectService.OnDisconnected += HandleDisconnected;

            if (_simConnectService.Raw != null)
            {
                HandleSimConnectReady(_simConnectService.Raw);
            }
        }

        private void HandleSimConnectReady(SimConnect sc)
        {
            _currentSimConnect = sc;
        }

        private void HandleDisconnected(object? sender, EventArgs e)
        {
            _currentSimConnect = null;
        }

        /// <summary>
        /// Solicita a leitura das variáveis de contexto do simulador.
        /// Retorna null caso o simulador não esteja conectado ou não responda em 5 segundos.
        /// </summary>
        public async Task<ContextSnapshot?> ReadSnapshotAsync(int timeoutMs = 5000)
        {
            if (_disposed) return null;

            await _semaphore.WaitAsync();
            try
            {
                var sc = _currentSimConnect ?? _simConnectService.Raw;
                if (sc == null)
                {
                    Debug.WriteLine("[ContextSnapshotReader] SimConnect não está conectado. Abortando snapshot de contexto.");
                    return null;
                }

                int engineCount = Math.Max(1, _frameReader.CurrentEngineCount);

                // Prepara slots numéricos
                var numericSlots = new List<SensorSlot>();
                foreach (var s in _contextRegistry.Sensors)
                {
                    if (string.Equals(s.Unit, "string", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (s.PerEngine)
                    {
                        for (int eng = 1; eng <= engineCount; eng++)
                        {
                            numericSlots.Add(new SensorSlot($"{s.Key}:{eng}", $"{s.SimVar}:{eng}", s.Unit));
                        }
                    }
                    else
                    {
                        numericSlots.Add(new SensorSlot(s.Key, s.SimVar, s.Unit));
                    }
                }

                var tcs = new TaskCompletionSource<ContextSnapshot?>(TaskCreationOptions.RunContinuationsAsynchronously);

                Dictionary<string, double>? receivedNumbers = null;
                Dictionary<string, string>? receivedStrings = null;
                var lockObj = new object();

                void DataHandler(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
                {
                    try
                    {
                        if (data.dwRequestID == (uint)NumericRequestId && data.dwData != null && data.dwData.Length > 0)
                        {
                            if (data.dwData[0] is SensorBlockData blockData && blockData.Values != null)
                            {
                                int count = Math.Min(numericSlots.Count, blockData.Values.Length);
                                var dict = new Dictionary<string, double>(count, StringComparer.Ordinal);
                                for (int i = 0; i < count; i++)
                                {
                                    dict[numericSlots[i].Key] = blockData.Values[i];
                                }

                                lock (lockObj)
                                {
                                    receivedNumbers = dict;
                                    if (receivedStrings != null)
                                    {
                                        tcs.TrySetResult(new ContextSnapshot(receivedNumbers, receivedStrings, DateTime.UtcNow));
                                    }
                                }
                            }
                        }
                        else if (data.dwRequestID == (uint)StringRequestId && data.dwData != null && data.dwData.Length > 0)
                        {
                            if (data.dwData[0] is ContextStringData stringData)
                            {
                                var dict = new Dictionary<string, string>(3, StringComparer.Ordinal)
                                {
                                    ["ctx-title"] = stringData.Title?.Trim() ?? string.Empty,
                                    ["ctx-atc-model"] = stringData.AtcModel?.Trim() ?? string.Empty,
                                    ["ctx-atc-type"] = stringData.AtcType?.Trim() ?? string.Empty
                                };

                                lock (lockObj)
                                {
                                    receivedStrings = dict;
                                    if (receivedNumbers != null)
                                    {
                                        tcs.TrySetResult(new ContextSnapshot(receivedNumbers, receivedStrings, DateTime.UtcNow));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ContextSnapshotReader] Erro ao processar dados de contexto: {ex.Message}");
                    }
                }

                sc.OnRecvSimobjectData += DataHandler;

                try
                {
                    // Registra e requisita numéricos (ID 2000)
                    sc.ClearDataDefinition((DEFINITIONS)NumericDefId);
                    foreach (var slot in numericSlots)
                    {
                        sc.AddToDataDefinition((DEFINITIONS)NumericDefId, slot.SimVar, slot.Unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    }
                    sc.RegisterDataDefineStruct<SensorBlockData>((DEFINITIONS)NumericDefId);
                    sc.RequestDataOnSimObject((DATA_REQUESTS)NumericRequestId, (DEFINITIONS)NumericDefId,
                        SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

                    // Registra e requisita strings (ID 2001)
                    sc.ClearDataDefinition((DEFINITIONS)StringDefId);
                    sc.AddToDataDefinition((DEFINITIONS)StringDefId, "TITLE", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    sc.AddToDataDefinition((DEFINITIONS)StringDefId, "ATC MODEL", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    sc.AddToDataDefinition((DEFINITIONS)StringDefId, "ATC TYPE", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    sc.RegisterDataDefineStruct<ContextStringData>((DEFINITIONS)StringDefId);
                    sc.RequestDataOnSimObject((DATA_REQUESTS)StringRequestId, (DEFINITIONS)StringDefId,
                        SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

                    // Aguarda resposta ou timeout de 5 segundos
                    using var cts = new CancellationTokenSource(timeoutMs);
                    var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, cts.Token));

                    if (completedTask == tcs.Task)
                    {
                        cts.Cancel();
                        return await tcs.Task;
                    }

                    Debug.WriteLine("[ContextSnapshotReader] Timeout de 5s aguardando snapshot de contexto do simulador.");
                    // Retorna parcial se tiver chegado algo, ou null se não veio nada
                    lock (lockObj)
                    {
                        if (receivedNumbers != null || receivedStrings != null)
                        {
                            return new ContextSnapshot(
                                receivedNumbers ?? new Dictionary<string, double>(),
                                receivedStrings ?? new Dictionary<string, string>(),
                                DateTime.UtcNow);
                        }
                    }

                    return null;
                }
                finally
                {
                    try
                    {
                        sc.OnRecvSimobjectData -= DataHandler;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ContextSnapshotReader] Erro ao desregistrar handler de dados: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextSnapshotReader] Exceção ao ler snapshot de contexto: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _simConnectService.OnSimConnectReady -= HandleSimConnectReady;
            _simConnectService.OnDisconnected -= HandleDisconnected;
            _semaphore.Dispose();
        }
    }
}
