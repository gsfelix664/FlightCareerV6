using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FlightCareer.Domain.Missions
{
    public class AirportProfileCatalog
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly Lazy<AirportProfileCatalog> s_shared = new(() => LoadDefault());
        public static AirportProfileCatalog Shared => s_shared.Value;

        private static readonly IReadOnlySet<string> EmptyTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IReadOnlySet<string>> _profiles;

        public AirportProfileCatalog(IReadOnlyDictionary<string, IReadOnlySet<string>> profiles)
        {
            _profiles = new Dictionary<string, IReadOnlySet<string>>(profiles, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlySet<string> TagsFor(string? icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return EmptyTags;
            return _profiles.TryGetValue(icao.Trim(), out var tags) ? tags : EmptyTags;
        }

        public static bool MatchTag(string pattern, string tag)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(tag)) return false;

            // "militar!" = só a etiqueta exata (não casa os subtipos "militar:xxx", que também aparecem
            // em aeroportos de uso misto)
            if (pattern.EndsWith('!'))
            {
                return string.Equals(pattern[..^1], tag, StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.Contains(':'))
            {
                return string.Equals(pattern, tag, StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(pattern, tag, StringComparison.OrdinalIgnoreCase)) return true;

            return tag.StartsWith(pattern + ":", StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesAny(string pattern, IEnumerable<string>? tags)
        {
            if (tags == null) return false;
            foreach (var t in tags)
            {
                if (MatchTag(pattern, t)) return true;
            }
            return false;
        }

        public static AirportProfileCatalog LoadFromJson(string json)
        {
            var dict = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("airports", out var airportsEl) && airportsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in airportsEl.EnumerateObject())
                {
                    var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (prop.Value.TryGetProperty("t", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var t in tagsEl.EnumerateArray())
                        {
                            var s = t.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                tagSet.Add(s.Trim());
                            }
                        }
                    }
                    dict[prop.Name] = tagSet;
                }
            }

            return new AirportProfileCatalog(dict);
        }

        public static AirportProfileCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "places", "airport_profiles.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "places", "airport_profiles.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "places", "airport_profiles.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "FlightCareer.Infrastructure", "Data", "places", "airport_profiles.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "places", "airport_profiles.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "airport_profiles.json") }.Concat(candidates).ToArray();
            }

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        return LoadFromJson(json);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AirportProfileCatalog] Erro ao carregar de '{path}': {ex.Message}");
                    }
                }
            }

            Debug.WriteLine("[AirportProfileCatalog] Arquivo 'airport_profiles.json' não encontrado. Retornando catálogo vazio.");
            return new AirportProfileCatalog(new Dictionary<string, IReadOnlySet<string>>());
        }
    }
}
