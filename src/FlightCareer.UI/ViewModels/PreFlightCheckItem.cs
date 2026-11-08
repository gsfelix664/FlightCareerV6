using CommunityToolkit.Mvvm.ComponentModel;

namespace FlightCareer.UI.ViewModels
{
    public partial class PreFlightCheckItem : ObservableObject
    {
        [ObservableProperty] private string _itemIcon = "✈";
        [ObservableProperty] private string _parameterName = string.Empty;
        [ObservableProperty] private string _softwareValue = string.Empty;
        [ObservableProperty] private string _simStatusValue = string.Empty;
        [ObservableProperty] private bool _isOk;
        [ObservableProperty] private string _explanation = string.Empty;
        [ObservableProperty] private string _diffMessage = string.Empty;

        // Comparações de Alta Legibilidade (Plan vs. Sim)
        [ObservableProperty] private string _cleanPlanned = string.Empty;
        [ObservableProperty] private string _cleanSim = string.Empty;
        [ObservableProperty] private string _toleranceBadge = string.Empty;

        // Propriedades mantidas para retrocompatibilidade
        [ObservableProperty] private string _plannedValue = string.Empty;
        [ObservableProperty] private string _simReportedValue = string.Empty;
        [ObservableProperty] private string _icon = "•";

        public string StatusColor => IsOk ? "#00E676" : "#EF4444";
        public string StatusBadgeBg => IsOk ? "#0D2B1E" : "#2E1214";
        public string StatusBorderColor => IsOk ? "#1B4D3E" : "#4A1C20";
        public string StatusText => IsOk ? "OK" : "DIVERGENTE";
        public string StatusDisplay => IsOk ? "✓ CONFORME" : "✕ DIVERGENTE";
        public string ExplanationBg => IsOk ? "#081F15" : "#240E10";
        public string ExplanationColor => IsOk ? "#81C784" : "#EF9A9A";

        partial void OnIsOkChanged(bool value)
        {
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusBadgeBg));
            OnPropertyChanged(nameof(StatusBorderColor));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(ExplanationBg));
            OnPropertyChanged(nameof(ExplanationColor));
        }
    }
}
