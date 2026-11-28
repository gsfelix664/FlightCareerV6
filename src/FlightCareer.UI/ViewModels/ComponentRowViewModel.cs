using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;

namespace FlightCareer.UI.ViewModels
{
    /// <summary>Paleta "Avionics Dark" usada no Hangar (mesma do resto do app).</summary>
    public static class HangarPalette
    {
        public static readonly SolidColorBrush Green = Freeze(0x00, 0xE6, 0x76);
        /// <summary>Verde-limão: peça gasta mas boa (80–40 %), sem cara de alerta.</summary>
        public static readonly SolidColorBrush Lime = Freeze(0xB2, 0xE0, 0x3A);
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

        public static SolidColorBrush ForBand(PartKnownBand band) => band switch
        {
            PartKnownBand.Broken => Red,
            PartKnownBand.Red => Red,
            PartKnownBand.Yellow => Amber,
            PartKnownBand.Green => Green,
            _ => Muted
        };

        public static SolidColorBrush ForDisplayState(PartDisplayInfo info)
        {
            if (info == null) return Muted;
            return info.State switch
            {
                PartDisplayState.Broken => Red,
                PartDisplayState.Red => Red,
                PartDisplayState.Yellow => Amber,
                PartDisplayState.Green => Green,
                PartDisplayState.Worn => Lime,
                PartDisplayState.Uncertain => ForBand(info.LastKnownBand),
                _ => Muted
            };
        }

        public static SolidColorBrush ForDisplayState(PartDisplayState state) => state switch
        {
            PartDisplayState.Broken => Red,
            PartDisplayState.Red => Red,
            PartDisplayState.Yellow => Amber,
            PartDisplayState.Green => Green,
            PartDisplayState.Worn => Lime,
            PartDisplayState.Uncertain => Amber,
            _ => Muted
        };

        /// <summary>Contagem compacta por estado (só os presentes), do pior para o melhor. Vermelho e quebrado contam juntos.</summary>
        public static string CountsText(IReadOnlyDictionary<PartDisplayState, int> counts)
        {
            int Get(PartDisplayState s) => counts.TryGetValue(s, out int n) ? n : 0;
            var list = new List<string>();
            int red = Get(PartDisplayState.Broken) + Get(PartDisplayState.Red);
            if (red > 0) list.Add($"✖ {red}");
            if (Get(PartDisplayState.Yellow) > 0) list.Add($"⚠ {Get(PartDisplayState.Yellow)}");
            if (Get(PartDisplayState.Uncertain) > 0) list.Add($"? {Get(PartDisplayState.Uncertain)}");
            if (Get(PartDisplayState.Worn) > 0) list.Add($"◐ {Get(PartDisplayState.Worn)}");
            if (Get(PartDisplayState.Green) > 0) list.Add($"✓ {Get(PartDisplayState.Green)}");
            return string.Join(" · ", list);
        }

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
        private PilotAircraft? _aircraft;
        private readonly DifficultyEconomy _economy;

        public ComponentRowViewModel(AircraftComponent component, ComponentDefinition definition, ComponentCatalog catalog,
            EngineClass engineClass, double purchasePrice, double airframeHours, IRelayCommand<ComponentRowViewModel> repairCommand,
            PilotAircraft? aircraft = null, DifficultyEconomy? economy = null)
        {
            Component = component;
            Definition = definition;
            _catalog = catalog;
            _engineClass = engineClass;
            _purchasePrice = purchasePrice;
            _airframeHours = airframeHours;
            RepairCommand = repairCommand;
            _aircraft = aircraft;
            _economy = economy ?? DifficultyEconomy.For(DifficultyMode.Medium);
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
        public SolidColorBrush Brush => IsGroupPartsDriven ? GroupStateBrush : HangarPalette.ForHealth(Range.Mid);
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
                if (IsGroupPartsDriven)
                {
                    return PartHangarView.GroupLifeText(Kind, _aircraft, _engineClass);
                }

                var (remaining, life, isCycles) = ComponentMaintenance.RemainingLife(Definition, Component, _engineClass);
                if (life <= 0) return string.Empty;
                string unit = isCycles ? "pousos" : "h";
                return remaining <= 0
                    ? $"vida útil vencida há {-remaining:F0} {unit}"
                    : $"{remaining:F0} {unit} até a revisão (de {life:F0})";
            }
        }

