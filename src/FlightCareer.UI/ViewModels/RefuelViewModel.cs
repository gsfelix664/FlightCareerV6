using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Services;
using System;
using System.Windows;

namespace FlightCareer.UI.ViewModels
{
    public partial class RefuelViewModel : ObservableObject
    {
        private readonly PilotAircraftWrapper _aircraftWrapper;
        private readonly Pilot _pilot;
        private readonly Infrastructure.Persistence.AppDbContext _context;

        [ObservableProperty] private double _currentFuel;
        [ObservableProperty] private double _maxFuel;
        [ObservableProperty] private string _fuelType;
        [ObservableProperty] private double _pricePerGallon;
        [ObservableProperty] private double _targetFuel;
        
        public double GallonsToBuy => Math.Max(0, TargetFuel - CurrentFuel);
        public double TotalCost => GallonsToBuy * PricePerGallon;
        public string BalanceInfo => $"Saldo disponível: {_pilot.Balance:C2}";

        public RefuelViewModel(PilotAircraftWrapper aircraftWrapper, Pilot pilot, Infrastructure.Persistence.AppDbContext context)
        {
            _aircraftWrapper = aircraftWrapper;
            _pilot = pilot;
            _context = context;

            CurrentFuel = aircraftWrapper.Aircraft.FuelAmount;
            MaxFuel = aircraftWrapper.Aircraft.AircraftType?.FuelTankGallons ?? 0;
            FuelType = aircraftWrapper.Aircraft.AircraftType?.FuelType ?? "AVGAS";
            PricePerGallon = FuelPriceService.GetCurrentPrice(FuelType);
            TargetFuel = CurrentFuel;
        }

        partial void OnTargetFuelChanged(double value)
        {
            if (value < CurrentFuel) TargetFuel = CurrentFuel;
            OnPropertyChanged(nameof(GallonsToBuy));
            OnPropertyChanged(nameof(TotalCost));
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ConfirmRefuel(Window window)
        {
            if (TotalCost > _pilot.Balance)
            {
                MessageBox.Show("Saldo insuficiente para este abastecimento!");
                return;
            }

            if (GallonsToBuy <= 0)
            {
                MessageBox.Show("A quantidade de combustível deve ser superior à atual!");
                return;
            }

            _pilot.Balance -= TotalCost;
            _aircraftWrapper.Aircraft.FuelAmount = TargetFuel;
            
            // Registrar transação financeira
            var transaction = new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                PilotId = _pilot.Id,
                Date = DateTime.Now,
                Description = $"REABASTECIMENTO: {_aircraftWrapper.Prefix} ({GallonsToBuy:F1} GAL {FuelType})",
                Amount = TotalCost,
                Type = TransactionType.Expense,
                Category = "Combustível"
            };

            _context.FinancialTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            MessageBox.Show($"Abastecimento concluído! Total: {TotalCost:C2}");
            window.DialogResult = true;
            window.Close();
        }
    }
}