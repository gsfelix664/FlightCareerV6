namespace FlightCareer.Domain.Economy
{
    public enum AircraftSizeClass
    {
        Light,
        Medium,
        Heavy
    }

    public static class AircraftSizeClassifier
    {
        public static AircraftSizeClass Classify(double mtowLbs)
        {
            if (mtowLbs < 12500.0)
                return AircraftSizeClass.Light;
            if (mtowLbs <= 60000.0)
                return AircraftSizeClass.Medium;
            return AircraftSizeClass.Heavy;
        }
    }
}
