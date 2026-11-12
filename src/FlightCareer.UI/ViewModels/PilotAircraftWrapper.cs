using CommunityToolkit.Mvvm.ComponentModel;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Services;
using System;
using System.Windows.Media;

namespace FlightCareer.UI.ViewModels
{
    public partial class PilotAircraftWrapper : ObservableObject
    {
        public PilotAircraft Aircraft { get; }

        public PilotAircraftWrapper(PilotAircraft aircraft)
        {
            Aircraft = aircraft;
            ResetOperations();
        }

        public void ResetOperations()
        {
            TargetEngineCondition = Aircraft.EngineCondition;
            TargetTiresCondition = Aircraft.TiresCondition;
            TargetAirframeCondition = Aircraft.AirframeCondition;
            TargetAvionicsCondition = Aircraft.AvionicsCondition;
            TargetFuel = Aircraft.FuelAmount;
            
            OnPropertyChanged(nameof(RepairCostMotor));
            OnPropertyChanged(nameof(RepairCostPneus));
            OnPropertyChanged(nameof(RepairCostFuselagem));
            OnPropertyChanged(nameof(RepairCostAvionicos));
            OnPropertyChanged(nameof(RefuelCost));
            OnPropertyChanged(nameof(TotalOperationCost));
            OnPropertyChanged(nameof(IsInsured));
            OnPropertyChanged(nameof(EngineTboRemainingHours));
            OnPropertyChanged(nameof(HoursUntilNext50hInspection));
            OnPropertyChanged(nameof(HoursUntilNext100hInspection));
            OnPropertyChanged(nameof(HoursUntilNext500hInspection));
            OnPropertyChanged(nameof(MagnetosCondition));
            OnPropertyChanged(nameof(MagnetosColor));
            OnPropertyChanged(nameof(OilCondition));
            OnPropertyChanged(nameof(OilColor));
            OnPropertyChanged(nameof(Cost500hInspection));
            OnPropertyChanged(nameof(IsInterdicted));
            OnPropertyChanged(nameof(InterdictionReason));
            OnPropertyChanged(nameof(Configuration));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
        }

        public string Prefix => Aircraft.Prefix;
        public string Model => Aircraft.AircraftType?.Model ?? "N/A";
        public double PurchasePrice => Aircraft.AircraftType?.PurchasePrice ?? 0;
        public double MTOW => AircraftWeightSpecs.GetMtow(Aircraft.AircraftType?.Model, Aircraft.AircraftType);
        public double ZFW => AircraftWeightSpecs.GetEmptyWeight(Aircraft.AircraftType?.Model, Aircraft.AircraftType);
        public double FuelCapacity => Aircraft.AircraftType?.FuelTankGallons ?? 0;
        public double Hobbs => Aircraft.CurrentAirframeHours;
        public string FuelType => Aircraft.AircraftType?.FuelType ?? "AVGAS";

        // Manutenção Regulatória & TBO
        public double EngineTboRemainingHours => Math.Round(Aircraft.EngineTboRemainingHours, 1);
        public double HoursUntilNext50hInspection => Math.Round(Aircraft.HoursUntilNext50hInspection, 1);
        public double HoursUntilNext100hInspection => Math.Round(Aircraft.HoursUntilNext100hInspection, 1);
        public double HoursUntilNext500hInspection => Math.Round(Aircraft.HoursUntilNext500hInspection, 1);
        public double MagnetosCondition => Math.Round(Aircraft.MagnetosCondition, 1);
        public SolidColorBrush MagnetosColor => GetConditionColor(MagnetosCondition);
        public double OilCondition => Math.Round(Aircraft.OilCondition, 1);
        public SolidColorBrush OilColor => GetConditionColor(OilCondition);
        public bool IsInterdicted => Aircraft.IsInterdicted;
        public string InterdictionReason => Aircraft.InterdictionReason;
        public string Configuration => string.IsNullOrEmpty(Aircraft.Configuration) ? "Passageiro" : Aircraft.Configuration;
        public bool IsCargoConfig => Configuration.Contains("Carga");

        // Custos Regulatórios
        public double Cost50hInspection => 850.0;
        public double Cost100hInspection => 2500.0;
        public double Cost500hInspection => 1450.0;
        public double CostOverhaul => Math.Max(25000.0, Math.Round(PurchasePrice * 0.20, 2));
        public double CostToggleConfiguration => 1200.0;

        // Estado do Heatmap
        public double EngineCondition => Aircraft.EngineCondition;
        public double TiresCondition => Aircraft.TiresCondition;
        public double AirframeCondition => Aircraft.AirframeCondition;
        public double AvionicsCondition => Aircraft.AvionicsCondition;

        public SolidColorBrush EngineColor => GetConditionColor(EngineCondition);
        public SolidColorBrush TiresColor => GetConditionColor(TiresCondition);
        public SolidColorBrush AirframeColor => GetConditionColor(AirframeCondition);
        public SolidColorBrush AvionicsColor => GetConditionColor(AvionicsCondition);