        public bool HasLifeText => !string.IsNullOrEmpty(LifeText);

        public SolidColorBrush LifeBrush
        {
            get
            {
                if (IsGroupPartsDriven)
                {
                    var (_, isOverdue) = PartHangarView.GroupLifeInfo(Kind, _aircraft, _engineClass);
                    return isOverdue ? HangarPalette.Red : HangarPalette.Secondary;
                }

                var (remaining, life, _) = ComponentMaintenance.RemainingLife(Definition, Component, _engineClass);
                if (life <= 0) return HangarPalette.Secondary;
                return remaining <= 0 ? HangarPalette.Red : remaining < life * 0.1 ? HangarPalette.Amber : HangarPalette.Secondary;
            }
        }

        // --- Estado de grupo por peças ---
        public bool IsGroupPartsDriven => HasParts;

        private PartDisplayState _groupWorstState = PartDisplayState.Green;
        public PartDisplayState GroupWorstState => _groupWorstState;

        public Geometry GroupStateIconGeometry => HangarIcons.ForDisplayState(_groupWorstState);

        public SolidColorBrush GroupStateBrush => HangarPalette.ForDisplayState(_groupWorstState);

        private string _groupCountsSummary = string.Empty;
        public string GroupCountsSummary => _groupCountsSummary;

        private void RecalculateGroupState()
        {
            if (!HasParts)
            {
                _groupWorstState = PartDisplayState.Green;
                _groupCountsSummary = string.Empty;
                return;
            }

            var (worst, counts) = PartHangarView.Summarize(Parts.Select(p => p.Row.Display));
            _groupWorstState = worst;
            _groupCountsSummary = HangarPalette.CountsText(counts);

            OnPropertyChanged(nameof(GroupWorstState));
            OnPropertyChanged(nameof(GroupStateIconGeometry));
            OnPropertyChanged(nameof(GroupStateBrush));
            OnPropertyChanged(nameof(GroupCountsSummary));
            OnPropertyChanged(nameof(Brush));
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
                    return $"Trocar {pecas} · {_economy.ApplyCost(RepairCost):C0}";
                }

                return CanRepair ? $"Reparar · {_economy.ApplyCost(RepairCost):C0}" : "Em dia";
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
            OnPropertyChanged(nameof(IsGroupPartsDriven));
            OnPropertyChanged(nameof(LifeText));
            OnPropertyChanged(nameof(HasLifeText));
            OnPropertyChanged(nameof(LifeBrush));
            RecalculateGroupState();
        }

        public void Refresh(double airframeHours, PilotAircraft? aircraft = null)
        {
            _airframeHours = airframeHours;
            if (aircraft != null) _aircraft = aircraft;
            RecalculateGroupState();
            OnPropertyChanged(string.Empty);
        }
    }

    /// <summary>Linha de peça no Hangar com estado por ícone e troca individual.</summary>
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

        public PartDisplayInfo Display => Row.Display;
        public PartDisplayState DisplayState => Display.State;

        public Geometry StateIconGeometry => HangarIcons.ForDisplayState(DisplayState);

        public SolidColorBrush StateBrush => HangarPalette.ForDisplayState(Display);

        public string StateTooltip => DisplayState switch
        {
            PartDisplayState.Broken => "Peça quebrada",
            PartDisplayState.Red => "No limite: risco de pane, trocar",
            PartDisplayState.Yellow => "Atenção: desgaste acelerado",
            PartDisplayState.Worn => "Em uso: desgaste normal, ainda boa",
            PartDisplayState.Green => "Nova ou quase nova",
            PartDisplayState.Uncertain => "Incerta: pode ter piorado desde a última inspeção",
            _ => string.Empty
        };

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
