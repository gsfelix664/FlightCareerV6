using CsvHelper;
using CsvHelper.Configuration;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public async Task SeedAsync(Action<string, double>? progressCallback = null)
        {
            await SeedAircraftsAsync(progressCallback);
            var runwaySurfaces = await GetRunwaySurfacesAsync(progressCallback);
            await SeedAirportsAsync(progressCallback, runwaySurfaces);
            await SeedRunwaysAsync(progressCallback);
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

                // Filtros: 4 letras, não heliporto, não nulo
                if (string.IsNullOrEmpty(icao) || icao.Length != 4 || type == "heliport" || processedIcaos.Contains(icao))
                    continue;

                processedIcaos.Add(icao);

                int tier = 4; // Default Small/Dirt
                if (type == "large_airport") tier = 1;
                else if (type == "medium_airport") tier = 2;
                else if (type == "small_airport")
                {
                    // Checar se tem pista pavimentada
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
                    Tier = tier
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
                var runway = new Runway
                {
                    Id = Guid.NewGuid(),
                    AirportIdent = csv.GetField<string>("airport_ident") ?? string.Empty,
                    LengthFt = int.TryParse(csv.GetField<string>("length_ft"), out var len) ? len : 0,
                    WidthFt = int.TryParse(csv.GetField<string>("width_ft"), out var wid) ? wid : 0,
                    Surface = csv.GetField<string>("surface") ?? string.Empty,
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
