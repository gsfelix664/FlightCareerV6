using CommunityToolkit.Mvvm.ComponentModel;

namespace FlightCareer.UI.ViewModels
{
    public partial class FlightLegModel : ObservableObject
    {
        [ObservableProperty] private int _legNumber;
        [ObservableProperty] private string _originICAO = string.Empty;
        [ObservableProperty] private string _destinationICAO = string.Empty;
        [ObservableProperty] private double _distanceNM;
        [ObservableProperty] private int _onboardPax;
        [ObservableProperty] private double _onboardCargoLbs;
        [ObservableProperty] private double _legPayloadLbs;
        [ObservableProperty] private double _takeoffWeightLbs;
        [ObservableProperty] private double _aircraftMtowLbs = 2000.0;
        [ObservableProperty] private double _mtowPercent;
        [ObservableProperty] private bool _isOverweight;
        [ObservableProperty] private string _actionSummary = string.Empty;
        [ObservableProperty] private string _eteFormatted = "--";

        public string MtowBarColor => IsOverweight ? "#FF3D00" : (MtowPercent > 85 ? "#FFB300" : "#00E676");
        public string RouteDisplay => $"{OriginICAO} ➔ {DestinationICAO}";
        public string DistanceDisplay => $"{DistanceNM:N0} NM";
        public string PayloadDisplay => $"{LegPayloadLbs:N0} LBS";
        public string MtowPercentDisplay => $"{MtowPercent:F0}% MTOW";
    }
}
