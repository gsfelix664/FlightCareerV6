using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using FlightCareer.Domain.Entities;
using FlightCareer.Application.Interfaces;

namespace FlightCareer.UI.Services
{
    public enum DATA_REQUESTS { RequestTelemetry }
    public enum DEFINITIONS { AircraftData, EngineFireState, OilLeakState }
    public enum EVENTS { FailureEvent, CrashedEvent }
    public enum GROUPS : uint { Default = 1 }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimDoubleValue
    {
        public double Value;
    }

    public class SimConnectService : ISimConnectService
    {
        private SimConnect? _simConnect;
        private const int WM_USER_SIMCONNECT = 0x0402;

        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;
        public event EventHandler<SimTelemetry>? OnDataReceived;
        public event EventHandler? OnCrashEvent;

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

                // 8. Motor (10 doubles)
                AddToDef("GENERAL ENG RPM:1", "rpm");
                AddToDef("RECIP ENG CYLINDER HEAD TEMPERATURE:1", "Celsius");
                AddToDef("GENERAL ENG OIL TEMPERATURE:1", "Celsius");
                AddToDef("GENERAL ENG OIL PRESSURE:1", "psi");
                AddToDef("TURB ENG ITT:1", "Celsius");
                AddToDef("TURB ENG MAX TORQUE PERCENT:1", "Percent");
                AddToDef("TURB ENG N1:1", "Percent");
                AddToDef("ENG FAILED:1", "Bool");
                AddToDef("ENG ON FIRE:1", "Bool");
                AddToDef("GENERAL ENG OIL LEAKED PERCENT:1", "Percent");

                // 9. Condições Ambientais e Atmosféricas (5 doubles)
                AddToDef("SURFACE CONDITION", "Number");
                AddToDef("AMBIENT TEMPERATURE", "Celsius");
                AddToDef("AMBIENT PRECIP STATE", "Mask");
                AddToDef("AMBIENT WIND VELOCITY", "Knots");
                AddToDef("AMBIENT WIND DIRECTION", "Degrees");

                // 10. Dinâmica Lateral e Comandos do Piloto (7 doubles)
                AddToDef("VELOCITY BODY X", "Feet per second");
                AddToDef("GENERAL ENG THROTTLE LEVER POSITION:1", "Percent");
                AddToDef("GENERAL ENG MIXTURE LEVER POSITION:1", "Percent");
                AddToDef("ENG ANTI ICE:1", "Bool");
                AddToDef("GENERAL ENG FUEL PUMP SWITCH:1", "Bool");
                AddToDef("GENERAL ENG STARTER:1", "Bool");
                AddToDef("TURB ENG CONDITION LEVER POSITION:1", "Percent");

                // 11. Gelo e Pressurização (5 doubles)
                AddToDef("STRUCTURAL ICE PCT", "Percent");
                AddToDef("PITOT ICE PCT", "Percent");
                AddToDef("CARB HEAT AVAILABLE", "Bool");
                AddToDef("RECIP CARBURETOR TEMPERATURE:1", "Celsius");
                AddToDef("PRESSURIZATION CABIN ALTITUDE", "Feet");

                // 12. Simulação e Tempo (1 double)
                AddToDef("SIMULATION RATE", "number");

                // 13. Estado de Falhas e Chaves para Enforcement (6 doubles)
                AddToDef("PARTIAL PANEL VACUUM", "Enum");
                AddToDef("PARTIAL PANEL PITOT", "Enum");
                AddToDef("PARTIAL PANEL STATIC", "Enum");
                AddToDef("RECIP ENG LEFT MAGNETO:1", "Bool");
                AddToDef("RECIP ENG RIGHT MAGNETO:1", "Bool");
                AddToDef("GENERAL ENG MASTER ALTERNATOR:1", "Bool");

                _simConnect.RegisterDataDefineStruct<SimTelemetry>(DEFINITIONS.AircraftData);

