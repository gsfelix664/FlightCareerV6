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

        // Filtros rápidos e Ordenação
        [ObservableProperty] private string _selectedCategoryFilter = "TODAS";
        [ObservableProperty] private string _selectedSortOption = "MENOR PREÇO";
        public IReadOnlyList<string> SortOptions { get; } = new[]
        {
            "MENOR PREÇO",
            "MAIOR CONDIÇÃO",
            "MENOS HORAS (TTAF)"
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
                "MAIOR CONDIÇÃO" => query.OrderByDescending(o => o.AverageCondition),
                "MENOS HORAS (TTAF)" => query.OrderBy(o => o.AirframeHours),
                _ => query.OrderBy(o => o.SalePrice) // Padrão: "MENOR PREÇO"
            };

            var filtered = query.ToList();
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
                        var type = (i < shuffledTypes.Count)
                            ? shuffledTypes[i]
                            : aircraftTypes[_random.Next(aircraftTypes.Count)];

                        // Gerar condições (entre 40% e 100%)
                        double engine = _random.Next(60, 101);
                        double tires = _random.Next(40, 101);
                        double airframe = _random.Next(70, 101);
                        double avionics = _random.Next(50, 101);

                        double avg = (engine + tires + airframe + avionics) / 4.0;

                        // Preço: Base * (Condição Média / 100) * Fator de Mercado (0.8 a 1.1)
                        double marketFactor = 0.8 + (_random.NextDouble() * 0.3);
                        double aircraftValue = type.PurchasePrice * (avg / 100.0) * marketFactor;

                        // Valor do combustível (Cotação diária)
                        double fuelAmount = Math.Round(_random.NextDouble() * type.FuelTankGallons, 1);
                        double currentFuelPrice = FuelPriceService.GetCurrentPrice(type.FuelType);
                        double fuelValue = fuelAmount * currentFuelPrice;

                        int airframeHours = _random.Next(150, 9500);
                        int year = _random.Next(1978, 2019);
                        double tbo = (type.FuelType?.Contains("JET", StringComparison.OrdinalIgnoreCase) == true ||
                                      type.Type?.Contains("Turboprop", StringComparison.OrdinalIgnoreCase) == true) ? 3600.0 : 1800.0;
                        double tsmo = Math.Round(_random.NextDouble() * Math.Min(tbo, airframeHours % tbo == 0 ? tbo : airframeHours % tbo), 1);

                        string incident = airframe switch
                        {
                            < 75 => "Histórico de pouso duro registrado em diário de bordo (reparado)",
                            < 85 => "Pequeno reparo de bordo de ataque registrado em caderneta",
                            _ => "Sem histórico de incidentes estruturais"
                        };

                        generatedOffers.Add(new MarketOffer
                        {
                            BaseType = type,
                            Prefix = GeneratePrefix(),
                            EngineCondition = engine,
                            TiresCondition = tires,
                            AirframeCondition = airframe,
                            AvionicsCondition = avionics,
                            AirframeHours = airframeHours,
                            YearOfManufacture = year,
                            TboHours = tbo,
                            TsmoHours = tsmo,
                            StructuralIncidentHistory = incident,
                            CategoryBadge = DetermineCategoryBadge(type),
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

        public async Task LoadPlayerAircraftsForSaleAsync()
        {
            try
            {
                var pilot = await _context.Pilots
                    .Include(p => p.Hangar)
                    .ThenInclude(a => a.AircraftType)
                    .FirstOrDefaultAsync(p => p.Name == _currentPilotName);

                if (pilot?.Hangar != null)
                {
                    var saleItems = pilot.Hangar.Select(a => new PlayerAircraftSaleItem
                    {
                        Aircraft = a
                    }).ToList();

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

        private string DetermineCategoryBadge(AircraftType type)
        {
            var model = type.Model?.ToUpperInvariant() ?? string.Empty;
            var typeDesc = type.Type?.ToUpperInvariant() ?? string.Empty;
            var fuel = type.FuelType?.ToUpperInvariant() ?? string.Empty;

            if (fuel.Contains("JET") && (type.CruiseSpeedKts > 320 || model.Contains("CITATION") || model.Contains("A320") || model.Contains("737") || model.Contains("CJ4") || typeDesc.Contains("JET")))
            {
                return "Jato";
            }

            if (typeDesc.Contains("TURBOPROP") || model.Contains("KING AIR") || model.Contains("CARAVAN") || model.Contains("TBM") || model.Contains("PC-12"))
            {
                return "Turboélice";
            }

            if (typeDesc.Contains("MEP") || model.Contains("BARON") || model.Contains("SENECA") || model.Contains("TWIN") || model.Contains("DA62") || model.Contains("DUCHESS"))
            {
                return "Bimotor Pistão";
            }

            if (model.Contains("X-CUB") || model.Contains("SAVANNAH") || model.Contains("STOL") || type.MinRunwayLength < 800)
            {
                return "STOL / Utilitário";
            }

            return "Monomotor Pistão";
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
            if (SelectedOffer == null) return;

            var result = MessageBox.Show($"Confirmar a compra do {SelectedOffer.BaseType.Model} ({SelectedOffer.Prefix}) por {SelectedOffer.SalePrice:C2}?", 
                "Mercado de Aeronaves", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
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

                // 1. Deduzir Saldo
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
                    AvionicsCondition = SelectedOffer.AvionicsCondition,
                    CurrentAirframeHours = SelectedOffer.AirframeHours,
                    FuelAmount = SelectedOffer.FuelAmount,
                    CurrentLocationICAO = pilot.CurrentAirportICAO,
                    EngineTboRemainingHours = Math.Max(100, SelectedOffer.TboHours - SelectedOffer.TsmoHours)
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

                // Se a aba de venda estiver ativa, atualiza
                if (SelectedTab == 1)
                {
                    await LoadPlayerAircraftsForSaleAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha na transação: {ex.Message}");
            }
        }

        private bool CanBuy => SelectedOffer != null && UserBalance >= SelectedOffer.SalePrice;

        [RelayCommand]
        public async Task SellAircraft(object? parameter)
        {
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

            double avgCondition = (aircraft.EngineCondition + aircraft.TiresCondition + aircraft.AirframeCondition + aircraft.AvionicsCondition) / 4.0;
            double basePrice = aircraft.AircraftType?.PurchasePrice ?? 0;
            double grossValue = Math.Round(basePrice * (avgCondition / 100.0) * 0.9, 2);
            double brokerFee = Math.Round(grossValue * 0.08, 2);
            double netAmount = Math.Round(grossValue - brokerFee, 2);

            var confirm = MessageBox.Show(
                $"Confirmar a VENDA da aeronave {aircraft.AircraftType?.Model} ({aircraft.Prefix})?\n\n" +
                $"• Avaliação Bluebook Bruta: ${grossValue:N2}\n" +
                $"• Taxa de Corretagem (8%): -${brokerFee:N2}\n" +
                $"• Valor Líquido a Receber: ${netAmount:N2}\n\n" +
                $"A aeronave será removida da frota da sua empresa.",
                "Confirmação de Venda de Aeronave",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
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
                    Description = $"Venda de Aeronave: {aircraft.AircraftType?.Model ?? "Aeronave"} ({aircraft.Prefix})"
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
    }
}
