using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.UI.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.UI.ViewModels
{
    public partial class BriefingViewModel : ObservableObject
    {
        private readonly SimConnectService _simConnect;
        private readonly Infrastructure.Persistence.AppDbContext _context;
        private DateTime _lastLookup = DateTime.MinValue;
        private string _nearestAirportICAO = string.Empty;

        [ObservableProperty] private string _aircraftTitle = "DESCONHECIDO";
        [ObservableProperty] private double _fuelQuantity;
        [ObservableProperty] private double _totalWeight;
        [ObservableProperty] private double _emptyWeight;
        [ObservableProperty] private double _latitude;
        [ObservableProperty] private double _longitude;
        [ObservableProperty] private string _currentLocation = "AGUARDANDO SIMULADOR...";
        [ObservableProperty] private string _nearestAirportInfo = "LOCALIZANDO...";
        [ObservableProperty] private Mission? _selectedMission;
        [ObservableProperty] private double _destLat;
        [ObservableProperty] private double _destLon;
        [ObservableProperty] private string _flightNumber = "---";
        [ObservableProperty] private string _hangarAircraftModel = "---";
        [ObservableProperty] private string _hangarAircraftLocation = "---";
        [ObservableProperty] private double _hangarAircraftFuel;
        
        [ObservableProperty] private string _weightStatusText = "AGUARDANDO DADOS";
        [ObservableProperty] private string _weightStatusColor = "#9CA3AF";
        [ObservableProperty] private double _calculatedTotalWeight;
        [ObservableProperty] private bool _canStartFlight;
        [ObservableProperty] private bool _isMonitoring;

        public bool CanStartFlightOrCancel => CanStartFlight || IsMonitoring;

        partial void OnCanStartFlightChanged(bool value) => OnPropertyChanged(nameof(CanStartFlightOrCancel));
        partial void OnIsMonitoringChanged(bool value) => OnPropertyChanged(nameof(CanStartFlightOrCancel));

        public string FlightDestination => SelectedMission?.DestinationICAO ?? "---";

        partial void OnSelectedMissionChanged(Mission? value)
        {
            OnPropertyChanged(nameof(FlightDestination));
            ValidatePreFlightConditions();

            if (value != null)
            {
                _ = UpdateDestinationCoordinatesAsync(value.DestinationICAO);
            }
            else
            {
                DestLat = 0;
                DestLon = 0;
            }
        }

        private async System.Threading.Tasks.Task UpdateDestinationCoordinatesAsync(string icao)
        {
            try
            {
                var airport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == icao);
                if (airport != null)
                {
                    DestLat = airport.Latitude;
                    DestLon = airport.Longitude;
                }
            }
            catch { }
        }

        public double TotalManifestWeight => AcceptedMissions.Sum(m => m.PayloadLbs) + 170; 
        public int TotalPaxCount => AcceptedMissions.Sum(m => m.PassengerCount);
        public double TotalCargoWeight => AcceptedMissions.Sum(m => m.CargoWeightLbs + m.LuggageWeightLbs);

        public ObservableCollection<Mission> AcceptedMissions { get; }

        public BriefingViewModel(SimConnectService simConnect, 
            ObservableCollection<Mission> activeMissions,
            Infrastructure.Persistence.AppDbContext context)
        {
            _simConnect = simConnect;
            _context = context;
            AcceptedMissions = activeMissions;
            AcceptedMissions.CollectionChanged += (s, e) => 
            {
                OnPropertyChanged(nameof(TotalManifestWeight));
                OnPropertyChanged(nameof(TotalPaxCount));
                OnPropertyChanged(nameof(TotalCargoWeight));
                ValidatePreFlightConditions();
            };

            _simConnect.OnDataReceived += (s, data) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AircraftTitle = data.Title;
                    FuelQuantity = Math.Round(data.FuelTotalGallons * 1.15, 1);
                    TotalWeight = data.TotalWeightLbs;
                    EmptyWeight = data.EmptyWeightLbs;
                    Latitude = data.Latitude;
                    Longitude = data.Longitude;
                    CurrentLocation = $"{data.Latitude:F4}, {data.Longitude:F4}";

                    CalculatedTotalWeight = TotalManifestWeight + data.FuelWeightLbs + data.EmptyWeightLbs;
                    ValidatePreFlightConditions();

                    if ((DateTime.Now - _lastLookup).TotalSeconds > 10)
                    {
                        _ = UpdateNearestAirportAsync(data.Latitude, data.Longitude);
                        _lastLookup = DateTime.Now;
                    }
                });
            };
        }

        private async System.Threading.Tasks.Task UpdateNearestAirportAsync(double lat, double lon)
        {
            try 
            {
                double range = 1.0; // Aumentado para 1 grau para ser mais tolerante
                var nearbyAirports = await _context.Airports.Where(a => 
                        a.Latitude > lat - range && a.Latitude < lat + range &&
                        a.Longitude > lon - range && a.Longitude < lon + range)
                    .ToListAsync();

                var nearest = nearbyAirports
                    .Select(a => new { Airport = a, Distance = Domain.Utils.LocationUtils.CalculateDistance(lat, lon, a.Latitude, a.Longitude) })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                if (nearest != null && nearest.Distance < 5) // Se estiver a menos de 5NM de um aeroporto
                {
                    _nearestAirportICAO = nearest.Airport.ICAO;
                    NearestAirportInfo = $"{nearest.Airport.ICAO} - {nearest.Airport.Name}\n{nearest.Airport.City}, {nearest.Airport.Country}";
                }
                else
                {
                    _nearestAirportICAO = string.Empty;
                    NearestAirportInfo = "NENHUM AEROPORTO PRÓXIMO (OU DISTANTE > 5NM)";
                }
                ValidatePreFlightConditions();
            }
            catch { NearestAirportInfo = "ERRO AO BUSCAR AEROPORTO"; }
        }

        public void Refresh()
        {
            ValidatePreFlightConditions();
        }

        private static bool IsAircraftMatching(string? simTitle, string? hangarModel)
        {
            if (string.IsNullOrWhiteSpace(simTitle) || string.IsNullOrWhiteSpace(hangarModel))
                return false;

            string sSim = simTitle.Trim().ToUpper();
            string sHangar = hangarModel.Split('(')[0].Trim().ToUpper();

            // 1. Contenção direta em qualquer direção
            if (sSim.Contains(sHangar) || sHangar.Contains(sSim))
                return true;

            // 2. Normalizar removendo 'C' antes de números de 3 dígitos (ex: C152 -> 152, C172 -> 172, C208 -> 208) e caracteres especiais
            string Clean(string s)
            {
                s = System.Text.RegularExpressions.Regex.Replace(s, @"\bC([0-9]{3})\b", "$1");
                return System.Text.RegularExpressions.Regex.Replace(s, @"[^A-Z0-9]", "");
            }

            string cSim = Clean(sSim);
            string cHangar = Clean(sHangar);

            if (!string.IsNullOrEmpty(cSim) && !string.IsNullOrEmpty(cHangar))
            {
                if (cSim.Contains(cHangar) || cHangar.Contains(cSim))
                    return true;
            }

            // 3. Comparação de identificador numérico de modelo (3 a 4 dígitos: 152, 172, 208, 350, 737, 747, 787, 320, 330)
            var simNums = System.Text.RegularExpressions.Regex.Matches(sSim, @"\d{3,4}")
                .Select(m => m.Value)
                .ToList();
            var hangarNums = System.Text.RegularExpressions.Regex.Matches(sHangar, @"\d{3,4}")
                .Select(m => m.Value)
                .ToList();

            if (simNums.Any() && hangarNums.Any())
            {
                if (simNums.Intersect(hangarNums).Any())
                    return true;
            }

            return false;
        }

        private void ValidatePreFlightConditions()
        {
            if (!_simConnect.IsConnected)
            {
                WeightStatusText = "SIMULADOR DESCONECTADO";
                WeightStatusColor = "#9CA3AF";
                CanStartFlight = false;
                return;
            }

            string modelOnly = (HangarAircraftModel ?? "").Split('(')[0].Trim();
            bool aircraftOk = IsAircraftMatching(AircraftTitle, HangarAircraftModel);
            
            // Comparação de localização robusta e insensível a maiúsculas
            string simLocation = (_nearestAirportICAO ?? "").Trim().ToUpper();
            string hangarLocation = (HangarAircraftLocation ?? "").Trim().ToUpper();
            bool locationOk = !string.IsNullOrEmpty(simLocation) && simLocation == hangarLocation;

            double fuelDiff = Math.Abs(FuelQuantity - HangarAircraftFuel);
            bool fuelOk = HangarAircraftFuel > 0 && (fuelDiff <= (HangarAircraftFuel * 0.05));
            double weightDiff = Math.Abs(TotalWeight - CalculatedTotalWeight);
            bool weightOk = weightDiff <= (CalculatedTotalWeight * 0.02);

            CanStartFlight = false;

            if (!aircraftOk) { WeightStatusText = $"ERRO: AERONAVE INCORRETA (SIM: {AircraftTitle} / HANGAR: {modelOnly})"; WeightStatusColor = "#EF4444"; }
            else if (!locationOk) { WeightStatusText = $"ERRO: LOCALIZAÇÃO INCORRETA (SIM: {simLocation} / HANGAR: {hangarLocation})"; WeightStatusColor = "#EF4444"; }
            else if (!fuelOk) { WeightStatusText = $"ERRO: COMBUSTÍVEL INCORRETO (SIM: {FuelQuantity:F1} / HANGAR: {HangarAircraftFuel:F1} GAL)"; WeightStatusColor = "#EF4444"; }
            else if (!weightOk) { WeightStatusText = $"ERRO: CARGA/ZFW INCORRETO (SIM: {TotalWeight:F0} / NECESSÁRIO: {CalculatedTotalWeight:F0} LBS)"; WeightStatusColor = "#EF4444"; }
            else { WeightStatusText = "PRONTO PARA DECOLAGEM"; WeightStatusColor = "#2ECC71"; CanStartFlight = true; }
        }
    }
}
