namespace FlightCareer.Domain.Missions
{
    public sealed record OverflyPoint(
        string Ident,
        double Latitude,
        double Longitude,
        double ElevationFt,
        double RadiusNm = 2.0,
        double MaxAglFt = 3000.0,
        double MinAglFt = 0.0);
}
