using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Utils;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.UI.Services
{
    public class FlightMonitor
    {
        private readonly SimConnectService _simConnect;
        private readonly FlightService _flightService;
        private readonly IDbContextFactory<Infrastructure.Persistence.AppDbContext>? _contextFactory;
        private readonly Infrastructure.Persistence.AppDbContext? _fallbackContext;
        private readonly WearAndFailureService _wearService;
        private readonly FlightQualityService _qualityService = new();
        private readonly FlightTelemetryLogger _telemetryLogger = new();
        private string _currentStatus = "AGUARDANDO PREPARAÇÃO (ENGINES/PB)";

        public bool IsMonitoring { get; private set; }
        public bool IsFlightActive { get; private set; }
        public string CurrentStatus => _currentStatus;
        public PilotAircraft? CurrentAircraft => _aircraft;
        public IReadOnlyList<Mission> ActiveMissions => _activeMissions;
        private bool _isAirborne;
        private bool _hasTouchedDown;
        private DateTime? _startConditionTime;

        private PilotAircraft? _aircraft;
        private Pilot? _pilot;
        private List<Mission> _activeMissions = new();

        private DateTime _startTime;
        private double _initialFuel;
        private double _distanceTraveled;
        private double _lastLat;
        private double _lastLon;
        private string _originIcao = string.Empty;
        private DateTime _lastDatabaseUpdate = DateTime.MinValue;
        private DateTime _lastWearUpdate = DateTime.MinValue;
        private DateTime? _groundAbortTime;
        private SimTelemetry? _lastData;

        public event Action<string, string, bool>? OnAlert;
        public event Action<string>? OnStatusChanged;
        public event Action<FlightOutcome>? OnFlightFinished;
        public event Action<PilotAircraft>? OnTotalLoss;
        public event Action? OnMonitoringCancelled;

        public WearAndFailureService WearService => _wearService;

        public FlightMonitor(
            SimConnectService simConnect, 
            FlightService flightService, 
            IDbContextFactory<Infrastructure.Persistence.AppDbContext> contextFactory)
        {
            _simConnect = simConnect;
            _flightService = flightService;
            _contextFactory = contextFactory;
            _wearService = new WearAndFailureService(simConnect);
            _wearService.OnFailureTriggered += OnAircraftFailureTriggered;
        }

        public FlightMonitor(
            SimConnectService simConnect, 
            FlightService flightService, 
            Infrastructure.Persistence.AppDbContext context)
        {
            _simConnect = simConnect;
            _flightService = flightService;
            _fallbackContext = context;
            _wearService = new WearAndFailureService(simConnect);
            _wearService.OnFailureTriggered += OnAircraftFailureTriggered;
        }

        private async Task<Infrastructure.Persistence.AppDbContext> CreateDbContextAsync()
        {
            if (_contextFactory != null)
            {
                return await _contextFactory.CreateDbContextAsync();
            }
            return _fallbackContext!;
        }

        private void OnAircraftFailureTriggered(FailureRecord failure)
        {
            OnAlert?.Invoke(failure.Title, failure.Description, failure.IsCritical);
            _telemetryLogger.LogEvent("PANE_EM_VOO", $"{failure.Title} - {failure.Description}");
        }

        public void StartMonitoring(Pilot pilot, PilotAircraft aircraft, List<Mission> missions)
        {
            if (IsMonitoring) return;

            if (!_simConnect.IsConnected)
            {
                OnAlert?.Invoke("Erro de Conexão", "O Simulador não está conectado!", true);
                return;
            }

            if (aircraft.IsInterdicted)
            {
                OnAlert?.Invoke("AERONAVE INTERDITADA", $"Aeronave impedida de voar! Motivo: {aircraft.InterdictionReason}. Efetue a manutenção obrigatória no Hangar.", true);
                return;
            }

            var prohibitedMission = missions.FirstOrDefault(m => m.RequiredCategory > pilot.PilotCategory);
            if (prohibitedMission != null)
            {
                OnAlert?.Invoke("OPERAÇÃO NÃO HOMOLOGADA", $"A missão '{prohibitedMission.Title}' exige homologação {prohibitedMission.RequiredCategory.GetDisplayName()}, mas sua empresa está operando em {pilot.PilotCategory.GetDisplayName()}!", true);
                return;
            }

            _pilot = pilot;
            _aircraft = aircraft;
            _activeMissions = missions;
            _originIcao = pilot.CurrentAirportICAO;

            _simConnect.ResetCrashState();
            _wearService.Reset();
            _startConditionTime = null;
            IsFlightActive = false;
            _isAirborne = false;
            _hasTouchedDown = false;

            _simConnect.OnDataReceived += OnSimDataReceived;
            IsMonitoring = true;

            OnAlert?.Invoke("SISTEMA INICIALIZADO", "O monitoramento começará automaticamente 10 segundos após você ligar os motores e soltar o freio de estacionamento.", false);
            OnStatusChanged?.Invoke("AGUARDANDO PREPARAÇÃO (ENGINES/PB)");
        }

        public void StopMonitoring()
        {
            if (!IsMonitoring) return;
            _simConnect.OnDataReceived -= OnSimDataReceived;
            _simConnect.ResetCrashState();
            _wearService.Reset();
            IsMonitoring = false;
            IsFlightActive = false;
            _startConditionTime = null;
            OnStatusChanged?.Invoke("MONITORAMENTO PARADO");
        }

        public async Task CancelMonitoringAsync(SimTelemetry? lastData)
        {
            if (!IsMonitoring) return;

            // Se o voo já estava ativo (motores ligados/PB solto), salvamos o estado atual
            if (IsFlightActive && lastData != null && _pilot != null && _aircraft != null)
            {
                _telemetryLogger.StopFlightLog("CANCELADO_PELO_PILOTO", 0, _qualityService.GetQualityScore());
                OnStatusChanged?.Invoke("CANCELANDO E SALVANDO ESTADO...");
                
                try
                {
                    await using var context = await CreateDbContextAsync();
                    // 1. Atualizar Desgaste e Combustível no Hangar
                    var dbAircraft = await context.PilotAircrafts.FindAsync(_aircraft.Id);
                    if (dbAircraft != null)
                    {
                        dbAircraft.FuelAmount = lastData.Value.FuelTotalGallons;
                        dbAircraft.EngineCondition = _aircraft.EngineCondition;
                        dbAircraft.AirframeCondition = _aircraft.AirframeCondition;
                        dbAircraft.TiresCondition = _aircraft.TiresCondition;
                        dbAircraft.LandingsCount = _aircraft.LandingsCount;
                        
                        // 2. Tentar localizar aeroporto mais próximo para salvar nova posição caso tenha se movido
                        string currentIcao = await GetNearestAirportICAO(lastData.Value.Latitude, lastData.Value.Longitude);
                        if (!string.IsNullOrEmpty(currentIcao))
                        {
                            dbAircraft.CurrentLocationICAO = currentIcao;
                            var dbPilot = await context.Pilots.FindAsync(_pilot.Id);
                            if (dbPilot != null) dbPilot.CurrentAirportICAO = currentIcao;

                            // ATUALIZAR MISSÕES: A carga parou aqui
                            foreach (var mission in _activeMissions)
                            {
                                mission.OriginICAO = currentIcao; // Atualiza em memória
                                var dbMission = await context.Missions.FindAsync(mission.Id);
                                if (dbMission != null)
                                {
                                    dbMission.OriginICAO = currentIcao;
                                    
                                    // Recalcular distância restante
                                    var destAirp = await context.Airports.FirstOrDefaultAsync(a => a.ICAO == dbMission.DestinationICAO);
                                    var curAirp = await context.Airports.FirstOrDefaultAsync(a => a.ICAO == currentIcao);
                                    if (destAirp != null && curAirp != null)
                                    {
                                        double newDist = LocationUtils.CalculateDistance(curAirp.Latitude, curAirp.Longitude, destAirp.Latitude, destAirp.Longitude);
                                        dbMission.DistanceNM = Math.Round(newDist, 0);
                                        mission.DistanceNM = dbMission.DistanceNM; // Atualiza em memória
                                    }
                                }
                            }
                        }
                        
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving state: {ex.Message}");
                }
            }

            StopMonitoring();
            OnStatusChanged?.Invoke("MONITORAMENTO CANCELADO");
            OnMonitoringCancelled?.Invoke();
        }

        private async Task HandleTotalLossAsync()
        {
            if (_aircraft == null) return;
            
            double fpm = _qualityService.TouchdownFPM > 0 ? _qualityService.TouchdownFPM : (_lastData != null ? Math.Abs(_lastData.Value.VerticalSpeed) : 0);
            _telemetryLogger.StopFlightLog("PERDA_TOTAL_DESTRUIDA", Math.Round(fpm, 0), 0);
            StopMonitoring();
            
            // Salvar estado de 0% no banco para garantir que a aeronave está "morta"
            try
            {
                await using var context = await CreateDbContextAsync();
                var dbAircraft = await context.PilotAircrafts.FindAsync(_aircraft.Id);
                if (dbAircraft != null)
                {
                    dbAircraft.EngineCondition = 0;
                    dbAircraft.TiresCondition = 0;
                    dbAircraft.AirframeCondition = 0;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving total loss: {ex.Message}");
            }

            OnTotalLoss?.Invoke(_aircraft);
        }

        private async Task<string> GetNearestAirportICAO(double lat, double lon)
        {
            try
            {
                double range = 0.5;
                await using var context = await CreateDbContextAsync();
                var nearbyAirports = await context.Airports.Where(a => 
                        a.Latitude > lat - range && a.Latitude < lat + range &&
                        a.Longitude > lon - range && a.Longitude < lon + range)
                    .ToListAsync();

                var nearest = nearbyAirports
                    .Select(a => new { Airport = a, Distance = LocationUtils.CalculateDistance(lat, lon, a.Latitude, a.Longitude) })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                return nearest?.Airport.ICAO ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        private void OnSimDataReceived(object? sender, SimTelemetry data)
        {
            if (!IsMonitoring) return;
            _lastData = data;

            // Lógica de Início de Gravação: Quando ligar motores E soltar PB por 10 segundos
            if (!IsFlightActive)
            {
                if (data.IsEngineRunning && data.ParkingBrake < 0.5)
                {
                    if (_startConditionTime == null) 
                    {
                        _startConditionTime = DateTime.Now;
                        OnStatusChanged?.Invoke("CONDIÇÕES OK - INICIANDO EM 10s");
                    }
                    
                    if ((DateTime.Now - _startConditionTime.Value).TotalSeconds >= 10)
                    {
                        ActivateFlight(data);
                    }
                }
                else
                {
                    if (_startConditionTime != null)
                    {
                        _startConditionTime = null;
                        OnStatusChanged?.Invoke("AGUARDANDO PREPARAÇÃO (ENGINES/PB)");
                    }
                }
                return;
            }

            // --- MONITORAMENTO DE QUALIDADE ---
            bool hasPax = _activeMissions.Any(m => m.PassengerCount > 0);
            _qualityService.ProcessFrame(data, hasPax);

            // Durante o Voo: Atualizar combustível, localização e estado no Hangar a cada 60 segundos (1 min)
            if ((DateTime.Now - _lastDatabaseUpdate).TotalSeconds >= 60)
            {
                _ = UpdateHangarStateAsync(data); // Chama de forma assíncrona mas segura
                _lastDatabaseUpdate = DateTime.Now;
            }

            // Processar Desgaste e Falhas a cada 1 segundo
            if (_aircraft != null)
            {
                double deltaHours = (DateTime.Now - (_lastWearUpdate == DateTime.MinValue ? DateTime.Now : _lastWearUpdate)).TotalHours;
                _wearService.ProcessWear(_aircraft, data, deltaHours);

                // Checar Perda Total
                if (IsFlightActive && _aircraft.EngineCondition <= 0 && _aircraft.TiresCondition <= 0 && _aircraft.AirframeCondition <= 0)
                {
                    _ = HandleTotalLossAsync();
                    return;
                }
            }
            _lastWearUpdate = DateTime.Now;

            double delta = LocationUtils.CalculateDistance(_lastLat, _lastLon, data.Latitude, data.Longitude);
            if (delta > 0.0005 && delta < 50) _distanceTraveled += delta;

            _lastLat = data.Latitude;
            _lastLon = data.Longitude;

            if (!_isAirborne && !data.IsOnGround && data.GroundSpeed > 30)
            {
                _isAirborne = true;
                _hasTouchedDown = false;
                _currentStatus = "VOANDO (NO AR)";
                OnStatusChanged?.Invoke(_currentStatus);
            }

            // --- DETECÇÃO DE TOUCHDOWN REAL EM VOO ---
            if (_isAirborne && data.IsOnGround)
            {
                double fpmBody = Math.Abs(data.VelocityBodyY * 60.0);
                double fpmVs = Math.Abs(data.VerticalSpeed);
                double touchdownRate = fpmBody > 10 ? fpmBody : fpmVs;

                if (!_hasTouchedDown)
                {
                    _hasTouchedDown = true;
                    _qualityService.RecordTouchdownFpm(touchdownRate);
                    if (_aircraft != null) _wearService.ProcessLanding(_aircraft, _qualityService.TouchdownFPM, data.VelocityBodyX);

                    _currentStatus = $"POUSO REALIZADO ({_qualityService.TouchdownFPM:F0} FPM) - TAXIANDO";
                    OnStatusChanged?.Invoke(_currentStatus);
                }
                else
                {
                    // Bounce detection: se o impacto subsequente for mais forte
                    if (touchdownRate > _qualityService.TouchdownFPM)
                    {
                        _qualityService.RecordTouchdownFpm(touchdownRate);
                        if (_aircraft != null) _wearService.ProcessLanding(_aircraft, touchdownRate, data.VelocityBodyX);
                        _currentStatus = $"POUSO REALIZADO ({_qualityService.TouchdownFPM:F0} FPM - BOUNCE) - TAXIANDO";
                        OnStatusChanged?.Invoke(_currentStatus);
                    }
                }
            }

            // Gravar Blackbox de Telemetria Contínua
            string destIcao = _activeMissions.FirstOrDefault()?.DestinationICAO ?? "DEST";
            _telemetryLogger.LogFrame(data, _aircraft, _originIcao, destIcao, _currentStatus);

            // Lógica de Finalização Inteligente: OnGround + Motores Desligados + (PB ou Parada Total GS < 1.0) por 5 segundos
            bool isParkedOrStopped = data.ParkingBrake > 0.5 || data.GroundSpeed < 1.0;
            if (_isAirborne && data.IsOnGround && !data.IsEngineRunning && isParkedOrStopped)
            {
                if (_startConditionTime == null)
                {
                    _startConditionTime = DateTime.Now;
                    _currentStatus = "POUSO E CORTE DE MOTORES DETECTADOS (ENCERRANDO EM 5s)...";
                    OnStatusChanged?.Invoke(_currentStatus);
                }
                
                if ((DateTime.Now - _startConditionTime.Value).TotalSeconds >= 5)
                {
                    if (!_hasTouchedDown)
                    {
                        double fpm = Math.Abs(data.VelocityBodyY * 60.0);
                        _qualityService.RecordTouchdownFpm(fpm > 10 ? fpm : Math.Abs(data.VerticalSpeed));
                        if (_aircraft != null) _wearService.ProcessLanding(_aircraft, _qualityService.TouchdownFPM, data.VelocityBodyX);
                    }
                    _ = FinalizeFlightAsync(data);
                }
            }
            else if (!_isAirborne && data.IsOnGround && !data.IsEngineRunning && isParkedOrStopped)
            {
                // Piloto iniciou o voo no app, mas cortou motores no pátio sem decolar (Cancelamento em solo)
                if (_groundAbortTime == null)
                {
                    _groundAbortTime = DateTime.Now;
                    _currentStatus = "CORTE DE MOTORES EM SOLO DETECTADO (CANCELANDO EM 5s)...";
                    OnStatusChanged?.Invoke(_currentStatus);
                }

                if ((DateTime.Now - _groundAbortTime.Value).TotalSeconds >= 5)
                {
                    _currentStatus = "VOO CANCELADO EM SOLO";
                    OnStatusChanged?.Invoke(_currentStatus);
                    _ = CancelMonitoringAsync(data);
                }
            }
            else
            {
                if (_startConditionTime != null && !isParkedOrStopped)
                {
                    _startConditionTime = null;
                }
                if (_groundAbortTime != null && (data.IsEngineRunning || !isParkedOrStopped))
                {
                    _groundAbortTime = null;
                }
            }
        }

        private async Task UpdateHangarStateAsync(SimTelemetry data)
        {
            if (_aircraft == null || _pilot == null) return;
            
            try 
            {
                await using var context = await CreateDbContextAsync();
                // Buscar novamente para evitar problemas de concorrência/contexto
                var dbAircraft = await context.PilotAircrafts.FindAsync(_aircraft.Id);
                var dbPilot = await context.Pilots.FindAsync(_pilot.Id);

                if (dbAircraft != null)
                {
                    dbAircraft.FuelAmount = data.FuelTotalGallons;
                    dbAircraft.EngineCondition = _aircraft.EngineCondition;
                    dbAircraft.AirframeCondition = _aircraft.AirframeCondition;
                    dbAircraft.TiresCondition = _aircraft.TiresCondition;
                    dbAircraft.LandingsCount = _aircraft.LandingsCount;

                    // Atualizar também o objeto local para garantir sincronia
                    _aircraft.FuelAmount = dbAircraft.FuelAmount;
                    _aircraft.CurrentLocationICAO = dbAircraft.CurrentLocationICAO;

                    // Tentar localizar aeroporto mais próximo para salvar posição em tempo real
                    string currentIcao = await GetNearestAirportICAO(data.Latitude, data.Longitude);
                    if (!string.IsNullOrEmpty(currentIcao))
                    {
                        dbAircraft.CurrentLocationICAO = currentIcao;
                        if (dbPilot != null) dbPilot.CurrentAirportICAO = currentIcao;

                        // ATUALIZAR MISSÕES: A carga está se movendo com o avião
                        foreach (var mission in _activeMissions)
                        {
                            mission.OriginICAO = currentIcao; // Atualiza em memória
                            var dbMission = await context.Missions.FindAsync(mission.Id);
                            if (dbMission != null)
                            {
                                dbMission.OriginICAO = currentIcao;
                                
                                // Atualizar distância restante
                                var destAirp = await context.Airports.FirstOrDefaultAsync(a => a.ICAO == dbMission.DestinationICAO);
                                var curAirp = await context.Airports.FirstOrDefaultAsync(a => a.ICAO == currentIcao);
                                if (destAirp != null && curAirp != null)
                                {
                                    double newDist = LocationUtils.CalculateDistance(curAirp.Latitude, curAirp.Longitude, destAirp.Latitude, destAirp.Longitude);
                                    dbMission.DistanceNM = Math.Round(newDist, 0);
                                    mission.DistanceNM = dbMission.DistanceNM; // Atualiza em memória
                                }
                            }
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in background save: {ex.Message}");
            }
        }

        private void UpdateHangarState(SimTelemetry data)
        {
            _ = UpdateHangarStateAsync(data);
        }

        private void ActivateFlight(SimTelemetry data)
        {
            IsFlightActive = true;
            _isAirborne = false;
            _hasTouchedDown = false;
            _simConnect.ResetCrashState();
            _wearService.Reset();
            _qualityService.Reset();
            _startTime = DateTime.Now;
            _initialFuel = data.FuelTotalGallons;
            _distanceTraveled = 0;
            _lastLat = data.Latitude;
            _lastLon = data.Longitude;
            _lastDatabaseUpdate = DateTime.Now;
            _lastWearUpdate = DateTime.Now;
            _currentStatus = "VOO EM PROGRESSO - GRAVANDO";
            OnStatusChanged?.Invoke(_currentStatus);

            // Iniciar Gravação Blackbox
            string destIcao = _activeMissions.FirstOrDefault()?.DestinationICAO ?? "DEST";
            _telemetryLogger.StartFlightLog(_originIcao, destIcao, _aircraft);
        }

        private async Task FinalizeFlightAsync(SimTelemetry data)
        {
            _telemetryLogger.StopFlightLog("VOO_CONCLUIDO", _qualityService.TouchdownFPM, _qualityService.GetQualityScore());
            StopMonitoring();

            if (_pilot == null || _aircraft == null) return;

            // Encontrar ICAO de destino (o mais próximo das coordenadas atuais)
            string destinationIcao = _originIcao;
            try
            {
                var lat = data.Latitude;
                var lon = data.Longitude;
                double range = 0.5;
                
                await using var context = await CreateDbContextAsync();
                var nearbyAirports = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(context.Airports.Where(a => 
                        a.Latitude > lat - range && a.Latitude < lat + range &&
                        a.Longitude > lon - range && a.Longitude < lon + range));

                var nearest = nearbyAirports
                    .Select(a => new { Airport = a, Distance = LocationUtils.CalculateDistance(lat, lon, a.Latitude, a.Longitude) })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                if (nearest != null) destinationIcao = nearest.Airport.ICAO;
            }
            catch { }

            var outcome = await _flightService.ProcessFlightFinishAsync(
                _pilot.Id,
                _aircraft.Id,
                _originIcao,
                destinationIcao,
                _startTime,
                _initialFuel,
                data.FuelTotalGallons,
                _distanceTraveled,
                _activeMissions,
                _qualityService
            );

            OnFlightFinished?.Invoke(outcome);
        }
    }
}
