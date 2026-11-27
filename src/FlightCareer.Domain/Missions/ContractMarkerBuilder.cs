using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed class ContractMapVisuals
    {
        public List<MapContractMarker> Markers { get; } = new();
        public SurveyMapArea? SurveyArea { get; set; }
        public List<ContractRouteLine> Routes { get; } = new();
    }

    public static class ContractMarkerBuilder
    {
        public static ContractMapVisuals Build(
            IEnumerable<Mission>? missions,
            Func<string, GeoPoint?>? airportByIcao = null)
        {
            var visuals = new ContractMapVisuals();
            if (missions == null) return visuals;

            foreach (var mission in missions)
            {
                if (mission == null) continue;

                // 1. Escalas contratuais (MultiStopContractData)
                var stops = BriefingLegPlanner.ReadStops(mission);
                if (stops != null && stops.Count > 0)
                {
                    for (int i = 0; i < stops.Count; i++)
                    {
                        var stop = stops[i];
                        int n = i + 1;
                        if (stop.Lat == 0 && stop.Lon == 0)
                        {
                            // Pular escala com Lat == 0 e Lon == 0 (não conta no desenho, mas a numeração segue a posição na lista)
                            continue;
                        }

                        TryAddMarker(visuals.Markers, new MapContractMarker(
                            $"{n} · {stop.Icao}",
                            stop.Lat,
                            stop.Lon,
                            0.0,
                            ContractMarkerKind.Stop));
                    }
                }

                // 2. Pontos de sobrevoo / áreas por coordenada
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

                            TryAddMarker(visuals.Markers, new MapContractMarker(
                                item.Ident,
                                item.Lat.Value,
                                item.Lon.Value,
                                item.RadiusNm,
                                kind));
                        }
                    }
                }

                // 3. Contratos especializados (STOL, Aerofoto, Paraquedismo, Aerial)
                StolContractData? stolData = null;
                if (!string.IsNullOrWhiteSpace(mission.ContractData))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(mission.ContractData);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("StripIcao", out _))
                        {
                            var stol = JsonSerializer.Deserialize<StolContractData>(mission.ContractData);
                            if (stol != null && !string.IsNullOrWhiteSpace(stol.StripIcao) && (stol.Lat != 0 || stol.Lon != 0))
                            {
                                stolData = stol;
                                TryAddMarker(visuals.Markers, new MapContractMarker(
                                    $"1 · {stol.StripIcao}",
                                    stol.Lat,
                                    stol.Lon,
                                    0.0,
                                    ContractMarkerKind.Stop));
                            }
                        }
                        else if (root.TryGetProperty("DropZoneLatitude", out _) && root.TryGetProperty("TargetAltitudeAglFt", out _))
                        {
                            var skydive = JsonSerializer.Deserialize<SkydiveContractData>(mission.ContractData);
                            if (skydive != null && (skydive.DropZoneLatitude != 0 || skydive.DropZoneLongitude != 0))
                            {
                                TryAddMarker(visuals.Markers, new MapContractMarker(
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
                                        TryAddMarker(visuals.Markers, new MapContractMarker(
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
                                        TryAddMarker(visuals.Markers, new MapContractMarker(
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
                                            TryAddMarker(visuals.Markers, new MapContractMarker(
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
                                            TryAddMarker(visuals.Markers, new MapContractMarker(
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
                        Debug.WriteLine($"[ContractMarkerBuilder] Erro ao analisar ContractData: {ex.Message}");
                    }
                }

                // 4. Linha do contrato (só quando a missão tem ao menos um ponto de escala/pista válido)
                bool hasValidStop = stops != null && stops.Any(s => s.Lat != 0 || s.Lon != 0);
                bool hasValidStol = stolData != null && (stolData.Lat != 0 || stolData.Lon != 0);

                if (hasValidStop || hasValidStol)
                {
                    var rawPoints = new List<GeoPoint>();

                    // Origem
                    if (airportByIcao != null && !string.IsNullOrWhiteSpace(mission.OriginICAO))
                    {
                        var orig = airportByIcao(mission.OriginICAO.Trim());
                        if (orig != null && (orig.Lat != 0 || orig.Lon != 0))
                        {
                            rawPoints.Add(orig);
                        }
                    }

                    if (hasValidStop)
                    {
                        // Pontos de coordenada de OverflyWaypoints na ordem (só escalas; STOL não tem)
                        if (!string.IsNullOrWhiteSpace(mission.OverflyWaypoints))
                        {
                            var items = OverflyWaypointCodec.Parse(mission.OverflyWaypoints);
                            foreach (var item in items)
                            {
                                if (!item.IsAirport && item.Lat.HasValue && item.Lon.HasValue &&
                                    (item.Lat.Value != 0 || item.Lon.Value != 0))
                                {
                                    rawPoints.Add(new GeoPoint(item.Lat.Value, item.Lon.Value));
                                }
                            }
                        }

                        // Escalas na ordem
                        foreach (var stop in stops!)
                        {
                            if (stop.Lat != 0 || stop.Lon != 0)
                            {
                                rawPoints.Add(new GeoPoint(stop.Lat, stop.Lon));
                            }
                        }
                    }
                    else if (hasValidStol)
                    {
                        rawPoints.Add(new GeoPoint(stolData!.Lat, stolData.Lon));
                    }

                    // Destino
                    if (airportByIcao != null)
                    {
                        string? destIcao = mission.DestinationICAO;
                        if (hasValidStol && string.IsNullOrWhiteSpace(destIcao))
                        {
                            // Para STOL com destino == origem (ou vazio) o fecho é a origem
                            destIcao = mission.OriginICAO;
                        }

                        if (!string.IsNullOrWhiteSpace(destIcao))
                        {
                            var dest = airportByIcao(destIcao.Trim());
                            if (dest != null && (dest.Lat != 0 || dest.Lon != 0))
                            {
                                rawPoints.Add(dest);
                            }
                        }
                    }

                    // Remover vértices consecutivos iguais
                    var distinctPoints = new List<GeoPoint>();
                    foreach (var pt in rawPoints)
                    {
                        if (distinctPoints.Count == 0 ||
                            Math.Abs(distinctPoints[^1].Lat - pt.Lat) > 1e-7 ||
                            Math.Abs(distinctPoints[^1].Lon - pt.Lon) > 1e-7)
                        {
                            distinctPoints.Add(pt);
                        }
                    }

                    // Menos de 2 vértices = nenhuma linha
                    if (distinctPoints.Count >= 2)
                    {
                        visuals.Routes.Add(new ContractRouteLine(distinctPoints));
                    }
                }
            }

            return visuals;
        }

        private static void TryAddMarker(List<MapContractMarker> markers, MapContractMarker marker)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                var existing = markers[i];
                if (existing.Kind == marker.Kind &&
                    string.Equals(existing.Label, marker.Label, StringComparison.Ordinal) &&
                    Math.Abs(existing.Lat - marker.Lat) < 1e-7 &&
                    Math.Abs(existing.Lon - marker.Lon) < 1e-7)
                {
                    return;
                }
            }
            markers.Add(marker);
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
