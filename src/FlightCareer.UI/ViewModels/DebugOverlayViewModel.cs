using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.UI.Services;
using System;
using System.Linq;
using System.Windows;

namespace FlightCareer.UI.ViewModels
{
    public partial class DebugOverlayViewModel : ObservableObject
    {
        private readonly SimConnectService _simConnect;
        private readonly FlightMonitor _flightMonitor;
        private readonly HangarViewModel _hangarViewModel;
        private readonly SensorRecorder _sensorRecorder;
        private readonly AppDbContext? _context;

        // --- CONTROLES DE JANELA (HUD OVERLAY) ---
        [ObservableProperty] private bool _isTopmost = true;
        [ObservableProperty] private double _windowOpacity = 0.90;
        [ObservableProperty] private bool _isCompactMode = false;
        [ObservableProperty] private string _opacityLabel = "90%";
        [ObservableProperty] private double _topmostOpacity = 1.0;
        [ObservableProperty] private string _compactToggleIcon = "🔼";
        [ObservableProperty] private string _parkingBrakeText = "PB";
        [ObservableProperty] private string _parkingBrakeColor = "#FF5252";
        [ObservableProperty] private string _engineModeLabel = "RPM";
        [ObservableProperty] private string _engineTempLabel = "CHT °C";

        // --- NAVEGAÇÃO & MOVING MAP (MINI-GPS) ---
        [ObservableProperty] private double _latitude = 0;
        [ObservableProperty] private double _longitude = 0;
        [ObservableProperty] private double _destLat = 0;
        [ObservableProperty] private double _destLon = 0;
        [ObservableProperty] private string _originIcao = string.Empty;
        [ObservableProperty] private string _destIcao = string.Empty;

        // --- CONEXÃO E IDENTIFICAÇÃO ---
        [ObservableProperty] private bool _isSimConnected = false;
        [ObservableProperty] private string _simStatusBadge = "⚪ DESCONECTADO";
        [ObservableProperty] private string _simStatusColor = "#6B7280";
        [ObservableProperty] private string _aircraftTitle = "Cessna C152";
        [ObservableProperty] private string _aircraftPrefix = "PR-NZY";
        [ObservableProperty] private string _flightPhase = "AGUARDANDO MOTORES";
        [ObservableProperty] private string _flightPhaseColor = "#8B9BAE";
        [ObservableProperty] private string _routeText = "SDTB ➔ DEST";

        // --- INSTRUMENTOS DE VOO (AVIONICS) ---
        [ObservableProperty] private double _altitude = 0;
        [ObservableProperty] private double _groundSpeed = 0;
        [ObservableProperty] private double _airspeed = 0;
        [ObservableProperty] private double _verticalSpeed = 0;
        [ObservableProperty] private string _verticalSpeedColor = "#00E5FF";

        // --- DINÂMICA & ATITUDE ---
        [ObservableProperty] private double _gForce = 1.0;
        [ObservableProperty] private string _gForceColor = "#00E5FF";
        [ObservableProperty] private double _pitch = 0;
        [ObservableProperty] private double _bank = 0;
        [ObservableProperty] private double _heading = 0;
        [ObservableProperty] private double _touchdownFpm = 0;
        [ObservableProperty] private double _flapsPercent = 0;
        [ObservableProperty] private bool _isOnGround = true;
        [ObservableProperty] private bool _isEngineRunning = false;
        [ObservableProperty] private bool _isParkingBrakeSet = true;

        // --- GRUPO MOTO-PROPULSOR ---
        [ObservableProperty] private bool _isJetMode = false;
        [ObservableProperty] private double _engineRPM = 0;
        [ObservableProperty] private double _maxRPM = 2700;
        [ObservableProperty] private double _engineN1 = 0;
        [ObservableProperty] private double _engineCHT = 0;
        [ObservableProperty] private string _engineChtColor = "#F0F4F8";
        [ObservableProperty] private double _engineOilTemp = 0;
        [ObservableProperty] private double _engineOilTempF = 0;
        [ObservableProperty] private string _engineOilTempLabel = "ÓLEO (°F)";
        [ObservableProperty] private string _engineOilTempColor = "#F0F4F8";
        [ObservableProperty] private double _engineOilPressure = 0;
        [ObservableProperty] private string _engineOilPressureColor = "#F0F4F8";
        [ObservableProperty] private double _turboITT = 0;
        [ObservableProperty] private string _turboIttColor = "#F0F4F8";

