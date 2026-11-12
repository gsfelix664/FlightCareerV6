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
    public enum DATA_REQUESTS { RequestTelemetry }
    public enum DEFINITIONS { AircraftData }
    public enum EVENTS { FailureEvent, CrashedEvent }

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

                // 8. Motor (8 doubles)
                AddToDef("GENERAL ENG RPM:1", "rpm");
                AddToDef("RECIP ENG CYLINDER HEAD TEMPERATURE:1", "Celsius");
                AddToDef("GENERAL ENG OIL TEMPERATURE:1", "Celsius");
                AddToDef("GENERAL ENG OIL PRESSURE:1", "psi");
                AddToDef("TURB ENG ITT:1", "Celsius");
                AddToDef("TURB ENG MAX TORQUE PERCENT:1", "Percent");
                AddToDef("TURB ENG N1:1", "Percent");
                AddToDef("ENG FAILED:1", "Bool");

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

                _simConnect.RegisterDataDefineStruct<SimTelemetry>(DEFINITIONS.AircraftData);

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
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetFailure(string eventName)
        {
            if (_simConnect == null) return;
            _simConnect.MapClientEventToSimEvent(EVENTS.FailureEvent, eventName);
            
            _simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.FailureEvent, 1, 
                (SIMCONNECT_PERIOD)0, (SIMCONNECT_EVENT_FLAG)0);
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
