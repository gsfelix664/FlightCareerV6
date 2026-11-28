using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Geo
{
    public sealed class AirportSpatialGrid
    {
        private readonly Dictionary<(int LatBin, int LonBin), List<(double Lat, double Lon, string Country)>> _grid = new();

        public void Add(double lat, double lon, string country)
        {
            if (string.IsNullOrWhiteSpace(country)) return;
            int latBin = (int)Math.Floor(lat);
            int lonBin = (int)Math.Floor(lon);
            var key = (latBin, lonBin);
            if (!_grid.TryGetValue(key, out var list))
            {
                list = new List<(double Lat, double Lon, string Country)>();
                _grid[key] = list;
            }
            list.Add((lat, lon, country));
        }

        public string? FindNearestCountry(double lat, double lon, double maxNm = 50.0)
        {
            int latBin = (int)Math.Floor(lat);
            int lonBin = (int)Math.Floor(lon);

            int latSpan = 1;
            double latRad = Math.Abs(lat) * Math.PI / 180.0;
            double cosLat = Math.Cos(latRad);
            int lonSpan = cosLat < 0.05 ? 180 : (int)Math.Ceiling(maxNm / (60.0 * cosLat));

            double bestDist = double.MaxValue;
            string? bestCountry = null;

            for (int bLat = latBin - latSpan; bLat <= latBin + latSpan; bLat++)
            {
                for (int dLon = -lonSpan; dLon <= lonSpan; dLon++)
                {
                    int bLon = lonBin + dLon;
                    while (bLon < -180) bLon += 360;
                    while (bLon > 180) bLon -= 360;

                    if (_grid.TryGetValue((bLat, bLon), out var candidates))
                    {
                        for (int i = 0; i < candidates.Count; i++)
                        {
                            var cand = candidates[i];
                            double dist = CountryLocator.DistanceNm(lat, lon, cand.Lat, cand.Lon);
                            if (dist <= maxNm && dist < bestDist)
                            {
                                bestDist = dist;
                                bestCountry = cand.Country;
                            }
                        }
                    }
                }
            }

            return bestCountry;
        }
    }
}
