using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Evento discreto derivado a partir do stream de telemetria/sensores de voo.
    /// </summary>
    public sealed record DerivedPartEvent(string MetricKey, double Value, int? EngineIndex);

    /// <summary>
    /// Deriva eventos discretos operacionais (partida de motor, tempo de starter, ciclos de trem, ciclo de voo,
    /// picos de tensão em aviônicos, descarga profunda de bateria e sobrecarga elétrica)
    /// a partir das leituras contínuas de SensorFrame e tempo simulado decorrido.
    /// Lógica pura de Domain, sem EF nem SimConnect.
    /// </summary>
    public sealed class PartEventDeriver
    {
        private readonly Dictionary<int, bool> _wasCombustion = new();
        private readonly Dictionary<int, bool> _wasStarter = new();
        private readonly Dictionary<int, double> _starterAccumulated = new();

        private bool _gearWasRetracted;
        private bool _wasInAir;
        private bool _flightCycleEmitted;

        // Configuração e estado do subsistema elétrico (F2.4b-CAL)
        private ElectricalConfig? _electricalConfig;
        private int _avionicsStartSpikesCount;

        private double _batteryDeepDischargeSec;
        private double _batteryDeepDischargeOffSec;
        private readonly HashSet<double> _batteryThresholdsEmitted = new();

        private double _overloadSec;
        private double _overloadOffSec;
        private readonly HashSet<double> _overloadThresholdsEmitted = new();

        private readonly List<string> _diagnosticMessages = new();
        private readonly HashSet<string> _diagnosticKeysReported = new(StringComparer.Ordinal);

        public IReadOnlyList<string> DiagnosticMessages => _diagnosticMessages;

        /// <summary>
        /// Devolve os diagnósticos ainda não entregues e os remove da fila. As chaves já reportadas
        /// continuam marcadas, então cada mensagem sai uma vez por voo (até o Reset).
        /// </summary>
        public IReadOnlyList<string> DrainDiagnostics()
        {
            if (_diagnosticMessages.Count == 0) return Array.Empty<string>();
            var drained = _diagnosticMessages.ToList();
            _diagnosticMessages.Clear();
            return drained;
        }

        public void ConfigureElectrical(ElectricalConfig? config)
        {
            _electricalConfig = config;
        }

        public void Reset()
        {
            _wasCombustion.Clear();
            _wasStarter.Clear();
            _starterAccumulated.Clear();
            _gearWasRetracted = false;
            _wasInAir = false;
            _flightCycleEmitted = false;

            _avionicsStartSpikesCount = 0;
            _batteryDeepDischargeSec = 0.0;
            _batteryDeepDischargeOffSec = 0.0;
            _batteryThresholdsEmitted.Clear();

            _overloadSec = 0.0;
            _overloadOffSec = 0.0;
            _overloadThresholdsEmitted.Clear();

            _diagnosticMessages.Clear();
            _diagnosticKeysReported.Clear();
        }

        /// <summary>
        /// Processa o frame atual e tempo simulado, emitindo os eventos operacionais decorrentes.
        /// </summary>
        public IReadOnlyList<DerivedPartEvent> Update(SensorFrame frame, double dtSimSeconds, IReadOnlyList<string>? features)
        {
            var events = new List<DerivedPartEvent>();
            if (frame == null) return events;

            var featSet = new HashSet<string>(features ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            bool isTurbine = featSet.Contains(AircraftFeatures.Turboprop) ||
                             featSet.Contains(AircraftFeatures.Jet) ||
                             featSet.Contains("turbine") ||
                             featSet.Contains("turbofan");
            bool isRetractableGear = featSet.Contains(AircraftFeatures.RetractableGear) ||
                                     featSet.Contains("retractableGear");

            // 1. Partida de motores e tempo de starter (motores 1 a 4)
            for (int eng = 1; eng <= 4; eng++)
            {
                bool hasData = TryGetEngineValues(frame, eng, out double combustion, out double starter);
                if (!hasData && eng > 1) continue;

                bool curCombustion = combustion > 0.5;
                bool curStarter = starter > 0.5;

                _wasCombustion.TryGetValue(eng, out bool prevCombustion);
                _wasStarter.TryGetValue(eng, out bool prevStarter);

                // Borda 0 -> 1 do starter para qualquer motor: checa partida com aviônicos ligados (Regra 1)
                if (!prevStarter && curStarter)
                {
                    CheckAvionicsStartWithMasterOn(frame, eng, events);
                }

                // Borda de combustão 0 -> 1 com starter engatado no frame anterior ou no atual
                if (!prevCombustion && curCombustion && (prevStarter || curStarter))
                {
                    string startKey = isTurbine ? WearRuleMetrics.TurbineStartCycle : WearRuleMetrics.EngineStartCycle;
                    events.Add(new DerivedPartEvent(startKey, 1.0, eng));
                }

                // Segundos acumulados de starter (tempo simulado)
                if (curStarter)
                {
                    double acc = _starterAccumulated.GetValueOrDefault(eng, 0.0) + Math.Max(0.0, dtSimSeconds);
                    _starterAccumulated[eng] = acc;
                }
                else if (prevStarter && !curStarter)
                {
                    double totalSec = _starterAccumulated.GetValueOrDefault(eng, 0.0);
                    if (totalSec > 0.01)
                    {
                        events.Add(new DerivedPartEvent(WearRuleMetrics.StarterSeconds, totalSec, eng));
                    }
                    _starterAccumulated[eng] = 0.0;
                }

                _wasCombustion[eng] = curCombustion;
                _wasStarter[eng] = curStarter;
            }

            // 2. Regras elétricas contínuas (Regra 2: Descarga Profunda, Regra 3: Sobrecarga de Amperagem)
            CheckBatteryDeepDischarge(frame, dtSimSeconds, events);
            CheckElectricalOverload(frame, dtSimSeconds, events);

            // 3. Ciclos de trem de pouso retrátil (recolhido e baixado de novo)
            if (isRetractableGear)
            {
                if (TryGetGearPosition(frame, out double gearPos))
                {
                    bool isUp = gearPos <= 10.0;
                    bool isDown = gearPos >= 90.0;

                    if (isUp)
                    {
                        _gearWasRetracted = true;
                    }
                    else if (_gearWasRetracted && isDown)
                    {
                        _gearWasRetracted = false;
                        events.Add(new DerivedPartEvent(WearRuleMetrics.GearCycles, 1.0, null));
                    }
                }
            }

            // 4. Ciclo de voo (decolagem até pouso, emitido no primeiro pouso após estar no ar)
            if (TryGetFlightPhase(frame, out bool onGround))
            {
                if (!onGround)
                {
                    _wasInAir = true;
                }
                else if (_wasInAir && !_flightCycleEmitted)
                {
                    _flightCycleEmitted = true;
                    events.Add(new DerivedPartEvent(WearRuleMetrics.FlightCycles, 1.0, null));
                }
            }

            return events;
        }

        private void CheckAvionicsStartWithMasterOn(SensorFrame frame, int eng, List<DerivedPartEvent> events)
        {
            if (_electricalConfig == null || !_electricalConfig.AvionicsCircuitIndex.HasValue || _electricalConfig.AvionicsCircuitIndex.Value <= 0)
            {
                ReportDiagnosticOnce("elec-avionics-missing-param",
                    "Regra 'avionics_start_with_avionics_master_on' desativada: elecAvionicsCircuitIndex ausente no catálogo da aeronave.");
                return;
            }

            int circuitIdx = _electricalConfig.AvionicsCircuitIndex.Value;
            string circuitKey = $"elec-circuit-on-{circuitIdx}";

            if (!frame.Values.TryGetValue(circuitKey, out double circuitOn))
            {
                ReportDiagnosticOnce($"elec-circuit-missing-sensor-{circuitIdx}",
                    $"Regra 'avionics_start_with_avionics_master_on' desativada: SimVar '{circuitKey}' ausente no frame de sensores.");
                return;
            }

            if (circuitOn >= 0.5)
            {
                _avionicsStartSpikesCount++;
                events.Add(new DerivedPartEvent(WearRuleMetrics.AvionicsMasterStartSpikes, _avionicsStartSpikesCount, eng));
            }
        }

        private void CheckBatteryDeepDischarge(SensorFrame frame, double dtSimSeconds, List<DerivedPartEvent> events)
        {
            if (_electricalConfig == null || !_electricalConfig.MainBusIndex.HasValue || _electricalConfig.MainBusIndex.Value <= 0 ||
                !_electricalConfig.SystemVolts.HasValue || _electricalConfig.SystemVolts.Value <= 0.0)
            {
                ReportDiagnosticOnce("elec-deep-discharge-missing-param",
                    "Regra 'battery_deep_discharge_damage' desativada: elecMainBusIndex ou elecSystemVolts ausente no catálogo da aeronave.");
                return;
            }

            int busIdx = _electricalConfig.MainBusIndex.Value;
            string busKey = $"elec-bus-voltage-{busIdx}";

            if (!frame.Values.TryGetValue("elec-master-battery", out double masterBat) ||
                !frame.Values.TryGetValue(busKey, out double busVolts))
            {
                ReportDiagnosticOnce($"elec-battery-missing-sensor-{busIdx}",
                    $"Regra 'battery_deep_discharge_damage' desativada: 'elec-master-battery' ou '{busKey}' ausente no frame de sensores.");
                return;
            }

            double systemVolts = _electricalConfig.SystemVolts.Value;
            double deepDischargeVolts = _electricalConfig.DeepDischargeVolts ?? (systemVolts * (22.0 / 28.0));

            bool isDeepDischarge = masterBat >= 0.5 && busVolts >= 1.0 && busVolts <= deepDischargeVolts;

            if (isDeepDischarge)
            {
                _batteryDeepDischargeSec += Math.Max(0.0, dtSimSeconds);
                _batteryDeepDischargeOffSec = 0.0;
                double accumulatedMinutes = _batteryDeepDischargeSec / 60.0;

                var thresholds = _electricalConfig.BatteryDeepDischargeThresholds ?? new[] { 10.0, 25.0, 45.0 };
                foreach (var th in thresholds.OrderBy(t => t))
                {
                    if (accumulatedMinutes >= th && !_batteryThresholdsEmitted.Contains(th))
                    {
                        _batteryThresholdsEmitted.Add(th);
                        events.Add(new DerivedPartEvent(WearRuleMetrics.BatteryDeepDischargeMinutes, th, null));
                    }
                }
            }
            else
            {
                _batteryDeepDischargeOffSec += Math.Max(0.0, dtSimSeconds);
                if (_batteryDeepDischargeOffSec >= 10.0)
                {
                    _batteryDeepDischargeSec = 0.0;
                    _batteryThresholdsEmitted.Clear();
                    _batteryDeepDischargeOffSec = 0.0;
                }
            }
        }

        private void CheckElectricalOverload(SensorFrame frame, double dtSimSeconds, List<DerivedPartEvent> events)
        {
            if (_electricalConfig == null || !_electricalConfig.AlternatorRatedAmps.HasValue || _electricalConfig.AlternatorRatedAmps.Value <= 0.0)
            {
                ReportDiagnosticOnce("elec-overload-missing-amps",
                    "Regra 'electrical_system_overload_amps' desativada: alternatorRatedAmps ausente no catálogo da aeronave (capacidade POH necessária).");
                return;
            }

            if (!_electricalConfig.MainBusIndex.HasValue || _electricalConfig.MainBusIndex.Value <= 0 ||
                !_electricalConfig.SystemVolts.HasValue || _electricalConfig.SystemVolts.Value <= 0.0)
            {
                ReportDiagnosticOnce("elec-overload-missing-bus",
                    "Regra 'electrical_system_overload_amps' desativada: elecMainBusIndex ou elecSystemVolts ausente no catálogo da aeronave.");
                return;
            }

            int busIdx = _electricalConfig.MainBusIndex.Value;
            string busKey = $"elec-bus-voltage-{busIdx}";

            if (!frame.Values.TryGetValue("elec-total-load-amps", out double totalLoadAmps) ||
                !frame.Values.TryGetValue(busKey, out double busVolts))
            {
                ReportDiagnosticOnce($"elec-overload-missing-sensor-{busIdx}",
                    $"Regra 'electrical_system_overload_amps' desativada: 'elec-total-load-amps' ou '{busKey}' ausente no frame de sensores.");
                return;
            }

            bool anyStarter = false;
            bool anyAlternatorAndCombustion = false;
            bool hasAnyAltKey = frame.Values.Keys.Any(k => k.StartsWith("elec-master-alternator", StringComparison.OrdinalIgnoreCase));

            for (int eng = 1; eng <= 4; eng++)
            {
                if (TryGetEngineValues(frame, eng, out double combVal, out double starterVal))
                {
                    if (starterVal > 0.5) anyStarter = true;

                    bool alt = false;
                    if (frame.Values.TryGetValue($"elec-master-alternator:{eng}", out double aVal) ||
                        (eng == 1 && frame.Values.TryGetValue("elec-master-alternator", out aVal)))
                    {
                        alt = aVal > 0.5;
                    }

                    if (combVal > 0.5 && (alt || !hasAnyAltKey))
                    {
                        anyAlternatorAndCombustion = true;
                    }
                }
            }

            double systemVolts = _electricalConfig.SystemVolts.Value;
            double ratedAmps = _electricalConfig.AlternatorRatedAmps.Value;

            // Heurística de Sobrecarga Elétrica Real (Decisão FIX-REPLAY-BUGS):
            // 1. O alternador deve estar ligado e com o motor em combustão (geração de energia ativa).
            // 2. Não deve estar em partida (exclui os picos normais de 150-170A do starter).
            // 3. A corrente total demandada deve exceder a capacidade nominal do alternador (elec-total-load-amps > alternatorRatedAmps).
            // 4. A tensão no barramento principal (elecMainBusIndex) deve sofrer queda ("voltage sag") para a faixa [0.88 * systemVolts, 0.95 * systemVolts):
            //    - Tensão estável (>= 0.95 * systemVolts, ex: >= 26.6V em sistema 28V): o alternador está conseguindo suprir a rede
            //      sem sobreaquecimento/estresse térmico grave, ou o simulador está reportando cargas virtuais sem impacto físico.
            //    - Tensão colapsada (< 0.88 * systemVolts, ex: < 24.64V em sistema 28V): a tensão caiu para o patamar da bateria isolada (~24V),
            //      indicando que o alternador desarmou, queimou ou foi desligado (bateria sozinha descarregando, coberta pela regra de descarga profunda).
            //    - Faixa intermediária [0.88, 0.95): alternador ativo porém operando em sobrecarga térmica real, incapaz de manter a tensão regulada.
            double vSagMin = 0.88 * systemVolts;
            double vSagMax = 0.95 * systemVolts;
            bool voltageSag = busVolts >= vSagMin && busVolts < vSagMax;

            bool isOverload = anyAlternatorAndCombustion && !anyStarter && (totalLoadAmps > ratedAmps) && voltageSag;

            if (isOverload)
            {
                _overloadSec += Math.Max(0.0, dtSimSeconds);
                _overloadOffSec = 0.0;
                double accumulatedMinutes = _overloadSec / 60.0;

                var thresholds = _electricalConfig.ElectricalOverloadThresholds ?? new[] { 5.0, 15.0, 30.0 };
                foreach (var th in thresholds.OrderBy(t => t))
                {
                    if (accumulatedMinutes >= th && !_overloadThresholdsEmitted.Contains(th))
                    {
                        _overloadThresholdsEmitted.Add(th);
                        events.Add(new DerivedPartEvent(WearRuleMetrics.ElectricalOverloadMinutes, th, null));
                    }
                }
            }
            else
            {
                _overloadOffSec += Math.Max(0.0, dtSimSeconds);
                if (_overloadOffSec >= 10.0)
                {
                    _overloadSec = 0.0;
                    _overloadThresholdsEmitted.Clear();
                    _overloadOffSec = 0.0;
                }
            }
        }

        private void ReportDiagnosticOnce(string key, string message)
        {
            if (_diagnosticKeysReported.Add(key))
            {
                _diagnosticMessages.Add(message);
            }
        }

        private static bool TryGetEngineValues(SensorFrame frame, int eng, out double combustion, out double starter)
        {
            combustion = 0.0;
            starter = 0.0;
            bool foundAny = false;

            if (frame.Values.TryGetValue($"eng-combustion:{eng}", out combustion) ||
                (eng == 1 && frame.Values.TryGetValue("eng-combustion", out combustion)))
            {
                foundAny = true;
            }

            if (frame.Values.TryGetValue($"eng-starter:{eng}", out starter) ||
                frame.Values.TryGetValue($"eng-starter-active:{eng}", out starter) ||
                (eng == 1 && (frame.Values.TryGetValue("eng-starter", out starter) ||
                              frame.Values.TryGetValue("eng-starter-active", out starter))))
            {
                foundAny = true;
            }

            return foundAny;
        }

        private static bool TryGetGearPosition(SensorFrame frame, out double position)
        {
            if (frame.Values.TryGetValue("gear-handle-position", out position) ||
                frame.Values.TryGetValue("gear-position", out position) ||
                frame.Values.TryGetValue("gear-total-pct-extended", out position))
            {
                return true;
            }

            position = 0.0;
            return false;
        }

        private static bool TryGetFlightPhase(SensorFrame frame, out bool onGround)
        {
            if (frame.Values.TryGetValue("ctx-sim-on-ground", out double simOnGround) ||
                frame.Values.TryGetValue("sim-on-ground", out simOnGround))
            {
                onGround = simOnGround >= 0.5;
                return true;
            }

            // Fallback para on-ground por ponto de contato / roda (qualquer roda = 1 -> no solo)
            bool foundOnGroundKey = false;
            bool anyWheelOnGround = false;
            foreach (var kvp in frame.Values)
            {
                if (kvp.Key.StartsWith("gear-contact-point-on-ground", StringComparison.OrdinalIgnoreCase))
                {
                    foundOnGroundKey = true;
                    if (kvp.Value >= 0.5)
                    {
                        anyWheelOnGround = true;
                    }
                }
            }

            if (foundOnGroundKey)
            {
                onGround = anyWheelOnGround;
                return true;
            }

            // Fallback para compressão de amortecedor se não houver sim-on-ground
            bool foundCompression = false;
            double maxComp = 0.0;
            foreach (var kvp in frame.Values)
            {
                if (kvp.Key.StartsWith("gear-contact-point-compression", StringComparison.OrdinalIgnoreCase))
                {
                    foundCompression = true;
                    if (kvp.Value > maxComp) maxComp = kvp.Value;
                }
            }

            if (foundCompression)
            {
                onGround = maxComp > 0.02;
                return true;
            }

            // Fallback para velocidade de toque normal
            if (frame.Values.TryGetValue("gear-plane-touchdown-normal-velocity", out double tdVel))
            {
                onGround = tdVel > 0.1;
                return true;
            }

            onGround = true;
            return false;
        }
    }
}
