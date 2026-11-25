using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FlightCareer.Domain.Airports.Sim
{
    public static class SimAirportFingerprint
    {
        public static string Of(SimAirportListItem i)
        {
            var ident = (i.Ident ?? string.Empty).Trim().ToUpperInvariant();
            var region = (i.Region ?? string.Empty).Trim().ToUpperInvariant();
            var lat = i.Latitude.ToString("F4", CultureInfo.InvariantCulture);
            var lon = i.Longitude.ToString("F4", CultureInfo.InvariantCulture);
            var alt = Math.Round(i.AltitudeM).ToString("F0", CultureInfo.InvariantCulture);
            return $"{ident}|{region}|{lat}|{lon}|{alt}";
        }

        public static string Digest(IEnumerable<SimAirportListItem> items)
        {
            var orderedFps = items.Select(Of).OrderBy(x => x, StringComparer.Ordinal);
            var joined = string.Join("\n", orderedFps);
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