        // --- PESOS E COMBUSTÍVEL ---
        [ObservableProperty] private double _fuelGallons = 0;
        [ObservableProperty] private double _fuelLbs = 0;
        [ObservableProperty] private double _totalWeightLbs = 0;
        [ObservableProperty] private double _maxGrossWeightLbs = 0;
        [ObservableProperty] private double _weightPercentage = 0;

        // --- SAÚDE DA CÉLULA E DESGASTE (HANGAR) ---
        [ObservableProperty] private double _airframeCondition = 100;
        [ObservableProperty] private string _airframeConditionColor = "#00E676";
        [ObservableProperty] private double _engineCondition = 100;
        [ObservableProperty] private string _engineConditionColor = "#00E676";
        [ObservableProperty] private double _tiresCondition = 100;
        [ObservableProperty] private string _tiresConditionColor = "#00E676";

        // --- SEGURANÇA E ALERTAS ---
        [ObservableProperty] private bool _overspeedWarning = false;
        [ObservableProperty] private bool _hasActiveFailure = false;
        [ObservableProperty] private string _activeFailureText = string.Empty;
        [ObservableProperty] private double _crashSequence = 0;
        [ObservableProperty] private string _crashText = "0 (NORMAL)";
        [ObservableProperty] private string _crashTextColor = "#00E676";

        // --- GRAVADOR DE VALIDAÇÃO DE SENSORES (F1.3) ---
        [ObservableProperty] private bool _isRecording = false;
        [ObservableProperty] private string _recordingStatus = "PARADO";
        [ObservableProperty] private string _markerText = string.Empty;

        public event Action? RequestClose;

        public DebugOverlayViewModel(
            SimConnectService simConnect,
            FlightMonitor flightMonitor,
            HangarViewModel hangarViewModel,
            SensorRecorder sensorRecorder,
            AppDbContext? context = null)
        {
            _simConnect = simConnect;
            _flightMonitor = flightMonitor;
            _hangarViewModel = hangarViewModel;
            _sensorRecorder = sensorRecorder;
            _context = context;

            _simConnect.OnDataReceived += OnSimDataReceived;
            _simConnect.OnConnected += (s, e) => UpdateConnectionStatus();
            _simConnect.OnDisconnected += (s, e) => UpdateConnectionStatus();

            _flightMonitor.OnStatusChanged += status =>
            {
                System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
                {
                    FlightPhase = status;
                    UpdateFlightPhaseColor();
                });
            };

            UpdateConnectionStatus();
            RefreshAircraftHealth();
        }

