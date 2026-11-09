using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Domain.Services;
using FlightCareer.Application.DTOs;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace FlightCareer.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IPilotRepository _pilotRepository;
        private readonly IMissionGenerator _missionGenerator;
        private readonly MarketViewModel _marketViewModel;
        private readonly HangarViewModel _hangarViewModel;
        private readonly BriefingViewModel _briefingViewModel;
        private readonly FinanceViewModel _financeViewModel;
        private readonly FlightLogsViewModel _flightLogsViewModel;
        private readonly Infrastructure.Persistence.AppDbContext _context;
        private readonly Services.SimConnectService _simConnect;
        private readonly Services.FlightMonitor _flightMonitor;
        private readonly Services.MetarService _metarService;
        private System.Timers.Timer? _metarRefreshTimer;
        private System.Timers.Timer? _missionLifecycleTimer;

        [ObservableProperty] private string _pilotName = string.Empty;
        [ObservableProperty] private string _pilotCompanyName = string.Empty;
        [ObservableProperty] private string _rank = string.Empty;
        [ObservableProperty] private double _totalHours;
        [ObservableProperty] private string _currentAirportICAO = string.Empty;
        [ObservableProperty] private string _currentAirportName = string.Empty;
        [ObservableProperty] private int _airportTier;
        [ObservableProperty] private double _balance;
        [ObservableProperty] private int _experiencePoints;
        [ObservableProperty] private Mission? _selectedMission;
        [ObservableProperty] private MissionDisplayItem? _selectedMissionItem;
        [ObservableProperty] private double _latitude;
        [ObservableProperty] private double _longitude;
        [ObservableProperty] private double _originLat;
        [ObservableProperty] private double _originLon;
        [ObservableProperty] private double _destLat;
        [ObservableProperty] private double _destLon;
        [ObservableProperty] private string _currentView = "Missions";
        [ObservableProperty] private Guid? _activeAircraftId;
        [ObservableProperty] private PilotCategory _pilotCategory = PilotCategory.Category1_FlightSchool;

        // Filtros e Busca da Tabela de Missões
        [ObservableProperty] private string _selectedCategoryFilter = "TODOS";
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private double _maxDistanceFilter = 1000.0;
        [ObservableProperty] private bool _filterOnlyCompatiblePayload = false;
        [ObservableProperty] private bool _hasFilteredMissions = true;
        [ObservableProperty] private int _selectedBatchCount = 0;

        private readonly List<MissionDisplayItem> _allMissionDisplayItems = new();
        public ObservableCollection<MissionDisplayItem> FilteredMissions { get; } = new();

        public string MaxDistanceDisplay => MaxDistanceFilter >= 990 ? "ILIMITADO" : $"{MaxDistanceFilter:N0} NM";

        public string CtaButtonText
        {
            get
            {
                if (SelectedBatchCount > 1)
                    return $"ACEITAR {SelectedBatchCount} CONTRATOS & INICIAR BRIEFING";
                if (SelectedBatchCount == 1)
                    return "ACEITAR 1 CONTRATO & INICIAR BRIEFING";
                if (SelectedMissionItem != null)
                    return "ACEITAR CONTRATO & INICIAR BRIEFING";
                return "SELECIONE UM CONTRATO";
            }
        }

        public bool CanAcceptMission => SelectedBatchCount > 0 || SelectedMissionItem != null;

        partial void OnSelectedCategoryFilterChanged(string value) => ApplyMissionFilters();
        partial void OnSearchQueryChanged(string value) => ApplyMissionFilters();
        partial void OnMaxDistanceFilterChanged(double value)
        {
            OnPropertyChanged(nameof(MaxDistanceDisplay));
            ApplyMissionFilters();
        }
        partial void OnFilterOnlyCompatiblePayloadChanged(bool value) => ApplyMissionFilters();

        partial void OnSelectedMissionItemChanged(MissionDisplayItem? value)
        {
            SelectedMission = value?.Mission;
            if (value != null)
            {
                DestLat = value.DestLat;
                DestLon = value.DestLon;

                if (!value.IsRealMetarLoaded)
                {
                    _ = Task.Run(async () =>
                    {
                        var metar = await _metarService.GetMetarAsync(value.DestinationICAO);
                        if (metar != null)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => value.ApplyRealMetar(metar));
                        }
                    });
                }
            }
            else
            {
                DestLat = 0;
                DestLon = 0;
            }
            OnPropertyChanged(nameof(SelectedMission));
            OnPropertyChanged(nameof(TotalPaxCount));
            OnPropertyChanged(nameof(TotalPaxWeight));
            OnPropertyChanged(nameof(TotalFlightWeight));
            OnPropertyChanged(nameof(CtaButtonText));
            OnPropertyChanged(nameof(CanAcceptMission));
        }

        public CompanyTier CompanyTier
        {
            get => (CompanyTier)(int)PilotCategory;
            set => PilotCategory = (PilotCategory)(int)value;
        }

        public string PilotCategoryDisplay => PilotCategory.GetDisplayName();
        public string PilotCategoryBadge => PilotCategory.GetShortName();
        public string PilotCategoryColor => PilotCategory.GetBadgeColor();

        // Aliases para compatibilidade XAML
        public string CompanyTierDisplay => PilotCategoryDisplay;
        public string CompanyTierBadge => PilotCategoryBadge;
        public string CompanyTierColor => PilotCategoryColor;
        
        partial void OnSelectedMissionChanged(Mission? value)
        {
            OnPropertyChanged(nameof(TotalPaxCount));
            OnPropertyChanged(nameof(TotalPaxWeight));
            OnPropertyChanged(nameof(TotalFlightWeight));

            if (value != null)
            {
                if (SelectedMissionItem?.Mission != value)
                {
                    _selectedMissionItem = FilteredMissions.FirstOrDefault(m => m.Mission == value);
                    OnPropertyChanged(nameof(SelectedMissionItem));
                }
                _ = UpdateMissionMapAsync(value);
            }
            else
            {
                OriginLat = 0;
                OriginLon = 0;
                DestLat = 0;
                DestLon = 0;
            }
        }

        private async Task UpdateMissionMapAsync(Mission mission)
        {
            try
            {
                // Buscar Origem
                var origin = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == mission.OriginICAO);
                if (origin != null)
                {
                    OriginLat = origin.Latitude;
                    OriginLon = origin.Longitude;
                }

                // Buscar Destino
                var dest = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == mission.DestinationICAO);
                if (dest != null)
                {
                    DestLat = dest.Latitude;
                    DestLon = dest.Longitude;
                }
                
                // Backup para posição da aeronave (Briefing)
                if (Latitude == 0 || Longitude == 0)
                {
                    var currentAirport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == CurrentAirportICAO);
                    if (currentAirport != null)
                    {
                        Latitude = currentAirport.Latitude;
                        Longitude = currentAirport.Longitude;
                    }
                }
            }
            catch { }
        }

        [ObservableProperty] private string _simStatusText = "SIMULADOR DESCONECTADO";
        [ObservableProperty] private string _simStatusColor = "#E74C3C";
        [ObservableProperty] private string _monitorStatus = "PRONTO PARA MISSÃO";
        [ObservableProperty] private bool _isMonitoring;

        // Propriedades para Detalhes da Missão Selecionada
        public int TotalPaxCount => (SelectedMission?.PassengerCount ?? 0) + 1; // +1 para o Piloto
        public double TotalPaxWeight => (SelectedMission?.PassengerCount ?? 0) * 170;
        public double TotalFlightWeight => (SelectedMission?.PayloadLbs ?? 0) + 170; // +170 lbs para o Piloto
        
        public string StartFlightButtonText => IsMonitoring ? "CANCELAR VOO" : "INICIAR VOO AGORA";
        public string StartFlightButtonColor => IsMonitoring ? "#EF4444" : "#2ECC71";

        public ObservableCollection<Mission> ActiveMissions { get; } = new();

        // Propriedades formatadas para a UI
        public MarketViewModel Market => _marketViewModel;
        public HangarViewModel Hangar => _hangarViewModel;
        public BriefingViewModel Briefing => _briefingViewModel;
        public FinanceViewModel Finance => _financeViewModel;
        public FlightLogsViewModel FlightLogs => _flightLogsViewModel;
        public string CashDisplay => Balance.ToString("C2");
        public string RankDisplay => Rank?.ToUpper() ?? "STUDENT";
        public double XpProgress => (TotalHours % 100);
        public string CurrentLocation => $"{CurrentAirportICAO} - {CurrentAirportName}";

        public ObservableCollection<Flight> RecentFlights { get; } = new();
        public ObservableCollection<Mission> AvailableMissions { get; } = new();

        public IAsyncRelayCommand RefreshMetarsCommand { get; }

        public MainViewModel(IPilotRepository pilotRepository, 
            IMissionGenerator missionGenerator, 
            MarketViewModel marketViewModel,
            HangarViewModel hangarViewModel,
            FlightLogsViewModel flightLogsViewModel,
            Infrastructure.Persistence.AppDbContext context,
            Services.SimConnectService simConnect,
            Services.FlightMonitor flightMonitor,
            Services.MetarService metarService)
        {
            _pilotRepository = pilotRepository;
            _missionGenerator = missionGenerator;
            _marketViewModel = marketViewModel;
            _hangarViewModel = hangarViewModel;
            _flightLogsViewModel = flightLogsViewModel;
            _context = context;
            _simConnect = simConnect;
            _flightMonitor = flightMonitor;
            _metarService = metarService;
            _briefingViewModel = new BriefingViewModel(_simConnect, ActiveMissions, _context);
            _financeViewModel = new FinanceViewModel(_context);

            RefreshMetarsCommand = new AsyncRelayCommand(FetchRealMetarsForMissionsAsync);

            // Timer para atualização periódica de METAR a cada 5 minutos
            _metarRefreshTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            _metarRefreshTimer.Elapsed += async (s, e) => await FetchRealMetarsForMissionsAsync();
            _metarRefreshTimer.AutoReset = true;
            _metarRefreshTimer.Start();

            // Timer para Mundo Vivo: Ciclo de Vida de Missões (TTL & Churn de Pilotos Virtuais NPC a cada 60s)
            _missionLifecycleTimer = new System.Timers.Timer(TimeSpan.FromSeconds(60).TotalMilliseconds);
            _missionLifecycleTimer.Elapsed += async (s, e) => await ProcessMissionLifecycleAndChurnAsync();
            _missionLifecycleTimer.AutoReset = true;
            _missionLifecycleTimer.Start();

            ShowMissionsCommand = new RelayCommand(() => CurrentView = "Missions");
            ShowBriefingCommand = new AsyncRelayCommand(async () => {
                await RefreshBriefingAircraftAsync();
                
                string prefix = "FC";
                if (!string.IsNullOrWhiteSpace(PilotCompanyName) && PilotCompanyName.Length >= 2)
                {
                    prefix = PilotCompanyName.Substring(0, 2).ToUpper();
                }
                else if (!string.IsNullOrWhiteSpace(PilotCompanyName))
                {
                    prefix = PilotCompanyName.Substring(0, 1).ToUpper() + "X";
                }

                _briefingViewModel.FlightNumber = $"{prefix}{new Random().Next(1000, 9999)}";
                await _briefingViewModel.RebuildItineraryAndValidateAsync();
                CurrentView = "Briefing";
            });
            ShowMarketCommand = new AsyncRelayCommand(async () => {
                await _marketViewModel.InitializeAsync(PilotName, Balance);
                CurrentView = "Market";
            });
            ShowHangarCommand = new AsyncRelayCommand(async () => {
                var pilots = await _pilotRepository.GetAllAsync();
                var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
                if (pilot != null)
                {
                    await _hangarViewModel.InitializeAsync(pilot.Id);
                    CurrentView = "Hangar";
                }
            });
            ShowFinanceCommand = new AsyncRelayCommand(async () => {
                var pilots = await _pilotRepository.GetAllAsync();
                var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
                if (pilot != null)
                {
                    await _financeViewModel.InitializeAsync(pilot.Id);
                    CurrentView = "Finance";
                }
            });
            ShowFlightLogsCommand = new AsyncRelayCommand(async () => {
                await _flightLogsViewModel.LoadLogsAsync();
                CurrentView = "FlightLogs";
            });

            AcceptSelectedMissionsCommand = new AsyncRelayCommand(AcceptSelectedMissionsAsync);
            AcceptMissionCommand = AcceptSelectedMissionsCommand;
            SetCategoryFilterCommand = new RelayCommand<string>(SetCategoryFilter);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            ToggleSelectAllBatchCommand = new RelayCommand(ToggleSelectAllBatch);

            RemoveMissionCommand = new RelayCommand<Mission>(RemoveMission);
            StartFlightCommand = new AsyncRelayCommand(async () => {
                if (IsMonitoring) await CancelFlightAsync();
                else StartFlight();
            });
            ShowDebugCommand = new RelayCommand(ShowDebug);
            ShowOverlayCommand = new RelayCommand(ShowOverlay);
            PromoteCompanyTierCommand = new AsyncRelayCommand(PromoteCompanyTierAsync);
            OpenFerryWindowCommand = new RelayCommand(OpenFerryWindow);
            RefreshMarketMissionsCommand = new AsyncRelayCommand(RefreshMarketMissionsAsync);

            _simConnect.OnConnected += (s, e) => {
                SimStatusText = "SIMULADOR CONECTADO";
                SimStatusColor = "#2ECC71";
            };
            _simConnect.OnDisconnected += (s, e) => {
                SimStatusText = "SIMULADOR DESCONECTADO";
                SimStatusColor = "#E74C3C";
            };

            _simConnect.OnDataReceived += (s, data) => {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    Latitude = data.Latitude;
                    Longitude = data.Longitude;
                });
            };

            _flightMonitor.OnStatusChanged += (msg) => {
                MonitorStatus = msg;
                IsMonitoring = _flightMonitor.IsMonitoring;
                _briefingViewModel.IsMonitoring = IsMonitoring;
                _hangarViewModel.IsMonitoring = IsMonitoring;
                OnPropertyChanged(nameof(StartFlightButtonText));
                OnPropertyChanged(nameof(StartFlightButtonColor));
            };
            _flightMonitor.OnAlert += (title, msg, isError) => {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    System.Windows.MessageBox.Show(msg, title, System.Windows.MessageBoxButton.OK, isError ? System.Windows.MessageBoxImage.Error : System.Windows.MessageBoxImage.Information);
                });
            };
            _flightMonitor.OnFlightFinished += OnFlightFinished;
            _flightMonitor.OnTotalLoss += OnTotalLoss;
            _flightMonitor.OnMonitoringCancelled += OnMonitoringCancelled;
        }

        private void OnMonitoringCancelled()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () => {
                IsMonitoring = false;
                _briefingViewModel.IsMonitoring = false;
                _hangarViewModel.IsMonitoring = false;
                OnPropertyChanged(nameof(StartFlightButtonText));
                OnPropertyChanged(nameof(StartFlightButtonColor));

                await InitializeForPilotAsync(PilotName);
            });
        }

        private void OnTotalLoss(PilotAircraft aircraft)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () => {
                IsMonitoring = false;
                _briefingViewModel.IsMonitoring = false;
                _hangarViewModel.IsMonitoring = false;
                OnPropertyChanged(nameof(StartFlightButtonText));
                OnPropertyChanged(nameof(StartFlightButtonColor));

                System.Windows.MessageBox.Show($"PERDA TOTAL! A aeronave {aircraft.Prefix} sofreu danos catastróficos e foi destruída.\n\nVerifique seu hangar para acionar o seguro.", "Crash - Perda Total", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                
                await InitializeForPilotAsync(PilotName);
            });
        }

        public IRelayCommand ShowMissionsCommand { get; }
        public IRelayCommand ShowBriefingCommand { get; }
        public IRelayCommand ShowMarketCommand { get; }
        public IRelayCommand ShowHangarCommand { get; }
        public IRelayCommand ShowFinanceCommand { get; }
        public IRelayCommand ShowFlightLogsCommand { get; }
        public IRelayCommand AcceptMissionCommand { get; }
        public IAsyncRelayCommand AcceptSelectedMissionsCommand { get; }
        public IRelayCommand<string> SetCategoryFilterCommand { get; }
        public IRelayCommand ResetFiltersCommand { get; }
        public IRelayCommand ToggleSelectAllBatchCommand { get; }
        public IRelayCommand RemoveMissionCommand { get; }
        public IAsyncRelayCommand StartFlightCommand { get; }
        public IRelayCommand ShowDebugCommand { get; }
        public IRelayCommand ShowOverlayCommand { get; }
        public IAsyncRelayCommand PromoteCompanyTierCommand { get; }
        public IAsyncRelayCommand PromotePilotCategoryCommand => PromoteCompanyTierCommand;
        public IRelayCommand OpenFerryWindowCommand { get; }
        public IAsyncRelayCommand RefreshMarketMissionsCommand { get; }

        public async Task RefreshMarketMissionsAsync()
        {
            try
            {
                var pilots = await _pilotRepository.GetAllAsync();
                var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
                if (pilot == null) return;

                // 1. Remover ofertas não aceitas do aeroporto atual
                var currentAirportIcao = pilot.CurrentAirportICAO;
                var currentMktMissions = await _context.Missions
                    .Where(m => m.OriginICAO == currentAirportIcao && !m.IsAccepted)
                    .ToListAsync();

                if (currentMktMissions.Any())
                {
                    _context.Missions.RemoveRange(currentMktMissions);
                    await _context.SaveChangesAsync();
                }

                // 2. Recarregar e gerar com o novo volume e novas regras
                await InitializeForPilotAsync(pilot.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Erro ao atualizar mercado: {ex.Message}");
            }
        }

        private async Task ProcessMissionLifecycleAndChurnAsync()
        {
            if (string.IsNullOrEmpty(PilotName) || string.IsNullOrEmpty(CurrentAirportICAO) || _flightMonitor.IsFlightActive) return;

            try
            {
                // Mundo Vivo: executa churn, expiração de TTL e reposição caso estejamos na tela de missões
                if (CurrentView == "Missions")
                {
                    await InitializeForPilotAsync(PilotName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MissionLifecycle] Erro no ciclo de vida e churn: {ex.Message}");
            }
        }

        private void OpenFerryWindow()
        {
            var pilots = _pilotRepository.GetAllAsync().GetAwaiter().GetResult();
            var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
            if (pilot == null || pilot.ActiveAircraftId == null)
            {
                System.Windows.MessageBox.Show("Você precisa ter uma aeronave ativa selecionada no Hangar para planejar um translado.", "Aeronave Necessária", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var vm = new FerryFlightViewModel(
                _context,
                _pilotRepository,
                PilotName,
                async () =>
                {
                    await InitializeForPilotAsync(PilotName);
                    CurrentView = "Briefing";
                });

            var window = new Views.FerryFlightWindow
            {
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        private void ShowDebug()
        {
            var debugWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Views.DebugWindow>(((App)System.Windows.Application.Current).ServiceProvider);
            debugWindow.Show();
        }

        private void ShowOverlay()
        {
            var overlayWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Views.DebugOverlayWindow>(((App)System.Windows.Application.Current).ServiceProvider);
            overlayWindow.Show();
            overlayWindow.Activate();
        }

        private async Task RefreshBriefingAircraftAsync()
        {
            var pilot = await _context.Pilots
                .Include(p => p.Hangar)
                .ThenInclude(a => a.AircraftType)
                .FirstOrDefaultAsync(p => p.Name == PilotName);

            if (pilot != null && pilot.ActiveAircraftId != null)
            {
                var active = pilot.Hangar.FirstOrDefault(a => a.Id == pilot.ActiveAircraftId);
                if (active != null)
                {
                    _briefingViewModel.HangarAircraftModel = $"{active.AircraftType?.Model} ({active.Prefix})";
                    _briefingViewModel.HangarAircraftPrefix = active.Prefix;
                    _briefingViewModel.HangarAircraftFuel = active.FuelAmount;
                    _briefingViewModel.HangarAircraftLocation = active.CurrentLocationICAO;
                    _briefingViewModel.HangarAircraftMtow = (active.AircraftType?.MaxPayloadLbs ?? 500) + 2000.0;
                    _briefingViewModel.HangarAircraftCruiseSpeed = active.AircraftType?.CruiseSpeedKts ?? 110.0;
                }
            }
            else
            {
                _briefingViewModel.HangarAircraftModel = "NENHUMA ATIVA";
                _briefingViewModel.HangarAircraftPrefix = "---";
                _briefingViewModel.HangarAircraftFuel = 0;
                _briefingViewModel.HangarAircraftLocation = "---";
                _briefingViewModel.HangarAircraftMtow = 2500.0;
                _briefingViewModel.HangarAircraftCruiseSpeed = 110.0;
            }
            
            await _briefingViewModel.RebuildItineraryAndValidateAsync();
        }

        private async void RemoveMission(Mission? mission)
        {
            if (mission == null) return;

            if (ActiveMissions.Contains(mission))
            {
                // Verificar se a carga foi movida
                if (mission.OriginICAO != CurrentAirportICAO)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"ATENÇÃO: Esta carga foi transportada de {mission.OriginICAO} até {CurrentAirportICAO}.\n\n" +
                        "Se você descarregar agora, você NÃO receberá o pagamento por este transporte e a missão voltará ao mercado local com valor atualizado.\n\n" +
                        "Deseja realmente descarregar e cancelar?", 
                        "Confirmar Descarregamento", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Warning);

                    if (result != System.Windows.MessageBoxResult.Yes) return;
                }
                else
                {
                    var result = System.Windows.MessageBox.Show(
                        "Deseja remover esta missão do seu manifesto?", 
                        "Remover Missão", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Question);

                    if (result != System.Windows.MessageBoxResult.Yes) return;
                }

                ActiveMissions.Remove(mission);
                mission.IsAccepted = false;
                
                // Persistência: A carga agora está no aeroporto atual
                mission.OriginICAO = CurrentAirportICAO;

                // Recalcular distância e recompensa do novo ponto de origem até o destino
                var origin = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == mission.OriginICAO);
                var dest = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == mission.DestinationICAO);

                if (origin != null && dest != null)
                {
                    double newDist = Domain.Utils.LocationUtils.CalculateDistance(origin.Latitude, origin.Longitude, dest.Latitude, dest.Longitude);
                    mission.DistanceNM = Math.Round(newDist, 0);
                    
                    // Recalcular usando a lógica do gerador
                    if (_missionGenerator is MissionGenerator mg)
                    {
                        mission.RewardMoney = mg.CalculateReward(
                            newDist, 
                            mission.PayloadLbs, 
                            mission.Urgency, 
                            mission.Title, 
                            mission.PassengerCount, 
                            origin.Tier, 
                            dest.Tier);
                        mission.ExperienceGain = (int)(newDist * 0.5 * mission.Urgency);
                    }
                }

                AvailableMissions.Add(mission);
                await _context.SaveChangesAsync();

                try
                {
                    var pilots = await _pilotRepository.GetAllAsync();
                    var p = pilots.FirstOrDefault(x => x.Name == PilotName);
                    var activeAircraft = p?.Hangar.FirstOrDefault(a => a.Id == p.ActiveAircraftId);
                    var pCat = p?.PilotCategory ?? PilotCategory.Category1_FlightSchool;
                    var originAirport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == mission.OriginICAO);
                    var destAirport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == mission.DestinationICAO);
                    var item = new MissionDisplayItem(mission, destAirport, activeAircraft, pCat, originAirport);
                    item.BatchSelectionChanged += (s, sel) => UpdateBatchCount();
                    _allMissionDisplayItems.Add(item);
                    ApplyMissionFilters();
                }
                catch { }
                
                System.Windows.MessageBox.Show($"Missão para {mission.DestinationICAO} removida. A carga foi deixada em {CurrentAirportICAO} e a recompensa foi atualizada.");
            }
        }

        public void ApplyMissionFilters()
        {
            var query = _allMissionDisplayItems.AsEnumerable();

            // 1. Categoria
            if (!string.IsNullOrWhiteSpace(SelectedCategoryFilter) && SelectedCategoryFilter != "TODOS")
            {
                query = query.Where(m =>
                {
                    if (SelectedCategoryFilter == "TÁXI AÉREO")
                        return m.MissionCategoryType.Contains("Táxi", StringComparison.OrdinalIgnoreCase) || m.MissionCategoryType.Contains("Charter", StringComparison.OrdinalIgnoreCase);
                    if (SelectedCategoryFilter == "LINHA REGIONAL")
                        return m.MissionCategoryType.Contains("Linha Regional", StringComparison.OrdinalIgnoreCase);
                    if (SelectedCategoryFilter == "LINHA REGULAR")
                        return m.MissionCategoryType.Contains("Linha Regular", StringComparison.OrdinalIgnoreCase) || m.MissionCategoryType.Contains("Heavy", StringComparison.OrdinalIgnoreCase);
                    if (SelectedCategoryFilter == "CARGA / SAE")
                        return m.MissionCategoryType.Contains("Carga", StringComparison.OrdinalIgnoreCase) || m.MissionCategoryType.Contains("SAE", StringComparison.OrdinalIgnoreCase);
                    if (SelectedCategoryFilter == "INSTRUÇÃO")
                        return m.MissionCategoryType.Contains("Instrução", StringComparison.OrdinalIgnoreCase) || m.MissionCategoryType.Contains("Treinamento", StringComparison.OrdinalIgnoreCase);
                    if (SelectedCategoryFilter == "MÉDICA / ESPECIAL")
                        return m.MissionCategoryType.Contains("Saúde", StringComparison.OrdinalIgnoreCase) || m.MissionCategoryType.Contains("Remoção", StringComparison.OrdinalIgnoreCase) || m.MissionCategoryType.Contains("Médica", StringComparison.OrdinalIgnoreCase);
                    return true;
                });
            }

            // 2. Busca Rápida (ICAO ou Cidade)
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var term = SearchQuery.Trim();
                query = query.Where(m =>
                    m.DestinationICAO.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    m.DestinationCity.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    m.DestinationName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            // 3. Raio de Distância
            if (MaxDistanceFilter < 990)
            {
                query = query.Where(m => m.DistanceNM <= MaxDistanceFilter);
            }

            // 4. Carga Compatível
            if (FilterOnlyCompatiblePayload)
            {
                query = query.Where(m => !m.IsOverweight);
            }

            var list = query.ToList();

            FilteredMissions.Clear();
            foreach (var item in list)
            {
                FilteredMissions.Add(item);
            }

            HasFilteredMissions = FilteredMissions.Count > 0;

            if (SelectedMissionItem == null || !FilteredMissions.Contains(SelectedMissionItem))
            {
                SelectedMissionItem = FilteredMissions.FirstOrDefault();
            }

            UpdateBatchCount();
        }

        public void UpdateBatchCount()
        {
            SelectedBatchCount = FilteredMissions.Count(m => m.IsSelectedForBatch);
            OnPropertyChanged(nameof(CtaButtonText));
            OnPropertyChanged(nameof(CanAcceptMission));
        }

        public void SetCategoryFilter(string? category)
        {
            SelectedCategoryFilter = category ?? "TODOS";
        }

        public void ResetFilters()
        {
            SelectedCategoryFilter = "TODOS";
            SearchQuery = string.Empty;
            MaxDistanceFilter = 1000.0;
            FilterOnlyCompatiblePayload = false;
        }

        public void ToggleSelectAllBatch()
        {
            bool anyUnselected = FilteredMissions.Any(m => !m.IsSelectedForBatch);
            foreach (var m in FilteredMissions)
            {
                m.IsSelectedForBatch = anyUnselected;
            }
            UpdateBatchCount();
        }

        public async Task AcceptSelectedMissionsAsync()
        {
            var pilots = await _pilotRepository.GetAllAsync();
            var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
            if (pilot == null) return;

            var itemsToAccept = FilteredMissions.Where(m => m.IsSelectedForBatch).ToList();
            if (itemsToAccept.Count == 0 && SelectedMissionItem != null)
            {
                itemsToAccept.Add(SelectedMissionItem);
            }

            if (itemsToAccept.Count == 0) return;

            // Checagem de Homologação
            var blocked = itemsToAccept.Where(i => i.Mission.RequiredCategory > pilot.PilotCategory).ToList();
            if (blocked.Count > 0)
            {
                var firstBlocked = blocked.First();
                System.Windows.MessageBox.Show(
                    $"Sua empresa ({pilot.PilotCategory.GetDisplayName()}) ainda não possui homologação para realizar contratos do tipo '{firstBlocked.Mission.RequiredCategory.GetDisplayName()}'.\n\nEvolua a categoria da sua empresa para desbloquear esta operação.",
                    "Contrato Bloqueado por Homologação",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Checagem de Compatibilidade da Aeronave Ativa (Prevenir absurdos operacionais: Cessna 152 com 50 pax ou Boeing 737 em fazenda)
            var activeAircraft = pilot.Hangar.FirstOrDefault(a => a.Id == pilot.ActiveAircraftId);
            if (activeAircraft?.AircraftType != null)
            {
                var type = activeAircraft.AircraftType;
                double totalBatchPayload = itemsToAccept.Sum(m => m.PayloadLbs);
                int totalBatchPax = itemsToAccept.Sum(m => m.PassengerCount);

                if (totalBatchPayload > type.MaxPayloadLbs || totalBatchPax > type.MaxPax)
                {
                    var capacityWarning = System.Windows.MessageBox.Show(
                        $"Aviso de Capacidade Operacional:\n\n" +
                        $"Sua aeronave ativa ({type.Model}) possui capacidade máxima de {type.MaxPayloadLbs:N0} LBS e {type.MaxPax} PAX.\n" +
                        $"O conjunto selecionado totaliza {totalBatchPayload:N0} LBS e {totalBatchPax} PAX.\n\n" +
                        $"Deseja aceitar mesmo assim (ex: para realizar em múltiplos voos ou trocar de aeronave no Hangar)?",
                        "Alerta de Capacidade da Aeronave",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (capacityWarning != System.Windows.MessageBoxResult.Yes) return;
                }
            }

            // Modal de Confirmação com detalhes
            string title = itemsToAccept.Count == 1 ? "Confirmar Contrato de Voo" : "Confirmar Contratos em Lote";
            string message = itemsToAccept.Count == 1
                ? $"Destino: {itemsToAccept[0].DestinationICAO} ({itemsToAccept[0].DestinationCity})\nDistância: {itemsToAccept[0].DistanceNM:N0} NM\nPeso: {itemsToAccept[0].PayloadLbs:N0} LBS\nRecompensa: {itemsToAccept[0].RewardMoney:C0}\n\nDeseja aceitar o contrato e prosseguir para o Briefing?"
                : $"Você selecionou {itemsToAccept.Count} contratos.\nPeso Total: {itemsToAccept.Sum(m => m.PayloadLbs):N0} LBS\nReceita Estimada: {itemsToAccept.Sum(m => m.RewardMoney):C0}\n\nDeseja aceitar todos os contratos selecionados e prosseguir para o Briefing?";

            var confirmResult = System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (confirmResult != System.Windows.MessageBoxResult.Yes) return;

            foreach (var item in itemsToAccept)
            {
                var mission = item.Mission;
                if (!ActiveMissions.Contains(mission))
                {
                    ActiveMissions.Add(mission);
                    AvailableMissions.Remove(mission);
                    _allMissionDisplayItems.Remove(item);
                    mission.IsAccepted = true;
                    mission.PilotId = pilot.Id;
                }
            }

            SelectedMissionItem = null;
            SelectedMission = null;

            await _context.SaveChangesAsync();
            ApplyMissionFilters();

            // Redireciona e prepara o Briefing
            ShowBriefingCommand.Execute(null);
        }

        private async void AcceptMission()
        {
            await AcceptSelectedMissionsAsync();
        }

        private async void StartFlight()
        {
            var pilots = await _pilotRepository.GetAllAsync();
            var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
            if (pilot == null || pilot.ActiveAircraftId == null)
            {
                System.Windows.MessageBox.Show("Você precisa de uma aeronave ativa para voar! Verifique no Hangar.");
                return;
            }

            var aircraft = pilot.Hangar.FirstOrDefault(a => a.Id == pilot.ActiveAircraftId);
            if (aircraft == null) return;

            if (aircraft.IsInterdicted)
            {
                System.Windows.MessageBox.Show($"Voo não autorizado! A aeronave {aircraft.Prefix} está INTERDITADA.\n\nMotivo: {aircraft.InterdictionReason}\n\nRealize a manutenção regulatória obrigatória no Hangar antes da decolagem.", "Aeronave Interditada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var prohibitedMission = ActiveMissions.FirstOrDefault(m => m.RequiredCategory > pilot.PilotCategory);
            if (prohibitedMission != null)
            {
                System.Windows.MessageBox.Show($"Operação não homologada!\nA missão '{prohibitedMission.Title}' exige homologação {prohibitedMission.RequiredCategory.GetDisplayName()}, mas sua empresa opera em {pilot.PilotCategory.GetDisplayName()}.\n\nRemova a missão do manifesto ou obtenha a certificação necessária.", "Homologação Regulatória Insuficiente", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (ActiveMissions.Any(m => m.PassengerCount > 0) && aircraft.Configuration.Contains("Carga"))
            {
                System.Windows.MessageBox.Show($"A aeronave {aircraft.Prefix} está configurada para CARGA (sem assentos de passageiros).\n\nPara transportar passageiros, alterne a configuração da cabine para Passageiro no Hangar.", "Configuração Incompatível", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            _flightMonitor.StartMonitoring(pilot, aircraft, ActiveMissions.ToList());
            IsMonitoring = true;
            _briefingViewModel.IsMonitoring = true;
            _hangarViewModel.IsMonitoring = true;
            OnPropertyChanged(nameof(StartFlightButtonText));
            OnPropertyChanged(nameof(StartFlightButtonColor));
        }

        private async Task PromoteCompanyTierAsync()
        {
            var pilots = await _pilotRepository.GetAllAsync();
            var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
            if (pilot == null) return;

            if (PilotCategory >= PilotCategory.Category5_MajorAirline)
            {
                System.Windows.MessageBox.Show("Parabéns! Sua companhia já atingiu a homologação máxima da aviação: Categoria 5 (Linha Aérea Regular)!", "Nível Máximo Atingido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var nextCategory = (PilotCategory)((int)PilotCategory + 1);
            double cost = PilotCategory.GetPromotionCost();
            double reqHours = PilotCategory.GetRequiredHours();
            int reqXP = PilotCategory.GetRequiredXP();

            string reqDesc = nextCategory switch
            {
                PilotCategory.Category2_SAE => "- Carteira de Piloto Privado (40+ Horas de Voo)\n- Manual de Serviços Especializados e Inspeção ANAC/FAA",
                PilotCategory.Category3_AirTaxi => "- Licença de Piloto Comercial (PC) + IFR (150+ Horas)\n- Homologação de Táxi Aéreo, Gestor SGSO e Mecânico Responsável (DTS)",
                PilotCategory.Category4_Regional => "- Habilitação Multimotor (MLTE) + IFR (500+ Horas)\n- Infraestrutura de Rampa/Ground Handling e Sistema DOV",
                PilotCategory.Category5_MajorAirline => "- Licença de Piloto de Linha Aérea (PLA) (1500+ Horas)\n- Slots em Hubs Principais, Auditoria IOSA e Homologação Regular",
                _ => ""
            };

            bool hoursOk = TotalHours >= reqHours;
            bool xpOk = pilot.ExperiencePoints >= reqXP;
            bool balanceOk = Balance >= cost;
            bool canPromote = hoursOk && xpOk && balanceOk;

            string statusMsg = $"=== CERTIFICAÇÃO REGULATÓRIA DA EMPRESA ===\n\n" +
                               $"Homologação Atual: {PilotCategory.GetDisplayName()}\n" +
                               $"Próxima Categoria: {nextCategory.GetDisplayName()}\n\n" +
                               $"REQUISITOS OPERACIONAIS:\n{reqDesc}\n\n" +
                               $"TRÍADE DE PROGRESSÃO:\n" +
                               $"• Horas de Voo: {TotalHours:F1}h / {reqHours:F0}h {(hoursOk ? "[OK]" : "[PENDENTE]")}\n" +
                               $"• Experiência (XP): {pilot.ExperiencePoints:N0} / {reqXP:N0} XP {(xpOk ? "[OK]" : "[PENDENTE]")}\n" +
                               $"• Taxa de Homologação: {cost:C2} (Saldo: {Balance:C2}) {(balanceOk ? "[OK]" : "[PENDENTE]")}\n\n";

            if (!canPromote)
            {
                statusMsg += "SUA EMPRESA AINDA NÃO CUMPRE TODOS OS REQUISITOS:\n";
                if (!hoursOk) statusMsg += $"- Faltam {(reqHours - TotalHours):F1} horas de voo registradas no manche.\n";
                if (!xpOk) statusMsg += $"- Faltam {(reqXP - pilot.ExperiencePoints):N0} pontos de XP. Realize voos com pousos suaves e sem infrações para elevar sua pontuação de proficiência.\n";
                if (!balanceOk) statusMsg += $"- Capital insuficiente. Faltam {(cost - Balance):C2} em caixa para pagar a homologação regulatória.\n";

                System.Windows.MessageBox.Show(statusMsg, "Progresso de Certificação", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            statusMsg += "Todos os requisitos regulatórios, horas e pontuação de XP foram atingidos!\n\nDeseja pagar a taxa e homologar sua empresa para a nova Categoria agora?";
            var result = System.Windows.MessageBox.Show(statusMsg, "Evolução de Categoria Operacional", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            pilot.Balance -= cost;
            pilot.PilotCategory = nextCategory;

            _context.FinancialTransactions.Add(new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                PilotId = pilot.Id,
                Date = DateTime.Now,
                Category = "Certificação",
                Description = $"HOMOLOGAÇÃO REGULATÓRIA: {nextCategory.GetDisplayName()}",
                Amount = cost,
                Type = TransactionType.Expense
            });

            await _context.SaveChangesAsync();
            await InitializeForPilotAsync(pilot.Name);

            System.Windows.MessageBox.Show($"HOMOLOGAÇÃO APROVADA!\n\nSua empresa agora opera oficialmente sob: {nextCategory.GetDisplayName()}.\nNovas missões e operações comerciais foram desbloqueadas!", "Certificação Concluída", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private async Task CancelFlightAsync()
        {
            var result = System.Windows.MessageBox.Show(
                "ATENÇÃO: Você deseja CANCELAR o monitoramento deste voo?\n\n" +
                "Caso você já tenha iniciado a decolagem ou movido a aeronave, sua localização atual, combustível e desgaste serão SALVOS no banco de dados.", 
                "Confirmar Cancelamento", 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            if (_simConnect.IsConnected)
            {
                await _flightMonitor.CancelMonitoringAsync(_simConnect.CurrentData);
            }
            else
            {
                _flightMonitor.StopMonitoring();
            }

            IsMonitoring = false;
            _briefingViewModel.IsMonitoring = false;
            _hangarViewModel.IsMonitoring = false;
            OnPropertyChanged(nameof(StartFlightButtonText));
            OnPropertyChanged(nameof(StartFlightButtonColor));
            
            // Recarregar dados do piloto caso a localização tenha mudado
            await InitializeForPilotAsync(PilotName);
        }

        private void OnFlightFinished(FlightOutcome outcome)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () => {
                IsMonitoring = false;
                _briefingViewModel.IsMonitoring = false;
                _hangarViewModel.IsMonitoring = false;
                OnPropertyChanged(nameof(StartFlightButtonText));
                OnPropertyChanged(nameof(StartFlightButtonColor));

                if (outcome.Success)
                {
                    System.Windows.MessageBox.Show($"Voo concluído com sucesso!\n\nRecompensa: {outcome.Revenue:C2}\nXP: {outcome.XP}\nMissões: {outcome.CompletedMissions}", "Debriefing");
                    
                    // Recarregar tudo do banco (isso vai atualizar as missões ativas restantes)
                    await InitializeForPilotAsync(PilotName);
                }
                else
                {
                    System.Windows.MessageBox.Show($"O voo não foi registrado: {outcome.Message}", "Aviso");
                }
            });
        }

        public async Task InitializeForPilotAsync(string pilotName)
        {
            var pilot = await _context.Pilots
                .Include(p => p.Hangar)
                .ThenInclude(a => a.AircraftType)
                .FirstOrDefaultAsync(p => p.Name == pilotName);
                
            if (pilot == null) return;

            PilotName = pilot.Name;
            PilotCompanyName = pilot.CompanyName;
            Rank = pilot.Rank;
            TotalHours = pilot.TotalHours;
            Balance = pilot.Balance;
            ExperiencePoints = pilot.ExperiencePoints;
            ActiveAircraftId = pilot.ActiveAircraftId;
            PilotCategory = pilot.PilotCategory;

            // REGRA: A localização do piloto é ONDE ESTÁ O AVIÃO ATIVO
            var activeAircraft = pilot.Hangar.FirstOrDefault(a => a.Id == pilot.ActiveAircraftId);
            if (activeAircraft != null)
            {
                CurrentAirportICAO = activeAircraft.CurrentLocationICAO;
                if (pilot.CurrentAirportICAO != CurrentAirportICAO)
                {
                    pilot.CurrentAirportICAO = CurrentAirportICAO;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                CurrentAirportICAO = pilot.CurrentAirportICAO;
            }

            OnPropertyChanged(nameof(CashDisplay));
            OnPropertyChanged(nameof(RankDisplay));
            OnPropertyChanged(nameof(XpProgress));
            OnPropertyChanged(nameof(PilotCategoryDisplay));
            OnPropertyChanged(nameof(PilotCategoryBadge));
            OnPropertyChanged(nameof(PilotCategoryColor));
            OnPropertyChanged(nameof(CompanyTierDisplay));
            OnPropertyChanged(nameof(CompanyTierBadge));
            OnPropertyChanged(nameof(CompanyTierColor));

            // Carregar missões aceitas do banco para o manifesto
            var acceptedMissions = await _context.Missions
                .Where(m => m.PilotId == pilot.Id && m.IsAccepted)
                .ToListAsync();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ActiveMissions.Clear();
                foreach (var m in acceptedMissions) ActiveMissions.Add(m);
            });

            var airport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == CurrentAirportICAO);
            if (airport != null)
            {
                CurrentAirportName = airport.Name;
                AirportTier = airport.Tier;
                OnPropertyChanged(nameof(CurrentLocation));

                // Inicializar coordenadas para o mapa (caso o simulador ainda não tenha enviado)
                if (Latitude == 0 || Longitude == 0)
                {
                    Latitude = airport.Latitude;
                    Longitude = airport.Longitude;
                    OriginLat = airport.Latitude;
                    OriginLon = airport.Longitude;
                }
                
                var missions = await _missionGenerator.GenerateMissionsAsync(airport);
                var missionList = missions.ToList();

                activeAircraft = pilot.Hangar.FirstOrDefault(a => a.Id == pilot.ActiveAircraftId);
                var pilotCategory = pilot.PilotCategory;

                var destIcaos = missionList.Select(m => m.DestinationICAO).Distinct().ToList();
                var destAirports = await _context.Airports
                    .Where(a => destIcaos.Contains(a.ICAO))
                    .ToDictionaryAsync(a => a.ICAO);

                var displayItems = new List<MissionDisplayItem>();
                foreach (var m in missionList)
                {
                    destAirports.TryGetValue(m.DestinationICAO, out var dest);
                    var item = new MissionDisplayItem(m, dest, activeAircraft, pilotCategory, airport);
                    item.BatchSelectionChanged += (s, sel) => UpdateBatchCount();
                    displayItems.Add(item);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableMissions.Clear();
                    foreach (var m in missionList) AvailableMissions.Add(m);

                    _allMissionDisplayItems.Clear();
                    _allMissionDisplayItems.AddRange(displayItems);
                    ApplyMissionFilters();
                });

                _ = FetchRealMetarsForMissionsAsync();
            }
            else
            {
                CurrentAirportName = "AEROPORTO NÃO LOCALIZADO";
                OnPropertyChanged(nameof(CurrentLocation));
            }

            // Atualizar dados da aeronave no briefing (para sincronizar localização/combustível)
            await RefreshBriefingAircraftAsync();
        }

        public async Task FetchRealMetarsForMissionsAsync()
        {
            try
            {
                var itemsToUpdate = _allMissionDisplayItems.ToList();
                if (!itemsToUpdate.Any()) return;

                var icaos = itemsToUpdate.Select(m => m.DestinationICAO).Distinct().ToList();
                if (!string.IsNullOrEmpty(CurrentAirportICAO) && !icaos.Contains(CurrentAirportICAO))
                    icaos.Add(CurrentAirportICAO);

                var metars = await _metarService.GetMetarsAsync(icaos);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in itemsToUpdate)
                    {
                        if (metars.TryGetValue(item.DestinationICAO, out var metar))
                        {
                            item.ApplyRealMetar(metar);
                        }
                    }
                });
            }
            catch { }
        }
    }
}
