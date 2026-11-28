using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FlightCareer.Domain.Xp
{
    public sealed record XpBaseRules(
        double AirborneRate = 2.0,
        double DistanceRate = 0.5,
        double LegBonus = 50.0);

    public sealed record XpRealismRules(
        double BaseValue = 1.0,
        double NoTimeAccelerationBonus = 0.05,
        double CrashDetectionBonus = 0.05,
        double AutoCoordinationOffBonus = 0.05,
        double Realism100Bonus = 0.05,
        double AutoStartPenalty = -0.10,
        double Min = 0.8,
        double Max = 1.2);

    public sealed record XpQualityRules(
        double BaseValue = 1.0,
        double ButterFpmMax = 120.0,
        double ButterGMax = 1.15,
        double ButterBonus = 0.15,
        double FirmFpmMax = 250.0,
        double FirmGMax = 1.40,
        double FirmBonus = 0.05,
        double HardFpmMax = 450.0,
        double HardPenalty = -0.10,
        double SevereFpmThreshold = 450.0,
        double SevereGThreshold = 2.00,
        double SeverePenalty = -0.30,
        double VneOrStallPenalty = 0.15,
        double GExcessPaxPenalty = 0.15,
        double LightsOrSpeedPenalty = 0.05,
        double OffRunwayPenalty = 0.40,
        double Min = 0.5,
        double Max = 1.15);

    public sealed record XpBonusRules(
        double ShortRunwayBonus = 40.0,
        double ShortRunwayMinRatio = 1.0,
        double ShortRunwayMaxRatio = 1.25,
        double UnpavedBonus = 30.0,
        double BushComboBonus = 30.0,
        double BushComboMinElevationFt = 4000.0,
        double CrosswindBonus = 35.0,
        double CrosswindThresholdSingleOrTurbopropKt = 12.0,
        double CrosswindThresholdJetKt = 20.0,
        double GustSpreadBonus = 20.0,
        double GustSpreadThresholdKt = 8.0,
        double HighElevationOrDensityAltBonus = 25.0,
        double HighElevationThresholdFt = 4000.0,
        double HighDensityAltitudeThresholdFt = 6000.0,
        double NightBonus = 30.0,
        double ImcBonus = 50.0,
        double ImcMaxVisibilityMeters = 4828.0,
        double OnTimeBonusNormal = 30.0,
        double OnTimeBonusUrgent = 60.0,
        double OnTimeRatioMax = 0.90,
        double AcceptableTimeBonus = 15.0,
        double AcceptableTimeRatioMax = 1.15,
        double EteFixedBufferMinutes = 15.0);

    public sealed class XpRules
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public XpBaseRules Base { get; init; } = new();
        public Dictionary<string, double> TypeMultipliers { get; init; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ferryOrNoMission"] = 0.80,
            ["fallback"] = 1.00,
            ["carga_expressa"] = 1.00,
            ["instrucao_basica"] = 1.10,
            ["instrucao"] = 1.10,
            ["aerofoto"] = 1.15,
            ["paraquedismo"] = 1.15,
            ["agro_municipal"] = 1.15,
            ["agro_bush"] = 1.15,
            ["taxi_aereo"] = 1.25,
            ["regional"] = 1.25,
            ["linha"] = 1.25,
            ["aog"] = 1.35,
            ["carga_fragil"] = 1.35,
            ["carga_global"] = 1.35,
            ["cargueiro_noturno"] = 1.00,
            ["medevac"] = 1.50
        };
        public XpRealismRules Realism { get; init; } = new();
        public XpQualityRules Quality { get; init; } = new();
        public XpBonusRules Bonuses { get; init; } = new();

        private static readonly Lazy<XpRules> _default = new(() => LoadDefault());
        public static XpRules Default => _default.Value;

        public static XpRules LoadFromJson(string json)
        {
            var doc = JsonSerializer.Deserialize<XpRulesDto>(json, JsonOpts);
            if (doc == null) return new XpRules();

            return new XpRules
            {
                Base = doc.Base ?? new XpBaseRules(),
                TypeMultipliers = doc.TypeMultipliers != null
                    ? new Dictionary<string, double>(doc.TypeMultipliers, StringComparer.OrdinalIgnoreCase)
                    : new XpRules().TypeMultipliers,
                Realism = doc.Realism ?? new XpRealismRules(),
                Quality = doc.Quality ?? new XpQualityRules(),
                Bonuses = doc.Bonuses ?? new XpBonusRules()
            };
        }

        public static XpRules LoadDefault(string? directory = null)
        {
            string[] candidates = directory != null
                ? new[] { Path.Combine(directory, "xp_rules.json") }
                : new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "Data", "xp_rules.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "xp_rules.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "xp_rules.json"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "xp_rules.json")
                };

            foreach (var path in candidates)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        return LoadFromJson(File.ReadAllText(path));
                    }
                }
                catch
                {
                    // Fallback para padrões embutidos se houver erro de leitura
                }
            }

            return new XpRules();
        }

        private class XpRulesDto
        {
            public XpBaseRules? Base { get; set; }
            public Dictionary<string, double>? TypeMultipliers { get; set; }
            public XpRealismRules? Realism { get; set; }
            public XpQualityRules? Quality { get; set; }
            public XpBonusRules? Bonuses { get; set; }
        }
    }
}
