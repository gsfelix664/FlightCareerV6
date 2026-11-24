using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    public sealed class SurveyContractData
    {
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double WidthNM { get; set; }
        public double LengthNM { get; set; }
        public double OrientationDeg { get; set; }
        public int RequiredLegs { get; set; }
        public double TargetAltitudeFt { get; set; }
        public double TargetSpeedKt { get; set; }
    }

    public sealed class SkydiveContractData
    {
        public double DropZoneLatitude { get; set; }
        public double DropZoneLongitude { get; set; }
        public double DropZoneElevationFt { get; set; }
        public double TargetAltitudeAglFt { get; set; }
        public double MaxDropSpeedKt { get; set; }
    }

    public sealed class MultiStopContractData
    {
        public List<StopPoint> Stops { get; set; } = new();
        public bool RequireNoRefuel { get; set; } = true;
        public string StopMode { get; set; } = "full_stop"; // "full_stop" | "touch_and_go"
    }

    public sealed class StopPoint
    {
        public string Icao { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double ElevationFt { get; set; }
    }

    public sealed class StolContractData
    {
        public string StripIcao { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double ElevationFt { get; set; }
        public double RunwayLengthFt { get; set; }
        public int RequiredLandings { get; set; } = 3;
        public double MaxStopDistanceFt { get; set; }
        public double MinApproachAngleDeg { get; set; } = 4.0;
        public double CrosswindBonusKt { get; set; } = 8.0;
    }

    public sealed class CheckFlightContractData
    {
        public double TestAltitudeFt { get; set; }
        public double AltitudeToleranceFt { get; set; } = 300.0;
        public double PowerHoldSeconds { get; set; } = 10.0;
        public double HeadingHoldSeconds { get; set; } = 15.0;
        public double HeadingToleranceDeg { get; set; } = 10.0;
    }

    public sealed class FerryPermitContractData
    {
        public string Restriction { get; set; } = string.Empty; // "max_altitude" ou "flaps_fixed"
        public double MaxAltitudeFt { get; set; }
        public double FlapTolerancePct { get; set; } = 5.0;
    }
}
