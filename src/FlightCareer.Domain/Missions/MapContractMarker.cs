using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    public enum ContractMarkerKind
    {
        Point,
        Area,
        DropZone,
        Stop
    }

    public sealed record MapContractMarker(
        string Label,
        double Lat,
        double Lon,
        double RadiusNm,
        ContractMarkerKind Kind);

    public sealed record GeoPoint(double Lat, double Lon);

    public sealed record ContractRouteLine(IReadOnlyList<GeoPoint> Points);

    public sealed record SurveyMapArea(
        IReadOnlyList<GeoPoint> Corners,
        IReadOnlyList<IReadOnlyList<GeoPoint>> Legs);
}
