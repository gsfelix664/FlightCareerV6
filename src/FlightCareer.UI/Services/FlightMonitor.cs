using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Utils;
using FlightCareer.Domain.Services;
using FlightCareer.Application.DTOs;
using FlightCareer.Application.Services;
using FlightCareer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.UI.Services
{
    public class FlightMonitor
    {
        private readonly SimConnectService _simConnect;
        private readonly FlightService _flightService;
        private readonly IDbContextFactory<Infrastructure.Persistence.AppDbContext> _contextFactory;
        private readonly ComponentFlightSession _failures;
        private readonly ComponentCatalog _catalog;
        private readonly IRandomSource _random = new SystemRandomSource();
        private readonly FlightQualityService _qualityService = new();
        private readonly FlightTelemetryLogger _telemetryLogger = new();
        private readonly PartCatalog _partCatalog;
        private readonly WearRuleCatalog _wearRules;
        private readonly AircraftLimitResolver _limitResolver;
        private readonly PartFlightSession _parts;
        private TouchdownEvent? _lastTouchdownEvent;
        private string _currentStatus = "PREPARAÇÃO";

        public bool IsMonitoring { get; private set; }
        public bool IsFlightActive { get; private set; }
        public string CurrentStatus => _currentStatus;
        public PilotAircraft? CurrentAircraft => _aircraft;
        public IReadOnlyList<Mission> ActiveMissions => _activeMissions;
        private bool _isAirborne;
        private bool _hasTouchedDown;
        private double _simulatedFlightTimeSeconds = 0;
        private DateTime _lastTick = DateTime.MinValue;
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

        // Leitura rápida (a cada 2 frames): toque e crash medidos pelo addon, independem do aviso de crash do sim
        private readonly TouchdownDetector _touchdown;
        private readonly Stopwatch _fastClock = Stopwatch.StartNew();
        private DateTime _lastFastSample = DateTime.MinValue;
        private bool _impactCrashHandled;
        // Perda total em andamento: crash do sim, crash do addon e componentes a 0% podem chegar juntos
        private bool _totalLossInProgress;
        private static readonly TimeSpan FastStreamTimeout = TimeSpan.FromSeconds(3);
        /// <summary>Leitura rápida chegando: o toque é medido nela; senão, cai no cálculo antigo pela telemetria de 1 s.</summary>
        private bool FastStreamAlive => DateTime.Now - _lastFastSample < FastStreamTimeout;

        public event Action<string, string, bool>? OnAlert;
        public event Action<string>? OnStatusChanged;
        public event Action<FlightOutcome>? OnFlightFinished;
        public event Action<PilotAircraft>? OnTotalLoss;
        public event Action? OnMonitoringCancelled;

        /// <summary>Desgaste e panes por componente do voo atual.</summary>
        public ComponentFlightSession Failures => _failures;

        /// <summary>Panes persistentes (discrepâncias entre voos), addon e nativas.</summary>
        public PersistentFailureService PersistentFailures => _persistentFailures;
        private readonly PersistentFailureService _persistentFailures;
        private PilotAircraft? _watchedAircraft;

        [ActivatorUtilitiesConstructor]
        public FlightMonitor(
            SimConnectService simConnect,
            FlightService flightService,
            IDbContextFactory<Infrastructure.Persistence.AppDbContext> contextFactory,
            ComponentCatalog catalog,
            SensorFrameReader sensorFrames,
            PartCatalog partCatalog,
            WearRuleCatalog wearRules,
            AircraftLimitResolver limitResolver)
        {
            _simConnect = simConnect;
            _flightService = flightService;
            _contextFactory = contextFactory;
            _catalog = catalog;
            _failures = new ComponentFlightSession(simConnect, catalog, _random);
            _failures.OnFailure += OnAircraftFailureTriggered;
            _touchdown = new TouchdownDetector(catalog.Landing);

            _partCatalog = partCatalog;
            _wearRules = wearRules;
            _limitResolver = limitResolver;
            _parts = new PartFlightSession(partCatalog, wearRules, limitResolver);
            sensorFrames.OnFrame += _parts.OnFrame;
            _parts.OnExceedance += ex => _telemetryLogger.LogEvent("EXCEDENCIA_PECA", $"{ex.RuleId} - {ex.TargetId} - {ex.PeakValue:F1} (limite {ex.LimitValue:F1}) - dano {ex.TotalDamagePct:F2}%");
            _parts.OnInspection += insp => _telemetryLogger.LogEvent("INSPECAO_PECA", $"{insp.DefectCode} - {insp.TargetPartId} - {insp.Description}");
            _parts.OnDiagnostic += diag => _telemetryLogger.LogEvent("PECAS_DIAG", diag);

            _persistentFailures = new PersistentFailureService(simConnect, contextFactory, sensorFrames);
            _persistentFailures.OnDefectRecorded += OnDefectRecorded;
            _persistentFailures.OnNativeCrash += HandleSimulatorCrash;
            _persistentFailures.OnEnforcementAction += msg => _telemetryLogger.LogEvent("PANE_PERSISTENTE", msg);
        }

        /// <summary>
        /// Aeronave ativa do piloto: mesmo fora de um voo monitorado (ex.: NO-GO, voo reiniciado no sim),
        /// as panes abertas dela continuam aplicadas e vigiadas no simulador.
        /// </summary>
        public void WatchAircraft(PilotAircraft? aircraft)
        {
            _watchedAircraft = aircraft;
            if (IsMonitoring) return; // durante o voo o modo completo já está ativo
            if (aircraft == null) _persistentFailures.Stop();
            else _persistentFailures.Start(aircraft, () => "SOLO (FORA DE VOO)", detectNative: false);
        }

        /// <summary>Panes abertas que impedem o voo (NO-GO).</summary>
        public List<AircraftDefect> GetGroundingDefects(Guid aircraftId) =>
            _persistentFailures.LoadOpenDefects(aircraftId).Where(d => d.IsGrounding).ToList();

        /// <summary>Pane nativa detectada no simulador: reflete o dano nos componentes.</summary>
        private void OnDefectRecorded(AircraftDefect defect)
        {
            if (defect.Origin != DefectOrigin.Native) return; // panes do addon já foram anunciadas pelo alerta da sessão

            if (_aircraft != null && IsMonitoring && NativeWearFailureMap.ComponentIdFromCause(defect.Cause) is int wearId)
            {
                // Pane do wear and tear nativo: a peça quebrou no sim e vai a 0% (plano › 0.1, regra 5; decisão 2026-11-14)
                if (_parts.IsActive)
                {
                    _parts.ApplyNativeFailed(wearId, NativeWearFailureMap.Get(wearId)?.Kind);
                    _parts.SyncComponents(_aircraft, _failures.Components);
                }
                else
                {
                    if (NativeWearFailureMap.Get(wearId)?.Kind is ComponentKind wearKind) _failures.ClampHealth(wearKind, 0.0);
                }
                SyncLegacyConditions();
            }
            else if (_aircraft != null && IsMonitoring)
            {
                if (_parts.IsActive)
                {
                    switch (defect.Code)
                    {
                        case DefectCatalog.EngineFailure:
                            _parts.ClampGroup(ComponentKind.Engine, 15.0);
                            break;
                        case DefectCatalog.EngineFire:
                            _parts.ClampGroup(ComponentKind.Engine, 5.0);
                            _parts.DamageGroup(ComponentKind.Airframe, 20.0);
                            break;
                        case DefectCatalog.EngineDamage:
                            _parts.ClampGroup(ComponentKind.Engine, 100.0 - defect.Parameter);
                            break;
                        case DefectCatalog.GearDamage:
                            _parts.DamageGroup(ComponentKind.LandingGear, 20.0);
                            break;
                        case DefectCatalog.BrakesWorn:
                            _parts.ClampGroup(ComponentKind.Brakes, 30.0);
                            break;
                    }
                    _parts.SyncComponents(_aircraft, _failures.Components);
                }
                else
                {
                    switch (defect.Code)
                    {
                        case DefectCatalog.EngineFailure:
                            _failures.ClampHealth(ComponentKind.Engine, 15.0);
                            break;
                        case DefectCatalog.EngineFire:
                            _failures.ClampHealth(ComponentKind.Engine, 5.0);
                            Damage(ComponentKind.Airframe, 20.0);
                            break;
                        case DefectCatalog.EngineDamage:
                            _failures.ClampHealth(ComponentKind.Engine, 100.0 - defect.Parameter);
                            break;
                        case DefectCatalog.GearDamage:
                            Damage(ComponentKind.LandingGear, 20.0);
                            break;
                        case DefectCatalog.BrakesWorn:
                            _failures.ClampHealth(ComponentKind.Brakes, 30.0);
                            break;
                    }
                }
                SyncLegacyConditions();
            }

            OnAlert?.Invoke($"PANE DO SIMULADOR: {defect.Title.ToUpper()}",
                $"{defect.Description} Registrada como defeito {defect.SeverityLabel} até o reparo na oficina.", defect.IsGrounding);
            _telemetryLogger.LogEvent("PANE_NATIVA_MSFS", $"{defect.Code} - {defect.Cause}");
        }

        private void Damage(ComponentKind kind, double amount)
        {
            if (_parts.IsActive)
            {
                _parts.DamageGroup(kind, amount);
                if (_aircraft != null) _parts.SyncComponents(_aircraft, _failures.Components);
            }
            else
            {
                var c = _failures.Get(kind);
                if (c != null) c.Health = Math.Max(0.0, c.Health - amount);
            }
        }

        /// <summary>
        /// Carrega (ou cria) os componentes da aeronave, aplica reparos/danos feitos fora do voo nas colunas
        /// antigas (Hangar, crash no pouso) e entrega cópias desanexadas para a sessão de voo.
        /// Se a aeronave possui peças individuais, inicializa o PartFlightSession e sincroniza as barras.
        /// </summary>
        private void LoadFlightComponents(PilotAircraft aircraft)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var dbAircraft = context.PilotAircrafts
                    .Include(a => a.AircraftType)
                    .Include(a => a.Components)
                    .Include(a => a.Parts)
                    .FirstOrDefault(a => a.Id == aircraft.Id);

                if (dbAircraft == null)
                {
                    _parts.Reset();
                    _failures.PartsDriven = false;
                    _failures.PartDamageSink = null;
                    _failures.Begin(new List<AircraftComponent>(), aircraft.AircraftType);
                    return;
                }

                var engineClass = _catalog.ResolveEngineClass(dbAircraft.AircraftType);
                var existingKinds = dbAircraft.Components.Select(c => c.Kind).ToHashSet();
                var missing = LegacyComponentMigrator.CreateComponents(dbAircraft, engineClass, _catalog, _random)
                    .Where(c => !existingKinds.Contains(c.Kind))
                    .ToList();
                if (missing.Count > 0) context.AircraftComponents.AddRange(missing);

                var all = dbAircraft.Components.Concat(missing).Distinct().ToList();
                LegacyConditionBridge.ReconcileFromLegacy(dbAircraft, all, _random, _catalog.Visibility);
                LegacyConditionBridge.WriteToLegacy(dbAircraft, all);
                context.SaveChanges();

                if (PartComponentBridge.HasParts(dbAircraft))
                {
                    string? model = dbAircraft.AircraftType?.Model;
                    List<string> features = new();
                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        var limitProfileEntity = context.AircraftLimitProfiles.FirstOrDefault(p => p.AircraftTypeModel == model);
                        if (limitProfileEntity != null && !string.IsNullOrWhiteSpace(limitProfileEntity.FeaturesJson))
                        {
                            try
                            {
                                features = System.Text.Json.JsonSerializer.Deserialize<List<string>>(limitProfileEntity.FeaturesJson) ?? new();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Peças] Erro ao desserializar FeaturesJson para {model}: {ex.Message}");
                            }
                        }
                    }

                    var domainProfile = _limitResolver.FindById(model);

                    PartComponentBridge.ReconcilePartsFromComponents(all, dbAircraft.Parts, _partCatalog);
                    PartComponentBridge.SyncComponentsFromParts(dbAircraft, all, dbAircraft.Parts, _partCatalog);
                    context.SaveChanges();

                    var partCopies = dbAircraft.Parts.Select(ClonePart).ToList();
                    var compCopies = all.Select(CloneComponent).ToList();
                    LegacyConditionBridge.WriteToLegacy(aircraft, compCopies);

                    _parts.Begin(partCopies, domainProfile, features);
                    _failures.Begin(compCopies, dbAircraft.AircraftType);
                    _failures.PartsDriven = _parts.IsActive;
                    _failures.PartDamageSink = _parts.IsActive ? _parts.DamageGroup : null;
                }
                else
                {
                    _parts.Reset();
                    _failures.PartsDriven = false;
                    _failures.PartDamageSink = null;

                    var copies = all.Select(CloneComponent).ToList();
                    LegacyConditionBridge.WriteToLegacy(aircraft, copies);
                    _failures.Begin(copies, dbAircraft.AircraftType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Componentes] Falha ao carregar componentes da aeronave: {ex.Message}");
                _parts.Reset();
                _failures.PartsDriven = false;
                _failures.PartDamageSink = null;
                _failures.Begin(new List<AircraftComponent>(), aircraft.AircraftType);
            }
        }

        private static AircraftPart ClonePart(AircraftPart p) => new()
        {
            Id = p.Id,
            PilotAircraftId = p.PilotAircraftId,
            PartId = p.PartId,
            EngineIndex = p.EngineIndex,
            Health = p.Health,
            HoursSinceReplacement = p.HoursSinceReplacement,
            CyclesSinceReplacement = p.CyclesSinceReplacement,
            LastReplacedAtUtc = p.LastReplacedAtUtc,
            LastReplacedAirframeHours = p.LastReplacedAirframeHours,
            Flag = p.Flag,
            FlaggedBy = p.FlaggedBy,
            FlaggedAtUtc = p.FlaggedAtUtc,
            IsBroken = p.IsBroken,
            BrokenAtUtc = p.BrokenAtUtc
        };

        private static AircraftComponent CloneComponent(AircraftComponent c) => new()
        {
            Id = c.Id,
            PilotAircraftId = c.PilotAircraftId,
            Kind = c.Kind,
            Health = c.Health,
            HoursSinceOverhaul = c.HoursSinceOverhaul,
            CyclesSinceOverhaul = c.CyclesSinceOverhaul,
            LastInspectionAirframeHours = c.LastInspectionAirframeHours,
            InspectionPrecision = c.InspectionPrecision,
            InspectionBias = c.InspectionBias
        };

        /// <summary>Copia o estado dos componentes da sessão para as linhas do banco (no contexto informado).</summary>
        private async Task SaveComponentsAsync(Infrastructure.Persistence.AppDbContext context)
        {
            if (_aircraft == null || _failures.Components.Count == 0) return;
            var aircraftId = _aircraft.Id;
            var dbComponents = await context.AircraftComponents.Where(c => c.PilotAircraftId == aircraftId).ToListAsync();
            foreach (var c in _failures.Components)
            {
                var d = dbComponents.FirstOrDefault(x => x.Id == c.Id);
                if (d == null) continue;
                d.Health = c.Health;
                d.HoursSinceOverhaul = c.HoursSinceOverhaul;
                d.CyclesSinceOverhaul = c.CyclesSinceOverhaul;
                d.LastInspectionAirframeHours = c.LastInspectionAirframeHours;
                d.InspectionPrecision = c.InspectionPrecision;
                d.InspectionBias = c.InspectionBias;
            }

            if (_parts.IsActive)
            {
                await _parts.SaveAsync(context);
            }
        }

        private void SyncLegacyConditions()
        {
            if (_aircraft != null) LegacyConditionBridge.WriteToLegacy(_aircraft, _failures.Components);
        }

        private Task<Infrastructure.Persistence.AppDbContext> CreateDbContextAsync()
        {
            return _contextFactory.CreateDbContextAsync();
        }

        private void OnAircraftFailureTriggered(FlightFailureRecord failure)
        {
            // Gravar e registrar ANTES do alerta: nada aqui pode depender da janela do alerta
            string tag = failure.Source == "Excedência" ? "EXCEDENCIA" : "PANE_EM_VOO";
            _telemetryLogger.LogEvent(tag, $"[{failure.Source}] {failure.Title} - {failure.Description}");

            if (failure.Source != "Excedência")
            {
                if (!failure.Persistent)
                {
                    // Efeito transitório (gelo no carburador): a borda de ENG FAILED que vem a seguir não é pane nativa nova
                    if (failure.Effect == FailureEffect.EngineFailure) _persistentFailures.ExpectEngineFailureFromAddon();
                }
                else if (DefectCatalog.CodeFor(failure.Effect, failure.Kind, failure.IsCritical) is { } code)
                {
                    // Discrepância persistente: segue no avião até o reparo na oficina
                    _persistentFailures.Record(code, DefectOrigin.Addon, $"[{failure.Source}] {failure.Title}",
                        title: failure.Title, description: failure.Description,
                        alreadyInjected: failure.Effect != FailureEffect.None);
                }

                // Excedências vão só para o blackbox: uma caixa modal a cada limite excedido atrapalharia o voo
                OnAlert?.Invoke(failure.Title, failure.Description, failure.IsCritical);
            }
        }

        public bool StartMonitoring(Pilot pilot, PilotAircraft aircraft, List<Mission> missions)
        {
            if (IsMonitoring) return false;

            if (!_simConnect.IsConnected)
            {
                OnAlert?.Invoke("Erro de Conexão", "O Simulador não está conectado!", true);
                return false;
            }

            if (aircraft.IsInterdicted)
            {
                OnAlert?.Invoke("AERONAVE INTERDITADA", $"Aeronave impedida de voar! Motivo: {aircraft.InterdictionReason}. Efetue a manutenção obrigatória no Hangar.", true);
                return false;
            }

            var groundingDefects = GetGroundingDefects(aircraft.Id);
            if (groundingDefects.Count > 0)
            {
                OnAlert?.Invoke("PANE EM ABERTO", $"Item inoperante: {string.Join(", ", groundingDefects.Select(d => d.Title))}. Repare na oficina do Hangar.", true);
                return false;
            }

            var prohibitedMission = missions.FirstOrDefault(m => m.RequiredCategory > pilot.PilotCategory);
            if (prohibitedMission != null)
            {
                OnAlert?.Invoke("OPERAÇÃO NÃO HOMOLOGADA", $"A missão '{prohibitedMission.Title}' exige homologação {prohibitedMission.RequiredCategory.GetDisplayName()}, mas sua empresa está operando em {pilot.PilotCategory.GetDisplayName()}!", true);
                return false;
            }

            _pilot = pilot;
            _aircraft = aircraft;
            _activeMissions = missions;
            _originIcao = pilot.CurrentAirportICAO;

            // Panes persistentes primeiro: registram os toggles que o início do voo não pode desfazer
            _persistentFailures.Start(aircraft, () => _currentStatus);

            _simConnect.ResetCrashState();
            LoadFlightComponents(aircraft);
            _failures.Reset(clearSimState: true); // Desfaz no MSFS falhas remanescentes do voo anterior (exceto panes abertas)
            _startConditionTime = null;
            IsFlightActive = false;
            _isAirborne = false;
            _hasTouchedDown = false;
            _simulatedFlightTimeSeconds = 0;
            _lastTick = DateTime.MinValue;

            _touchdown.Reset();
            _impactCrashHandled = false;
            _totalLossInProgress = false;
            _simConnect.OnDataReceived += OnSimDataReceived;
            _simConnect.OnFastFlightReceived += OnFastFlight;
            _simConnect.OnCrashEvent += OnSimConnectCrash;
            IsMonitoring = true;

            OnAlert?.Invoke("SISTEMA INICIALIZADO", "O monitoramento começará automaticamente 10 segundos após você ligar os motores e soltar o freio de estacionamento.", false);
            var deferred = _persistentFailures.OpenDefects;
            if (deferred.Count > 0)
            {
                OnAlert?.Invoke("PANES DIFERIDAS ATIVAS", $"Voo com itens inoperantes: {string.Join(", ", deferred.Select(d => d.Title))}.", false);
            }
            _currentStatus = "PREPARAÇÃO";
            OnStatusChanged?.Invoke(_currentStatus);
            return true;
        }

        public void StopMonitoring()
        {
            if (!IsMonitoring) return;
            _simConnect.OnDataReceived -= OnSimDataReceived;
            _simConnect.OnFastFlightReceived -= OnFastFlight;
            _simConnect.OnCrashEvent -= OnSimConnectCrash;
            _persistentFailures.Stop();
            // Volta ao modo vigia: as panes (inclusive as que surgiram neste voo) seguem aplicadas após o pouso
            var keepWatching = _aircraft ?? _watchedAircraft;
            if (keepWatching != null)
            {
                _watchedAircraft = keepWatching;
                _persistentFailures.Start(keepWatching, () => "SOLO (FORA DE VOO)", detectNative: false);
            }
            _simConnect.ResetCrashState();
            _parts.Reset();
            _failures.PartsDriven = false;
            _failures.PartDamageSink = null;
            _failures.Reset();
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
                        dbAircraft.FuelAmount = Math.Round(lastData.Value.FuelTotalGallons + lastData.Value.UnusableFuelGallons, 1);
                        dbAircraft.EngineCondition = _aircraft.EngineCondition;
                        dbAircraft.AirframeCondition = _aircraft.AirframeCondition;
                        dbAircraft.TiresCondition = _aircraft.TiresCondition;
                        dbAircraft.AvionicsCondition = _aircraft.AvionicsCondition;
                        dbAircraft.MagnetosCondition = _aircraft.MagnetosCondition;
                        dbAircraft.OilCondition = _aircraft.OilCondition;
                        dbAircraft.LandingsCount = _aircraft.LandingsCount;
                        await SaveComponentsAsync(context);

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

        private void OnSimConnectCrash(object? sender, EventArgs e) =>
            HandleSimulatorCrash("Acidente reportado pelo simulador");

        private void HandleSimulatorCrash(string cause)
        {
            var decision = CrashPolicy.Decide(IsMonitoring, IsFlightActive, _impactCrashHandled);
            switch (decision)
            {
                case CrashDecision.TotalLoss:
                    _impactCrashHandled = true;
                    bool alreadyApplied = _failures.CrashApplied;
                    _failures.MarkCrashApplied();
                    if (!alreadyApplied)
                    {
                        _telemetryLogger.LogEvent("CRASH_MSFS", cause);
                        _currentStatus = "ACIDENTE";
                        OnStatusChanged?.Invoke(_currentStatus);
                        OnAlert?.Invoke("ACIDENTE", $"O simulador reportou acidente: {cause}. A aeronave foi perdida.", true);
                    }
                    _ = HandleTotalLossAsync();
                    break;

                case CrashDecision.AlertOnly:
                    _telemetryLogger.LogEvent("CRASH_FLAG_MSFS", cause);
                    OnAlert?.Invoke("ACIDENTE REPORTADO PELO SIMULADOR", cause, true);
                    break;

                case CrashDecision.Ignore:
                    break;
            }
        }

        private async Task HandleTotalLossAsync()
        {
            if (_aircraft == null || _totalLossInProgress) return;
            _totalLossInProgress = true;
            
            double fpm = _qualityService.TouchdownFPM > 0 ? _qualityService.TouchdownFPM : (_lastData != null ? Math.Abs(_lastData.Value.VerticalSpeed) : 0);
            _telemetryLogger.StopFlightLog("PERDA_TOTAL_DESTRUIDA", Math.Round(fpm, 0), 0);
            _failures.ZeroAll();
            if (_parts.IsActive)
            {
                _parts.ZeroAll();
                _parts.SyncComponents(_aircraft, _failures.Components);
            }
            SyncLegacyConditions();
            try
            {
                await using var componentContext = await CreateDbContextAsync();
                await SaveComponentsAsync(componentContext);
                await componentContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving components on total loss: {ex.Message}");
            }
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
                    dbAircraft.AvionicsCondition = 0;
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
                        _currentStatus = "INICIANDO (10s)";
                        OnStatusChanged?.Invoke(_currentStatus);
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
                        _currentStatus = "PREPARAÇÃO";
                        OnStatusChanged?.Invoke(_currentStatus);
                    }
                }
                return;
            }

            // --- CONTAGEM DE TEMPO DE VOO SIMULADO COM PAUSA E SIM RATE ---
            DateTime now = DateTime.Now;
            double dt = _lastTick == DateTime.MinValue ? 0 : (now - _lastTick).TotalSeconds;
            _lastTick = now;

            bool isPaused = data.IsEngineRunning && data.GroundSpeed == 0 && data.EngineRPM == 0;
            if (!isPaused)
            {
                // Salto (pausa/travamento) limitado no tempo real, antes do sim rate (antes cortava em 1 s por pacote)
                _simulatedFlightTimeSeconds += SimTimeStep.ToSimSeconds(dt, data.SimulationRate);
            }

            // Panes nativas do MSFS (ENG FAILED, fogo, dano de motor/flap/trem, freios, vácuo) são detectadas pelo
            // PersistentFailureService na leitura rápida de panes e tratadas em OnDefectRecorded.

            // --- DETECÇÃO DE PLANEIO / MOTOR CORTADO EM VOO (TREINAMENTO OU VOLUNTÁRIO) ---
            if (_isAirborne && !data.IsOnGround && !data.IsEngineRunning)
            {
                if (_currentStatus != "VOANDO (PLANANDO / MOTOR CORTADO)")
                {
                    _currentStatus = "VOANDO (PLANANDO / MOTOR CORTADO)";
                    OnStatusChanged?.Invoke(_currentStatus);
                    _telemetryLogger.LogEvent("ENGINE_CUT_GLIDING", "Voo planado / motor cortado em voo");
                }
            }
            else if (_isAirborne && !data.IsOnGround && data.IsEngineRunning && _currentStatus == "VOANDO (PLANANDO / MOTOR CORTADO)")
            {
                _currentStatus = "VOANDO";
                OnStatusChanged?.Invoke(_currentStatus);
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

            // Processar Desgaste e Falhas a cada pacote de telemetria
            if (_aircraft != null)
            {
                double wallSeconds = (DateTime.Now - (_lastWearUpdate == DateTime.MinValue ? DateTime.Now : _lastWearUpdate)).TotalSeconds;
                // Desgaste no mesmo relógio do TBO/inspeções: tempo simulado (sim rate), com o salto limitado no tempo real
                double deltaHours = SimTimeStep.ToSimHours(wallSeconds, data.SimulationRate);
                _failures.ProcessTick(data, deltaHours);

                if (_parts.IsActive)
                {
                    if (_failures.CrashApplied)
                    {
                        _parts.ZeroAll();
                    }
                    else
                    {
                        _parts.ProcessTick(deltaHours);
                    }
                    _parts.SyncComponents(_aircraft, _failures.Components);
                }

                SyncLegacyConditions();

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
                _currentStatus = data.IsEngineRunning ? "VOANDO" : "VOANDO (PLANANDO / MOTOR CORTADO)";
                OnStatusChanged?.Invoke(_currentStatus);
            }

            // --- DETECÇÃO DE TOUCHDOWN REAL EM VOO ---
            if (_isAirborne && data.IsOnGround)
            {
                double fpmBody = Math.Abs(data.VelocityBodyY * 60.0);
                double fpmVs = Math.Abs(data.VerticalSpeed);
                double touchdownRate = fpmBody > 10 ? fpmBody : fpmVs;

                // Com a leitura rápida ativa, o toque e o bounce são registrados em OnFastFlight (medidos no contato)
                if (!_hasTouchedDown && !FastStreamAlive)
                {
                    _hasTouchedDown = true;
                    _qualityService.RecordTouchdownFpm(touchdownRate);
                    _lastTouchdownEvent = null;
                    RegisterLanding(_qualityService.TouchdownFPM, data, isBounce: false);

                    _currentStatus = $"POUSO ({_qualityService.TouchdownFPM:F0} FPM)";
                    OnStatusChanged?.Invoke(_currentStatus);
                }
                else
                {
                    // Bounce detection: se o impacto subsequente for mais forte (durante a desaceleração de pouso > 35 kts)
                    if (!FastStreamAlive && data.GroundSpeed > 35 && touchdownRate > _qualityService.TouchdownFPM)
                    {
                        _qualityService.RecordTouchdownFpm(touchdownRate);
                        _lastTouchdownEvent = null;
                        RegisterLanding(touchdownRate, data, isBounce: true);
                        _currentStatus = $"POUSO BOUNCE ({_qualityService.TouchdownFPM:F0} FPM)";
                        OnStatusChanged?.Invoke(_currentStatus);
                    }
                    else if (data.GroundSpeed <= 35 && _currentStatus.StartsWith("POUSO") && _startConditionTime == null)
                    {
                        _currentStatus = "TÁXI / PÁTIO";
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
                    _currentStatus = "FINALIZANDO (5s)";
                    OnStatusChanged?.Invoke(_currentStatus);
                }
                
                if ((DateTime.Now - _startConditionTime.Value).TotalSeconds >= 5)
                {
                    if (!_hasTouchedDown)
                    {
                        double fpm = Math.Abs(data.VelocityBodyY * 60.0);
                        _qualityService.RecordTouchdownFpm(fpm > 10 ? fpm : Math.Abs(data.VerticalSpeed));
                        RegisterLanding(_qualityService.TouchdownFPM, data, isBounce: false);
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
                    _currentStatus = "CANCELANDO (5s)";
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

        /// <summary>Leitura rápida: alimenta o detector de toque; o evento sai 0,5 s depois do contato.</summary>
        private void OnFastFlight(SimFastFlightState s)
        {
            if (!IsMonitoring) return;
            _lastFastSample = DateTime.Now;
            if (!IsFlightActive || _impactCrashHandled) return;

            var ev = _touchdown.Update(new FastFlightSample(
                _fastClock.Elapsed.TotalSeconds, s.OnGround >= 0.5, s.VerticalSpeed, s.BodyVertical, s.GForce,
                -s.Pitch, s.Bank, s.Airspeed, s.GroundSpeed));
            if (ev != null) HandleTouchdown(ev);
        }

        private void HandleTouchdown(TouchdownEvent ev)
        {
            var inv = CultureInfo.InvariantCulture;
            string summary = string.Format(inv, "{0} fpm, pico {1:F2} G, nariz {2:F1}°, inclinação {3:F1}°, GS {4:F0} kt",
                ev.TouchdownFpm, ev.PeakG, ev.NoseUpDeg, ev.BankDeg, ev.GroundSpeedKnots);
            string trace = string.Join(" | ", ev.Trace.Select(x => string.Format(inv, "{0:F2}s {1}{2:F0}fpm {3:F2}G {4:F0}/{5:F0}°",
                x.TimeSec - ev.ContactTimeSec, x.OnGround ? "S " : "A ", x.VerticalSpeedFpm, x.GForce, x.NoseUpDeg, x.BankDeg)));

            if (ev.Kind == TouchdownKind.Crash)
            {
                _impactCrashHandled = true;
                _failures.MarkCrashApplied();
                _telemetryLogger.LogEvent("CRASH_ADDON", $"{ev.Reason} ({summary})");
                _telemetryLogger.LogEvent("TOQUE_TRACE", trace);
                _currentStatus = "ACIDENTE";
                OnStatusChanged?.Invoke(_currentStatus);
                OnAlert?.Invoke("ACIDENTE", $"Impacto com o solo: {ev.Reason}. A aeronave foi perdida.", true);
                _ = HandleTotalLossAsync();
                return;
            }

            _telemetryLogger.LogEvent("TOQUE", summary);
            _telemetryLogger.LogEvent("TOQUE_TRACE", trace);
            if (!_isAirborne) return; // contato antes de decolar de fato (ex.: salto na corrida): só registro

            if (!_hasTouchedDown)
            {
                _hasTouchedDown = true;
                _qualityService.RecordTouchdownFpm(ev.TouchdownFpm);
                _lastTouchdownEvent = ev;
                if (_lastData is { } data) RegisterLanding(_qualityService.TouchdownFPM, data, isBounce: false);
                _currentStatus = $"POUSO ({_qualityService.TouchdownFPM:F0} FPM)";
                OnStatusChanged?.Invoke(_currentStatus);
            }
            else if (ev.GroundSpeedKnots > 35 && ev.TouchdownFpm > _qualityService.TouchdownFPM)
            {
                _qualityService.RecordTouchdownFpm(ev.TouchdownFpm);
                _lastTouchdownEvent = ev;
                if (_lastData is { } data) RegisterLanding(ev.TouchdownFpm, data, isBounce: true);
                _currentStatus = $"POUSO BOUNCE ({_qualityService.TouchdownFPM:F0} FPM)";
                OnStatusChanged?.Invoke(_currentStatus);
            }
        }

        /// <summary>Toque na pista: ciclo de trem/pneus/freios e dano por impacto. Bounce não conta como novo pouso.</summary>
        private void RegisterLanding(double touchdownFpm, SimTelemetry data, bool isBounce)
        {
            if (_aircraft == null) return;
            if (!isBounce) _aircraft.LandingsCount++;
            _failures.ProcessLanding(touchdownFpm, data.VelocityBodyX, data.IsWetOrContaminatedSurface);

            if (_parts.IsActive)
            {
                double? pitch = _lastTouchdownEvent?.NoseUpDeg;
                double? bank = _lastTouchdownEvent?.BankDeg;
                _parts.ProcessTouchdown(touchdownFpm, pitch, bank, data.VelocityBodyX, isBounce);
                _parts.SyncComponents(_aircraft, _failures.Components);
            }

            SyncLegacyConditions();
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
                    dbAircraft.FuelAmount = Math.Round(data.FuelTotalGallons + data.UnusableFuelGallons, 1);
                    dbAircraft.EngineCondition = _aircraft.EngineCondition;
                    dbAircraft.AirframeCondition = _aircraft.AirframeCondition;
                    dbAircraft.TiresCondition = _aircraft.TiresCondition;
                    dbAircraft.AvionicsCondition = _aircraft.AvionicsCondition;
                    dbAircraft.MagnetosCondition = _aircraft.MagnetosCondition;
                    dbAircraft.OilCondition = _aircraft.OilCondition;
                    dbAircraft.HoursUntilNext50hInspection = _aircraft.HoursUntilNext50hInspection;
                    dbAircraft.HoursUntilNext100hInspection = _aircraft.HoursUntilNext100hInspection;
                    dbAircraft.HoursUntilNext500hInspection = _aircraft.HoursUntilNext500hInspection;
                    dbAircraft.EngineTboRemainingHours = _aircraft.EngineTboRemainingHours;
                    dbAircraft.LandingsCount = _aircraft.LandingsCount;
                    await SaveComponentsAsync(context);

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
            _failures.Reset(clearSimState: true);
            _qualityService.Reset();
            _touchdown.Reset();
            _impactCrashHandled = false;
            _totalLossInProgress = false;
            _startTime = DateTime.Now;
            _lastTick = DateTime.Now;
            _simulatedFlightTimeSeconds = 0;
            _initialFuel = data.FuelTotalGallons + data.UnusableFuelGallons;
            _distanceTraveled = 0;
            _lastLat = data.Latitude;
            _lastLon = data.Longitude;
            _lastDatabaseUpdate = DateTime.Now;
            _lastWearUpdate = DateTime.Now;
            _currentStatus = "VOO ATIVO";
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
            double nearestDistance = 0.0;
            bool isOffFieldLanding = false;

            try
            {
                var lat = data.Latitude;
                var lon = data.Longitude;
                double range = 1.0;
                
                await using var context = await CreateDbContextAsync();
                var nearbyAirports = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(context.Airports.Where(a => 
                        a.Latitude > lat - range && a.Latitude < lat + range &&
                        a.Longitude > lon - range && a.Longitude < lon + range));

                var nearest = nearbyAirports
                    .Select(a => new { Airport = a, Distance = LocationUtils.CalculateDistance(lat, lon, a.Latitude, a.Longitude) })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                if (nearest != null)
                {
                    destinationIcao = nearest.Airport.ICAO;
                    nearestDistance = nearest.Distance;
                    if (nearestDistance > 3.0)
                    {
                        isOffFieldLanding = true;
                    }
                }
                else
                {
                    var allAirports = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(context.Airports);
                    var globalNearest = allAirports
                        .Select(a => new { Airport = a, Distance = LocationUtils.CalculateDistance(lat, lon, a.Latitude, a.Longitude) })
                        .OrderBy(x => x.Distance)
                        .FirstOrDefault();

                    if (globalNearest != null)
                    {
                        destinationIcao = globalNearest.Airport.ICAO;
                        nearestDistance = globalNearest.Distance;
                        isOffFieldLanding = true;
                    }
                    else
                    {
                        isOffFieldLanding = true;
                        nearestDistance = 99.0;
                    }
                }
            }
            catch { }

            double flightDurationMinutes = _simulatedFlightTimeSeconds / 60.0;
            double finalFuel = Math.Round(data.FuelTotalGallons + data.UnusableFuelGallons, 1);

            // Componentes primeiro: o FlightService grava as colunas antigas (já sincronizadas) e as horas
            try
            {
                SyncLegacyConditions();
                await using var componentContext = await CreateDbContextAsync();
                await SaveComponentsAsync(componentContext);
                await componentContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving components at flight end: {ex.Message}");
            }

            var outcome = await _flightService.ProcessFlightFinishAsync(
                _pilot.Id,
                _aircraft.Id,
                _originIcao,
                destinationIcao,
                _startTime,
                _initialFuel,
                finalFuel,
                _distanceTraveled,
                _activeMissions,
                _qualityService,
                _aircraft.EngineCondition,
                _aircraft.TiresCondition,
                _aircraft.AirframeCondition,
                _aircraft.AvionicsCondition,
                _aircraft.LandingsCount,
                isOffFieldLanding,
                nearestDistance,
                flightDurationMinutes,
                _aircraft.MagnetosCondition,
                _aircraft.OilCondition
            );

            OnFlightFinished?.Invoke(outcome);
        }
    }
}
