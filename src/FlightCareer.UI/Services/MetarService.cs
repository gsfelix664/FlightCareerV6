using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.UI.Services
{
    public class MetarInfo
    {
        public string Icao { get; set; } = string.Empty;
        public string RawMetar { get; set; } = string.Empty;
        public string FlightCategory { get; set; } = "VFR"; // VFR, MVFR, IFR, LIFR
        public int? WindDir { get; set; }
        public int? WindSpeedKt { get; set; }
        public int? WindGustKt { get; set; }
        public string VisibilityStr { get; set; } = string.Empty;
        public double? TempC { get; set; }
        public double? DewpointC { get; set; }
        public double? AltimeterHpa { get; set; }
        public DateTime ReportTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool IsNearestStation { get; set; }
        public string? NearestStationIcao { get; set; }
        public double? DistanceToStationNm { get; set; }

        public string CategoryColor => FlightCategory switch
        {
            "LIFR" => "#E040FB", // Roxo / Magenta
            "IFR" => "#FF5252",  // Vermelho
            "MVFR" => "#FFB300", // Âmbar
            _ => "#00E676"       // Verde VFR
        };

        public string CategoryIcon => FlightCategory switch
        {
            "LIFR" => "🌫️",
            "IFR" => "🌧️",
            "MVFR" => "⛅",
            _ => "☀️"
        };

        public string FormattedSummary
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RawMetar))
                    return "Meteorologia indisponível no momento";

                if (IsNearestStation)
                {
                    return $"Via {NearestStationIcao} ({DistanceToStationNm:F0} NM): {RawMetar}";
                }

                return RawMetar;
            }
        }
    }

    /// <summary>
    /// Serviço de Meteorologia Aeronáutica Real conectado diretamente à API oficial da NOAA (AviationWeather.gov).
    /// Realiza requisições em lote de alta performance, armazena cache com TTL e resolve estações próximas para pistas não reportantes.
    /// </summary>
    public class MetarService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext? _context;
        private readonly IDbContextFactory<AppDbContext>? _contextFactory;
        private readonly ConcurrentDictionary<string, (MetarInfo Info, DateTime FetchedAt)> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

        public MetarService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FlightCareerV6/1.0 (Aviation Weather Client; Windows)");
        }

        public MetarService(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FlightCareerV6/1.0 (Aviation Weather Client; Windows)");
        }

        /// <summary>
        /// Obtém o METAR real de um aeroporto específico.
        /// </summary>
        public async Task<MetarInfo?> GetMetarAsync(string icao, CancellationToken ct = default)
        {
            var dict = await GetMetarsAsync(new[] { icao }, ct);
            dict.TryGetValue(icao, out var metar);
            return metar;
        }

        /// <summary>
        /// Obtém em lote os METARs reais para uma coleção de códigos ICAO.
        /// </summary>
        public async Task<Dictionary<string, MetarInfo>> GetMetarsAsync(IEnumerable<string> icaos, CancellationToken ct = default)
        {
            var result = new Dictionary<string, MetarInfo>(StringComparer.OrdinalIgnoreCase);
            var distinctIcaos = icaos
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim().ToUpper())
                .Distinct()
                .ToList();

            if (!distinctIcaos.Any())
                return result;

            var toFetch = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var icao in distinctIcaos)
            {
                if (_cache.TryGetValue(icao, out var entry) && (now - entry.FetchedAt) < _cacheTtl)
                {
                    result[icao] = entry.Info;
                }
                else
                {
                    toFetch.Add(icao);
                }
            }

            if (toFetch.Any())
            {
                // Dividir em lotes de 60 aeroportos por chamada
                const int batchSize = 60;
                for (int i = 0; i < toFetch.Count; i += batchSize)
                {
                    var batch = toFetch.Skip(i).Take(batchSize).ToList();
                    await FetchBatchFromNoaaAsync(batch, result, ct);
                }
            }

            // Para os aeroportos solicitados que não foram retornados pela NOAA (pistas pequenas sem estação AWOS/METAR):
            // Encontra a estação reportante real mais próxima no cache
            var missingIcaos = distinctIcaos.Where(id => !result.ContainsKey(id)).ToList();
            if (missingIcaos.Any())
            {
                await ResolveNearestReportingStationsAsync(missingIcaos, result);
            }

            return result;
        }

        private async Task FetchBatchFromNoaaAsync(List<string> batch, Dictionary<string, MetarInfo> result, CancellationToken ct)
        {
            try
            {
                string idsParam = string.Join(",", batch);
                string url = $"https://aviationweather.gov/api/data/metar?ids={idsParam}&format=json";

                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                    return;

                string json = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
                    return;

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return;

                var now = DateTime.UtcNow;

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var metar = ParseMetarElement(element);
                    if (metar != null && !string.IsNullOrWhiteSpace(metar.Icao))
                    {
                        _cache[metar.Icao] = (metar, now);
                        result[metar.Icao] = metar;
                    }
                }
            }
            catch
            {
                // Resiliente a falhas de rede temporárias
            }
        }

        private MetarInfo? ParseMetarElement(JsonElement el)
        {
            try
            {
                string icao = el.TryGetProperty("icaoId", out var pIcao) ? pIcao.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(icao))
                    return null;

                string rawMetar = el.TryGetProperty("rawOb", out var pRaw) ? pRaw.GetString() ?? "" : "";
                string fltCat = el.TryGetProperty("fltCat", out var pCat) ? pCat.GetString() ?? "VFR" : "VFR";
                
                // Normalizar categoria
                fltCat = fltCat.Trim().ToUpper() switch
                {
                    "LIFR" => "LIFR",
                    "IFR" => "IFR",
                    "MVFR" => "MVFR",
                    _ => "VFR"
                };

                int? wdir = null;
                if (el.TryGetProperty("wdir", out var pWdir) && pWdir.ValueKind == JsonValueKind.Number)
                    wdir = pWdir.GetInt32();

                int? wspd = null;
                if (el.TryGetProperty("wspd", out var pWspd) && pWspd.ValueKind == JsonValueKind.Number)
                    wspd = pWspd.GetInt32();

                int? wgst = null;
                if (el.TryGetProperty("wgst", out var pWgst) && pWgst.ValueKind == JsonValueKind.Number)
                    wgst = pWgst.GetInt32();

                double? temp = null;
                if (el.TryGetProperty("temp", out var pTemp) && pTemp.ValueKind == JsonValueKind.Number)
                    temp = pTemp.GetDouble();

                double? dewp = null;
                if (el.TryGetProperty("dewp", out var pDewp) && pDewp.ValueKind == JsonValueKind.Number)
                    dewp = pDewp.GetDouble();

                double? altim = null;
                if (el.TryGetProperty("altim", out var pAltim) && pAltim.ValueKind == JsonValueKind.Number)
                    altim = pAltim.GetDouble();

                double lat = el.TryGetProperty("lat", out var pLat) && pLat.ValueKind == JsonValueKind.Number ? pLat.GetDouble() : 0.0;
                double lon = el.TryGetProperty("lon", out var pLon) && pLon.ValueKind == JsonValueKind.Number ? pLon.GetDouble() : 0.0;

                DateTime reportTime = DateTime.UtcNow;
                if (el.TryGetProperty("reportTime", out var pRep) && pRep.GetString() is string repStr && DateTime.TryParse(repStr, out var parsedTime))
                    reportTime = parsedTime;

                string visibStr = "";
                if (el.TryGetProperty("visib", out var pVis))
                {
                    visibStr = pVis.ValueKind == JsonValueKind.String ? pVis.GetString() ?? "" : pVis.ToString();
                }

                return new MetarInfo
                {
                    Icao = icao,
                    RawMetar = rawMetar,
                    FlightCategory = fltCat,
                    WindDir = wdir,
                    WindSpeedKt = wspd,
                    WindGustKt = wgst,
                    TempC = temp,
                    DewpointC = dewp,
                    AltimeterHpa = altim,
                    Latitude = lat,
                    Longitude = lon,
                    ReportTime = reportTime,
                    VisibilityStr = visibStr,
                    IsNearestStation = false
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task ResolveNearestReportingStationsAsync(List<string> missingIcaos, Dictionary<string, MetarInfo> result)
        {
            try
            {
                // Obter coordenadas dos aeródromos sem METAR direto
                Dictionary<string, Domain.Entities.Airport> missingAirports;
                if (_contextFactory != null)
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    missingAirports = await context.Airports
                        .Where(a => missingIcaos.Contains(a.ICAO))
                        .ToDictionaryAsync(a => a.ICAO);
                }
                else if (_context != null)
                {
                    missingAirports = await _context.Airports
                        .Where(a => missingIcaos.Contains(a.ICAO))
                        .ToDictionaryAsync(a => a.ICAO);
                }
                else
                {
                    return;
                }

                // Estações ativas em cache que possuem coordenadas válidas
                var reportingStations = _cache.Values
                    .Select(v => v.Info)
                    .Where(m => !m.IsNearestStation && Math.Abs(m.Latitude) > 0.01 && Math.Abs(m.Longitude) > 0.01)
                    .ToList();

                if (!reportingStations.Any())
                    return;

                var now = DateTime.UtcNow;

                foreach (var icao in missingIcaos)
                {
                    if (!missingAirports.TryGetValue(icao, out var targetAirport))
                        continue;

                    // Encontra a estação reportante mais próxima
                    var closest = reportingStations
                        .Select(s => new
                        {
                            Station = s,
                            Distance = CalculateDistanceNm(targetAirport.Latitude, targetAirport.Longitude, s.Latitude, s.Longitude)
                        })
                        .OrderBy(x => x.Distance)
                        .FirstOrDefault();

                    if (closest != null)
                    {
                        var derivedMetar = new MetarInfo
                        {
                            Icao = icao,
                            RawMetar = closest.Station.RawMetar,
                            FlightCategory = closest.Station.FlightCategory,
                            WindDir = closest.Station.WindDir,
                            WindSpeedKt = closest.Station.WindSpeedKt,
                            WindGustKt = closest.Station.WindGustKt,
                            TempC = closest.Station.TempC,
                            DewpointC = closest.Station.DewpointC,
                            AltimeterHpa = closest.Station.AltimeterHpa,
                            ReportTime = closest.Station.ReportTime,
                            VisibilityStr = closest.Station.VisibilityStr,
                            Latitude = targetAirport.Latitude,
                            Longitude = targetAirport.Longitude,
                            IsNearestStation = true,
                            NearestStationIcao = closest.Station.Icao,
                            DistanceToStationNm = closest.Distance
                        };

                        _cache[icao] = (derivedMetar, now);
                        result[icao] = derivedMetar;
                    }
                }
            }
            catch
            {
                // Resiliente
            }
        }

        private static double CalculateDistanceNm(double lat1, double lon1, double lat2, double lon2)
        {
            var rlat1 = Math.PI * lat1 / 180.0;
            var rlat2 = Math.PI * lat2 / 180.0;
            var theta = lon1 - lon2;
            var rtheta = Math.PI * theta / 180.0;
            var dist = Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Cos(rtheta);
            dist = Math.Acos(Math.Min(1.0, Math.Max(-1.0, dist)));
            dist = dist * 180.0 / Math.PI;
            return dist * 60.0; // Milhas Náuticas
        }
    }
}
