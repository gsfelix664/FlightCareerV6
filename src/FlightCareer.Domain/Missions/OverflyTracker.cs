using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Rastreia o cumprimento de pontos de sobrevoo obrigatórios em missões panorâmicas.
    /// Condição: aeronave passar a <= 2.0 NM de distância horizontal e altitude <= elevação + 3000 ft.
    /// </summary>
    public sealed class OverflyTracker
    {
        private readonly List<OverflyPoint> _targets = new();
        private readonly HashSet<string> _visited = new(StringComparer.OrdinalIgnoreCase);

        public bool HasWaypoints => _targets.Count > 0;
        public IReadOnlySet<string> VisitedWaypoints => _visited;

        public void ConfigureWaypoints(IEnumerable<OverflyPoint> waypoints)
        {
            _targets.Clear();
            _visited.Clear();
            if (waypoints != null)
            {
                _targets.AddRange(waypoints);
            }
        }

        public void Reset()
        {
            _targets.Clear();
            _visited.Clear();
        }

        public bool IsPointVisited(string ident)
        {
            if (string.IsNullOrWhiteSpace(ident)) return false;
            return _visited.Contains(ident.Trim());
        }

        public void Update(double currentLat, double currentLon, double currentAltitudeFt)
        {
            if (_targets.Count == 0) return;

            for (int i = 0; i < _targets.Count; i++)
            {
                var point = _targets[i];
                if (_visited.Contains(point.Ident)) continue;

                double distNM = CalculateDistanceNM(currentLat, currentLon, point.Latitude, point.Longitude);
                double maxAllowedAltitudeFt = point.ElevationFt + 3000.0;

                if (distNM <= 2.0 && currentAltitudeFt <= maxAllowedAltitudeFt)
                {
                    _visited.Add(point.Ident);
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
