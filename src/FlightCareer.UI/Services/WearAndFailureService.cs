using FlightCareer.Domain.Entities;
using FlightCareer.UI.Services;
using System;

namespace FlightCareer.UI.Services
{
    public class WearAndFailureService
    {
        private readonly SimConnectService _simConnect;
        private readonly Random _random = new();
        private DateTime _lastEngineFailureCheck = DateTime.MinValue;
        private DateTime _lastTireFailureCheck = DateTime.MinValue;
        private DateTime _lastAirframeFailureCheck = DateTime.MinValue;

        public WearAndFailureService(SimConnectService simConnect)
        {
            _simConnect = simConnect;
        }

        public void ProcessWear(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            // 1. Desgaste do MOTOR
            double engineWearRate = 0;
            bool isJetOrTurbo = data.EngineType == 1.0 || data.EngineType == 5.0;
            double normalLifeHours = isJetOrTurbo ? 1000 : 100;

            // Desgaste base
            engineWearRate = 100.0 / normalLifeHours;

            if (isJetOrTurbo)
            {
                // --- LÓGICA PARA JATO / TURBOPROP ---
                if (data.TurboITT > 800)
                {
                    double ittExcess = data.TurboITT - 800;
                    engineWearRate += ittExcess * 2.0;
                }

                if (data.EngineN1 > 100)
                {
                    engineWearRate += (data.EngineN1 - 100) * 10.0;
                }
            }
            else
            {
                // --- LÓGICA PARA MOTORES A PISTÃO ---
                if (data.MaxRPM > 0 && data.EngineRPM > (data.MaxRPM * 0.95))
                {
                    engineWearRate += 150.0; 
                }

                if (data.EngineCHT > 230)
                {
                    engineWearRate += (data.EngineCHT - 230) * 5.0;
                }

                if (data.IsEngineRunning && data.EngineOilPressure < 20)
                {
                    engineWearRate += 500.0;
                }

                if (data.EngineOilTemp > 110)
                {
                    engineWearRate += (data.EngineOilTemp - 110) * 10.0;
                }
            }

            aircraft.EngineCondition = Math.Max(0, aircraft.EngineCondition - (engineWearRate * deltaTimeHours));

            // 2. Desgaste da FUSELAGEM
            double airframeWearRate = 100.0 / 400.0; 

            double gForce = Math.Abs(data.GForce);
            if (data.GForce > 3.8 || data.GForce < -1.5)
            {
                airframeWearRate += (gForce * 50.0); 
            }

            if (data.OverspeedWarning > 0.5)
            {
                airframeWearRate += 300.0;
            }

            if (data.CrashSequence > 0)
            {
                aircraft.AirframeCondition = 0;
                aircraft.EngineCondition = 0;
                aircraft.TiresCondition = 0;
                airframeWearRate = 0;
            }
            else
            {
                aircraft.AirframeCondition = Math.Max(0, aircraft.AirframeCondition - (airframeWearRate * deltaTimeHours));
            }

            // 3. Desgaste dos PNEUS
            if (data.OnGround > 0.5)
            {
                double tireWearRate = 0.1;
                if (data.GroundSpeed > 25) tireWearRate += (data.GroundSpeed - 25) * 0.5;
                double avgBrake = (data.BrakeLeftPosition + data.BrakeRightPosition) / 2.0;
                if (avgBrake > 1000) tireWearRate += (avgBrake / 32767.0) * 50.0;
                aircraft.TiresCondition = Math.Max(0, aircraft.TiresCondition - (tireWearRate * deltaTimeHours));
            }

            CheckForFailures(aircraft);
        }

        public void ProcessLanding(PilotAircraft aircraft, SimTelemetry data)
        {
            aircraft.LandingsCount++;
            double touchdownFPM = Math.Abs(data.VelocityBodyY) * 60.0;
            double tireWear = 100.0 / 30.0;

            if (touchdownFPM > 400)
            {
                double extraTireDamage = ((touchdownFPM - 400) / 100.0) * 5.0;
                tireWear += extraTireDamage;
                double airframeDamage = ((touchdownFPM - 400) / 100.0) * 2.0;
                aircraft.AirframeCondition = Math.Max(0, aircraft.AirframeCondition - airframeDamage);
            }

            aircraft.TiresCondition = Math.Max(0, aircraft.TiresCondition - tireWear);
        }

        private void CheckForFailures(PilotAircraft aircraft)
        {
            if (aircraft.EngineCondition < 30)
            {
                if ((DateTime.Now - _lastEngineFailureCheck).TotalMinutes >= 10)
                {
                    _lastEngineFailureCheck = DateTime.Now;
                    double failureChance = (30 - aircraft.EngineCondition) / 30.0;
                    if (_random.NextDouble() < failureChance) _simConnect.SetFailure("TOGGLE_ENGINE_FAILURE_1");
                }
            }
            else _lastEngineFailureCheck = DateTime.MinValue;

            if (aircraft.TiresCondition < 30)
            {
                if ((DateTime.Now - _lastTireFailureCheck).TotalMinutes >= 10)
                {
                    _lastTireFailureCheck = DateTime.Now;
                    double failureChance = (30 - aircraft.TiresCondition) / 30.0;
                    if (_random.NextDouble() < failureChance) _simConnect.SetFailure("TOGGLE_TIRE_FAILURE");
                }
            }
            else _lastTireFailureCheck = DateTime.MinValue;

            if (aircraft.AirframeCondition < 30)
            {
                if ((DateTime.Now - _lastAirframeFailureCheck).TotalMinutes >= 10)
                {
                    _lastAirframeFailureCheck = DateTime.Now;
                    double failureChance = (30 - aircraft.AirframeCondition) / 30.0;
                    if (_random.NextDouble() < failureChance) _simConnect.SetFailure("FLAPS_UP");
                }
            }
            else _lastAirframeFailureCheck = DateTime.MinValue;
        }
    }
}
