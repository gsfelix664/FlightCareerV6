using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FlightCareer.Domain.Entities;

namespace FlightCareer.UI.Services
{
    public class FlightTelemetryLogger
    {
        private StreamWriter? _writer;
        private string? _currentFilePath;
        private DateTime _flightStartTime;
        private DateTime _lastLogTime = DateTime.MinValue;
        private double _maxGForce = 1.0;
        private double _minGForce = 1.0;
        private readonly object _lock = new();

        public bool IsLogging => _writer != null;

        public void StartFlightLog(string originIcao, string destinationIcao, PilotAircraft? aircraft)
        {
            lock (_lock)
            {
                try
                {
                    StopFlightLog("RESTART");

                    string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "flights");
                    Directory.CreateDirectory(logDir);

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string safeOrigin = string.IsNullOrWhiteSpace(originIcao) ? "ORIG" : originIcao.Trim().ToUpper();
                    string safeDest = string.IsNullOrWhiteSpace(destinationIcao) ? "DEST" : destinationIcao.Trim().ToUpper();
                    string safeAircraft = aircraft != null
                        ? (!string.IsNullOrWhiteSpace(aircraft.Prefix) ? aircraft.Prefix : (aircraft.AircraftType?.Model ?? "AIRCRAFT"))
                        : "AIRCRAFT";
                    safeAircraft = safeAircraft.Replace(" ", "_");

                    string fileName = $"flight_{timestamp}_{safeOrigin}_{safeDest}_{safeAircraft}.csv";
                    _currentFilePath = Path.Combine(logDir, fileName);

                    _flightStartTime = DateTime.Now;
                    _lastLogTime = DateTime.MinValue;
                    _maxGForce = 1.0;
                    _minGForce = 1.0;

                    _writer = new StreamWriter(new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                    
                    // Cabeçalho CSV formatado
                    _writer.WriteLine("Timestamp;ElapsedSec;Origin;Dest;Latitude;Longitude;AltitudeFt;GroundSpeedKt;AirspeedKt;VerticalSpeedFps;GForce;PitchDeg;BankDeg;HeadingDeg;IsOnGround;ParkingBrake;FlapsPct;EngineRunning;EngineRPM;EngineCHT;EngineOilTemp;EngineOilPress;TurboITT;EngineN1;FuelGallons;FuelWeightLbs;BrakeLeft;BrakeRight;AirframeCondition;EngineCondition;TiresCondition;Status");
                    _writer.Flush();

                    System.Diagnostics.Debug.WriteLine($"[TelemetryLogger] Blackbox iniciada em: {_currentFilePath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TelemetryLogger] Erro ao iniciar log: {ex.Message}");
                }
            }
        }

        public void LogFrame(SimTelemetry data, PilotAircraft? aircraft, string originIcao, string destinationIcao, string flightStatus)
        {
            if (_writer == null) return;

            // Log a cada 2 segundos para não sobrecarregar I/O
            if ((DateTime.Now - _lastLogTime).TotalSeconds < 2.0) return;

            lock (_lock)
            {
                try
                {
                    if (_writer == null) return;

                    _lastLogTime = DateTime.Now;
                    double elapsed = Math.Round((DateTime.Now - _flightStartTime).TotalSeconds, 1);

                    if (data.GForce > _maxGForce) _maxGForce = data.GForce;
                    if (data.GForce < _minGForce) _minGForce = data.GForce;

                    var inv = CultureInfo.InvariantCulture;

                    string line = string.Join(";",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        elapsed.ToString("F1", inv),
                        originIcao,
                        destinationIcao,
                        data.Latitude.ToString("F6", inv),
                        data.Longitude.ToString("F6", inv),
                        data.Altitude.ToString("F1", inv),
                        data.GroundSpeed.ToString("F1", inv),
                        data.Airspeed.ToString("F1", inv),
                        data.VerticalSpeed.ToString("F2", inv),
                        data.GForce.ToString("F2", inv),
                        data.PlanePitch.ToString("F2", inv),
                        data.PlaneBank.ToString("F2", inv),
                        data.PlaneHeading.ToString("F1", inv),
                        data.IsOnGround ? "1" : "0",
                        data.ParkingBrake.ToString("F2", inv),
                        data.FlapsHandlePercent.ToString("F1", inv),
                        data.IsEngineRunning ? "1" : "0",
                        data.EngineRPM.ToString("F0", inv),
                        data.EngineCHT.ToString("F1", inv),
                        data.EngineOilTemp.ToString("F1", inv),
                        data.EngineOilPressure.ToString("F1", inv),
                        data.TurboITT.ToString("F1", inv),
                        data.EngineN1.ToString("F1", inv),
                        data.FuelTotalGallons.ToString("F2", inv),
                        data.FuelWeightLbs.ToString("F1", inv),
                        data.BrakeLeftPosition.ToString("F2", inv),
                        data.BrakeRightPosition.ToString("F2", inv),
                        aircraft?.AirframeCondition.ToString("F1", inv) ?? "100.0",
                        aircraft?.EngineCondition.ToString("F1", inv) ?? "100.0",
                        aircraft?.TiresCondition.ToString("F1", inv) ?? "100.0",
                        flightStatus
                    );

                    _writer.WriteLine(line);
                    _writer.Flush();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TelemetryLogger] Erro ao gravar frame: {ex.Message}");
                }
            }
        }

        public void StopFlightLog(string summaryReason, double touchdownFpm = 0, double qualityScore = 100)
        {
            lock (_lock)
            {
                if (_writer == null) return;

                try
                {
                    double totalMinutes = Math.Round((DateTime.Now - _flightStartTime).TotalMinutes, 1);
                    var inv = CultureInfo.InvariantCulture;

                    _writer.WriteLine($"# --- RESUMO DE ENCERRAMENTO DA BLACKBOX ---");
                    _writer.WriteLine($"# Status Final: {summaryReason}");
                    _writer.WriteLine($"# Duração Total: {totalMinutes.ToString("F1", inv)} min");
                    _writer.WriteLine($"# Touchdown FPM: {touchdownFpm.ToString("F0", inv)} fpm");
                    _writer.WriteLine($"# G-Force Range: {_minGForce.ToString("F2", inv)}G a {_maxGForce.ToString("F2", inv)}G");
                    _writer.WriteLine($"# Score de Qualidade: {qualityScore.ToString("F0", inv)}%");
                    _writer.WriteLine($"# Encerrado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _writer.Flush();
                    _writer.Dispose();
                }
                catch { }
                finally
                {
                    _writer = null;
                }
            }
        }
    }
}
