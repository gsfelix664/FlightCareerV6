using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.UI.Services;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using GMap.NET;

namespace FlightCareer.UI.ViewModels
{
    public partial class BriefingViewModel : ObservableObject
    {
        private readonly SimConnectService _simConnect;
        private readonly Infrastructure.Persistence.AppDbContext _context;
        private DateTime _lastLookup = DateTime.MinValue;
        private string _nearestAirportICAO = string.Empty;

        // Propriedades do Simulador (SimConnect)
        [ObservableProperty] private string _aircraftTitle = "DESCONHECIDO";
        [ObservableProperty] private double _fuelQuantity;
        [ObservableProperty] private double _totalWeight;
        [ObservableProperty] private double _emptyWeight;
        [ObservableProperty] private double _latitude;
        [ObservableProperty] private double _longitude;
        [ObservableProperty] private bool _isOnGround = true;
        [ObservableProperty] private double _aircraftHeading;
        [ObservableProperty] private double _altitude;
        [ObservableProperty] private double _groundSpeed;
        [ObservableProperty] private string _flightPhaseText = "SOLO / PREPARAÇÃO";
        [ObservableProperty] private string _currentLocation = "AGUARDANDO SIMULADOR...";
        [ObservableProperty] private string _nearestAirportInfo = "LOCALIZANDO...";
        
        // Propriedades da Aeronave Ativa (Hangar)
        [ObservableProperty] private string _hangarAircraftModel = "---";
        [ObservableProperty] private string _hangarAircraftPrefix = "---";
        [ObservableProperty] private string _hangarAircraftLocation = "---";
        [ObservableProperty] private double _hangarAircraftFuel;
        [ObservableProperty] private double _hangarAircraftMtow = 2500;
        [ObservableProperty] private double _hangarAircraftCruiseSpeed = 110;

        // Voo e Rota
        [ObservableProperty] private string _flightNumber = "---";
        [ObservableProperty] private Mission? _selectedMission;
        [ObservableProperty] private double _destLat;
        [ObservableProperty] private double _destLon;
        [ObservableProperty] private string _routeSummaryDisplay = "--- ➔ ---";
        [ObservableProperty] private double _totalRouteDistanceNM;
        [ObservableProperty] private string _totalRouteEteFormatted = "--";

        // Status e Gatekeeper (Plan vs Sim)
        [ObservableProperty] private string _weightStatusText = "AGUARDANDO DADOS";
        [ObservableProperty] private string _weightStatusColor = "#9CA3AF";
        [ObservableProperty] private double _calculatedTotalWeight;
        [ObservableProperty] private bool _canStartFlight;
        [ObservableProperty] private bool _isMonitoring;
        [ObservableProperty] private string _gatekeeperBlockedReason = string.Empty;
        [ObservableProperty] private bool _isGatekeeperAllOk;

        public string GatekeeperBadgeBg => IsGatekeeperAllOk ? "#0D2B1E" : "#2E1214";
        public string GatekeeperIcon => IsGatekeeperAllOk ? "🟢" : "🔴";

        partial void OnIsGatekeeperAllOkChanged(bool value)
        {
            OnPropertyChanged(nameof(GatekeeperBadgeBg));
            OnPropertyChanged(nameof(GatekeeperIcon));
        }

        public bool CanStartFlightOrCancel => CanStartFlight || IsMonitoring;

        partial void OnCanStartFlightChanged(bool value) => OnPropertyChanged(nameof(CanStartFlightOrCancel));
        partial void OnIsMonitoringChanged(bool value) => OnPropertyChanged(nameof(CanStartFlightOrCancel));

        public string FlightDestination => SelectedMission?.DestinationICAO ?? (RouteLegs.LastOrDefault()?.DestinationICAO ?? "---");

