using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FlightCareer.Domain.Economy
{
    public sealed record MissionPayFamily(
        string Id,
        string[] Prefixes,
        double BaseFee,
        double RatePerMile,
        double RatePerPaxMile,
        bool PaxAtLeastOne,
        double RatePerPayloadLb,
        double Scale);

    public sealed class MissionPayTable
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public IReadOnlyList<MissionPayFamily> Families { get; }

        public MissionPayTable(IEnumerable<MissionPayFamily> families)
        {
            Families = families?.ToList() ?? new List<MissionPayFamily>();
        }

        private static readonly Lazy<MissionPayTable> _defaults = new(() => new MissionPayTable(new List<MissionPayFamily>
        {
            new("instrucao_basica", new[] { "3.4.1" }, 220.0, 6.5, 0.0, false, 0.10, 1.0),
            new("instrucao", new[] { "3.4" }, 350.0, 6.5, 0.0, false, 0.10, 1.0),
            new("medevac", new[] { "4.3.1", "3.3" }, 2400.0, 14.0, 0.0, false, 0.25, 1.0),
            new("aog", new[] { "4.3.2", "3.2.1" }, 950.0, 8.0, 0.0, false, 0.35, 1.0),
            new("agro_bush", new[] { "4." }, 380.0, 4.5, 0.0, false, 0.18, 1.0),
            new("agro_municipal", new[] { "3.2" }, 450.0, 5.0, 0.0, false, 0.15, 1.0),
            new("taxi_aereo", new[] { "3.1", "2.2", "1.3" }, 950.0, 7.5, 1.5, true, 0.12, 1.0),
            new("carga_expressa", new[] { "2.3" }, 500.0, 4.0, 0.0, false, 0.22, 1.0),
            new("regional", new[] { "2.1" }, 1800.0, 6.0, 0.45, false, 0.08, 1.0),
            new("linha", new[] { "1.1", "1.2" }, 5500.0, 8.0, 0.30, false, 0.05, 1.0),
            new("fallback", Array.Empty<string>(), 400.0, 5.5, 0.0, false, 0.10, 1.0)
        }));

        public static MissionPayTable Defaults => _defaults.Value;

        public static MissionPayTable LoadFromJson(string json)
        {
            var dto = JsonSerializer.Deserialize<MissionPayTableDto>(json, JsonOpts);
            if (dto?.Families == null || dto.Families.Count == 0)
            {
                throw new InvalidDataException("JSON de tabela de pagamento sem famílias válidas.");
            }

            var families = dto.Families.Select(f => new MissionPayFamily(
                f.Id ?? string.Empty,
                f.Prefixes ?? Array.Empty<string>(),
                f.BaseFee,
                f.RatePerMile,
                f.RatePerPaxMile,
                f.PaxAtLeastOne,
                f.RatePerPayloadLb,
                f.Scale <= 0 ? 1.0 : f.Scale
            )).ToList();

            return new MissionPayTable(families);
        }

        private static readonly Lazy<MissionPayTable> _shared = new(() => LoadDefault());

        /// <summary>Tabela padrão lida uma única vez (o gerador calcula milhares de missões).</summary>
        public static MissionPayTable Shared => _shared.Value;

        public static MissionPayTable LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "mission_pay.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "mission_pay.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "mission_pay.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "mission_pay.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "mission_pay.json") }.Concat(candidates).ToArray();
            }

            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null)
            {
                Debug.WriteLine($"[MissionPayTable] Arquivo 'mission_pay.json' não encontrado. Usando padrões embutidos.");
                return Defaults;
            }

            try
            {
                string json = File.ReadAllText(path);
                return LoadFromJson(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MissionPayTable] Erro ao carregar '{path}': {ex.Message}. Usando padrões embutidos.");
                return Defaults;
            }
        }

        public MissionPayFamily Resolve(string? subId)
        {
            if (!string.IsNullOrWhiteSpace(subId))
            {
                foreach (var family in Families)
                {
                    if (family.Id == "fallback" || family.Prefixes == null || family.Prefixes.Length == 0)
                        continue;

                    foreach (var prefix in family.Prefixes)
                    {
                        if (string.IsNullOrWhiteSpace(prefix)) continue;

                        bool isExactId = prefix.Count(c => c == '.') >= 2 && !prefix.EndsWith('.');
                        if (isExactId)
                        {
                            if (string.Equals(subId, prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                return family;
                            }
                        }
                        else
                        {
                            if (subId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                return family;
                            }
                        }
                    }
                }
            }

            return Families.FirstOrDefault(f => f.Id == "fallback")
                ?? Defaults.Families.First(f => f.Id == "fallback");
        }

        private class MissionPayTableDto
        {
            public int Version { get; set; }
            public List<MissionPayFamilyDto>? Families { get; set; }
        }

        private class MissionPayFamilyDto
        {
            public string? Id { get; set; }
            public string[]? Prefixes { get; set; }
            public double BaseFee { get; set; }
            public double RatePerMile { get; set; }
            public double RatePerPaxMile { get; set; }
            public bool PaxAtLeastOne { get; set; }
            public double RatePerPayloadLb { get; set; }
            public double Scale { get; set; } = 1.0;
        }
    }
}
