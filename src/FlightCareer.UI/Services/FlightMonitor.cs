using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Fuel;
using FlightCareer.Domain.Integrity;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Missions;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Utils;
using FlightCareer.Domain.Services;
using FlightCareer.Domain.Xp;
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
        private readonly FuelUsageAccumulator _fuelAccumulator = new();
        private readonly FuelTruckDelivery _fuelTruck = new();
        private readonly TouchAndGoCounter _touchAndGoCounter = new();
        private readonly OverflyTracker _overflyTracker = new();
        private readonly SurveyLegTracker _surveyTracker = new();
        private readonly SkydiveDropTracker _skydiveTracker = new();
        private readonly ExceedanceLogAggregator _exceedanceLog = new();
        private bool _autoStartUsed;
        private FuelIntegrityMonitor? _fuelIntegrity;
        private SlewMonitor _slewMonitor = new();
        private bool _flightInvalidated;
        private string? _flightInvalidationReason;
        private TouchdownEvent? _lastTouchdownEvent;
        private string _currentStatus = "PREPARAÇÃO";

        public bool IsMonitoring { get; private set; }
        public bool IsFlightActive { get; private set; }
        public string CurrentStatus => _currentStatus;
        public PilotAircraft? CurrentAircraft => _aircraft;
        public Guid? WatchedAircraftId => _aircraft?.Id ?? _watchedAircraft?.Id;
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
        private DateTime _lastPreflightTick = DateTime.MinValue;
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
        /// <summary>Caminhão de combustível cobrado em outro contexto: quem guarda piloto/aeronave em contexto longo deve recarregar (id da aeronave).</summary>
        public event Action<Guid>? OnFuelTruckCharged;

        /// <summary>Desgaste e panes por componente do voo atual.</summary>
        public ComponentFlightSession Failures => _failures;

        /// <summary>Panes persistentes (discrepâncias entre voos), addon e nativas.</summary>
        public PersistentFailureService PersistentFailures => _persistentFailures;
        private readonly PersistentFailureService _persistentFailures;
        private PilotAircraft? _watchedAircraft;

        // Métricas e fatos operacionais para XP (XP-NOVO)
        private double _airborneSeconds;
        private bool _usedTimeAcceleration;
        private double _worstTouchdownFpm;
        private double _worstTouchdownG = 1.0;
        private int _flightTouchdowns;
        private int _flightLandingsCount;
        private FlightInfractionTracker _infractionTracker = new();
        private readonly Queue<(double SimSec, double WindKnots)> _windHistory = new();
        // Últimos 5 min simulados de visibilidade/nuvem: o bônus de IMC vale só na aproximação, não em cruzeiro
        private readonly Queue<(double SimSec, double VisibilityM, bool InCloud)> _approachEnvHistory = new();
        private double _landingCrosswindKt;
        private double _landingGustSpreadKt;
        private double _landingDensityAltitudeFt;
        private double _landingVisibilityM = 10000.0;
        private bool _landingInCloud;
        private bool _landingNight;

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
            // Uma linha no início e outra no fim de cada faixa (antes: uma por peça a cada tick)
            _parts.OnExceedanceTick += tick =>
            {
                foreach (var line in _exceedanceLog.Update(tick, _simulatedFlightTimeSeconds))
                    _telemetryLogger.LogEvent("EXCEDENCIA_PECA", line);
            };
            _parts.OnInspection += insp => _telemetryLogger.LogEvent("INSPECAO_PECA", $"{insp.DefectCode} - {insp.TargetPartId} - {insp.Description}");
            _parts.OnDiagnostic += diag => _telemetryLogger.LogEvent("PECAS_DIAG", diag);

            _persistentFailures = new PersistentFailureService(simConnect, contextFactory, sensorFrames);
            _persistentFailures.OnDefectRecorded += OnDefectRecorded;
            _persistentFailures.OnNativeCrash += HandleSimulatorCrash;
            _persistentFailures.OnEnforcementAction += msg => _telemetryLogger.LogEvent("PANE_PERSISTENTE", msg);

            _simConnect.OnObservedEvent += OnObservedEventReceived;
            _simConnect.OnDataReceived += OnTelemetryForFuelTruck;
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
                    _failures.PartFailureSource = null;
                    _failures.PartBreakSink = null;
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
                    _failures.PartFailureSource = _parts.IsActive ? _parts.EvaluateFailures : null;
                    _failures.PartBreakSink = _parts.IsActive ? _parts.BreakPart : null;
                }
                else
                {
                    _parts.Reset();
                    _failures.PartsDriven = false;
                    _failures.PartDamageSink = null;
                    _failures.PartFailureSource = null;
                    _failures.PartBreakSink = null;

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
                _failures.PartFailureSource = null;
                _failures.PartBreakSink = null;
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

        private void OnObservedEventReceived(string name, uint data)
        {
            if (string.Equals(name, "ENGINE_AUTO_START", StringComparison.OrdinalIgnoreCase))
            {
                if (IsMonitoring && !_autoStartUsed)
                {
                    _autoStartUsed = true;
                    _telemetryLogger.LogEvent("AUTO_START", "Partida automática usada");
                }
            }
            else if (string.Equals(name, "REQUEST_FUEL_KEY", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsMonitoring && WatchedAircraftId.HasValue && _simConnect.CurrentData.IsOnGround)
                {
                    double simFuel = _simConnect.CurrentData.FuelTotalGallons;
                    _fuelTruck.Arm(simFuel, DateTime.UtcNow);
                    _telemetryLogger.LogEvent("CAMINHAO_SOLICITADO", $"Caminhão solicitado no solo com {simFuel:F1} gal");
                }
            }
        }

        private void OnTelemetryForFuelTruck(object? sender, SimTelemetry data)
        {
            if (IsMonitoring) return;
            if (_fuelTruck.Status != FuelTruckDeliveryStatus.Armed) return;

            _fuelTruck.Update(data.FuelTotalGallons, data.IsOnGround, DateTime.UtcNow);
            if (_fuelTruck.Status == FuelTruckDeliveryStatus.Completed)
            {
                var aircraftId = WatchedAircraftId;
                if (aircraftId.HasValue)
                {
                    ProcessFuelTruckDelivery(aircraftId.Value, _fuelTruck.MaxGallons + data.UnusableFuelGallons);
                }
            }
        }

        /// <returns>Novo combustível da aeronave gravado no banco, ou null se nada foi cobrado.</returns>
        /// <param name="simFinalGallons">Combustível no sim depois do caminhão: vira o valor do app ("cobra e aceita").</param>
        private double? ProcessFuelTruckDelivery(Guid aircraftId, double simFinalGallons)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var aircraft = context.PilotAircrafts
                    .Include(a => a.AircraftType)
                    .FirstOrDefault(a => a.Id == aircraftId);
                if (aircraft == null) return null;

                // Cobra o que o app ainda não tinha pago (sim final − combustível do app), não a subida medida no sim:
                // se o sim não estava sincronizado, a subida medida daria combustível de graça ou cobraria a mais
                double fuelCapacity = aircraft.AircraftType?.FuelTankGallons ?? simFinalGallons;
                double newFuel = Math.Min(fuelCapacity, simFinalGallons);
                double deliveredGallons = Math.Round(newFuel - aircraft.FuelAmount, 2);
                if (deliveredGallons <= 0) return null;

                var pilot = context.Pilots.FirstOrDefault(p => p.Id == aircraft.PilotId);
                if (pilot == null) return null;

                int airportTier = 3;
                if (!string.IsNullOrEmpty(pilot.CurrentAirportICAO))
                {
                    var airport = context.Airports.FirstOrDefault(a => a.ICAO == pilot.CurrentAirportICAO);
                    if (airport != null && airport.Tier > 0)
                    {
                        airportTier = airport.Tier;
                    }
                }

                string fuelType = aircraft.AircraftType?.FuelType ?? "AVGAS";
                double pricePerGallon = FlightCareer.Application.Services.FuelPriceService.GetCurrentPrice(fuelType, airportTier);
                double costMultiplier = DifficultyEconomy.For(pilot.Difficulty).CostMultiplier;
                double cost = Math.Round(deliveredGallons * pricePerGallon * costMultiplier, 2);

                aircraft.FuelAmount = newFuel;

                pilot.Balance -= cost;
                context.FinancialTransactions.Add(new FinancialTransaction
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    Date = DateTime.Now,
                    Category = "Combustível",
                    Description = $"Combustível (caminhão do sim) {deliveredGallons:F1} gal",
                    Amount = cost,
                    Type = TransactionType.Expense
                });
                context.SaveChanges();

                if (_watchedAircraft != null && _watchedAircraft.Id == aircraftId)
                {
                    _watchedAircraft.FuelAmount = aircraft.FuelAmount;
                }
                if (_aircraft != null && _aircraft.Id == aircraftId)
                {
                    _aircraft.FuelAmount = aircraft.FuelAmount;
                }
                if (_pilot != null && _pilot.Id == pilot.Id)
                {
                    _pilot.Balance = pilot.Balance;
                }

                _telemetryLogger.LogEvent("COMBUSTIVEL_CAMINHAO", $"{deliveredGallons:F1} gal cobrados ({cost:C2}) a {pricePerGallon:C2}/gal");
                OnAlert?.Invoke("ABASTECIMENTO", $"Caminhão: {deliveredGallons:F1} gal cobrados ({cost:C2})", false);
                OnFuelTruckCharged?.Invoke(aircraftId);
                return aircraft.FuelAmount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FlightMonitor] Erro ao processar cobrança do caminhão de combustível: {ex.Message}");
                return null;
            }
        }

        public bool StartMonitoring(Pilot pilot, PilotAircraft aircraft, List<Mission> missions)
        {
            if (IsMonitoring) return false;

            _autoStartUsed = false;

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

            var prohibitedMission = missions.FirstOrDefault(m => m.RequiredPhase > pilot.CareerPhase);
            if (prohibitedMission != null)
            {
                OnAlert?.Invoke("OPERAÇÃO NÃO HOMOLOGADA", $"A missão '{prohibitedMission.Title}' exige a {prohibitedMission.RequiredPhase.GetDisplayName()}, mas sua empresa está operando na {pilot.CareerPhase.GetDisplayName()}!", true);
                return false;
            }

            if (_simConnect.CurrentFuelState.UnlimitedFuel > 0.5)
            {
                OnAlert?.Invoke("COMBUSTÍVEL ILIMITADO", "Desligue o combustível ilimitado nas opções do simulador para voar na carreira.", true);
                return false;
            }

            if (_fuelTruck.Status == FuelTruckDeliveryStatus.Armed)
            {
                double currentSimFuel = _simConnect.CurrentData.FuelTotalGallons;
                _fuelTruck.ForceComplete(currentSimFuel);
                if (_fuelTruck.Status == FuelTruckDeliveryStatus.Completed)
                {
                    // A aeronave recebida aqui é a do contexto longo do MainViewModel: atualizar em memória evita que a
                    // sincronização devolva o valor antigo e que um SaveChanges posterior regrave o combustível velho
                    if (ProcessFuelTruckDelivery(aircraft.Id, _fuelTruck.MaxGallons + _simConnect.CurrentData.UnusableFuelGallons) is double newFuel)
                    {
                        aircraft.FuelAmount = newFuel;
                    }
                }
            }

            // Abastecimento manual (decisão do usuário, 2026-11-21): o piloto compra no Hangar e coloca a mesma quantidade
            // no simulador (menu ou EFB). O app não grava nos tanques: só confere, como o briefing, e bloqueia se divergir.
            var startData = _simConnect.CurrentData;
            if (!startData.IsOnGround)
            {
                OnAlert?.Invoke("FORA DO SOLO", "Inicie o monitoramento com a aeronave no solo.", true);
                return false;
            }
            double fuelCapacity = aircraft.AircraftType?.FuelTankGallons ?? startData.FuelTotalCapacity;
            double fuelTolerance = Math.Max(0.5, fuelCapacity * 0.01);
            double simOnBoard = Math.Round(startData.FuelTotalGallons + startData.UnusableFuelGallons, 1);
            double startTolerance = Math.Max(1.0, aircraft.FuelAmount * 0.02); // mesma tolerância do briefing
            if (Math.Abs(simOnBoard - aircraft.FuelAmount) > startTolerance)
            {
                OnAlert?.Invoke("COMBUSTÍVEL DIVERGENTE",
                    $"A bordo no simulador: {simOnBoard:F1} gal. Abastecido no Hangar: {aircraft.FuelAmount:F1} gal. Ajuste o combustível no simulador (menu ou EFB) ou abasteça no Hangar.", true);
                return false;
            }

            _pilot = pilot;
            _aircraft = aircraft;
            _activeMissions = missions;
            _originIcao = pilot.CurrentAirportICAO;

            double failureMultiplier = DifficultyEconomy.For(pilot.Difficulty).FailureMultiplier;
            _failures.HazardMultiplier = failureMultiplier;
            _parts.HazardMultiplier = failureMultiplier;

            // Panes persistentes primeiro: registram os toggles que o início do voo não pode desfazer
            _persistentFailures.Start(aircraft, () => _currentStatus);

            _simConnect.ResetCrashState();
            LoadFlightComponents(aircraft);
            _failures.Reset(clearSimState: true); // Desfaz no MSFS falhas remanescentes do voo anterior (exceto panes abertas)
            _fuelAccumulator.Reset();
            _touchAndGoCounter.Reset(_simConnect.CurrentData.IsOnGround);
            _overflyTracker.Reset();
            _surveyTracker.Reset();
            _skydiveTracker.Reset();
            foreach (var m in missions)
            {
                if (m.SubId == "3.6.1" && !string.IsNullOrWhiteSpace(m.ContractData))
                {
                    try
                    {
                        var survey = System.Text.Json.JsonSerializer.Deserialize<SurveyContractData>(m.ContractData);
                        if (survey != null) _surveyTracker.Configure(survey);
                    }
                    catch { }
                }
                else if (m.SubId == "3.6.2" && !string.IsNullOrWhiteSpace(m.ContractData))
                {
                    try
                    {
                        var skydive = System.Text.Json.JsonSerializer.Deserialize<SkydiveContractData>(m.ContractData);
                        if (skydive != null) _skydiveTracker.Configure(skydive);
                    }
                    catch { }
                }
            }
            if (missions.Any(m => !string.IsNullOrWhiteSpace(m.OverflyWaypoints)))
            {
                using var wpContext = _contextFactory.CreateDbContext();
                var wpList = new List<OverflyPoint>();
                foreach (var m in missions)
                {
                    if (!string.IsNullOrWhiteSpace(m.OverflyWaypoints))
                    {
                        var idents = m.OverflyWaypoints.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var ident in idents)
                        {
                            var airp = wpContext.Airports.AsNoTracking().FirstOrDefault(a => a.ICAO == ident);
                            if (airp != null && !wpList.Any(p => p.Ident.Equals(airp.ICAO, StringComparison.OrdinalIgnoreCase)))
                            {
                                wpList.Add(new OverflyPoint(airp.ICAO, airp.Latitude, airp.Longitude, airp.ElevationFt));
                            }
                        }
                    }
                }
                if (wpList.Count > 0)
                {
                    _overflyTracker.ConfigureWaypoints(wpList);
                }
            }
            _fuelIntegrity = new FuelIntegrityMonitor(fuelCapacity, fuelTolerance);
            _slewMonitor = new SlewMonitor();
            _flightInvalidated = false;
            _flightInvalidationReason = null;
            _startConditionTime = null;
            IsFlightActive = false;
            _isAirborne = false;
            _hasTouchedDown = false;
            _simulatedFlightTimeSeconds = 0;
            _lastTick = DateTime.MinValue;
            _lastPreflightTick = DateTime.MinValue;

            _airborneSeconds = 0;
            _usedTimeAcceleration = false;
            _worstTouchdownFpm = 0;
            _worstTouchdownG = 1.0;
            _flightTouchdowns = 0;
            _flightLandingsCount = 0;
            var firstMission = _activeMissions.FirstOrDefault();
            string? missionFamilyId = firstMission != null ? MissionPayTable.Shared.Resolve(firstMission.SubId)?.Id : null;
            bool isInstruction = string.Equals(missionFamilyId, "instrucao", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(missionFamilyId, "instrucao_basica", StringComparison.OrdinalIgnoreCase);
            _infractionTracker = new FlightInfractionTracker(isInstruction);
            _windHistory.Clear();
            _approachEnvHistory.Clear();
            _landingCrosswindKt = 0;
            _landingGustSpreadKt = 0;
            _landingDensityAltitudeFt = 0;
            _landingVisibilityM = 10000.0;
            _landingInCloud = false;
            _landingNight = false;

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
            // As peças NÃO são zeradas aqui: FinalizeFlightAsync chama StopMonitoring antes de salvar
            // (SaveComponentsAsync). O próximo LoadFlightComponents faz Begin/Reset da sessão de peças.
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
                FlushExceedanceLog();
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

                // Voo cancelado também passa o tempo de carreira (tempo simulado voado + solo), como o fim de voo normal
                await _flightService.ProcessFlightCancellationAsync(_pilot.Id, _simulatedFlightTimeSeconds / 3600.0);
            }
            else if (!IsFlightActive && _parts.HasPreflightChanges && _aircraft != null)
            {
                try
                {
                    await using var context = await CreateDbContextAsync();
                    await SaveComponentsAsync(context);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving preflight parts state on cancel: {ex.Message}");
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
            
            // Capturar dados do voo antes de parar o monitoramento (que zera o estado)
            var pilot = _pilot;
            var aircraft = _aircraft;
            string originIcao = _originIcao ?? "";
            double durationMinutes = _simulatedFlightTimeSeconds / 60.0;
            double distanceNM = _distanceTraveled;
            var activeMissions = _activeMissions != null ? new List<Mission>(_activeMissions) : new List<Mission>();
            string crashSiteIcao = "";
            if (_lastData != null)
            {
                try
                {
                    crashSiteIcao = await GetNearestAirportICAO(_lastData.Value.Latitude, _lastData.Value.Longitude);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error finding nearest airport for crash site: {ex.Message}");
                }
            }

            double fpm = _qualityService.TouchdownFPM > 0 ? _qualityService.TouchdownFPM : (_lastData != null ? Math.Abs(_lastData.Value.VerticalSpeed) : 0);
            FlushExceedanceLog();
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

            if (pilot != null && aircraft != null)
            {
                try
                {
                    await _flightService.ProcessTotalLossAsync(
                        pilot.Id,
                        aircraft.Id,
                        originIcao,
                        crashSiteIcao,
                        durationMinutes,
                        distanceNM,
                        activeMissions
                    );
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing total loss flight: {ex.Message}");
                }
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

            _touchAndGoCounter.Update(data.IsOnGround, data.GroundSpeed, _simulatedFlightTimeSeconds);
            if (_overflyTracker.HasWaypoints)
            {
                _overflyTracker.Update(data.Latitude, data.Longitude, data.Altitude);
            }

            // --- MONITORAMENTO DE INTEGRIDADE: SLEW E CONGELAMENTOS ---
            var integrityState = _simConnect.CurrentIntegrityState;
            bool slewActive = integrityState.IsSlewActive > 0.5;
            bool altFreeze = integrityState.IsAltitudeFreezeOn > 0.5;
            bool attFreeze = integrityState.IsAttitudeFreezeOn > 0.5;
            bool latLonFreeze = integrityState.IsLatitudeLongitudeFreezeOn > 0.5;

            var slewVerdict = _slewMonitor.Update(
                slewActive: slewActive,
                onGround: data.IsOnGround,
                hasTakenOff: _isAirborne,
                latDeg: data.Latitude,
                lonDeg: data.Longitude
            );

            if (slewVerdict.Invalidated)
            {
                InvalidateFlight(slewVerdict.Reason ?? "slew");
            }
            else if (slewVerdict.UsageCompleted)
            {
                _telemetryLogger.LogEvent("SLEW", $"slew no solo: {Math.Round(slewVerdict.UsageMeters)} m");
            }

            var freezeVerdict = _slewMonitor.CheckFreeze(
                altitudeFreeze: altFreeze,
                attitudeFreeze: attFreeze,
                latLonFreeze: latLonFreeze,
                onGround: data.IsOnGround,
                hasTakenOff: _isAirborne
            );

            if (freezeVerdict.Invalidated)
            {
                InvalidateFlight(freezeVerdict.Reason ?? "congelamento");
            }

            var fuelState = _simConnect.CurrentFuelState;
            _fuelAccumulator.Update(new[]
            {
                fuelState.FuelUsedSinceStart1,
                fuelState.FuelUsedSinceStart2,
                fuelState.FuelUsedSinceStart3,
                fuelState.FuelUsedSinceStart4
            });

            foreach (var diag in _fuelAccumulator.DrainDiagnostics())
            {
                _telemetryLogger.LogEvent("FUEL_ACCUMULATOR", diag);
            }

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

                // Combustível colocado depois de iniciar o monitoramento (antes de ativar o voo) não foi pago no Hangar
                if (_aircraft != null && !_flightInvalidated)
                {
                    double onBoard = Math.Round(data.FuelTotalGallons + data.UnusableFuelGallons, 1);
                    if (onBoard > _aircraft.FuelAmount + Math.Max(1.0, _aircraft.FuelAmount * 0.02))
                    {
                        InvalidateFlight($"combustível abastecido depois de iniciar o monitoramento ({onBoard:F1} gal a bordo, {_aircraft.FuelAmount:F1} gal pagos)");
                    }
                }

                if (!IsFlightActive && _parts.IsActive && _aircraft != null)
                {
                    DateTime nowPreflight = DateTime.Now;
                    double wallSec = _lastPreflightTick == DateTime.MinValue ? 0.0 : (nowPreflight - _lastPreflightTick).TotalSeconds;
                    _lastPreflightTick = nowPreflight;
                    double dtHours = SimTimeStep.ToSimHours(wallSec, data.SimulationRate);
                    if (dtHours > 0)
                    {
                        _parts.ObservePreflight(dtHours);
                        _parts.SyncComponents(_aircraft, _failures.Components);
                        SyncLegacyConditions();
                    }
                }

                return;
            }

            // --- CONTAGEM DE TEMPO DE VOO SIMULADO COM PAUSA E SIM RATE ---
            DateTime now = DateTime.Now;
            double dt = _lastTick == DateTime.MinValue ? 0 : (now - _lastTick).TotalSeconds;
            _lastTick = now;

            bool isPaused = data.IsEngineRunning && data.GroundSpeed == 0 && data.EngineRPM == 0;
            double simDt = 0.0;
            if (!isPaused)
            {
                simDt = SimTimeStep.ToSimSeconds(dt, data.SimulationRate);
                // Salto (pausa/travamento) limitado no tempo real, antes do sim rate (antes cortava em 1 s por pacote)
                _simulatedFlightTimeSeconds += simDt;
            }

            if (_surveyTracker.HasActiveContract)
            {
                _surveyTracker.Update(data.Latitude, data.Longitude, data.Altitude, data.Airspeed, data.PlaneHeading);
            }
            if (_skydiveTracker.HasActiveContract)
            {
                double chtCooling = _parts.LatestChtCoolingRate;
                _skydiveTracker.Update(data.Latitude, data.Longitude, data.Altitude, data.Airspeed, chtCooling, simDt);
            }

            // --- FATOS OPERACIONAIS E AMBIENTAIS PARA XP ---
            if (data.SimulationRate > 1.01) _usedTimeAcceleration = true;
            _infractionTracker.OnTelemetryTick(
                isAirborne: _isAirborne && !data.IsOnGround,
                stallWarning: data.IsStallWarning,
                airspeedKnots: data.Airspeed,
                altitudeFeet: data.Altitude,
                hasOverspeedVne: data.HasOverspeed);

            if (_isAirborne && !data.IsOnGround)
            {
                _airborneSeconds += SimTimeStep.ToSimSeconds(dt, data.SimulationRate);
                _approachEnvHistory.Enqueue((_simulatedFlightTimeSeconds, data.AmbientVisibility, data.IsInCloud));
                while (_approachEnvHistory.Count > 0 && (_simulatedFlightTimeSeconds - _approachEnvHistory.Peek().SimSec) > 300.0)
                    _approachEnvHistory.Dequeue();
            }

            _windHistory.Enqueue((_simulatedFlightTimeSeconds, data.AmbientWindVelocity));
            while (_windHistory.Count > 0 && (_simulatedFlightTimeSeconds - _windHistory.Peek().SimSec) > 180.0)
                _windHistory.Dequeue();

            // --- MONITORAMENTO DE INTEGRIDADE DE COMBUSTÍVEL EM VOO ---
            if (_fuelIntegrity != null)
            {
                var integrityEvents = _fuelIntegrity.Update(
                    simFuelTotalGal: data.FuelTotalGallons,
                    unlimitedFuel: fuelState.UnlimitedFuel > 0.5,
                    simOnGround: data.IsOnGround,
                    simulatedTimeSeconds: _simulatedFlightTimeSeconds
                );

                foreach (var evt in integrityEvents)
                {
                    if (evt.Type == FuelIntegrityEventType.UnlimitedFuelActive || evt.Type == FuelIntegrityEventType.InFlightRefueling)
                    {
                        InvalidateFlight(evt.Message);
                    }
                }
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
            double qualitySimSeconds = isPaused ? 0.0 : SimTimeStep.ToSimSeconds(dt, data.SimulationRate);
            _qualityService.ProcessSample(FlightQualitySample.FromTelemetry(data), hasPax, qualitySimSeconds);

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
                _infractionTracker.OnTakeoff(data.IsLandingLightOn, data.IsTaxiLightOn);
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
                -s.Pitch, s.Bank, s.Airspeed, s.GroundSpeed, s.TouchdownNormalVelocity));
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
                _flightTouchdowns++;
                if (ev.TouchdownFpm > _worstTouchdownFpm) _worstTouchdownFpm = ev.TouchdownFpm;
                if (ev.PeakG > _worstTouchdownG) _worstTouchdownG = ev.PeakG;
                UpdateLandingEnvironmentSnapshot();

                _impactCrashHandled = true;
                _failures.MarkCrashApplied();
                _telemetryLogger.LogEvent("CRASH_ADDON", $"{ev.Reason} ({summary})");
                _telemetryLogger.LogEvent("TOQUE_TRACE", trace);
                _currentStatus = "ACIDENTE";
                OnStatusChanged?.Invoke(_currentStatus);
                OnAlert?.Invoke("ACIDENTE", $"Impacto com o solo: {ev.Reason}. A aeronave foi perdida.", true);
                _qualityService.RecordTouchdownFpm(ev.TouchdownFpm);
                _ = HandleTotalLossAsync();
                return;
            }

            _telemetryLogger.LogEvent("TOQUE", summary);
            _telemetryLogger.LogEvent("TOQUE_TRACE", trace);
            _flightTouchdowns++;
            if (ev.TouchdownFpm > _worstTouchdownFpm) _worstTouchdownFpm = ev.TouchdownFpm;
            if (ev.PeakG > _worstTouchdownG) _worstTouchdownG = ev.PeakG;
            UpdateLandingEnvironmentSnapshot();

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

        private void UpdateLandingEnvironmentSnapshot()
        {
            if (_lastData is { } data)
            {
                _landingCrosswindKt = data.CrosswindComponentKnots;
                _landingDensityAltitudeFt = data.DensityAltitude;
            }
            if (_windHistory.Count > 0)
            {
                double minW = double.MaxValue;
                double maxW = double.MinValue;
                foreach (var (_, w) in _windHistory)
                {
                    if (w < minW) minW = w;
                    if (w > maxW) maxW = w;
                }
                _landingGustSpreadKt = Math.Max(0.0, maxW - minW);
            }
            if (_approachEnvHistory.Count > 0)
            {
                double minVis = double.MaxValue;
                bool cloud = false;
                foreach (var (_, vis, inCloud) in _approachEnvHistory)
                {
                    if (vis > 0 && vis < minVis) minVis = vis;
                    cloud |= inCloud;
                }
                _landingVisibilityM = minVis < double.MaxValue ? minVis : 10000.0;
                _landingInCloud = cloud;
            }
        }

        /// <summary>Toque na pista: ciclo de trem/pneus/freios e dano por impacto. Bounce não conta como novo pouso.</summary>
        private void RegisterLanding(double touchdownFpm, SimTelemetry data, bool isBounce)
        {
            if (_aircraft == null) return;
            _infractionTracker.OnTouchdown(data.IsLandingLightOn, data.IsTaxiLightOn, isBounce);
            if (!isBounce)
            {
                _aircraft.LandingsCount++;
                _flightLandingsCount++;
            }
            if (touchdownFpm > _worstTouchdownFpm) _worstTouchdownFpm = touchdownFpm;
            if (data.GForce > _worstTouchdownG) _worstTouchdownG = data.GForce;
            UpdateLandingEnvironmentSnapshot();

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
            _exceedanceLog.Reset();
            string destIcao = _activeMissions.FirstOrDefault()?.DestinationICAO ?? "DEST";
            _telemetryLogger.StartFlightLog(_originIcao, destIcao, _aircraft);
        }

        private async Task FinalizeFlightAsync(SimTelemetry data)
        {
            FlushExceedanceLog();
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
            double realFuelUsed = _fuelAccumulator.TotalGallons(data.FuelWeightPerGallon);

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

            RunwayFacts? originFacts = null;
            RunwayFacts? destFacts = null;
            try
            {
                await using var rwyCtx = await CreateDbContextAsync();
                var origAirp = await rwyCtx.Airports.FirstOrDefaultAsync(a => a.ICAO == _originIcao);
                var destAirp = await rwyCtx.Airports.FirstOrDefaultAsync(a => a.ICAO == destinationIcao);

                var origRwys = await rwyCtx.Runways.Where(r => r.AirportIdent == _originIcao).ToListAsync();
                var destRwys = await rwyCtx.Runways.Where(r => r.AirportIdent == destinationIcao).ToListAsync();

                double minRunwayReq = _aircraft?.AircraftType?.MinRunwayLength ?? 0.0;

                if (origAirp != null && origRwys.Count > 0)
                {
                    var best = origRwys.OrderByDescending(r => r.LengthFt).First();
                    bool unpaved = RunwaySurface.IsUnpaved(best.Surface);
                    originFacts = new RunwayFacts(best.LengthFt, minRunwayReq, unpaved, origAirp.ElevationFt);
                }
                if (destAirp != null && destRwys.Count > 0)
                {
                    var best = destRwys.OrderByDescending(r => r.LengthFt).First();
                    bool unpaved = RunwaySurface.IsUnpaved(best.Surface);
                    destFacts = new RunwayFacts(best.LengthFt, minRunwayReq, unpaved, destAirp.ElevationFt);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resolving runway facts for XP: {ex.Message}");
            }

            // ENGINE TYPE: 0 pistão, 1 jato, 5 turboélice. Turboélice usa o limite de vento cruzado do monomotor (12 kt).
            bool isJet = data.EngineType == 1.0;

            var landingEnv = new EnvFacts(
                CrosswindKt: _landingCrosswindKt,
                IsJet: isJet,
                GustSpreadKt: _landingGustSpreadKt,
                DensityAltitudeFt: _landingDensityAltitudeFt,
                Night: _landingNight,
                VisibilityM: _landingVisibilityM,
                InCloud: _landingInCloud
            );

            double cruiseSpeed = _aircraft?.AircraftType?.CruiseSpeedKts ?? 100.0;
            if (cruiseSpeed <= 0) cruiseSpeed = 100.0;
            // ETE pela distância direta origem → destino (a distância voada inclui desvios e deixaria quase todo voo "no prazo")
            double plannedNm = _distanceTraveled;
            try
            {
                await using var etaCtx = await CreateDbContextAsync();
                var o = await etaCtx.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.ICAO == _originIcao);
                var d = await etaCtx.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.ICAO == destinationIcao);
                if (o != null && d != null)
                    plannedNm = LocationUtils.CalculateDistance(o.Latitude, o.Longitude, d.Latitude, d.Longitude);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error computing planned distance for ETE: {ex.Message}");
            }
            double eteMinutes = (plannedNm / cruiseSpeed) * 60.0 + 15.0;

            var firstMission = _activeMissions.FirstOrDefault();
            string? missionFamilyId = firstMission != null ? MissionPayTable.Shared.Resolve(firstMission.SubId)?.Id : null;
            bool isFerry = _activeMissions.Count == 0;
            bool isUrgent = _activeMissions.Any(m => m.Urgency > 1.0 || (m.SubId != null && (m.SubId.StartsWith("3.3") || m.SubId.StartsWith("4.3.1"))));

            int gPaxViolations = (_qualityService.IsPaxFlight && (_qualityService.MaxGForce > 2.2 || _qualityService.MinGForce < -0.5)) ? 1 : 0;
            double airborneMin = _airborneSeconds > 0 ? _airborneSeconds / 60.0 : flightDurationMinutes;

            var xpInputs = new XpFlightInputs(
                AirborneMinutes: airborneMin,
                DistanceNm: _distanceTraveled,
                LegsCompleted: Math.Max(1, _flightLandingsCount),
                MissionFamilyId: missionFamilyId,
                IsFerryOrNoMission: isFerry,
                UsedTimeAcceleration: _usedTimeAcceleration,
                CrashDetectionOn: data.IsCrashDetectionOn,
                AutoCoordinationOn: data.IsAutoCoordinationOn,
                RealismPct: data.Realism > 0 ? data.Realism : 100.0,
                AutoStartUsed: _autoStartUsed,
                WorstTouchdownFpm: _worstTouchdownFpm,
                WorstTouchdownG: _worstTouchdownG,
                TouchdownCount: Math.Max(1, _flightTouchdowns),
                VneOrStallInfractions: _infractionTracker.VneOrStallInfractions,
                GExcessInfractionsWithPax: gPaxViolations,
                LightsOrSpeedInfractions: _infractionTracker.LightsOrSpeedInfractions,
                OffRunwayOrExcursion: isOffFieldLanding,
                Origin: originFacts,
                Destination: destFacts,
                Landing: landingEnv,
                BlockMinutes: flightDurationMinutes,
                EteMinutes: eteMinutes,
                IsUrgentMission: isUrgent
            );

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
                _aircraft.OilCondition,
                realFuelUsed: realFuelUsed,
                isFlightInvalidated: _flightInvalidated,
                invalidationReason: _flightInvalidationReason,
                autoStartUsed: _autoStartUsed,
                xpInputs: xpInputs,
                visitedWaypoints: _overflyTracker.VisitedWaypoints,
                touchAndGoCount: _touchAndGoCounter.Count,
                surveyLegsCompleted: _surveyTracker.CompletedLegs,
                skydiveDropCompleted: _skydiveTracker.DropCompleted,
                skydiveThermalShock: _skydiveTracker.ThermalShockDetected,
                skydiveMaxCoolingRate: _skydiveTracker.MaxCoolingRateObserved
            );

            OnFlightFinished?.Invoke(outcome);
        }

        private void FlushExceedanceLog()
        {
            foreach (var line in _exceedanceLog.Flush(_simulatedFlightTimeSeconds))
                _telemetryLogger.LogEvent("EXCEDENCIA_PECA", line);
        }

        private void InvalidateFlight(string reason)
        {
            if (_flightInvalidated) return;
            _flightInvalidated = true;
            _flightInvalidationReason = reason.StartsWith("Voo invalidado", StringComparison.OrdinalIgnoreCase)
                ? reason
                : $"Voo invalidado: {reason}";
            _telemetryLogger.LogEvent("INTEGRIDADE", _flightInvalidationReason);
            OnAlert?.Invoke("VOO INVALIDADO", _flightInvalidationReason, true);
        }
    }
}
