using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GMap.NET;

namespace FlightCareer.UI.ViewModels
{
    public partial class FlightLogsViewModel : ObservableObject
    {
        [ObservableProperty] private FlightLogEntry? _selectedLog;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private int _totalFlightsCount;

        public ObservableCollection<FlightLogEntry> FlightLogs { get; } = new();
        public bool HasSelectedLog => SelectedLog != null;

        public FlightLogsViewModel()
        {
            _ = LoadLogsAsync();
        }

        partial void OnSelectedLogChanged(FlightLogEntry? value)
        {
            OnPropertyChanged(nameof(HasSelectedLog));
            if (value != null && !value.IsLoaded)
            {
                _ = LoadDetailedLogDataAsync(value);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            _ = LoadLogsAsync();
        }

        [RelayCommand]
        public async Task LoadLogsAsync()
        {
            try
            {
                IsLoading = true;
                FlightLogs.Clear();

                var directoriesToSearch = new HashSet<string>
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "flights"),
                    Path.Combine(Directory.GetCurrentDirectory(), "logs", "flights"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.UI", "bin", "Debug", "net10.0-windows", "logs", "flights")
                };

                var foundFiles = new List<string>();
                foreach (var dir in directoriesToSearch)
                {
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir, "flight_*.csv");
                        foundFiles.AddRange(files);
                    }
                }

