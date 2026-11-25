using System.Collections.Generic;

namespace FlightCareer.Domain.Airports.Sim
{
    public sealed record SimAirportListItem(
        string Ident,
        string Region,
        double Latitude,
        double Longitude,
        double AltitudeM);

    public sealed record SimRunwayData(
        double LengthM,
        double WidthM,
        int SurfaceCode,
        string PrimaryIlsIcao,
        string SecondaryIlsIcao);

    public sealed record SimAirportDetail(
        string Ident,
        string Region,
        string Name,
        double Latitude,
        double Longitude,
        double AltitudeM,
        int Approaches,
        int Jetways,
        double TowerLatitude,
        double TowerLongitude,
        IReadOnlyList<SimRunwayData> Runways);
}
