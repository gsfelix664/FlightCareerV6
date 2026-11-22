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
}
