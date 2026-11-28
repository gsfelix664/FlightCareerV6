using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FlightCareer.Domain.Missions
{
    public enum AerialWorkAltitudeState
    {
        InBand,
        Below,
        Above
    }

    /// <summary>
    /// Rastreador puro para missões de trabalho aéreo (patrulha, inspeção de linhas,
    /// monitoramento de tráfego em órbita e busca e salvamento - SAR).
    /// </summary>
    public sealed class AerialWorkTracker
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private AerialWorkContractData? _data;
        private readonly HashSet<string> _visitedSectors = new(StringComparer.OrdinalIgnoreCase);
        private int _routeIndex;
        private int _searchLegIndex;
        private (double Lat, double Lon)? _entryPoint;

        public bool HasActiveContract => _data != null;
        public AerialWorkContractData? ContractData => _data;
        public string Mode => _data?.Mode ?? string.Empty;
        public int CompletedPoints { get; private set; }
        public int TotalPoints => _data?.Points?.Count ?? 0;
        public string NextPointIdent { get; private set; } = string.Empty;
        public double OrbitSecondsAccumulated { get; private set; }
        public bool TargetFound { get; private set; }
        public bool IsCompleted { get; private set; }
        public string? FailureReason { get; private set; }

        public double DistanceToNextNm { get; private set; }
        public double LateralDeviationNm { get; private set; }
        public AerialWorkAltitudeState AltitudeState { get; private set; } = AerialWorkAltitudeState.InBand;
        public bool IsIasExceeded { get; private set; }

        public void Configure(AerialWorkContractData? data)
        {
            Reset();
            _data = data;
            if (_data == null) return;

            UpdateProgressMetadata(0, 0);
            UpdateFailureReason();
        }

        public void Reset()
        {
            _data = null;
            _visitedSectors.Clear();
            _routeIndex = 0;
            _searchLegIndex = 0;
            _entryPoint = null;
            CompletedPoints = 0;
            NextPointIdent = string.Empty;
            OrbitSecondsAccumulated = 0;
            TargetFound = false;
            IsCompleted = false;
            FailureReason = null;
            DistanceToNextNm = 0;
            LateralDeviationNm = 0;
            AltitudeState = AerialWorkAltitudeState.InBand;
            IsIasExceeded = false;
        }

        public void Update(bool onGround, double lat, double lon, double altitudeFt, double iasKt, double simDt)
        {
            if (_data == null) return;

            // 1. Avaliação da amostra
            double minAllowedAlt = _data.RefElevationFt + _data.MinAglFt;
            double maxAllowedAlt = _data.RefElevationFt + _data.MaxAglFt;

            if (_data.MinAglFt > 0.0 && altitudeFt < minAllowedAlt)
            {
                AltitudeState = AerialWorkAltitudeState.Below;
            }
            else if (altitudeFt > maxAllowedAlt)
            {
                AltitudeState = AerialWorkAltitudeState.Above;
            }
            else
            {
                AltitudeState = AerialWorkAltitudeState.InBand;
            }

            IsIasExceeded = _data.MaxIasKt > 0.0 && iasKt > _data.MaxIasKt;
            bool isValidSample = !onGround && (AltitudeState == AerialWorkAltitudeState.InBand) && !IsIasExceeded;

            string mode = _data.Mode?.ToLowerInvariant() ?? string.Empty;

            switch (mode)
            {
                case "sectors":
                    UpdateSectors(isValidSample, lat, lon);
                    break;
                case "route":
                    UpdateRoute(isValidSample, lat, lon);
                    break;
                case "orbit":
                    UpdateOrbit(isValidSample, lat, lon, simDt);
                    break;
                case "search":
                    UpdateSearch(isValidSample, lat, lon);
                    break;
            }

            UpdateFailureReason();
        }

        private void UpdateSectors(bool isValidSample, double lat, double lon)
        {
            if (_data?.Points == null || _data.Points.Count == 0) return;

            if (isValidSample)
            {
                for (int i = 0; i < _data.Points.Count; i++)
                {
                    var pt = _data.Points[i];
                    if (_visitedSectors.Contains(pt.Ident)) continue;

                    double d = OverflyTracker.CalculateDistanceNM(lat, lon, pt.Lat, pt.Lon);
                    if (d <= _data.PointRadiusNm)
                    {
                        _visitedSectors.Add(pt.Ident);
                    }
                }
            }

            CompletedPoints = _visitedSectors.Count;
            IsCompleted = CompletedPoints >= _data.Points.Count;

            var unvisited = _data.Points.Where(p => !_visitedSectors.Contains(p.Ident)).ToList();
            if (unvisited.Count > 0)
            {
                var nearest = unvisited
                    .OrderBy(p => OverflyTracker.CalculateDistanceNM(lat, lon, p.Lat, p.Lon))
                    .First();
                NextPointIdent = nearest.Ident;
                DistanceToNextNm = OverflyTracker.CalculateDistanceNM(lat, lon, nearest.Lat, nearest.Lon);
            }
            else
            {
                NextPointIdent = string.Empty;
                DistanceToNextNm = 0;
            }
        }

        private void UpdateRoute(bool isValidSample, double lat, double lon)
        {
            if (_data?.Points == null || _data.Points.Count == 0) return;

            if (_routeIndex < _data.Points.Count)
            {
                var targetPt = _data.Points[_routeIndex];
                DistanceToNextNm = OverflyTracker.CalculateDistanceNM(lat, lon, targetPt.Lat, targetPt.Lon);
                NextPointIdent = targetPt.Ident;

                // Cálculo do desvio lateral
                if (_routeIndex == 0)
                {
                    if (!_entryPoint.HasValue && DistanceToNextNm <= 2.0)
                    {
                        _entryPoint = (lat, lon);
                    }

                    if (_entryPoint.HasValue)
                    {
                        LateralDeviationNm = CalculateCrossTrackDistance(
                            _entryPoint.Value.Lat, _entryPoint.Value.Lon,
                            targetPt.Lat, targetPt.Lon,
                            lat, lon);
                    }
                    else
                    {
                        LateralDeviationNm = DistanceToNextNm;
                    }
                }
                else
                {
                    var prevPt = _data.Points[_routeIndex - 1];
                    LateralDeviationNm = CalculateCrossTrackDistance(
                        prevPt.Lat, prevPt.Lon,
                        targetPt.Lat, targetPt.Lon,
                        lat, lon);
                }

                if (isValidSample && DistanceToNextNm <= _data.PointRadiusNm)
                {
                    _routeIndex++;
                    CompletedPoints = _routeIndex;
                    if (_routeIndex >= _data.Points.Count)
                    {
                        IsCompleted = true;
                        DistanceToNextNm = 0;
                        LateralDeviationNm = 0;
                        NextPointIdent = string.Empty;
                    }
                    else
                    {
                        var nextPt = _data.Points[_routeIndex];
                        NextPointIdent = nextPt.Ident;
                        DistanceToNextNm = OverflyTracker.CalculateDistanceNM(lat, lon, nextPt.Lat, nextPt.Lon);
                    }
                }
            }
            else
            {
                IsCompleted = true;
                CompletedPoints = _data.Points.Count;
                DistanceToNextNm = 0;
                LateralDeviationNm = 0;
                NextPointIdent = string.Empty;
            }
        }

        private void UpdateOrbit(bool isValidSample, double lat, double lon, double simDt)
        {
            if (_data == null) return;

            double distToCenter = OverflyTracker.CalculateDistanceNM(lat, lon, _data.OrbitCenterLat, _data.OrbitCenterLon);
            DistanceToNextNm = distToCenter;
            NextPointIdent = "Órbita";

            if (isValidSample && distToCenter <= _data.OrbitRadiusNm)
            {
                OrbitSecondsAccumulated += Math.Max(0.0, simDt);
            }

            if (OrbitSecondsAccumulated >= _data.OrbitMinutes * 60.0)
            {
                IsCompleted = true;
            }
        }

        private void UpdateSearch(bool isValidSample, double lat, double lon)
        {
            if (_data == null) return;

            // 1. Verificação do alvo a qualquer momento
            double distTarget = OverflyTracker.CalculateDistanceNM(lat, lon, _data.TargetLat, _data.TargetLon);
            if (isValidSample && distTarget <= _data.TargetRadiusNm)
            {
                TargetFound = true;
                IsCompleted = true;
            }

            // 2. Progresso pelas pernas do padrão
            if (_data.Points != null && _data.Points.Count > 0)
            {
                if (_searchLegIndex < _data.Points.Count)
                {
                    var pt = _data.Points[_searchLegIndex];
                    DistanceToNextNm = OverflyTracker.CalculateDistanceNM(lat, lon, pt.Lat, pt.Lon);
                    NextPointIdent = pt.Ident;

                    if (isValidSample && DistanceToNextNm <= _data.PointRadiusNm)
                    {
                        _searchLegIndex++;
                        CompletedPoints = _searchLegIndex;
                        if (_searchLegIndex < _data.Points.Count)
                        {
                            var nextPt = _data.Points[_searchLegIndex];
                            NextPointIdent = nextPt.Ident;
                            DistanceToNextNm = OverflyTracker.CalculateDistanceNM(lat, lon, nextPt.Lat, nextPt.Lon);
                        }
                    }
                }
                else
                {
                    CompletedPoints = _data.Points.Count;
                }
            }
        }

        private void UpdateProgressMetadata(double lat, double lon)
        {
            if (_data == null) return;
            string mode = _data.Mode?.ToLowerInvariant() ?? string.Empty;
            if (mode == "sectors" && _data.Points?.Count > 0)
            {
                NextPointIdent = _data.Points[0].Ident;
            }
            else if (mode == "route" && _data.Points?.Count > 0)
            {
                NextPointIdent = _data.Points[0].Ident;
            }
            else if (mode == "orbit")
            {
                NextPointIdent = "Órbita";
            }
            else if (mode == "search" && _data.Points?.Count > 0)
            {
                NextPointIdent = _data.Points[0].Ident;
            }
        }

        private void UpdateFailureReason()
        {
            if (_data == null)
            {
                FailureReason = null;
                return;
            }

            if (IsCompleted)
            {
                FailureReason = null;
                return;
            }

            string mode = _data.Mode?.ToLowerInvariant() ?? string.Empty;
            double maxAlt = _data.RefElevationFt + _data.MaxAglFt;

            switch (mode)
            {
                case "sectors":
                    var missing = (_data.Points ?? new List<WorkPoint>())
                        .Where(p => !_visitedSectors.Contains(p.Ident))
                        .Select(p => p.Ident);
                    string altText = _data.MinAglFt > 0.0
                        ? $"voe entre {(_data.RefElevationFt + _data.MinAglFt).ToString("N0", PtBr)} e {maxAlt.ToString("N0", PtBr)} ft"
                        : $"voe a até {maxAlt.ToString("N0", PtBr)} ft";
                    string speedText = _data.MaxIasKt > 0.0 ? $" e {_data.MaxIasKt.ToString("N0", PtBr)} kt" : "";
                    FailureReason = $"Setores sem cobertura: {string.Join(", ", missing)} ({altText}{speedText})";
                    break;

                case "route":
                    int nextIdx = _routeIndex + 1;
                    string ident = (nextIdx <= (_data.Points?.Count ?? 0))
                        ? _data.Points![nextIdx - 1].Ident
                        : $"L{nextIdx}";
                    FailureReason = $"Traçado incompleto: ponto {ident} de {_data.Points?.Count ?? 0} não inspecionado";
                    break;

                case "orbit":
                    int minAccum = (int)Math.Floor(OrbitSecondsAccumulated / 60.0);
                    FailureReason = $"Órbita de {minAccum} de {(int)_data.OrbitMinutes} min";
                    break;

                case "search":
                    int leg = Math.Min(_searchLegIndex + 1, _data.Points?.Count ?? 0);
                    FailureReason = $"Alvo não encontrado (perna {leg} de {_data.Points?.Count ?? 0} do padrão)";
                    break;

                default:
                    FailureReason = "Trabalho aéreo não concluído.";
                    break;
            }
        }

        public static double CalculateCrossTrackDistance(
            double latA, double lonA,
            double latB, double lonB,
            double latC, double lonC)
        {
            double midLat = (latA + latB) / 2.0;
            double cosLat = Math.Max(0.01, Math.Cos(midLat * Math.PI / 180.0));

            double dx = (lonB - lonA) * 60.0 * cosLat;
            double dy = (latB - latA) * 60.0;
            double len = Math.Sqrt((dx * dx) + (dy * dy));

            if (len < 0.0001)
            {
                return OverflyTracker.CalculateDistanceNM(latC, lonC, latB, lonB);
            }

            double px = (lonC - lonA) * 60.0 * cosLat;
            double py = (latC - latA) * 60.0;

            // Distância perpendicular à reta: |px * dy - py * dx| / len
            double crossProduct = (px * dy) - (py * dx);
            return Math.Abs(crossProduct) / len;
        }
    }
}