        partial void OnSelectedMissionChanged(Mission? value)
        {
            OnPropertyChanged(nameof(FlightDestination));
            _ = RebuildItineraryAndValidateAsync();

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

        // Coleções do Briefing Multi-Leg e Checklist
        public ObservableCollection<Mission> AcceptedMissions { get; }
        public ObservableCollection<FlightLegModel> RouteLegs { get; } = new();
        public ObservableCollection<PreFlightCheckItem> ChecklistItems { get; } = new();

        public double TotalManifestWeight => AcceptedMissions.Sum(m => m.PayloadLbs) + 170; // +170 lbs Piloto
        public int TotalPaxCount => AcceptedMissions.Sum(m => m.PassengerCount);
        public double TotalCargoWeight => AcceptedMissions.Sum(m => m.CargoWeightLbs + m.LuggageWeightLbs);
        public bool HasAcceptedMissions => AcceptedMissions.Any();

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
                OnPropertyChanged(nameof(HasAcceptedMissions));
                _ = RebuildItineraryAndValidateAsync();
            };

            _simConnect.OnDataReceived += (s, data) =>
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    AircraftTitle = data.Title;
                    FuelQuantity = Math.Round(data.FuelTotalGallons * 1.15, 1);
                    TotalWeight = data.TotalWeightLbs;
                    EmptyWeight = data.EmptyWeightLbs;
                    Latitude = data.Latitude;
                    Longitude = data.Longitude;
                    IsOnGround = data.IsOnGround;
                    AircraftHeading = data.PlaneHeading;
                    Altitude = data.Altitude;
                    GroundSpeed = data.GroundSpeed;
                    CurrentLocation = $"{data.Latitude:F4}, {data.Longitude:F4}";

                    CalculatedTotalWeight = TotalManifestWeight + data.FuelWeightLbs + data.EmptyWeightLbs;
                    ValidatePreFlightConditions();

