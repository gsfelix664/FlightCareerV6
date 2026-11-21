using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;

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

        /// <summary>Cor por saúde: vermelho &lt; 20, âmbar &lt; 40, senão verde.</summary>
        public static SolidColorBrush ForHealth(double value) =>
            value < 20 ? Red : value < 40 ? Amber : Green;

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
        public Geometry IconGeometry => HangarIcons.For(Kind);

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

        private PartReplacementPlan? _replacementPlan;
        public PartReplacementPlan? ReplacementPlan => _replacementPlan;

        public void SetReplacementPlan(PartReplacementPlan? plan)
        {
            _replacementPlan = plan;
            OnPropertyChanged(nameof(ReplacementPlan));
            OnPropertyChanged(nameof(RepairCost));
            OnPropertyChanged(nameof(CanRepair));
            OnPropertyChanged(nameof(RepairButtonText));
        }

        public double RepairCost => _replacementPlan != null
            ? _replacementPlan.TotalCost
            : ComponentMaintenance.RepairCost(Definition, Component, _purchasePrice);

        public bool CanRepair => _replacementPlan != null
            ? _replacementPlan.Items.Count > 0
            : RepairCost > 0;

        public string RepairButtonText
        {
            get
            {
                if (_replacementPlan != null)
                {
                    if (_replacementPlan.Items.Count == 0) return "Em dia";
                    string pecas = _replacementPlan.Items.Count == 1 ? "1 peça" : $"{_replacementPlan.Items.Count} peças";
                    return $"Trocar {pecas} · {RepairCost:C0}";
                }

                return CanRepair ? $"Reparar · {RepairCost:C0}" : "Em dia";
            }
        }

        public ObservableCollection<FlaggedPartViewModel> FlaggedParts { get; } = new();
        public bool HasFlaggedParts => FlaggedParts.Count > 0;

        public void SetFlaggedParts(IEnumerable<FlaggedPartViewModel> parts)
        {
            FlaggedParts.Clear();
            if (parts != null)
            {
                foreach (var p in parts) FlaggedParts.Add(p);
            }
            OnPropertyChanged(nameof(HasFlaggedParts));
        }

        public ObservableCollection<PartRowViewModel> Parts { get; } = new();
        public bool HasParts => Parts.Count > 0;
        public string PartsHeader => $"Peças ({Parts.Count})";

        public void SetParts(IEnumerable<PartRowViewModel> parts)
        {
            Parts.Clear();
            if (parts != null)
            {
                foreach (var p in parts) Parts.Add(p);
            }
            OnPropertyChanged(nameof(HasParts));
            OnPropertyChanged(nameof(PartsHeader));
        }

        public void Refresh(double airframeHours)
        {
            _airframeHours = airframeHours;
            OnPropertyChanged(string.Empty);
        }
    }

    /// <summary>Linha de peça no Hangar com saúde conhecida e troca individual.</summary>
    public sealed class PartRowViewModel
    {
        public PartHangarRow Row { get; }
        public AircraftPart Part => Row.Part;
        public PartDefinition Def => Row.Def;
        public string DisplayName => Row.DisplayName;
        public string HealthText => Row.HealthText;
        public string SeenText => Row.SeenText;
        public string? LifeNote => Row.LifeNote;
        public bool HasLifeNote => !string.IsNullOrEmpty(LifeNote);
        public SolidColorBrush Brush => Row.Band switch
        {
            PartKnownBand.Broken => HangarPalette.Red,
            PartKnownBand.Red => HangarPalette.Red,
            PartKnownBand.Yellow => HangarPalette.Amber,
            PartKnownBand.Green => HangarPalette.Green,
            _ => HangarPalette.Muted
        };
        public string ReplaceText { get; }
        public IRelayCommand<PartRowViewModel>? ReplaceCommand { get; }

        public PartRowViewModel(PartHangarRow row, string replaceText, IRelayCommand<PartRowViewModel>? replaceCommand = null)
        {
            Row = row ?? throw new ArgumentNullException(nameof(row));
            ReplaceText = replaceText;
            ReplaceCommand = replaceCommand;
        }
    }

    /// <summary>Peça com atenção para exibição de chip de aviso no card de componente.</summary>
    public sealed class FlaggedPartViewModel
    {
        public string Name { get; }
        public PartFlag Level { get; }
        public string Reason { get; }
        public SolidColorBrush Brush => Level == PartFlag.Broken ? HangarPalette.Red : HangarPalette.Amber;
        public string Icon => "!";

        public FlaggedPartViewModel(string name, PartFlag level, string reason)
        {
            Name = name;
            Level = level;
            Reason = reason;
        }
    }
}
