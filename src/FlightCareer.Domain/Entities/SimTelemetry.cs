using System;
using System.Runtime.InteropServices;

namespace FlightCareer.Domain.Entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimTelemetry
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;

        // Posição e Movimento (6 doubles)
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double GroundSpeed;
        public double Airspeed;
        public double VerticalSpeed; // ft/min

        // Estados Gerais (4 doubles)
        public double OnGround;      // 1.0 = true
        public double EngineRunning; // 1.0 = true
        public double ParkingBrake;  // 1.0 = true
        public double EngineType;    // 0=Piston, 1=Jet, 5=Turboprop

        // Combustível e Pesos (6 doubles)
        public double FuelTotalGallons;
        public double FuelTotalCapacity;
        public double FuelWeightLbs;
        public double MaxGrossWeightLbs;
        public double TotalWeightLbs;
        public double EmptyWeightLbs;

        // Atitude e Força G (4 doubles)
        public double GForce;
        public double PlanePitch;
        public double PlaneBank;
        public double PlaneHeading;

        // Controles de Voo e Freios (4 doubles)
        public double FlapsHandlePercent; // 0.0 a 1.0 (Percent Over 100)
        public double VelocityBodyY;      // ft/sec
        public double BrakeLeftPosition;  // 0 a 32767
        public double BrakeRightPosition; // 0 a 32767

        // Alertas de Voo (1 double)
        public double OverspeedWarning;   // 1.0 = true

        // Motor e Propulsão (6 doubles)
        public double EngineRPM;
        public double EngineCHT;
        public double EngineOilTemp;
        public double EngineOilPressure;
        public double TurboITT;
        public double EngineN1;

        // Condições Ambientais e Atmosféricas (5 doubles)
        public double SurfaceCondition;      // 0=Normal, 1=Wet, 2=Icy, 3=Snow
        public double AmbientTemperature;    // Celsius
        public double AmbientPrecipState;    // 0=None, >0=Chuva/Precipitação
        public double AmbientWindVelocity;   // Knots
        public double AmbientWindDirection;  // Degrees

        // Dinâmica Lateral e Comandos do Piloto (4 doubles)
        public double VelocityBodyX;         // ft/sec (Velocidade lateral do corpo no toque/rolagem)
        public double ThrottlePercent;       // 0 a 100%
        public double MixturePercent;        // 0 a 100%
        public double EngAntiIce;            // 1=On (Carb Heat / Anti-Ice), 0=Off

        // Gelo e Pressurização (3 doubles)
        public double StructuralIcePct;      // 0 a 100%
        public double PitotIcePct;           // 0 a 100%
        public double CabinAltitude;         // Feet

        // Simulação e Tempo (1 double)
        public double SimulationRate;        // Taxa de simulação (1.0, 2.0, 4.0, etc.)

        // --- PROPRIEDADES CALCULADAS (NÃO AFETAM LAYOUT DO STRUCT) ---
        public double PlaneLatitude => Latitude;
        public double PlaneLongitude => Longitude;
        public double EngineOilTempF => (EngineOilTemp * 1.8) + 32.0;
        public bool IsOnGround => OnGround > 0.5;
        public bool IsEngineRunning => EngineRunning > 0.5;
        public bool IsParkingBrakeSet => ParkingBrake > 0.5;
        public bool HasOverspeed => OverspeedWarning > 0.5;
        public double FlapsPercent => FlapsHandlePercent <= 1.0 ? Math.Round(FlapsHandlePercent * 100.0, 1) : Math.Round(FlapsHandlePercent, 1);
        public double FuelWeightPerGallon => FuelTotalGallons > 0 ? (FuelWeightLbs / FuelTotalGallons) : 6.0;
        public double MaxRPM => (EngineType == 1.0 || EngineType == 5.0) ? 100.0 : 2700.0;
        public double ZeroFuelWeightLbs { get => EmptyWeightLbs; set { } }

        public bool IsWetOrContaminatedSurface => SurfaceCondition >= 1.0;
        public bool IsPrecipitating => AmbientPrecipState > 0.5;
        public bool IsCarbHeatOn => EngAntiIce > 0.5;
        public double CrosswindComponentKnots
        {
            get
            {
                double angleDiff = (AmbientWindDirection - PlaneHeading) * (Math.PI / 180.0);
                return Math.Abs(AmbientWindVelocity * Math.Sin(angleDiff));
            }
        }
        public double CrashSequence => 0.0;
    }
}
