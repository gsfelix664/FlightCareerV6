using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FlightCareer.UI.ViewModels
{
    public partial class HangarViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly IPilotRepository _pilotRepository;
        private Guid _pilotId;

        [ObservableProperty] private ObservableCollection<PilotAircraftWrapper> _fleet = new();
        [ObservableProperty] private PilotAircraftWrapper? _selectedAircraft;
        [ObservableProperty] private bool _isMonitoring;

        public HangarViewModel(AppDbContext context, IPilotRepository pilotRepository)
        {
            _context = context;
            _pilotRepository = pilotRepository;
        }

        public async Task InitializeAsync(Guid pilotId)
        {
            _pilotId = pilotId;
            await LoadFleetAsync();
        }

        public async Task LoadFleetAsync()
        {
            var pilot = await _context.Pilots
                .Include(p => p.Hangar)
                .ThenInclude(a => a.AircraftType)
                .FirstOrDefaultAsync(p => p.Id == _pilotId);

            if (pilot != null)
            {
                var wrappedFleet = pilot.Hangar.Select(a => new PilotAircraftWrapper(a)).ToList();
                
                var activeId = pilot.ActiveAircraftId;
                foreach (var wrapper in wrappedFleet)
                {
                    wrapper.IsActive = wrapper.Aircraft.Id == activeId;
                }

                Fleet = new ObservableCollection<PilotAircraftWrapper>(wrappedFleet);
                SelectedAircraft = Fleet.FirstOrDefault(a => a.Aircraft.Id == activeId) ?? Fleet.FirstOrDefault();
            }
        }

        partial void OnSelectedAircraftChanged(PilotAircraftWrapper? value)
        {
            value?.ResetOperations();
        }

        [RelayCommand]
        private async Task SetActive()
        {
            if (SelectedAircraft == null) return;
            if (IsMonitoring)
            {
                MessageBox.Show("Você não pode alterar a aeronave ativa durante um voo!");
                return;
            }

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot != null)
            {
                // SINCRONIA DE LOCALIZAÇÃO: O piloto vai para onde a aeronave está
                pilot.ActiveAircraftId = SelectedAircraft.Aircraft.Id;
                pilot.CurrentAirportICAO = SelectedAircraft.Aircraft.CurrentLocationICAO;
                
                await _context.SaveChangesAsync();

                // Atualizar estado visual na coleção
                foreach (var wrapper in Fleet)
                {
                    wrapper.IsActive = wrapper.Aircraft.Id == pilot.ActiveAircraftId;
                }
                
                MessageBox.Show($"{SelectedAircraft.Model} ({SelectedAircraft.Prefix}) definida como ativa.\nSua localização atual agora é {pilot.CurrentAirportICAO}.");
                
                // Notificar MainViewModel para recarregar missões (via evento ou refresh direto se possível)
                if (Application.Current.MainWindow.DataContext is MainViewModel mainVM)
                {
                    await mainVM.InitializeForPilotAsync(pilot.Name);
                }
            }
        }

        [RelayCommand]
        private async Task AuthorizeServices()
        {
            if (SelectedAircraft == null) return;
            if (SelectedAircraft.IsCrashed)
            {
                MessageBox.Show("Esta aeronave sofreu PERDA TOTAL e não pode ser reparada. Acione o seguro ou descarte-a.");
                return;
            }
            if (IsMonitoring)
            {
                MessageBox.Show("Você não pode realizar serviços durante um voo!");
                return;
            }

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;

            double totalCost = SelectedAircraft.TotalOperationCost;

            if (totalCost <= 0)
            {
                MessageBox.Show("Nenhum serviço selecionado ou valores zerados.");
                return;
            }

            if (pilot.Balance < totalCost)
            {
                MessageBox.Show($"Saldo insuficiente! Você precisa de {totalCost:C2}.");
                return;
            }

            var result = MessageBox.Show($"Deseja autorizar todos os serviços selecionados por {totalCost:C2}?", 
                "Confirmação de Orçamento", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes) return;

            // 1. Deduzir Saldo
            pilot.Balance -= totalCost;

            // 2. Atualizar Aeronave
            var aircraft = SelectedAircraft.Aircraft;

            // Reparos
            if (SelectedAircraft.RepairCostMotor > 0)
            {
                _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Manutenção", $"REPARO MOTOR: {aircraft.Prefix}", SelectedAircraft.RepairCostMotor));
                aircraft.EngineCondition = SelectedAircraft.TargetEngineCondition;
            }
            if (SelectedAircraft.RepairCostPneus > 0)
            {
                _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Manutenção", $"REPARO PNEUS: {aircraft.Prefix}", SelectedAircraft.RepairCostPneus));
                aircraft.TiresCondition = SelectedAircraft.TargetTiresCondition;
            }
            if (SelectedAircraft.RepairCostFuselagem > 0)
            {
                _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Manutenção", $"REPARO FUSELAGEM: {aircraft.Prefix}", SelectedAircraft.RepairCostFuselagem));
                aircraft.AirframeCondition = SelectedAircraft.TargetAirframeCondition;
            }

            // Combustível
            if (SelectedAircraft.RefuelCost > 0)
            {
                double gallons = SelectedAircraft.TargetFuel - aircraft.FuelAmount;
                _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Combustível", $"ABASTECIMENTO: {aircraft.Prefix} ({gallons:F1} GAL)", SelectedAircraft.RefuelCost));
                aircraft.FuelAmount = SelectedAircraft.TargetFuel;
            }

            await _context.SaveChangesAsync();
            
            SelectedAircraft.ResetOperations();
            SelectedAircraft.Refresh();

            MessageBox.Show("Serviços realizados com sucesso! Sua aeronave está pronta.");
        }

        [RelayCommand]
        private async Task Perform50hInspection()
        {
            if (SelectedAircraft == null) return;
            if (SelectedAircraft.IsCrashed)
            {
                MessageBox.Show("Aeronave destruída!");
                return;
            }
            if (IsMonitoring)
            {
                MessageBox.Show("Você não pode realizar manutenção durante um voo!");
                return;
            }

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;

            double cost = SelectedAircraft.Cost50hInspection;
            if (pilot.Balance < cost)
            {
                MessageBox.Show($"Saldo insuficiente! A inspeção de 50h custa {cost:C2}.");
                return;
            }

            var result = MessageBox.Show($"Deseja realizar a Inspeção Preventiva de 50 Horas para {SelectedAircraft.Prefix} por {cost:C2}?\n\nIsso revisará filtros, velas e fluidos do motor.",
                "Inspeção de 50 Horas", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            pilot.Balance -= cost;
            var aircraft = SelectedAircraft.Aircraft;
            aircraft.HoursUntilNext50hInspection = 50.0;

            if (aircraft.IsInterdicted && aircraft.InterdictionReason.Contains("50") && aircraft.HoursUntilNext100hInspection > 0 && aircraft.EngineTboRemainingHours > 0)
            {
                aircraft.IsInterdicted = false;
                aircraft.InterdictionReason = string.Empty;
            }

            _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Manutenção", $"INSPEÇÃO 50H: {aircraft.Prefix}", cost));
            await _context.SaveChangesAsync();

            SelectedAircraft.ResetOperations();
            SelectedAircraft.Refresh();
            MessageBox.Show("Inspeção de 50 horas concluída com sucesso!");
        }

        [RelayCommand]
        private async Task Perform100hInspection()
        {
            if (SelectedAircraft == null) return;
            if (SelectedAircraft.IsCrashed)
            {
                MessageBox.Show("Aeronave destruída!");
                return;
            }
            if (IsMonitoring)
            {
                MessageBox.Show("Você não pode realizar manutenção durante um voo!");
                return;
            }

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;

            double cost = SelectedAircraft.Cost100hInspection;
            if (pilot.Balance < cost)
            {
                MessageBox.Show($"Saldo insuficiente! A inspeção regulatória de 100h custa {cost:C2}.");
                return;
            }

            var result = MessageBox.Show($"Deseja realizar a Inspeção Obrigatória de 100 Horas para {SelectedAircraft.Prefix} por {cost:C2}?\n\nEsta inspeção zera tanto a contagem de 100h quanto a de 50h.",
                "Inspeção de 100 Horas", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            pilot.Balance -= cost;
            var aircraft = SelectedAircraft.Aircraft;
            aircraft.HoursUntilNext100hInspection = 100.0;
            aircraft.HoursUntilNext50hInspection = 50.0;

            if (aircraft.IsInterdicted && aircraft.EngineTboRemainingHours > 0)
            {
                aircraft.IsInterdicted = false;
                aircraft.InterdictionReason = string.Empty;
            }

            _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Manutenção", $"INSPEÇÃO REGULATÓRIA 100H: {aircraft.Prefix}", cost));
            await _context.SaveChangesAsync();

            SelectedAircraft.ResetOperations();
            SelectedAircraft.Refresh();
            MessageBox.Show("Inspeção regulatória de 100 horas concluída com sucesso! Aeronave liberada para voo.");
        }

        [RelayCommand]
        private async Task PerformOverhaul()
        {
            if (SelectedAircraft == null) return;
            if (SelectedAircraft.IsCrashed)
            {
                MessageBox.Show("Aeronave destruída!");
                return;
            }
            if (IsMonitoring)
            {
                MessageBox.Show("Você não pode realizar manutenção durante um voo!");
                return;
            }

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;

            double cost = SelectedAircraft.CostOverhaul;
            if (pilot.Balance < cost)
            {
                MessageBox.Show($"Saldo insuficiente! A retífica geral (Overhaul de TBO) custa {cost:C2}.");
                return;
            }

            var result = MessageBox.Show($"Deseja realizar o Overhaul Geral do Motor (Retífica de TBO) para {SelectedAircraft.Prefix} por {cost:C2}?\n\nIsso restaura o TBO para 1.800 horas e condição do motor para 100%.",
                "Overhaul de Motor (TBO)", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            pilot.Balance -= cost;
            var aircraft = SelectedAircraft.Aircraft;
            aircraft.EngineTboRemainingHours = 1800.0;
            aircraft.EngineCondition = 100.0;

            if (aircraft.IsInterdicted && aircraft.HoursUntilNext100hInspection > 0)
            {
                aircraft.IsInterdicted = false;
                aircraft.InterdictionReason = string.Empty;
            }

            _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Manutenção", $"RETÍFICA GERAL / OVERHAUL TBO: {aircraft.Prefix}", cost));
            await _context.SaveChangesAsync();

            SelectedAircraft.ResetOperations();
            SelectedAircraft.Refresh();
            MessageBox.Show("Overhaul concluído com sucesso! Motor retificado e TBO restaurado para 1.800h.");
        }

        [RelayCommand]
        private async Task ToggleConfiguration()
        {
            if (SelectedAircraft == null) return;
            if (SelectedAircraft.IsCrashed)
            {
                MessageBox.Show("Aeronave destruída!");
                return;
            }
            if (IsMonitoring)
            {
                MessageBox.Show("Você não pode alterar a configuração durante um voo!");
                return;
            }

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;

            double cost = SelectedAircraft.CostToggleConfiguration;
            if (pilot.Balance < cost)
            {
                MessageBox.Show($"Saldo insuficiente! A conversão de cabine custa {cost:C2}.");
                return;
            }

            string currentConfig = SelectedAircraft.Configuration;
            string newConfig = currentConfig.Contains("Carga") ? "Passageiro" : "Carga (Sem Assentos)";

            var result = MessageBox.Show($"Deseja converter a aeronave {SelectedAircraft.Prefix} de '{currentConfig}' para '{newConfig}' por {cost:C2}?",
                "Conversão de Configuração de Cabine", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            pilot.Balance -= cost;
            var aircraft = SelectedAircraft.Aircraft;
            aircraft.Configuration = newConfig;

            _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Manutenção", $"CONVERSÃO CABINE ({newConfig}): {aircraft.Prefix}", cost));
            await _context.SaveChangesAsync();

            SelectedAircraft.ResetOperations();
            SelectedAircraft.Refresh();
            MessageBox.Show($"Aeronave convertida com sucesso para configuração: {newConfig}!");
        }

        [RelayCommand]
        private async Task BuyInsurance()
        {
            if (SelectedAircraft == null) return;
            if (IsMonitoring)
            {
                MessageBox.Show("Você não pode contratar seguro durante um voo!");
                return;
            }

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;

            double premium = SelectedAircraft.InsurancePremium;

            if (pilot.Balance < premium)
            {
                MessageBox.Show($"Saldo insuficiente! O prêmio mensal é de {premium:C2}.");
                return;
            }

            var result = MessageBox.Show($"Deseja contratar seguro para {SelectedAircraft.Prefix} por 30 dias pelo valor de {premium:C2}?\n\nFranquia: {SelectedAircraft.InsuranceDeductible:C2}\nCobertura: {SelectedAircraft.InsurancePayout:C2}", 
                "Seguro Aeronáutico", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            pilot.Balance -= premium;
            
            var aircraft = SelectedAircraft.Aircraft;
            var baseDate = aircraft.InsuranceExpirationDate > DateTime.Now ? aircraft.InsuranceExpirationDate.Value : DateTime.Now;
            aircraft.InsuranceExpirationDate = baseDate.AddDays(30);

            _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Seguro", $"CONTRATAÇÃO SEGURO: {aircraft.Prefix}", premium));

            await _context.SaveChangesAsync();
            
            SelectedAircraft.ResetOperations();
            SelectedAircraft.Refresh();

            MessageBox.Show("Seguro contratado com sucesso!");
        }

        [RelayCommand]
        private async Task ClaimInsurance()
        {
            if (SelectedAircraft == null || !SelectedAircraft.IsCrashed) return;

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;

            if (!SelectedAircraft.IsInsured)
            {
                MessageBox.Show("Esta aeronave não estava segurada no momento do acidente. Você não tem direito a cobertura.");
                return;
            }

            double payout = SelectedAircraft.InsurancePayout;
            double deductible = SelectedAircraft.InsuranceDeductible;

            if (pilot.Balance < deductible)
            {
                MessageBox.Show($"Você precisa de {deductible:C2} em saldo para pagar a franquia e receber a cobertura de {payout:C2}.");
                return;
            }

            var result = MessageBox.Show($"Deseja acionar o seguro?\n\nPagamento de Franquia: -{deductible:C2}\nRecebimento de Cobertura: +{payout:C2}\n\nA aeronave será removida da sua frota.", 
                "Acionar Seguro", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Processar Financeiro
            pilot.Balance -= deductible;
            pilot.Balance += payout;

            _context.FinancialTransactions.Add(CreateTransaction(pilot.Id, "Seguro", $"PAGTO FRANQUIA: {SelectedAircraft.Prefix}", deductible));
            _context.FinancialTransactions.Add(new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                PilotId = pilot.Id,
                Date = DateTime.Now,
                Description = $"RECEBIMENTO SEGURO (PERDA TOTAL): {SelectedAircraft.Prefix}",
                Amount = payout,
                Type = TransactionType.Income,
                Category = "Seguro"
            });

            // Remover Aeronave
            var aircraft = await _context.PilotAircrafts.FindAsync(SelectedAircraft.Aircraft.Id);
            if (aircraft != null) _context.PilotAircrafts.Remove(aircraft);
            
            if (pilot.ActiveAircraftId == SelectedAircraft.Aircraft.Id) pilot.ActiveAircraftId = null;

            await _context.SaveChangesAsync();
            await LoadFleetAsync();

            MessageBox.Show($"Seguro processado com sucesso! {payout:C2} foram creditados em sua conta.");
        }

        [RelayCommand]
        private async Task ScrapAircraft()
        {
            if (SelectedAircraft == null || !SelectedAircraft.IsCrashed) return;

            var result = MessageBox.Show($"Deseja realmente DESCARTAR a aeronave {SelectedAircraft.Prefix}?\n\nEla será removida da frota permanentemente e você NÃO receberá nada por isso.", 
                "Descartar Aeronave", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result != MessageBoxResult.Yes) return;

            var aircraft = await _context.PilotAircrafts.FindAsync(SelectedAircraft.Aircraft.Id);
            if (aircraft != null) _context.PilotAircrafts.Remove(aircraft);

            var pilot = await _context.Pilots.FindAsync(_pilotId);
            if (pilot != null && pilot.ActiveAircraftId == SelectedAircraft.Aircraft.Id) pilot.ActiveAircraftId = null;

            await _context.SaveChangesAsync();
            await LoadFleetAsync();

            MessageBox.Show("Aeronave removida da frota.");
        }

        private FinancialTransaction CreateTransaction(Guid pilotId, string category, string desc, double amount)
        {
            return new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                PilotId = pilotId,
                Date = DateTime.Now,
                Category = category,
                Description = desc,
                Amount = amount,
                Type = TransactionType.Expense
            };
        }
    }
}
