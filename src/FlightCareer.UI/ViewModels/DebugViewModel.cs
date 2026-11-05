using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FlightCareer.UI.ViewModels
{
    public partial class DebugViewModel : ObservableObject
    {
        private readonly SimConnectService _simConnect;
        private readonly Infrastructure.Persistence.AppDbContext _context;
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

        [ObservableProperty] private string _airportSearch = string.Empty;
        public ObservableCollection<Airport> Airports { get; } = new();

        public DebugViewModel(SimConnectService simConnect, Infrastructure.Persistence.AppDbContext context)
        {
            _simConnect = simConnect;
            _context = context;
            UpdateCommand = new RelayCommand(UpdateData);
            ToggleDebugCommand = new RelayCommand(ToggleDebug);
            TeleportCommand = new RelayCommand(Teleport);

            LoadNearbyAirports();

            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => { if (DebugEnabled) UpdateData(); };
        }

        public IRelayCommand UpdateCommand { get; }
        public IRelayCommand ToggleDebugCommand { get; }
        public IRelayCommand TeleportCommand { get; }

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
            if (DebugEnabled) 
            {
                _timer?.Start();
                UpdateData();
            }
            else _timer?.Stop();
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
                MaxGrossWeightLbs = MaxGrossWeight,
                ZeroFuelWeightLbs = TotalWeight - (FuelGallons * 6.0),
                FuelWeightPerGallon = 6.0,

                EngineRPM = EngineRPM,
                MaxRPM = MaxRPM,
                VerticalSpeed = VerticalSpeed,
                GForce = GForce,
                Airspeed = Airspeed,
                FlapsHandlePercent = FlapsPercent,

                VelocityBodyY = VelocityBodyY,
                BrakeLeftPosition = BrakeLeft,
                BrakeRightPosition = BrakeRight,
                OverspeedWarning = Overspeed ? 1.0 : 0.0,
                CrashSequence = CrashSequence,

                EngineCHT = EngineCHT,
                EngineOilTemp = EngineOilTemp,
                EngineOilPressure = EngineOilPressure,
                TurboITT = TurboITT,
                EngineN1 = EngineN1,

                PlaneBank = PlaneBank,
                PlanePitch = PlanePitch
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
