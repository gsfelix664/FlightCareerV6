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
        void SetEngineFire(bool isOnFire);
        void SetOilLeakedPercent(double percent);
    }
}
