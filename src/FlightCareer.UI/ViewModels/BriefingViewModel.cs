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
        private string _nearestAirportMsfsIdent = string.Empty;

        // Status de Conexão com Simulador / Debug (Exibição Superior)
        [ObservableProperty] private string _connectionModeText = "SIMULADOR DESCONECTADO";
        [ObservableProperty] private string _connectionModeColor = "#EF4444";
        [ObservableProperty] private string _connectionModeBg = "#2E1214";
        [ObservableProperty] private string _connectionModeBorder = "#EF4444";
        [ObservableProperty] private string _connectionModeIcon = "🔴";
        [ObservableProperty] private string _connectionModeDetail = "Inicie o MSFS ou habilite o Modo Debug para testar";

        // Propriedades do Simulador (SimConnect)
        [ObservableProperty] private string _aircraftTitle = "DESCONHECIDO";
        [ObservableProperty] private double _fuelQuantity;
        [ObservableProperty] private double _fuelWeightLbs;
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

        partial void OnHangarAircraftLocationChanged(string value)
        {
            _ = RebuildItineraryAndValidateAsync();
        }

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

        // Gatekeeper Consolidated Summary Badge
        [ObservableProperty] private string _gatekeeperSummaryText = "4/4 SISTEMAS CONFORMES";
        [ObservableProperty] private string _gatekeeperSummaryIcon = "🟢";
        [ObservableProperty] private string _gatekeeperSummaryBadgeBg = "#0D2B1E";
        [ObservableProperty] private string _gatekeeperSummaryBadgeBorder = "#1B4D3E";
        [ObservableProperty] private string _gatekeeperSummaryTextColor = "#00E676";

        // Botão de Cópia W&B Contextual
        [ObservableProperty] private string _copyWandBButtonText = "📋 COPIAR W&B P/ O SIMULADOR";
        [ObservableProperty] private string _copyWandBButtonBg = "#1F2B37";
        [ObservableProperty] private string _copyWandBButtonBorder = "#00E5FF";
        [ObservableProperty] private string _copyWandBButtonColor = "#00E5FF";
        [ObservableProperty] private bool _isWandBAlreadySynced = false;

        // Envelope W&B Dedicado
        [ObservableProperty] private double _towPercentage;
        public double TowMarginLbs => Math.Max(0, HangarAircraftMtow - CalculatedTotalWeight);
        public string TowEnvelopeColor => TowPercentage > 100 ? "#EF4444" : (TowPercentage > 90 ? "#FFB300" : "#00E676");
        public string TowMarginDisplay => TowPercentage > 100 ? "⚠️ SOBREPESO" : $"{TowMarginLbs:N0} LBS LIVRES";
        public string TowMarginBadgeBg => TowPercentage > 100 ? "#2E1214" : "#0D2B1E";
        public string TowMarginBadgeBorder => TowPercentage > 100 ? "#4A1C20" : "#1B4D3E";

        private void UpdateTowEnvelope()
        {
            TowPercentage = HangarAircraftMtow > 0 ? Math.Min(100.0, Math.Round((CalculatedTotalWeight / HangarAircraftMtow) * 100.0, 1)) : 0;
            OnPropertyChanged(nameof(TowMarginLbs));
            OnPropertyChanged(nameof(TowEnvelopeColor));
            OnPropertyChanged(nameof(TowMarginDisplay));
            OnPropertyChanged(nameof(TowMarginBadgeBg));
            OnPropertyChanged(nameof(TowMarginBadgeBorder));
        }

        partial void OnCalculatedTotalWeightChanged(double value)
        {
            UpdateTowEnvelope();
        }

        partial void OnHangarAircraftMtowChanged(double value)
        {
            UpdateTowEnvelope();
        }

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
        public double TotalSeatsWeight => (TotalPaxCount + 1) * 170.0;
        public int TotalOccupantsCount => TotalPaxCount + 1;
        public string MsfsSeatsGuideDisplay => $"{TotalOccupantsCount} assentos ({TotalSeatsWeight:N0} lbs)";
        public string MsfsCargoGuideDisplay => $"{TotalCargoWeight:N0} lbs na Carga";
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
                OnPropertyChanged(nameof(TotalSeatsWeight));
                OnPropertyChanged(nameof(TotalOccupantsCount));
                OnPropertyChanged(nameof(MsfsSeatsGuideDisplay));
                OnPropertyChanged(nameof(MsfsCargoGuideDisplay));
                OnPropertyChanged(nameof(HasAcceptedMissions));
                _ = RebuildItineraryAndValidateAsync();
            };

            // Atualização contínua de telemetria
            _simConnect.OnDataReceived += (s, data) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    AircraftTitle = data.Title;
                    FuelQuantity = Math.Round(data.FuelTotalGallons, 1);
                    FuelWeightLbs = Math.Round(data.FuelWeightLbs, 1);
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
                    UpdateConnectionStatus();
                    ValidatePreFlightConditions();

                    if ((DateTime.Now - _lastLookup).TotalSeconds > 8)
                    {
                        _ = UpdateNearestAirportAsync(data.Latitude, data.Longitude);
                        _lastLookup = DateTime.Now;
                    }
                });
            };

            // Handlers para conexão / desconexão imediata
            _simConnect.OnConnected += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    UpdateConnectionStatus();
                    ValidatePreFlightConditions();
                });
            };

            _simConnect.OnDisconnected += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    UpdateConnectionStatus();
                    ValidatePreFlightConditions();
                });
            };

            // Iniciar com checklist padrão e status de conexão inicial
            InitializeChecklistItems();
            UpdateConnectionStatus();
            UpdateTowEnvelope();
        }

        public void UpdateConnectionStatus()
        {
            if (_simConnect.DebugMode)
            {
                ConnectionModeText = "MODO DEBUG / TESTE ATIVO";
                ConnectionModeColor = "#00E5FF";
                ConnectionModeBg = "#0B2533";
                ConnectionModeBorder = "#00E5FF";
                ConnectionModeIcon = "⚡";
                ConnectionModeDetail = "Telemetria artificial simulada via painel de Depuração";
            }
            else if (_simConnect.IsConnected)
            {
                ConnectionModeText = "MSFS 2020/2024 CONECTADO";
                ConnectionModeColor = "#00E676";
                ConnectionModeBg = "#0D2B1E";
                ConnectionModeBorder = "#00E676";
                ConnectionModeIcon = "🟢";
                ConnectionModeDetail = "SimConnect SDK sincronizado em tempo real";
            }
            else
            {
                ConnectionModeText = "SIMULADOR DESCONECTADO";
                ConnectionModeColor = "#EF4444";
                ConnectionModeBg = "#2E1214";
                ConnectionModeBorder = "#EF4444";
                ConnectionModeIcon = "🔴";
                ConnectionModeDetail = "Inicie o simulador MSFS ou ative o painel Debug";
            }
        }

        private void InitializeChecklistItems()
        {
            if (ChecklistItems.Count == 0)
            {
                ChecklistItems.Add(new PreFlightCheckItem { ItemIcon = "✈", ParameterName = "MODELO DA AERONAVE", SoftwareValue = "Aguardando Hangar...", SimStatusValue = "Aguardando Simulador...", IsOk = false });
                ChecklistItems.Add(new PreFlightCheckItem { ItemIcon = "📍", ParameterName = "LOCALIZAÇÃO / RAMPA", SoftwareValue = "Aguardando Base...", SimStatusValue = "Aguardando GPS...", IsOk = false });
                ChecklistItems.Add(new PreFlightCheckItem { ItemIcon = "👥", ParameterName = "PAYLOAD & PASSAGEIROS", SoftwareValue = "Aguardando Manifesto...", SimStatusValue = "Aguardando W&B...", IsOk = false });
                ChecklistItems.Add(new PreFlightCheckItem { ItemIcon = "⛽", ParameterName = "COMBUSTÍVEL NOS TANQUES", SoftwareValue = "Aguardando Dados...", SimStatusValue = "Aguardando Tanques...", IsOk = false });
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

                // Construção sequencial das pernas (Multi-Leg & TGL)
                var legDefinitions = new List<(string from, string to, bool isTgl, Mission? mission)>();

                // 1. Verificar se existem voos locais / TGL na base de partida (ex: SDTB -> SDTB)
                var localMissionsAtOrigin = AcceptedMissions
                    .Where(m => (m.OriginICAO == originIcao && m.DestinationICAO == originIcao) || (m.OriginICAO == originIcao && m.MissionCategoryType.Contains("Local", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var lm in localMissionsAtOrigin)
                {
                    legDefinitions.Add((originIcao, originIcao, true, lm));
                }

                // 2. Encadear pernas de travessia / navegação para outros aeroportos
                var remainingCrossMissions = AcceptedMissions
                    .Where(m => !localMissionsAtOrigin.Contains(m))
                    .ToList();

                string currentHop = originIcao;

                while (remainingCrossMissions.Any())
                {
                    // Encontrar missão que decola do aeroporto atual
                    var nextMission = remainingCrossMissions.FirstOrDefault(m => m.OriginICAO == currentHop && m.DestinationICAO != currentHop);

                    if (nextMission == null)
                    {
                        // Se não há missão saindo diretamente de currentHop, pegar a primeira disponível
                        nextMission = remainingCrossMissions.First();
                    }

                    string nextDest = nextMission.DestinationICAO;
                    legDefinitions.Add((currentHop, nextDest, false, nextMission));
                    remainingCrossMissions.RemoveAll(m => (m.OriginICAO == currentHop && m.DestinationICAO == nextDest));

                    currentHop = nextDest;
                }

                // Montar lista de pontos para o sumário de rota
                var summaryHops = new List<string>();
                double totalDistance = 0;
                int legIndex = 1;

                for (int i = 0; i < legDefinitions.Count; i++)
                {
                    var def = legDefinitions[i];
                    var aptOrigin = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == def.from);
                    var aptDest = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == def.to);

                    double dist = 40.0;
                    if (def.isTgl || def.from == def.to)
                    {
                        dist = (def.mission != null && def.mission.DistanceNM > 0) ? def.mission.DistanceNM : 10.0;
                    }
                    else if (aptOrigin != null && aptDest != null)
                    {
                        dist = Math.Round(Domain.Utils.LocationUtils.CalculateDistance(aptOrigin.Latitude, aptOrigin.Longitude, aptDest.Latitude, aptDest.Longitude), 0);
                        if (dist <= 0) dist = (def.mission != null && def.mission.DistanceNM > 0) ? def.mission.DistanceNM : 40.0;
                    }
                    else if (def.mission != null && def.mission.DistanceNM > 0)
                    {
                        dist = def.mission.DistanceNM;
                    }

                    totalDistance += dist;

                    // Formatar sumário da rota
                    if (i == 0)
                    {
                        summaryHops.Add(def.isTgl ? $"{def.from} (TGL)" : def.from);
                    }
                    if (!def.isTgl)
                    {
                        summaryHops.Add(def.to);
                    }

                    // Ações e carga nesta perna
                    string actionText;
                    int legPax;
                    double legCargo;

                    if (def.isTgl)
                    {
                        actionText = $"↳ Circuito de Tráfego: Toque e Arremetida (TGL) em {def.to}";
                        legPax = def.mission?.PassengerCount ?? 0;
                        legCargo = (def.mission?.CargoWeightLbs ?? 0) + (def.mission?.LuggageWeightLbs ?? 0);
                    }
                    else
                    {
                        // Entregas que acontecem especificamente neste destino
                        var deliveringMissions = AcceptedMissions.Where(m => m.DestinationICAO == def.to && !localMissionsAtOrigin.Contains(m)).ToList();
                        int deliveringPax = deliveringMissions.Sum(m => m.PassengerCount);
                        double deliveringCargo = deliveringMissions.Sum(m => m.CargoWeightLbs + m.LuggageWeightLbs);

                        legPax = AcceptedMissions.Where(m => !localMissionsAtOrigin.Contains(m)).Sum(m => m.PassengerCount);
                        legCargo = AcceptedMissions.Where(m => !localMissionsAtOrigin.Contains(m)).Sum(m => m.CargoWeightLbs + m.LuggageWeightLbs);

                        if (deliveringPax > 0 && deliveringCargo > 0)
                            actionText = $"↳ Desembarque: {deliveringPax} PAX e {deliveringCargo:N0} lbs carga em {def.to}";
                        else if (deliveringPax > 0)
                            actionText = $"↳ Desembarque: {deliveringPax} PAX em {def.to}";
                        else if (deliveringCargo > 0)
                            actionText = $"↳ Entrega: {deliveringCargo:N0} lbs de carga em {def.to}";
                        else
                            actionText = $"↳ Escala técnica / Trânsito em {def.to}";
                    }

                    double legPayload = (legPax * 170.0) + legCargo + 170.0; // +170 lbs Piloto

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
                        OriginICAO = def.from,
                        DestinationICAO = def.to,
                        OriginMsfsIdent = aptOrigin?.MsfsIdent ?? def.from,
                        DestinationMsfsIdent = aptDest?.MsfsIdent ?? def.to,
                        OriginLat = aptOrigin?.Latitude ?? 0,
                        OriginLon = aptOrigin?.Longitude ?? 0,
                        DestLat = aptDest?.Latitude ?? aptOrigin?.Latitude ?? 0,
                        DestLon = aptDest?.Longitude ?? aptOrigin?.Longitude ?? 0,
                        IsTglOrLocal = def.isTgl,
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
                }

                RouteSummaryDisplay = summaryHops.Any() ? string.Join(" ➔ ", summaryHops) : $"{originIcao} ➔ (SEM ROTA)";
                TotalRouteDistanceNM = totalDistance;

                double totalSpeed = HangarAircraftCruiseSpeed > 0 ? HangarAircraftCruiseSpeed : 110.0;
                double totalMin = Math.Round((totalDistance / totalSpeed) * 60.0);
                int th = (int)totalMin / 60;
                int tm = (int)totalMin % 60;
                TotalRouteEteFormatted = th > 0 ? $"{th}h {tm:D2}m" : $"{tm} min";

                // Atualizar destino padrão no mapa se houver primeira perna
                if (RouteLegs.Any())
                {
                    var nonTglLeg = RouteLegs.FirstOrDefault(l => !l.IsTglOrLocal) ?? RouteLegs.First();
                    _ = UpdateDestinationCoordinatesAsync(nonTglLeg.DestinationICAO);
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
                    _nearestAirportMsfsIdent = nearest.Airport.MsfsIdent ?? nearest.Airport.ICAO;
                    NearestAirportInfo = $"{nearest.Airport.DisplayIdent} - {nearest.Airport.Name}";
                }
                else
                {
                    _nearestAirportICAO = string.Empty;
                    _nearestAirportMsfsIdent = string.Empty;
                    NearestAirportInfo = "NENHUM AEROPORTO PRÓXIMO (< 5 NM)";
                }
                ValidatePreFlightConditions();
            }
            catch { NearestAirportInfo = "ERRO AO BUSCAR AEROPORTO"; }
        }

        public void Refresh()
        {
            UpdateConnectionStatus();
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
            UpdateConnectionStatus();

            // CASO 1: MONITORAMENTO EM ANDAMENTO (EM VOO / TÁXI)
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

                UpdateChecklistItem(0, "✈", "AERONAVE EM OPERAÇÃO",
                    HangarAircraftModel,
                    $"✓ Em operação no MSFS: {AircraftTitle}",
                    true,
                    "Aeronave em voo e telemetria registrada continuamente.",
                    "ATIVO");

                UpdateChecklistItem(1, "📍", "POSIÇÃO EM ROTA",
                    $"Partida: {HangarAircraftLocation} ➔ Destino: {FlightDestination}",
                    isAirborne ? $"✓ Em Voo • Altitude: {Altitude:N0} ft • GS: {GroundSpeed:N0} kt" : $"✓ Solo / Rampa em {NearestAirportInfo}",
                    true,
                    "Navegação por GPS e telemetria ativas.",
                    "A BORDO");

                UpdateChecklistItem(2, "👥", "PAYLOAD & MANIFESTO",
                    $"{TotalPaxCount} PAX • {TotalManifestWeight:N0} lbs despachados",
                    $"✓ Carga útil a bordo: {currentPayload:N0} lbs",
                    true,
                    "Manifesto de carga e passageiros embarcado com sucesso.",
                    "DESPACHADO");

                UpdateChecklistItem(3, "⛽", "COMBUSTÍVEL RESTANTE",
                    $"Início: {HangarAircraftFuel:F1} gal",
                    $"✓ Tanques: {FuelQuantity:F1} gal ({FuelQuantity * 6.0:N0} lbs)",
                    true,
                    "Consumo e fluxo de combustível monitorados em tempo real.",
                    "EM USO");

                return;
            }

            // CASO 2: DESPACHO PRÉ-VOO (RAMPA / SOLO)
            bool connectionOk = _simConnect.IsConnected;
            string modelOnly = (HangarAircraftModel ?? "").Split('(')[0].Trim();
            bool aircraftOk = connectionOk && IsAircraftMatching(AircraftTitle, HangarAircraftModel);

            string simLocation = (_nearestAirportICAO ?? "").Trim().ToUpper();
            string simMsfsIdent = (_nearestAirportMsfsIdent ?? "").Trim().ToUpper();
            string hangarLocation = (HangarAircraftLocation ?? "").Trim().ToUpper();
            bool locationOk = connectionOk && (!string.IsNullOrEmpty(simLocation) || !string.IsNullOrEmpty(simMsfsIdent)) &&
                (simLocation == hangarLocation || (!string.IsNullOrEmpty(simMsfsIdent) && simMsfsIdent == hangarLocation));

            double fuelDiff = FuelQuantity - HangarAircraftFuel;
            bool fuelOk = connectionOk && HangarAircraftFuel > 0 && (Math.Abs(fuelDiff) <= Math.Max(2.0, HangarAircraftFuel * 0.05));

            double plannedPayload = TotalManifestWeight;
            double fuelWeight = FuelWeightLbs > 0 ? FuelWeightLbs : (FuelQuantity * 6.0);
            double reportedSimPayload = Math.Max(0, TotalWeight - EmptyWeight - fuelWeight);
            double payloadDiff = reportedSimPayload - plannedPayload;
            bool payloadOk = connectionOk && (Math.Abs(payloadDiff) <= Math.Max(55.0, plannedPayload * 0.06));

            // 1. Aeronave
            string acftSoftware = string.IsNullOrWhiteSpace(HangarAircraftModel) || HangarAircraftModel == "---"
                ? "Nenhuma aeronave selecionada no Hangar"
                : HangarAircraftModel;
            string acftSim;
            string acftExplanation;
            if (!connectionOk)
            {
                acftSim = "Aguardando conexão com o simulador...";
                acftExplanation = "Inicie o Microsoft Flight Simulator ou ative o painel Debug para sincronizar.";
            }
            else if (aircraftOk)
            {
                acftSim = $"✓ Sincronizada no MSFS: {AircraftTitle}";
                acftExplanation = $"Aeronave '{modelOnly}' identificada e pronta para o despacho da missão.";
            }
            else
            {
                acftSim = $"✕ Detectada no MSFS: {(string.IsNullOrWhiteSpace(AircraftTitle) ? "Não identificada" : AircraftTitle)}";
                acftExplanation = $"⚠️ Selecione o modelo '{modelOnly}' no menu de aeronaves do MSFS antes de decolar.";
            }
            string acftTolerance = !connectionOk ? "AGUARDANDO" : (aircraftOk ? "✓ CORRESPONDENTE" : "✕ DIVERGENTE");
            UpdateChecklistItem(0, "✈", "MODELO DA AERONAVE", acftSoftware, acftSim, aircraftOk, acftExplanation, aircraftOk ? "OK" : "DIVERGENTE",
                cleanPlanned: modelOnly,
                cleanSim: string.IsNullOrWhiteSpace(AircraftTitle) ? "N/A" : AircraftTitle,
                toleranceBadge: acftTolerance);

            // 2. Localização / Posição
            string locSoftware = string.IsNullOrWhiteSpace(hangarLocation) || hangarLocation == "---"
                ? "Base de partida não definida"
                : $"Base de Saída: {hangarLocation}";
            string locSim;
            string locExplanation;
            if (!connectionOk)
            {
                locSim = "Aguardando GPS do simulador...";
                locExplanation = $"Aguardando link de telemetria para confirmar posicionamento em {hangarLocation}.";
            }
            else if (locationOk)
            {
                locSim = $"✓ Posicionada em: {simLocation} ({NearestAirportInfo})";
                locExplanation = $"Aeronave no aeroporto de partida correto ({hangarLocation}) para as missões aceitas.";
            }
            else
            {
                locSim = string.IsNullOrEmpty(simLocation)
                    ? "✕ Fora de alcance de aeródromos conhecidos"
                    : $"✕ Detectada em: {simLocation} (Esperado: {hangarLocation})";
                locExplanation = $"⚠️ Posicione sua aeronave no aeroporto {hangarLocation} no MSFS antes de iniciar o voo.";
            }
            string locTolerance = !connectionOk ? "AGUARDANDO" : (locationOk ? "✓ NA RAMPA" : "✕ LOCAL ERRADO");
            UpdateChecklistItem(1, "📍", "LOCALIZAÇÃO / RAMPA", locSoftware, locSim, locationOk, locExplanation, locationOk ? "OK" : "DIVERGENTE",
                cleanPlanned: hangarLocation,
                cleanSim: string.IsNullOrEmpty(simLocation) ? "GPS N/A" : simLocation,
                toleranceBadge: locTolerance);

            // 3. Payload & Passageiros
            string payloadSoftware = $"Manifesto: {TotalPaxCount} PAX • {plannedPayload:N0} lbs (Assentos: {TotalSeatsWeight:N0} lbs | Carga: {TotalCargoWeight:N0} lbs)";
            string payloadSim;
            string payloadExplanation;
            string payloadTolerance;
            if (!connectionOk)
            {
                payloadSim = "Aguardando telemetria de peso...";
                payloadExplanation = $"Carga útil exigida no manifesto: {plannedPayload:N0} lbs (Aba 'Assentos': {TotalSeatsWeight:N0} lbs para {TotalOccupantsCount} ocupantes | Aba 'Carga': {TotalCargoWeight:N0} lbs).";
                payloadTolerance = "AGUARDANDO";
            }
            else if (payloadOk)
            {
                payloadSim = $"✓ Carga útil no MSFS: {reportedSimPayload:N0} lbs";
                payloadExplanation = "Peso de passageiros e carga configurados conforme o manifesto de despacho.";
                payloadTolerance = Math.Abs(payloadDiff) < 1.0 ? "✓ 100% EXATO" : $"✓ ±{Math.Abs(payloadDiff):N0} lbs";
            }
            else
            {
                string diffStr = payloadDiff > 0 ? $"+{payloadDiff:N0} lbs (excesso)" : $"{payloadDiff:N0} lbs (falta)";
                payloadSim = $"✕ Carga útil no MSFS: {reportedSimPayload:N0} lbs ({diffStr})";
                payloadTolerance = payloadDiff > 0 ? $"+{payloadDiff:N0} lbs" : $"{payloadDiff:N0} lbs";

                if (payloadDiff < -120)
                {
                    int missingSeats = (int)Math.Round(Math.Abs(payloadDiff) / 170.0);
                    payloadExplanation = $"⚠️ Na aba 'Assentos' do MSFS, falta preencher {missingSeats} assento(s) ({missingSeats * 170} lbs)! Preencha o piloto (170 lbs) e o(s) passageiro(s) ({TotalPaxCount} PAX x 170 lbs) na aba Assentos ({TotalSeatsWeight:N0} lbs total), e a bagagem ({TotalCargoWeight:N0} lbs) na aba Carga. IMPORTANTE: No MSFS 2024, clique no botão azul 'CARREGAR A AERONAVE' para aplicar!";
                }
                else
                {
                    payloadExplanation = $"⚠️ Ajuste no MSFS: Aba 'Assentos' = {TotalSeatsWeight:N0} lbs ({TotalOccupantsCount} assentos preenchidos) e Aba 'Carga' = {TotalCargoWeight:N0} lbs. Total: {plannedPayload:N0} lbs. IMPORTANTE: No MSFS 2024, clique no botão azul 'CARREGAR A AERONAVE' para aplicar!";
                }
            }
            UpdateChecklistItem(2, "👥", "PAYLOAD & PASSAGEIROS", payloadSoftware, payloadSim, payloadOk, payloadExplanation, payloadOk ? "OK" : "DIVERGENTE",
                cleanPlanned: $"{plannedPayload:N0} lbs ({TotalPaxCount} PAX)",
                cleanSim: !connectionOk ? "--" : $"{reportedSimPayload:N0} lbs",
                toleranceBadge: payloadTolerance);

            // 4. Combustível nos Tanques
            string fuelSoftware = $"Combustível Solicitado: {HangarAircraftFuel:F1} gal ({HangarAircraftFuel * 6.0:N0} lbs)";
            string fuelSim;
            string fuelExplanation;
            string fuelTolerance;
            if (!connectionOk)
            {
                fuelSim = "Aguardando telemetria dos tanques...";
                fuelExplanation = $"Quantidade nos tanques do Hangar: {HangarAircraftFuel:F1} galões.";
                fuelTolerance = "AGUARDANDO";
            }
            else if (fuelOk)
            {
                fuelSim = $"✓ Abastecido no MSFS: {FuelQuantity:F1} gal ({FuelQuantity * 6.0:N0} lbs)";
                fuelExplanation = "Nível de combustível conferido e seguro para completar as pernas da rota.";
                fuelTolerance = Math.Abs(fuelDiff) < 0.1 ? "✓ 100% EXATO" : $"✓ ±{Math.Abs(fuelDiff):F1} gal";
            }
            else
            {
                string diffStr = fuelDiff > 0 ? $"+{fuelDiff:F1} gal (excesso)" : $"{fuelDiff:F1} gal (falta)";
                fuelSim = $"✕ Abastecido no MSFS: {FuelQuantity:F1} gal ({diffStr})";
                fuelTolerance = fuelDiff > 0 ? $"+{fuelDiff:F1} gal" : $"{fuelDiff:F1} gal";
                fuelExplanation = $"⚠️ Abasteça {HangarAircraftFuel:F1} galões nos tanques do MSFS ou abasteça a aeronave no Hangar.";
            }
            UpdateChecklistItem(3, "⛽", "COMBUSTÍVEL NOS TANQUES", fuelSoftware, fuelSim, fuelOk, fuelExplanation, fuelOk ? "OK" : "DIVERGENTE",
                cleanPlanned: $"{HangarAircraftFuel:F1} gal ({HangarAircraftFuel * 6.0:N0} lbs)",
                cleanSim: !connectionOk ? "--" : $"{FuelQuantity:F1} gal ({FuelQuantity * 6.0:N0} lbs)",
                toleranceBadge: fuelTolerance);

            bool allChecksPassed = connectionOk && aircraftOk && locationOk && fuelOk && payloadOk;
            IsGatekeeperAllOk = allChecksPassed;
            CanStartFlight = allChecksPassed;

            int passedCount = (aircraftOk ? 1 : 0) + (locationOk ? 1 : 0) + (payloadOk ? 1 : 0) + (fuelOk ? 1 : 0);
            if (!connectionOk)
            {
                GatekeeperSummaryText = "MSFS DESCONECTADO";
                GatekeeperSummaryIcon = "⚪";
                GatekeeperSummaryBadgeBg = "#1F2937";
                GatekeeperSummaryBadgeBorder = "#374151";
                GatekeeperSummaryTextColor = "#94A3B8";
            }
            else if (allChecksPassed)
            {
                GatekeeperSummaryText = "4/4 SISTEMAS CONFORMES";
                GatekeeperSummaryIcon = "🟢";
                GatekeeperSummaryBadgeBg = "#0D2B1E";
                GatekeeperSummaryBadgeBorder = "#1B4D3E";
                GatekeeperSummaryTextColor = "#00E676";
            }
            else
            {
                GatekeeperSummaryText = $"{passedCount}/4 SINCRONIZADOS";
                GatekeeperSummaryIcon = "⚠️";
                GatekeeperSummaryBadgeBg = "#2D220D";
                GatekeeperSummaryBadgeBorder = "#5C4408";
                GatekeeperSummaryTextColor = "#FFB300";
            }

            IsWandBAlreadySynced = payloadOk && fuelOk;
            if (IsWandBAlreadySynced)
            {
                CopyWandBButtonText = "✓ W&B JÁ SINCRONIZADO NO MSFS";
                CopyWandBButtonBg = "#0D2B1E";
                CopyWandBButtonBorder = "#1B4D3E";
                CopyWandBButtonColor = "#00E676";
            }
            else
            {
                CopyWandBButtonText = "📋 COPIAR W&B P/ O SIMULADOR";
                CopyWandBButtonBg = "#1F2B37";
                CopyWandBButtonBorder = "#00E5FF";
                CopyWandBButtonColor = "#00E5FF";
            }

            if (!connectionOk)
            {
                WeightStatusText = _simConnect.DebugMode ? "MODO DEBUG ATIVO" : "SIMULADOR DESCONECTADO";
                WeightStatusColor = "#EF4444";
                GatekeeperBlockedReason = "Voo bloqueado: O simulador MSFS não está conectado.";
            }
            else if (!aircraftOk)
            {
                WeightStatusText = $"AERONAVE DIVERGENTE ({AircraftTitle})";
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
                WeightStatusText = $"COMBUSTÍVEL DIVERGENTE ({FuelQuantity:F1} gal)";
                WeightStatusColor = "#FFB300";
                GatekeeperBlockedReason = $"Voo bloqueado: Ajuste o combustível nos tanques do MSFS para {HangarAircraftFuel:F1} galões.";
            }
            else if (!payloadOk)
            {
                WeightStatusText = $"PAYLOAD DIVERGENTE ({reportedSimPayload:F0} lbs)";
                WeightStatusColor = "#FFB300";
                GatekeeperBlockedReason = $"Voo bloqueado: Ajuste o Peso e Balanceamento (W&B) no MSFS para {plannedPayload:F0} lbs.";
            }
            else
            {
                WeightStatusText = "DESPACHO APROVADO • PRONTO PARA DECOLAR";
                WeightStatusColor = "#00E676";
                GatekeeperBlockedReason = "Todos os parâmetros coincidem perfeitamente. Voo liberado para início!";
            }
        }

        private void UpdateChecklistItem(int index, string icon, string paramName, string softwareVal, string simVal, bool isOk, string explanation, string diff, string cleanPlanned = "", string cleanSim = "", string toleranceBadge = "")
        {
            if (index < ChecklistItems.Count)
            {
                var item = ChecklistItems[index];
                item.ItemIcon = icon;
                item.ParameterName = paramName;
                item.SoftwareValue = softwareVal;
                item.SimStatusValue = simVal;
                item.IsOk = isOk;
                item.Explanation = explanation;
                item.DiffMessage = diff;
                item.PlannedValue = softwareVal;
                item.SimReportedValue = simVal;
                item.CleanPlanned = !string.IsNullOrWhiteSpace(cleanPlanned) ? cleanPlanned : softwareVal;
                item.CleanSim = !string.IsNullOrWhiteSpace(cleanSim) ? cleanSim : simVal;
                item.ToleranceBadge = toleranceBadge;
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

[COMO AJUSTAR NO MENU 'MASSA E EQUILÍBRIO' DO MSFS]
1. ABA 'ASSENTOS':
   • Piloto: 170 lbs
   • Passageiro(s): {TotalPaxCount} PAX ({TotalPaxCount * 170:N0} lbs)
   ↳ TOTAL NA ABA ASSENTOS: {TotalSeatsWeight:N0} lbs ({TotalOccupantsCount} assentos preenchidos)

2. ABA 'CARGA':
   • Bagagem / Compartimento de Carga: {TotalCargoWeight:N0} lbs

3. ABA 'COMBUSTÍVEL':
   • Combustível: {HangarAircraftFuel:F1} gal ({fuelLbs:N0} lbs)

------------------------------------------------
• PAYLOAD TOTAL (W&B): {TotalManifestWeight:N0} lbs
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
