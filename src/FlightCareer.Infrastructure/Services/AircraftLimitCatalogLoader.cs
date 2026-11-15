using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;

namespace FlightCareer.Infrastructure.Services
{
    /// <summary>
    /// Carrega e valida os arquivos do catálogo de limites de aeronaves (Data/aircraft/*.json).
    /// </summary>
    public class AircraftLimitCatalogLoader
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly string? _customDirectory;

        public AircraftLimitCatalogLoader(string? customDirectory = null)
        {
            _customDirectory = customDirectory;
        }

        public string ResolveAircraftDataDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_customDirectory) && Directory.Exists(_customDirectory))
            {
                return _customDirectory;
            }

            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "aircraft"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "aircraft"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "FlightCareer.Infrastructure", "Data", "aircraft"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "aircraft")
            };

            var dir = candidates.FirstOrDefault(Directory.Exists);
            if (dir != null)
            {
                return Path.GetFullPath(dir);
            }

            throw new DirectoryNotFoundException($"Diretório 'Data/aircraft' não encontrado. Buscado em: {string.Join(", ", candidates)}");
        }

        public IReadOnlyList<AircraftLimitProfile> LoadProfiles()
        {
            var dir = ResolveAircraftDataDirectory();
            var files = Directory.GetFiles(dir, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("_", StringComparison.Ordinal))
                .OrderBy(f => f)
                .ToList();

            var profiles = new List<AircraftLimitProfile>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var profile = LoadProfileFromFile(file);

                if (!ids.Add(profile.AircraftId))
                {
                    throw new InvalidDataException($"Id duplicado no catálogo de limites: '{profile.AircraftId}' no arquivo {file}.");
                }

                profiles.Add(profile);
            }

            // Validar que nenhum padrão de simMatch (title ou atcModel) se repete entre aeronaves diferentes
            ValidateNoSimMatchCollisions(profiles);

            return profiles;
        }

        public AircraftLimitProfile LoadProfileFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<AircraftLimitFileDto>(json, JsonOpts)
                ?? throw new InvalidDataException($"Arquivo vazio ou inválido: {filePath}");

            ValidateFileDto(dto, filePath);

            var limits = new List<AircraftLimit>();
            if (dto.Limits != null)
            {
                foreach (var l in dto.Limits)
                {
                    if (!Enum.TryParse<LimitSource>(l.Source, true, out var sourceEnum))
                    {
                        throw new InvalidDataException($"Origem '{l.Source}' inválida para o limite '{l.Key}' no arquivo {filePath}.");
                    }

                    limits.Add(new AircraftLimit(
                        l.Key,
                        l.Value,
                        l.Unit,
                        sourceEnum,
                        l.Reference,
                        l.Notes
                    ));
                }
            }

            int engineCount = dto.EngineCount ?? (dto.Features != null && dto.Features.Contains(AircraftFeatures.MultiEngine) ? 2 : 1);
            if (engineCount <= 0) engineCount = 1;

            return new AircraftLimitProfile(
                dto.Id,
                dto.Class,
                dto.DataVersion,
                dto.SimMatch?.Title ?? new List<string>(),
                dto.SimMatch?.AtcModel ?? new List<string>(),
                limits,
                dto.Features ?? new List<string>(),
                engineCount
            );
        }

        public IReadOnlyDictionary<string, IReadOnlyList<AircraftLimit>> LoadClassDefaults()
        {
            var dir = ResolveAircraftDataDirectory();
            var defaultsFile = Path.Combine(dir, "_class_defaults.json");

            if (!File.Exists(defaultsFile))
            {
                throw new FileNotFoundException($"Arquivo de padrões de classe não encontrado: {defaultsFile}");
            }

            string json = File.ReadAllText(defaultsFile);
            var dictDto = JsonSerializer.Deserialize<Dictionary<string, List<LimitItemDto>>>(json, JsonOpts)
                ?? throw new InvalidDataException($"Arquivo '_class_defaults.json' vazio ou inválido.");

            var result = new Dictionary<string, IReadOnlyList<AircraftLimit>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (cls, list) in dictDto)
            {
                var limits = new List<AircraftLimit>();
                foreach (var item in list)
                {
                    if (!LimitKeys.IsValid(item.Key))
                    {
                        throw new InvalidDataException($"Chave '{item.Key}' inválida em _class_defaults.json para classe {cls}.");
                    }

                    string expectedUnit = LimitKeys.UnitOf(item.Key);
                    if (!string.Equals(item.Unit, expectedUnit, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"Unidade '{item.Unit}' incompatível com chave '{item.Key}' (esperada: '{expectedUnit}') em _class_defaults.json.");
                    }

                    if (!Enum.TryParse<LimitSource>(item.Source, true, out var sourceEnum))
                    {
                        sourceEnum = LimitSource.ClassDefault;
                    }

                    limits.Add(new AircraftLimit(item.Key, item.Value, item.Unit, sourceEnum, item.Reference, item.Notes));
                }
                result[cls] = limits;
            }

            return result;
        }

        public AircraftLimitResolver CreateResolver()
        {
            return new AircraftLimitResolver(LoadProfiles(), LoadClassDefaults());
        }

        private static void ValidateFileDto(AircraftLimitFileDto dto, string filePath)
        {
            if (dto.SchemaVersion < 1)
            {
                throw new InvalidDataException($"schemaVersion inválido ({dto.SchemaVersion}) no arquivo {filePath}.");
            }

            if (dto.DataVersion < 1)
            {
                throw new InvalidDataException($"dataVersion inválido ({dto.DataVersion}) no arquivo {filePath}.");
            }

            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                throw new InvalidDataException($"Id vazio no arquivo {filePath}.");
            }

            if (string.IsNullOrWhiteSpace(dto.Class))
            {
                throw new InvalidDataException($"Class vazia no arquivo {filePath}.");
            }

            if (dto.Features == null || dto.Features.Count == 0)
            {
                throw new InvalidDataException($"Features vazias ou não especificadas no arquivo {filePath}.");
            }

            AircraftFeatures.ValidateCoherence(dto.Features, filePath);

            if (dto.EngineCount.HasValue)
            {
                if (dto.EngineCount.Value <= 0)
                {
                    throw new InvalidDataException($"'engineCount' deve ser maior que zero no arquivo {filePath}.");
                }
                if (dto.Features != null)
                {
                    if (dto.Features.Contains(AircraftFeatures.SingleEngine) && dto.EngineCount.Value != 1)
                    {
                        throw new InvalidDataException($"Aeronave com feature 'singleEngine' deve ter engineCount == 1 no arquivo {filePath} (encontrado {dto.EngineCount.Value}).");
                    }
                    if (dto.Features.Contains(AircraftFeatures.MultiEngine) && dto.EngineCount.Value < 2)
                    {
                        throw new InvalidDataException($"Aeronave com feature 'multiEngine' deve ter engineCount >= 2 no arquivo {filePath} (encontrado {dto.EngineCount.Value}).");
                    }
                }
            }

            if (dto.Limits != null)
            {
                foreach (var l in dto.Limits)
                {
                    if (string.IsNullOrWhiteSpace(l.Key) || !LimitKeys.IsValid(l.Key))
                    {
                        throw new InvalidDataException($"Chave de limite '{l.Key}' inválida no arquivo {filePath}.");
                    }

                    string expectedUnit = LimitKeys.UnitOf(l.Key);
                    if (!string.Equals(l.Unit, expectedUnit, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"Unidade '{l.Unit}' inválida para chave '{l.Key}' (esperado '{expectedUnit}') no arquivo {filePath}.");
                    }

                    if (!Enum.TryParse<LimitSource>(l.Source, true, out var sourceEnum))
                    {
                        throw new InvalidDataException($"Origem '{l.Source}' inválida para chave '{l.Key}' no arquivo {filePath}.");
                    }

                    if (sourceEnum == LimitSource.POH && string.IsNullOrWhiteSpace(l.Reference))
                    {
                        throw new InvalidDataException($"Limite POH '{l.Key}' sem referência obrigatória no arquivo {filePath}.");
                    }
                }
            }
        }

        private static void ValidateNoSimMatchCollisions(IReadOnlyList<AircraftLimitProfile> profiles)
        {
            var titleToProfile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var atcToProfile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var profile in profiles)
            {
                foreach (var title in profile.TitlePatterns)
                {
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    if (titleToProfile.TryGetValue(title, out var existingId))
                    {
                        throw new InvalidDataException($"Padrão de título simMatch '{title}' repetido entre '{existingId}' e '{profile.AircraftId}'.");
                    }
                    titleToProfile[title] = profile.AircraftId;
                }

                foreach (var atc in profile.AtcModelPatterns)
                {
                    if (string.IsNullOrWhiteSpace(atc)) continue;
                    if (atcToProfile.TryGetValue(atc, out var existingId))
                    {
                        throw new InvalidDataException($"Padrão de ATC model simMatch '{atc}' repetido entre '{existingId}' e '{profile.AircraftId}'.");
                    }
                    atcToProfile[atc] = profile.AircraftId;
                }
            }
        }

        private class AircraftLimitFileDto
        {
            public int SchemaVersion { get; set; }
            public int DataVersion { get; set; }
            public string Id { get; set; } = string.Empty;
            public string Class { get; set; } = string.Empty;
            public int? EngineCount { get; set; }
            public List<string>? Features { get; set; }
            public SimMatchDto? SimMatch { get; set; }
            public List<LimitItemDto>? Limits { get; set; }
        }

        private class SimMatchDto
        {
            public List<string>? Title { get; set; }
            public List<string>? AtcModel { get; set; }
        }

        private class LimitItemDto
        {
            public string Key { get; set; } = string.Empty;
            public double Value { get; set; }
            public string Unit { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public string? Notes { get; set; }
        }
    }
}
