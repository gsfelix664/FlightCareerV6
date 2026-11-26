using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    public enum OverflyAltitudeState { InBand, Below, Above }

    /// <summary>
    /// Rastreia o cumprimento de pontos de sobrevoo obrigatórios em missões panorâmicas.
    /// Condição: aeronave passar a <= 2.0 NM de distância horizontal e altitude <= elevação + 3000 ft.
    /// </summary>
    public sealed class OverflyTracker
    {
        public const double AltitudeToleranceFt = 100.0;

        private readonly List<OverflyPoint> _targets = new();
        private readonly HashSet<string> _visited = new(StringComparer.OrdinalIgnoreCase);

        public bool HasWaypoints => _targets.Count > 0;
        public IReadOnlySet<string> VisitedWaypoints => _visited;
        public IReadOnlyList<OverflyPoint> Targets => _targets;
        public double? LastLatitude { get; private set; }
        public double? LastLongitude { get; private set; }
        public double? LastAltitudeFt { get; private set; }

        public static OverflyAltitudeState EvaluateAltitude(OverflyPoint point, double altitudeFt)
        {
            if (point.MinAglFt > 0.0)
            {
                double minAllowedAlt = point.ElevationFt + point.MinAglFt - AltitudeToleranceFt;
                double maxAllowedAlt = point.ElevationFt + point.MaxAglFt + AltitudeToleranceFt;
                if (altitudeFt < minAllowedAlt) return OverflyAltitudeState.Below;
                if (altitudeFt > maxAllowedAlt) return OverflyAltitudeState.Above;
                return OverflyAltitudeState.InBand;
            }
            else
            {
                double maxAllowedAltitudeFt = point.ElevationFt + point.MaxAglFt;
                if (altitudeFt > maxAllowedAltitudeFt) return OverflyAltitudeState.Above;
                return OverflyAltitudeState.InBand;
            }
        }

        public void ConfigureWaypoints(IEnumerable<OverflyPoint> waypoints)
        {
            _targets.Clear();
            _visited.Clear();
            LastLatitude = null;
            LastLongitude = null;
            LastAltitudeFt = null;
            if (waypoints != null)
            {
                _targets.AddRange(waypoints);
            }
        }

        public void Reset()
        {
            _targets.Clear();
            _visited.Clear();
            LastLatitude = null;
            LastLongitude = null;
            LastAltitudeFt = null;
        }

        public bool IsPointVisited(string ident)
        {
            if (string.IsNullOrWhiteSpace(ident)) return false;
            return _visited.Contains(ident.Trim());
        }

        public void Update(double currentLat, double currentLon, double currentAltitudeFt)
        {
            LastLatitude = currentLat;
            LastLongitude = currentLon;
            LastAltitudeFt = currentAltitudeFt;

            if (_targets.Count == 0) return;

            for (int i = 0; i < _targets.Count; i++)
            {
                var point = _targets[i];
                if (_visited.Contains(point.Ident)) continue;

                double distNM = CalculateDistanceNM(currentLat, currentLon, point.Latitude, point.Longitude);
                if (distNM <= point.RadiusNm)
                {
                    if (EvaluateAltitude(point, currentAltitudeFt) == OverflyAltitudeState.InBand)
                    {
                        _visited.Add(point.Ident);
                    }
                }
            }
        }

        public static double CalculateDistanceNM(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return 6371.0 * c / 1.852;
        }
    }
}
