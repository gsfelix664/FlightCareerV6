using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FlightCareer.Application.Interfaces;

namespace FlightCareer.Application.Services
{
    public class PlacesDataService : IPlacesDataService
    {
        private static readonly object s_lock = new();
        private static bool s_loaded = false;

        private static readonly Dictionary<string, DropZoneInfo> s_dropZones = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> s_trainingAerodromes = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ScenicAerodromeInfo> s_scenicAerodromes = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<(int LatCell, int LonCell), List<TouristPoiInfo>> s_poiGrid = new();
        private static readonly List<TouristPoiInfo> s_allPois = new();

        private readonly Dictionary<string, DropZoneInfo>? _customDropZones;
        private readonly HashSet<string>? _customTrainingAerodromes;
        private readonly Dictionary<string, ScenicAerodromeInfo>? _customScenicAerodromes;
        private readonly Dictionary<(int LatCell, int LonCell), List<TouristPoiInfo>>? _customPoiGrid;
        private readonly List<TouristPoiInfo>? _customAllPois;

        public PlacesDataService()
        {
            EnsureDataLoaded();
        }

        public PlacesDataService(
            IEnumerable<DropZoneInfo>? dropZones,
            IEnumerable<string>? trainingAerodromes,
            IEnumerable<TouristPoiInfo>? pois,
            IEnumerable<ScenicAerodromeInfo>? scenicAerodromes = null)
        {
            if (dropZones != null || trainingAerodromes != null || pois != null || scenicAerodromes != null)
            {
                _customDropZones = new Dictionary<string, DropZoneInfo>(StringComparer.OrdinalIgnoreCase);
                if (dropZones != null)
                {
                    foreach (var dz in dropZones)
                    {
                        if (!string.IsNullOrWhiteSpace(dz.Icao))
                            _customDropZones[dz.Icao.Trim().ToUpperInvariant()] = dz;
                    }
                }

                _customTrainingAerodromes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (trainingAerodromes != null)
                {
                    foreach (var icao in trainingAerodromes)
                    {
                        if (!string.IsNullOrWhiteSpace(icao))
                            _customTrainingAerodromes.Add(icao.Trim().ToUpperInvariant());
                    }
                }

                if (scenicAerodromes != null)
                {
                    _customScenicAerodromes = new Dictionary<string, ScenicAerodromeInfo>(StringComparer.OrdinalIgnoreCase);
                    foreach (var sc in scenicAerodromes)
                    {
                        if (!string.IsNullOrWhiteSpace(sc.Icao))
                            _customScenicAerodromes[sc.Icao.Trim().ToUpperInvariant()] = sc;
                    }
                }

                _customPoiGrid = new Dictionary<(int, int), List<TouristPoiInfo>>();
                _customAllPois = new List<TouristPoiInfo>();
                if (pois != null)
                {
                    foreach (var poi in pois)
                    {
                        _customAllPois.Add(poi);
                        var cell = ((int)Math.Floor(poi.Lat), (int)Math.Floor(poi.Lon));
                        if (!_customPoiGrid.TryGetValue(cell, out var list))
                        {
                            list = new List<TouristPoiInfo>();
                            _customPoiGrid[cell] = list;
                        }
                        list.Add(poi);
                    }
                }
            }
            else
            {
                EnsureDataLoaded();
            }
        }

        public static void EnsureDataLoaded()
        {
            if (s_loaded) return;

            lock (s_lock)
            {
                if (s_loaded) return;

                LoadDropZones();
                LoadTrainingAerodromes();
                LoadScenicAerodromes();
                LoadTouristPois();

                s_loaded = true;
            }
        }

        private static string? ResolvePlacesFilePath(string fileName)
        {
            var p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "places", fileName);
            if (File.Exists(p1)) return p1;

            var p2 = Path.Combine(Directory.GetCurrentDirectory(), "Data", "places", fileName);
            if (File.Exists(p2)) return p2;

            var p3 = Path.Combine(Directory.GetCurrentDirectory(), "..", "FlightCareer.Infrastructure", "Data", "places", fileName);
            if (File.Exists(p3)) return p3;

