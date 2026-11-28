using System.Collections.Generic;

namespace FlightCareer.Application.Interfaces
{
    public sealed record DropZoneInfo(string Icao, string Airport, string Cc, string Name, double Lat, double Lon, string Note = "");

    public sealed record TouristPoiInfo(string Id, string Name, double Lat, double Lon, string Kind, string Country, int Popularity);

    public sealed record ScenicPoint(string Name, double Lat, double Lon);

    public sealed record ScenicAerodromeInfo(
        string Icao,
        string Name,
        string Cc,
        string Note,
        IReadOnlyList<ScenicPoint> Points,
        string Role = "panoramico")
    {
        /// <summary>Base de voos panorâmicos (puxa mais 3.5.1 no gerador). "destino" = destino turístico: só fornece os pontos.</summary>
        public bool IsScenicBase => !string.Equals(Role, "destino", System.StringComparison.OrdinalIgnoreCase);
    }

    public sealed record TrainingAreaInfo(
        string Id,
        string Name,
        string Kind,
        double Lat,
        double Lon,
        double RadiusNm,
        int FloorFt,
        string FloorRef,
        int CeilFt,
        string CeilRef);

    public interface IPlacesDataService
    {
        bool IsTrainingAerodrome(string icao);
        DropZoneInfo? GetDropZone(string icao);
        ScenicAerodromeInfo? GetScenicAerodrome(string icao);
        IReadOnlyList<TouristPoiInfo> FindPois(double lat, double lon, double minNm, double maxNm, IReadOnlySet<string>? kinds = null);
        IReadOnlyList<TrainingAreaInfo> FindTrainingAreas(double lat, double lon, double maxNm = 25.0, string kind = "instrucao");
    }
}
