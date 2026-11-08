using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GMap.NET;

namespace FlightCareer.UI.ViewModels
{
    public class FlightTelemetryRecord
    {
        public string Timestamp { get; set; } = string.Empty;
        public double ElapsedSec { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Dest { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AltitudeFt { get; set; }
        public double GroundSpeedKt { get; set; }
        public double AirspeedKt { get; set; }
        public double VerticalSpeedFpm { get; set; }
        public double VerticalSpeedFps 
        { 
            get => VerticalSpeedFpm; 
            set => VerticalSpeedFpm = value; 
        }
        public double GForce { get; set; }
        public double PitchDeg { get; set; }
        public double BankDeg { get; set; }
        public double HeadingDeg { get; set; }
        public bool IsOnGround { get; set; }
        public double EngineRPM { get; set; }
        public double FuelGallons { get; set; }
        public string Status { get; set; } = string.Empty;

        public string ElapsedFormatted
        {
            get
            {
                int min = (int)(ElapsedSec / 60);
                int sec = (int)(ElapsedSec % 60);
                return $"{min:D2}:{sec:D2}";
            }
        }
    }

    public partial class FlightLogEntry : ObservableObject
    {
        [ObservableProperty] private string _filePath = string.Empty;
        [ObservableProperty] private string _fileName = string.Empty;
        [ObservableProperty] private DateTime _date = DateTime.Now;
        [ObservableProperty] private string _originICAO = "----";
        [ObservableProperty] private string _destinationICAO = "----";
        [ObservableProperty] private string _aircraft = "---";
        [ObservableProperty] private double _durationMinutes;
        [ObservableProperty] private string _status = "VOO_CONCLUIDO";
        [ObservableProperty] private double _touchdownFpm;
        [ObservableProperty] private double _qualityScore = 100;
        [ObservableProperty] private double _minGForce = 1.0;
        [ObservableProperty] private double _maxGForce = 1.0;
        [ObservableProperty] private double _maxAltitudeFt;
        [ObservableProperty] private double _maxGroundSpeedKt;
        [ObservableProperty] private double _fuelUsedGallons;
        [ObservableProperty] private int _totalFrames;
        [ObservableProperty] private bool _isLoaded;

        public string RouteDisplay => $"{OriginICAO} ➔ {DestinationICAO}";
        public string DateFormatted => Date.ToString("dd/MM/yyyy HH:mm");
        public string DurationFormatted => DurationMinutes >= 60 
            ? $"{(int)DurationMinutes / 60}h {(int)DurationMinutes % 60:D2}m"
            : $"{DurationMinutes:F1} min";

        public string StatusColor => Status switch
        {
            "VOO_CONCLUIDO" => "#00E676",
            "CANCELADO_PELO_PILOTO" => "#FF5252",
            "RESTART" => "#FFB300",
            _ => "#00E5FF"
        };

        public string StatusBg => Status switch
        {
            "VOO_CONCLUIDO" => "#0D2B1E",
            "CANCELADO_PELO_PILOTO" => "#2E1214",
            _ => "#1F2B37"
        };

        public string StatusDisplay => Status switch
        {
            "VOO_CONCLUIDO" => "✓ CONCLUÍDO",
            "CANCELADO_PELO_PILOTO" => "✕ CANCELADO",
            "RESTART" => "⚡ REINICIADO",
            _ => Status
        };

        public string QualityRating => QualityScore switch
        {
            >= 90 => $"{QualityScore:F0}% • EXCELENTE",
            >= 75 => $"{QualityScore:F0}% • BOM",
            >= 50 => $"{QualityScore:F0}% • REGULAR",
            _ => $"{QualityScore:F0}% • CRÍTICO"
        };

        public string QualityColor => QualityScore switch
        {
            >= 90 => "#00E676",
            >= 75 => "#00E5FF",
            >= 50 => "#FFB300",
            _ => "#FF5252"
        };

        public string GForceRange => $"{MinGForce:F2}G a {MaxGForce:F2}G";
        public string TouchdownDisplay => $"{TouchdownFpm:F0} FPM";

        public ObservableCollection<FlightTelemetryRecord> TelemetryRecords { get; } = new();
        [ObservableProperty] private ObservableCollection<PointLatLng> _mapRoutePoints = new();
    }
}
