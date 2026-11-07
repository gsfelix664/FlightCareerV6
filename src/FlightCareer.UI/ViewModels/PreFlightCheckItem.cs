using CommunityToolkit.Mvvm.ComponentModel;

namespace FlightCareer.UI.ViewModels
{
    public partial class PreFlightCheckItem : ObservableObject
    {
        [ObservableProperty] private string _parameterName = string.Empty;
        [ObservableProperty] private string _plannedValue = string.Empty;
        [ObservableProperty] private string _simReportedValue = string.Empty;
        [ObservableProperty] private bool _isOk;
        [ObservableProperty] private string _diffMessage = string.Empty;
        [ObservableProperty] private string _icon = "•";

        public string StatusColor => IsOk ? "#00E676" : "#FF3D00";
        public string StatusBadgeBg => IsOk ? "#0D2B1E" : "#2E1214";
        public string StatusText => IsOk ? "OK" : "DIVERGENTE";
        public string StatusDisplay => IsOk ? "🟢 OK" : "🔴 DIVERGENTE";
    }
}
