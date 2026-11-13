using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using FlightCareer.Domain.Entities;
using FlightCareer.Application.Interfaces;

namespace FlightCareer.UI.Services
{
    public enum DATA_REQUESTS { RequestTelemetry, RequestFailureState }
    public enum DEFINITIONS { AircraftData, FailureState, WriteLeftMagneto, WriteRightMagneto, WriteFlapsHandleIndex }
    public enum EVENTS { FailureEvent, CrashedEvent, AircraftLoaded, FlightLoaded }
    public enum GROUPS { MaskedPilotInputs = 1 }

    /// <summary>
    /// Leitura rápida (~5-10 Hz) usada para vigilância das panes persistentes e detecção de panes nativas.
    /// Todas as variáveis foram validadas no MSFS 2024 (C152) via SimConnect.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SimFailureState
    {
        public double LeftMagneto;          // RECIP ENG LEFT MAGNETO:1 (gravável)
        public double RightMagneto;         // RECIP ENG RIGHT MAGNETO:1 (gravável)
        public double FlapsHandleIndex;     // FLAPS HANDLE INDEX (gravável)
        public double AlternatorSwitch;     // GENERAL ENG MASTER ALTERNATOR:1 (somente leitura; corrigir via ALTERNATOR_OFF)
        public double EngineFailed;         // ENG FAILED:1
        public double EngineOnFire;         // ENG ON FIRE:1
        public double EngineDamagePercent;  // GENERAL ENG DAMAGE PERCENT:1
        public double FlapDamage;           // FLAP DAMAGE BY SPEED
        public double GearDamage;           // GEAR DAMAGE BY SPEED
        public double BrakeLeftRemaining;   // BRAKE LEFT WEIGHT REMAINING (%)
        public double BrakeRightRemaining;  // BRAKE RIGHT WEIGHT REMAINING (%)
        public double CrashFlag;            // CRASH FLAG
        public double SuctionPressure;      // SUCTION PRESSURE (inHg)
        public double EngineCombustion;     // GENERAL ENG COMBUSTION:1
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SingleDouble { public double Value; }

    public class SimConnectService : ISimConnectService
    {
        private SimConnect? _simConnect;
        private const int WM_USER_SIMCONNECT = 0x0402;

        // Cada evento do simulador precisa do seu próprio ID de cliente: o SimConnect recusa remapear um ID já mapeado.
        private const uint FirstDynamicEventId = 1000;
        private readonly Dictionary<string, uint> _eventIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, string> _eventNames = new();
        private uint _nextEventId = FirstDynamicEventId;
        private HashSet<string> _maskedEvents = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;
        public event EventHandler<SimTelemetry>? OnDataReceived;
        public event EventHandler? OnCrashEvent;

        /// <summary>Estado rápido de panes (vigilância + detecção nativa).</summary>
        public event Action<SimFailureState>? OnFailureStateReceived;
        /// <summary>Comando do piloto interceptado (mascarado) antes de chegar à aeronave.</summary>
        public event Action<string, uint>? OnMaskedEvent;
        /// <summary>O simulador (re)carregou aeronave ou voo: as panes nativas foram zeradas pelo sim.</summary>
        public event Action? OnAircraftReloaded;

        public SimFailureState? CurrentFailureState { get; private set; }

        public bool IsCrashed { get; set; } = false;

        public void ResetCrashState()
        {
            IsCrashed = false;
        }

        private bool _debugMode;
        public bool DebugMode 
        { 
            get => _debugMode;
            set 
            {
                _debugMode = value;
                if (_debugMode) OnConnected?.Invoke(this, EventArgs.Empty);
                else if (!IsConnected) OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsConnected => _debugMode || _simConnect != null;
        public SimTelemetry CurrentData { get; private set; }
        public double EngineOilTempF => (CurrentData.EngineOilTemp * 1.8) + 32.0;

        public void SetDebugData(SimTelemetry data)
        {
            if (!DebugMode) return;
            CurrentData = data;
            OnDataReceived?.Invoke(this, data);
        }

        private IntPtr _handle;
        private HwndSource? _hwndSource;
        private System.Timers.Timer? _reconnectTimer;

        public void Initialize(IntPtr windowHandle)
        {
            _handle = windowHandle;

            _hwndSource = HwndSource.FromHwnd(_handle);
            _hwndSource.AddHook(WndProc);

            _reconnectTimer = new System.Timers.Timer(5000);
            _reconnectTimer.Elapsed += (s, e) => {
                if (!IsConnected) Connect();
            };
            _reconnectTimer.Start();
        }

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            Initialize(helper.Handle);
        }

        public void Connect()
        {
            if (_simConnect != null) return;

            try
            {
                _simConnect = new SimConnect("Flight Career V6", _handle, WM_USER_SIMCONNECT, null, 0);

                // 1. Identificação da Aeronave (256 bytes)
                _simConnect.AddToDataDefinition(DEFINITIONS.AircraftData, "TITLE", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                // 2. Posição e Movimento (6 doubles)
                AddToDef("PLANE LATITUDE", "degrees");
                AddToDef("PLANE LONGITUDE", "degrees");
                AddToDef("PLANE ALTITUDE", "feet");
                AddToDef("GROUND VELOCITY", "knots");
                AddToDef("AIRSPEED INDICATED", "knots");
                AddToDef("VERTICAL SPEED", "ft/min");

                // 3. Estados Gerais (4 doubles)
                AddToDef("SIM ON GROUND", "Bool");
                AddToDef("GENERAL ENG COMBUSTION:1", "Bool");
                AddToDef("BRAKE PARKING POSITION", "Bool");
                AddToDef("ENGINE TYPE", "number");

                // 4. Combustível e Pesos (8 doubles)
                AddToDef("FUEL TOTAL QUANTITY", "Gallons");
                AddToDef("FUEL TOTAL CAPACITY", "Gallons");
                AddToDef("FUEL TOTAL QUANTITY WEIGHT", "Pounds");
                AddToDef("MAX GROSS WEIGHT", "Pounds");
                AddToDef("TOTAL WEIGHT", "Pounds");
                AddToDef("EMPTY WEIGHT", "Pounds");
                AddToDef("UNUSABLE FUEL TOTAL QUANTITY", "Gallons");
                AddToDef("FUEL WEIGHT PER GALLON", "Pounds");

                // 5. Atitude e Força G (4 doubles)
                AddToDef("G FORCE", "GForce");
                AddToDef("PLANE PITCH DEGREES", "degrees");
                AddToDef("PLANE BANK DEGREES", "degrees");
                AddToDef("PLANE HEADING DEGREES MAGNETIC", "degrees");

                // 6. Controles e Trem/Freios (4 doubles)
                AddToDef("FLAPS HANDLE PERCENT", "Percent Over 100");
                AddToDef("VELOCITY BODY Y", "Feet per second");
                AddToDef("BRAKE LEFT POSITION", "Position");
                AddToDef("BRAKE RIGHT POSITION", "Position");

                // 7. Alertas (1 double)
                AddToDef("OVERSPEED WARNING", "Bool");

                // 8. Motor (7 doubles)
                AddToDef("GENERAL ENG RPM:1", "rpm");
                AddToDef("RECIP ENG CYLINDER HEAD TEMPERATURE:1", "Celsius");
                AddToDef("GENERAL ENG OIL TEMPERATURE:1", "Celsius");
                AddToDef("GENERAL ENG OIL PRESSURE:1", "psi");
                AddToDef("TURB ENG ITT:1", "Celsius");
                AddToDef("TURB ENG N1:1", "Percent");
                AddToDef("ENG FAILED:1", "Bool");

                // 9. Condições Ambientais e Atmosféricas (5 doubles)
                AddToDef("SURFACE CONDITION", "Number");
                AddToDef("AMBIENT TEMPERATURE", "Celsius");
                AddToDef("AMBIENT PRECIP STATE", "Mask");
                AddToDef("AMBIENT WIND VELOCITY", "Knots");
                AddToDef("AMBIENT WIND DIRECTION", "Degrees");

                // 10. Dinâmica Lateral e Comandos do Piloto (4 doubles)
                AddToDef("VELOCITY BODY X", "Feet per second");
                AddToDef("GENERAL ENG THROTTLE LEVER POSITION:1", "Percent");
                AddToDef("GENERAL ENG MIXTURE LEVER POSITION:1", "Percent");
                AddToDef("ENG ANTI ICE:1", "Bool");

                // 11. Gelo e Pressurização (3 doubles)
                AddToDef("STRUCTURAL ICE PCT", "Percent");
                AddToDef("PITOT ICE PCT", "Percent");
                AddToDef("PRESSURIZATION CABIN ALTITUDE", "Feet");

                // 12. Simulação e Tempo (1 double)
                AddToDef("SIMULATION RATE", "number");

                _simConnect.RegisterDataDefineStruct<SimTelemetry>(DEFINITIONS.AircraftData);

                // Estado rápido de panes (vigilância contínua + panes nativas)
                AddFailureDef("RECIP ENG LEFT MAGNETO:1", "Bool");
                AddFailureDef("RECIP ENG RIGHT MAGNETO:1", "Bool");
                AddFailureDef("FLAPS HANDLE INDEX", "Number");
                AddFailureDef("GENERAL ENG MASTER ALTERNATOR:1", "Bool");
                AddFailureDef("ENG FAILED:1", "Bool");
                AddFailureDef("ENG ON FIRE:1", "Bool");
                AddFailureDef("GENERAL ENG DAMAGE PERCENT:1", "Percent");
                AddFailureDef("FLAP DAMAGE BY SPEED", "Bool");
                AddFailureDef("GEAR DAMAGE BY SPEED", "Bool");
                AddFailureDef("BRAKE LEFT WEIGHT REMAINING", "Percent");
                AddFailureDef("BRAKE RIGHT WEIGHT REMAINING", "Percent");
                AddFailureDef("CRASH FLAG", "Enum");
                AddFailureDef("SUCTION PRESSURE", "inHg");
                AddFailureDef("GENERAL ENG COMBUSTION:1", "Bool");
                _simConnect.RegisterDataDefineStruct<SimFailureState>(DEFINITIONS.FailureState);

                // Definições de escrita (correção imediata pela vigilância)
                AddWriteDef(DEFINITIONS.WriteLeftMagneto, "RECIP ENG LEFT MAGNETO:1", "Bool");
                AddWriteDef(DEFINITIONS.WriteRightMagneto, "RECIP ENG RIGHT MAGNETO:1", "Bool");
                AddWriteDef(DEFINITIONS.WriteFlapsHandleIndex, "FLAPS HANDLE INDEX", "Number");

                // Inscrição no evento do sistema de Crash
                _simConnect.SubscribeToSystemEvent(EVENTS.CrashedEvent, "Crashed");
                // O simulador zera as falhas ao recarregar a aeronave: avisar para reaplicar
                _simConnect.SubscribeToSystemEvent(EVENTS.AircraftLoaded, "AircraftLoaded");
                _simConnect.SubscribeToSystemEvent(EVENTS.FlightLoaded, "FlightLoaded");

                _simConnect.OnRecvOpen += (s, e) => {
                    IsCrashed = false;
                    _reconnectTimer?.Stop();
                    ApplyMaskedEvents();
                    OnConnected?.Invoke(this, EventArgs.Empty);
                };

                _simConnect.OnRecvQuit += (s, e) => {
                    Disconnect();
                    _reconnectTimer?.Start();
                };

                _simConnect.OnRecvEvent += (s, e) => {
                    if (e.uEventID == (uint)EVENTS.CrashedEvent)
                    {
                        IsCrashed = true;
                        OnCrashEvent?.Invoke(this, EventArgs.Empty);
                    }
                    else if (e.uEventID == (uint)EVENTS.AircraftLoaded || e.uEventID == (uint)EVENTS.FlightLoaded)
                    {
                        OnAircraftReloaded?.Invoke();
                    }
                    else if (e.uEventID >= FirstDynamicEventId && _eventNames.TryGetValue(e.uEventID, out var name))
                    {
                        // Só chegam aqui eventos do grupo mascarado: o comando já foi bloqueado pelo SimConnect
                        OnMaskedEvent?.Invoke(name, e.dwData);
                    }
                };

                _simConnect.OnRecvException += (s, e) => {
                    Debug.WriteLine($"[SimConnect EXCEPTION] ID: {(SIMCONNECT_EXCEPTION)e.dwException}, SendID: {e.dwSendID}, Index: {e.dwIndex}");
                };

                _simConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;

                _simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestTelemetry, DEFINITIONS.AircraftData,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 4, 0);

                // A cada 6 frames (~5-10 Hz): rápido o bastante para desfazer um comando do piloto quase na hora
                _simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestFailureState, DEFINITIONS.FailureState,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 6, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SimConnect Error: {ex.Message}");
                _simConnect = null;
            }
        }

        private void AddToDef(string datum, string unit)
        {
            _simConnect?.AddToDataDefinition(DEFINITIONS.AircraftData, datum, unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        }

        private void AddFailureDef(string datum, string unit)
        {
            _simConnect?.AddToDataDefinition(DEFINITIONS.FailureState, datum, unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        }

        private void AddWriteDef(DEFINITIONS def, string datum, string unit)
        {
            if (_simConnect == null) return;
            _simConnect.AddToDataDefinition(def, datum, unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            _simConnect.RegisterDataDefineStruct<SingleDouble>(def);
        }

        public void Disconnect()
        {
            if (_simConnect != null)
            {
                _simConnect.Dispose();
                _simConnect = null;
                _eventIds.Clear();
                _eventNames.Clear();
                _nextEventId = FirstDynamicEventId;
                CurrentFailureState = null;
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private uint GetOrMapEvent(string simEventName)
        {
            if (_eventIds.TryGetValue(simEventName, out var id)) return id;
            id = _nextEventId++;
            _simConnect!.MapClientEventToSimEvent((EVENTS)id, simEventName);
            _eventIds[simEventName] = id;
            _eventNames[id] = simEventName;
            return id;
        }

        /// <summary>Envia um evento do simulador. Cada nome recebe um ID próprio, mapeado uma única vez por conexão.</summary>
        public void TransmitEvent(string simEventName, uint data = 0)
        {
            if (_simConnect == null || string.IsNullOrWhiteSpace(simEventName)) return;
            try
            {
                var id = GetOrMapEvent(simEventName);
                _simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (EVENTS)id, data,
                    (GROUPS)SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SimConnect] TransmitEvent {simEventName} falhou: {ex.Message}");
            }
        }

        public void SetFailure(string eventName) => TransmitEvent(eventName, 1);

        /// <summary>
        /// Define os comandos do piloto que devem ser bloqueados (grupo mascarável de prioridade máxima).
        /// Nunca incluir aqui um evento que o próprio addon transmite, ou ele também seria bloqueado.
        /// </summary>
        public void SetMaskedEvents(IEnumerable<string> simEventNames)
        {
            var desired = new HashSet<string>(simEventNames, StringComparer.OrdinalIgnoreCase);
            if (desired.SetEquals(_maskedEvents)) return;
            _maskedEvents = desired;
            ApplyMaskedEvents();
        }

        private void ApplyMaskedEvents()
        {
            if (_simConnect == null) return;
            try
            {
                _simConnect.ClearNotificationGroup(GROUPS.MaskedPilotInputs);
                if (_maskedEvents.Count == 0) return;
                foreach (var name in _maskedEvents)
                {
                    var id = GetOrMapEvent(name);
                    _simConnect.AddClientEventToNotificationGroup(GROUPS.MaskedPilotInputs, (EVENTS)id, true);
                }
                _simConnect.SetNotificationGroupPriority(GROUPS.MaskedPilotInputs, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST_MASKABLE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SimConnect] Falha ao aplicar mascaramento: {ex.Message}");
            }
        }

        public void WriteMagneto(bool left, bool on) =>
            WriteSingle(left ? DEFINITIONS.WriteLeftMagneto : DEFINITIONS.WriteRightMagneto, on ? 1.0 : 0.0);

        public void WriteFlapsHandleIndex(double index) => WriteSingle(DEFINITIONS.WriteFlapsHandleIndex, index);

        private void WriteSingle(DEFINITIONS def, double value)
        {
            if (_simConnect == null) return;
            try
            {
                _simConnect.SetDataOnSimObject(def, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new SingleDouble { Value = value });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SimConnect] Escrita {def} falhou: {ex.Message}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_USER_SIMCONNECT && _simConnect != null)
            {
                try
                {
                    _simConnect.ReceiveMessage();
                    handled = true;
                }
                catch { }
            }
            return IntPtr.Zero;
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwData == null || data.dwData.Length == 0) return;

            if (data.dwData[0] is SimTelemetry telemetry)
            {
                CurrentData = telemetry;
                OnDataReceived?.Invoke(this, telemetry);
            }
            else if (data.dwData[0] is SimFailureState failureState)
            {
                CurrentFailureState = failureState;
                OnFailureStateReceived?.Invoke(failureState);
            }
        }
    }
}