        // Sliders de Operação
        [ObservableProperty] private double _targetEngineCondition;
        [ObservableProperty] private double _targetTiresCondition;
        [ObservableProperty] private double _targetAirframeCondition;
        [ObservableProperty] private double _targetAvionicsCondition;
        [ObservableProperty] private double _targetFuel;

        [ObservableProperty] private bool _isActive;

        // Custos Dinâmicos
        public double RepairCostMotor => MaintenanceService.CalculateRepairCost(PurchasePrice, "MOTOR", Math.Max(0, TargetEngineCondition - EngineCondition));
        public double RepairCostPneus => MaintenanceService.CalculateRepairCost(PurchasePrice, "PNEUS", Math.Max(0, TargetTiresCondition - TiresCondition));
        public double RepairCostFuselagem => MaintenanceService.CalculateRepairCost(PurchasePrice, "FUSELAGEM", Math.Max(0, TargetAirframeCondition - AirframeCondition));
        public double RepairCostAvionicos => MaintenanceService.CalculateRepairCost(PurchasePrice, "AVIONICOS", Math.Max(0, TargetAvionicsCondition - AvionicsCondition));
        
        public double FuelPricePerGallon => FuelPriceService.GetCurrentPrice(FuelType);
        public double RefuelCost => Math.Max(0, TargetFuel - Aircraft.FuelAmount) * FuelPricePerGallon;
        
        // Informações de Seguro
        public double InsurancePremium => InsuranceService.CalculateMonthlyPremium(PurchasePrice);
        public double InsuranceDeductible => InsuranceService.CalculateDeductible(PurchasePrice);
        public double InsurancePayout => InsuranceService.CalculatePayout(PurchasePrice);
        public bool IsInsured => Aircraft.InsuranceExpirationDate > DateTime.Now;

        public double TotalOperationCost => RepairCostMotor + RepairCostPneus + RepairCostFuselagem + RepairCostAvionicos + RefuelCost;

        // Status Tag
        public bool IsCrashed => EngineCondition <= 0 && TiresCondition <= 0 && AirframeCondition <= 0 && AvionicsCondition <= 0;

        public string StatusText
        {
            get
            {
                if (IsCrashed) return "PERDA TOTAL (CRASH)";
                if (IsInterdicted) return $"INTERDITADA: {InterdictionReason}";
                if (EngineCondition < 30 || TiresCondition < 30 || AirframeCondition < 30 || MagnetosCondition < 30 || OilCondition < 30) return "GROUNDED (MANUT. CRÍTICA)";
                if (EngineCondition < 70 || TiresCondition < 70 || AirframeCondition < 70 || MagnetosCondition < 70 || OilCondition < 70 || HoursUntilNext50hInspection <= 5 || HoursUntilNext100hInspection <= 10 || HoursUntilNext500hInspection <= 10 || EngineTboRemainingHours <= 50) return "MANUTENÇÃO NECESSÁRIA";
                return "PRONTA PARA VOO";
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                if (IsCrashed) return new SolidColorBrush(Color.FromRgb(0, 0, 0)); // Preto para Perda Total
                if (IsInterdicted || StatusText.StartsWith("GROUNDED")) return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Vermelho
                if (StatusText == "MANUTENÇÃO NECESSÁRIA") return new SolidColorBrush(Color.FromRgb(241, 196, 15)); // Amarelo
                return new SolidColorBrush(Color.FromRgb(46, 204, 113)); // Verde
            }
        }

        private SolidColorBrush GetConditionColor(double value)
        {
            if (value < 30) return new SolidColorBrush(Color.FromRgb(231, 76, 60));
            if (value < 70) return new SolidColorBrush(Color.FromRgb(241, 196, 15));
            return new SolidColorBrush(Color.FromRgb(46, 204, 113));
        }

        // Notificações de Reatividade
        partial void OnTargetEngineConditionChanged(double value) { if (value < EngineCondition) TargetEngineCondition = EngineCondition; OnPropertyChanged(nameof(RepairCostMotor)); OnPropertyChanged(nameof(TotalOperationCost)); }
        partial void OnTargetTiresConditionChanged(double value) { if (value < TiresCondition) TargetTiresCondition = TiresCondition; OnPropertyChanged(nameof(RepairCostPneus)); OnPropertyChanged(nameof(TotalOperationCost)); }
        partial void OnTargetAirframeConditionChanged(double value) { if (value < AirframeCondition) TargetAirframeCondition = AirframeCondition; OnPropertyChanged(nameof(RepairCostFuselagem)); OnPropertyChanged(nameof(TotalOperationCost)); }
        partial void OnTargetAvionicsConditionChanged(double value) { if (value < AvionicsCondition) TargetAvionicsCondition = AvionicsCondition; OnPropertyChanged(nameof(RepairCostAvionicos)); OnPropertyChanged(nameof(TotalOperationCost)); }
        partial void OnTargetFuelChanged(double value) { if (value < Aircraft.FuelAmount) TargetFuel = Aircraft.FuelAmount; OnPropertyChanged(nameof(RefuelCost)); OnPropertyChanged(nameof(TotalOperationCost)); }

        public void Refresh()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}
