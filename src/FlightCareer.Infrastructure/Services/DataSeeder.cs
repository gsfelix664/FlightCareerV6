using Microsoft.Data.Sqlite;
using CsvHelper;
using CsvHelper.Configuration;
using FlightCareer.Domain.Airports;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Missions;
using FlightCareer.Domain.Parts;
using FlightCareer.Application.Interfaces;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.Infrastructure.Services
{
    public class DataSeeder : IDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly string _csvPath;

        public DataSeeder(AppDbContext context)
        {
            _context = context;
            _csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CSV");
            
            // Garantir que a pasta CSV exista no diretório de saída
            if (!Directory.Exists(_csvPath))
            {
                // Fallback para o caminho relativo durante desenvolvimento
                _csvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FlightCareer.Infrastructure", "CSV");
            }
        }

        public bool IsSeeded()
        {
            return _context.Airports.Any();
        }

        public static string? LocateLittleNavmapDatabase()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var candidateDirs = new[]
            {
                Path.Combine(appData, "ABarthel", "little_navmap_db"),
                Path.Combine("C:\\Program Files", "Little Navmap", "little_navmap_db"),
                Path.Combine("C:\\Program Files (x86)", "Little Navmap", "little_navmap_db")
            };

            var priorityFiles = new[]
            {
                "little_navmap_msfs24.sqlite",
                "little_navmap_msfs.sqlite",
                "little_navmap_navigraph.sqlite"
            };

            foreach (var dir in candidateDirs)
            {
                if (!Directory.Exists(dir)) continue;

                foreach (var file in priorityFiles)
                {
                    var fullPath = Path.Combine(dir, file);
                    if (File.Exists(fullPath)) return fullPath;
                }

                var anyMsfs = Directory.GetFiles(dir, "*msfs*.sqlite").FirstOrDefault();
                if (anyMsfs != null) return anyMsfs;

                var anySqlite = Directory.GetFiles(dir, "*.sqlite").FirstOrDefault();
                if (anySqlite != null) return anySqlite;
            }

            return null;
        }

        public async Task SeedAsync(Action<string, double>? progressCallback = null)
        {
            await SeedAircraftsAsync(progressCallback);

            var lnmPath = LocateLittleNavmapDatabase();
            if (!string.IsNullOrEmpty(lnmPath) && File.Exists(lnmPath))
            {
                progressCallback?.Invoke("Importando base global de aeroportos e pistas do Little Navmap (MSFS)...", 0.25);
                bool lnmSuccess = await SeedFromLittleNavmapAsync(lnmPath, progressCallback);
                if (lnmSuccess)
                {
                    progressCallback?.Invoke("Enriquecendo aeroportos com municípios e países do CSV...", 0.70);
                    await EnrichFromCsvAsync(progressCallback);
                    progressCallback?.Invoke("Normalizando dados aeronáuticos e higienizando municípios...", 0.90);
                    await NormalizeAirportsInDatabaseAsync(progressCallback);
                    progressCallback?.Invoke("Base de dados global do simulador pronta!", 1.0);
                    return;
                }
            }

            // Fallback para os arquivos CSV tradicionais
            var runwayInfo = await GetRunwayInfoFromCsvAsync(progressCallback);
            await SeedAirportsAsync(progressCallback, runwayInfo);
            await SeedRunwaysAsync(progressCallback);
            await NormalizeAirportsInDatabaseAsync(progressCallback);
        }

        private static string ResolveInitialCountry(string ident, string? country, string? region)
        {
            if (ident.StartsWith("SB") || ident.StartsWith("SS") || ident.StartsWith("SW") || ident.StartsWith("SD") || ident.StartsWith("SJ") || ident.StartsWith("SN") || ident.StartsWith("SI")) return "BR";
            if (ident.StartsWith("K") && ident.Length == 4) return "US";
            if ((ident.StartsWith("C") || ident.StartsWith("CY") || ident.StartsWith("CZ")) && ident.Length == 4) return "CA";
            if (ident.StartsWith("MM")) return "MX";
            if (ident.StartsWith("SA")) return "AR";
            if (ident.StartsWith("SC")) return "CL";
            if (ident.StartsWith("SK")) return "CO";
            if (ident.StartsWith("SP")) return "PE";
            if (ident.StartsWith("SU")) return "UY";
            if (ident.StartsWith("SV")) return "VE";
            if (ident.StartsWith("SG")) return "PY";
            if (ident.StartsWith("SL")) return "BO";
            if (ident.StartsWith("EG")) return "GB";
            if (ident.StartsWith("LF")) return "FR";
            if (ident.StartsWith("ED") || ident.StartsWith("ET")) return "DE";
            if (ident.StartsWith("LE")) return "ES";
            if (ident.StartsWith("LP")) return "PT";
            if (ident.StartsWith("LI")) return "IT";
            if (ident.StartsWith("EH")) return "NL";
            if (ident.StartsWith("EB")) return "BE";
            if (ident.StartsWith("RJ") || ident.StartsWith("RO")) return "JP";
            if (ident.StartsWith("Y") && ident.Length == 4) return "AU";
            if (ident.StartsWith("NZ")) return "NZ";
            if (!string.IsNullOrEmpty(country) && country.Length == 2 && country != "SB" && country != "SS" && country != "SW" && country != "SD" && country != "SI" && country != "SN") return country;
            return !string.IsNullOrEmpty(country) ? country : (!string.IsNullOrEmpty(region) ? region : "");
        }

        private async Task<bool> SeedFromLittleNavmapAsync(string lnmPath, Action<string, double>? progressCallback)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var cmd = connection.CreateCommand();

            try
            {
                // 1. Conectar banco do Little Navmap no SQLite da aplicação
                cmd.CommandText = $"ATTACH DATABASE '{lnmPath.Replace("'", "''")}' AS lnm;";
                await cmd.ExecuteNonQueryAsync();

                progressCallback?.Invoke("Gravando aeródromos e pistas de aviões nativos do MSFS...", 0.35);

                // 2. Ler aeródromos e estrutura do Little Navmap e classificar Tier via AirportTierClassifier
                cmd.CommandText = @"
                    SELECT 
                        COALESCE(ident, icao),
                        COALESCE(name, 'Aeródromo MSFS'),
                        COALESCE(city, ''),
                        country,
                        region,
                        laty,
                        lonx,
                        CAST(altitude AS INTEGER),
                        longest_runway_length,
                        longest_runway_surface,
                        num_runway_hard,
                        num_runway_end_ils,
                        num_approach,
                        num_parking_gate,
                        num_jetway,
                        num_parking_cargo,
                        has_tower_object,
                        tower_frequency
                    FROM lnm.airport
                    WHERE is_closed = 0 
                      AND ident IS NOT NULL 
                      AND length(ident) >= 3
                      AND (num_runway_hard > 0 OR num_runway_soft > 0)
                      AND longest_runway_length >= 1000;
                ";

                var rules = AirportTierRules.Default;
                var profileCatalog = AirportProfileCatalog.Shared;

                var airportsToInsert = new List<Airport>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var ident = reader.GetString(0);
                        var name = reader.GetString(1);
                        var city = reader.GetString(2);
                        var rawCountry = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        var rawRegion = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        var lat = reader.GetDouble(5);
                        var lon = reader.GetDouble(6);
                        var alt = reader.IsDBNull(7) ? 0 : Convert.ToInt32(reader.GetValue(7));
                        var longestRunway = reader.IsDBNull(8) ? 0 : Convert.ToInt32(reader.GetValue(8));
                        var rwSurface = reader.IsDBNull(9) ? "" : reader.GetString(9);
                        var numHard = reader.IsDBNull(10) ? 0 : Convert.ToInt32(reader.GetValue(10));
                        var ilsEnds = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader.GetValue(11));
                        var approaches = reader.IsDBNull(12) ? 0 : Convert.ToInt32(reader.GetValue(12));
                        var gates = reader.IsDBNull(13) ? 0 : Convert.ToInt32(reader.GetValue(13));
                        var jetways = reader.IsDBNull(14) ? 0 : Convert.ToInt32(reader.GetValue(14));
                        var cargoParking = reader.IsDBNull(15) ? 0 : Convert.ToInt32(reader.GetValue(15));
                        var hasTowerObj = reader.IsDBNull(16) ? 0 : Convert.ToInt32(reader.GetValue(16));
                        var towerFreq = reader.IsDBNull(17) ? 0.0 : Convert.ToDouble(reader.GetValue(17));

                        bool hasTower = hasTowerObj > 0 || towerFreq > 0;
                        bool isPaved = rwSurface is "A" or "C" or "B" or "T" ||
                                       (numHard > 0 && rwSurface is not ("G" or "D" or "GR" or "S" or "SN" or "CR" or "W" or "Grass" or "Dirt" or "Gravel" or "Sand" or "Snow"));
                        var country = ResolveInitialCountry(ident, rawCountry, rawRegion);

                        var tags = profileCatalog.TagsFor(ident);
                        var input = new AirportTierInput(
                            tags,
                            IlsEnds: ilsEnds,
                            Approaches: approaches,
                            Gates: gates,
                            Jetways: jetways,
                            CargoParking: cargoParking,
                            HasTower: hasTower,
                            LongestRunwayFt: longestRunway,
                            LongestRunwayPaved: isPaved,
                            CsvType: null,
                            ScheduledService: null
                        );

                        var tierScore = AirportTierClassifier.Score(input, rules);

                        airportsToInsert.Add(new Airport
                        {
                            Id = Guid.NewGuid(),
                            ICAO = ident,
                            Name = name,
                            City = city,
                            Country = country,
                            Latitude = lat,
                            Longitude = lon,
                            ElevationFt = alt,
                            Tier = tierScore.Tier,
                            IsInSimulator = true,
                            MsfsIdent = ident,
                            IlsEnds = ilsEnds,
                            Approaches = approaches,
                            Gates = gates + jetways,
                            CargoParking = cargoParking,
                            HasTower = hasTower,
                            LongestRunwayFt = longestRunway,
                            LongestRunwayPaved = isPaved,
                            TierScore = tierScore.TotalScore
                        });
                    }
                }

                // Inserir aeroportos em lote no SQLite
                using (var transaction = connection.BeginTransaction())
                {
                    using var insertAirportCmd = connection.CreateCommand();
                    insertAirportCmd.Transaction = transaction;
                    insertAirportCmd.CommandText = @"
                        INSERT OR IGNORE INTO Airports (
                            Id, ICAO, Name, City, Country, Latitude, Longitude, ElevationFt, 
                            Tier, IsInSimulator, MsfsIdent, IlsEnds, Approaches, Gates, 
                            CargoParking, HasTower, LongestRunwayFt, LongestRunwayPaved, TierScore
                        ) VALUES (
                            @Id, @ICAO, @Name, @City, @Country, @Latitude, @Longitude, @ElevationFt,
                            @Tier, @IsInSimulator, @MsfsIdent, @IlsEnds, @Approaches, @Gates,
                            @CargoParking, @HasTower, @LongestRunwayFt, @LongestRunwayPaved, @TierScore
                        );";

                    var pId = insertAirportCmd.CreateParameter(); pId.ParameterName = "@Id"; insertAirportCmd.Parameters.Add(pId);
                    var pIcao = insertAirportCmd.CreateParameter(); pIcao.ParameterName = "@ICAO"; insertAirportCmd.Parameters.Add(pIcao);
                    var pName = insertAirportCmd.CreateParameter(); pName.ParameterName = "@Name"; insertAirportCmd.Parameters.Add(pName);
                    var pCity = insertAirportCmd.CreateParameter(); pCity.ParameterName = "@City"; insertAirportCmd.Parameters.Add(pCity);
                    var pCountry = insertAirportCmd.CreateParameter(); pCountry.ParameterName = "@Country"; insertAirportCmd.Parameters.Add(pCountry);
                    var pLat = insertAirportCmd.CreateParameter(); pLat.ParameterName = "@Latitude"; insertAirportCmd.Parameters.Add(pLat);
                    var pLon = insertAirportCmd.CreateParameter(); pLon.ParameterName = "@Longitude"; insertAirportCmd.Parameters.Add(pLon);
                    var pElev = insertAirportCmd.CreateParameter(); pElev.ParameterName = "@ElevationFt"; insertAirportCmd.Parameters.Add(pElev);
                    var pTier = insertAirportCmd.CreateParameter(); pTier.ParameterName = "@Tier"; insertAirportCmd.Parameters.Add(pTier);
                    var pInSim = insertAirportCmd.CreateParameter(); pInSim.ParameterName = "@IsInSimulator"; insertAirportCmd.Parameters.Add(pInSim);
                    var pMsfs = insertAirportCmd.CreateParameter(); pMsfs.ParameterName = "@MsfsIdent"; insertAirportCmd.Parameters.Add(pMsfs);
                    var pIls = insertAirportCmd.CreateParameter(); pIls.ParameterName = "@IlsEnds"; insertAirportCmd.Parameters.Add(pIls);
                    var pApp = insertAirportCmd.CreateParameter(); pApp.ParameterName = "@Approaches"; insertAirportCmd.Parameters.Add(pApp);
                    var pGates = insertAirportCmd.CreateParameter(); pGates.ParameterName = "@Gates"; insertAirportCmd.Parameters.Add(pGates);
                    var pCargo = insertAirportCmd.CreateParameter(); pCargo.ParameterName = "@CargoParking"; insertAirportCmd.Parameters.Add(pCargo);
                    var pTower = insertAirportCmd.CreateParameter(); pTower.ParameterName = "@HasTower"; insertAirportCmd.Parameters.Add(pTower);
                    var pRwFt = insertAirportCmd.CreateParameter(); pRwFt.ParameterName = "@LongestRunwayFt"; insertAirportCmd.Parameters.Add(pRwFt);
                    var pRwPvd = insertAirportCmd.CreateParameter(); pRwPvd.ParameterName = "@LongestRunwayPaved"; insertAirportCmd.Parameters.Add(pRwPvd);
                    var pScore = insertAirportCmd.CreateParameter(); pScore.ParameterName = "@TierScore"; insertAirportCmd.Parameters.Add(pScore);

                    foreach (var a in airportsToInsert)
                    {
                        pId.Value = a.Id.ToString("N");
                        pIcao.Value = a.ICAO;
                        pName.Value = a.Name;
                        pCity.Value = a.City;
                        pCountry.Value = a.Country;
                        pLat.Value = a.Latitude;
                        pLon.Value = a.Longitude;
                        pElev.Value = a.ElevationFt;
                        pTier.Value = a.Tier;
                        pInSim.Value = a.IsInSimulator ? 1 : 0;
                        pMsfs.Value = a.MsfsIdent ?? "";
                        pIls.Value = a.IlsEnds;
                        pApp.Value = a.Approaches;
                        pGates.Value = a.Gates;
                        pCargo.Value = a.CargoParking;
                        pTower.Value = a.HasTower ? 1 : 0;
                        pRwFt.Value = a.LongestRunwayFt;
                        pRwPvd.Value = a.LongestRunwayPaved ? 1 : 0;
                        pScore.Value = a.TierScore;

                        await insertAirportCmd.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                }

                progressCallback?.Invoke("Gravando pistas operacionais para aviões...", 0.55);

                // 3. Inserir Pistas reais com superfícies normalizadas e iluminação (exclui água e pistas curtas < 800ft)
                cmd.CommandText = @"
                    INSERT INTO Runways (Id, AirportIdent, LengthFt, WidthFt, Surface, IsLighted)
                    SELECT 
                        hex(randomblob(16)),
                        a.ident,
                        CAST(r.length AS INTEGER),
                        CAST(r.width AS INTEGER),
                        CASE r.surface
                            WHEN 'A' THEN 'Asphalt'
                            WHEN 'C' THEN 'Concrete'
                            WHEN 'B' THEN 'Bituminous'
                            WHEN 'G' THEN 'Grass'
                            WHEN 'D' THEN 'Dirt'
                            WHEN 'GR' THEN 'Gravel'
                            WHEN 'S' THEN 'Sand'
                            WHEN 'SN' THEN 'Snow'
                            WHEN 'CR' THEN 'Coral'
                            WHEN 'M' THEN 'Mat'
                            WHEN 'T' THEN 'Tarmac'
                            ELSE COALESCE(r.surface, 'Other')
                        END,
                        CASE WHEN r.edge_light IS NOT NULL AND r.edge_light != 'NO' THEN 1 ELSE 0 END
                    FROM lnm.runway r
                    JOIN lnm.airport a ON r.airport_id = a.airport_id
                    WHERE a.is_closed = 0
                      AND (a.num_runway_hard > 0 OR a.num_runway_soft > 0)
                      AND a.longest_runway_length >= 1000
                      AND r.surface != 'W'
                      AND r.length >= 800;
                ";
                await cmd.ExecuteNonQueryAsync();

                // 4. Desconectar LNM
                cmd.CommandText = "DETACH DATABASE lnm;";
                await cmd.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataSeeder] Erro ao importar do Little Navmap: {ex.Message}");
                try
                {
                    cmd.CommandText = "DETACH DATABASE lnm;";
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { }
                return false;
            }
        }

        /// <summary>Distância máxima (NM) para considerar que o código do csv é o mesmo aeródromo do simulador.</summary>
        public const double SameAirportMaxNm = 10.0;

        /// <summary>True quando as duas posições estão a até <see cref="SameAirportMaxNm"/> NM.</summary>
        public static bool IsSameAirportPosition(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double nm = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)) * 3440.065;
            return nm <= SameAirportMaxNm;
        }

        private async Task EnrichFromCsvAsync(Action<string, double>? progressCallback)
        {
            var filePath = Path.Combine(_csvPath, "airports.csv");
            if (!File.Exists(filePath)) return;

            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = @"
                CREATE TEMP TABLE IF NOT EXISTS CsvAirports (
                    ident TEXT PRIMARY KEY,
                    municipality TEXT,
                    iso_country TEXT,
                    csv_name TEXT,
                    elevation_ft INTEGER,
                    lat REAL,
                    lon REAL,
                    type TEXT,
                    scheduled_service TEXT
                );
                DELETE FROM CsvAirports;
            ";
            await createCmd.ExecuteNonQueryAsync();

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            await csv.ReadAsync();
            csv.ReadHeader();

            using var transaction = connection.BeginTransaction();
            using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = "INSERT OR IGNORE INTO CsvAirports (ident, municipality, iso_country, csv_name, elevation_ft, lat, lon, type, scheduled_service) VALUES (@ident, @municipality, @iso_country, @csv_name, @elevation_ft, @lat, @lon, @type, @scheduled_service);";
            
            var pIdent = insertCmd.CreateParameter(); pIdent.ParameterName = "@ident"; insertCmd.Parameters.Add(pIdent);
            var pMuni = insertCmd.CreateParameter(); pMuni.ParameterName = "@municipality"; insertCmd.Parameters.Add(pMuni);
            var pCountry = insertCmd.CreateParameter(); pCountry.ParameterName = "@iso_country"; insertCmd.Parameters.Add(pCountry);
            var pName = insertCmd.CreateParameter(); pName.ParameterName = "@csv_name"; insertCmd.Parameters.Add(pName);
            var pElev = insertCmd.CreateParameter(); pElev.ParameterName = "@elevation_ft"; insertCmd.Parameters.Add(pElev);
            var pLat = insertCmd.CreateParameter(); pLat.ParameterName = "@lat"; insertCmd.Parameters.Add(pLat);
            var pLon = insertCmd.CreateParameter(); pLon.ParameterName = "@lon"; insertCmd.Parameters.Add(pLon);
            var pType = insertCmd.CreateParameter(); pType.ParameterName = "@type"; insertCmd.Parameters.Add(pType);
            var pSched = insertCmd.CreateParameter(); pSched.ParameterName = "@scheduled_service"; insertCmd.Parameters.Add(pSched);

            while (await csv.ReadAsync())
            {
                var ident = csv.GetField<string>("ident")?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(ident)) continue;

                pIdent.Value = ident;
                pMuni.Value = csv.GetField<string>("municipality") ?? "";
                pCountry.Value = csv.GetField<string>("iso_country") ?? "";
                pName.Value = csv.GetField<string>("name") ?? "";
                pElev.Value = int.TryParse(csv.GetField<string>("elevation_ft"), out var el) ? el : 0;
                pLat.Value = double.TryParse(csv.GetField<string>("latitude_deg"), NumberStyles.Float, CultureInfo.InvariantCulture, out var la) ? la : (object)DBNull.Value;
                pLon.Value = double.TryParse(csv.GetField<string>("longitude_deg"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lo) ? lo : (object)DBNull.Value;
                pType.Value = csv.GetField<string>("type") ?? "";
                pSched.Value = csv.GetField<string>("scheduled_service") ?? "";

                await insertCmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            progressCallback?.Invoke("Aplicando municípios e países aos aeroportos...", 0.85);

            // Códigos reaproveitados: o mesmo código no airports.csv antigo pode ser outro aeródromo no
            // simulador (ex.: SBOK = fazenda no MT no csv, Amontada/CE no MSFS 2024). Só completa cidade,
            // país e elevação quando a posição do csv bate com a do simulador.
            if (connection is SqliteConnection sqlite)
            {
                sqlite.CreateFunction("fc_same_place", (double? csvLat, double? csvLon, double simLat, double simLon) =>
                    csvLat.HasValue && csvLon.HasValue && IsSameAirportPosition(csvLat.Value, csvLon.Value, simLat, simLon));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DataSeeder] Conexão não é SQLite: enriquecimento pelo CSV sem conferência de posição.");
            }
            string positionMatch = connection is SqliteConnection
                ? "AND fc_same_place(CsvAirports.lat, CsvAirports.lon, Airports.Latitude, Airports.Longitude)"
                : "";

            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE Airports
                SET 
                    City = CASE WHEN City IS NULL OR City = '' THEN (SELECT municipality FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO) ELSE City END,
                    Country = CASE 
                        WHEN ICAO LIKE 'SB%' OR ICAO LIKE 'SS%' OR ICAO LIKE 'SW%' OR ICAO LIKE 'SD%' OR ICAO LIKE 'SJ%' OR ICAO LIKE 'SN%' OR ICAO LIKE 'SI%' THEN 'BR'
                        WHEN Country IS NULL OR length(Country) > 2 OR Country = '' OR Country IN ('SB','SS','SW','SJ','SD','SI','SN') 
                            THEN COALESCE((SELECT iso_country FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO), Country) 
                        ELSE Country 
                    END,
                    ElevationFt = CASE WHEN ElevationFt IS NULL OR ElevationFt = 0 THEN (SELECT elevation_ft FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO) ELSE ElevationFt END
                WHERE EXISTS (SELECT 1 FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO " + positionMatch + @");
            ";
            await updateCmd.ExecuteNonQueryAsync();

            progressCallback?.Invoke("Recalculando Tiers por movimento com dados do CSV...", 0.88);

            using var recalculateCmd = connection.CreateCommand();
            recalculateCmd.CommandText = @"
                SELECT 
                    Airports.ICAO, 
                    Airports.IlsEnds, 
                    Airports.Approaches, 
                    Airports.Gates, 
                    Airports.CargoParking, 
                    Airports.HasTower, 
                    Airports.LongestRunwayFt, 
                    Airports.LongestRunwayPaved, 
                    CsvAirports.type, 
                    CsvAirports.scheduled_service
                FROM Airports
                JOIN CsvAirports ON CsvAirports.ident = Airports.ICAO
                WHERE 1=1 " + positionMatch + ";";

            var tierUpdates = new List<(string Icao, int Tier, int TierScore)>();
            using (var recalculateReader = await recalculateCmd.ExecuteReaderAsync())
            {
                var rules = AirportTierRules.Default;
                var catalog = AirportProfileCatalog.Shared;

                while (await recalculateReader.ReadAsync())
                {
                    var icao = recalculateReader.GetString(0);
                    var ils = recalculateReader.GetInt32(1);
                    var app = recalculateReader.GetInt32(2);
                    var gates = recalculateReader.GetInt32(3);
                    var cargo = recalculateReader.GetInt32(4);
                    var tower = recalculateReader.GetInt32(5) != 0;
                    var rwFt = recalculateReader.GetInt32(6);
                    var rwPaved = recalculateReader.GetInt32(7) != 0;
                    var csvType = recalculateReader.IsDBNull(8) ? null : recalculateReader.GetString(8);
                    var schedStr = recalculateReader.IsDBNull(9) ? null : recalculateReader.GetString(9);
                    bool sched = string.Equals(schedStr, "yes", StringComparison.OrdinalIgnoreCase);

                    var tags = catalog.TagsFor(icao);
                    var input = new AirportTierInput(
                        tags,
                        IlsEnds: ils,
                        Approaches: app,
                        Gates: gates,
                        Jetways: 0,
                        CargoParking: cargo,
                        HasTower: tower,
                        LongestRunwayFt: rwFt,
                        LongestRunwayPaved: rwPaved,
                        CsvType: csvType,
                        ScheduledService: sched
                    );

                    var score = AirportTierClassifier.Score(input, rules);
                    tierUpdates.Add((icao, score.Tier, score.TotalScore));
                }
            }

            if (tierUpdates.Count > 0)
            {
                using var updateTx = connection.BeginTransaction();
                using var updateTierCmd = connection.CreateCommand();
                updateTierCmd.Transaction = updateTx;
                updateTierCmd.CommandText = "UPDATE Airports SET Tier = @tier, TierScore = @tierScore WHERE ICAO = @icao;";

                var pUptTier = updateTierCmd.CreateParameter(); pUptTier.ParameterName = "@tier"; updateTierCmd.Parameters.Add(pUptTier);
                var pUptScore = updateTierCmd.CreateParameter(); pUptScore.ParameterName = "@tierScore"; updateTierCmd.Parameters.Add(pUptScore);
                var pUptIcao = updateTierCmd.CreateParameter(); pUptIcao.ParameterName = "@icao"; updateTierCmd.Parameters.Add(pUptIcao);

                foreach (var (icao, tier, tierScore) in tierUpdates)
                {
                    pUptTier.Value = tier;
                    pUptScore.Value = tierScore;
                    pUptIcao.Value = icao;
                    await updateTierCmd.ExecuteNonQueryAsync();
                }

                await updateTx.CommitAsync();
            }

            using var dropCmd = connection.CreateCommand();
            dropCmd.CommandText = "DROP TABLE IF EXISTS CsvAirports;";
            await dropCmd.ExecuteNonQueryAsync();
        }

        private async Task NormalizeAirportsInDatabaseAsync(Action<string, double>? progressCallback)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            // 1. Correções SQL imediatas para FIRs e SBWM
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE Airports 
                    SET Country = 'BR' 
                    WHERE (ICAO LIKE 'SB%' OR ICAO LIKE 'SS%' OR ICAO LIKE 'SW%' OR ICAO LIKE 'SD%' OR ICAO LIKE 'SJ%' OR ICAO LIKE 'SN%' OR ICAO LIKE 'SI%')
                      AND (Country IN ('SB','SS','SW','SJ','SD','SI','SN') OR Country = '' OR Country IS NULL);

                    UPDATE Airports 
                    SET City = 'Ribeirão Branco', Country = 'BR' 
                    WHERE ICAO = 'SBWM' AND (City IS NULL OR City = '' OR City = 'SB');
                ";
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Extração inteligente de municípios vazios via AviationDataNormalizer
            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT Id, ICAO, Name, Country FROM Airports WHERE City IS NULL OR City = '' OR City IN ('SB','SS','SW','SJ','SD','SI','SN');";

            var pendingUpdates = new List<(string Id, string City, string Country)>();
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = reader.GetString(0);
                    var icao = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var name = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    var country = reader.IsDBNull(3) ? "" : reader.GetString(3);

                    if (AviationDataNormalizer.TryExtractCityFromName(name, out var extractedCity))
                    {
                        var resolvedCountry = AviationDataNormalizer.ResolveCountryFromIcao(icao, country);
                        pendingUpdates.Add((id, extractedCity, resolvedCountry));
                    }
                }
            }

            if (pendingUpdates.Count > 0)
            {
                using var transaction = connection.BeginTransaction();
                using var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = "UPDATE Airports SET City = @city, Country = @country WHERE Id = @id;";

                var pCity = updateCmd.CreateParameter(); pCity.ParameterName = "@city"; updateCmd.Parameters.Add(pCity);
                var pCountry = updateCmd.CreateParameter(); pCountry.ParameterName = "@country"; updateCmd.Parameters.Add(pCountry);
                var pId = updateCmd.CreateParameter(); pId.ParameterName = "@id"; updateCmd.Parameters.Add(pId);

                for (int i = 0; i < pendingUpdates.Count; i++)
                {
                    var (id, city, country) = pendingUpdates[i];
                    pCity.Value = city;
                    pCountry.Value = country;
                    pId.Value = id;
                    await updateCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
        }

        private async Task<Dictionary<string, (int LongestLengthFt, bool IsPaved)>> GetRunwayInfoFromCsvAsync(Action<string, double>? progressCallback)
        {
            progressCallback?.Invoke("Analisando superfícies das pistas...", 0.2);
            var filePath = Path.Combine(_csvPath, "runways.csv");
            var dict = new Dictionary<string, (int LongestLengthFt, bool IsPaved)>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(filePath)) return dict;

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var ident = csv.GetField<string>("airport_ident")?.Trim();
                var surface = csv.GetField<string>("surface") ?? "";
                int length = int.TryParse(csv.GetField<string>("length_ft"), out var l) ? l : 0;
                bool isHard = RunwaySurface.IsHard(surface);

                if (!string.IsNullOrEmpty(ident))
                {
                    if (!dict.TryGetValue(ident, out var existing))
                    {
                        dict[ident] = (length, isHard);
                    }
                    else
                    {
                        int bestLen = Math.Max(existing.LongestLengthFt, length);
                        bool bestPaved = existing.IsPaved || isHard;
                        dict[ident] = (bestLen, bestPaved);
                    }
                }
            }
            return dict;
        }

        private async Task SeedAircraftsAsync(Action<string, double>? progressCallback)
        {
            if (await _context.AircraftTypes.AnyAsync()) return;

            progressCallback?.Invoke("Carregando Aeronaves...", 0.1);
            var filePath = Path.Combine(_csvPath, "aircrafts.csv");
            if (!File.Exists(filePath)) return;

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            var profiles = new AircraftLimitCatalogLoader().LoadProfiles();
            var resolver = new AircraftLimitResolver(profiles, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var records = csv.GetRecords<dynamic>();
            foreach (var record in records)
            {
                var dict = (IDictionary<string, object>)record;
                var model = dict["Modelo"].ToString()!;
                var matched = resolver.FindById(model) ?? resolver.FindProfile(model, model);
                bool hasConvertible = matched != null && matched.Features.Contains(AircraftFeatures.ConvertibleCabin, StringComparer.OrdinalIgnoreCase);

                var aircraft = new AircraftType
                {
                    Id = Guid.NewGuid(),
                    Model = model,
                    Type = dict["Tipo"].ToString()!,
                    PurchasePrice = double.Parse(dict["PrecoNovo(dolar)"].ToString()!, CultureInfo.InvariantCulture),
                    FuelTankGallons = double.Parse(dict["Tanque(gal)"].ToString()!, CultureInfo.InvariantCulture),
                    RangeNM = int.Parse(dict["Alcance(nm)"].ToString()!, CultureInfo.InvariantCulture),
                    MaxPayloadLbs = int.Parse(dict["CargaMax(lbs)"].ToString()!, CultureInfo.InvariantCulture),
                    CruiseSpeedKts = int.Parse(dict["VelCruzeiro(kts)"].ToString()!, CultureInfo.InvariantCulture),
                    MaxPax = int.Parse(dict["PaxMax"].ToString()!, CultureInfo.InvariantCulture),
                    FuelConsumptionGPH = double.Parse(dict["ConsumoHora(GPH)"].ToString()!, CultureInfo.InvariantCulture),
                    MinRunwayLength = int.Parse(dict["PistaMin"].ToString()!, CultureInfo.InvariantCulture),
                    FuelType = dict["Combustivel"].ToString()!,
                    HasConvertibleCabin = hasConvertible
                };
                _context.AircraftTypes.Add(aircraft);
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedAirportsAsync(Action<string, double>? progressCallback, Dictionary<string, (int LongestLengthFt, bool IsPaved)> runwayInfo)
        {
            if (await _context.Airports.AnyAsync()) return;

            progressCallback?.Invoke("Carregando Aeroportos e classificando Tiers...", 0.4);
            var filePath = Path.Combine(_csvPath, "airports.csv");
            if (!File.Exists(filePath)) return;

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            await csv.ReadAsync();
            csv.ReadHeader();

            var airports = new List<Airport>();
            var processedIcaos = new HashSet<string>();
            var rules = AirportTierRules.Default;
            var catalog = AirportProfileCatalog.Shared;
            int count = 0;

            while (await csv.ReadAsync())
            {
                var icao = csv.GetField<string>("icao_code");
                if (string.IsNullOrEmpty(icao)) icao = csv.GetField<string>("ident");
                var type = csv.GetField<string>("type") ?? "";

                // Filtros de aviação: apenas aeródromos para aviões (exclui heliportos, hidro bases, balões e fechados)
                if (string.IsNullOrEmpty(icao) || icao.Length < 3 || 
                    type == "heliport" || type == "seaplane_base" || type == "balloonport" || type == "closed" ||
                    processedIcaos.Contains(icao))
                    continue;

                processedIcaos.Add(icao);

                int longestRunwayFt = 0;
                bool longestPaved = false;
                if (runwayInfo.TryGetValue(icao, out var rinfo))
                {
                    longestRunwayFt = rinfo.LongestLengthFt;
                    longestPaved = rinfo.IsPaved;
                }

                var tags = catalog.TagsFor(icao);
                bool scheduled = string.Equals(csv.GetField<string>("scheduled_service"), "yes", StringComparison.OrdinalIgnoreCase);

                var input = new AirportTierInput(
                    tags,
                    IlsEnds: 0,
                    Approaches: 0,
                    Gates: 0,
                    Jetways: 0,
                    CargoParking: 0,
                    HasTower: false,
                    LongestRunwayFt: longestRunwayFt,
                    LongestRunwayPaved: longestPaved,
                    CsvType: type,
                    ScheduledService: scheduled
                );

                var score = AirportTierClassifier.Score(input, rules);

                var airport = new Airport
                {
                    Id = Guid.NewGuid(),
                    ICAO = icao,
                    Name = csv.GetField<string>("name") ?? string.Empty,
                    City = csv.GetField<string>("municipality") ?? string.Empty,
                    Country = csv.GetField<string>("iso_country") ?? string.Empty,
                    Latitude = double.TryParse(csv.GetField<string>("latitude_deg"), CultureInfo.InvariantCulture, out var lat) ? lat : 0,
                    Longitude = double.TryParse(csv.GetField<string>("longitude_deg"), CultureInfo.InvariantCulture, out var lon) ? lon : 0,
                    ElevationFt = int.TryParse(csv.GetField<string>("elevation_ft"), out var elev) ? elev : 0,
                    Tier = score.Tier,
                    IsInSimulator = true,
                    MsfsIdent = icao,
                    LongestRunwayFt = longestRunwayFt,
                    LongestRunwayPaved = longestPaved,
                    TierScore = score.TotalScore
                };
                AviationDataNormalizer.NormalizeAirport(airport);
                airports.Add(airport);
                count++;

                if (count % 5000 == 0)
                {
                    progressCallback?.Invoke($"Processando {count} aeroportos (Tier {score.Tier})...", 0.4 + (count / 150000.0));
                    await _context.Airports.AddRangeAsync(airports);
                    await _context.SaveChangesAsync();
                    airports.Clear();
                }
            }
            if (airports.Any())
            {
                await _context.Airports.AddRangeAsync(airports);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedRunwaysAsync(Action<string, double>? progressCallback)
        {
            if (await _context.Runways.AnyAsync()) return;

            progressCallback?.Invoke("Carregando Pistas...", 0.8);
            var filePath = Path.Combine(_csvPath, "runways.csv");
            if (!File.Exists(filePath)) return;

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            await csv.ReadAsync();
            csv.ReadHeader();

            var runways = new List<Runway>();
            int count = 0;

            while (await csv.ReadAsync())
            {
                var surface = csv.GetField<string>("surface") ?? string.Empty;
                var len = int.TryParse(csv.GetField<string>("length_ft"), out var l) ? l : 0;

                // Excluir pistas de água e pistas muito curtas para aviões (< 800 ft)
                if (surface.ToUpperInvariant().Contains("WAT") || len < 800)
                    continue;

                var runway = new Runway
                {
                    Id = Guid.NewGuid(),
                    AirportIdent = csv.GetField<string>("airport_ident") ?? string.Empty,
                    LengthFt = len,
                    WidthFt = int.TryParse(csv.GetField<string>("width_ft"), out var wid) ? wid : 0,
                    Surface = surface,
                    IsLighted = csv.GetField<string>("lighted") == "1"
                };
                runways.Add(runway);
                count++;

                if (count % 5000 == 0)
                {
                    await _context.Runways.AddRangeAsync(runways);
                    await _context.SaveChangesAsync();
                    runways.Clear();
                }
            }
            await _context.Runways.AddRangeAsync(runways);
            await _context.SaveChangesAsync();
            progressCallback?.Invoke("Concluído!", 1.0);
        }
    }
}
