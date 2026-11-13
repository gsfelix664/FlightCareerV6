using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace FlightCareer.UI.ViewModels
{
    /// <summary>
    /// Aeronave do hangar para a UI: identificação, barras por componente (com saúde oculta),
    /// status, inspeções, abastecimento e seguro.
    /// </summary>
    public partial class PilotAircraftWrapper : ObservableObject
    {
        private readonly ComponentCatalog _catalog;

        public PilotAircraft Aircraft { get; }
        public EngineClass EngineClass { get; }
        public ObservableCollection<ComponentRowViewModel> Components { get; } = new();

        public PilotAircraftWrapper(PilotAircraft aircraft, ComponentCatalog catalog, IRelayCommand<ComponentRowViewModel> repairCommand)
        {
            Aircraft = aircraft;
            _catalog = catalog;
            EngineClass = catalog.ResolveEngineClass(aircraft.AircraftType);

            var order = catalog.Components.Select(d => d.Kind).ToList();
            foreach (var c in aircraft.Components.OrderBy(c => order.IndexOf(c.Kind)))
            {
                var def = catalog.Components.FirstOrDefault(d => d.Kind == c.Kind);
                if (def == null || !def.AppliesTo.Contains(EngineClass)) continue;
                Components.Add(new ComponentRowViewModel(c, def, catalog, EngineClass, PurchasePrice, aircraft.CurrentAirframeHours, repairCommand));
            }

            _targetFuel = aircraft.FuelAmount;
        }

        // --- Identificação ---
        public string Prefix => Aircraft.Prefix;
        public string Model => Aircraft.AircraftType?.Model ?? "N/A";
        public double PurchasePrice => Aircraft.AircraftType?.PurchasePrice ?? 0;
        public double MTOW => AircraftWeightSpecs.GetMtow(Aircraft.AircraftType?.Model, Aircraft.AircraftType);
        public double FuelCapacity => Aircraft.AircraftType?.FuelTankGallons ?? 0;
        public string FuelType => Aircraft.AircraftType?.FuelType ?? "AVGAS";
        public int MaxPax => Aircraft.AircraftType?.MaxPax ?? 0;
        public int CruiseSpeedKts => Aircraft.AircraftType?.CruiseSpeedKts ?? 0;
        public int RangeNM => Aircraft.AircraftType?.RangeNM ?? 0;
        public string Location => string.IsNullOrEmpty(Aircraft.CurrentLocationICAO) ? "—" : Aircraft.CurrentLocationICAO;
        public double AirframeHours => Aircraft.CurrentAirframeHours;
        public int LandingsCount => Aircraft.LandingsCount;
        public bool IsPiston => EngineClass == EngineClass.Piston;
        public string EngineClassText => EngineClass switch
        {
            EngineClass.Piston => "PISTÃO",
            EngineClass.Turboprop => "TURBOÉLICE",
            _ => "JATO"
        };
        public string Configuration => string.IsNullOrEmpty(Aircraft.Configuration) ? "Passageiro" : Aircraft.Configuration;
        public bool IsCargoConfig => Configuration.Contains("Carga");

        [ObservableProperty] private bool _isActive;

        // --- Saúde geral e status ---
        private AircraftHealthSummary Summary =>
            AircraftHealthSummary.Compute(Components.Select(r => r.Component), _catalog, EngineClass, Aircraft.CurrentAirframeHours);

        public string AirworthinessText => Summary.DisplayedAirworthiness.ToString();
        public string ConditionText => Summary.DisplayedCondition.ToString();
        public double AirworthinessMid => Summary.DisplayedAirworthiness.Mid;
        public SolidColorBrush AirworthinessBrush => HangarPalette.ForHealth(AirworthinessMid);
        public GridLength AirworthinessFill => new(Math.Max(0, AirworthinessMid), GridUnitType.Star);
        public GridLength AirworthinessEmpty => new(Math.Max(0, 100 - AirworthinessMid), GridUnitType.Star);

        private AircraftStatus Status
        {
            get
            {
                var status = AircraftStatusEvaluator.Evaluate(Aircraft, Components.Select(r => r.Component).ToList(), _catalog, EngineClass);
                if (status.Level is AircraftStatusLevel.Destroyed or AircraftStatusLevel.Interdicted) return status;

                // Panes persistentes abertas: NO-GO interdita; diferida rebaixa "pronta" para atenção
                var grounding = OpenDefects.Where(d => d.IsGrounding).ToList();
                if (grounding.Count > 0)
                    return new AircraftStatus(AircraftStatusLevel.Interdicted, "GROUNDED (PANE)",
                        "Item inoperante: " + string.Join(", ", grounding.Select(d => d.Title)) + ". Repare a pane na oficina.");
                if (OpenDefects.Count > 0 && status.Level is AircraftStatusLevel.Ready or AircraftStatusLevel.MaintenanceDue)
                    return new AircraftStatus(AircraftStatusLevel.Attention, "PANE DIFERIDA",
                        "Voo permitido com: " + string.Join(", ", OpenDefects.Select(d => d.Title)) + ".");
                return status;
            }
        }

        // --- Panes persistentes (discrepâncias abertas, addon ou simulador) ---
        public ObservableCollection<AircraftDefect> OpenDefects { get; } = new();
        public bool HasOpenDefects => OpenDefects.Count > 0;
        public bool HasGroundingDefects => OpenDefects.Any(d => d.IsGrounding);
        public double DefectsRepairTotal => OpenDefects.Sum(d => d.RepairCost);
        public string RepairAllDefectsText => $"Reparar todas as panes · {DefectsRepairTotal:C0}";

        public void SetOpenDefects(IEnumerable<AircraftDefect> defects)
        {
            OpenDefects.Clear();
            foreach (var d in defects) OpenDefects.Add(d);
            OnPropertyChanged(nameof(HasOpenDefects));
            OnPropertyChanged(nameof(HasGroundingDefects));
            OnPropertyChanged(nameof(DefectsRepairTotal));
            OnPropertyChanged(nameof(RepairAllDefectsText));
            OnPropertyChanged(nameof(StatusLevel));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDetail));
            OnPropertyChanged(nameof(StatusBrush));
        }

        public AircraftStatusLevel StatusLevel => Status.Level;
        public string StatusText => Status.Text;
        public string StatusDetail => Status.Detail;
        public SolidColorBrush StatusBrush => HangarPalette.ForStatus(Status.Level);
        public bool IsCrashed => Status.Level == AircraftStatusLevel.Destroyed;
        public bool CanService => !IsCrashed;

        public double TotalRepairCost => Components.Sum(c => c.RepairCost);
        public bool HasRepairs => TotalRepairCost > 0;
        public string RepairAllText => HasRepairs ? $"Reparar tudo · {TotalRepairCost:C0}" : "Nada a reparar";

        // --- Inspeções e TBO ---
        public double EngineTboRemainingHours => Math.Round(Aircraft.EngineTboRemainingHours, 1);
        public double HoursUntilNext50hInspection => Math.Round(Aircraft.HoursUntilNext50hInspection, 1);
        public double HoursUntilNext100hInspection => Math.Round(Aircraft.HoursUntilNext100hInspection, 1);
        public double HoursUntilNext500hInspection => Math.Round(Aircraft.HoursUntilNext500hInspection, 1);
        public bool IsInterdicted => Aircraft.IsInterdicted;
        public string InterdictionReason => Aircraft.InterdictionReason;

        public SolidColorBrush Brush50h => DueBrush(HoursUntilNext50hInspection, 5);
        public SolidColorBrush Brush100h => DueBrush(HoursUntilNext100hInspection, 10);
        public SolidColorBrush Brush500h => DueBrush(HoursUntilNext500hInspection, 10);
        public SolidColorBrush BrushTbo => DueBrush(EngineTboRemainingHours, 50);

        private static SolidColorBrush DueBrush(double remaining, double warn) =>
            remaining <= 0 ? HangarPalette.Red : remaining <= warn ? HangarPalette.Amber : HangarPalette.Green;

        public double EngineLifeHours => _catalog.Get(ComponentKind.Engine).LifeHoursFor(EngineClass);

        public double Cost50hInspection => 850.0;
        public double Cost100hInspection => 2500.0;
        public double Cost500hInspection => 1450.0;
        public double CostOverhaul => Math.Max(25000.0, Math.Round(PurchasePrice * 0.20, 2));
        public double CostToggleConfiguration => 1200.0;

        public string Text50h => $"Fazer 50 h · {Cost50hInspection:C0}";
        public string Text100h => $"Fazer 100 h · {Cost100hInspection:C0}";
        public string Text500h => $"Fazer 500 h · {Cost500hInspection:C0}";
        public string TextOverhaul => $"Retífica · {CostOverhaul:C0}";
        public string TextToggleConfiguration => $"Converter · {CostToggleConfiguration:C0}";
        public string OverhaulTooltip => $"Restaura o motor a 100% e o TBO para {EngineLifeHours:N0} h";

        // --- Abastecimento ---
        [ObservableProperty] private double _targetFuel;
        public double FuelAmount => Aircraft.FuelAmount;
        public double FuelPricePerGallon => FuelPriceService.GetCurrentPrice(FuelType);
        public double RefuelGallons => Math.Max(0, TargetFuel - Aircraft.FuelAmount);
        public double RefuelCost => RefuelGallons * FuelPricePerGallon;
        public bool CanRefuel => RefuelCost > 0.01 && !IsCrashed;
        public string RefuelButtonText => CanRefuel ? $"Abastecer {RefuelGallons:F1} gal · {RefuelCost:C0}" : "Ajuste o volume para abastecer";

        partial void OnTargetFuelChanged(double value)
        {
            if (value < Aircraft.FuelAmount) { TargetFuel = Aircraft.FuelAmount; return; }
            OnPropertyChanged(nameof(RefuelGallons));
            OnPropertyChanged(nameof(RefuelCost));
            OnPropertyChanged(nameof(CanRefuel));
            OnPropertyChanged(nameof(RefuelButtonText));
        }

        // --- Seguro ---
        public double InsurancePremium => InsuranceService.CalculateMonthlyPremium(PurchasePrice);
        public double InsuranceDeductible => InsuranceService.CalculateDeductible(PurchasePrice);
        public double InsurancePayout => InsuranceService.CalculatePayout(PurchasePrice);
        public bool IsInsured => Aircraft.InsuranceExpirationDate > DateTime.Now;
        public string InsuranceStatusText => IsInsured ? "ATIVO" : "INATIVO";
        public SolidColorBrush InsuranceBrush => IsInsured ? HangarPalette.Green : HangarPalette.Red;
        public string InsuranceExpiryText => IsInsured
            ? $"vence em {Aircraft.InsuranceExpirationDate:dd/MM/yyyy} ({(Aircraft.InsuranceExpirationDate!.Value - DateTime.Now).TotalDays:F0} dias)"
            : "sem cobertura";
        public string InsuranceButtonText => IsInsured ? $"Renovar +30 dias · {InsurancePremium:C0}" : $"Contratar 30 dias · {InsurancePremium:C0}";

        /// <summary>Recalcula tudo após um serviço ou recarga.</summary>
        public void Refresh()
        {
            foreach (var row in Components) row.Refresh(Aircraft.CurrentAirframeHours);
            if (TargetFuel < Aircraft.FuelAmount) TargetFuel = Aircraft.FuelAmount;
            OnPropertyChanged(string.Empty);
        }
    }
}
