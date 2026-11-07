using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using FlightCareer.Domain.Entities;

namespace FlightCareer.UI.Services
{
    public enum DATA_REQUESTS { RequestTelemetry }
    public enum DEFINITIONS { AircraftData }
    public enum EVENTS { FailureEvent }

    public class SimConnectService
    {
        private SimConnect? _simConnect;
        private const int WM_USER_SIMCONNECT = 0x0402;

        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;
        public event EventHandler<SimTelemetry>? OnDataReceived;

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

        public void SetDebugData(SimTelemetry data)
        {
            if (!DebugMode) return;
            CurrentData = data;
            OnDataReceived?.Invoke(this, data);
        }

        private IntPtr _handle;
        private HwndSource? _hwndSource;
        private System.Timers.Timer? _reconnectTimer;

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _handle = helper.Handle;

            _hwndSource = HwndSource.FromHwnd(_handle);
            _hwndSource.AddHook(WndProc);

            _reconnectTimer = new System.Timers.Timer(5000);
            _reconnectTimer.Elapsed += (s, e) => {
                if (!IsConnected) Connect();
            };
            _reconnectTimer.Start();
        }

        public void Connect()
        {
            if (_simConnect != null) return;

            try
            {
                _simConnect = new SimConnect("Flight Career V6", _handle, WM_USER_SIMCONNECT, null, 0);

                _simConnect.AddToDataDefinition(DEFINITIONS.AircraftData, "TITLE", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                AddToDef("PLANE LATITUDE", "degrees");
                AddToDef("PLANE LONGITUDE", "degrees");
                AddToDef("PLANE ALTITUDE", "feet");
                AddToDef("GROUND VELOCITY", "knots");
                AddToDef("SIM ON GROUND", "Bool");
                AddToDef("GENERAL ENG COMBUSTION:1", "Bool");
                AddToDef("BRAKE PARKING POSITION", "Bool");
                AddToDef("ENGINE TYPE", "Enum");
                AddToDef("FUEL TOTAL QUANTITY", "Gallons");
                AddToDef("FUEL TOTAL CAPACITY", "Gallons");
                AddToDef("FUEL TOTAL QUANTITY WEIGHT", "Pounds");
                AddToDef("MAX GROSS WEIGHT", "Pounds");
                AddToDef("TOTAL WEIGHT", "Pounds");
                AddToDef("EMPTY WEIGHT", "Pounds");
                AddToDef("ZERO FUEL WEIGHT", "Pounds");
                AddToDef("FUEL WEIGHT PER GALLON", "Pounds per gallon");

                AddToDef("GENERAL ENG RPM:1", "rpm");
                AddToDef("GENERAL ENG MAX RPM:1", "rpm");
                AddToDef("VERTICAL SPEED", "Feet per second");
                AddToDef("G FORCE", "G force");
                AddToDef("AIRSPEED INDICATED", "knots");
                AddToDef("FLAPS HANDLE PERCENT", "Percent");
                
                AddToDef("VELOCITY BODY Y", "Feet per second");
                AddToDef("BRAKE LEFT POSITION", "Position");
                AddToDef("BRAKE RIGHT POSITION", "Position");

                AddToDef("OVERSPEED WARNING", "Bool");
                AddToDef("CRASH SEQUENCE", "Enum");

                AddToDef("RECIP ENG CYLINDER HEAD TEMPERATURE:1", "Celsius");
                AddToDef("ENG OIL TEMPERATURE:1", "Celsius");
                AddToDef("ENG OIL PRESSURE:1", "psi");
                AddToDef("TURB ENG ITT:1", "Celsius");
                AddToDef("TURB ENG N1:1", "Percent");

                AddToDef("PLANE BANK DEGREES", "degrees");
                AddToDef("PLANE PITCH DEGREES", "degrees");
                AddToDef("PLANE HEADING DEGREES MAGNETIC", "degrees");

                _simConnect.RegisterDataDefineStruct<SimTelemetry>(DEFINITIONS.AircraftData);

                _simConnect.OnRecvOpen += (s, e) => {
                    _reconnectTimer?.Stop();
                    OnConnected?.Invoke(this, EventArgs.Empty);
                };

                _simConnect.OnRecvQuit += (s, e) => {
                    Disconnect();
                    _reconnectTimer?.Start();
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
