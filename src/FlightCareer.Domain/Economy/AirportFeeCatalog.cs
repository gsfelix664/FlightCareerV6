using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightCareer.Domain.Economy
{
    public class AirportFeeCatalog
    {
        private static AirportFeeCatalog? _default;
        public static AirportFeeCatalog Default => _default ??= LoadDefault();

        public int SchemaVersion { get; set; } = 1;
        public string Currency { get; set; } = "$";
        public string Note { get; set; } = string.Empty;

        public Dictionary<int, LandingTierConfig> LandingTiers { get; } = new();
        public Dictionary<int, double> PassengerFeeTiers { get; } = new();
        public Dictionary<int, HandlingTierConfig> HandlingTiers { get; } = new();
        public NavTerminalConfig NavTerminal { get; private set; } = new();
        public ParkingConfig Parking { get; private set; } = new();

        public AircraftSizeClass Classify(double mtowLbs) => AircraftSizeClassifier.Classify(mtowLbs);

        public static AirportFeeCatalog LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("O JSON do catálogo de taxas de aeroporto não pode ser vazio.", nameof(json));
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            AirportFeeCatalogDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<AirportFeeCatalogDto>(json, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Erro ao deserializar 'airport_fees.json': {ex.Message}", ex);
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Falha na deserialização do catálogo de taxas de aeroporto: objeto nulo retornado.");
            }

            if (dto.Landing?.Tiers == null || dto.PassengerFee?.Tiers == null ||
                dto.Handling?.Tiers == null || dto.NavTerminal == null || dto.Parking?.Tiers == null)
            {
                throw new InvalidOperationException("Estrutura inválida em 'airport_fees.json': seções obrigatórias ausentes.");
            }

            var catalog = new AirportFeeCatalog
            {
                SchemaVersion = dto.SchemaVersion,
                Currency = dto.Currency ?? "$",
                Note = dto.Note ?? string.Empty
            };

            foreach (var kvp in dto.Landing.Tiers)
            {
                if (int.TryParse(kvp.Key, out int tier))
                {
                    catalog.LandingTiers[tier] = new LandingTierConfig(kvp.Value.BaseFee, kvp.Value.PerLbFee);
                }
            }

            foreach (var kvp in dto.PassengerFee.Tiers)
            {
                if (int.TryParse(kvp.Key, out int tier))
                {
                    catalog.PassengerFeeTiers[tier] = kvp.Value;
                }
            }

            foreach (var kvp in dto.Handling.Tiers)
            {
                if (int.TryParse(kvp.Key, out int tier))
                {
                    catalog.HandlingTiers[tier] = new HandlingTierConfig(kvp.Value.Medium, kvp.Value.Heavy);
                }
            }

            catalog.NavTerminal = new NavTerminalConfig(
                dto.NavTerminal.FeePerThousandLbs,
                dto.NavTerminal.ApplicableTiers ?? new List<int> { 1, 2, 3 },
                dto.NavTerminal.ApplicableSizeClasses ?? new List<string> { "medium", "heavy" }
            );

            var parkingTiers = new Dictionary<int, ParkingTierRates>();
            foreach (var kvp in dto.Parking.Tiers)
            {
                if (int.TryParse(kvp.Key, out int tier))
                {
                    parkingTiers[tier] = new ParkingTierRates(kvp.Value.Light, kvp.Value.Medium, kvp.Value.Heavy);
                }
            }

            catalog.Parking = new ParkingConfig(
                dto.Parking.FreeHours,
                parkingTiers
            );

            return catalog;
        }

        public static AirportFeeCatalog LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "airport_fees.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "airport_fees.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "airport_fees.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "airport_fees.json")
            };

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates = new[] { Path.Combine(directory, "airport_fees.json") }.Concat(candidates).ToArray();
            }

            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null)
            {
                throw new FileNotFoundException($"Arquivo 'airport_fees.json' não encontrado. Buscado em: {string.Join(", ", candidates)}");
            }

            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        public record LandingTierConfig(double BaseFee, double PerLbFee);
        public record HandlingTierConfig(double Medium, double Heavy);
        public record NavTerminalConfig(double FeePerThousandLbs = 3.0, List<int>? ApplicableTiers = null, List<string>? ApplicableSizeClasses = null);
        public record ParkingTierRates(double Light, double Medium, double Heavy);
        public record ParkingConfig(double FreeHours = 2.0, Dictionary<int, ParkingTierRates>? Tiers = null);

        private class AirportFeeCatalogDto
        {
            public int SchemaVersion { get; set; } = 1;
            public string? Currency { get; set; }
            public string? Note { get; set; }
            public Dictionary<string, SizeClassDto>? SizeClasses { get; set; }
            public LandingSectionDto? Landing { get; set; }
            public PassengerFeeSectionDto? PassengerFee { get; set; }
            public HandlingSectionDto? Handling { get; set; }
            public NavTerminalDto? NavTerminal { get; set; }
            public ParkingSectionDto? Parking { get; set; }
            public Dictionary<string, object>? CountryOverrides { get; set; }
            public Dictionary<string, object>? IcaoOverrides { get; set; }
        }

        private class SizeClassDto
        {
            public double? MaxMtowLbs { get; set; }
            public string? Description { get; set; }
        }

        private class LandingSectionDto
        {
            public Dictionary<string, LandingRateDto>? Tiers { get; set; }
        }

        private class LandingRateDto
        {
            public double BaseFee { get; set; }
            public double PerLbFee { get; set; }
        }

        private class PassengerFeeSectionDto
        {
            public Dictionary<string, double>? Tiers { get; set; }
        }

        private class HandlingSectionDto
        {
            public Dictionary<string, HandlingRateDto>? Tiers { get; set; }
        }

        private class HandlingRateDto
        {
            public double Medium { get; set; }
            public double Heavy { get; set; }
        }

        private class NavTerminalDto
        {
            public double FeePerThousandLbs { get; set; }
            public List<int>? ApplicableTiers { get; set; }
            public List<string>? ApplicableSizeClasses { get; set; }
            public string? Comment { get; set; }
        }

        private class ParkingSectionDto
        {
            public double FreeHours { get; set; }
            public Dictionary<string, ParkingRateDto>? Tiers { get; set; }
        }

        private class ParkingRateDto
        {
            public double Light { get; set; }
            public double Medium { get; set; }
            public double Heavy { get; set; }
        }
    }
}
