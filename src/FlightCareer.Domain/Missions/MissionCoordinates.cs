using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions;

public sealed record MissionCoordinatePoint(string Label, double Lat, double Lon)
{
    public string Display { get; } = MissionCoordinates.FormatDegreesMinutes(Lat, Lon);
    public string CopyText { get; } = string.Create(CultureInfo.InvariantCulture, $"{Lat:F5}, {Lon:F5}");
}

public static class MissionCoordinates
{
    public static string FormatDegreesMinutes(double lat, double lon)
    {
        char latHemi = lat >= 0 ? 'N' : 'S';
        char lonHemi = lon >= 0 ? 'E' : 'W';

        int latTenths = (int)Math.Round(Math.Abs(lat) * 600.0, MidpointRounding.AwayFromZero);
        int latDeg = latTenths / 600;
        int latRemTenths = latTenths % 600;
        int latMinInt = latRemTenths / 10;
        int latMinDec = latRemTenths % 10;

        int lonTenths = (int)Math.Round(Math.Abs(lon) * 600.0, MidpointRounding.AwayFromZero);
        int lonDeg = lonTenths / 600;
        int lonRemTenths = lonTenths % 600;
        int lonMinInt = lonRemTenths / 10;
        int lonMinDec = lonRemTenths % 10;

        return $"{latHemi}{latDeg:D2}°{latMinInt:D2},{latMinDec}' {lonHemi}{lonDeg:D3}°{lonMinInt:D2},{lonMinDec}'";
    }

    public static IReadOnlyList<MissionCoordinatePoint> For(Mission? mission)
    {
        if (mission == null)
            return Array.Empty<MissionCoordinatePoint>();

        var result = new List<MissionCoordinatePoint>();

        // 1. Pontos de coordenada de OverflyWaypoints (rótulo = Ident)
        if (!string.IsNullOrWhiteSpace(mission.OverflyWaypoints))
        {
            try
            {
                var waypoints = OverflyWaypointCodec.Parse(mission.OverflyWaypoints);
                foreach (var wp in waypoints)
                {
                    if (!wp.IsAirport && wp.Lat.HasValue && wp.Lon.HasValue)
                    {
                        double lat = wp.Lat.Value;
                        double lon = wp.Lon.Value;
                        if (lat == 0 && lon == 0) continue;

                        TryAddPoint(result, wp.Ident, lat, lon);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MissionCoordinates] Erro ao ler OverflyWaypoints ({mission.Id}): {ex.Message}");
            }
        }

        // 2. Escalas (rótulo = "{n} · {Icao}", n = posição 1-based na lista original contando as puladas)
        var stops = BriefingLegPlanner.ReadStops(mission);
        if (stops != null && stops.Count > 0)
        {
            for (int i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];
                int n = i + 1;
                if (stop.Lat == 0 && stop.Lon == 0)
                {
                    continue;
                }

                TryAddPoint(result, $"{n} · {stop.Icao}", stop.Lat, stop.Lon);
            }
        }

        // 3. Pista STOL ("1 · {StripIcao}")
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
                        TryAddPoint(result, $"1 · {stol.StripIcao}", stol.Lat, stol.Lon);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MissionCoordinates] Erro ao ler STOL ContractData ({mission.Id}): {ex.Message}");
            }
        }

        return result;
    }

    private static void TryAddPoint(List<MissionCoordinatePoint> list, string label, double lat, double lon)
    {
        if (string.IsNullOrWhiteSpace(label)) return;
        if (lat == 0 && lon == 0) return;

        // Sem repetir item com mesmo rótulo e mesma coordenada
        if (list.Any(p => p.Label == label && Math.Abs(p.Lat - lat) < 1e-7 && Math.Abs(p.Lon - lon) < 1e-7))
        {
            return;
        }

        list.Add(new MissionCoordinatePoint(label, lat, lon));
    }
}