            var p4 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "FlightCareer.Infrastructure", "Data", "places", fileName);
            if (File.Exists(p4)) return p4;

            return null;
        }

        private static void LoadDropZones()
        {
            try
            {
                var path = ResolvePlacesFilePath("dropzones.json");
                if (path == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PlacesDataService] dropzones.json não encontrado.");
                    return;
                }

                using var stream = File.OpenRead(path);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("dropzones", out var arrayElem) && arrayElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        string icao = elem.GetProperty("icao").GetString()?.Trim().ToUpperInvariant() ?? "";
                        string airport = elem.TryGetProperty("airport", out var a) ? a.GetString() ?? "" : "";
                        string cc = elem.TryGetProperty("cc", out var c) ? c.GetString() ?? "" : "";
                        string name = elem.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        double lat = elem.GetProperty("lat").GetDouble();
                        double lon = elem.GetProperty("lon").GetDouble();
                        string note = elem.TryGetProperty("note", out var nt) ? nt.GetString() ?? "" : "";

                        if (!string.IsNullOrEmpty(icao))
                        {
                            s_dropZones[icao] = new DropZoneInfo(icao, airport, cc, name, lat, lon, note);
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Carregadas {s_dropZones.Count} zonas de salto.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Erro ao carregar dropzones.json: {ex.Message}");
            }
        }

        private static void LoadTrainingAerodromes()
        {
            try
            {
                var path = ResolvePlacesFilePath("training_aerodromes.json");
                if (path == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PlacesDataService] training_aerodromes.json não encontrado.");
                    return;
                }

                using var stream = File.OpenRead(path);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("aerodromes", out var arrayElem) && arrayElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        string icao = elem.GetProperty("icao").GetString()?.Trim().ToUpperInvariant() ?? "";
                        if (!string.IsNullOrEmpty(icao))
                        {
                            s_trainingAerodromes.Add(icao);
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Carregados {s_trainingAerodromes.Count} aeródromos de instrução.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Erro ao carregar training_aerodromes.json: {ex.Message}");
            }
        }

        private static void LoadScenicAerodromes()
        {
            try
            {
                var path = ResolvePlacesFilePath("scenic_aerodromes.json");
                if (path == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PlacesDataService] scenic_aerodromes.json não encontrado.");
                    return;
                }

                using var stream = File.OpenRead(path);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("aerodromes", out var arrayElem) && arrayElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        string icao = elem.GetProperty("icao").GetString()?.Trim().ToUpperInvariant() ?? "";
                        string name = elem.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        string cc = elem.TryGetProperty("cc", out var c) ? c.GetString() ?? "" : "";
                        string note = elem.TryGetProperty("note", out var nt) ? nt.GetString() ?? "" : "";

                        var points = new List<ScenicPoint>();
                        if (elem.TryGetProperty("points", out var ptsElem) && ptsElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var pt in ptsElem.EnumerateArray())
                            {
                                string ptName = pt.GetProperty("n").GetString() ?? "";
                                double lat = pt.GetProperty("lat").GetDouble();
                                double lon = pt.GetProperty("lon").GetDouble();
                                if (!string.IsNullOrWhiteSpace(ptName))
                                {
                                    points.Add(new ScenicPoint(ptName, lat, lon));
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(icao))
                        {
                            s_scenicAerodromes[icao] = new ScenicAerodromeInfo(icao, name, cc, note, points);
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Carregados {s_scenicAerodromes.Count} aeródromos turísticos.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Erro ao carregar scenic_aerodromes.json: {ex.Message}");
            }
        }

        private static void LoadTouristPois()
        {
            try
            {
                var path = ResolvePlacesFilePath("tourist_pois.json");
                if (path == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PlacesDataService] tourist_pois.json não encontrado.");
                    return;
                }

                using var stream = File.OpenRead(path);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("pois", out var arrayElem) && arrayElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        string id = elem.GetProperty("id").GetString() ?? "";
                        string n = elem.GetProperty("n").GetString() ?? "";
                        double lat = elem.GetProperty("lat").GetDouble();
                        double lon = elem.GetProperty("lon").GetDouble();
                        string k = elem.GetProperty("k").GetString() ?? "";
                        string cc = elem.TryGetProperty("cc", out var c) ? c.GetString() ?? "" : "";
                        int p = elem.TryGetProperty("p", out var pElem) ? pElem.GetInt32() : 0;

                        var poi = new TouristPoiInfo(id, n, lat, lon, k, cc, p);
                        s_allPois.Add(poi);

                        var cell = ((int)Math.Floor(lat), (int)Math.Floor(lon));
                        if (!s_poiGrid.TryGetValue(cell, out var list))
                        {
                            list = new List<TouristPoiInfo>();
                            s_poiGrid[cell] = list;
                        }
                        list.Add(poi);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Carregados {s_allPois.Count} pontos turísticos (POIs).");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlacesDataService] Erro ao carregar tourist_pois.json: {ex.Message}");
            }
        }

        public int TouristPoisCount => _customAllPois != null ? _customAllPois.Count : s_allPois.Count;
        public int TrainingAerodromesCount => _customTrainingAerodromes != null ? _customTrainingAerodromes.Count : s_trainingAerodromes.Count;
        public int DropZonesCount => _customDropZones != null ? _customDropZones.Count : s_dropZones.Count;
        public int ScenicAerodromesCount => _customScenicAerodromes != null ? _customScenicAerodromes.Count : s_scenicAerodromes.Count;

        public bool IsTrainingAerodrome(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return false;
            string key = icao.Trim().ToUpperInvariant();
            if (_customTrainingAerodromes != null)
                return _customTrainingAerodromes.Contains(key);

            return s_trainingAerodromes.Contains(key);
        }

        public DropZoneInfo? GetDropZone(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return null;
            string key = icao.Trim().ToUpperInvariant();
            if (_customDropZones != null)
            {
                return _customDropZones.TryGetValue(key, out var dz) ? dz : null;
            }

            return s_dropZones.TryGetValue(key, out var globalDz) ? globalDz : null;
        }

        public ScenicAerodromeInfo? GetScenicAerodrome(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return null;
            string key = icao.Trim().ToUpperInvariant();
            if (_customScenicAerodromes != null)
            {
                return _customScenicAerodromes.TryGetValue(key, out var sc) ? sc : null;
            }

            return s_scenicAerodromes.TryGetValue(key, out var globalSc) ? globalSc : null;
        }

        public IReadOnlyList<TouristPoiInfo> FindPois(double lat, double lon, double minNm, double maxNm, IReadOnlySet<string>? kinds = null)
        {
            var grid = _customPoiGrid ?? s_poiGrid;
            var results = new List<TouristPoiInfo>();

            int minLatCell = (int)Math.Floor(lat - (maxNm / 60.0));
            int maxLatCell = (int)Math.Floor(lat + (maxNm / 60.0));
            double cosLat = Math.Max(0.01, Math.Cos(lat * Math.PI / 180.0));
            int minLonCell = (int)Math.Floor(lon - (maxNm / (60.0 * cosLat)));
            int maxLonCell = (int)Math.Floor(lon + (maxNm / (60.0 * cosLat)));

            for (int rLat = minLatCell; rLat <= maxLatCell; rLat++)
            {
                for (int rLon = minLonCell; rLon <= maxLonCell; rLon++)
                {
                    if (grid.TryGetValue((rLat, rLon), out var cellPois))
                    {
                        for (int i = 0; i < cellPois.Count; i++)
                        {
                            var poi = cellPois[i];
                            if (kinds != null && !kinds.Contains(poi.Kind))
                                continue;

                            double distNm = CalculateDistanceNM(lat, lon, poi.Lat, poi.Lon);
                            if (distNm >= minNm && distNm <= maxNm)
                            {
                                results.Add(poi);
                            }
                        }
                    }
                }
            }

            return results;
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
