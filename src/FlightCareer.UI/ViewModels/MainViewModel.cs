using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Domain.Services;
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
        private readonly Infrastructure.Persistence.AppDbContext _context;
        private readonly Services.SimConnectService _simConnect;
        private readonly Services.FlightMonitor _flightMonitor;

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
        [ObservableProperty] private double _latitude;
        [ObservableProperty] private double _longitude;
        [ObservableProperty] private double _originLat;
        [ObservableProperty] private double _originLon;
        [ObservableProperty] private double _destLat;
        [ObservableProperty] private double _destLon;
        [ObservableProperty] private string _currentView = "Missions";
        [ObservableProperty] private Guid? _activeAircraftId;
        [ObservableProperty] private CompanyTier _companyTier = CompanyTier.Tier1_FlightSchool;

        public string CompanyTierDisplay => CompanyTier.GetDisplayName();
        public string CompanyTierBadge => CompanyTier.GetShortName();
        public string CompanyTierColor => CompanyTier.GetBadgeColor();
        
        partial void OnSelectedMissionChanged(Mission? value)
        {
            OnPropertyChanged(nameof(TotalPaxCount));
            OnPropertyChanged(nameof(TotalPaxWeight));
            OnPropertyChanged(nameof(TotalFlightWeight));

            if (value != null)
            {
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
        public string CashDisplay => Balance.ToString("C2");
        public string RankDisplay => Rank?.ToUpper() ?? "STUDENT";
        public double XpProgress => (TotalHours % 100);
        public string CurrentLocation => $"{CurrentAirportICAO} - {CurrentAirportName}";

        public ObservableCollection<Flight> RecentFlights { get; } = new();
        public ObservableCollection<Mission> AvailableMissions { get; } = new();

        public MainViewModel(IPilotRepository pilotRepository, 
            IMissionGenerator missionGenerator, 
            MarketViewModel marketViewModel,
            HangarViewModel hangarViewModel,
            Infrastructure.Persistence.AppDbContext context,
            Services.SimConnectService simConnect,
            Services.FlightMonitor flightMonitor)
        {
            _pilotRepository = pilotRepository;
            _missionGenerator = missionGenerator;
            _marketViewModel = marketViewModel;
            _hangarViewModel = hangarViewModel;
            _context = context;
            _simConnect = simConnect;
            _flightMonitor = flightMonitor;
            _briefingViewModel = new BriefingViewModel(_simConnect, ActiveMissions, _context);
            _financeViewModel = new FinanceViewModel(_context);

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

            AcceptMissionCommand = new RelayCommand(AcceptMission);
            RemoveMissionCommand = new RelayCommand<Mission>(RemoveMission);
            StartFlightCommand = new AsyncRelayCommand(async () => {
                if (IsMonitoring) await CancelFlightAsync();
                else StartFlight();
            });
            ShowDebugCommand = new RelayCommand(ShowDebug);
            PromoteCompanyTierCommand = new AsyncRelayCommand(PromoteCompanyTierAsync);

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
        public IRelayCommand AcceptMissionCommand { get; }
        public IRelayCommand RemoveMissionCommand { get; }
        public IAsyncRelayCommand StartFlightCommand { get; }
        public IRelayCommand ShowDebugCommand { get; }
        public IAsyncRelayCommand PromoteCompanyTierCommand { get; }

        private void ShowDebug()
        {
            var debugWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Views.DebugWindow>(((App)System.Windows.Application.Current).ServiceProvider);
            debugWindow.Show();
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
                    _briefingViewModel.HangarAircraftFuel = active.FuelAmount;
                    _briefingViewModel.HangarAircraftLocation = active.CurrentLocationICAO;
                }
            }
            else
            {
                _briefingViewModel.HangarAircraftModel = "NENHUMA ATIVA";
                _briefingViewModel.HangarAircraftFuel = 0;
                _briefingViewModel.HangarAircraftLocation = "---";
            }
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
                    if (_missionGenerator is Infrastructure.Services.MissionGenerator mg)
                    {
                        mission.RewardMoney = mg.CalculateReward(newDist, mission.PayloadLbs, mission.Urgency);
                        mission.ExperienceGain = (int)(newDist * 0.5 * mission.Urgency);
                    }
                }

                AvailableMissions.Add(mission);
                await _context.SaveChangesAsync();
                
                System.Windows.MessageBox.Show($"Missão para {mission.DestinationICAO} removida. A carga foi deixada em {CurrentAirportICAO} e a recompensa foi atualizada.");
            }
        }

        private async void AcceptMission()
        {
            if (SelectedMission == null) return;
            
            var mission = SelectedMission;
            if (!ActiveMissions.Contains(mission))
            {
                var pilots = await _pilotRepository.GetAllAsync();
                var pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
                if (pilot != null)
                {
                    if (mission.RequiredTier > pilot.CompanyTier)
                    {
                        System.Windows.MessageBox.Show($"Sua empresa ({pilot.CompanyTier.GetDisplayName()}) ainda não possui homologação para realizar missões do tipo '{mission.RequiredTier.GetDisplayName()}'.\n\nEvolua a certificação da sua empresa para desbloquear esta operação.", "Missão Bloqueada por Homologação", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    ActiveMissions.Add(mission);
                    AvailableMissions.Remove(mission);
                    mission.IsAccepted = true;
                    mission.PilotId = pilot.Id;
                    
                    SelectedMission = null;
                    await _context.SaveChangesAsync();
                    System.Windows.MessageBox.Show($"Missão para {mission.DestinationICAO} aceita!");
                }
            }
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

            var prohibitedMission = ActiveMissions.FirstOrDefault(m => m.RequiredTier > pilot.CompanyTier);
            if (prohibitedMission != null)
            {
                System.Windows.MessageBox.Show($"Operação não homologada!\nA missão '{prohibitedMission.Title}' exige homologação {prohibitedMission.RequiredTier.GetDisplayName()}, mas sua empresa opera em {pilot.CompanyTier.GetDisplayName()}.\n\nRemova a missão do manifesto ou obtenha a certificação necessária.", "Homologação Regulatória Insuficiente", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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

            if (CompanyTier >= CompanyTier.Tier5_MajorAirline)
            {
                System.Windows.MessageBox.Show("Parabéns! Sua companhia já atingiu a homologação máxima da aviação: Tier 5 (Grande Linha Aérea Regular)!", "Nível Máximo Atingido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var nextTier = (CompanyTier)((int)CompanyTier + 1);
            double cost = CompanyTier.GetPromotionCost();
            double reqHours = CompanyTier.GetRequiredHours();

            string reqDesc = nextTier switch
            {
                CompanyTier.Tier2_SAE => "- Carteira de Piloto Privado (40+ Horas de Voo)\n- Manual de Serviços Especializados e Inspeção ANAC/FAA",
                CompanyTier.Tier3_AirTaxi => "- Licença de Piloto Comercial (PC) + IFR (150+ Horas)\n- Homologação de Táxi Aéreo, Gestor SGSO e Mecânico Responsável (DTS)",
                CompanyTier.Tier4_Regional => "- Habilitação Multimotor (MLTE) + IFR (500+ Horas)\n- Infraestrutura de Rampa/Ground Handling e Sistema DOV",
                CompanyTier.Tier5_MajorAirline => "- Licença de Piloto de Linha Aérea (PLA) (1500+ Horas)\n- Slots em Hubs Principais, Auditoria IOSA e Homologação Regular",
                _ => ""
            };

            string statusMsg = $"CERTIFICAÇÃO REGULATÓRIA DA EMPRESA\n\n" +
                               $"Homologação Atual: {CompanyTier.GetDisplayName()}\n" +
                               $"Próximo Nível: {nextTier.GetDisplayName()}\n\n" +
                               $"REQUISITOS OPERACIONAIS:\n{reqDesc}\n\n" +
                               $"Horas de Voo: {TotalHours:F1}h / {reqHours:F0}h necessárias\n" +
                               $"Taxa de Homologação: {cost:C2} (Seu saldo: {Balance:C2})\n\n";

            bool canPromote = (TotalHours >= reqHours) && (Balance >= cost);

            if (!canPromote)
            {
                statusMsg += "Você ainda NÃO cumpre todos os requisitos para homologar este nível.";
                System.Windows.MessageBox.Show(statusMsg, "Progressão de Companhia", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            statusMsg += "Deseja pagar a taxa de homologação e certificar sua empresa para o novo Tier agora?";
            var result = System.Windows.MessageBox.Show(statusMsg, "Evolução de Homologação", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            pilot.Balance -= cost;
            pilot.CompanyTier = nextTier;

            _context.FinancialTransactions.Add(new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                PilotId = pilot.Id,
                Date = DateTime.Now,
                Category = "Certificação",
                Description = $"HOMOLOGAÇÃO REGULATÓRIA: {nextTier.GetDisplayName()}",
                Amount = cost,
                Type = TransactionType.Expense
            });

            await _context.SaveChangesAsync();
            await InitializeForPilotAsync(pilot.Name);

            System.Windows.MessageBox.Show($"HOMOLOGAÇÃO APROVADA!\n\nSua empresa agora opera oficialmente sob: {nextTier.GetDisplayName()}.\nNovas missões e operações comerciais foram desbloqueadas!", "Certificação Concluída", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

        private void OnFlightFinished(Infrastructure.Services.FlightOutcome outcome)
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
            CompanyTier = pilot.CompanyTier;

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
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableMissions.Clear();
                    foreach (var m in missionList) AvailableMissions.Add(m);
                });
            }
            else
            {
                CurrentAirportName = "AEROPORTO NÃO LOCALIZADO";
                OnPropertyChanged(nameof(CurrentLocation));
            }

            // Atualizar dados da aeronave no briefing (para sincronizar localização/combustível)
            await RefreshBriefingAircraftAsync();
        }
    }
}
