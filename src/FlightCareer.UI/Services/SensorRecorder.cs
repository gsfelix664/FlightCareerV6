using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Gravador de validação de sensores do MSFS.
    /// Exporta leituras em tempo real em formato CSV estruturado e metadados de contexto em JSON.
    /// Suporta rotação automática de arquivos quando o layout de sensores é reconstruído.
    /// </summary>
    public sealed class SensorRecorder : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly SensorFrameReader _frameReader;
        private readonly ContextSnapshotReader _contextReader;
        private readonly object _fileLock = new();

        private StreamWriter? _writer;
        private SensorLayout? _activeLayout;
        private IReadOnlyList<string> _activeKeys = Array.Empty<string>();
        private ContextSnapshot? _currentSnapshot;

        private string? _baseDirectory;
        private string? _baseFilePrefix;
        private int _fileRotationIndex = 1;

        private string? _pendingMarker;
        private DateTime _lastFlushUtc = DateTime.UtcNow;

        private bool _isRecording;
        private string? _currentFilePath;
        private long _rowsWritten;
        private string? _lastError;
        private bool _disposed;

        public bool IsRecording
        {
            get { lock (_fileLock) return _isRecording; }
            private set { lock (_fileLock) _isRecording = value; }
        }

        public string? CurrentFilePath
        {
            get { lock (_fileLock) return _currentFilePath; }
            private set { lock (_fileLock) _currentFilePath = value; }
        }

        public long RowsWritten
        {
            get { lock (_fileLock) return _rowsWritten; }
            private set { lock (_fileLock) _rowsWritten = value; }
        }

        public string? LastError
        {
            get { lock (_fileLock) return _lastError; }
            private set { lock (_fileLock) _lastError = value; }
        }

        public SensorRecorder(SensorFrameReader frameReader, ContextSnapshotReader contextReader)
        {
            _frameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
            _contextReader = contextReader ?? throw new ArgumentNullException(nameof(contextReader));
        }

        /// <summary>
        /// Inicia a gravação de telemetria em CSV na pasta %LocalAppData%/FlightCareer/sensor-logs/.
        /// Lê o snapshot de contexto assincronamente e inicia o arquivo.
        /// </summary>
        public async Task StartAsync()
        {
            if (_disposed) return;

            lock (_fileLock)
            {
                if (_isRecording)
                {
                    Debug.WriteLine("[SensorRecorder] Gravação já está ativa.");
                    return;
                }

                _lastError = null;
                _rowsWritten = 0;
                _fileRotationIndex = 1;
            }

            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightCareer", "sensor-logs");
                Directory.CreateDirectory(folder);
                _baseDirectory = folder;

                // Solicita contexto (timeout de 5 s embutido no reader)
                ContextSnapshot? snapshot = null;
                try
                {
                    snapshot = await _contextReader.ReadSnapshotAsync(5000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SensorRecorder] Falha ao obter snapshot de contexto: {ex.Message}");
                }

                _currentSnapshot = snapshot;

                string rawTitle = "UNKNOWN";
                if (snapshot?.Strings != null && snapshot.Strings.TryGetValue("ctx-title", out var titleVal) && !string.IsNullOrWhiteSpace(titleVal))
                {
                    rawTitle = titleVal;
                }

                string safeTitle = SensorCsvFormatter.SafeFileName(rawTitle);
                string timeStamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
                _baseFilePrefix = $"{timeStamp}-{safeTitle}";

                lock (_fileLock)
                {
                    OpenNewLogFile(isInitial: true);
                    _isRecording = true;
                }

                _frameReader.OnFrame += HandleOnFrame;
                Debug.WriteLine($"[SensorRecorder] Gravação iniciada: {CurrentFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SensorRecorder] Erro fatal ao iniciar gravação: {ex.Message}");
                LastError = ex.Message;
                Stop();
            }
        }

        public void Start() => _ = StartAsync();

        /// <summary>
        /// Envia um marcador textual para ser inserido na coluna 'marker' do próximo frame gravado.
        /// </summary>
        public void Mark(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Interlocked.Exchange(ref _pendingMarker, text);
            Debug.WriteLine($"[SensorRecorder] Marcador registrado para o próximo frame: '{text}'");
        }

        /// <summary>
        /// Interrompe a gravação atual e fecha os arquivos abertos.
        /// </summary>
        public void Stop()
        {
            _frameReader.OnFrame -= HandleOnFrame;

            lock (_fileLock)
            {
                if (!_isRecording && _writer == null) return;
                _isRecording = false;

                if (_writer != null)
                {
                    try
                    {
                        _writer.Flush();
                        _writer.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SensorRecorder] Erro ao fechar StreamWriter: {ex.Message}");
                        LastError = ex.Message;
                    }
                    finally
                    {
                        _writer = null;
                    }
                }

                Debug.WriteLine($"[SensorRecorder] Gravação finalizada. Total de linhas: {RowsWritten}");
            }
        }

        private void HandleOnFrame(SensorFrame frame)
        {
            try
            {
                lock (_fileLock)
                {
                    if (!_isRecording || _writer == null) return;

                    // Se o layout do leitor mudou (ex.: aeronave recarregada ou SimVar rejeitada), rotaciona o arquivo
                    if (_frameReader.CurrentLayout != null && _frameReader.CurrentLayout != _activeLayout)
                    {
                        Debug.WriteLine("[SensorRecorder] Mudança de layout detectada durante gravação. Rotacionando arquivo...");
                        _fileRotationIndex++;
                        OpenNewLogFile(isInitial: false);
                    }

                    string? marker = Interlocked.Exchange(ref _pendingMarker, null);
                    string row = SensorCsvFormatter.Row(frame.ReceivedUtc, frame, _activeKeys, marker);

                    _writer.WriteLine(row);
                    _rowsWritten++;

                    if (_rowsWritten % 10 == 0 || (DateTime.UtcNow - _lastFlushUtc).TotalSeconds >= 5)
                    {
                        _writer.Flush();
                        _lastFlushUtc = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SensorRecorder] Erro de I/O ao gravar linha no log: {ex.Message}");
                LastError = ex.Message;
            }
        }

        private void OpenNewLogFile(bool isInitial)
        {
            if (_writer != null)
            {
                try
                {
                    _writer.Flush();
                    _writer.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SensorRecorder] Erro ao fechar arquivo anterior: {ex.Message}");
                }
                _writer = null;
            }

            var layout = _frameReader.CurrentLayout;
            if (layout == null)
            {
                try
                {
                    var reg = SensorRegistry.LoadDefault("sensor_registry.json");
                    layout = SensorLayout.Build(reg.Sensors, Math.Max(1, _frameReader.CurrentEngineCount));
                }
                catch
                {
                    layout = new SensorLayout(Array.Empty<SensorBlock>());
                }
            }

            _activeLayout = layout;
            _activeKeys = layout.Blocks
                .SelectMany(b => b.Slots)
                .Where(s => !string.Equals(s.Key, SensorLayout.SimulationRateKey, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Key)
                .ToList();

            string suffix = _fileRotationIndex == 1 ? string.Empty : $"-{_fileRotationIndex}";
            string csvFileName = $"{_baseFilePrefix}{suffix}.csv";
            string contextFileName = $"{_baseFilePrefix}{suffix}-context.json";

            string csvPath = Path.Combine(_baseDirectory!, csvFileName);
            string contextPath = Path.Combine(_baseDirectory!, contextFileName);

            // Garante que não sobrescreva arquivo preexistente se já houver um com o mesmo nome
            int disambiguation = 2;
            while (File.Exists(csvPath))
            {
                suffix = $"-{_fileRotationIndex}_{disambiguation++}";
                csvFileName = $"{_baseFilePrefix}{suffix}.csv";
                contextFileName = $"{_baseFilePrefix}{suffix}-context.json";
                csvPath = Path.Combine(_baseDirectory!, csvFileName);
                contextPath = Path.Combine(_baseDirectory!, contextFileName);
            }

            // Grava o arquivo de contexto JSON
            try
            {
                var metadata = new
                {
                    CreatedUtc = DateTime.UtcNow,
                    Title = _currentSnapshot?.Strings?.GetValueOrDefault("ctx-title") ?? "UNKNOWN",
                    EngineCount = _frameReader.CurrentEngineCount,
                    SlotCount = _activeKeys.Count,
                    RejectedSimVars = _frameReader.RejectedSimVars,
                    Strings = _currentSnapshot?.Strings ?? (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(),
                    Numbers = _currentSnapshot?.Numbers ?? (IReadOnlyDictionary<string, double>)new Dictionary<string, double>()
                };

                string jsonContent = JsonSerializer.Serialize(metadata, JsonOptions);
                File.WriteAllText(contextPath, jsonContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SensorRecorder] Erro ao gravar context.json: {ex.Message}");
            }

            // Abre o StreamWriter para o CSV
            var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream, System.Text.Encoding.UTF8);

            string header = SensorCsvFormatter.Header(_activeKeys);
            _writer.WriteLine(header);
            _writer.Flush();

            CurrentFilePath = csvPath;
            _lastFlushUtc = DateTime.UtcNow;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
