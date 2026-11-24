using System;
using System.IO;
using System.Text.Json;

namespace FlightCareer.Domain.Airports
{
    public sealed class TagTierScores
    {
        public int MegaHub { get; set; } = 100;
        public int HubLinha { get; set; } = 70;
        public int CargaPesada { get; set; } = 60;
        public int Linha { get; set; } = 50;
        public int Alimentador { get; set; } = 35;
        public int ExecutivoSaudeMroCarga { get; set; } = 25;
        public int InstrucaoAgroOutros { get; set; } = 10;
    }

    public sealed class StructureTierScores
    {
        public int IlsPerEnd { get; set; } = 8;
        public int MaxIls { get; set; } = 32;
        public int ApproachEach { get; set; } = 1;
        public int MaxApproaches { get; set; } = 20;
        public int GateJetwayEach { get; set; } = 1;
        public int MaxGateJetways { get; set; } = 40;
        public int CargoParkingEach { get; set; } = 2;
        public int MaxCargoParking { get; set; } = 20;
        public int Tower { get; set; } = 8;
        public int RunwayPavedTier1MinFt { get; set; } = 8000;
        public int RunwayPavedTier1Points { get; set; } = 15;
        public int RunwayPavedTier2MinFt { get; set; } = 5000;
        public int RunwayPavedTier2Points { get; set; } = 8;
        public int RunwayPavedAnyPoints { get; set; } = 3;
    }

    public sealed class OurAirportsTierScores
    {
        public int LargeAirport { get; set; } = 30;
        public int MediumAirport { get; set; } = 15;
        public int ScheduledService { get; set; } = 15;
    }

    public sealed class TierThresholdScores
    {
        public int Tier1 { get; set; } = 90;
        public int Tier2 { get; set; } = 45;
        public int Tier3 { get; set; } = 15;
    }

    public sealed class AirportTierRules
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private static readonly Lazy<AirportTierRules> s_default = new(() => LoadDefault());
        public static AirportTierRules Default => s_default.Value;

        public TagTierScores Tags { get; set; } = new();
        public StructureTierScores Structure { get; set; } = new();
        public OurAirportsTierScores OurAirports { get; set; } = new();
        public TierThresholdScores Thresholds { get; set; } = new();

        public static AirportTierRules CreateDefault() => new();

        public static AirportTierRules LoadFromJson(string json)
        {
            return JsonSerializer.Deserialize<AirportTierRules>(json, JsonOpts) ?? CreateDefault();
        }

        public static AirportTierRules LoadDefault(string? directory = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "airport_tier_rules.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "airport_tier_rules.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "airport_tier_rules.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "airport_tier_rules.json")
            };

            if (!string.IsNullOrEmpty(directory))
            {
                var custom = Path.Combine(directory, "airport_tier_rules.json");
                if (File.Exists(custom))
                {
                    try { return LoadFromJson(File.ReadAllText(custom)); } catch { }
                }
            }

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try { return LoadFromJson(File.ReadAllText(path)); } catch { }
                }
            }

            return CreateDefault();
        }
    }
}
