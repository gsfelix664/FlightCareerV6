using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FlightCareer.Domain.Missions
{
    public sealed class AirlineRouteCatalog
    {
        private static readonly Lazy<AirlineRouteCatalog> s_default = new(() => LoadDefault());
        public static AirlineRouteCatalog Default => s_default.Value;

        public static AirlineRouteCatalog Empty { get; } = new(new Dictionary<string, IReadOnlyDictionary<string, int>>());

        private readonly Dictionary<string, Dictionary<string, int>> _routes;
        public int PairCount { get; }

        public AirlineRouteCatalog(IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> routes)
        {
            _routes = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var uniquePairs = new HashSet<(string, string)>();

            if (routes != null)
            {
                foreach (var (origin, dests) in routes)
                {
                    if (string.IsNullOrWhiteSpace(origin) || dests == null) continue;

                    var inner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var (dest, count) in dests)
                    {
                        if (string.IsNullOrWhiteSpace(dest)) continue;
                        inner[dest] = count;

                        string a = origin.ToUpperInvariant();
                        string b = dest.ToUpperInvariant();
                        if (string.CompareOrdinal(a, b) <= 0)
                        {
                            uniquePairs.Add((a, b));
                        }
                        else
                        {
                            uniquePairs.Add((b, a));
                        }
                    }
                    _routes[origin] = inner;
                }
            }

            PairCount = uniquePairs.Count;
        }

        public static AirlineRouteCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "places", "airline_routes.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "places", "airline_routes.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "places", "airline_routes.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "FlightCareer.Infrastructure", "Data", "places", "airline_routes.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "places", "airline_routes.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "airline_routes.json") }.Concat(candidates).ToArray();
            }

            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AirlineRouteCatalog] Arquivo 'airline_routes.json' não encontrado. Buscado em: {string.Join(", ", candidates)}");
                return Empty;
            }

            try
            {
                string json = File.ReadAllText(path);
                return LoadFromJson(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AirlineRouteCatalog] Erro ao ler arquivo '{path}': {ex.Message}");
                return Empty;
            }
        }

        public static AirlineRouteCatalog LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Empty;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("routes", out var routesElem) || routesElem.ValueKind != JsonValueKind.Object)
                {
                    return Empty;
                }

                var routesDict = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

                foreach (var originProp in routesElem.EnumerateObject())
                {
                    if (originProp.Value.ValueKind != JsonValueKind.Object) continue;

                    var inner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var destProp in originProp.Value.EnumerateObject())
                    {
                        if (destProp.Value.TryGetInt32(out int n))
                        {
                            inner[destProp.Name] = n;
                        }
                    }
                    routesDict[originProp.Name] = inner;
                }

                return new AirlineRouteCatalog(routesDict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AirlineRouteCatalog] Falha ao processar JSON: {ex.Message}");
                return Empty;
            }
        }

        public int Airlines(string? originIcao, string? destIcao)
        {
            if (string.IsNullOrWhiteSpace(originIcao) || string.IsNullOrWhiteSpace(destIcao))
            {
                return 0;
            }

            if (_routes.TryGetValue(originIcao, out var dests) && dests.TryGetValue(destIcao, out int count))
            {
                return count;
            }

            return 0;
        }

        public bool HasRoute(string? originIcao, string? destIcao) => Airlines(originIcao, destIcao) > 0;
    }
}
