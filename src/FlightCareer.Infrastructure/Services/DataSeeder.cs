using CsvHelper;
using CsvHelper.Configuration;
using FlightCareer.Domain.Entities;
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
                    progressCallback?.Invoke("Base de dados global do simulador pronta!", 1.0);
                    return;
                }
            }

            // Fallback para os arquivos CSV tradicionais
            var runwaySurfaces = await GetRunwaySurfacesAsync(progressCallback);
            await SeedAirportsAsync(progressCallback, runwaySurfaces);
            await SeedRunwaysAsync(progressCallback);
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

                // 2. Inserir Aeródromos com ICAO oficial do simulador e elevação real
                // FILTRO ESTRITO: Apenas aeródromos com pistas terrestres para aviões (exclui 42k helipontos e bases de água)
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO Airports (Id, ICAO, Name, City, Country, Latitude, Longitude, ElevationFt, Tier, IsInSimulator, MsfsIdent)
                    SELECT 
                        hex(randomblob(16)),
                        COALESCE(ident, icao),
                        COALESCE(name, 'Aeródromo MSFS'),
                        COALESCE(city, ''),
                        COALESCE(country, region, ''),
                        laty,
                        lonx,
                        CAST(altitude AS INTEGER),
                        CASE 
                            WHEN longest_runway_length >= 7500 AND num_runway_hard > 0 THEN 1
                            WHEN longest_runway_length >= 4500 AND num_runway_hard > 0 THEN 2
                            WHEN num_runway_hard > 0 OR longest_runway_length >= 2500 THEN 3
                            ELSE 4
                        END,
                        1,
                        ident
                    FROM lnm.airport
                    WHERE is_closed = 0 
                      AND ident IS NOT NULL 
                      AND length(ident) >= 3
                      AND (num_runway_hard > 0 OR num_runway_soft > 0)
                      AND longest_runway_length >= 1000;
                ";
                await cmd.ExecuteNonQueryAsync();

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
                    elevation_ft INTEGER
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
            insertCmd.CommandText = "INSERT OR IGNORE INTO CsvAirports (ident, municipality, iso_country, csv_name, elevation_ft) VALUES (@ident, @municipality, @iso_country, @csv_name, @elevation_ft);";
            
            var pIdent = insertCmd.CreateParameter(); pIdent.ParameterName = "@ident"; insertCmd.Parameters.Add(pIdent);
            var pMuni = insertCmd.CreateParameter(); pMuni.ParameterName = "@municipality"; insertCmd.Parameters.Add(pMuni);
            var pCountry = insertCmd.CreateParameter(); pCountry.ParameterName = "@iso_country"; insertCmd.Parameters.Add(pCountry);
            var pName = insertCmd.CreateParameter(); pName.ParameterName = "@csv_name"; insertCmd.Parameters.Add(pName);
            var pElev = insertCmd.CreateParameter(); pElev.ParameterName = "@elevation_ft"; insertCmd.Parameters.Add(pElev);

            while (await csv.ReadAsync())
            {
                var ident = csv.GetField<string>("ident")?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(ident)) continue;

                pIdent.Value = ident;
                pMuni.Value = csv.GetField<string>("municipality") ?? "";
                pCountry.Value = csv.GetField<string>("iso_country") ?? "";
                pName.Value = csv.GetField<string>("name") ?? "";
                pElev.Value = int.TryParse(csv.GetField<string>("elevation_ft"), out var el) ? el : 0;

                await insertCmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            progressCallback?.Invoke("Aplicando municípios e países aos aeroportos...", 0.85);

            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE Airports
                SET 
                    City = CASE WHEN City IS NULL OR City = '' THEN (SELECT municipality FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO) ELSE City END,
                    Country = CASE WHEN Country IS NULL OR length(Country) > 2 OR Country = '' THEN (SELECT iso_country FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO) ELSE Country END,
                    ElevationFt = CASE WHEN ElevationFt IS NULL OR ElevationFt = 0 THEN (SELECT elevation_ft FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO) ELSE ElevationFt END
                WHERE EXISTS (SELECT 1 FROM CsvAirports WHERE CsvAirports.ident = Airports.ICAO);
                
                DROP TABLE IF EXISTS CsvAirports;
            ";
            await updateCmd.ExecuteNonQueryAsync();
        }

        private async Task<Dictionary<string, List<string>>> GetRunwaySurfacesAsync(Action<string, double>? progressCallback)
        {
            progressCallback?.Invoke("Analisando superfícies das pistas...", 0.2);
            var filePath = Path.Combine(_csvPath, "runways.csv");
            var dict = new Dictionary<string, List<string>>();
            if (!File.Exists(filePath)) return dict;

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var ident = csv.GetField<string>("airport_ident");
                var surface = csv.GetField<string>("surface")?.ToUpper() ?? "";
                if (!string.IsNullOrEmpty(ident))
                {
                    if (!dict.ContainsKey(ident)) dict[ident] = new List<string>();
                    dict[ident].Add(surface);
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
            
            var records = csv.GetRecords<dynamic>();
            foreach (var record in records)
            {
                var dict = (IDictionary<string, object>)record;
                var aircraft = new AircraftType
                {
                    Id = Guid.NewGuid(),
                    Model = dict["Modelo"].ToString()!,
                    Type = dict["Tipo"].ToString()!,
                    PurchasePrice = double.Parse(dict["PrecoNovo(dolar)"].ToString()!, CultureInfo.InvariantCulture),
                    FuelTankGallons = double.Parse(dict["Tanque(gal)"].ToString()!, CultureInfo.InvariantCulture),
                    RangeNM = int.Parse(dict["Alcance(nm)"].ToString()!, CultureInfo.InvariantCulture),
                    MaxPayloadLbs = int.Parse(dict["CargaMax(lbs)"].ToString()!, CultureInfo.InvariantCulture),
                    CruiseSpeedKts = int.Parse(dict["VelCruzeiro(kts)"].ToString()!, CultureInfo.InvariantCulture),
                    MaxPax = int.Parse(dict["PaxMax"].ToString()!, CultureInfo.InvariantCulture),
                    FuelConsumptionGPH = double.Parse(dict["ConsumoHora(GPH)"].ToString()!, CultureInfo.InvariantCulture),
                    MinRunwayLength = int.Parse(dict["PistaMin"].ToString()!, CultureInfo.InvariantCulture),
                    FuelType = dict["Combustivel"].ToString()!
                };
                _context.AircraftTypes.Add(aircraft);
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedAirportsAsync(Action<string, double>? progressCallback, Dictionary<string, List<string>> runwaySurfaces)
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
            int count = 0;
            
            string[] hardSurfaces = { "ASPH", "CON", "CONC", "HARD", "BIT", "PEM", "ASP" };

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

                int tier = 4; // Default Small/Dirt
                if (type == "large_airport") tier = 1;
                else if (type == "medium_airport") tier = 2;
                else if (type == "small_airport")
                {
                    if (runwaySurfaces.TryGetValue(icao, out var surfaces))
                    {
                        if (surfaces.Any(s => hardSurfaces.Any(h => s.Contains(h))))
                            tier = 3;
                    }
                }

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
                    Tier = tier,
                    IsInSimulator = true,
                    MsfsIdent = icao
                };
                airports.Add(airport);
                count++;

                if (count % 5000 == 0)
                {
                    progressCallback?.Invoke($"Processando {count} aeroportos (Tier {tier})...", 0.4 + (count / 150000.0));
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
