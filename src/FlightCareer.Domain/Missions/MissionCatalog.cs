using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace FlightCareer.Domain.Missions
{
    public class MissionCatalog
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        private static readonly Lazy<MissionCatalog> s_shared = new(() => LoadDefault());
        public static MissionCatalog Shared => s_shared.Value;

        private readonly Dictionary<string, MissionDefinition> _missionsById;
        private readonly Dictionary<string, string> _legacyCodeToId;
        private readonly Dictionary<int, TierLayoutDefinition> _tiers;
        private readonly List<AirportAdjustmentDefinition> _airportAdjustments;

        public IReadOnlyDictionary<string, MissionDefinition> Missions => _missionsById;
        public IReadOnlyDictionary<string, string> LegacyCodes => _legacyCodeToId;
        public IReadOnlyDictionary<int, TierLayoutDefinition> Tiers => _tiers;
        public IReadOnlyList<AirportAdjustmentDefinition> AirportAdjustments => _airportAdjustments;

        public MissionCatalog(
            IEnumerable<MissionDefinition> missions,
            IDictionary<int, TierLayoutDefinition> tiers,
            IEnumerable<AirportAdjustmentDefinition>? airportAdjustments = null)
        {
            _missionsById = new Dictionary<string, MissionDefinition>(StringComparer.OrdinalIgnoreCase);
            _legacyCodeToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in missions)
            {
                _missionsById[m.Id] = m;
                if (m.LegacyCodes != null)
                {
                    foreach (var code in m.LegacyCodes)
                    {
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            _legacyCodeToId[code.Trim()] = m.Id;
                        }
                    }
                }
            }

            _tiers = new Dictionary<int, TierLayoutDefinition>(tiers);
            _airportAdjustments = airportAdjustments?.ToList() ?? new List<AirportAdjustmentDefinition>();
        }

        public MissionDefinition? Get(string? idOrCode)
        {
            if (string.IsNullOrWhiteSpace(idOrCode)) return null;
            string key = idOrCode.Trim();

            if (_missionsById.TryGetValue(key, out var def))
                return def;

            if (_legacyCodeToId.TryGetValue(key, out var normalizedId) && _missionsById.TryGetValue(normalizedId, out var normalizedDef))
                return normalizedDef;

            return null;
        }

        public string Normalize(string? idOrCode)
        {
            if (string.IsNullOrWhiteSpace(idOrCode)) return string.Empty;
            string key = idOrCode.Trim();

            if (_missionsById.ContainsKey(key))
                return key;

            if (_legacyCodeToId.TryGetValue(key, out var normalizedId))
                return normalizedId;

            return key;
        }

        public TierLayoutDefinition? TierLayout(int tier)
        {
            return _tiers.GetValueOrDefault(tier);
        }

        public static MissionCatalog LoadFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var missions = new List<MissionDefinition>();
            if (root.TryGetProperty("missions", out var missionsEl) && missionsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in missionsEl.EnumerateArray())
                {
                    var m = ParseMissionDefinition(el);
                    if (m != null) missions.Add(m);
                }
            }

            var tiers = new Dictionary<int, TierLayoutDefinition>();
            if (root.TryGetProperty("tiers", out var tiersEl) && tiersEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in tiersEl.EnumerateObject())
                {
                    if (int.TryParse(prop.Name, out int tierNum))
                    {
                        var layout = JsonSerializer.Deserialize<TierLayoutDefinition>(prop.Value.GetRawText(), JsonOpts);
                        if (layout != null) tiers[tierNum] = layout;
                    }
                }
            }

            var adjustments = new List<AirportAdjustmentDefinition>();
            if (root.TryGetProperty("airportAdjustments", out var adjEl) && adjEl.ValueKind == JsonValueKind.Array)
            {
                var list = JsonSerializer.Deserialize<List<AirportAdjustmentDefinition>>(adjEl.GetRawText(), JsonOpts);
                if (list != null) adjustments = list;
            }

            return new MissionCatalog(missions, tiers, adjustments);
        }

        private static MissionDefinition? ParseMissionDefinition(JsonElement el)
        {
            return JsonSerializer.Deserialize<MissionDefinition>(el.GetRawText(), JsonOpts);
        }

        public static MissionCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "mission_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "mission_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "mission_catalog.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "mission_catalog.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "mission_catalog.json") }.Concat(candidates).ToArray();
            }

            Exception? lastEx = null;
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
                        lastEx = ex;
                        Debug.WriteLine($"[MissionCatalog] Erro ao carregar de '{path}': {ex.Message}");
                    }
                }
            }

            // Fallback para recurso embutido
            var asm = typeof(MissionCatalog).Assembly;
            string[] resNames = asm.GetManifestResourceNames();
            var targetRes = resNames.FirstOrDefault(r => r.EndsWith("mission_catalog.json", StringComparison.OrdinalIgnoreCase));
            if (targetRes != null)
            {
                try
                {
                    using var stream = asm.GetManifestResourceStream(targetRes);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        string json = reader.ReadToEnd();
                        return LoadFromJson(json);
                    }
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    Debug.WriteLine($"[MissionCatalog] Erro ao carregar recurso embutido '{targetRes}': {ex.Message}");
                }
            }

            if (lastEx != null)
            {
                throw new InvalidOperationException($"Falha ao carregar 'mission_catalog.json': {lastEx.Message}", lastEx);
            }

            throw new FileNotFoundException($"Arquivo 'mission_catalog.json' não encontrado nos caminhos buscados nem embutido no assembly.");
        }
    }
}
