using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace FlightCareer.UI.Services
{
    public sealed class WindowPositionItem
    {
        [JsonPropertyName("left")]
        public double Left { get; set; }

        [JsonPropertyName("top")]
        public double Top { get; set; }
    }

    public sealed class WindowPositionStore
    {
        private static readonly Lazy<WindowPositionStore> _shared = new(() => new WindowPositionStore());
        public static WindowPositionStore Shared => _shared.Value;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _filePath;
        private readonly object _lock = new();
        private Dictionary<string, WindowPositionItem>? _cache;

        public WindowPositionStore(string? filePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _filePath = Path.Combine(localAppData, "FlightCareer", "window-positions.json");
            }
            else
            {
                _filePath = filePath;
            }
        }

        private void EnsureLoadedLocked()
        {
            if (_cache != null) return;

            _cache = new Dictionary<string, WindowPositionItem>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(_filePath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                var items = JsonSerializer.Deserialize<Dictionary<string, WindowPositionItem>>(json, JsonOptions);
                if (items != null)
                {
                    foreach (var kvp in items)
                    {
                        _cache[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowPositionStore] Falha ao carregar posições: {ex.Message}");
            }
        }

        public bool TryGet(string key, out Point pos)
        {
            lock (_lock)
            {
                EnsureLoadedLocked();
                if (_cache != null && _cache.TryGetValue(key, out var item) &&
                    !double.IsNaN(item.Left) && !double.IsInfinity(item.Left) &&
                    !double.IsNaN(item.Top) && !double.IsInfinity(item.Top))
                {
                    pos = new Point(item.Left, item.Top);
                    return true;
                }

                pos = default;
                return false;
            }
        }

        public void Save(string key, double left, double top)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) ||
                double.IsNaN(top) || double.IsInfinity(top))
            {
                return;
            }

            lock (_lock)
            {
                EnsureLoadedLocked();
                _cache![key] = new WindowPositionItem
                {
                    Left = Math.Round(left, 1),
                    Top = Math.Round(top, 1)
                };

                try
                {
                    string? dir = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string tempPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    string json = JsonSerializer.Serialize(_cache, JsonOptions);
                    File.WriteAllText(tempPath, json);
                    File.Move(tempPath, _filePath, overwrite: true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WindowPositionStore] Falha ao gravar posições: {ex.Message}");
                }
            }
        }
    }
}
