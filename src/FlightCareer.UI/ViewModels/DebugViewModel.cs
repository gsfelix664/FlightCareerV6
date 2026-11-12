using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FlightCareer.UI.ViewModels
{
    public partial class DebugViewModel : ObservableObject
    {
        private readonly SimConnectService _simConnect;
        private readonly Infrastructure.Persistence.AppDbContext _context;
        private readonly IServiceProvider? _serviceProvider;
        private System.Windows.Threading.DispatcherTimer? _timer;

        [ObservableProperty] private bool _debugEnabled;
        [ObservableProperty] private string _title = "Cessna C152";
        [ObservableProperty] private double _latitude = -23.5017;
        [ObservableProperty] private double _longitude = -46.6358;
        [ObservableProperty] private double _altitude = 2500;
        [ObservableProperty] private double _groundSpeed = 0;
        [ObservableProperty] private bool _onGround = true;
        [ObservableProperty] private bool _engineRunning = false;
        [ObservableProperty] private double _parkingBrake = 1.0;
        [ObservableProperty] private double _fuelGallons = 13.0;
        [ObservableProperty] private double _fuelCapacity = 26.0;
        [ObservableProperty] private double _emptyWeight = 1101;
        [ObservableProperty] private double _totalWeight = 1349;
        [ObservableProperty] private double _maxGrossWeight = 1670;

        // Novos campos para Debug de Desgaste
        [ObservableProperty] private double _verticalSpeed = 0;
        [ObservableProperty] private double _gForce = 1.0;
        [ObservableProperty] private double _airspeed = 0;
        [ObservableProperty] private double _flapsPercent = 0;
        [ObservableProperty] private double _velocityBodyY = 0;
        [ObservableProperty] private double _brakeLeft = 0;
        [ObservableProperty] private double _brakeRight = 0;
        [ObservableProperty] private bool _overspeed = false;
        [ObservableProperty] private double _crashSequence = 0;

        // Motor
        [ObservableProperty] private double _engineRPM = 0;
        [ObservableProperty] private double _maxRPM = 2700;
        [ObservableProperty] private double _engineCHT = 100;
        [ObservableProperty] private double _engineOilTemp = 80;
        [ObservableProperty] private double _engineOilPressure = 60;
        [ObservableProperty] private double _turboITT = 400;
        [ObservableProperty] private double _engineN1 = 0;
        [ObservableProperty] private bool _isJetMode = false;

        // Atitude
        [ObservableProperty] private double _planeBank = 0;
        [ObservableProperty] private double _planePitch = 0;
        [ObservableProperty] private double _planeHeading = 0;

        // Status de Conexão e Telemetria
        [ObservableProperty] private string _simStatusBadge = "⚪ SIMULADOR DESCONECTADO (AGUARDANDO MSFS)";
        [ObservableProperty] private string _simStatusColor = "#6B7280";
        [ObservableProperty] private bool _isSimulatorLive = false;

        [ObservableProperty] private string _airportSearch = string.Empty;
        public ObservableCollection<Airport> Airports { get; } = new();

        public DebugViewModel(SimConnectService simConnect, Infrastructure.Persistence.AppDbContext context, IServiceProvider? serviceProvider = null)
        {
            _simConnect = simConnect;
            _context = context;
            _serviceProvider = serviceProvider;
            UpdateCommand = new RelayCommand(UpdateData);
            ToggleDebugCommand = new RelayCommand(ToggleDebug);
            TeleportCommand = new RelayCommand(Teleport);

            LoadNearbyAirports();

            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => { if (DebugEnabled) UpdateData(); };

            // Inscrição para receber telemetria real do simulador
            _simConnect.OnDataReceived += OnSimDataReceived;
            _simConnect.OnConnected += (s, e) => UpdateConnectionStatus();
            _simConnect.OnDisconnected += (s, e) => UpdateConnectionStatus();
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (DebugEnabled)
                {
                    SimStatusBadge = "🟡 MODO DEBUG ATIVO (INJEÇÃO MANUAL HABILITADA)";
                    SimStatusColor = "#F59E0B";
                    IsSimulatorLive = false;
                }
                else if (_simConnect.IsConnected)
                {
                    SimStatusBadge = "🟢 TELEMETRIA AO VIVO DO SIMULADOR (MSFS CONECTADO)";
                    SimStatusColor = "#10B981";
                    IsSimulatorLive = true;
                }
                else
                {
                    SimStatusBadge = "⚪ SIMULADOR DESCONECTADO (AGUARDANDO MSFS)";
                    SimStatusColor = "#6B7280";
                    IsSimulatorLive = false;
                }
            });
        }

        private void OnSimDataReceived(object? sender, SimTelemetry data)
        {
            if (DebugEnabled) return;

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                Title = data.Title ?? string.Empty;
                Latitude = Math.Round(data.Latitude, 6);
                Longitude = Math.Round(data.Longitude, 6);
                Altitude = Math.Round(data.Altitude, 0);
                GroundSpeed = Math.Round(data.GroundSpeed, 1);
                OnGround = data.IsOnGround;
                EngineRunning = data.IsEngineRunning;
                ParkingBrake = Math.Round(data.ParkingBrake, 2);
                FuelGallons = Math.Round(data.FuelTotalGallons, 1);
                FuelCapacity = Math.Round(data.FuelTotalCapacity, 1);
                EmptyWeight = Math.Round(data.EmptyWeightLbs, 0);
                TotalWeight = Math.Round(data.TotalWeightLbs, 0);
                MaxGrossWeight = Math.Round(data.MaxGrossWeightLbs, 0);

                VerticalSpeed = Math.Round(data.VerticalSpeed, 0);
                GForce = Math.Round(data.GForce, 2);
                Airspeed = Math.Round(data.Airspeed, 1);
                FlapsPercent = Math.Round(data.FlapsHandlePercent * 100.0, 1);
                VelocityBodyY = Math.Round(data.VelocityBodyY, 2);
                BrakeLeft = Math.Round(data.BrakeLeftPosition, 0);
                BrakeRight = Math.Round(data.BrakeRightPosition, 0);
                Overspeed = data.OverspeedWarning > 0.5;
                CrashSequence = _simConnect.IsCrashed ? 11 : 0;

                EngineRPM = Math.Round(data.EngineRPM, 0);
                MaxRPM = Math.Round(data.MaxRPM, 0);
                EngineCHT = Math.Round(data.EngineCHT, 1);
                EngineOilTemp = Math.Round(data.EngineOilTemp, 1);
                EngineOilPressure = Math.Round(data.EngineOilPressure, 1);
                TurboITT = Math.Round(data.TurboITT, 1);
                EngineN1 = Math.Round(data.EngineN1, 1);
                IsJetMode = data.EngineType == 1 || data.EngineType == 5;

                PlaneBank = Math.Round(data.PlaneBank, 1);
                PlanePitch = Math.Round(data.PlanePitch, 1);
                PlaneHeading = (Math.Round(data.PlaneHeading, 0) % 360 + 360) % 360;

                UpdateConnectionStatus();
            });
        }

        [RelayCommand]
        private async Task ResetMarketMissionsAsync()
        {
            try
            {
                if (_serviceProvider != null)
                {
                    var mainVm = _serviceProvider.GetService<MainViewModel>();
                    if (mainVm != null)
                    {
                        await mainVm.RefreshMarketMissionsAsync();
                        System.Windows.MessageBox.Show("Missões do aeroporto atual foram resetadas e regeradas com sucesso com base nas regras atualizadas de hubs!", "Debug - Mercado de Missões", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        return;
                    }
                }

                System.Windows.MessageBox.Show("Não foi possível acessar a instância de MainViewModel.", "Aviso Debug", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao resetar missões: {ex.Message}", "Erro Debug", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public IRelayCommand UpdateCommand { get; }
        public IRelayCommand ToggleDebugCommand { get; }
        public IRelayCommand TeleportCommand { get; }

        [RelayCommand]
        private void OpenOverlay()
        {
            if (_serviceProvider != null)
            {
                var overlayWindow = _serviceProvider.GetService<Views.DebugOverlayWindow>();
                overlayWindow?.Show();
                overlayWindow?.Activate();
            }
        }

        [RelayCommand]
        private void LoadPreset(string preset)
        {
            switch (preset)
            {
                case "C152":
                    Title = "Cessna C152";
                    IsJetMode = false;
                    FuelCapacity = 26;
                    FuelGallons = 13;
                    EmptyWeight = 1101;
                    MaxGrossWeight = 1670;
                    TotalWeight = 1349;
                    MaxRPM = 2700;
                    break;
                case "C172":
                    Title = "C172SP G1000";
                    IsJetMode = false;
                    FuelCapacity = 56;
                    FuelGallons = 28;
                    EmptyWeight = 1665;
                    MaxGrossWeight = 2550;
                    TotalWeight = 2003;
                    MaxRPM = 2700;
                    break;
                case "B350":
                    Title = "Beechcraft King Air";
                    IsJetMode = true;
                    FuelCapacity = 539;
                    FuelGallons = 200;
                    EmptyWeight = 9955;
                    MaxGrossWeight = 15000;
                    TotalWeight = 11325;
                    break;
            }
            UpdateData();
        }

        private void Teleport()
        {
            var airport = _context.Airports.FirstOrDefault(a => a.ICAO == AirportSearch.ToUpper());
            if (airport != null)
            {
                Latitude = airport.Latitude;
                Longitude = airport.Longitude;
                Altitude = 2500; 
                OnGround = true;
                LoadNearbyAirports();
                UpdateData();
            }
        }

        private void ToggleDebug()
        {
            _simConnect.DebugMode = DebugEnabled;
            UpdateConnectionStatus();
            if (DebugEnabled) 
            {
                _timer?.Start();
                UpdateData();
            }
            else 
            {
                _timer?.Stop();
            }
        }

        private void UpdateData()
        {
            var data = new SimTelemetry
            {
                Title = Title,
                Latitude = Latitude,
                Longitude = Longitude,
                Altitude = Altitude,
                GroundSpeed = GroundSpeed,
                OnGround = OnGround ? 1.0 : 0.0,
                EngineRunning = EngineRunning ? 1.0 : 0.0,
                ParkingBrake = ParkingBrake,
                EngineType = IsJetMode ? 1.0 : 0.0,
                FuelTotalGallons = FuelGallons / 1.15, 
                FuelTotalCapacity = FuelCapacity,
                FuelWeightLbs = FuelGallons * 6.0,
                TotalWeightLbs = TotalWeight,
                EmptyWeightLbs = EmptyWeight,
                UnusableFuelGallons = 0,
                FuelWeightPerGallon = 6.0,
                MaxGrossWeightLbs = MaxGrossWeight,
                ZeroFuelWeightLbs = TotalWeight - (FuelGallons * 6.0),

                EngineRPM = EngineRPM,
                VerticalSpeed = VerticalSpeed,
                GForce = GForce,
                Airspeed = Airspeed,
                FlapsHandlePercent = FlapsPercent / 100.0,

                VelocityBodyY = VelocityBodyY,
                BrakeLeftPosition = BrakeLeft,
                BrakeRightPosition = BrakeRight,
                OverspeedWarning = Overspeed ? 1.0 : 0.0,

                EngineCHT = EngineCHT,
                EngineOilTemp = EngineOilTemp,
                EngineOilPressure = EngineOilPressure,
                TurboITT = TurboITT,
                EngineN1 = EngineN1,
                EngineFailed = 0.0,

                PlaneBank = PlaneBank,
                PlanePitch = PlanePitch,
                PlaneHeading = PlaneHeading
            };

            _simConnect.SetDebugData(data);
        }

        [RelayCommand] private void StartEngines() { EngineRunning = true; UpdateData(); }
        [RelayCommand] private void ReleaseParkingBrake() { ParkingBrake = 0.0; UpdateData(); }
        [RelayCommand] private void SetParkingBrake() { ParkingBrake = 1.0; UpdateData(); }
        [RelayCommand] private void TakeOff() { OnGround = false; GroundSpeed = 80; Altitude = 500; UpdateData(); }
        [RelayCommand] private void Land() { OnGround = true; GroundSpeed = 0; Altitude = 2500; UpdateData(); }
        [RelayCommand] private void StopEngines() { EngineRunning = false; UpdateData(); }

        private void LoadNearbyAirports()
        {
            var nearby = _context.Airports
                .Where(a => a.Latitude > Latitude - 2 && a.Latitude < Latitude + 2 &&
                            a.Longitude > Longitude - 2 && a.Longitude < Longitude + 2)
                .Take(50).ToList();
            Airports.Clear();
            foreach (var a in nearby) Airports.Add(a);
        }
    }
}
