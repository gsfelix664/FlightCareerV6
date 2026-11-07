using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using FlightCareer.UI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FlightCareer.UI.ViewModels
{
    public partial class MarketViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly IPilotRepository _pilotRepository;
        private readonly Random _random = new();
        private string _currentPilotName = string.Empty;

        [ObservableProperty] private ObservableCollection<MarketOffer> _offers = new();
        [ObservableProperty] 
        [NotifyCanExecuteChangedFor(nameof(BuyCommand))]
        private MarketOffer? _selectedOffer;
        
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private double _userBalance;
        [ObservableProperty] private string _searchText = string.Empty;

        private List<MarketOffer> _allOffers = new();

        public MarketViewModel(AppDbContext context, IPilotRepository pilotRepository)
        {
            _context = context;
            _pilotRepository = pilotRepository;
        }

        public async Task InitializeAsync(string pilotName, double balance)
        {
            _currentPilotName = pilotName;
            UserBalance = balance;
            await LoadOffersAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allOffers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim().ToLowerInvariant();
                query = query.Where(o =>
                    (o.BaseType?.Model != null && o.BaseType.Model.ToLowerInvariant().Contains(term)) ||
                    (o.BaseType?.Type != null && o.BaseType.Type.ToLowerInvariant().Contains(term)) ||
                    (o.Prefix != null && o.Prefix.ToLowerInvariant().Contains(term)));
            }

            var filtered = query.OrderBy(o => o.SalePrice).ToList();
            Offers = new ObservableCollection<MarketOffer>(filtered);

            if (SelectedOffer == null || !Offers.Contains(SelectedOffer))
            {
                SelectedOffer = Offers.FirstOrDefault();
            }
        }

        public async Task LoadOffersAsync()
        {
            IsLoading = true;
            try
            {
                var aircraftTypes = await _context.AircraftTypes.ToListAsync();
                var generatedOffers = new List<MarketOffer>();

                if (aircraftTypes.Count > 0)
                {
                    // Embaralha para que cada geração mostre modelos variados em destaque
                    var shuffledTypes = aircraftTypes.OrderBy(_ => _random.Next()).ToList();
                    const int targetCount = 50;

                    for (int i = 0; i < targetCount; i++)
                    {
                        // Garante que cada modelo apareça ao menos uma vez antes de repetir
                        var type = (i < shuffledTypes.Count)
                            ? shuffledTypes[i]
                            : aircraftTypes[_random.Next(aircraftTypes.Count)];

                        // Gerar condições (entre 40% e 100%)
                        double engine = _random.Next(60, 101);
                        double tires = _random.Next(40, 101);
                        double airframe = _random.Next(70, 101);

                        double avg = (engine + tires + airframe) / 3.0;

                        // Preço: Base * (Condição Média / 100) * Fator de Mercado (0.8 a 1.1)
                        double marketFactor = 0.8 + (_random.NextDouble() * 0.3);
                        double aircraftValue = type.PurchasePrice * (avg / 100.0) * marketFactor;

                        // Valor do combustível (Cotação diária)
                        double fuelAmount = Math.Round(_random.NextDouble() * type.FuelTankGallons, 1);
                        double currentFuelPrice = FuelPriceService.GetCurrentPrice(type.FuelType);
                        double fuelValue = fuelAmount * currentFuelPrice;

                        generatedOffers.Add(new MarketOffer
                        {
                            BaseType = type,
                            Prefix = GeneratePrefix(),
                            EngineCondition = engine,
                            TiresCondition = tires,
                            AirframeCondition = airframe,
                            AirframeHours = _random.Next(100, 8000), // Horas totais da célula
                            FuelAmount = fuelAmount,
                            SalePrice = Math.Round(aircraftValue + fuelValue, 2)
                        });
                    }
                }

                _allOffers = generatedOffers.OrderBy(o => o.SalePrice).ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar mercado: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GeneratePrefix()
        {
            string[] brPrefixes = { "PP-", "PT-", "PR-", "PS-" };
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string letters = new string(Enumerable.Repeat(chars, 3).Select(s => s[_random.Next(s.Length)]).ToArray());
            return brPrefixes[_random.Next(brPrefixes.Length)] + letters;
        }

        public string BuyButtonText
        {
            get
            {
                if (SelectedOffer == null) return "SELECIONE UMA AERONAVE";
                if (UserBalance < SelectedOffer.SalePrice) return "SALDO INSUFICIENTE";
                return $"COMPRAR POR {SelectedOffer.SalePrice:C2}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanBuy))]
        private async Task Buy()
        {
            if (SelectedOffer == null) return;

            var result = MessageBox.Show($"Confirmar a compra do {SelectedOffer.BaseType.Model} ({SelectedOffer.Prefix}) por {SelectedOffer.SalePrice:C2}?", 
                "Mercado de Aeronaves", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Carregar piloto de forma limpa
                var pilot = await _context.Pilots
                    .Include(p => p.Hangar)
                    .FirstOrDefaultAsync(p => p.Name == _currentPilotName);
                
                if (pilot == null)
                {
                    MessageBox.Show("Piloto não encontrado no banco de dados.");
                    return;
                }

                if (pilot.Balance < SelectedOffer.SalePrice)
                {
                    MessageBox.Show("Você não tem saldo suficiente!");
                    return;
                }

                // 1. Deduzir Saldo (O EF detectará a mudança no objeto 'pilot' automaticamente)
                pilot.Balance -= SelectedOffer.SalePrice;
                UserBalance = pilot.Balance;

                // 2. Criar e adicionar aeronave ao hangar do piloto
                var newAircraft = new PilotAircraft
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    AircraftTypeId = SelectedOffer.BaseType.Id,
                    PurchaseDate = DateTime.Now,
                    Prefix = SelectedOffer.Prefix,
                    EngineCondition = SelectedOffer.EngineCondition,
                    TiresCondition = SelectedOffer.TiresCondition,
                    AirframeCondition = SelectedOffer.AirframeCondition,
                    CurrentAirframeHours = SelectedOffer.AirframeHours,
                    FuelAmount = SelectedOffer.FuelAmount,
                    CurrentLocationICAO = pilot.CurrentAirportICAO
                };
                
                _context.PilotAircrafts.Add(newAircraft);

                // 3. Registrar no Financeiro
                _context.FinancialTransactions.Add(new FinancialTransaction
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    Date = DateTime.Now,
                    Amount = SelectedOffer.SalePrice,
                    Type = TransactionType.Expense,
                    Category = "Aeronave",
                    Description = $"Compra de Aeronave: {SelectedOffer.BaseType.Model} ({SelectedOffer.Prefix})"
                });

                // 4. Salvar tudo em uma única transação atômica
                await _context.SaveChangesAsync();

                MessageBox.Show($"Parabéns! O {SelectedOffer.BaseType.Model} ({SelectedOffer.Prefix}) agora faz parte da sua frota.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Remover a oferta comprada da lista UI e da lista geral
                var offerToRemove = SelectedOffer;
                SelectedOffer = null;
                if (offerToRemove != null)
                {
                    _allOffers.Remove(offerToRemove);
                    Offers.Remove(offerToRemove);
                }
                
                OnPropertyChanged(nameof(BuyButtonText));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha na transação: {ex.Message}");
            }
        }

        private bool CanBuy => SelectedOffer != null && UserBalance >= SelectedOffer.SalePrice;

        partial void OnSelectedOfferChanged(MarketOffer? value)
        {
            OnPropertyChanged(nameof(BuyButtonText));
        }
    }
}
