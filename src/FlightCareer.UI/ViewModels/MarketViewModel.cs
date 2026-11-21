using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Infrastructure.Persistence;
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
        private readonly IMarketSimulationService _marketService;
        private string _currentPilotName = string.Empty;

        [ObservableProperty] private ObservableCollection<MarketOffer> _offers = new();
        [ObservableProperty] 
        [NotifyCanExecuteChangedFor(nameof(BuyCommand))]
        private MarketOffer? _selectedOffer;
        
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private double _userBalance;
        [ObservableProperty] private string _searchText = string.Empty;

        // Filtros rápidos e Ordenação
        [ObservableProperty] private string _selectedCategoryFilter = "TODAS";
        [ObservableProperty] private string _selectedSortOption = "MENOR PREÇO";
        public IReadOnlyList<string> SortOptions { get; } = new[]
        {
            "MENOR PREÇO",
            "MAIOR CONDIÇÃO",
            "MENOS HORAS (TTAF)",
            "MELHORES OPORTUNIDADES"
        };

        [RelayCommand]
        private void SetCategoryFilter(string? category)
        {
            if (!string.IsNullOrWhiteSpace(category))
            {
                SelectedCategoryFilter = category;
            }
        }

        [RelayCommand]
        private void SelectTab(object? parameter)
        {
            if (parameter is int idx)
            {
                SelectedTab = idx;
            }
            else if (parameter != null && int.TryParse(parameter.ToString(), out int parsed))
            {
                SelectedTab = parsed;
            }
        }

        // Abas: 0 = Comprar, 1 = Vender
        [ObservableProperty] private int _selectedTab = 0;
        [ObservableProperty] private ObservableCollection<PlayerAircraftSaleItem> _playerAircraftsForSale = new();
        [ObservableProperty] private PlayerAircraftSaleItem? _selectedPlayerAircraft;

        private List<MarketOffer> _allOffers = new();

        public MarketViewModel(
            AppDbContext context, 
            IPilotRepository pilotRepository, 
            IMarketSimulationService marketService)
        {
            _context = context;
            _pilotRepository = pilotRepository;
            _marketService = marketService;
        }

        public async Task InitializeAsync(string pilotName, double balance)
        {
            _currentPilotName = pilotName;
            UserBalance = balance;
            
            await _marketService.EnsureMarketInitializedAsync();
            await _marketService.ProcessSimulationTicksAsync();
            
            await LoadOffersAsync();
            if (SelectedTab == 1)
            {
                await LoadPlayerAircraftsForSaleAsync();
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedCategoryFilterChanged(string value) => ApplyFilter();
        partial void OnSelectedSortOptionChanged(string value) => ApplyFilter();

        partial void OnSelectedTabChanged(int value)
        {
            if (value == 1)
            {
                _ = LoadPlayerAircraftsForSaleAsync();
            }
        }

        partial void OnSelectedOfferChanged(MarketOffer? value)
        {
            OnPropertyChanged(nameof(BuyButtonText));
        }

        partial void OnSelectedPlayerAircraftChanged(PlayerAircraftSaleItem? value)
        {
            OnPropertyChanged(nameof(SellButtonText));
        }

        private void ApplyFilter()
        {
            var query = _allOffers.AsEnumerable();

            // 1. Filtro textual
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim().ToLowerInvariant();
                query = query.Where(o =>
                    (o.BaseType?.Model != null && o.BaseType.Model.ToLowerInvariant().Contains(term)) ||
                    (o.BaseType?.Type != null && o.BaseType.Type.ToLowerInvariant().Contains(term)) ||
                    (o.CategoryBadge != null && o.CategoryBadge.ToLowerInvariant().Contains(term)) ||
                    (o.Prefix != null && o.Prefix.ToLowerInvariant().Contains(term)));
            }

            // 2. Filtro rápido de categoria
            if (!string.IsNullOrWhiteSpace(SelectedCategoryFilter) && SelectedCategoryFilter != "TODAS")
            {
                if (SelectedCategoryFilter == "MONOMOTOR")
                {
                    query = query.Where(o => o.CategoryBadge.Contains("Monomotor", StringComparison.OrdinalIgnoreCase) ||
                                             o.CategoryBadge.Contains("STOL", StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedCategoryFilter == "BIMOTOR")
                {
                    query = query.Where(o => o.CategoryBadge.Contains("Bimotor", StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedCategoryFilter == "TURBOÉLICE")
                {
                    query = query.Where(o => o.CategoryBadge.Contains("Turboélice", StringComparison.OrdinalIgnoreCase) ||
                                             o.CategoryBadge.Contains("Jato", StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedCategoryFilter == "ATÉ $100K")
                {
                    query = query.Where(o => o.SalePrice <= 100000.0);
                }
            }

            // 3. Ordenação
            query = SelectedSortOption switch
            {
                "MAIOR CONDIÇÃO" => query.OrderBy(o => o.IsSold).ThenByDescending(o => o.AverageCondition),
                "MENOS HORAS (TTAF)" => query.OrderBy(o => o.IsSold).ThenBy(o => o.AirframeHours),
                "MELHORES OPORTUNIDADES" => query.OrderBy(o => o.IsSold).ThenByDescending(o => o.DealRatio),
                _ => query.OrderBy(o => o.IsSold).ThenBy(o => o.SalePrice) // Padrão: "MENOR PREÇO"
            };

            var filtered = query.ToList();
            Offers = new ObservableCollection<MarketOffer>(filtered);

            if (SelectedOffer == null || !Offers.Contains(SelectedOffer))
            {
                SelectedOffer = Offers.FirstOrDefault(o => !o.IsSold) ?? Offers.FirstOrDefault();
            }
        }

        public async Task LoadOffersAsync()
        {
            IsLoading = true;
            try
            {
                var activeListings = await _marketService.GetActiveListingsAsync();
                var recentlySold = await _marketService.GetRecentlySoldListingsAsync(5);

                var generatedOffers = new List<MarketOffer>();

                foreach (var l in activeListings)
                {
                    generatedOffers.Add(new MarketOffer
                    {
                        Id = l.Id,
                        BaseType = l.AircraftType ?? new AircraftType(),
                        Prefix = l.Prefix,
                        EngineCondition = l.EngineCondition,
                        TiresCondition = l.TiresCondition,
                        AirframeCondition = l.AirframeCondition,
                        AvionicsCondition = l.AvionicsCondition,
                        AirframeHours = l.AirframeHours,
                        YearOfManufacture = l.YearOfManufacture,
                        TboHours = l.TboHours,
                        TsmoHours = l.TsmoHours,
                        StructuralIncidentHistory = l.StructuralIncidentHistory,
                        CategoryBadge = l.CategoryBadge,
                        FuelAmount = l.FuelAmount,
                        SalePrice = l.AskingPrice,
                        BaseMarketValue = l.BaseMarketValue,
                        OriginalAskingPrice = l.OriginalAskingPrice,
                        PriceDropsCount = l.PriceDropsCount,
                        IsSold = false,
                        SoldAtUtc = null,
                        IsPlayerOwned = l.IsPlayerOwned,
                        PlayerAircraftId = l.PlayerAircraftId
                    });
                }

                foreach (var s in recentlySold)
                {
                    generatedOffers.Add(new MarketOffer
                    {
                        Id = s.Id,
                        BaseType = s.AircraftType ?? new AircraftType(),
                        Prefix = s.Prefix,
                        EngineCondition = s.EngineCondition,
                        TiresCondition = s.TiresCondition,
                        AirframeCondition = s.AirframeCondition,
                        AvionicsCondition = s.AvionicsCondition,
                        AirframeHours = s.AirframeHours,
                        YearOfManufacture = s.YearOfManufacture,
                        TboHours = s.TboHours,
                        TsmoHours = s.TsmoHours,
                        StructuralIncidentHistory = s.StructuralIncidentHistory,
                        CategoryBadge = s.CategoryBadge,
                        FuelAmount = s.FuelAmount,
                        SalePrice = s.AskingPrice,
                        BaseMarketValue = s.BaseMarketValue,
                        OriginalAskingPrice = s.OriginalAskingPrice,
                        PriceDropsCount = s.PriceDropsCount,
                        IsSold = true,
                        SoldAtUtc = s.SoldAtUtc,
                        IsPlayerOwned = s.IsPlayerOwned,
                        PlayerAircraftId = s.PlayerAircraftId
                    });
                }

                _allOffers = generatedOffers;
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

        public async Task LoadPlayerAircraftsForSaleAsync()
        {
            _context.ChangeTracker.Clear(); // Contexto de longa duração: descarta entidades em cache para ler o estado atual do banco
            try
            {
                var pilot = await _context.Pilots
                    .Include(p => p.Hangar)
                    .ThenInclude(a => a.AircraftType)
                    .FirstOrDefaultAsync(p => p.Name == _currentPilotName);

                if (pilot?.Hangar != null)
                {
                    var playerListings = await _marketService.GetPlayerActiveListingsAsync(_currentPilotName);

                    var saleItems = new List<PlayerAircraftSaleItem>();
                    foreach (var a in pilot.Hangar)
                    {
                        int year = a.PurchaseDate != default && a.PurchaseDate.Year > 1900 ? a.PurchaseDate.Year : 2018;
                        double baseVal = a.AircraftType != null
                            ? _marketService.CalculateBaseMarketValue(
                                a.AircraftType,
                                a.EngineCondition,
                                a.TiresCondition,
                                a.AirframeCondition,
                                a.AvionicsCondition,
                                year,
                                a.CurrentAirframeHours)
                            : 0.0;

                        var existingListing = playerListings.FirstOrDefault(l => l.PlayerAircraftId == a.Id);

                        saleItems.Add(new PlayerAircraftSaleItem
                        {
                            Aircraft = a,
                            CalculatedBaseValue = baseVal,
                            AskingPrice = existingListing != null ? existingListing.AskingPrice : baseVal,
                            IsListed = existingListing != null,
                            ListingId = existingListing?.Id
                        });
                    }

                    PlayerAircraftsForSale = new ObservableCollection<PlayerAircraftSaleItem>(saleItems);
                    if (SelectedPlayerAircraft == null || !PlayerAircraftsForSale.Contains(SelectedPlayerAircraft))
                    {
                        SelectedPlayerAircraft = PlayerAircraftsForSale.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarketViewModel] Erro ao carregar frota para venda: {ex.Message}");
            }
        }

        public string BuyButtonText
        {
            get
            {
                if (SelectedOffer == null) return "SELECIONE UMA AERONAVE";
                if (SelectedOffer.IsSold) return "AERONAVE VENDIDA (INDISPONÍVEL)";
                if (UserBalance < SelectedOffer.SalePrice) return "SALDO INSUFICIENTE";
                return $"COMPRAR POR {SelectedOffer.SalePrice:C2}";
            }
        }

        public string SellButtonText
        {
            get
            {
                if (SelectedPlayerAircraft == null) return "SELECIONE UMA AERONAVE DA SUA FROTA";
                return $"VENDER AERONAVE POR {SelectedPlayerAircraft.NetValue:C2}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanBuy))]
        private async Task Buy()
        {
            _context.ChangeTracker.Clear(); // Contexto de longa duração: descarta entidades em cache para ler o estado atual do banco
            if (SelectedOffer == null) return;

            if (SelectedOffer.IsSold)
            {
                MessageBox.Show("Esta aeronave já foi arrematada por outro comprador!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Confirmar a compra do {SelectedOffer.BaseType?.Model} ({SelectedOffer.Prefix}) por {SelectedOffer.SalePrice:C2}?", 
                "Mercado de Aeronaves", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var (success, message, aircraft) = await _marketService.BuyMarketListingAsync(SelectedOffer.Id, _currentPilotName);

                if (!success)
                {
                    MessageBox.Show(message, "Falha na Transação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Atualizar Saldo do Piloto
                var pilot = await _context.Pilots.FirstOrDefaultAsync(p => p.Name == _currentPilotName);
                if (pilot != null)
                {
                    UserBalance = pilot.Balance;
                }

                MessageBox.Show(message, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Remover ou atualizar status da oferta
                var offerToRemove = SelectedOffer;
                SelectedOffer = null;
                if (offerToRemove != null)
                {
                    _allOffers.Remove(offerToRemove);
                    Offers.Remove(offerToRemove);
                }

                OnPropertyChanged(nameof(BuyButtonText));

                // Se a aba de venda estiver ativa, atualiza
                if (SelectedTab == 1)
                {
                    await LoadPlayerAircraftsForSaleAsync();
                }

                // Atualizar MainWindow se estiver aberta
                if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVM && pilot != null)
                {
                    await mainVM.InitializeForPilotAsync(pilot.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha na transação: {ex.Message}");
            }
        }

        private bool CanBuy => SelectedOffer != null && !SelectedOffer.IsSold && UserBalance >= SelectedOffer.SalePrice;

        [RelayCommand]
        public Task Sell(object? parameter) => SellAircraft(parameter);

        [RelayCommand]
        public async Task SellAircraft(object? parameter)
        {
            _context.ChangeTracker.Clear(); // Contexto de longa duração: descarta entidades em cache para ler o estado atual do banco
            PilotAircraft? aircraft = null;

            if (parameter is PlayerAircraftSaleItem item)
            {
                aircraft = item.Aircraft;
            }
            else if (parameter is PilotAircraft pa)
            {
                aircraft = pa;
            }
            else if (SelectedPlayerAircraft != null)
            {
                aircraft = SelectedPlayerAircraft.Aircraft;
            }

            if (aircraft == null)
            {
                MessageBox.Show("Nenhuma aeronave selecionada para venda.");
                return;
            }

            var pilot = await _context.Pilots
                .Include(p => p.Hangar)
                .ThenInclude(a => a.AircraftType)
                .FirstOrDefaultAsync(p => p.Name == _currentPilotName);

            if (pilot == null)
            {
                MessageBox.Show("Piloto não encontrado.");
                return;
            }

            int year = aircraft.PurchaseDate != default && aircraft.PurchaseDate.Year > 1900 ? aircraft.PurchaseDate.Year : 2018;
            double grossValue = aircraft.AircraftType != null
                ? _marketService.CalculateBaseMarketValue(
                    aircraft.AircraftType,
                    aircraft.EngineCondition,
                    aircraft.TiresCondition,
                    aircraft.AirframeCondition,
                    aircraft.AvionicsCondition,
                    year,
                    aircraft.CurrentAirframeHours)
                : 0.0;

            if (grossValue <= 0)
            {
                double avgCondition = (aircraft.EngineCondition + aircraft.TiresCondition + aircraft.AirframeCondition + aircraft.AvionicsCondition) / 4.0;
                double basePrice = aircraft.AircraftType?.PurchasePrice ?? 0;
                grossValue = Math.Round(basePrice * (avgCondition / 100.0) * 0.9, 2);
            }

            double brokerFee = Math.Round(grossValue * 0.08, 2);
            double netAmount = Math.Round(grossValue - brokerFee, 2);

            var confirm = MessageBox.Show(
                $"Confirmar a LIQUIDAÇÃO IMEDIATA da aeronave {aircraft.AircraftType?.Model} ({aircraft.Prefix})?\n\n" +
                $"• Avaliação de Mercado Justo: ${grossValue:N2}\n" +
                $"• Taxa de Corretagem (8%): -${brokerFee:N2}\n" +
                $"• Valor Líquido a Receber: ${netAmount:N2}\n\n" +
                $"A aeronave será removida da frota da sua empresa com crédito imediato em conta.",
                "Confirmação de Venda de Aeronave",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // Se houver anúncio ativo no mercado, cancela
                var activeListing = await _context.MarketListings
                    .FirstOrDefaultAsync(l => l.PlayerAircraftId == aircraft.Id && !l.IsSold);
                if (activeListing != null)
                {
                    _context.MarketListings.Remove(activeListing);
                }

                // Se for a aeronave ativa, desvincula
                if (pilot.ActiveAircraftId == aircraft.Id)
                {
                    pilot.ActiveAircraftId = null;
                }

                // Creditar saldo líquido
                pilot.Balance += netAmount;
                UserBalance = pilot.Balance;

                // Registrar transação financeira de receita
                _context.FinancialTransactions.Add(new FinancialTransaction
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    Date = DateTime.Now,
                    Amount = netAmount,
                    Type = TransactionType.Income,
                    Category = "Venda de Aeronave",
                    Description = $"Liquidação de Aeronave: {aircraft.AircraftType?.Model ?? "Aeronave"} ({aircraft.Prefix})"
                });

                // Remover do banco
                var entityToRemove = await _context.PilotAircrafts.FirstOrDefaultAsync(a => a.Id == aircraft.Id);
                if (entityToRemove != null)
                {
                    _context.PilotAircrafts.Remove(entityToRemove);
                }

                await _context.SaveChangesAsync();

                MessageBox.Show($"Venda concluída com sucesso!\n\nForam creditados ${netAmount:N2} no caixa da sua empresa.", "Venda Realizada", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadPlayerAircraftsForSaleAsync();
                OnPropertyChanged(nameof(BuyButtonText));
                OnPropertyChanged(nameof(SellButtonText));

                // Atualizar MainWindow se estiver aberta
                if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
                {
                    await mainVM.InitializeForPilotAsync(pilot.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao processar a venda: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task ListOnMarket(PlayerAircraftSaleItem? item)
        {
            var target = item ?? SelectedPlayerAircraft;
            if (target == null || target.Aircraft == null)
            {
                MessageBox.Show("Selecione uma aeronave para anunciar.");
                return;
            }

            double asking = target.AskingPrice > 0 ? target.AskingPrice : target.GrossValue;

            var (success, message, listing) = await _marketService.ListPlayerAircraftAsync(target.Aircraft.Id, _currentPilotName, asking);
            if (!success)
            {
                MessageBox.Show(message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(message, "Anúncio Publicado", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadPlayerAircraftsForSaleAsync();
            await LoadOffersAsync();
        }

        [RelayCommand]
        public async Task CancelListing(PlayerAircraftSaleItem? item)
        {
            var target = item ?? SelectedPlayerAircraft;
            if (target == null || !target.ListingId.HasValue)
            {
                MessageBox.Show("Esta aeronave não possui anúncio ativo para cancelar.");
                return;
            }

            var (success, message) = await _marketService.CancelPlayerListingAsync(target.ListingId.Value, _currentPilotName);
            if (!success)
            {
                MessageBox.Show(message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(message, "Anúncio Cancelado", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadPlayerAircraftsForSaleAsync();
            await LoadOffersAsync();
        }
    }
}
