using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FlightCareer.Domain.Geo
{
    public sealed class CountryPolygon
    {
        public double[] OuterRing { get; }
        public double[][]? Holes { get; }

        public CountryPolygon(double[] outerRing, double[][]? holes)
        {
            OuterRing = outerRing ?? throw new ArgumentNullException(nameof(outerRing));
            Holes = holes;
        }
    }

    public sealed class CountryBorder
    {
        public string Iso { get; }
        public double MinLon { get; }
        public double MinLat { get; }
        public double MaxLon { get; }
        public double MaxLat { get; }
        public CountryPolygon[] Polygons { get; }

        public CountryBorder(string iso, double minLon, double minLat, double maxLon, double maxLat, CountryPolygon[] polygons)
        {
            Iso = iso ?? throw new ArgumentNullException(nameof(iso));
            MinLon = minLon;
            MinLat = minLat;
            MaxLon = maxLon;
            MaxLat = maxLat;
            Polygons = polygons ?? Array.Empty<CountryPolygon>();
        }
    }

    public class CountryLocator
    {
        private static readonly Lazy<CountryLocator> s_shared = new(() => LoadDefault());
        public static CountryLocator Shared => s_shared.Value;

        private readonly IReadOnlyList<CountryBorder> _countries;
        private readonly HashSet<string> _knownIsoCodes;

        public IReadOnlySet<string> KnownIsoCodes => _knownIsoCodes;

        public CountryLocator(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var locator = LoadFromJson(json);
            _countries = locator._countries;
            _knownIsoCodes = locator._knownIsoCodes;
        }

        public CountryLocator(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var locator = LoadFromStream(stream);
            _countries = locator._countries;
            _knownIsoCodes = locator._knownIsoCodes;
        }

        public CountryLocator(IEnumerable<CountryBorder> countries, HashSet<string>? knownIsoCodes = null)
        {
            _countries = countries?.ToList() ?? new List<CountryBorder>();
            _knownIsoCodes = knownIsoCodes ?? new HashSet<string>(_countries.Select(c => c.Iso), StringComparer.OrdinalIgnoreCase);
        }

        public string? Locate(double lat, double lon)
        {
            int count = _countries.Count;
            for (int i = 0; i < count; i++)
            {
                var c = _countries[i];
                if (lon < c.MinLon || lon > c.MaxLon || lat < c.MinLat || lat > c.MaxLat)
                {
                    continue;
                }

                var polys = c.Polygons;
                int pCount = polys.Length;
                for (int p = 0; p < pCount; p++)
                {
                    var poly = polys[p];
                    if (IsPointInRing(poly.OuterRing, lon, lat))
                    {
                        bool inHole = false;
                        if (poly.Holes != null)
                        {
                            int hCount = poly.Holes.Length;
                            for (int h = 0; h < hCount; h++)
                            {
                                if (IsPointInRing(poly.Holes[h], lon, lat))
                                {
                                    inHole = true;
                                    break;
                                }
                            }
                        }

                        if (!inHole)
                        {
                            return c.Iso;
                        }
                    }
                }
            }

            return null;
        }

        public static bool IsPointInRing(double[] ring, double px, double py)
        {
            bool inside = false;
            int len = ring.Length;
            int j = len - 2;
            for (int i = 0; i < len; i += 2)
            {
                double xi = ring[i];
                double yi = ring[i + 1];
                double xj = ring[j];
                double yj = ring[j + 1];

                bool intersect = ((yi > py) != (yj > py)) &&
                                 (px < (xj - xi) * (py - yi) / (yj - yi) + xi);
                if (intersect)
                {
                    inside = !inside;
                }

                j = i;
            }
            return inside;
        }

        public static double DistanceNm(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)) * 3440.065;
        }

        public static CountryLocator LoadFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return LoadFromDocument(doc);
        }

        public static CountryLocator LoadFromStream(Stream stream)
        {
            using var doc = JsonDocument.Parse(stream);
            return LoadFromDocument(doc);
        }

        private static CountryLocator LoadFromDocument(JsonDocument doc)
        {
            var root = doc.RootElement;
            var countriesList = new List<CountryBorder>();
            var knownIso = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("countries", out var countriesEl) && countriesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var cEl in countriesEl.EnumerateArray())
                {
                    var iso = cEl.GetProperty("iso").GetString();
                    if (string.IsNullOrWhiteSpace(iso)) continue;
                    iso = iso.Trim().ToUpperInvariant();
                    knownIso.Add(iso);

                    var bboxEl = cEl.GetProperty("bbox");
                    double minLon = bboxEl[0].GetDouble();
                    double minLat = bboxEl[1].GetDouble();
                    double maxLon = bboxEl[2].GetDouble();
                    double maxLat = bboxEl[3].GetDouble();

                    var polysEl = cEl.GetProperty("polys");
                    var polyList = new List<CountryPolygon>();

                    foreach (var pEl in polysEl.EnumerateArray())
                    {
                        int ringCount = pEl.GetArrayLength();
                        if (ringCount == 0) continue;

                        double[]? outerRing = null;
                        List<double[]>? holes = null;

                        int ringIndex = 0;
                        foreach (var rEl in pEl.EnumerateArray())
                        {
                            int coordCount = rEl.GetArrayLength();
                            var coords = new double[coordCount];
                            int ci = 0;
                            foreach (var num in rEl.EnumerateArray())
                            {
                                coords[ci++] = num.GetDouble();
                            }

                            if (ringIndex == 0)
                            {
                                outerRing = coords;
                            }
                            else
                            {
                                holes ??= new List<double[]>();
                                holes.Add(coords);
                            }
                            ringIndex++;
                        }

                        if (outerRing != null && outerRing.Length >= 6)
                        {
                            polyList.Add(new CountryPolygon(outerRing, holes?.ToArray()));
                        }
                    }

                    countriesList.Add(new CountryBorder(iso, minLon, minLat, maxLon, maxLat, polyList.ToArray()));
                }
            }

            return new CountryLocator(countriesList, knownIso);
        }

        public static CountryLocator LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "places", "country_borders.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "places", "country_borders.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "places", "country_borders.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "FlightCareer.Infrastructure", "Data", "places", "country_borders.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "places", "country_borders.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "country_borders.json") }.Concat(candidates).ToArray();
            }

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        using var stream = File.OpenRead(path);
                        return LoadFromStream(stream);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CountryLocator] Erro ao carregar de '{path}': {ex.Message}");
                    }
                }
            }

            Debug.WriteLine("[CountryLocator] Arquivo 'country_borders.json' não encontrado. Retornando localizador vazio.");
            return new CountryLocator(Array.Empty<CountryBorder>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }
    }
}
