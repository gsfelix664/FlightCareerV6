using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.FlightSimulator.SimConnect;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Leitor genérico contínuo de sensores do simulador MSFS.
    /// Particiona leituras em blocos de até 256 doubles, respeitando limites do SimConnect,
    /// e emite frames estruturados (SensorFrame) a cada ciclo de 30 frames do simulador.
    /// </summary>
    public sealed class SensorFrameReader : IDisposable
    {
        public const int EngineCountId = 999;
        public const int RequestBlockBase = 1000;
        public const int DefinitionBlockBase = 1000;
        public const uint RequestIntervalFrames = 30;

        private readonly SimConnectService _simConnectService;
        private readonly SensorRegistry _registry;
        private readonly object _cycleLock = new();

        private SimConnect? _currentSimConnect;
        private SensorLayout? _layout;
        private int _currentEngineCount = 1;

        private readonly HashSet<string> _rejectedSimVars = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, string> _sendIdToSimVar = new();
        private readonly Dictionary<string, double> _currentCycleValues = new(StringComparer.Ordinal);
        private readonly HashSet<int> _blocksReceivedInCycle = new();
        private bool _disposed;

        /// <summary>
        /// Disparado quando um ciclo completo de blocos de sensores foi recebido e consolidado.
        /// </summary>
        public event Action<SensorFrame>? OnFrame;

        public SensorLayout? CurrentLayout => _layout;
        public int CurrentEngineCount => _currentEngineCount;
        public IReadOnlyCollection<string> RejectedSimVars => _rejectedSimVars;

        public SensorFrameReader(SimConnectService simConnectService, SensorRegistry? registry = null)
        {
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _registry = registry ?? SensorRegistry.LoadDefault("sensor_registry.json");

            _simConnectService.OnSimConnectReady += HandleSimConnectReady;
            _simConnectService.OnAircraftReloaded += HandleAircraftReloaded;
            _simConnectService.OnDisconnected += HandleDisconnected;

            if (_simConnectService.Raw != null)
            {
                HandleSimConnectReady(_simConnectService.Raw);
            }
        }

        private void HandleSimConnectReady(SimConnect sc)
        {
            if (_disposed) return;

            AttachToSimConnect(sc);
            RequestEngineCount(sc);
        }

        private void HandleAircraftReloaded()
        {
            if (_disposed || _currentSimConnect == null) return;

            Debug.WriteLine("[SensorFrameReader] Aeronave recarregada no simulador; solicitando contagem de motores...");
            RequestEngineCount(_currentSimConnect);
        }

        private void HandleDisconnected(object? sender, EventArgs e)
        {
            lock (_cycleLock)
            {
                _currentCycleValues.Clear();
                _blocksReceivedInCycle.Clear();
            }

            DetachFromSimConnect();
        }

        private void AttachToSimConnect(SimConnect sc)
        {
            if (_currentSimConnect == sc) return;

            DetachFromSimConnect();
            _currentSimConnect = sc;

            try
            {
                _currentSimConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;
                _currentSimConnect.OnRecvException += SimConnect_OnRecvException;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SensorFrameReader] Erro ao associar eventos no SimConnect: {ex.Message}");
            }
        }

        private void DetachFromSimConnect()
        {
            if (_currentSimConnect != null)
            {
                try
                {
                    _currentSimConnect.OnRecvSimobjectData -= SimConnect_OnRecvSimobjectData;
                    _currentSimConnect.OnRecvException -= SimConnect_OnRecvException;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SensorFrameReader] Erro ao desassociar eventos do SimConnect: {ex.Message}");
                }

                _currentSimConnect = null;
            }
        }

        private void RequestEngineCount(SimConnect sc)
        {
            try
            {
                sc.ClearDataDefinition((DEFINITIONS)EngineCountId);
                sc.AddToDataDefinition((DEFINITIONS)EngineCountId, "NUMBER OF ENGINES", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                sc.RegisterDataDefineStruct<SimDoubleValue>((DEFINITIONS)EngineCountId);

                sc.RequestDataOnSimObject((DATA_REQUESTS)EngineCountId, (DEFINITIONS)EngineCountId,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SensorFrameReader] Erro ao requisitar contagem de motores: {ex.Message}");
            }
        }

        private void SetupSensorLayout(SimConnect sc, int engineCount)
        {
            lock (_cycleLock)
            {
                _currentCycleValues.Clear();
                _blocksReceivedInCycle.Clear();
            }

            // Cancela requests e limpa definições de blocos anteriores
            if (_layout != null)
            {
                foreach (var b in _layout.Blocks)
                {
                    try
                    {
                        sc.RequestDataOnSimObject((DATA_REQUESTS)(RequestBlockBase + b.Index), (DEFINITIONS)(DefinitionBlockBase + b.Index),
                            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.NEVER, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                        sc.ClearDataDefinition((DEFINITIONS)(DefinitionBlockBase + b.Index));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SensorFrameReader] Erro ao cancelar bloco anterior {b.Index}: {ex.Message}");
                    }
                }
            }

            var layout = SensorLayout.Build(_registry.Sensors, engineCount, blockSize: 256);
            if (_rejectedSimVars.Count > 0)
            {
                layout = layout.Without(_rejectedSimVars);
            }

            _layout = layout;
            _sendIdToSimVar.Clear();

            Debug.WriteLine($"[SensorFrameReader] Configurando {layout.Blocks.Count} blocos de sensores para {engineCount} motor(es)...");

            foreach (var block in layout.Blocks)
            {
                var defId = (DEFINITIONS)(DefinitionBlockBase + block.Index);
                var reqId = (DATA_REQUESTS)(RequestBlockBase + block.Index);

                try
                {
                    sc.ClearDataDefinition(defId);

                    foreach (var slot in block.Slots)
                    {
                        sc.AddToDataDefinition(defId, slot.SimVar, slot.Unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                        uint sendId = sc.GetLastSentPacketID();
                        if (sendId != 0)
                        {
                            _sendIdToSimVar[sendId] = slot.SimVar;
                        }
                    }

                    sc.RegisterDataDefineStruct<SensorBlockData>(defId);

                    sc.RequestDataOnSimObject(reqId, defId,
                        SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, RequestIntervalFrames, 0);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SensorFrameReader] Erro ao registrar bloco {block.Index}: {ex.Message}");
                }
            }
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwData == null || data.dwData.Length == 0) return;

            if (data.dwRequestID == (uint)EngineCountId)
            {
                if (data.dwData[0] is SimDoubleValue val)
                {
                    int engines = (int)Math.Round(val.Value);
                    if (engines < 1) engines = 1;
                    _currentEngineCount = engines;
                    Debug.WriteLine($"[SensorFrameReader] Contagem de motores recebida: {engines}");
                    SetupSensorLayout(sender, engines);
                }
                return;
            }

            if (_layout == null || _layout.Blocks.Count == 0) return;

            int blockIndex = (int)data.dwRequestID - RequestBlockBase;
            if (blockIndex < 0 || blockIndex >= _layout.Blocks.Count) return;

            if (data.dwData[0] is SensorBlockData blockData)
            {
                var mapped = _layout.Map(blockIndex, blockData.Values);

                SensorFrame? frameToEmit = null;

                lock (_cycleLock)
                {
                    foreach (var kvp in mapped)
                    {
                        _currentCycleValues[kvp.Key] = kvp.Value;
                    }

                    _blocksReceivedInCycle.Add(blockIndex);

                    if (_blocksReceivedInCycle.Count == _layout.Blocks.Count)
                    {
                        double simRate = 1.0;
                        if (_currentCycleValues.TryGetValue("ctx-simulation-rate", out var r))
                        {
                            simRate = r;
                        }
                        else if (_currentCycleValues.TryGetValue("simulation-rate", out var r2))
                        {
                            simRate = r2;
                        }

                        var snapshot = new Dictionary<string, double>(_currentCycleValues, StringComparer.Ordinal);
                        frameToEmit = new SensorFrame(snapshot, DateTime.UtcNow, simRate);

                        _currentCycleValues.Clear();
                        _blocksReceivedInCycle.Clear();
                    }
                }

                if (frameToEmit != null)
                {
                    OnFrame?.Invoke(frameToEmit);
                }
            }
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            var exceptionType = (SIMCONNECT_EXCEPTION)data.dwException;

            if (exceptionType == SIMCONNECT_EXCEPTION.NAME_UNRECOGNIZED)
            {
                string? simVar = null;
                if (_sendIdToSimVar.TryGetValue(data.dwSendID, out var identified))
                {
                    simVar = identified;
                }

                Debug.WriteLine($"[SensorFrameReader] SIMCONNECT_EXCEPTION.NAME_UNRECOGNIZED interceptada. SendID={data.dwSendID}, Index={data.dwIndex}, SimVar={simVar ?? "(não mapeada)"}");

                if (simVar != null)
                {
                    if (_rejectedSimVars.Add(simVar))
                    {
                        Debug.WriteLine($"[SensorFrameReader] SimVar '{simVar}' adicionada à lista de rejeitadas. Reconfigurando layout sem ela...");
                        SetupSensorLayout(sender, _currentEngineCount);
                    }
                }
            }
            else
            {
                Debug.WriteLine($"[SensorFrameReader EXCEPTION] Tipo={exceptionType}, SendID={data.dwSendID}, Index={data.dwIndex}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _simConnectService.OnSimConnectReady -= HandleSimConnectReady;
            _simConnectService.OnAircraftReloaded -= HandleAircraftReloaded;
            _simConnectService.OnDisconnected -= HandleDisconnected;

            if (_currentSimConnect != null && _layout != null)
            {
                foreach (var b in _layout.Blocks)
                {
                    try
                    {
                        _currentSimConnect.RequestDataOnSimObject((DATA_REQUESTS)(RequestBlockBase + b.Index), (DEFINITIONS)(DefinitionBlockBase + b.Index),
                            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.NEVER, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                        _currentSimConnect.ClearDataDefinition((DEFINITIONS)(DefinitionBlockBase + b.Index));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SensorFrameReader Dispose] Erro ao limpar bloco {b.Index}: {ex.Message}");
                    }
                }
            }

            DetachFromSimConnect();
        }
    }
}
