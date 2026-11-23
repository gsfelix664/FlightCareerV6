using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class GoldenMissionRecord
    {
        public int Seed { get; set; }
        public string OriginIcao { get; set; } = string.Empty;
        public string Subtype { get; set; } = string.Empty;
        public string DestinationIcao { get; set; } = string.Empty;
        public int Pax { get; set; }
        public double CargoLbs { get; set; }
        public double Reward { get; set; }
        public double DistanceNm { get; set; }
        public string Title { get; set; } = string.Empty;
        public int RequiredTouchAndGoes { get; set; }
        public string? OverflyWaypoints { get; set; }
        public string? ContractData { get; set; }
    }

    public class MissionCatalogEquivalenceTests
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        private static string GetGoldenFilePath()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Missions", "Golden", "missions_golden.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Missions", "Golden", "missions_golden.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Tests", "Missions", "Golden", "missions_golden.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Missions", "Golden", "missions_golden.json")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return Path.GetFullPath(path);
            }

            // Fallback para gravação
            var devPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Missions", "Golden", "missions_golden.json");
            var dir = Path.GetDirectoryName(devPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.GetFullPath(devPath);
        }

        private static (List<Airport> Origins, List<Airport> AllAirports, List<Runway> Runways) BuildTestAviationData()
        {
            var allAirports = new List<Airport>
            {
                // TIER 1
                new() { ICAO = "SBGR", Name = "Guarulhos Int'l", Country = "Brazil", Tier = 1, Latitude = -23.4356, Longitude = -46.4731, ElevationFt = 2461 },
                new() { ICAO = "SBGL", Name = "Galeão Int'l", Country = "Brazil", Tier = 1, Latitude = -22.8089, Longitude = -43.2436, ElevationFt = 28 },
                new() { ICAO = "SBCF", Name = "Confins Int'l", Country = "Brazil", Tier = 1, Latitude = -19.6244, Longitude = -43.9719, ElevationFt = 2715 },
                new() { ICAO = "SBBR", Name = "Brasília Int'l", Country = "Brazil", Tier = 1, Latitude = -15.8692, Longitude = -47.9172, ElevationFt = 3497 },
                new() { ICAO = "SBPA", Name = "Salgado Filho", Country = "Brazil", Tier = 1, Latitude = -29.9939, Longitude = -51.1711, ElevationFt = 11 },
                new() { ICAO = "SBBE", Name = "Val de Cans", Country = "Brazil", Tier = 1, Latitude = -1.3847, Longitude = -48.4789, ElevationFt = 54 },
                new() { ICAO = "SBEG", Name = "Eduardo Gomes", Country = "Brazil", Tier = 1, Latitude = -3.0386, Longitude = -60.0497, ElevationFt = 264 },
                new() { ICAO = "KJFK", Name = "John F Kennedy", Country = "United States", Tier = 1, Latitude = 40.6398, Longitude = -73.7789, ElevationFt = 13 },
                new() { ICAO = "KMIA", Name = "Miami Int'l", Country = "United States", Tier = 1, Latitude = 25.7959, Longitude = -80.2870, ElevationFt = 8 },
                new() { ICAO = "LFPG", Name = "Charles de Gaulle", Country = "France", Tier = 1, Latitude = 49.0097, Longitude = 2.5479, ElevationFt = 392 },
                new() { ICAO = "EGLL", Name = "Heathrow", Country = "United Kingdom", Tier = 1, Latitude = 51.4706, Longitude = -0.4619, ElevationFt = 83 },
                new() { ICAO = "SAEZ", Name = "Ezeiza Int'l", Country = "Argentina", Tier = 1, Latitude = -34.8222, Longitude = -58.5358, ElevationFt = 67 },
                new() { ICAO = "SCEL", Name = "Santiago Int'l", Country = "Chile", Tier = 1, Latitude = -33.3930, Longitude = -70.7858, ElevationFt = 1555 },

                // TIER 2
                new() { ICAO = "SBKP", Name = "Viracopos", Country = "Brazil", Tier = 2, Latitude = -23.0074, Longitude = -47.1345, ElevationFt = 2170 },
                new() { ICAO = "SBRP", Name = "Leite Lopes", Country = "Brazil", Tier = 2, Latitude = -21.1364, Longitude = -47.7744, ElevationFt = 1795 },
                new() { ICAO = "SBJD", Name = "Jundiaí", Country = "Brazil", Tier = 2, Latitude = -23.1822, Longitude = -46.9431, ElevationFt = 2484 },
                new() { ICAO = "SBJR", Name = "Jacarepaguá", Country = "Brazil", Tier = 2, Latitude = -22.9875, Longitude = -43.3703, ElevationFt = 10 },
                new() { ICAO = "SBMT", Name = "Campo de Marte", Country = "Brazil", Tier = 2, Latitude = -23.5092, Longitude = -46.6378, ElevationFt = 2369 },
                new() { ICAO = "SBRF", Name = "Guararapes", Country = "Brazil", Tier = 2, Latitude = -8.1264, Longitude = -34.9228, ElevationFt = 33 },
                new() { ICAO = "SBSV", Name = "Dep. Luís Eduardo Magalhães", Country = "Brazil", Tier = 2, Latitude = -12.9086, Longitude = -38.3225, ElevationFt = 64 },
                new() { ICAO = "SBAR", Name = "Santa Maria", Country = "Brazil", Tier = 2, Latitude = -10.9840, Longitude = -37.0703, ElevationFt = 23 },

                // TIER 3
                new() { ICAO = "SDCO", Name = "Sorocaba", Country = "Brazil", Tier = 3, Latitude = -23.4808, Longitude = -47.4897, ElevationFt = 2077 },
                new() { ICAO = "SBDN", Name = "Pres. Prudente", Country = "Brazil", Tier = 3, Latitude = -22.1969, Longitude = -49.9261, ElevationFt = 1476 },
                new() { ICAO = "SBSJ", Name = "São José dos Campos", Country = "Brazil", Tier = 3, Latitude = -23.2289, Longitude = -45.8625, ElevationFt = 2120 },
                new() { ICAO = "SBFI", Name = "Foz do Iguaçu", Country = "Brazil", Tier = 3, Latitude = -25.5969, Longitude = -54.4878, ElevationFt = 787 },
                new() { ICAO = "SBBW", Name = "Barra do Garças", Country = "Brazil", Tier = 3, Latitude = -15.8611, Longitude = -52.3892, ElevationFt = 1148 },
                new() { ICAO = "SDVG", Name = "Major Brigadeiro Trompowsky", Country = "Brazil", Tier = 3, Latitude = -21.5878, Longitude = -45.4736, ElevationFt = 3025 },
                new() { ICAO = "SDAA", Name = "Americana", Country = "Brazil", Tier = 3, Latitude = -22.7561, Longitude = -47.2658, ElevationFt = 1886 },
                new() { ICAO = "SDMC", Name = "Marília", Country = "Brazil", Tier = 3, Latitude = -22.1967, Longitude = -49.9261, ElevationFt = 2123 },
                new() { ICAO = "SDRS", Name = "Resende", Country = "Brazil", Tier = 3, Latitude = -22.4789, Longitude = -44.4839, ElevationFt = 1319 },
                new() { ICAO = "SBNP", Name = "Ponta Porã", Country = "Brazil", Tier = 3, Latitude = -22.5497, Longitude = -55.7028, ElevationFt = 2156 },

                // TIER 4
                new() { ICAO = "SDUB", Name = "Gastão Madeira (Ubatuba)", Country = "Brazil", Tier = 4, Latitude = -23.4417, Longitude = -45.0747, ElevationFt = 10 },
                new() { ICAO = "SWCA", Name = "Fazenda Cachoeira", Country = "Brazil", Tier = 4, Latitude = -22.1000, Longitude = -47.0000, ElevationFt = 2100 },
                new() { ICAO = "SSKW", Name = "Fazenda Pouso Redondo", Country = "Brazil", Tier = 4, Latitude = -15.5000, Longitude = -55.5000, ElevationFt = 1250 },
                new() { ICAO = "SDOI", Name = "Centro Nacional de Paraquedismo (Boituva)", Country = "Brazil", Tier = 4, Latitude = -23.2750, Longitude = -47.6694, ElevationFt = 2050 },
                new() { ICAO = "SNAA", Name = "Fazenda Água Doce", Country = "Brazil", Tier = 4, Latitude = -21.8000, Longitude = -48.2000, ElevationFt = 1800 },
                new() { ICAO = "SNBB", Name = "Fazenda Bela Vista", Country = "Brazil", Tier = 4, Latitude = -20.5000, Longitude = -49.1000, ElevationFt = 1600 }
            };

            var runways = new List<Runway>
            {
                new() { AirportIdent = "SBGR", LengthFt = 12139, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBGR", LengthFt = 12139, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBGL", LengthFt = 13123, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "KJFK", LengthFt = 12079, WidthFt = 150, Surface = "Concrete" },
                new() { AirportIdent = "EGLL", LengthFt = 12802, WidthFt = 164, Surface = "Asphalt" },
                new() { AirportIdent = "LFPG", LengthFt = 13779, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SAEZ", LengthFt = 10827, WidthFt = 197, Surface = "Asphalt" },
                new() { AirportIdent = "SCEL", LengthFt = 12467, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "KMIA", LengthFt = 10502, WidthFt = 200, Surface = "Asphalt" },
                new() { AirportIdent = "SBCF", LengthFt = 9843, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBBR", LengthFt = 10827, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBPA", LengthFt = 10499, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBBE", LengthFt = 9186, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBEG", LengthFt = 8858, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBKP", LengthFt = 10630, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBRP", LengthFt = 6890, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBJD", LengthFt = 4593, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SBJR", LengthFt = 4265, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SBMT", LengthFt = 5249, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SBRF", LengthFt = 9843, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBSV", LengthFt = 9852, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBAR", LengthFt = 7218, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SDCO", LengthFt = 4856, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SBDN", LengthFt = 3937, WidthFt = 98, Surface = "Asphalt" }, // <= 4000 ft
                new() { AirportIdent = "SBSJ", LengthFt = 8779, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBFI", LengthFt = 7218, WidthFt = 148, Surface = "Asphalt" },
                new() { AirportIdent = "SBBW", LengthFt = 5249, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SDVG", LengthFt = 4921, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SDAA", LengthFt = 3937, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SDMC", LengthFt = 5577, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SDRS", LengthFt = 3937, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SBNP", LengthFt = 6562, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SDUB", LengthFt = 3117, WidthFt = 98, Surface = "Asphalt" },
                new() { AirportIdent = "SWCA", LengthFt = 2625, WidthFt = 60, Surface = "Turf" }, // Não pavimentada
                new() { AirportIdent = "SSKW", LengthFt = 1969, WidthFt = 50, Surface = "Dirt" }, // Não pavimentada
                new() { AirportIdent = "SDOI", LengthFt = 2789, WidthFt = 60, Surface = "Asphalt" },
                new() { AirportIdent = "SNAA", LengthFt = 2460, WidthFt = 50, Surface = "Grass" },
                new() { AirportIdent = "SNBB", LengthFt = 2132, WidthFt = 50, Surface = "Grass" }
            };

            var origins = new List<Airport>
            {
                allAirports.First(a => a.ICAO == "SBGR"), // Tier 1 BR (Hub PAX/Cargo/Linha/Carreira)
                allAirports.First(a => a.ICAO == "SBGL"), // Tier 1 BR (Hub Long-Haul)
                allAirports.First(a => a.ICAO == "KJFK"), // Tier 1 US (Hub Global / Outro País)
                allAirports.First(a => a.ICAO == "SBKP"), // Tier 2 BR (Hub Carga / MRO)
                allAirports.First(a => a.ICAO == "SBRP"), // Tier 2 BR (Hub Agro / Regional)
                allAirports.First(a => a.ICAO == "SBJR"), // Tier 2 BR (Turístico Curado / Dropzone)
                allAirports.First(a => a.ICAO == "SBMT"), // Tier 2 BR (Turístico Curado / Instrução)
                allAirports.First(a => a.ICAO == "SDCO"), // Tier 3 BR (MRO / Executiva / Próx Boituva)
                allAirports.First(a => a.ICAO == "SBDN"), // Tier 3 BR (Saúde / Agro Suporte / Pista Curta)
                allAirports.First(a => a.ICAO == "SBSJ"), // Tier 3 BR (Aeroclube / Instrução Training)
                allAirports.First(a => a.ICAO == "SBFI"), // Tier 3 BR (Turístico Curado / Dropzone)
                allAirports.First(a => a.ICAO == "SDUB"), // Tier 4 BR (Litoral / Pista Curta)
                allAirports.First(a => a.ICAO == "SWCA"), // Tier 4 BR (Rural Não Pavimentada)
                allAirports.First(a => a.ICAO == "SSKW")  // Tier 4 BR (Bush Remoto)
            };

            return (origins, allAirports, runways);
        }

        [Fact]
        public void Gerador_SnapshotEquivalencia_GeraEMantemComportamentoIdentico()
        {
            var (origins, allAirports, runways) = BuildTestAviationData();
            var hubService = new HubDataService();
            var placesService = new PlacesDataService();
            var refTime = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

            var currentRuns = new List<GoldenMissionRecord>();
            int seedBase = 777000;

            for (int i = 0; i < origins.Count; i++)
            {
                var origin = origins[i];
                int seed = seedBase + (i * 137);
                var rng = new Random(seed);

                var missions = MissionGenerator.GenerateMissionsInMemory(
                    origin,
                    allAirports,
                    hubService,
                    rng,
                    refTime,
                    countOverride: 20,
                    payMultiplier: 1.0,
                    payTable: null,
                    allRunways: runways,
                    placesService: placesService);

                foreach (var m in missions)
                {
                    currentRuns.Add(new GoldenMissionRecord
                    {
                        Seed = seed,
                        OriginIcao = m.OriginICAO,
                        Subtype = m.SubId,
                        DestinationIcao = m.DestinationICAO,
                        Pax = m.PassengerCount,
                        CargoLbs = m.CargoWeightLbs,
                        Reward = m.RewardMoney,
                        DistanceNm = m.DistanceNM,
                        Title = m.Title,
                        RequiredTouchAndGoes = m.RequiredTouchAndGoes,
                        OverflyWaypoints = m.OverflyWaypoints,
                        ContractData = m.ContractData
                    });
                }
            }

            Assert.True(currentRuns.Count >= 200, $"Esperava >= 200 missões geradas, obteve {currentRuns.Count}");

            string goldenPath = GetGoldenFilePath();

            if (!File.Exists(goldenPath))
            {
                string json = JsonSerializer.Serialize(currentRuns, JsonOpts);
                File.WriteAllText(goldenPath, json);
                return;
            }

            string goldenJson = File.ReadAllText(goldenPath);
            var goldenRecords = JsonSerializer.Deserialize<List<GoldenMissionRecord>>(goldenJson, JsonOpts);
            Assert.NotNull(goldenRecords);
            Assert.Equal(goldenRecords!.Count, currentRuns.Count);

            for (int i = 0; i < goldenRecords.Count; i++)
            {
                var g = goldenRecords[i];
                var c = currentRuns[i];

                Assert.Equal(g.Seed, c.Seed);
                Assert.Equal(g.OriginIcao, c.OriginIcao);

                // Equivalência de subtipo:
                // Se a missão foi migrada para id estável em texto, normalizamos
                string expectedSubtype = NormalizeSubtypeForComparison(g.Subtype);
                Assert.Equal(expectedSubtype, c.Subtype);

                Assert.Equal(g.DestinationIcao, c.DestinationIcao);
                Assert.Equal(g.Pax, c.Pax);
                Assert.Equal(g.CargoLbs, c.CargoLbs);
                Assert.Equal(g.Reward, c.Reward);
                Assert.Equal(g.DistanceNm, c.DistanceNm);
                Assert.Equal(g.Title, c.Title);
                Assert.Equal(g.RequiredTouchAndGoes, c.RequiredTouchAndGoes);
                Assert.Equal(g.OverflyWaypoints, c.OverflyWaypoints);
                Assert.Equal(g.ContractData, c.ContractData);
            }
        }

        private static string NormalizeSubtypeForComparison(string goldenSubtype)
        {
            // Na primeira execução antes da refatoração, devolve o próprio código.
            // Quando MissionCatalog for implementado, chamará MissionCatalog.Normalize.
            var catalogType = Type.GetType("FlightCareer.Domain.Missions.MissionCatalog, FlightCareer.Domain");
            if (catalogType != null)
            {
                var method = catalogType.GetMethod("Normalize", new[] { typeof(string) });
                if (method != null)
                {
                    var result = method.Invoke(null, new object[] { goldenSubtype });
                    if (result is string s && !string.IsNullOrEmpty(s)) return s;
                }
            }

            return goldenSubtype;
        }
    }
}
