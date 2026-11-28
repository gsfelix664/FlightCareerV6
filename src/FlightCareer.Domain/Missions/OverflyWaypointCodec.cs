using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FlightCareer.Domain.Missions
{
    public sealed record OverflyWaypointItem(
        string Ident,
        bool IsAirport,
        double? Lat = null,
        double? Lon = null,
        double RadiusNm = 2.0,
        double MaxAglFt = 3000.0,
        double MinAglFt = 0.0);

    public static class OverflyWaypointCodec
    {
        /// <summary>Remove os separadores do formato (';', '@', ',') de nomes vindos dos bancos de locais.</summary>
        public static string SanitizeName(string name) =>
            string.IsNullOrWhiteSpace(name) ? "Ponto" : name.Replace(';', ' ').Replace('@', ' ').Replace(',', ' ').Trim();

        public static IReadOnlyList<OverflyWaypointItem> Parse(string? waypoints)
        {
            var result = new List<OverflyWaypointItem>();
            if (string.IsNullOrWhiteSpace(waypoints)) return result;

            // Divide inicialmente por ';'
            var segments = waypoints.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var seg in segments)
            {
                if (seg.StartsWith("P:", StringComparison.OrdinalIgnoreCase))
                {
                    // Formato: P:<nome>@<lat>,<lon>[,<raioNM>[,<maxAglFt>]]
                    string body = seg.Substring(2);
                    int atIdx = body.IndexOf('@');
                    if (atIdx <= 0 || atIdx == body.Length - 1) continue;

                    string name = body.Substring(0, atIdx).Trim();
                    string coordsPart = body.Substring(atIdx + 1).Trim();
                    var parts = coordsPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length < 2) continue;

                    if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                        continue;
                    if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                        continue;

                    double radiusNm = 2.0;
                    if (parts.Length >= 3 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
                    {
                        radiusNm = r;
                    }

                    double maxAglFt = 3000.0;
                    if (parts.Length >= 4 && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double agl))
                    {
                        maxAglFt = agl;
                    }

                    double minAglFt = 0.0;
                    if (parts.Length >= 5 && double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double minAgl))
                    {
                        minAglFt = minAgl;
                    }

                    result.Add(new OverflyWaypointItem(name, false, lat, lon, radiusNm, maxAglFt, minAglFt));
                }
                else
                {
                    // Pode ser um ou mais ICAOs separados por vírgula
                    var icaos = seg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var icaoRaw in icaos)
                    {
                        string icao = icaoRaw.Trim().ToUpperInvariant();
                        if (!string.IsNullOrEmpty(icao))
                        {
                            result.Add(new OverflyWaypointItem(icao, true, null, null, 2.0, 3000.0));
                        }
                    }
                }
            }

            return result;
        }

        public static string Format(IEnumerable<OverflyWaypointItem> items)
        {
            if (items == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                if (sb.Length > 0) sb.Append(';');
                if (item.IsAirport)
                {
                    sb.Append(item.Ident.Trim().ToUpperInvariant());
                }
                else
                {
                    sb.Append("P:");
                    sb.Append(SanitizeName(item.Ident));
                    sb.Append('@');
                    sb.Append((item.Lat ?? 0.0).ToString("F5", CultureInfo.InvariantCulture));
                    sb.Append(',');
                    sb.Append((item.Lon ?? 0.0).ToString("F5", CultureInfo.InvariantCulture));
                    if (item.MinAglFt > 0.001)
                    {
                        sb.Append(',');
                        sb.Append(item.RadiusNm.ToString("F1", CultureInfo.InvariantCulture));
                        sb.Append(',');
                        sb.Append(item.MaxAglFt.ToString("F0", CultureInfo.InvariantCulture));
                        sb.Append(',');
                        sb.Append(item.MinAglFt.ToString("F0", CultureInfo.InvariantCulture));
                    }
                    else if (Math.Abs(item.RadiusNm - 2.0) > 0.001 || Math.Abs(item.MaxAglFt - 3000.0) > 0.001)
                    {
                        sb.Append(',');
                        sb.Append(item.RadiusNm.ToString("F1", CultureInfo.InvariantCulture));
                        if (Math.Abs(item.MaxAglFt - 3000.0) > 0.001)
                        {
                            sb.Append(',');
                            sb.Append(item.MaxAglFt.ToString("F0", CultureInfo.InvariantCulture));
                        }
                    }
                }
            }
            return sb.ToString();
        }

        public static string FormatCoordinatePoint(string name, double lat, double lon, double? radiusNm = null, double? maxAglFt = null, double? minAglFt = null)
        {
            var sb = new StringBuilder();
            sb.Append("P:");
            sb.Append(SanitizeName(name));
            sb.Append('@');
            sb.Append(lat.ToString("F5", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(lon.ToString("F5", CultureInfo.InvariantCulture));
            if (minAglFt.HasValue && minAglFt.Value > 0.001)
            {
                double r = radiusNm ?? 2.0;
                double max = maxAglFt ?? 3000.0;
                sb.Append(',');
                sb.Append(r.ToString("F1", CultureInfo.InvariantCulture));
                sb.Append(',');
                sb.Append(max.ToString("F0", CultureInfo.InvariantCulture));
                sb.Append(',');
                sb.Append(minAglFt.Value.ToString("F0", CultureInfo.InvariantCulture));
            }
            else if (radiusNm.HasValue)
            {
                sb.Append(',');
                sb.Append(radiusNm.Value.ToString("F1", CultureInfo.InvariantCulture));
                if (maxAglFt.HasValue)
                {
                    sb.Append(',');
                    sb.Append(maxAglFt.Value.ToString("F0", CultureInfo.InvariantCulture));
                }
            }
            else if (maxAglFt.HasValue)
            {
                sb.Append(",2.0,");
                sb.Append(maxAglFt.Value.ToString("F0", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }
}
