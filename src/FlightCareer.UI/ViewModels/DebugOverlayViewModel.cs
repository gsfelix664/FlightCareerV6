using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.UI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FlightCareer.UI.ViewModels
{
    public partial class DebugOverlayViewModel : ObservableObject
    {
        private readonly SimConnectService _simConnect;
        private readonly FlightMonitor _flightMonitor;
        private readonly HangarViewModel _hangarViewModel;
        private readonly SensorRecorder _sensorRecorder;
        private readonly AppDbContext? _context;
        private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;
        private readonly ComponentCatalog _componentCatalog;
        private readonly PartCatalog _partCatalog;
        private readonly ObjectiveCardService? _objectiveCard;

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
        [ObservableProperty] private double _originLat = 0;
        [ObservableProperty] private double _originLon = 0;
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

        // --- SAÚDE DA CÉLULA E DESGASTE (HANGAR - SAÚDE CONHECIDA / SEM VAZAR REAL) ---
        [ObservableProperty] private string _airframeStatusText = "Em ordem";
        [ObservableProperty] private string _airframeStatusColor = "#00E676";
        [ObservableProperty] private SolidColorBrush _airframeStatusBrush = HangarPalette.Green;
        [ObservableProperty] private Geometry _airframeStatusIconGeometry = HangarIcons.Check;

        [ObservableProperty] private string _engineStatusText = "Em ordem";
        [ObservableProperty] private string _engineStatusColor = "#00E676";
        [ObservableProperty] private SolidColorBrush _engineStatusBrush = HangarPalette.Green;
        [ObservableProperty] private Geometry _engineStatusIconGeometry = HangarIcons.Check;

        [ObservableProperty] private string _tiresStatusText = "Em ordem";
        [ObservableProperty] private string _tiresStatusColor = "#00E676";
        [ObservableProperty] private SolidColorBrush _tiresStatusBrush = HangarPalette.Green;
        [ObservableProperty] private Geometry _tiresStatusIconGeometry = HangarIcons.Check;

        public string AirframeConditionColor => AirframeStatusColor;
        public string EngineConditionColor => EngineStatusColor;
        public string TiresConditionColor => TiresStatusColor;

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

        // --- MODO CALIBRAÇÃO (F2.4b-CAL) ---
        [ObservableProperty] private bool _isCalibrating = false;
        [ObservableProperty] private string _calibrationStepTitle = string.Empty;
        [ObservableProperty] private string _calibrationStepText = string.Empty;
        [ObservableProperty] private string _calibrationTimerText = string.Empty;
        [ObservableProperty] private bool _canAdvanceStep = false;
        [ObservableProperty] private string _calibrationCommandOutput = string.Empty;
        [ObservableProperty] private string _calibrationCsvPath = string.Empty;
        [ObservableProperty] private bool _hasCalibrationOutput = false;
        private CalibrationScript? _calibrationScript;
        private DispatcherTimer? _calibrationTimer;
        private DateTime _lastCalibrationTick;

        public event Action? RequestClose;

        /// <summary>Áreas, pontos e escalas dos contratos ativos, desenhados no mini-mapa.</summary>
        [ObservableProperty] private IReadOnlyList<Mission>? _contractMissions;
        [ObservableProperty] private string _contractProgressText = string.Empty;
        [ObservableProperty] private bool _hasContractProgress;
        private string _contractMarkersKey = string.Empty;

        /// <summary>Contratos aceitos no briefing: o mini-mapa os mostra antes de o monitoramento começar.</summary>
        public IReadOnlyCollection<Mission>? AcceptedMissions { get; set; }

        private void RefreshContractMarkers()
        {
            IReadOnlyList<Mission> missions = _flightMonitor.IsMonitoring
                ? _flightMonitor.ActiveMissions
                : AcceptedMissions?.ToList() ?? new List<Mission>();
            string key = string.Join("|", missions.Select(m => m.Id));
            if (key == _contractMarkersKey) return;
            _contractMarkersKey = key;

            // O mapa monta área, pontos e faixa a partir das missões (FlightMapControl.ContractMissions)
            ContractMissions = missions.ToList();
        }

        public DebugOverlayViewModel(
            SimConnectService simConnect,
            FlightMonitor flightMonitor,
            HangarViewModel hangarViewModel,
            SensorRecorder sensorRecorder,
            AppDbContext? context = null,
            IDbContextFactory<AppDbContext>? dbContextFactory = null,
            ComponentCatalog? componentCatalog = null,
            PartCatalog? partCatalog = null,
            ObjectiveCardService? objectiveCard = null)
        {
            _simConnect = simConnect;
            _flightMonitor = flightMonitor;
            _hangarViewModel = hangarViewModel;
            _sensorRecorder = sensorRecorder;
            _context = context;
            _dbContextFactory = dbContextFactory;
            _componentCatalog = componentCatalog ?? ComponentCatalog.LoadDefault();
            _partCatalog = partCatalog ?? PartCatalog.LoadDefault();
            _objectiveCard = objectiveCard;

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
                    if (DestLat == 0 || DestLon == 0)
                    {
                        var destApt = FindAirport(m.DestinationICAO);
                        if (destApt != null)
                        {
                            DestLat = destApt.Latitude;
                            DestLon = destApt.Longitude;
                        }
                    }
                    if (OriginLat == 0 || OriginLon == 0)
                    {
                        var origApt = FindAirport(m.OriginICAO);
                        if (origApt != null)
                        {
                            OriginLat = origApt.Latitude;
                            OriginLon = origApt.Longitude;
                        }
                    }
                }
                else if (_flightMonitor.IsMonitoring)
                {
                    RouteText = "VOO LOCAL / CIRCUITO";
                    DestLat = 0;
                    DestLon = 0;
                    DestIcao = string.Empty;
                    if (OriginLat == 0 && OriginLon == 0 && (data.PlaneLatitude != 0 || data.PlaneLongitude != 0))
                    {
                        OriginLat = data.PlaneLatitude;
                        OriginLon = data.PlaneLongitude;
                        OriginIcao = "PARTIDA";
                    }
                }
                else
                {
                    RouteText = "EM SOLO";
                    DestLat = 0;
                    DestLon = 0;
                    DestIcao = string.Empty;
                    if (AcceptedMissions is { Count: > 0 } && (data.PlaneLatitude != 0 || data.PlaneLongitude != 0))
                    {
                        // Com contrato aceito, o ponto de partida entra no enquadramento junto com a área
                        // Atualiza só se não houver partida ou se ela sobrou de outro voo (> ~1 NM)
                        if (OriginIcao != "PARTIDA" || Math.Abs(OriginLat - data.PlaneLatitude) > 0.02 || Math.Abs(OriginLon - data.PlaneLongitude) > 0.02)
                        {
                            OriginLat = data.PlaneLatitude;
                            OriginLon = data.PlaneLongitude;
                            OriginIcao = "PARTIDA";
                        }
                    }
                    else
                    {
                        OriginLat = 0;
                        OriginLon = 0;
                        OriginIcao = string.Empty;
                    }
                }

                // Depois da origem e do destino, para o mapa enquadrar tudo
                RefreshContractMarkers();

                ContractProgressText = _flightMonitor.IsMonitoring ? _flightMonitor.ContractProgressText : string.Empty;
                HasContractProgress = ContractProgressText.Length > 0;

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
                else if (IsRecording || _sensorRecorder.LastError != null)
                {
                    // Gravação parou sozinha (erro de I/O) ou falhou ao iniciar: o botão volta para REC
                    IsRecording = false;
                    RecordingStatus = _sensorRecorder.LastError != null ? $"ERRO: {_sensorRecorder.LastError}" : "PARADO";
                }
            });
        }

        private void RefreshAircraftHealth()
        {
            var activeAircraft = _flightMonitor.CurrentAircraft ?? _hangarViewModel.SelectedAircraft?.Aircraft;
            if (activeAircraft == null)
            {
                ApplyHudSummary(HudHealthSummary.Build(Array.Empty<AircraftPart>(), _partCatalog, 0, 0.4));
                return;
            }

            AircraftPrefix = activeAircraft.Prefix;
            double airframeHours = activeAircraft.CurrentAirframeHours;

            List<AircraftPart>? parts = null;
            List<AircraftComponent>? components = null;

            if (_dbContextFactory != null)
            {
                using var ctx = _dbContextFactory.CreateDbContext();
                parts = ctx.AircraftParts.AsNoTracking().Where(p => p.PilotAircraftId == activeAircraft.Id).ToList();
                components = ctx.AircraftComponents.AsNoTracking().Where(c => c.PilotAircraftId == activeAircraft.Id).ToList();
            }
            else if (_context != null)
            {
                parts = _context.AircraftParts.AsNoTracking().Where(p => p.PilotAircraftId == activeAircraft.Id).ToList();
                components = _context.AircraftComponents.AsNoTracking().Where(c => c.PilotAircraftId == activeAircraft.Id).ToList();
            }
            else
            {
                parts = activeAircraft.Parts?.ToList();
                components = activeAircraft.Components?.ToList();
            }

            HudHealthSummary summary;
            if (parts != null && parts.Count > 0)
            {
                summary = HudHealthSummary.Build(
                    parts,
                    _partCatalog,
                    airframeHours,
                    _componentCatalog.Visibility?.GrowthPerHour ?? 0.4,
                    _componentCatalog.PartBands);
            }
            else if (components != null && components.Count > 0)
            {
                summary = HudHealthSummary.BuildFromComponents(
                    components,
                    airframeHours,
                    _componentCatalog.Visibility);
            }
            else
            {
                summary = HudHealthSummary.Build(
                    Array.Empty<AircraftPart>(),
                    _partCatalog,
                    airframeHours,
                    _componentCatalog.Visibility?.GrowthPerHour ?? 0.4,
                    _componentCatalog.PartBands);
            }

            ApplyHudSummary(summary);
        }

        private void ApplyHudSummary(HudHealthSummary summary)
        {
            AirframeStatusText = summary.Airframe.StatusText;
            AirframeStatusBrush = HangarPalette.ForDisplayState(summary.Airframe.State);
            AirframeStatusColor = BrushToHex(AirframeStatusBrush);
            AirframeStatusIconGeometry = HangarIcons.ForDisplayState(summary.Airframe.State);

            EngineStatusText = summary.Engine.StatusText;
            EngineStatusBrush = HangarPalette.ForDisplayState(summary.Engine.State);
            EngineStatusColor = BrushToHex(EngineStatusBrush);
            EngineStatusIconGeometry = HangarIcons.ForDisplayState(summary.Engine.State);

            TiresStatusText = summary.Tires.StatusText;
            TiresStatusBrush = HangarPalette.ForDisplayState(summary.Tires.State);
            TiresStatusColor = BrushToHex(TiresStatusBrush);
            TiresStatusIconGeometry = HangarIcons.ForDisplayState(summary.Tires.State);
        }

        private static string BrushToHex(SolidColorBrush brush)
        {
            return $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
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

        private Airport? FindAirport(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return null;
            if (_dbContextFactory != null)
            {
                using var ctx = _dbContextFactory.CreateDbContext();
                return ctx.Airports.AsNoTracking().FirstOrDefault(a => a.ICAO == icao);
            }
            if (_context != null)
            {
                return _context.Airports.AsNoTracking().FirstOrDefault(a => a.ICAO == icao);
            }
            return null;
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
        private void ShowObjectiveCard()
        {
            _objectiveCard?.Show();
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

        // ---------------------------------------------------------------- testes do spike de falhas
        // Botões de depuração para testar no sim as opções de "falhar o sistema sem mexer no comando"
        // (docs/plans/spike-falhas-sem-comando.md). Cada envio grava um marcador "teste-..." no REC.

        [ObservableProperty] private string _debugEventName = "ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE";
        [ObservableProperty] private string _debugEventParams = "1 1";
        [ObservableProperty] private string _debugEventStatus = string.Empty;
        [ObservableProperty] private bool _isFlapSurfaceLockActive;
        private bool _flapLockSubscribed;

        [RelayCommand]
        private void PresetAlternatorBus()
        {
            DebugEventName = "ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE";
            DebugEventParams = "1 1";
        }

        [RelayCommand]
        private void PresetAlternatorBreaker()
        {
            DebugEventName = "ELECTRICAL_ALTERNATOR_BREAKER_TOGGLE";
            DebugEventParams = "1";
        }

        [RelayCommand]
        private void PresetCircuit()
        {
            DebugEventName = "ELECTRICAL_CIRCUIT_TOGGLE";
            DebugEventParams = "1";
        }

        [RelayCommand]
        private void SendDebugEvent()
        {
            var name = (DebugEventName ?? string.Empty).Trim().ToUpperInvariant();
            if (name.Length == 0) return;
            var parts = (DebugEventParams ?? string.Empty)
                .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var p = new uint[5];
            for (int i = 0; i < parts.Length && i < 5; i++)
            {
                if (!uint.TryParse(parts[i], out p[i]))
                {
                    DebugEventStatus = $"Parâmetro inválido: {parts[i]}";
                    return;
                }
            }
            _simConnect.SendEventEx(name, p[0], p[1], p[2], p[3], p[4]);
            string label = $"teste-evento {name} {string.Join(' ', parts)}";
            _sensorRecorder.Mark(label);
            DebugEventStatus = $"Enviado: {name} {string.Join(' ', parts)} ({DateTime.Now:HH:mm:ss})";
        }

        [RelayCommand]
        private void ToggleFlapSurfaceLock()
        {
            IsFlapSurfaceLockActive = !IsFlapSurfaceLockActive;
            if (IsFlapSurfaceLockActive && !_flapLockSubscribed)
            {
                _simConnect.OnFailureStateReceived += HoldFlapSurface;
                _flapLockSubscribed = true;
            }
            else if (!IsFlapSurfaceLockActive && _flapLockSubscribed)
            {
                _simConnect.OnFailureStateReceived -= HoldFlapSurface;
                _flapLockSubscribed = false;
            }
            _sensorRecorder.Mark(IsFlapSurfaceLockActive ? "teste-flap-superficie-50-on" : "teste-flap-superficie-50-off");
            DebugEventStatus = IsFlapSurfaceLockActive ? "Superfície do flap travada em 50%" : "Superfície do flap liberada";
        }

        // A cada leitura rápida (~6 frames) reescreve a superfície: é o teste de "jitter" do spike
        private void HoldFlapSurface(SimFailureState _) => _simConnect.WriteFlapSurfacePercent(0.5);

        [RelayCommand]
        private void MarkEvent()
        {
            if (!string.IsNullOrWhiteSpace(MarkerText))
            {
                _sensorRecorder.Mark(MarkerText);
                MarkerText = string.Empty;
            }
        }

        [RelayCommand]
        private async Task StartCalibration()
        {
            if (IsCalibrating) return;

            try
            {
                _calibrationScript = CalibrationScript.LoadDefault("calibration_script.json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DebugOverlay] Erro ao carregar calibration_script.json: {ex.Message}");
                return;
            }

            _sensorRecorder.FrameReader.CalibrationMode = true;

            if (!_sensorRecorder.IsRecording)
            {
                await _sensorRecorder.StartAsync();
            }

            IsCalibrating = true;
            HasCalibrationOutput = false;
            CalibrationCommandOutput = string.Empty;
            CalibrationCsvPath = string.Empty;

            _lastCalibrationTick = DateTime.UtcNow;
            if (_calibrationTimer == null)
            {
                _calibrationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                _calibrationTimer.Tick += OnCalibrationTimerTick;
            }
            _calibrationTimer.Start();

            UpdateCalibrationUi();
        }

        private void OnCalibrationTimerTick(object? sender, EventArgs e)
        {
            if (!IsCalibrating || _calibrationScript == null) return;

            var now = DateTime.UtcNow;
            double dt = (now - _lastCalibrationTick).TotalSeconds;
            _lastCalibrationTick = now;

            _calibrationScript.Tick(dt);
            UpdateCalibrationUi();
        }

        private void UpdateCalibrationUi()
        {
            if (_calibrationScript == null || _calibrationScript.CurrentStep == null)
            {
                CalibrationStepTitle = string.Empty;
                CalibrationStepText = string.Empty;
                CalibrationTimerText = string.Empty;
                CanAdvanceStep = false;
                return;
            }

            var step = _calibrationScript.CurrentStep;
            CalibrationStepTitle = $"Passo {_calibrationScript.CurrentIndex + 1}/{_calibrationScript.ActiveSteps.Count}: {step.Id}";
            CalibrationStepText = step.Text;
            CanAdvanceStep = _calibrationScript.CanAdvance;

            if (_calibrationScript.TimeRemaining > 0)
            {
                CalibrationTimerText = $"{_calibrationScript.ElapsedInCurrentStep:F1}s / mín {step.MinSeconds:F0}s (aguarde {_calibrationScript.TimeRemaining:F1}s)";
            }
            else
            {
                CalibrationTimerText = $"{_calibrationScript.ElapsedInCurrentStep:F1}s (tempo mínimo cumprido)";
            }
        }

        [RelayCommand]
        private async Task AdvanceCalibrationStep()
        {
            if (!IsCalibrating || _calibrationScript == null || !_calibrationScript.CanAdvance) return;

            var current = _calibrationScript.CurrentStep;
            if (current != null)
            {
                _sensorRecorder.Mark($"cal:{current.Id}");
            }

            _calibrationScript.TryAdvance(out var next);

            if (_calibrationScript.IsCompleted || next?.Id == "end")
            {
                if (next?.Id == "end")
                {
                    _sensorRecorder.Mark("cal:end");
                }
                await FinishCalibrationAsync();
            }
            else
            {
                UpdateCalibrationUi();
            }
        }

        [RelayCommand]
        private async Task SkipCalibrationStep()
        {
            if (!IsCalibrating || _calibrationScript == null) return;

            var current = _calibrationScript.CurrentStep;
            if (current != null)
            {
                _sensorRecorder.Mark($"cal-skip:{current.Id}");
            }

            var next = _calibrationScript.Skip();

            if (_calibrationScript.IsCompleted || next?.Id == "end")
            {
                if (next?.Id == "end")
                {
                    _sensorRecorder.Mark("cal:end");
                }
                await FinishCalibrationAsync();
            }
            else
            {
                UpdateCalibrationUi();
            }
        }

        [RelayCommand]
        private void CancelCalibration()
        {
            if (!IsCalibrating) return;

            _calibrationTimer?.Stop();
            _sensorRecorder.FrameReader.CalibrationMode = false;
            IsCalibrating = false;
            UpdateCalibrationUi();
        }

        private async Task FinishCalibrationAsync()
        {
            _calibrationTimer?.Stop();
            string csvPath = _sensorRecorder.CurrentFilePath ?? string.Empty;
            _sensorRecorder.Stop();
            _sensorRecorder.FrameReader.CalibrationMode = false;

            IsCalibrating = false;
            CalibrationCsvPath = csvPath;

            CalibrationCommandOutput = $"python tools/aircraft_limits/calibrate.py \"{csvPath}\" --write";
            HasCalibrationOutput = true;
            UpdateCalibrationUi();
            await Task.CompletedTask;
        }
    }
}
