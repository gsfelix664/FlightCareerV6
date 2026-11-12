using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Domain.Utils;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FlightCareer.UI.ViewModels
{
    public partial class FerryFlightViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly IPilotRepository _pilotRepository;
        private readonly Action _onFerryCompleted;

        [ObservableProperty] private string _pilotName = string.Empty;
        [ObservableProperty] private double _pilotBalance;
        [ObservableProperty] private string _originIcao = string.Empty;
        [ObservableProperty] private string _originAirportName = "---";
        [ObservableProperty] private string _originLocation = "---";
        [ObservableProperty] private double _originLat;
        [ObservableProperty] private double _originLon;

        [ObservableProperty] private string _aircraftPrefix = "---";
        [ObservableProperty] private string _aircraftModel = "---";
        [ObservableProperty] private double _cruiseSpeed = 110;
        [ObservableProperty] private double _fuelConsumptionGph = 8.0;
        [ObservableProperty] private string _fuelType = "Avgas";

        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _destinationIcao = string.Empty;
        [ObservableProperty] private string _destinationAirportName = "Nenhum selecionado";
        [ObservableProperty] private string _destinationLocation = "Informe o código ICAO de destino";
        [ObservableProperty] private double _destLat;
        [ObservableProperty] private double _destLon;
        [ObservableProperty] private bool _hasValidDestination;

        [ObservableProperty] private double _distanceNM;
        [ObservableProperty] private double _estimatedHours;
        [ObservableProperty] private double _estimatedFuelGallons;
        [ObservableProperty] private double _fuelPricePerGallon;
        [ObservableProperty] private double _directFuelCost;

        // Custos do Translado Imediato (Pular)
        [ObservableProperty] private double _jumpPilotFee;
        [ObservableProperty] private double _jumpHandlingFee;
        [ObservableProperty] private double _totalJumpCost;
        [ObservableProperty] private bool _canAffordJump;
        [ObservableProperty] private string _statusMessage = string.Empty;

        private Pilot? _pilot;
        private PilotAircraft? _aircraft;
        private Airport? _destAirport;

        public FerryFlightViewModel(
            AppDbContext context, 
            IPilotRepository pilotRepository,
            string pilotName,
            Action onFerryCompleted)
        {
            _context = context;
            _pilotRepository = pilotRepository;
            _pilotName = pilotName;
            _onFerryCompleted = onFerryCompleted;

            _ = LoadCurrentDataAsync();
        }

        public async Task LoadCurrentDataAsync()
        {
            _context.ChangeTracker.Clear(); // Contexto de longa duração: descarta entidades em cache para ler o estado atual do banco
            _pilot = await _context.Pilots
                .Include(p => p.Hangar)
                .ThenInclude(a => a.AircraftType)
                .FirstOrDefaultAsync(p => p.Name == PilotName);

            if (_pilot == null)
            {
                var pilots = await _pilotRepository.GetAllAsync();
                _pilot = pilots.FirstOrDefault(p => p.Name == PilotName);
            }

            if (_pilot == null) return;

            PilotBalance = _pilot.Balance;
            OriginIcao = _pilot.CurrentAirportICAO;

            var originAirport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == OriginIcao);
            if (originAirport != null)
            {
                OriginAirportName = originAirport.Name;
                OriginLocation = $"{originAirport.City}, {originAirport.Country} (Tier {originAirport.Tier})";
                OriginLat = originAirport.Latitude;
                OriginLon = originAirport.Longitude;
            }

            _aircraft = _pilot.Hangar?.FirstOrDefault(a => a.Id == _pilot.ActiveAircraftId);
            if (_aircraft == null && _pilot.ActiveAircraftId != null)
            {
                _aircraft = await _context.PilotAircrafts
                    .Include(a => a.AircraftType)
                    .FirstOrDefaultAsync(a => a.Id == _pilot.ActiveAircraftId.Value);
            }

            if (_aircraft != null)
            {
                AircraftPrefix = _aircraft.Prefix;
                AircraftModel = _aircraft.AircraftType?.Model ?? "Aeronave Padrão";
                CruiseSpeed = _aircraft.AircraftType?.CruiseSpeedKts > 0 ? _aircraft.AircraftType.CruiseSpeedKts : 110;
                FuelConsumptionGph = _aircraft.AircraftType?.FuelConsumptionGPH > 0 ? _aircraft.AircraftType.FuelConsumptionGPH : 8.0;
                FuelType = string.IsNullOrWhiteSpace(_aircraft.AircraftType?.FuelType) ? "Avgas" : _aircraft.AircraftType.FuelType;
            }

            FuelPricePerGallon = FuelPriceService.GetCurrentPrice(FuelType);
        }

        [RelayCommand]
        public async Task SearchDestinationAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = "Digite o código ICAO do aeroporto de destino.";
                return;
            }

            string cleanQuery = SearchQuery.Trim().ToUpper();

            if (cleanQuery == OriginIcao)
            {
                StatusMessage = "O destino não pode ser o mesmo aeroporto de origem.";
                HasValidDestination = false;
                return;
            }

            // Buscar por ICAO, MsfsIdent ou Nome
            _destAirport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == cleanQuery || a.MsfsIdent == cleanQuery)
                        ?? await _context.Airports.FirstOrDefaultAsync(a => a.Name.ToUpper().Contains(cleanQuery) || a.City.ToUpper().Contains(cleanQuery));

            if (_destAirport == null)
            {
                StatusMessage = $"Nenhum aeroporto encontrado com o código/nome '{cleanQuery}'.";
                HasValidDestination = false;
                DestinationIcao = string.Empty;
                DestinationAirportName = "Não encontrado";
                DestinationLocation = "Verifique o ICAO e tente novamente";
                return;
            }

            if (_destAirport.ICAO == OriginIcao)
            {
                StatusMessage = "O destino não pode ser o mesmo aeroporto de origem.";
                HasValidDestination = false;
                return;
            }

            DestinationIcao = _destAirport.ICAO;
            DestinationAirportName = _destAirport.Name;
            DestinationLocation = $"{_destAirport.City}, {_destAirport.Country} (Tier {_destAirport.Tier})";
            DestLat = _destAirport.Latitude;
            DestLon = _destAirport.Longitude;

            // Recalcular distâncias e custos
            DistanceNM = Math.Round(LocationUtils.CalculateDistance(OriginLat, OriginLon, DestLat, DestLon), 0);
            EstimatedHours = Math.Max(0.1, DistanceNM / Math.Max(50, CruiseSpeed));
            EstimatedFuelGallons = Math.Round(EstimatedHours * FuelConsumptionGph, 1);
            DirectFuelCost = Math.Round(EstimatedFuelGallons * FuelPricePerGallon, 2);

            // Fórmula do Translado Imediato (Pular):
            // Custo deve ser sempre superior ao voo real (combustível com margem + diária de piloto terceirizado + despacho operacional)
            JumpPilotFee = Math.Round((EstimatedHours * 100.0) + 150.0, 2);
            JumpHandlingFee = Math.Round(80.0 + (DistanceNM * 0.15), 2);
            TotalJumpCost = Math.Round((DirectFuelCost * 1.5) + JumpPilotFee + JumpHandlingFee, 2);

            CanAffordJump = PilotBalance >= TotalJumpCost;
            HasValidDestination = true;
            StatusMessage = CanAffordJump 
                ? "Destino selecionado! Escolha abaixo como deseja realizar o translado." 
                : "Aviso: Saldo insuficiente para Translado Imediato. Você pode voar por conta própria no simulador.";
        }

        [RelayCommand]
        public async Task SetupRealFlightAsync(Window? window)
        {
            if (!HasValidDestination || _destAirport == null)
            {
                MessageBox.Show("Selecione um destino válido primeiro pesquisando pelo código ICAO ou nome da cidade.", "Destino Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_pilot == null)
            {
                MessageBox.Show("Perfil de piloto não encontrado.", "Erro de Perfil", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_aircraft == null)
            {
                MessageBox.Show("Você precisa ter uma aeronave ativa selecionada no seu Hangar para realizar o translado.", "Aeronave Necessária", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Criar missão de Translado no Manifesto
            var ferryMission = new Mission
            {
                Id = Guid.NewGuid(),
                Title = $"Voo de Translado (Ferry) - {OriginIcao} ➔ {_destAirport.ICAO}",
                Description = $"Voo técnico de reposicionamento de aeronave até {_destAirport.Name} ({_destAirport.City}, {_destAirport.Country}). Sem passageiros ou carga comercial a bordo.",
                OriginICAO = OriginIcao,
                DestinationICAO = _destAirport.ICAO,
                OriginMsfsIdent = (await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == OriginIcao))?.MsfsIdent ?? OriginIcao,
                DestinationMsfsIdent = _destAirport.MsfsIdent ?? _destAirport.ICAO,
                DistanceNM = DistanceNM,
                PassengerCount = 0,
                LuggageWeightLbs = 0,
                CargoWeightLbs = 0,
                PayloadLbs = 0,
                Urgency = 1.0,
                RewardMoney = 0,
                ExperienceGain = (int)(DistanceNM * 0.5),
                ExpirationTime = DateTime.Now.AddHours(72),
                IsAccepted = true,
                PilotId = _pilot.Id,
                RequiredCategory = PilotCategory.Category1_FlightSchool,
                MissionCategoryType = "Translado / Ferry"
            };

            await _context.Missions.AddAsync(ferryMission);
            await _context.SaveChangesAsync();

            MessageBox.Show(
                $"Voo de translado configurado com sucesso!\n\n" +
                $"Origem: {OriginIcao}\n" +
                $"Destino: {_destAirport.ICAO} ({_destAirport.Name})\n" +
                $"Distância: {DistanceNM:F0} NM (~{EstimatedHours:F1}h de voo)\n\n" +
                $"O manifesto foi atualizado no Briefing. Decole no simulador e complete o voo para acumular horas de voo e XP no seu perfil!",
                "Translado no Simulador Configurado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _onFerryCompleted?.Invoke();
            window?.Close();
        }

        [RelayCommand]
        public async Task ExecuteInstantJumpAsync(Window? window)
        {
            if (!HasValidDestination || _destAirport == null)
            {
                MessageBox.Show("Selecione um destino válido primeiro pesquisando pelo código ICAO ou nome da cidade.", "Destino Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_pilot == null)
            {
                MessageBox.Show("Perfil de piloto não encontrado.", "Erro de Perfil", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_aircraft == null)
            {
                MessageBox.Show("Você precisa ter uma aeronave ativa selecionada no seu Hangar para realizar o translado.", "Aeronave Necessária", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!CanAffordJump)
            {
                MessageBox.Show($"Saldo insuficiente! Você precisa de {TotalJumpCost:C2}, mas seu saldo atual é {PilotBalance:C2}.", "Saldo Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"CONFIRMAÇÃO DE TRANSLADO IMEDIATO (PULAR)\n\n" +
                $"Origem: {OriginIcao}\n" +
                $"Destino: {_destAirport.ICAO} ({_destAirport.Name})\n" +
                $"Distância: {DistanceNM:F0} NM (~{EstimatedHours:F1}h)\n\n" +
                $"DETALHAMENTO DO CUSTO:\n" +
                $"- Combustível ({EstimatedFuelGallons:F1} GAL): {DirectFuelCost * 1.5:C2}\n" +
                $"- Diária do Piloto de Translado: {JumpPilotFee:C2}\n" +
                $"- Taxas de Despacho e Rampa: {JumpHandlingFee:C2}\n" +
                $"TOTAL A PAGAR: {TotalJumpCost:C2}\n\n" +
                $"ATENÇÃO: A aeronave {AircraftPrefix} terá o desgaste de {EstimatedHours:F1}h aplicado ao motor/célula. Como o voo foi terceirizado, NÃO serão adicionadas horas nem XP ao seu perfil de piloto.\n\nDeseja pagar e reposicionar agora?",
                "Confirmar Translado Imediato",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // 1. Debitar saldo
                _pilot.Balance -= TotalJumpCost;

                // 2. Reposicionar piloto e aeronave
                _pilot.CurrentAirportICAO = _destAirport.ICAO;
                _aircraft.CurrentLocationICAO = _destAirport.ICAO;

                // 3. Aplicar desgaste de horas na aeronave
                _aircraft.CurrentAirframeHours += EstimatedHours;
                _aircraft.EngineTboRemainingHours = Math.Max(0, _aircraft.EngineTboRemainingHours - EstimatedHours);
                _aircraft.HoursUntilNext50hInspection = Math.Max(0, _aircraft.HoursUntilNext50hInspection - EstimatedHours);
                _aircraft.HoursUntilNext100hInspection = Math.Max(0, _aircraft.HoursUntilNext100hInspection - EstimatedHours);

                if (_aircraft.EngineTboRemainingHours <= 0)
                {
                    _aircraft.IsInterdicted = true;
                    _aircraft.InterdictionReason = "TBO Vencido durante Translado";
                }
                else if (_aircraft.HoursUntilNext100hInspection <= 0)
                {
                    _aircraft.IsInterdicted = true;
                    _aircraft.InterdictionReason = "Inspeção de 100h Vencida durante Translado";
                }

                // 4. Registrar transação financeira
                _context.FinancialTransactions.Add(new FinancialTransaction
                {
                    Id = Guid.NewGuid(),
                    PilotId = _pilot.Id,
                    Date = DateTime.Now,
                    Type = TransactionType.Expense,
                    Category = "Translado",
                    Description = $"TRANSLADO IMEDIATO: {OriginIcao} ➔ {_destAirport.ICAO} ({DistanceNM:F0} NM) - Piloto Terceirizado e Combustível",
                    Amount = TotalJumpCost
                });

                await _context.SaveChangesAsync();

                MessageBox.Show(
                    $"TRANSLADO CONCLUÍDO COM SUCESSO!\n\n" +
                    $"Sua aeronave {AircraftPrefix} e sua empresa foram reposicionadas em {_destAirport.ICAO} ({_destAirport.Name}).\n" +
                    $"O mercado de missões deste aeroporto foi carregado.",
                    "Translado Concluído",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _onFerryCompleted?.Invoke();
                window?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar translado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
