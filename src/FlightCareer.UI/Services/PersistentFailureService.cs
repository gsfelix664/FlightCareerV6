using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Sensors;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Panes persistentes (discrepâncias entre voos). Pane nativa: o sim gera, o addon detecta e grava. Pane do addon:
    /// sorteada pelo sistema de componentes (ComponentFlightSession) e gravada aqui pelo FlightMonitor.
    /// As duas vão para a mesma tabela (AircraftDefects) e só saem pela oficina.
    /// No voo, o SimFailureActuator aplica o efeito; este serviço mantém o efeito entre voos e recarregamentos.
    ///
    /// Enquanto houver pane aberta e o voo estiver sendo monitorado, este serviço:
    ///  1. reaplica a pane ao carregar a aeronave (o simulador zera as falhas ao recarregar);
    ///  2. bloqueia os comandos do piloto (grupo de notificação mascarável) quando faz sentido;
    ///  3. vigia as SimVars a ~5-10 Hz e corrige o estado na hora (cockpits do MSFS 2024 podem usar
    ///     input events que passam por fora dos eventos interceptados).
    /// </summary>
    public class PersistentFailureService
    {
        // Chaves e comandos do piloto permanecem livres:
        // - Alternador: desconectado do barramento (ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE); chave física fica em ON.
        // - Flaps: superfície travada continuamente (WriteFlapSurfacePercent); alavanca física se move livremente.
        // - Magnetos: chave acompanha a ignição (limitação do modelo 3D).

        private const double EngineDamageThresholdPercent = 10.0;
        private const double BrakesWornThresholdPercent = 15.0;
        private const double SuctionHealthyInHg = 3.0;
        private const double SuctionFailedInHg = 1.0;
        private static readonly TimeSpan SuctionLowConfirmation = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan CorrectionThrottle = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan ReapplyDelayAfterLoad = TimeSpan.FromSeconds(5);

        private readonly SimConnectService _simConnect;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        // Pane do wear and tear nativo (F4.2c): WEAR AND TEAR IS FAILED:<id> vem do leitor genérico de sensores
        private readonly SensorFrameReader? _sensorFrames;
        private readonly NativeWearFailureDetector _nativeWear = new();

        private readonly List<AircraftDefect> _openDefects = new();
        private readonly HashSet<string> _togglesApplied = new();
        private readonly Dictionary<string, DateTime> _lastCorrection = new();
        // Panes registradas cuja gravação assíncrona ainda não terminou: não podem parecer "reparadas" numa releitura.
        private readonly HashSet<Guid> _unsaved = new();

        private readonly Dictionary<int, bool> _nativeFlapFailedById = new();
        private bool _nativeFlapFailed;

        private PilotAircraft? _aircraft;
        private Func<string> _phaseProvider = () => string.Empty;
        private bool _active;
        private bool _detectNative;
        private DateTime _lastDbSync = DateTime.MinValue;
        private static readonly TimeSpan WatchModeDbSyncInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan EngineReapplyThrottle = TimeSpan.FromSeconds(3);

        private SimFailureState? _previous;
        private bool _suctionSeenHealthy;
        private DateTime? _suctionLowSince;
        private DateTime? _reapplyAfter;
        private DateTime _expectEngineFailureUntil = DateTime.MinValue;

        public event Action<AircraftDefect>? OnDefectRecorded;
        public event Action<string>? OnNativeCrash;
        /// <summary>Correção aplicada pela vigilância/bloqueio (para log e HUD).</summary>
        public event Action<string>? OnEnforcementAction;

        public IReadOnlyList<AircraftDefect> OpenDefects => _openDefects;
        public bool IsActive => _active;

        public PersistentFailureService(SimConnectService simConnect, IDbContextFactory<AppDbContext> contextFactory,
            SensorFrameReader? sensorFrames = null)
        {
            _simConnect = simConnect;
            _contextFactory = contextFactory;
            _sensorFrames = sensorFrames;

            // O registro de TOGGLE_* enviados vale para a aeronave carregada no simulador, não para o voo do app:
            // só é limpo quando o sim recarrega a aeronave ou a conexão cai.
            _simConnect.OnAircraftReloaded += OnAircraftReloaded;
            _simConnect.OnDisconnected += (s, e) => _togglesApplied.Clear();
        }

        // ------------------------------------------------------------------ consulta

        public List<AircraftDefect> LoadOpenDefects(Guid aircraftId)
        {
            using var context = _contextFactory.CreateDbContext();
            return context.AircraftDefects.AsNoTracking()
                .Where(d => d.PilotAircraftId == aircraftId && !d.IsResolved)
                .OrderBy(d => d.DetectedAtUtc)
                .ToList();
        }

        // ------------------------------------------------------------------ ciclo de vida

        /// <summary>
        /// detectNative = true: voo monitorado (detecta e grava panes nativas).
        /// detectNative = false: modo vigia, fora do voo (avião em solo, NO-GO, recém-carregado no sim):
        /// só mantém as panes abertas e acompanha o banco para refletir reparos feitos na oficina.
        /// </summary>
        public void Start(PilotAircraft aircraft, Func<string> phaseProvider, bool detectNative = true)
        {
            bool sameAircraft = _aircraft?.Id == aircraft.Id;
            var previousOpen = sameAircraft ? _openDefects.ToList() : new List<AircraftDefect>();
            Stop();

            _aircraft = aircraft;
            _phaseProvider = phaseProvider;
            _detectNative = detectNative;
            _openDefects.Clear();
            _openDefects.AddRange(WithUnsaved(LoadOpenDefects(aircraft.Id), previousOpen));
            _lastDbSync = DateTime.Now;
            ReleaseResolved(previousOpen);
            _lastCorrection.Clear();
            _nativeFlapFailedById.Clear();
            _nativeFlapFailed = false;
            _previous = null;
            _suctionSeenHealthy = false;
            _suctionLowSince = null;
            _reapplyAfter = null;
            _expectEngineFailureUntil = DateTime.MinValue;

            _simConnect.OnFailureStateReceived += OnFailureState;
            _simConnect.OnMaskedEvent += OnMaskedEvent;
            if (_sensorFrames != null)
            {
                if (_detectNative) _nativeWear.Reset();
                _sensorFrames.OnFrame += OnSensorFrame;
            }
            _active = true;

            ApplyAll();
        }

        public void Stop()
        {
            if (!_active) return;
            _simConnect.OnFailureStateReceived -= OnFailureState;
            _simConnect.OnMaskedEvent -= OnMaskedEvent;
            if (_sensorFrames != null) _sensorFrames.OnFrame -= OnSensorFrame;
            _simConnect.SetMaskedEvents(Array.Empty<string>());
            _simConnect.SetPersistentToggles(Array.Empty<string>());
            _active = false;
        }

        /// <summary>
        /// O addon vai derrubar o motor por um efeito transitório (carb icing, stall de compressor):
        /// a borda de ENG FAILED que vem em seguida não é uma pane nativa nova.
        /// </summary>
        public void ExpectEngineFailureFromAddon()
        {
            _expectEngineFailureUntil = DateTime.Now.AddSeconds(5);
        }

        private List<AircraftDefect> WithUnsaved(List<AircraftDefect> fromDb, IEnumerable<AircraftDefect> inMemory)
        {
            var ids = fromDb.Select(d => d.Id).ToHashSet();
            fromDb.AddRange(inMemory.Where(d => _unsaved.Contains(d.Id) && !ids.Contains(d.Id)));
            return fromDb;
        }

        // ------------------------------------------------------------------ registro

        /// <summary>
        /// Grava uma pane. Retorna null se já existe pane aberta com o mesmo código (mesma pane, outra origem).
        /// </summary>
        public AircraftDefect? Record(string code, DefectOrigin origin, string cause, double? parameter = null,
            string? title = null, string? description = null, bool alreadyInjected = false)
        {
            if (_aircraft == null) return null;
            if (_openDefects.Any(d => d.Code == code)) return null;

            var spec = DefectCatalog.Get(code);
            double param = parameter ?? DefaultParameterFor(code);

            var defect = new AircraftDefect
            {
                Id = Guid.NewGuid(),
                PilotAircraftId = _aircraft.Id,
                Code = code,
                Origin = origin,
                Title = string.IsNullOrWhiteSpace(title) ? spec.Title : title,
                Description = description ?? string.Empty,
                Cause = cause,
                FlightPhase = _phaseProvider(),
                IsGrounding = spec.IsGrounding,
                RepairCost = DefectCatalog.CalculateRepairCost(code, _aircraft.AircraftType?.PurchasePrice ?? 0),
                Parameter = param,
                DetectedAtUtc = DateTime.UtcNow
            };

            _openDefects.Add(defect);
            _unsaved.Add(defect.Id);
            if (alreadyInjected) _togglesApplied.Add(code);

            // Gravar já (anti save-scum): se o sim cair agora, a pane continua lá
            _ = SaveAsync(defect);

            if (_active) ApplyAll();
            OnDefectRecorded?.Invoke(defect);
            return defect;
        }

        private double DefaultParameterFor(string code)
        {
            if ((code == DefectCatalog.FlapLock || code == DefectCatalog.FlapDamage) && _simConnect.CurrentFailureState is { } st)
                return Math.Round(st.FlapsHandleIndex);
            return 0;
        }

        private async Task SaveAsync(AircraftDefect defect)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.AircraftDefects.Add(defect);
                await context.SaveChangesAsync();
                _unsaved.Remove(defect.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Defects] Falha ao gravar pane {defect.Code}: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------ reaplicação e bloqueio

        private void ApplyAll()
        {
            if (!_active) return;

            // Chaves e comandos livres no simulador (sem mascaramento)
            _simConnect.SetMaskedEvents(Array.Empty<string>());

            // Toggles das panes abertas não são desfeitos pelo início de voo (ClearSimFailures)
            _simConnect.SetPersistentToggles(_openDefects
                .Select(d => DefectCatalog.Get(d.Code))
                .Where(s => s.Enforcement == DefectEnforcement.NativeToggle && s.ToggleEvent != null)
                .Select(s => s.ToggleEvent!));

            // Panes reaplicadas por evento nativo TOGGLE_* (ou desconexão de alternador via SetAlternatorConnected)
            foreach (var defect in _openDefects)
            {
                if (defect.Code == DefectCatalog.Alternator)
                {
                    _simConnect.SetAlternatorConnected(1, false);
                    continue;
                }

                var spec = DefectCatalog.Get(defect.Code);
                if (spec.Enforcement != DefectEnforcement.NativeToggle || spec.ToggleEvent == null) continue;
                if (_togglesApplied.Contains(defect.Code)) continue;

                // Motor: tem leitura de volta (ENG FAILED), então é mantido pela vigilância em Enforce().
                if (defect.Code == DefectCatalog.EngineFailure) continue;

                _simConnect.SetFailure(spec.ToggleEvent);
                _togglesApplied.Add(defect.Code);
                OnEnforcementAction?.Invoke($"Pane reaplicada no simulador: {defect.Title}");
            }

            if (_simConnect.CurrentFailureState is { } state) Enforce(state);
        }

        private void OnAircraftReloaded()
        {
            // O sim zerou as falhas. Reaplicar depois que a aeronave terminar de carregar.
            _togglesApplied.Clear();
            _previous = null;
            _suctionSeenHealthy = false;
            _suctionLowSince = null;
            _nativeFlapFailedById.Clear();
            _nativeFlapFailed = false;
            if (_active) _reapplyAfter = DateTime.Now + ReapplyDelayAfterLoad;
        }

        private void OnMaskedEvent(string simEvent, uint data)
        {
            // Comandos de alternador e flap não são mais mascarados (comandos do piloto permanecem livres)
        }

        // ------------------------------------------------------------------ vigilância contínua

        private void OnFailureState(SimFailureState state)
        {
            if (!_active) return;

            if (_reapplyAfter.HasValue && DateTime.Now >= _reapplyAfter.Value)
            {
                _reapplyAfter = null;
                ApplyAll();
            }

            if (!_detectNative && DateTime.Now - _lastDbSync >= WatchModeDbSyncInterval)
            {
                SyncWithDatabase();
            }

            Enforce(state);
            if (_detectNative) DetectNative(state);
            _previous = state;
        }

        /// <summary>Modo vigia: pega reparos feitos na oficina (e panes novas) gravados por outros contextos.</summary>
        private void SyncWithDatabase()
        {
            if (_aircraft == null) return;
            _lastDbSync = DateTime.Now;
            List<AircraftDefect> fresh;
            try { fresh = WithUnsaved(LoadOpenDefects(_aircraft.Id), _openDefects); }
            catch (Exception ex) { Debug.WriteLine($"[Defects] Falha ao sincronizar: {ex.Message}"); return; }

            var oldIds = _openDefects.Select(d => d.Id).ToHashSet();
            var newIds = fresh.Select(d => d.Id).ToHashSet();
            if (oldIds.SetEquals(newIds)) return;

            var previous = _openDefects.ToList();
            _openDefects.Clear();
            _openDefects.AddRange(fresh);
            ReleaseResolved(previous);
            ApplyAll();
        }

        /// <summary>
        /// Pane reparada na oficina: desfaz no simulador o que o addon tinha injetado por TOGGLE_*
        /// (a vigilância de magneto/flap/alternador simplesmente para de corrigir).
        /// </summary>
        private void ReleaseResolved(List<AircraftDefect> previous)
        {
            foreach (var old in previous)
            {
                if (_openDefects.Any(d => d.Code == old.Code)) continue;

                if (old.Code == DefectCatalog.Alternator)
                {
                    _simConnect.SetAlternatorConnected(1, true);
                    OnEnforcementAction?.Invoke($"Pane reparada, removida do simulador: {old.Title}");
                    continue;
                }

                var spec = DefectCatalog.Get(old.Code);
                if (spec.Enforcement != DefectEnforcement.NativeToggle || spec.ToggleEvent == null) continue;

                if (old.Code == DefectCatalog.EngineFailure)
                {
                    if (_simConnect.CurrentFailureState is { } st && st.EngineFailed > 0.5)
                        _simConnect.SendEvent(spec.ToggleEvent);
                }
                else if (_togglesApplied.Contains(old.Code))
                {
                    // Enviado por este serviço neste carregamento da aeronave: o toggle desfaz
                    _simConnect.SendEvent(spec.ToggleEvent);
                }
                _togglesApplied.Remove(old.Code);
                OnEnforcementAction?.Invoke($"Pane reparada, removida do simulador: {old.Title}");
            }
        }

        private void Enforce(SimFailureState state)
        {
            foreach (var defect in _openDefects)
            {
                switch (defect.Code)
                {
                    case DefectCatalog.MagnetoLeft when state.LeftMagneto > 0.5:
                        if (ShouldCorrect(defect.Code)) _simConnect.WriteMagneto(left: true, on: false);
                        break;

                    case DefectCatalog.MagnetoRight when state.RightMagneto > 0.5:
                        if (ShouldCorrect(defect.Code)) _simConnect.WriteMagneto(left: false, on: false);
                        break;

                    case DefectCatalog.FlapLock:
                    case DefectCatalog.FlapDamage:
                        double target = FlapSurfaceLock.TargetFraction(defect.Parameter, state.FlapsNumHandlePositions);
                        _simConnect.WriteFlapSurfacePercent(target);
                        break;

                    // Motor inoperante: o sim zera a falha ao reiniciar o voo/recarregar a aeronave.
                    // Se ele aparecer bom, reaplica (TOGGLE só quando a leitura confirma que não está falhado).
                    case DefectCatalog.EngineFailure when state.EngineFailed < 0.5:
                        if (ShouldCorrect(defect.Code, EngineReapplyThrottle))
                        {
                            // Leitura rápida confirma que não está falhado: o toggle não repara nada
                            _simConnect.SendEvent(DefectCatalog.Get(defect.Code).ToggleEvent!);
                            _togglesApplied.Add(defect.Code);
                            OnEnforcementAction?.Invoke($"Pane reaplicada no simulador: {defect.Title}");
                        }
                        break;
                }
            }
        }

        private bool ShouldCorrect(string key, TimeSpan? throttle = null)
        {
            var now = DateTime.Now;
            if (_lastCorrection.TryGetValue(key, out var last) && now - last < (throttle ?? CorrectionThrottle)) return false;
            _lastCorrection[key] = now;
            return true;
        }

        // ------------------------------------------------------------------ panes nativas

        private void DetectNative(SimFailureState s)
        {
            if (_previous is not { } p) return; // primeira leitura = referência, só bordas contam

            if (Rising(p.EngineFailed, s.EngineFailed))
            {
                if (DateTime.Now <= _expectEngineFailureUntil)
                {
                    _togglesApplied.Add(DefectCatalog.EngineFailure);
                }
                else
                {
                    Record(DefectCatalog.EngineFailure, DefectOrigin.Native,
                        "ENG FAILED:1 passou de 0 para 1 (falha do simulador)",
                        description: "O simulador reportou o motor #1 como falhado.", alreadyInjected: true);
                }
            }

            if (Rising(p.EngineOnFire, s.EngineOnFire))
                Record(DefectCatalog.EngineFire, DefectOrigin.Native, "ENG ON FIRE:1 ativo",
                    description: "Fogo no motor reportado pelo simulador.");

            // Motor falhado no sim lê 100% de dano (testado no MSFS 2024): isso é a mesma pane, não um dano parcial.
            bool engineFailedNow = s.EngineFailed >= 0.5 || _openDefects.Any(d => d.Code == DefectCatalog.EngineFailure)
                                   || DateTime.Now <= _expectEngineFailureUntil;
            if (!engineFailedNow && p.EngineDamagePercent < EngineDamageThresholdPercent && s.EngineDamagePercent >= EngineDamageThresholdPercent)
                Record(DefectCatalog.EngineDamage, DefectOrigin.Native,
                    $"GENERAL ENG DAMAGE PERCENT subiu para {s.EngineDamagePercent:F0}%", parameter: Math.Round(s.EngineDamagePercent, 1),
                    description: "Dano interno de motor acumulado no simulador.");

            if (Rising(p.FlapDamage, s.FlapDamage))
                Record(DefectCatalog.FlapDamage, DefectOrigin.Native, "FLAP DAMAGE BY SPEED (flaps estendidos acima da velocidade)",
                    parameter: Math.Round(s.FlapsHandleIndex),
                    description: "O dano de flap do simulador não degrada a aerodinâmica: o addon trava os flaps na posição do dano.");

            if (Rising(p.GearDamage, s.GearDamage))
                Record(DefectCatalog.GearDamage, DefectOrigin.Native, "GEAR DAMAGE BY SPEED (trem estendido acima da velocidade)",
                    description: "Trem de pouso danificado por excesso de velocidade.");

            double prevBrakes = Math.Min(p.BrakeLeftRemaining, p.BrakeRightRemaining);
            double brakes = Math.Min(s.BrakeLeftRemaining, s.BrakeRightRemaining);
            if (prevBrakes >= BrakesWornThresholdPercent && brakes < BrakesWornThresholdPercent)
                Record(DefectCatalog.BrakesWorn, DefectOrigin.Native, $"BRAKE WEIGHT REMAINING caiu para {brakes:F0}%",
                    parameter: Math.Round(brakes, 1), description: "Pastilhas de freio abaixo do limite de serviço.");

            if (p.CrashFlag < 0.5 && s.CrashFlag >= 0.5)
                OnNativeCrash?.Invoke(DescribeCrashFlag(s.CrashFlag));

            DetectVacuumByEffect(s);
        }

        /// <summary>
        /// Pane do wear and tear nativo (plano › 0.1, regra 5; F4.2c): a borda 0 → 1 de WEAR AND TEAR IS FAILED:&lt;id&gt;
        /// vira discrepância. Não passa por ENG FAILED nem GENERAL ENG DAMAGE PERCENT (F1.4c), por isso a leitura própria.
        /// Só em voo monitorado; o modo vigia não detecta panes novas.
        /// </summary>
        private void OnSensorFrame(SensorFrame frame)
        {
            if (frame.Values != null)
            {
                bool updated = false;
                for (int id = 5; id <= 8; id++)
                {
                    string key = $"{NativeWearFailureMap.SensorKeyPrefix}{id}";
                    if (frame.Values.TryGetValue(key, out double val) && !double.IsNaN(val))
                    {
                        _nativeFlapFailedById[id] = val >= 0.5;
                        updated = true;
                    }
                }
                if (updated)
                {
                    _nativeFlapFailed = _nativeFlapFailedById.Values.Any(f => f);
                }
            }

            if (!_active || !_detectNative || _aircraft == null || frame.Values == null) return;

            foreach (var e in _nativeWear.Update(frame.Values))
            {
                if (e.DefectCode == null)
                {
                    OnEnforcementAction?.Invoke($"Wear and tear do simulador: {e.NativeName} (ID {e.ComponentId}) quebrado, só registro");
                    continue;
                }

                var defect = Record(e.DefectCode, DefectOrigin.Native, NativeWearFailureMap.CauseFor(e.ComponentId, e.NativeName),
                    description: "O desgaste do próprio simulador (wear and tear) quebrou esta peça.");
                if (defect == null)
                    OnEnforcementAction?.Invoke($"Wear and tear do simulador: {e.NativeName} (ID {e.ComponentId}) quebrado; pane {e.DefectCode} já aberta");
            }
        }

        /// <summary>
        /// Panes do menu de falhas não têm variável de leitura: detectar pelo efeito.
        /// Só vale para aeronaves que já mostraram sucção normal neste carregamento (glass cockpits ficam sempre em 0).
        /// </summary>
        private void DetectVacuumByEffect(SimFailureState s)
        {
            bool running = s.EngineCombustion > 0.5;
            if (running && s.SuctionPressure >= SuctionHealthyInHg) _suctionSeenHealthy = true;

            if (_suctionSeenHealthy && running && s.SuctionPressure < SuctionFailedInHg)
            {
                _suctionLowSince ??= DateTime.Now;
                if (DateTime.Now - _suctionLowSince.Value >= SuctionLowConfirmation)
                {
                    Record(DefectCatalog.Vacuum, DefectOrigin.Native,
                        $"SUCTION PRESSURE {s.SuctionPressure:F1} inHg com motor funcionando",
                        description: "Perda de vácuo detectada pelo efeito (falha do menu do simulador).", alreadyInjected: true);
                    _suctionLowSince = null;
                }
            }
            else
            {
                _suctionLowSince = null;
            }
        }

        private static bool Rising(double before, double now) => before < 0.5 && now >= 0.5;

        private static string DescribeCrashFlag(double flag) => (int)Math.Round(flag) switch
        {
            2 => "Colisão com montanha",
            4 => "Acidente (geral)",
            6 => "Colisão com prédio",
            8 => "Pouso/impacto na água",
            10 => "Pouso de barriga (trem recolhido)",
            11 => "Sobrecarga estrutural",
            12 => "Colisão com prédio",
            13 => "Colisão com aeronave",
            14 => "Colisão com caminhão de combustível",
            _ => $"Acidente (CRASH FLAG {flag:F0})"
        };
    }
}