                // Definições de escrita (SetDataOnSimObject) para Fogo Real e Vazamento Progressivo de Óleo
                _simConnect.AddToDataDefinition(DEFINITIONS.EngineFireState, "ENG ON FIRE:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simConnect.RegisterDataDefineStruct<SimDoubleValue>(DEFINITIONS.EngineFireState);

                _simConnect.AddToDataDefinition(DEFINITIONS.OilLeakState, "GENERAL ENG OIL LEAKED PERCENT:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simConnect.RegisterDataDefineStruct<SimDoubleValue>(DEFINITIONS.OilLeakState);

                // Inscrição no evento do sistema de Crash
                _simConnect.SubscribeToSystemEvent(EVENTS.CrashedEvent, "Crashed");

                _simConnect.OnRecvOpen += (s, e) => {
                    IsCrashed = false;
                    _reconnectTimer?.Stop();
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
                };

                _simConnect.OnRecvException += (s, e) => {
                    Debug.WriteLine($"[SimConnect EXCEPTION] ID: {(SIMCONNECT_EXCEPTION)e.dwException}, SendID: {e.dwSendID}, Index: {e.dwIndex}");
                };

                _simConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;

                _simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestTelemetry, DEFINITIONS.AircraftData,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 4, 0);
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

        public void Disconnect()
        {
            if (_simConnect != null)
            {
                _simConnect.Dispose();
                _simConnect = null;
                _mappedEvents.Clear();
                _nextEventId = FirstDynamicEventId;
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        // O SimConnect não permite remapear um mesmo ID de evento cliente: cada evento
        // do simulador recebe seu próprio ID, mapeado uma única vez por conexão.
        private const uint FirstDynamicEventId = 1000;
        private readonly Dictionary<string, uint> _mappedEvents = new(StringComparer.OrdinalIgnoreCase);
        private uint _nextEventId = FirstDynamicEventId;

        // Eventos de falha do SimConnect que funcionam como toggle (enviar duas vezes = reparar).
        private static readonly string[] ToggleFailureEvents =
        {
            "TOGGLE_ENGINE1_FAILURE",
            "TOGGLE_VACUUM_FAILURE",
            "TOGGLE_PITOT_BLOCKAGE",
            "TOGGLE_STATIC_PORT_BLOCKAGE"
        };

        public void SendEvent(string eventName, uint data = 0)
        {
            if (_simConnect == null || string.IsNullOrWhiteSpace(eventName)) return;
            try
            {
                if (!_mappedEvents.TryGetValue(eventName, out uint id))
                {
                    id = _nextEventId++;
                    _simConnect.MapClientEventToSimEvent((EVENTS)id, eventName);
                    _mappedEvents[eventName] = id;
                }

                _simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (EVENTS)id, data,
                    (GROUPS)SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SimConnect SendEvent({eventName}) Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Estado atual de uma falha do tipo toggle no simulador.
        /// null = o simulador não expõe uma SimVar confiável para esse evento.
        /// </summary>
        private bool? IsToggleFailureActive(string eventName) => eventName.ToUpperInvariant() switch
        {
            "TOGGLE_ENGINE1_FAILURE" => CurrentData.IsEngineFailed,
            "TOGGLE_VACUUM_FAILURE" => CurrentData.PartialPanelVacuum > 0.5,
            "TOGGLE_PITOT_BLOCKAGE" => CurrentData.PartialPanelPitot > 0.5,
            "TOGGLE_STATIC_PORT_BLOCKAGE" => CurrentData.PartialPanelStatic > 0.5,
            _ => null
        };

        public void SetFailure(string eventName)
        {
            // Evita que um segundo TOGGLE repare uma falha que o simulador já reporta como ativa
            if (IsToggleFailureActive(eventName) == true) return;
            SendEvent(eventName);
        }

        /// <summary>
        /// Desfaz no simulador as falhas toggle que ainda estão ativas (início de um novo voo).
        /// Só re-envia o toggle quando a SimVar confirma a falha, para não criar uma falha nova.
        /// </summary>
        public void ClearSimFailures()
        {
            if (_simConnect == null) return;
            foreach (var eventName in ToggleFailureEvents)
            {
                if (IsToggleFailureActive(eventName) == true)
                {
                    SendEvent(eventName);
                }
            }
        }

        public void SetEngineFire(bool isOnFire)
        {
            if (_simConnect == null) return;
            try
            {
                var val = new SimDoubleValue { Value = isOnFire ? 1.0 : 0.0 };
                _simConnect.SetDataOnSimObject(DEFINITIONS.EngineFireState, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, val);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SimConnect SetEngineFire Error: {ex.Message}");
            }
        }

        public void SetOilLeakedPercent(double percent)
        {
            if (_simConnect == null) return;
            try
            {
                var val = new SimDoubleValue { Value = Math.Clamp(percent, 0.0, 100.0) };
                _simConnect.SetDataOnSimObject(DEFINITIONS.OilLeakState, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, val);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SimConnect SetOilLeakedPercent Error: {ex.Message}");
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
            if (data.dwData != null && data.dwData.Length > 0 && data.dwData[0] is SimTelemetry telemetry)
            {
                CurrentData = telemetry;
                OnDataReceived?.Invoke(this, telemetry);
            }
        }
    }
}
