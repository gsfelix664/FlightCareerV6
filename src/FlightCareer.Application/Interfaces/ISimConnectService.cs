using FlightCareer.Domain.Entities;
using System;

namespace FlightCareer.Application.Interfaces
{
    public interface ISimConnectService
    {
        event EventHandler? OnConnected;
        event EventHandler? OnDisconnected;
        event EventHandler<SimTelemetry>? OnDataReceived;
        event EventHandler? OnCrashEvent;
        event Action? OnAircraftReloaded;

        bool IsCrashed { get; set; }
        bool DebugMode { get; set; }
        bool IsConnected { get; }
        SimTelemetry CurrentData { get; }

        void ResetCrashState();
        void SetDebugData(SimTelemetry data);
        void Initialize(IntPtr windowHandle);
        void Connect();
        void Disconnect();
        void SetFailure(string eventName);
        void SendEvent(string eventName, uint data = 0);
        void ClearSimFailures();
        void SetEngineFire(bool isOnFire);
        void SetOilLeakedPercent(double percent);
        void SetAlternatorConnected(int alternatorIndex, bool connected);
        void WriteFlapSurfacePercent(double fraction);
        void SetTurbineLosses(int engineIndex, Domain.Components.TurbineLosses losses);
        Domain.Components.TurbineLosses GetTurbineLosses(int engineIndex);
    }
}
