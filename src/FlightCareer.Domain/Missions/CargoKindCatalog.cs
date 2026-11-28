using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace FlightCareer.Domain.Missions
{
    public sealed record DangerousGoodsClass(string Code, string Label, bool CargoAircraftOnly);
    public sealed record CargoKindDefinition(string Id, double Weight, IReadOnlyDictionary<string, double> Tags, IReadOnlyList<string> Names, string? Dg);
    public sealed record CargoPick(string KindId, string Name, DangerousGoodsClass? Dg);

    public class CargoKindCatalog
    {
        private static readonly Lazy<CargoKindCatalog> s_default = new(() => LoadDefault());
        public static CargoKindCatalog Default => s_default.Value;

        private readonly Dictionary<string, DangerousGoodsClass> _dangerousGoods;
        private readonly Dictionary<string, List<CargoKindDefinition>> _missions;

        public IReadOnlyDictionary<string, DangerousGoodsClass> DangerousGoods => _dangerousGoods;
        public IReadOnlyDictionary<string, List<CargoKindDefinition>> Missions => _missions;

        public CargoKindCatalog(
            Dictionary<string, DangerousGoodsClass> dangerousGoods,
            Dictionary<string, List<CargoKindDefinition>> missions)
        {
            _dangerousGoods = new Dictionary<string, DangerousGoodsClass>(dangerousGoods, StringComparer.OrdinalIgnoreCase);
            _missions = new Dictionary<string, List<CargoKindDefinition>>(missions, StringComparer.OrdinalIgnoreCase);
        }

        public static CargoKindCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "cargo_kinds.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "cargo_kinds.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "cargo_kinds.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "FlightCareer.Infrastructure", "Data", "cargo_kinds.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "cargo_kinds.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "cargo_kinds.json") }.Concat(candidates).ToArray();
            }

            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoKindCatalog] Arquivo 'cargo_kinds.json' não encontrado. Buscado em: {string.Join(", ", candidates)}");
                return new CargoKindCatalog(new Dictionary<string, DangerousGoodsClass>(), new Dictionary<string, List<CargoKindDefinition>>());
            }

            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        public static CargoKindCatalog LoadFromJson(string json)
        {
            var dgDict = new Dictionary<string, DangerousGoodsClass>(StringComparer.OrdinalIgnoreCase);
            var missionsDict = new Dictionary<string, List<CargoKindDefinition>>(StringComparer.OrdinalIgnoreCase);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("dangerousGoods", out var dgElement) && dgElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in dgElement.EnumerateObject())
                {
                    string code = prop.Name;
                    string label = prop.Value.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
                    bool cargoOnly = prop.Value.TryGetProperty("cargoAircraftOnly", out var c) && c.GetBoolean();
                    dgDict[code] = new DangerousGoodsClass(code, label, cargoOnly);
                }
            }

            if (root.TryGetProperty("missions", out var missionsElement) && missionsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var missionProp in missionsElement.EnumerateObject())
                {
                    var list = new List<CargoKindDefinition>();
                    if (missionProp.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in missionProp.Value.EnumerateArray())
                        {
                            string id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                            double weight = item.TryGetProperty("weight", out var wProp) ? wProp.GetDouble() : 0.0;
                            string? dg = item.TryGetProperty("dg", out var dgProp) && dgProp.ValueKind == JsonValueKind.String ? dgProp.GetString() : null;

                            var tags = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                            if (item.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var t in tagsProp.EnumerateObject())
                                {
                                    tags[t.Name] = t.Value.GetDouble();
                                }
                            }

                            var names = new List<string>();
                            if (item.TryGetProperty("names", out var namesProp) && namesProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var n in namesProp.EnumerateArray())
                                {
                                    names.Add(n.GetString() ?? "");
                                }
                            }

                            list.Add(new CargoKindDefinition(id, weight, tags, names, dg));
                        }
                    }
                    missionsDict[missionProp.Name] = list;
                }
            }

            return new CargoKindCatalog(dgDict, missionsDict);
        }

        public bool HasMission(string missionId)
        {
            if (_missions.ContainsKey(missionId)) return true;
            string normalized = MissionCatalog.Shared.Normalize(missionId);
            return _missions.ContainsKey(normalized);
        }

        private List<CargoKindDefinition>? ResolveMissionKinds(string missionId)
        {
            if (_missions.TryGetValue(missionId, out var kinds) && kinds != null)
            {
                return kinds;
            }
            string normalized = MissionCatalog.Shared.Normalize(missionId);
            if (normalized != missionId && _missions.TryGetValue(normalized, out kinds) && kinds != null)
            {
                return kinds;
            }
            return null;
        }

        public CargoKindDefinition? GetKind(string missionId, string kindId)
        {
            var kinds = ResolveMissionKinds(missionId);
            return kinds?.FirstOrDefault(k => string.Equals(k.Id, kindId, StringComparison.OrdinalIgnoreCase));
        }

        public DangerousGoodsClass? GetDangerousGoods(string missionId, string? kindId)
        {
            if (string.IsNullOrEmpty(kindId)) return null;
            var kind = GetKind(missionId, kindId);
            if (kind == null || string.IsNullOrEmpty(kind.Dg)) return null;

            _dangerousGoods.TryGetValue(kind.Dg, out var dgClass);
            return dgClass;
        }

        public CargoPick? Pick(string missionId, IReadOnlySet<string>? originTags, IReadOnlySet<string>? destTags, int seed)
        {
            var kinds = ResolveMissionKinds(missionId);
            if (kinds == null || kinds.Count == 0) return null;

            var eligible = new List<(CargoKindDefinition Kind, double Weight)>();
            foreach (var k in kinds)
            {
                double effectiveWeight = k.Weight;
                if (k.Tags != null && k.Tags.Count > 0)
                {
                    foreach (var kvp in k.Tags)
                    {
                        bool hasTag = (originTags != null && originTags.Contains(kvp.Key)) ||
                                      (destTags != null && destTags.Contains(kvp.Key));
                        if (hasTag)
                        {
                            effectiveWeight += kvp.Value;
                        }
                    }
                }

                if (effectiveWeight > 0.0)
                {
                    eligible.Add((k, effectiveWeight));
                }
            }

            if (eligible.Count == 0) return null;

            double totalWeight = eligible.Sum(x => x.Weight);
            var rng = new Random(seed);
            double roll = rng.NextDouble() * totalWeight;
            double accumulated = 0.0;
            CargoKindDefinition selected = eligible[0].Kind;

            foreach (var item in eligible)
            {
                accumulated += item.Weight;
                if (accumulated >= roll)
                {
                    selected = item.Kind;
                    break;
                }
            }

            if (selected.Names == null || selected.Names.Count == 0) return null;

            int nameIndex = rng.Next(selected.Names.Count);
            string selectedName = selected.Names[nameIndex];

            DangerousGoodsClass? dgClass = null;
            if (!string.IsNullOrEmpty(selected.Dg) && _dangerousGoods.TryGetValue(selected.Dg, out var resolvedDg))
            {
                dgClass = resolvedDg;
            }

            return new CargoPick(selected.Id, selectedName, dgClass);
        }

        public static int StableSeed(params string[] parts)
        {
            string text = string.Join("|", parts ?? Array.Empty<string>());
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            unchecked
            {
                uint hash = 2166136261;
                foreach (byte b in bytes)
                {
                    hash ^= b;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }
    }
}
