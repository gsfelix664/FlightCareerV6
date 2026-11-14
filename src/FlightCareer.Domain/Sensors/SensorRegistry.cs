using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlightCareer.Domain.Sensors
{
    /// <summary>
    /// Registro de sensores / SimVars carregado de arquivo JSON.
    /// Permite validar integridade e consultar definições de telemetria e contexto.
    /// </summary>
    public sealed class SensorRegistry
    {
        private static readonly Regex KebabCaseRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public List<SensorDefinition> Sensors { get; set; } = new();

        /// <summary>
        /// Valida as regras de negócio do registro de sensores:
        /// - Chaves únicas
        /// - Chaves no formato kebab-case (^[a-z0-9]+(-[a-z0-9]+)*$)
        /// - SimVar e Unit não vazias
        /// - SimVar sem sufixo ou delimitador de índice (':')
        /// </summary>
        public void Validate()
        {
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var sensor in Sensors)
            {
                if (string.IsNullOrWhiteSpace(sensor.Key))
                    throw new InvalidDataException("Chave do sensor não pode ser vazia.");

                if (!KebabCaseRegex.IsMatch(sensor.Key))
                    throw new InvalidDataException($"Chave '{sensor.Key}' está fora do padrão kebab-case.");

                if (!seenKeys.Add(sensor.Key))
                    throw new InvalidDataException($"Chave duplicada encontrada no registro: '{sensor.Key}'.");

                if (string.IsNullOrWhiteSpace(sensor.SimVar))
                    throw new InvalidDataException($"SimVar da chave '{sensor.Key}' não pode ser vazia.");

                if (sensor.SimVar.Contains(':'))
                    throw new InvalidDataException($"SimVar '{sensor.SimVar}' não pode conter ':' (use PerEngine = true em vez de sufixos de índice).");

                if (string.IsNullOrWhiteSpace(sensor.Unit))
                    throw new InvalidDataException($"Unidade da chave '{sensor.Key}' não pode ser vazia.");

                if (string.IsNullOrWhiteSpace(sensor.Group))
                    throw new InvalidDataException($"Grupo da chave '{sensor.Key}' não pode ser vazio.");
            }
        }

        public static SensorRegistry FromJson(string json)
        {
            var registry = JsonSerializer.Deserialize<SensorRegistry>(json, JsonOptions)
                ?? throw new InvalidDataException("Registro de sensores vazio ou inválido.");
            registry.Validate();
            return registry;
        }

        public static SensorRegistry LoadFromFile(string path) => FromJson(File.ReadAllText(path));

        /// <summary>
        /// Procura o arquivo JSON informado a partir dos diretórios candidatos.
        /// </summary>
        public static SensorRegistry LoadDefault(string fileName = "sensor_registry.json")
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "FlightCareer.Infrastructure", "Data", fileName)
            };

            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException($"Arquivo '{fileName}' não encontrado nas pastas Data candidatas.");

            return LoadFromFile(path);
        }
    }
}