                // Desduplicar arquivos pelo nome
                var uniqueFiles = foundFiles
                    .GroupBy(f => Path.GetFileName(f).ToLowerInvariant())
                    .Select(g => g.First())
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                foreach (var file in uniqueFiles)
                {
                    var entry = ParseLogHeaderAndSummary(file);
                    if (entry != null)
                    {
                        if (string.IsNullOrWhiteSpace(SearchText) ||
                            entry.OriginICAO.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            entry.DestinationICAO.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            entry.Aircraft.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            entry.Status.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                        {
                            FlightLogs.Add(entry);
                        }
                    }
                }

                TotalFlightsCount = FlightLogs.Count;

                // Selecionar o primeiro automaticamente se nenhum estiver selecionado
                if (SelectedLog == null && FlightLogs.Any())
                {
                    SelectedLog = FlightLogs.First();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FlightLogsViewModel] Erro ao carregar logs: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private FlightLogEntry? ParseLogHeaderAndSummary(string filePath)
        {
            try
            {
                var fi = new FileInfo(filePath);
                if (!fi.Exists || fi.Length == 0) return null;

                string fileName = fi.Name;
                // Exemplo: flight_20261108_091214_SDTB_SDTB_PT-GNW.csv
                string[] parts = fileName.Replace(".csv", "").Split('_');

                DateTime fileDate = fi.LastWriteTime;
                string origin = "----";
                string dest = "----";
                string acft = "---";

                if (parts.Length >= 5)
                {
                    if (DateTime.TryParseExact(parts[1] + parts[2], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        fileDate = parsedDate;
                    }
                    origin = parts[3].ToUpper();
                    dest = parts[4].ToUpper();
                    if (parts.Length >= 6) acft = parts[5].ToUpper();
                }

                var entry = new FlightLogEntry
                {
                    FilePath = filePath,
                    FileName = fileName,
                    Date = fileDate,
                    OriginICAO = origin,
                    DestinationICAO = dest,
                    Aircraft = acft,
                    DurationMinutes = Math.Round((fi.LastWriteTime - fileDate).TotalMinutes, 1)
                };

                // Ler as últimas linhas para pegar o resumo
                var lastLines = ReadTailLines(filePath, 12);
                foreach (var line in lastLines)
                {
                    if (line.StartsWith("# Status Final:"))
                        entry.Status = line.Replace("# Status Final:", "").Trim();
                    else if (line.StartsWith("# Duração Total:"))
                    {
                        var s = line.Replace("# Duração Total:", "").Replace("min", "").Trim();
                        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                            entry.DurationMinutes = d;
                    }
                    else if (line.StartsWith("# Touchdown FPM:"))
                    {
                        var s = line.Replace("# Touchdown FPM:", "").Replace("fpm", "").Trim();
                        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                            entry.TouchdownFpm = d;
                    }
                    else if (line.StartsWith("# Score de Qualidade:"))
                    {
                        var s = line.Replace("# Score de Qualidade:", "").Replace("%", "").Trim();
                        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                            entry.QualityScore = d;
                    }
                    else if (line.StartsWith("# G-Force Range:"))
                    {
                        var s = line.Replace("# G-Force Range:", "").Trim();
                        var gParts = s.Split(new[] { "a" }, StringSplitOptions.RemoveEmptyEntries);
                        if (gParts.Length >= 2)
                        {
                            if (double.TryParse(gParts[0].Replace("G", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var gMin))
                                entry.MinGForce = gMin;
                            if (double.TryParse(gParts[1].Replace("G", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var gMax))
                                entry.MaxGForce = gMax;
                        }
                    }
                }

                return entry;
            }
            catch
            {
                return null;
            }
        }

        private async Task LoadDetailedLogDataAsync(FlightLogEntry entry)
        {
            try
            {
                if (!File.Exists(entry.FilePath)) return;

                var lines = await File.ReadAllLinesAsync(entry.FilePath);
                if (lines.Length <= 1) return;

                double maxAlt = 0;
                double maxGs = 0;
                double firstFuel = -1;
                double lastFuel = -1;
                var routePoints = new List<PointLatLng>();
                var telemetryRecords = new List<FlightTelemetryRecord>();

                var inv = CultureInfo.InvariantCulture;

                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string[] cols = line.Split(';');
                    if (cols.Length < 16) continue;

                    // 0: Timestamp, 1: ElapsedSec, 2: Origin, 3: Dest, 4: Lat, 5: Lon, 6: Alt, 7: GS, 8: IAS, 9: VS, 10: G, 11: Pitch, 12: Bank, 13: Heading, 14: OnGround, 15: PB, ... 18: RPM, 24: Fuel
                    double.TryParse(cols[1], NumberStyles.Any, inv, out var elapsed);
                    double.TryParse(cols[4], NumberStyles.Any, inv, out var lat);
                    double.TryParse(cols[5], NumberStyles.Any, inv, out var lon);
                    double.TryParse(cols[6], NumberStyles.Any, inv, out var alt);
                    double.TryParse(cols[7], NumberStyles.Any, inv, out var gs);
                    double.TryParse(cols[8], NumberStyles.Any, inv, out var ias);
                    double.TryParse(cols[9], NumberStyles.Any, inv, out var vs);
                    double.TryParse(cols[10], NumberStyles.Any, inv, out var g);
                    double.TryParse(cols[11], NumberStyles.Any, inv, out var pitch);
                    double.TryParse(cols[12], NumberStyles.Any, inv, out var bank);
                    double.TryParse(cols[13], NumberStyles.Any, inv, out var heading);
                    bool onGround = cols[14].Trim() == "1";

                    double rpm = 0;
                    if (cols.Length > 18) double.TryParse(cols[18], NumberStyles.Any, inv, out rpm);

                    double fuel = 0;
                    if (cols.Length > 24) double.TryParse(cols[24], NumberStyles.Any, inv, out fuel);

                    string status = cols.Length > 31 ? cols[31] : string.Empty;

                    if (alt > maxAlt) maxAlt = alt;
                    if (gs > maxGs) maxGs = gs;

                    if (fuel > 0)
                    {
                        if (firstFuel < 0) firstFuel = fuel;
                        lastFuel = fuel;
                    }

                    // Gravar pontos para o mapa (evitar coordenadas 0,0)
                    if (lat != 0 && lon != 0)
                    {
                        var pt = new PointLatLng(lat, lon);
                        if (routePoints.Count == 0 ||
                            Math.Abs(routePoints.Last().Lat - lat) > 0.0001 ||
                            Math.Abs(routePoints.Last().Lng - lon) > 0.0001)
                        {
                            routePoints.Add(pt);
                        }
                    }

                    telemetryRecords.Add(new FlightTelemetryRecord
                    {
                        Timestamp = cols[0],
                        ElapsedSec = elapsed,
                        Origin = cols[2],
                        Dest = cols[3],
                        Latitude = lat,
                        Longitude = lon,
                        AltitudeFt = alt,
                        GroundSpeedKt = gs,
                        AirspeedKt = ias,
                        VerticalSpeedFpm = vs,
                        GForce = g,
                        PitchDeg = pitch,
                        BankDeg = bank,
                        HeadingDeg = heading,
                        IsOnGround = onGround,
                        EngineRPM = rpm,
                        FuelGallons = fuel,
                        Status = status
                    });
                }

                var obsPoints = new ObservableCollection<PointLatLng>(routePoints);

                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    entry.TelemetryRecords.Clear();
                    foreach (var rec in telemetryRecords)
                    {
                        entry.TelemetryRecords.Add(rec);
                    }

                    entry.MapRoutePoints = obsPoints;
                    entry.MaxAltitudeFt = maxAlt;
                    entry.MaxGroundSpeedKt = maxGs;
                    if (firstFuel > 0 && lastFuel > 0)
                    {
                        entry.FuelUsedGallons = Math.Max(0, firstFuel - lastFuel);
                    }

                    entry.TotalFrames = entry.TelemetryRecords.Count;
                    entry.IsLoaded = true;

                    OnPropertyChanged(nameof(SelectedLog));
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FlightLogsViewModel] Erro ao ler detalhes do log: {ex.Message}");
            }
        }

        private static List<string> ReadTailLines(string filePath, int count)
        {
            var lines = new List<string>();
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                string? line;
                var buffer = new Queue<string>(count);
                while ((line = reader.ReadLine()) != null)
                {
                    if (buffer.Count >= count) buffer.Dequeue();
                    buffer.Enqueue(line);
                }
                lines.AddRange(buffer);
            }
            catch { }
            return lines;
        }

        [RelayCommand]
        private void OpenLogsFolder()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "flights");
                if (!Directory.Exists(dir))
                {
                    dir = Path.Combine(Directory.GetCurrentDirectory(), "logs", "flights");
                }
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir a pasta de logs:\n{ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenCsvFile()
        {
            if (SelectedLog == null || !File.Exists(SelectedLog.FilePath)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SelectedLog.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir o arquivo CSV:\n{ex.Message}");
            }
        }
    }
}