        private void UpdateConnectionStatus()
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                IsSimConnected = _simConnect.IsConnected;
                if (_simConnect.IsConnected)
                {
                    SimStatusBadge = "🟢 MSFS AO VIVO";
                    SimStatusColor = "#00E676";
                }
                else
                {
                    SimStatusBadge = "⚪ DESCONECTADO";
                    SimStatusColor = "#6B7280";
                }
            });
        }

        private void OnSimDataReceived(object? sender, SimTelemetry data)
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                // Aeronave e Título
                AircraftTitle = string.IsNullOrWhiteSpace(data.Title) ? "Aeronave MSFS" : data.Title;
                var activeAircraft = _flightMonitor.CurrentAircraft ?? _hangarViewModel.SelectedAircraft?.Aircraft;
                if (activeAircraft != null)
                {
                    AircraftPrefix = activeAircraft.Prefix;
                    AirframeCondition = Math.Round(activeAircraft.AirframeCondition, 1);
                    EngineCondition = Math.Round(activeAircraft.EngineCondition, 1);
                    TiresCondition = Math.Round(activeAircraft.TiresCondition, 1);
                    UpdateConditionColors();
                }

                // Posição GPS em Tempo Real
                Latitude = data.PlaneLatitude;
                Longitude = data.PlaneLongitude;

                // Rota
                if (_flightMonitor.ActiveMissions.Count > 0)
                {
                    var m = _flightMonitor.ActiveMissions[0];
                    RouteText = $"{m.OriginICAO} ➔ {m.DestinationICAO} ({m.DistanceNM:N0} NM)";
                    OriginIcao = m.OriginICAO;
                    DestIcao = m.DestinationICAO;
                    if (_context != null && (DestLat == 0 || DestLon == 0))
                    {
                        var destApt = _context.Airports.FirstOrDefault(a => a.ICAO == m.DestinationICAO);
                        if (destApt != null)
                        {
                            DestLat = destApt.Latitude;
                            DestLon = destApt.Longitude;
                        }
                    }
                }
                else if (_flightMonitor.IsMonitoring)
                {
                    RouteText = "VOO LOCAL / CIRCUITO";
                    DestLat = 0;
                    DestLon = 0;
                    DestIcao = string.Empty;
                }
                else
                {
                    RouteText = "EM SOLO";
                    DestLat = 0;
                    DestLon = 0;
                    DestIcao = string.Empty;
                }

                // Velocidades e Altitude
                Altitude = Math.Round(data.Altitude, 0);
                GroundSpeed = Math.Round(data.GroundSpeed, 1);
                Airspeed = Math.Round(data.Airspeed, 1);
                VerticalSpeed = Math.Round(data.VerticalSpeed, 0); // SimConnect já fornece em ft/min
                UpdateVerticalSpeedColor();

                // Atitude e G-Force
                GForce = Math.Round(data.GForce, 2);
                UpdateGForceColor();
                Pitch = Math.Round(data.PlanePitch, 1);
                Bank = Math.Round(data.PlaneBank, 1);
                Heading = (Math.Round(data.PlaneHeading, 0) % 360 + 360) % 360;
                TouchdownFpm = Math.Round(Math.Abs(data.VelocityBodyY * 60.0), 0);
                FlapsPercent = Math.Round(data.FlapsPercent, 0); // 0 a 100%
                IsOnGround = data.IsOnGround;
                IsEngineRunning = data.IsEngineRunning;
                IsParkingBrakeSet = data.ParkingBrake > 0.5;
                ParkingBrakeText = IsParkingBrakeSet ? "PB" : "LIVRE";
                ParkingBrakeColor = IsParkingBrakeSet ? "#FF5252" : "#00E676";

                // Motores
                IsJetMode = data.EngineType == 1 || data.EngineType == 5;
                EngineModeLabel = IsJetMode ? "N1 %" : "RPM";
                EngineTempLabel = IsJetMode ? "ITT °C" : "CHT °C";
                EngineRPM = Math.Round(data.EngineRPM, 0);
                MaxRPM = Math.Round(data.MaxRPM, 0);
                EngineN1 = Math.Round(data.EngineN1, 1);

                EngineCHT = Math.Round(data.EngineCHT, 1);
                EngineChtColor = EngineCHT > 230 ? "#FF5252" : (EngineCHT > 210 ? "#FFB300" : "#F0F4F8");

                EngineOilTemp = Math.Round(data.EngineOilTemp, 1);
                EngineOilTempF = Math.Round(data.EngineOilTempF, 1);
                EngineOilTempColor = EngineOilTempF > 245 ? "#FF5252" : (EngineOilTempF > 212 ? "#FFB300" : "#F0F4F8");

                EngineOilPressure = Math.Round(data.EngineOilPressure, 1);
                EngineOilPressureColor = (IsEngineRunning && EngineOilPressure < 20) ? "#FF5252" : "#F0F4F8";

                TurboITT = Math.Round(data.TurboITT, 1);
                TurboIttColor = TurboITT > 800 ? "#FF5252" : (TurboITT > 750 ? "#FFB300" : "#F0F4F8");

                // Pesos
                FuelGallons = Math.Round(data.FuelTotalGallons, 1);
                FuelLbs = Math.Round(data.FuelWeightLbs, 0);
                TotalWeightLbs = Math.Round(data.TotalWeightLbs, 0);
                MaxGrossWeightLbs = Math.Round(data.MaxGrossWeightLbs, 0);
                WeightPercentage = MaxGrossWeightLbs > 0 ? Math.Min(100, Math.Round((TotalWeightLbs / MaxGrossWeightLbs) * 100, 1)) : 0;

                // Alertas e Crash
                OverspeedWarning = data.OverspeedWarning > 0.5;
                HasActiveFailure = _flightMonitor.Failures.HasFailures;
                ActiveFailureText = _flightMonitor.Failures.HasFailures 
                    ? $"⚠️ {_flightMonitor.Failures.LatestFailureSummary.ToUpper()}" 
                    : string.Empty;

                if (_simConnect.IsCrashed)
                {
                    CrashText = "⚠️ CRASH DETECTADO";
                    CrashTextColor = "#FF5252";
                }
                else
                {
                    CrashText = "0 (NORMAL)";
                    CrashTextColor = "#00E676";
                }

                RefreshAircraftHealth();

                if (!string.IsNullOrEmpty(_flightMonitor.CurrentStatus))
                {
                    FlightPhase = _flightMonitor.CurrentStatus;
                    UpdateFlightPhaseColor();
                }

                if (_sensorRecorder.IsRecording)
                {
                    IsRecording = true;
                    RecordingStatus = $"REC {_sensorRecorder.RowsWritten} lin";
                }
                else if (!IsRecording && _sensorRecorder.LastError != null)
                {
                    RecordingStatus = $"ERRO: {_sensorRecorder.LastError}";
                }
            });
        }

        private void RefreshAircraftHealth()
        {
            var activeAircraft = _flightMonitor.CurrentAircraft ?? _hangarViewModel.SelectedAircraft?.Aircraft;
            if (activeAircraft != null)
            {
                AircraftPrefix = activeAircraft.Prefix;
                AirframeCondition = Math.Round(activeAircraft.AirframeCondition, 1);
                EngineCondition = Math.Round(activeAircraft.EngineCondition, 1);
                TiresCondition = Math.Round(activeAircraft.TiresCondition, 1);
                UpdateConditionColors();
            }
        }

        private void UpdateVerticalSpeedColor()
        {
            if (VerticalSpeed > 150) VerticalSpeedColor = "#00E676"; // Subindo (Verde)
            else if (VerticalSpeed < -800) VerticalSpeedColor = "#FF5252"; // Descida acentuada (Vermelho)
            else if (VerticalSpeed < -200) VerticalSpeedColor = "#FFB300"; // Descida normal (Amarelo)
            else VerticalSpeedColor = "#00E5FF"; // Nivelado (Ciano)
        }

        private void UpdateGForceColor()
        {
            if (GForce > 3.8 || GForce < -1.0) GForceColor = "#FF5252"; // Perigo estrutural
            else if (GForce > 2.5 || GForce < 0.2) GForceColor = "#FFB300"; // Atenção
            else GForceColor = "#00E5FF"; // Normal
        }

        private void UpdateFlightPhaseColor()
        {
            if (FlightPhase.Contains("VOANDO") || FlightPhase.Contains("VOO ATIVO") || FlightPhase.Contains("NO AR")) FlightPhaseColor = "#00E676";
            else if (FlightPhase.Contains("POUSO") || FlightPhase.Contains("FINALIZANDO") || FlightPhase.Contains("ENCERRANDO")) FlightPhaseColor = "#00E5FF";
            else if (FlightPhase.Contains("CANCEL")) FlightPhaseColor = "#FFB300";
            else FlightPhaseColor = "#8B9BAE";
        }

        private void UpdateConditionColors()
        {
            AirframeConditionColor = AirframeCondition > 70 ? "#00E676" : (AirframeCondition > 35 ? "#FFB300" : "#FF5252");
            EngineConditionColor = EngineCondition > 70 ? "#00E676" : (EngineCondition > 35 ? "#FFB300" : "#FF5252");
            TiresConditionColor = TiresCondition > 70 ? "#00E676" : (TiresCondition > 35 ? "#FFB300" : "#FF5252");
        }

        [RelayCommand]
        private void ToggleTopmost()
        {
            IsTopmost = !IsTopmost;
            TopmostOpacity = IsTopmost ? 1.0 : 0.35;
        }

        [RelayCommand]
        private void ToggleCompact()
        {
            IsCompactMode = !IsCompactMode;
            CompactToggleIcon = IsCompactMode ? "🔽" : "🔼";
        }

        [RelayCommand]
        private void CycleOpacity()
        {
            if (WindowOpacity >= 0.95)
            {
                WindowOpacity = 0.80;
                OpacityLabel = "80%";
            }
            else if (WindowOpacity >= 0.75)
            {
                WindowOpacity = 0.60;
                OpacityLabel = "60%";
            }
            else if (WindowOpacity >= 0.55)
            {
                WindowOpacity = 0.40;
                OpacityLabel = "40%";
            }
            else
            {
                WindowOpacity = 1.00;
                OpacityLabel = "100%";
            }
        }

        [RelayCommand]
        private void Close()
        {
            RequestClose?.Invoke();
        }

        [RelayCommand]
        private async Task ToggleRecording()
        {
            if (_sensorRecorder.IsRecording)
            {
                _sensorRecorder.Stop();
                IsRecording = false;
                RecordingStatus = "PARADO";
            }
            else
            {
                RecordingStatus = "INICIANDO...";
                await _sensorRecorder.StartAsync();
                IsRecording = _sensorRecorder.IsRecording;
                if (IsRecording)
                {
                    RecordingStatus = $"REC {_sensorRecorder.RowsWritten} lin";
                }
                else if (_sensorRecorder.LastError != null)
                {
                    RecordingStatus = $"ERRO: {_sensorRecorder.LastError}";
                }
                else
                {
                    RecordingStatus = "PARADO";
                }
            }
        }

        [RelayCommand]
        private void MarkEvent()
        {
            if (!string.IsNullOrWhiteSpace(MarkerText))
            {
                _sensorRecorder.Mark(MarkerText);
                MarkerText = string.Empty;
            }
        }
    }
}
