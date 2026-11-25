using System;
using System.Collections.Generic;
using System.Text.Json;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed class ContractMapVisuals
    {
        public List<MapContractMarker> Markers { get; } = new();
        public SurveyMapArea? SurveyArea { get; set; }
    }

    public static class ContractMarkerBuilder
    {
        public static ContractMapVisuals Build(IEnumerable<Mission>? missions)
        {
            var visuals = new ContractMapVisuals();
            if (missions == null) return visuals;

            foreach (var mission in missions)
            {
                if (mission == null) continue;

                // 1. Pontos de sobrevoo / áreas por coordenada
                if (!string.IsNullOrWhiteSpace(mission.OverflyWaypoints))
                {
                    var items = OverflyWaypointCodec.Parse(mission.OverflyWaypoints);
                    foreach (var item in items)
                    {
                        if (!item.IsAirport && item.Lat.HasValue && item.Lon.HasValue)
                        {
                            var kind = (item.RadiusNm > 2.0 || item.Ident.StartsWith("Área", StringComparison.OrdinalIgnoreCase))
                                ? ContractMarkerKind.Area
                                : ContractMarkerKind.Point;

                            visuals.Markers.Add(new MapContractMarker(
                                item.Ident,
                                item.Lat.Value,
                                item.Lon.Value,
                                item.RadiusNm,
                                kind));
                        }
                    }
                }

                // 2. Contratos especializados (Aerofoto e Paraquedismo)
                if (!string.IsNullOrWhiteSpace(mission.ContractData))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(mission.ContractData);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("DropZoneLatitude", out _) && root.TryGetProperty("TargetAltitudeAglFt", out _))
                        {
                            var skydive = JsonSerializer.Deserialize<SkydiveContractData>(mission.ContractData);
                            if (skydive != null && (skydive.DropZoneLatitude != 0 || skydive.DropZoneLongitude != 0))
                            {
                                visuals.Markers.Add(new MapContractMarker(
                                    "Zona de Salto",
                                    skydive.DropZoneLatitude,
                                    skydive.DropZoneLongitude,
                                    2.0,
                                    ContractMarkerKind.DropZone));
                            }
                        }
                        else if (root.TryGetProperty("CenterLatitude", out _) && root.TryGetProperty("RequiredLegs", out _))
                        {
                            var survey = JsonSerializer.Deserialize<SurveyContractData>(mission.ContractData);
                            if (survey != null && survey.RequiredLegs > 0 && survey.WidthNM > 0 && survey.LengthNM > 0)
                            {
                                visuals.SurveyArea = ComputeSurveyArea(survey);
                            }
                        }
                        else if (root.TryGetProperty("Mode", out _) && root.TryGetProperty("Points", out _))
                        {
                            var aerial = JsonSerializer.Deserialize<AerialWorkContractData>(mission.ContractData);
                            if (aerial != null)
                            {
                                string mode = aerial.Mode?.ToLowerInvariant() ?? string.Empty;
                                if (mode == "orbit")
                                {
                                    if (aerial.OrbitCenterLat != 0 || aerial.OrbitCenterLon != 0)
                                    {
                                        visuals.Markers.Add(new MapContractMarker(
                                            "Área de Órbita",
                                            aerial.OrbitCenterLat,
                                            aerial.OrbitCenterLon,
                                            aerial.OrbitRadiusNm > 0 ? aerial.OrbitRadiusNm : 1.5,
                                            ContractMarkerKind.Area));
                                    }
                                }
                                else if (mode == "search")
                                {
                                    // SAR: só o datum e os pontos do padrão, NUNCA o alvo
                                    if (aerial.OrbitCenterLat != 0 || aerial.OrbitCenterLon != 0)
                                    {
                                        visuals.Markers.Add(new MapContractMarker(
                                            "Datum SAR",
                                            aerial.OrbitCenterLat,
                                            aerial.OrbitCenterLon,
                                            aerial.PointRadiusNm > 0 ? aerial.PointRadiusNm : 0.5,
                                            ContractMarkerKind.Point));
                                    }
                                    if (aerial.Points != null)
                                    {
                                        foreach (var pt in aerial.Points)
                                        {
                                            visuals.Markers.Add(new MapContractMarker(
                                                pt.Ident,
                                                pt.Lat,
                                                pt.Lon,
                                                aerial.PointRadiusNm > 0 ? aerial.PointRadiusNm : 0.5,
                                                ContractMarkerKind.Point));
                                        }
                                    }
                                }
                                else
                                {
                                    // sectors e route
                                    if (aerial.Points != null)
                                    {
                                        foreach (var pt in aerial.Points)
                                        {
                                            visuals.Markers.Add(new MapContractMarker(
                                                pt.Ident,
                                                pt.Lat,
                                                pt.Lon,
                                                aerial.PointRadiusNm > 0 ? aerial.PointRadiusNm : 1.0,
                                                ContractMarkerKind.Point));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ContractMarkerBuilder] Erro ao analisar ContractData: {ex.Message}");
                    }
                }
            }

            return visuals;
        }

        private static SurveyMapArea ComputeSurveyArea(SurveyContractData survey)
        {
            double halfL = survey.LengthNM / 2.0;
            double halfW = survey.WidthNM / 2.0;
            double rad = survey.OrientationDeg * Math.PI / 180.0;
            double cosLat = Math.Cos(survey.CenterLatitude * Math.PI / 180.0);
            if (Math.Abs(cosLat) < 0.01) cosLat = 0.01;

            GeoPoint UvToGeo(double u, double v)
            {
                double dNorth = (u * Math.Cos(rad)) - (v * Math.Sin(rad));
                double dEast = (u * Math.Sin(rad)) + (v * Math.Cos(rad));
                double lat = survey.CenterLatitude + (dNorth / 60.0);
                double lon = survey.CenterLongitude + (dEast / (60.0 * cosLat));
                return new GeoPoint(lat, lon);
            }

            var corners = new List<GeoPoint>
            {
                UvToGeo(-halfL, -halfW),
                UvToGeo(halfL, -halfW),
                UvToGeo(halfL, halfW),
                UvToGeo(-halfL, halfW)
            };

            var legs = new List<IReadOnlyList<GeoPoint>>();
            int nLegs = Math.Max(1, survey.RequiredLegs);
            double legStep = survey.WidthNM / nLegs;

            for (int k = 0; k < nLegs; k++)
            {
                double v = -halfW + ((k + 0.5) * legStep);
                var pStart = UvToGeo(-halfL, v);
                var pEnd = UvToGeo(halfL, v);
                legs.Add(new List<GeoPoint> { pStart, pEnd });
            }

            return new SurveyMapArea(corners, legs);
        }
    }
}