                    if ((DateTime.Now - _lastLookup).TotalSeconds > 8)
                    {
                        _ = UpdateNearestAirportAsync(data.Latitude, data.Longitude);
                        _lastLookup = DateTime.Now;
                    }
                });
            };

            // Iniciar com checklist padrão
            InitializeChecklistItems();
        }

        private void InitializeChecklistItems()
        {
            if (ChecklistItems.Count == 0)
            {
                ChecklistItems.Add(new PreFlightCheckItem { ParameterName = "Conexão", PlannedValue = "SimConnect Ativo", SimReportedValue = "Aguardando...", IsOk = false });
                ChecklistItems.Add(new PreFlightCheckItem { ParameterName = "Aeronave", PlannedValue = "---", SimReportedValue = "Aguardando...", IsOk = false });
                ChecklistItems.Add(new PreFlightCheckItem { ParameterName = "Posição", PlannedValue = "---", SimReportedValue = "Aguardando...", IsOk = false });
                ChecklistItems.Add(new PreFlightCheckItem { ParameterName = "Payload (ZFW)", PlannedValue = "---", SimReportedValue = "Aguardando...", IsOk = false });
                ChecklistItems.Add(new PreFlightCheckItem { ParameterName = "Combustível", PlannedValue = "---", SimReportedValue = "Aguardando...", IsOk = false });
            }
        }

        public async System.Threading.Tasks.Task RebuildItineraryAndValidateAsync()
        {
            try
            {
                RouteLegs.Clear();
                string originIcao = string.IsNullOrWhiteSpace(HangarAircraftLocation) || HangarAircraftLocation == "---"
                    ? (AcceptedMissions.FirstOrDefault()?.OriginICAO ?? "SBJD")
                    : HangarAircraftLocation;

                if (!AcceptedMissions.Any())
                {
                    RouteSummaryDisplay = $"{originIcao} ➔ (SEM MISSÕES)";
                    TotalRouteDistanceNM = 0;
                    TotalRouteEteFormatted = "--";
                    ValidatePreFlightConditions();
                    return;
                }

                // Agrupar destinos únicos respeitando a ordem de pernas
                var distinctDestinations = AcceptedMissions.Select(m => m.DestinationICAO).Distinct().ToList();
                var legPoints = new List<string> { originIcao };
                legPoints.AddRange(distinctDestinations);

                RouteSummaryDisplay = string.Join(" ➔ ", legPoints);

                double totalDistance = 0;
                string currentPoint = originIcao;
                int legIndex = 1;

                foreach (var nextDest in distinctDestinations)
                {
                    var aptOrigin = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == currentPoint);
                    var aptDest = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == nextDest);

                    double dist = 40.0;
                    if (aptOrigin != null && aptDest != null)
                    {
                        dist = Math.Round(Domain.Utils.LocationUtils.CalculateDistance(aptOrigin.Latitude, aptOrigin.Longitude, aptDest.Latitude, aptDest.Longitude), 0);
                    }

                    totalDistance += dist;

                    // Missões a bordo nesta perna (cujas entregas acontecem neste destino ou em destinos posteriores)
                    int remainingDestIndex = distinctDestinations.IndexOf(nextDest);
                    var applicableMissions = AcceptedMissions
                        .Where(m => distinctDestinations.IndexOf(m.DestinationICAO) >= remainingDestIndex)
                        .ToList();

                    int legPax = applicableMissions.Sum(m => m.PassengerCount);
                    double legCargo = applicableMissions.Sum(m => m.CargoWeightLbs + m.LuggageWeightLbs);
                    double legPayload = (legPax * 170.0) + legCargo + 170.0; // +170 lbs Piloto

                    // Ações que acontecem ao pousar nesta escala
                    var deliveringMissions = AcceptedMissions.Where(m => m.DestinationICAO == nextDest).ToList();
                    int deliveringPax = deliveringMissions.Sum(m => m.PassengerCount);
                    double deliveringCargo = deliveringMissions.Sum(m => m.CargoWeightLbs + m.LuggageWeightLbs);

                    string actionText;
                    if (deliveringPax > 0 && deliveringCargo > 0)
                        actionText = $"↳ Desembarque: {deliveringPax} PAX e {deliveringCargo:N0} lbs carga em {nextDest}";
                    else if (deliveringPax > 0)
                        actionText = $"↳ Desembarque: {deliveringPax} PAX em {nextDest}";
                    else
                        actionText = $"↳ Entrega: {deliveringCargo:N0} lbs de carga em {nextDest}";

                    double emptyW = EmptyWeight > 0 ? EmptyWeight : 1400.0;
                    double fuelW = HangarAircraftFuel > 0 ? (HangarAircraftFuel * 6.0) : 180.0;
                    double tow = emptyW + fuelW + legPayload;
                    double mtow = HangarAircraftMtow > 0 ? HangarAircraftMtow : 2500.0;
                    double mtowPct = Math.Round((tow / mtow) * 100.0, 1);

                    double speed = HangarAircraftCruiseSpeed > 0 ? HangarAircraftCruiseSpeed : 110.0;
                    double legMinutes = Math.Round((dist / speed) * 60.0);
                    int lh = (int)legMinutes / 60;
                    int lm = (int)legMinutes % 60;
                    string legEte = lh > 0 ? $"{lh}h {lm:D2}m" : $"{lm} min";

                    RouteLegs.Add(new FlightLegModel
                    {
                        LegNumber = legIndex++,
                        OriginICAO = currentPoint,
                        DestinationICAO = nextDest,
                        DistanceNM = dist,
                        OnboardPax = legPax,
                        OnboardCargoLbs = legCargo,
                        LegPayloadLbs = legPayload,
                        TakeoffWeightLbs = tow,
                        AircraftMtowLbs = mtow,
                        MtowPercent = mtowPct,
                        IsOverweight = mtowPct > 100.0,
                        ActionSummary = actionText,
                        EteFormatted = legEte
                    });

                    currentPoint = nextDest;
                }

                TotalRouteDistanceNM = totalDistance;

                double totalSpeed = HangarAircraftCruiseSpeed > 0 ? HangarAircraftCruiseSpeed : 110.0;
                double totalMin = Math.Round((totalDistance / totalSpeed) * 60.0);
                int th = (int)totalMin / 60;
                int tm = (int)totalMin % 60;
                TotalRouteEteFormatted = th > 0 ? $"{th}h {tm:D2}m" : $"{tm} min";

                // Atualizar destino padrão no mapa se houver primeira perna
                if (RouteLegs.Any())
                {
                    _ = UpdateDestinationCoordinatesAsync(RouteLegs.First().DestinationICAO);
                }

                ValidatePreFlightConditions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BriefingViewModel] Erro ao reconstruir pernas: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task UpdateNearestAirportAsync(double lat, double lon)
        {
            try 
            {
                double range = 1.0; 
                var nearbyAirports = await _context.Airports.Where(a => 
                        a.Latitude > lat - range && a.Latitude < lat + range &&
                        a.Longitude > lon - range && a.Longitude < lon + range)
                    .ToListAsync();

                var nearest = nearbyAirports
                    .Select(a => new { Airport = a, Distance = Domain.Utils.LocationUtils.CalculateDistance(lat, lon, a.Latitude, a.Longitude) })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                if (nearest != null && nearest.Distance < 5)
                {
                    _nearestAirportICAO = nearest.Airport.ICAO;
                    NearestAirportInfo = $"{nearest.Airport.ICAO} - {nearest.Airport.Name}";
                }
                else
                {
                    _nearestAirportICAO = string.Empty;
                    NearestAirportInfo = "NENHUM AEROPORTO PRÓXIMO (< 5 NM)";
                }
                ValidatePreFlightConditions();
            }
            catch { NearestAirportInfo = "ERRO AO BUSCAR AEROPORTO"; }
        }

        public void Refresh()
        {
            _ = RebuildItineraryAndValidateAsync();
        }

        private static bool IsAircraftMatching(string? simTitle, string? hangarModel)
        {
            if (string.IsNullOrWhiteSpace(simTitle) || string.IsNullOrWhiteSpace(hangarModel))
                return false;

            string sSim = simTitle.Trim().ToUpper();
            string sHangar = hangarModel.Split('(')[0].Trim().ToUpper();

            if (sSim.Contains(sHangar) || sHangar.Contains(sSim))
                return true;

            string Clean(string s)
            {
                s = Regex.Replace(s, @"\bC([0-9]{3})\b", "$1");
                return Regex.Replace(s, @"[^A-Z0-9]", "");
            }

            string cSim = Clean(sSim);
            string cHangar = Clean(sHangar);

            if (!string.IsNullOrEmpty(cSim) && !string.IsNullOrEmpty(cHangar))
            {
                if (cSim.Contains(cHangar) || cHangar.Contains(cSim))
                    return true;
            }

            var simNums = Regex.Matches(sSim, @"\d{3,4}").Select(m => m.Value).ToList();
            var hangarNums = Regex.Matches(sHangar, @"\d{3,4}").Select(m => m.Value).ToList();

            if (simNums.Any() && hangarNums.Any())
            {
                if (simNums.Intersect(hangarNums).Any())
                    return true;
            }

            return false;
        }

        private void ValidatePreFlightConditions()
        {
            InitializeChecklistItems();

            // CASO 1: MONITORAMENTO EM ANDAMENTO
            if (IsMonitoring)
            {
                bool isAirborne = !IsOnGround || GroundSpeed > 35;
                string phase = isAirborne ? "VOO EM PROGRESSO" : (GroundSpeed > 4 ? "TÁXI / ROLAGEM" : "SOLO (MONITORAMENTO ATIVO)");
                FlightPhaseText = phase;
                WeightStatusText = $"{phase} • GS: {GroundSpeed:N0} KT • ALT: {Altitude:N0} FT";
                WeightStatusColor = isAirborne ? "#00E5FF" : "#00E676";
                GatekeeperBlockedReason = "Monitoramento ativo. Telemetria e parâmetros operacionais estão sendo gravados continuamente.";
                CanStartFlight = true;
                IsGatekeeperAllOk = true;

                double currentPayload = Math.Max(0, TotalWeight - EmptyWeight - (FuelQuantity * 6.0));
                UpdateChecklistItem(0, "Conexão", "MSFS 2020/2024", "Conectado (Ao Vivo)", true, "ATIVO");
                UpdateChecklistItem(1, "Aeronave", HangarAircraftModel, AircraftTitle, true, "CONFIRMADO");
                UpdateChecklistItem(2, "Posição / Voo", HangarAircraftLocation, isAirborne ? $"Em Voo ({Altitude:N0} ft)" : "Solo / Rampa", true, "A BORDO");
                UpdateChecklistItem(3, "Payload (W&B)", $"{TotalManifestWeight:N0} lbs", $"{currentPayload:N0} lbs", true, "DESPACHADO");
                UpdateChecklistItem(4, "Combustível", $"{HangarAircraftFuel:F1} gal", $"{FuelQuantity:F1} gal", true, "EM USO");
                return;
            }

            // CASO 2: DESPACHO PRÉ-VOO (RAMPA / SOLO)
            bool connectionOk = _simConnect.IsConnected;
            string modelOnly = (HangarAircraftModel ?? "").Split('(')[0].Trim();
            bool aircraftOk = connectionOk && IsAircraftMatching(AircraftTitle, HangarAircraftModel);

            string simLocation = (_nearestAirportICAO ?? "").Trim().ToUpper();
            string hangarLocation = (HangarAircraftLocation ?? "").Trim().ToUpper();
            bool locationOk = connectionOk && !string.IsNullOrEmpty(simLocation) && simLocation == hangarLocation;

            double fuelDiff = Math.Abs(FuelQuantity - HangarAircraftFuel);
            bool fuelOk = connectionOk && HangarAircraftFuel > 0 && (fuelDiff <= Math.Max(2.0, HangarAircraftFuel * 0.05));

            double plannedPayload = TotalManifestWeight;
            double reportedSimPayload = Math.Max(0, TotalWeight - EmptyWeight - (FuelQuantity * 6.0));
            double payloadDiff = Math.Abs(reportedSimPayload - plannedPayload);
            bool payloadOk = connectionOk && (payloadDiff <= Math.Max(50.0, plannedPayload * 0.04));

            // Atualizar os 5 itens do checklist
            UpdateChecklistItem(0, "Conexão", "MSFS 2020/2024", connectionOk ? "Conectado" : "Desconectado", connectionOk, connectionOk ? "OK" : "Sem Conexão");
            UpdateChecklistItem(1, "Aeronave", string.IsNullOrEmpty(modelOnly) ? "---" : modelOnly, string.IsNullOrEmpty(AircraftTitle) ? "---" : AircraftTitle, aircraftOk, aircraftOk ? "OK" : "Diferente");
            UpdateChecklistItem(2, "Posição", string.IsNullOrEmpty(hangarLocation) ? "---" : hangarLocation, string.IsNullOrEmpty(simLocation) ? "Aguardando GPS" : simLocation, locationOk, locationOk ? "OK" : "Posição Divergente");
            UpdateChecklistItem(3, "Payload (W&B)", $"{plannedPayload:N0} lbs ({TotalPaxCount} PAX)", $"{reportedSimPayload:N0} lbs", payloadOk, payloadOk ? "OK" : $"{reportedSimPayload - plannedPayload:+0;-0} lbs");
            UpdateChecklistItem(4, "Combustível", $"{HangarAircraftFuel:F1} gal ({HangarAircraftFuel * 6:N0} lbs)", $"{FuelQuantity:F1} gal ({FuelQuantity * 6:N0} lbs)", fuelOk, fuelOk ? "OK" : $"{FuelQuantity - HangarAircraftFuel:+0.0;-0.0} gal");

            bool allChecksPassed = connectionOk && aircraftOk && locationOk && fuelOk && payloadOk;
            IsGatekeeperAllOk = allChecksPassed;
            CanStartFlight = allChecksPassed;

            if (!connectionOk)
            {
                WeightStatusText = "SIMULADOR DESCONECTADO (Inicie o MSFS)";
                WeightStatusColor = "#EF4444";
                GatekeeperBlockedReason = "Voo bloqueado: O simulador MSFS não está conectado.";
            }
            else if (!aircraftOk)
            {
                WeightStatusText = $"AERONAVE INCORRETA (Sim: {AircraftTitle})";
                WeightStatusColor = "#EF4444";
                GatekeeperBlockedReason = $"Voo bloqueado: A aeronave carregada no MSFS ({AircraftTitle}) não coincide com o modelo ativo ({modelOnly}).";
            }
            else if (!locationOk)
            {
                WeightStatusText = $"LOCALIZAÇÃO DIVERGENTE (Sim: {simLocation} / Base: {hangarLocation})";
                WeightStatusColor = "#EF4444";
                GatekeeperBlockedReason = $"Voo bloqueado: Sua aeronave está em {hangarLocation}, mas o simulador reportou estar em {simLocation}.";
            }
            else if (!fuelOk)
            {
                WeightStatusText = $"COMBUSTÍVEL DIVERGENTE (Sim: {FuelQuantity:F1} / Plano: {HangarAircraftFuel:F1} gal)";
                WeightStatusColor = "#FFB300";
                GatekeeperBlockedReason = $"Voo bloqueado: Ajuste o combustível nos tanques do MSFS para {HangarAircraftFuel:F1} galões.";
            }
            else if (!payloadOk)
            {
                WeightStatusText = $"CARGA/PAYLOAD DIVERGENTE (Sim: {reportedSimPayload:F0} / Plano: {plannedPayload:F0} lbs)";
                WeightStatusColor = "#FFB300";
                GatekeeperBlockedReason = $"Voo bloqueado: Ajuste o Peso e Balanceamento (W&B) no MSFS para {plannedPayload:F0} lbs.";
            }
            else
            {
                WeightStatusText = "PRONTO PARA DECOLAGEM";
                WeightStatusColor = "#00E676";
                GatekeeperBlockedReason = "Todos os parâmetros coincidem perfeitamente. Voo liberado!";
            }
        }

        private void UpdateChecklistItem(int index, string paramName, string planned, string reported, bool isOk, string diff)
        {
            if (index < ChecklistItems.Count)
            {
                var item = ChecklistItems[index];
                item.ParameterName = paramName;
                item.PlannedValue = planned;
                item.SimReportedValue = reported;
                item.IsOk = isOk;
                item.DiffMessage = diff;
                item.Icon = isOk ? "🟢" : "🔴";
            }
        }

        [RelayCommand]
        private void CopyPreFlightWeights()
        {
            try
            {
                double fuelLbs = HangarAircraftFuel * 6.0;
                string text = 
$@"=== FLIGHTCAREER V6 - PRE-FLIGHT W&B DISPATCH ===
Voo: {FlightNumber} | Rota: {RouteSummaryDisplay}
Aeronave: {HangarAircraftModel}
Localização: {HangarAircraftLocation}

[PESOS PARA AJUSTAR NO SIMULADOR]
• Passageiros a Bordo: {TotalPaxCount} PAX ({TotalPaxCount * 170:N0} lbs)
• Bagagem + Carga: {TotalCargoWeight:N0} lbs
• PAYLOAD TOTAL (W&B): {TotalManifestWeight:N0} lbs (inclui Piloto 170 lbs)
• COMBUSTÍVEL: {HangarAircraftFuel:F1} gal ({fuelLbs:N0} lbs)
• TOW ESTIMADO: {CalculatedTotalWeight:N0} lbs (MTOW: {HangarAircraftMtow:N0} lbs)
================================================";

                Clipboard.SetText(text);
                MessageBox.Show("Dados de Peso e Combustível copiados para a Área de Transferência!\n\nCole no Bloco de Notas ou consulte ao ajustar o W&B no simulador.", 
                    "Despacho W&B Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao copiar: {ex.Message}");
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
    }
}
