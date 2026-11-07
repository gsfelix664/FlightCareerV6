using System;
using System.Runtime.InteropServices;

namespace FlightCareer.Domain.Entities
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SimTelemetry
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;

        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double GroundSpeed;

        public double OnGround;      // 1.0 = true
        public double EngineRunning; // 1.0 = true
        public double ParkingBrake;
        public double EngineType;    // 0=Piston, 1=Jet, 5=Turboprop

        public double FuelTotalGallons;
        public double FuelTotalCapacity;
        public double FuelWeightLbs;

        public double MaxGrossWeightLbs;
        public double TotalWeightLbs;
        public double EmptyWeightLbs;
        public double ZeroFuelWeightLbs;
        public double FuelWeightPerGallon;

        public double EngineRPM;
        public double MaxRPM;
        public double VerticalSpeed;
        public double GForce;
        public double Airspeed;
        public double FlapsHandlePercent;

        // Variáveis para Pneus/Pouso
        public double VelocityBodyY;
        public double BrakeLeftPosition;
        public double BrakeRightPosition;

        // Variáveis para Fuselagem e Integridade
        public double OverspeedWarning; 
        public double CrashSequence;    

        // Variáveis Avançadas de Motor (Pistão e Jato)
        public double EngineCHT;        
        public double EngineOilTemp;
        public double EngineOilPressure;
        public double TurboITT;         
        public double EngineN1;

        // Atitude para Qualidade de Voo
        public double PlaneBank;  // Degrees
        public double PlanePitch; // Degrees

        // Proa / Navegação
        public double PlaneHeading; // Degrees Magnetic

        public bool IsOnGround => OnGround > 0.5;
        public bool IsEngineRunning => EngineRunning > 0.5;
    }
}
