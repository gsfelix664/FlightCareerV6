using System;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.UI.ViewModels
{
    /// <summary>Paleta "Avionics Dark" usada no Hangar (mesma do resto do app).</summary>
    public static class HangarPalette
    {
        public static readonly SolidColorBrush Green = Freeze(0x00, 0xE6, 0x76);
        public static readonly SolidColorBrush Amber = Freeze(0xFF, 0xB3, 0x00);
        public static readonly SolidColorBrush Orange = Freeze(0xFF, 0x70, 0x43);
        public static readonly SolidColorBrush Red = Freeze(0xFF, 0x52, 0x52);
        public static readonly SolidColorBrush Cyan = Freeze(0x00, 0xE5, 0xFF);
        public static readonly SolidColorBrush Muted = Freeze(0x52, 0x63, 0x75);
        public static readonly SolidColorBrush Secondary = Freeze(0x8B, 0x9B, 0xAE);

        /// <summary>Cor por saúde: verde ≥ 60, âmbar 30–60, laranja 20–30 (sintomas), vermelho &lt; 20 (risco de pane).</summary>
        public static SolidColorBrush ForHealth(double value) =>
            value < 20 ? Red : value < 30 ? Orange : value < 60 ? Amber : Green;

        public static SolidColorBrush ForStatus(AircraftStatusLevel level) => level switch
        {
            AircraftStatusLevel.Ready => Green,
            AircraftStatusLevel.MaintenanceDue => Cyan,
            AircraftStatusLevel.Attention => Amber,
            AircraftStatusLevel.FailureRisk => Red,
            AircraftStatusLevel.Interdicted => Red,
            _ => Muted
        };

        private static SolidColorBrush Freeze(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }

    /// <summary>Uma barra de componente no Hangar: faixa de saúde exibida, vida útil, orçamento e reparo.</summary>
    public partial class ComponentRowViewModel : ObservableObject
    {
        private readonly ComponentCatalog _catalog;
        private readonly EngineClass _engineClass;
        private readonly double _purchasePrice;
        private double _airframeHours;

        public ComponentRowViewModel(AircraftComponent component, ComponentDefinition definition, ComponentCatalog catalog,
            EngineClass engineClass, double purchasePrice, double airframeHours, IRelayCommand<ComponentRowViewModel> repairCommand)
        {
            Component = component;
            Definition = definition;
            _catalog = catalog;
            _engineClass = engineClass;
            _purchasePrice = purchasePrice;
            _airframeHours = airframeHours;
            RepairCommand = repairCommand;
        }

        public AircraftComponent Component { get; }
        public ComponentDefinition Definition { get; }
        public IRelayCommand<ComponentRowViewModel> RepairCommand { get; }

        public ComponentKind Kind => Component.Kind;
        public string Name => Definition.Name;
        public string ServiceName => string.IsNullOrEmpty(Definition.ServiceName) ? "Reparo" : Definition.ServiceName;
        public bool IsCritical => Definition.Critical;

        public HealthRange Range => HealthVisibility.GetDisplayedRange(Component, _airframeHours, _catalog.Visibility);
        public bool IsExact => Range.IsExact;
        public string RangeText => Range.ToString();
        public SolidColorBrush Brush => HangarPalette.ForHealth(Range.Mid);

        // Barra: [preenchido 0..Min][faixa Min..Max][vazio Max..100]
        public GridLength FillWidth => new(Math.Max(0, Range.Min), GridUnitType.Star);
        public GridLength BandWidth => new(Math.Max(0, Range.Max - Range.Min), GridUnitType.Star);
        public GridLength EmptyWidth => new(Math.Max(0, 100 - Range.Max), GridUnitType.Star);

        public string VisibilityText
        {
            get
            {
                if (IsExact) return "valor exato (inspecionado)";
                double since = Math.Max(0, _airframeHours - Component.LastInspectionAirframeHours);
                return $"estimativa · inspecionado há {since:F0} h";
            }
        }

        public string LifeText
        {
            get
            {
                var (remaining, life, isCycles) = ComponentMaintenance.RemainingLife(Definition, Component, _engineClass);
                if (life <= 0) return string.Empty;
                string unit = isCycles ? "pousos" : "h";
                return remaining <= 0
                    ? $"vida útil vencida há {-remaining:F0} {unit}"
                    : $"{remaining:F0} {unit} até a revisão (de {life:F0})";
            }
        }

        public SolidColorBrush LifeBrush
        {
            get
            {
                var (remaining, life, _) = ComponentMaintenance.RemainingLife(Definition, Component, _engineClass);
                if (life <= 0) return HangarPalette.Secondary;
                return remaining <= 0 ? HangarPalette.Red : remaining < life * 0.1 ? HangarPalette.Amber : HangarPalette.Secondary;
            }
        }

        public double RepairCost => ComponentMaintenance.RepairCost(Definition, Component, _purchasePrice);
        public bool CanRepair => RepairCost > 0;
        public string RepairButtonText => CanRepair ? $"Reparar · {RepairCost:C0}" : "Em dia";

        public void Refresh(double airframeHours)
        {
            _airframeHours = airframeHours;
            OnPropertyChanged(string.Empty);
        }
    }
}
