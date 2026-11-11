using CommunityToolkit.Mvvm.ComponentModel;
using FlightCareer.Domain.Entities;

namespace FlightCareer.UI.ViewModels
{
    public partial class FlightLegModel : ObservableObject
    {
        [ObservableProperty] private int _legNumber;
        [ObservableProperty] private string _originICAO = string.Empty;
        [ObservableProperty] private string _destinationICAO = string.Empty;
        [ObservableProperty] private double _originLat;
        [ObservableProperty] private double _originLon;
        [ObservableProperty] private double _destLat;
        [ObservableProperty] private double _destLon;
        [ObservableProperty] private bool _isTglOrLocal;
        [ObservableProperty] private string _originMsfsIdent = string.Empty;
        [ObservableProperty] private string _destinationMsfsIdent = string.Empty;
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

        [ObservableProperty] private Mission? _associatedMission;
        [ObservableProperty] private string _missionTitle = string.Empty;
        [ObservableProperty] private decimal _rewardMoney;

        public bool HasAssociatedMission => AssociatedMission != null;
        partial void OnAssociatedMissionChanged(Mission? value) => OnPropertyChanged(nameof(HasAssociatedMission));

        public string MtowBarColor => IsOverweight ? "#FF3D00" : (MtowPercent > 85 ? "#FFB300" : "#00E676");
        public string DestinationDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(DestinationMsfsIdent) && !string.Equals(DestinationICAO, DestinationMsfsIdent, StringComparison.OrdinalIgnoreCase))
                    return $"{DestinationICAO} (MSFS: {DestinationMsfsIdent})";

                return MissionDisplayItem.MsfsIcaoAliases.TryGetValue(DestinationICAO, out var alias)
                    ? $"{DestinationICAO} (MSFS: {alias})"
                    : DestinationICAO;
            }
        }
        public string RouteDisplay => IsTglOrLocal ? $"{OriginICAO} ➔ {DestinationDisplay} (TGL)" : $"{OriginICAO} ➔ {DestinationDisplay}";
        public string DistanceDisplay => $"{DistanceNM:N0} NM";
        public string PayloadDisplay => $"{LegPayloadLbs:N0} LBS";
        public string MtowPercentDisplay => $"{MtowPercent:F0}% MTOW";
    }
}
