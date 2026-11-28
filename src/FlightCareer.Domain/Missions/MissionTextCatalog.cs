using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlightCareer.Domain.Missions
{
    public sealed record MissionTextVariant(string Title, string Description);

    public class MissionTextCatalog
    {
        private static readonly Lazy<MissionTextCatalog> s_default = new(() => LoadDefault());
        public static MissionTextCatalog Default => s_default.Value;

        private readonly Dictionary<string, List<MissionTextVariant>> _templates;

        public IReadOnlyDictionary<string, List<MissionTextVariant>> Templates => _templates;
        public int SubtypesCount => _templates.Count;

        public MissionTextCatalog(Dictionary<string, List<MissionTextVariant>> templates)
        {
            _templates = new Dictionary<string, List<MissionTextVariant>>(templates, StringComparer.OrdinalIgnoreCase);
        }

        public static MissionTextCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "mission_texts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "mission_texts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "mission_texts.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "FlightCareer.Infrastructure", "Data", "mission_texts.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "mission_texts.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "mission_texts.json") }.Concat(candidates).ToArray();
            }

            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null)
            {
                System.Diagnostics.Debug.WriteLine($"[MissionTextCatalog] Arquivo 'mission_texts.json' não encontrado. Buscado em: {string.Join(", ", candidates)}");
                return new MissionTextCatalog(new Dictionary<string, List<MissionTextVariant>>());
            }

            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        public static MissionTextCatalog LoadFromJson(string json)
        {
            var result = new Dictionary<string, List<MissionTextVariant>>(StringComparer.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var list = new List<MissionTextVariant>();
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        string title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                        string desc = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                        list.Add(new MissionTextVariant(title, desc));
                    }
                }
                result[prop.Name] = list;
            }
            return new MissionTextCatalog(result);
        }

        public (string Title, string Description) Render(
            string subId,
            Random? random,
            IReadOnlyDictionary<string, string>? values,
            string? fallbackTitle = null,
            string? fallbackDescription = null)
        {
            if (!_templates.TryGetValue(subId, out var variants) || variants == null || variants.Count == 0)
            {
                string normalized = MissionCatalog.Shared.Normalize(subId);
                if (normalized != subId && _templates.TryGetValue(normalized, out variants) && variants != null && variants.Count > 0)
                {
                    // Encontrado via id normalizado
                }
                else
                {
                    return (fallbackTitle ?? string.Empty, fallbackDescription ?? string.Empty);
                }
            }

            var rng = random ?? new Random();
            int idx = rng.Next(variants.Count);
            var variant = variants[idx];

            string title = ReplacePlaceholders(variant.Title, values);
            string desc = ReplacePlaceholders(variant.Description, values);

            if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(fallbackTitle))
                title = fallbackTitle;
            if (string.IsNullOrWhiteSpace(desc) && !string.IsNullOrWhiteSpace(fallbackDescription))
                desc = fallbackDescription;

            return (title, desc);
        }

        private static string ReplacePlaceholders(string template, IReadOnlyDictionary<string, string>? values)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            var pattern = new Regex(@"\{([a-zA-Z0-9_]+)\}");
            string result = pattern.Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                if (values != null && values.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    return val.Trim();
                }
                return string.Empty;
            });

            // Remove qualquer chave residual que não foi capturada pelo padrão
            result = Regex.Replace(result, @"\{.*?\}", "");

            // Limpa espaços duplicados e pontuações isoladas
            result = Regex.Replace(result, @"\s{2,}", " ");
            result = result.Replace(" ()", "").Replace("()", "");
            result = result.Replace(" ,", ",").Replace(" .", ".");
            return result.Trim();
        }
    }
}
