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
        private EngineLimitsConfig? _engineLimitsConfig;
        private int _avionicsStartSpikesCount;

        private double _batteryDeepDischargeSec;
        private double _batteryDeepDischargeOffSec;
        private readonly HashSet<double> _batteryThresholdsEmitted = new();

        private double _overloadSec;
        private double _overloadOffSec;
        private readonly HashSet<double> _overloadThresholdsEmitted = new();

        // 1. Freio (brakeEnergyPctRef)
        private bool _isBrakingOccurrence;
        private double _brakeEnergySum;
        private double _prevBrakeVelocityMs;
        private double _brakeLowTimeSec;

        // 2. Curva rápida em táxi (taxiTurnSpeedKnots)
        private bool _isTaxiTurnOccurrence;
        private double _taxiTurnMaxGs;
        private double _taxiTurnLowTimeSec;

        // 3. Derrapagem de pneus no solo (skidSeconds)
        private bool _isSkidOccurrence;
        private double _skidAccumulatedSec;
        private double _skidOffTimeSec;

        // 5. Alta potência com óleo frio (highPowerColdOilMinutes) por motor
        private readonly Dictionary<int, double> _coldOilSecByEngine = new();
        private readonly Dictionary<int, double> _coldOilOffSecByEngine = new();
        private readonly Dictionary<int, HashSet<double>> _coldOilThresholdsByEngine = new();

        // 6. Ciclos de compensador de profundor (trimCycles)
        private double? _trimAnchor;
        private double? _trimDirection; // +1.0 ou -1.0
        private int _trimCycleCount;
        private readonly HashSet<double> _trimThresholdsEmitted = new();

        // 7. Turbulência com AP (turbulenceGExceedanceCycles)
        private bool _gExceedanceActive;
        private int _gExceedanceCycleCount;
        private readonly HashSet<double> _gExceedanceThresholdsEmitted = new();

        // 8. Transientes rápidos de torque turboélice (rapidPowerTransientCycles) por motor
        private readonly Dictionary<int, Queue<(double TimeSec, double Torque)>> _torqueHistoryByEngine = new();
        private double _transientSimTimeSec;
        private readonly Dictionary<int, int> _transientCycleCountByEngine = new();
        private readonly Dictionary<int, HashSet<double>> _transientThresholdsByEngine = new();
        private readonly Dictionary<int, double> _transientCooldownByEngine = new();

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

        public void ConfigureEngine(EngineLimitsConfig? config)
        {
            _engineLimitsConfig = config;
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

            _isBrakingOccurrence = false;
            _brakeEnergySum = 0.0;
            _prevBrakeVelocityMs = 0.0;
            _brakeLowTimeSec = 0.0;

            _isTaxiTurnOccurrence = false;
            _taxiTurnMaxGs = 0.0;
            _taxiTurnLowTimeSec = 0.0;

            _isSkidOccurrence = false;
            _skidAccumulatedSec = 0.0;
            _skidOffTimeSec = 0.0;

            _coldOilSecByEngine.Clear();
            _coldOilOffSecByEngine.Clear();
            _coldOilThresholdsByEngine.Clear();

            _trimAnchor = null;
            _trimDirection = null;
            _trimCycleCount = 0;
            _trimThresholdsEmitted.Clear();

            _gExceedanceActive = false;
            _gExceedanceCycleCount = 0;
            _gExceedanceThresholdsEmitted.Clear();

            _torqueHistoryByEngine.Clear();
            _transientSimTimeSec = 0.0;
            _transientCycleCountByEngine.Clear();
            _transientThresholdsByEngine.Clear();
            _transientCooldownByEngine.Clear();

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

                    // Regra nova engine_start_electrical_load (métrica startLightsOnCount)
                    if (!featSet.Contains(AircraftFeatures.NoElectrical) && !featSet.Contains("noElectrical"))
                    {
                        CheckStartLightsOnLoad(frame, eng, events);
                    }
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

            // 5. Novas métricas e regras da spec METRICAS-A
            CheckBrakeEnergy(frame, dtSimSeconds, onGround, events);
            CheckTaxiTurnSpeed(frame, dtSimSeconds, onGround, featSet, events);
            CheckGroundTireSkid(frame, dtSimSeconds, onGround, events);
            CheckHighPowerColdOil(frame, dtSimSeconds, featSet, isTurbine, events);
            CheckTrimCycles(frame, onGround, events);
            CheckTurbulenceGExceedance(frame, onGround, events);
            CheckRapidPowerTransient(frame, dtSimSeconds, featSet, events);

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

        private void CheckStartLightsOnLoad(SensorFrame frame, int eng, List<DerivedPartEvent> events)
        {
            int lightsCount = 0;
            if (frame.Values.TryGetValue("light-landing-on", out double landing) && landing >= 0.5) lightsCount++;
            if (frame.Values.TryGetValue("light-taxi-on", out double taxi) && taxi >= 0.5) lightsCount++;
            if (frame.Values.TryGetValue("light-strobe-on", out double strobe) && strobe >= 0.5) lightsCount++;

            if (lightsCount >= 1)
            {
                events.Add(new DerivedPartEvent(WearRuleMetrics.StartLightsOnCount, lightsCount, eng));
            }
        }

        private void CheckBrakeEnergy(SensorFrame frame, double dtSimSeconds, bool onGround, List<DerivedPartEvent> events)
        {
            if (!TryGetGroundSpeed(frame, out double gsKnots))
            {
                ReportDiagnosticOnce("brake-missing-gs", "Regra 'brake_high_energy_antiskid' desativada: velocidade de solo ausente no frame de sensores.");
                return;
            }

            if (!frame.Values.TryGetValue("brake-left-position", out double bLeft) ||
                !frame.Values.TryGetValue("brake-right-position", out double bRight))
            {
                ReportDiagnosticOnce("brake-missing-pos", "Regra 'brake_high_energy_antiskid' desativada: posição de freios ausente no frame de sensores.");
                return;
            }

            frame.Values.TryGetValue("brake-parking-position", out double parkingBrake);
            bool hasParkingBrake = parkingBrake >= 0.5;

            // Posição média normalizada (0..32767 -> 0..1)
            double brakeLeftNorm = Math.Clamp(bLeft / 32767.0, 0.0, 1.0);
            double brakeRightNorm = Math.Clamp(bRight / 32767.0, 0.0, 1.0);
            double brakeAvg = (brakeLeftNorm + brakeRightNorm) / 2.0;

            double currentVelocityMs = gsKnots * 0.5144;

            if (!_isBrakingOccurrence)
            {
                if (onGround && gsKnots > 5.0 && brakeAvg >= 0.30 && !hasParkingBrake)
                {
                    _isBrakingOccurrence = true;
                    _brakeEnergySum = 0.0;
                    _prevBrakeVelocityMs = currentVelocityMs;
                    _brakeLowTimeSec = 0.0;
                }
            }
            else
            {
                // Durante a ocorrência: se no solo e freio aplicado, computar deceleração
                if (currentVelocityMs < _prevBrakeVelocityMs)
                {
                    double deltaKinetic = (_prevBrakeVelocityMs * _prevBrakeVelocityMs - currentVelocityMs * currentVelocityMs) / 2.0;
                    _brakeEnergySum += deltaKinetic;
                }
                _prevBrakeVelocityMs = currentVelocityMs;

                bool isLowBrake = brakeAvg < 0.10;
                if (isLowBrake)
                {
                    _brakeLowTimeSec += Math.Max(0.0, dtSimSeconds);
                }
                else
                {
                    _brakeLowTimeSec = 0.0;
                }

                bool endOccurrence = !onGround || hasParkingBrake || gsKnots < 2.0 || _brakeLowTimeSec >= 2.0;
                if (endOccurrence)
                {
                    // Determinar Vs0
                    double vs0Knots = 50.0;
                    if (frame.Values.TryGetValue("af-design-speed-vs0", out double vs0Val) && vs0Val > 0.0)
                    {
                        vs0Knots = vs0Val;
                    }
                    else
                    {
                        ReportDiagnosticOnce("brake-vs0-default", "Regra 'brake_high_energy_antiskid': af-design-speed-vs0 ausente ou <= 0; usando 50 kt como referência.");
                    }

                    double vs0Ms = vs0Knots * 0.5144;
                    double refEnergy = Math.Pow(1.3 * vs0Ms, 2) / 2.0;
                    double pctRef = refEnergy > 0 ? (100.0 * _brakeEnergySum / refEnergy) : 0.0;

                    if (pctRef >= 60.0)
                    {
                        events.Add(new DerivedPartEvent(WearRuleMetrics.BrakeEnergyPctRef, pctRef, null));
                    }

                    _isBrakingOccurrence = false;
                    _brakeEnergySum = 0.0;
                    _prevBrakeVelocityMs = 0.0;
                    _brakeLowTimeSec = 0.0;
                }
            }
        }

        private void CheckTaxiTurnSpeed(SensorFrame frame, double dtSimSeconds, bool onGround, HashSet<string> features, List<DerivedPartEvent> events)
        {
            if (!features.Contains("noseWheelSteering") && !features.Contains(AircraftFeatures.NoseWheelSteering))
            {
                return;
            }

            if (!frame.Values.TryGetValue("gear-steer-angle-pct", out double steerAnglePct))
            {
                ReportDiagnosticOnce("taxi-steer-missing-sensor", "Regra 'taxi_high_speed_turn' desativada: SimVar 'gear-steer-angle-pct' ausente no frame de sensores.");
                return;
            }

            if (!TryGetGroundSpeed(frame, out double gsKnots))
            {
                ReportDiagnosticOnce("taxi-steer-missing-gs", "Regra 'taxi_high_speed_turn' desativada: velocidade de solo ausente no frame de sensores.");
                return;
            }

            double absSteer = Math.Abs(steerAnglePct);

            if (!_isTaxiTurnOccurrence)
            {
                if (onGround && gsKnots < 40.0 && absSteer >= 30.0)
                {
                    _isTaxiTurnOccurrence = true;
                    _taxiTurnMaxGs = gsKnots;
                    _taxiTurnLowTimeSec = 0.0;
                }
            }
            else
            {
                if (gsKnots > _taxiTurnMaxGs)
                {
                    _taxiTurnMaxGs = gsKnots;
                }

                if (absSteer < 15.0)
                {
                    _taxiTurnLowTimeSec += Math.Max(0.0, dtSimSeconds);
                }
                else
                {
                    _taxiTurnLowTimeSec = 0.0;
                }

                bool endOccurrence = !onGround || gsKnots < 3.0 || _taxiTurnLowTimeSec >= 2.0;
                if (endOccurrence)
                {
                    if (_taxiTurnMaxGs >= 15.0)
                    {
                        events.Add(new DerivedPartEvent(WearRuleMetrics.TaxiTurnSpeedKnots, _taxiTurnMaxGs, null));
                    }

                    _isTaxiTurnOccurrence = false;
                    _taxiTurnMaxGs = 0.0;
                    _taxiTurnLowTimeSec = 0.0;
                }
            }
        }

        private void CheckGroundTireSkid(SensorFrame frame, double dtSimSeconds, bool onGround, List<DerivedPartEvent> events)
        {
            if (!frame.Values.TryGetValue("gear-is-skidding", out double isSkidding))
            {
                ReportDiagnosticOnce("skid-missing-sensor", "Regra 'ground_tire_skid' desativada: SimVar 'gear-is-skidding' ausente no frame de sensores.");
                return;
            }

            if (!TryGetGroundSpeed(frame, out double gsKnots))
            {
                ReportDiagnosticOnce("skid-missing-gs", "Regra 'ground_tire_skid' desativada: velocidade de solo ausente no frame de sensores.");
                return;
            }

            bool skidding = onGround && gsKnots > 3.0 && isSkidding >= 0.5;

            if (skidding)
            {
                _isSkidOccurrence = true;
                _skidAccumulatedSec += Math.Max(0.0, dtSimSeconds);
                _skidOffTimeSec = 0.0;
            }
            else if (_isSkidOccurrence)
            {
                _skidOffTimeSec += Math.Max(0.0, dtSimSeconds);
                if (_skidOffTimeSec >= 3.0)
                {
                    if (_skidAccumulatedSec >= 1.0)
                    {
                        events.Add(new DerivedPartEvent(WearRuleMetrics.SkidSeconds, _skidAccumulatedSec, null));
                    }

                    _isSkidOccurrence = false;
                    _skidAccumulatedSec = 0.0;
                    _skidOffTimeSec = 0.0;
                }
            }
        }

        private void CheckHighPowerColdOil(SensorFrame frame, double dtSimSeconds, HashSet<string> features, bool isTurbine, List<DerivedPartEvent> events)
        {
            int engineCount = DetectEngineCount(frame);

            for (int eng = 1; eng <= engineCount; eng++)
            {
                if (!TryGetEngineValues(frame, eng, out double combustion, out _))
                {
                    if (eng > 1) continue;
                }

                bool isRunning = combustion > 0.5;

                // Temperatura do óleo
                double oilTemp;
                bool hasOil = frame.Values.TryGetValue($"oil-temperature:{eng}", out oilTemp) ||
                              (eng == 1 && frame.Values.TryGetValue("oil-temperature", out oilTemp));

                if (!hasOil)
                {
                    ReportDiagnosticOnce($"cold-oil-missing-sensor-{eng}", $"Regra 'engine_high_power_cold_oil' desativada para motor {eng}: sensor 'oil-temperature' ausente.");
                    continue;
                }

                // Limite mínimo de temperatura do óleo
                double minOilTemp;
                if (_engineLimitsConfig?.OilTempMin.HasValue == true)
                {
                    minOilTemp = _engineLimitsConfig.OilTempMin.Value;
                }
                else
                {
                    minOilTemp = isTurbine ? 0.0 : 24.0;
                    ReportDiagnosticOnce($"cold-oil-default-limit-{eng}",
                        $"Regra 'engine_high_power_cold_oil': oilTempMin ausente no catálogo; usando padrão {minOilTemp:F0} °C.");
                }

                // Condição de alta potência
                bool isHighPower = false;
                if (!isTurbine)
                {
                    double rpm = 0.0;
                    bool hasRpm = frame.Values.TryGetValue($"eng-rpm:{eng}", out rpm) ||
                                  (eng == 1 && frame.Values.TryGetValue("eng-rpm", out rpm));

                    double throttle = 0.0;
                    bool hasThrottle = frame.Values.TryGetValue($"eng-throttle-lever-position:{eng}", out throttle) ||
                                       (eng == 1 && frame.Values.TryGetValue("eng-throttle-lever-position", out throttle));

                    double redline = _engineLimitsConfig?.RpmRedline ?? 2700.0;
                    if (hasRpm && redline > 0 && rpm >= 0.90 * redline)
                    {
                        isHighPower = true;
                    }
                    else if (hasThrottle && throttle >= 80.0)
                    {
                        isHighPower = true;
                    }
                }
                else
                {
                    double torquePct = 0.0;
                    bool hasTorquePct = frame.Values.TryGetValue($"turb-max-torque-percent:{eng}", out torquePct) ||
                                        (eng == 1 && frame.Values.TryGetValue("turb-max-torque-percent", out torquePct));
                    if (!hasTorquePct)
                    {
                        hasTorquePct = frame.Values.TryGetValue($"turb-eng-torque:{eng}", out torquePct) ||
                                       (eng == 1 && frame.Values.TryGetValue("turb-eng-torque", out torquePct));
                    }

                    if (hasTorquePct && torquePct >= 80.0)
                    {
                        isHighPower = true;
                    }
                }

                bool isCondition = isRunning && (oilTemp < minOilTemp) && isHighPower;

                if (!_coldOilThresholdsByEngine.TryGetValue(eng, out var thresholdsEmitted))
                {
                    thresholdsEmitted = new HashSet<double>();
                    _coldOilThresholdsByEngine[eng] = thresholdsEmitted;
                }

                if (isCondition)
                {
                    double sec = _coldOilSecByEngine.GetValueOrDefault(eng, 0.0) + Math.Max(0.0, dtSimSeconds);
                    _coldOilSecByEngine[eng] = sec;
                    _coldOilOffSecByEngine[eng] = 0.0;

                    double accumulatedMinutes = sec / 60.0;
                    double[] thresholds = { 1.0, 3.0, 6.0 };
                    foreach (var th in thresholds)
                    {
                        if (accumulatedMinutes >= th && !thresholdsEmitted.Contains(th))
                        {
                            thresholdsEmitted.Add(th);
                            events.Add(new DerivedPartEvent(WearRuleMetrics.HighPowerColdOilMinutes, th, eng));
                        }
                    }
                }
                else
                {
                    double offSec = _coldOilOffSecByEngine.GetValueOrDefault(eng, 0.0) + Math.Max(0.0, dtSimSeconds);
                    _coldOilOffSecByEngine[eng] = offSec;

                    if (offSec >= 120.0) // 2 minutos fora da condição
                    {
                        _coldOilSecByEngine[eng] = 0.0;
                        thresholdsEmitted.Clear();
                        _coldOilOffSecByEngine[eng] = 0.0;
                    }
                }
            }
        }

        private void CheckTrimCycles(SensorFrame frame, bool onGround, List<DerivedPartEvent> events)
        {
            if (onGround) return; // Contar ciclos no voo (no ar)

            if (!frame.Values.TryGetValue("af-elevator-trim-position", out double trimPos))
            {
                ReportDiagnosticOnce("trim-missing-sensor", "Regra 'trim_actuator_cycle_stress' desativada: SimVar 'af-elevator-trim-position' ausente no frame de sensores.");
                return;
            }

            if (!_trimAnchor.HasValue)
            {
                _trimAnchor = trimPos;
                _trimDirection = null;
                return;
            }

            double delta = trimPos - _trimAnchor.Value;

            if (!_trimDirection.HasValue)
            {
                if (Math.Abs(delta) >= 0.10)
                {
                    _trimDirection = Math.Sign(delta);
                }
            }
            else
            {
                double dir = _trimDirection.Value;
                double travel = delta * dir;

                // Se andou >= 10% na direção e agora recuou ou inverteu (travel < 0.09)
                if (travel < 0.09)
                {
                    _trimCycleCount++;
                    _trimAnchor = trimPos;
                    _trimDirection = null;

                    int[] thresholds = { 1, 4, 8 };
                    foreach (var th in thresholds)
                    {
                        if (_trimCycleCount >= th && !_trimThresholdsEmitted.Contains(th))
                        {
                            _trimThresholdsEmitted.Add(th);
                            events.Add(new DerivedPartEvent(WearRuleMetrics.TrimCycles, th, null));
                        }
                    }
                }
            }
        }

        private void CheckTurbulenceGExceedance(SensorFrame frame, bool onGround, List<DerivedPartEvent> events)
        {
            if (onGround) return;

            if (!frame.Values.TryGetValue("af-autopilot-master", out double apMaster) ||
                !frame.Values.TryGetValue("af-g-force", out double gForce))
            {
                ReportDiagnosticOnce("ap-g-missing-sensor", "Regra 'autopilot_turbulence_servo_stress' desativada: 'af-autopilot-master' ou 'af-g-force' ausente no frame de sensores.");
                return;
            }

            bool isApOn = apMaster >= 0.5;
            if (!isApOn)
            {
                _gExceedanceActive = false;
                return;
            }

            double deltaG = Math.Abs(gForce - 1.0);

            if (!_gExceedanceActive)
            {
                if (deltaG >= 0.5)
                {
                    _gExceedanceActive = true;
                    _gExceedanceCycleCount++;

                    int[] thresholds = { 10, 25, 50 };
                    foreach (var th in thresholds)
                    {
                        if (_gExceedanceCycleCount >= th && !_gExceedanceThresholdsEmitted.Contains(th))
                        {
                            _gExceedanceThresholdsEmitted.Add(th);
                            events.Add(new DerivedPartEvent(WearRuleMetrics.TurbulenceGExceedanceCycles, th, null));
                        }
                    }
                }
            }
            else
            {
                if (deltaG < 0.2)
                {
                    _gExceedanceActive = false;
                }
            }
        }

        private void CheckRapidPowerTransient(SensorFrame frame, double dtSimSeconds, HashSet<string> features, List<DerivedPartEvent> events)
        {
            if (!features.Contains("turboprop") && !features.Contains(AircraftFeatures.Turboprop))
            {
                return;
            }

            _transientSimTimeSec += Math.Max(0.0, dtSimSeconds);
            int engineCount = DetectEngineCount(frame);

            for (int eng = 1; eng <= engineCount; eng++)
            {
                double torque = 0.0;
                bool hasTorque = frame.Values.TryGetValue($"turb-max-torque-percent:{eng}", out torque) ||
                                 (eng == 1 && frame.Values.TryGetValue("turb-max-torque-percent", out torque));
                if (!hasTorque)
                {
                    hasTorque = frame.Values.TryGetValue($"turb-eng-torque:{eng}", out torque) ||
                                (eng == 1 && frame.Values.TryGetValue("turb-eng-torque", out torque));
                }

                if (!hasTorque)
                {
                    ReportDiagnosticOnce($"transient-torque-missing-sensor-{eng}", $"Regra 'turboprop_rapid_power_lever_movement' desativada para motor {eng}: sensor de torque ausente.");
                    continue;
                }

                if (!_torqueHistoryByEngine.TryGetValue(eng, out var history))
                {
                    history = new Queue<(double TimeSec, double Torque)>();
                    _torqueHistoryByEngine[eng] = history;
                }

                history.Enqueue((_transientSimTimeSec, torque));

                // Remove pontos mais antigos que 2 s
                while (history.Count > 0 && (_transientSimTimeSec - history.Peek().TimeSec) > 2.0)
                {
                    history.Dequeue();
                }

                double minTorque = double.MaxValue;
                double maxTorque = double.MinValue;
                foreach (var pt in history)
                {
                    if (pt.Torque < minTorque) minTorque = pt.Torque;
                    if (pt.Torque > maxTorque) maxTorque = pt.Torque;
                }

                double deltaTorque = maxTorque - minTorque;

                double cooldown = _transientCooldownByEngine.GetValueOrDefault(eng, 0.0);
                if (cooldown > 0.0)
                {
                    _transientCooldownByEngine[eng] = Math.Max(0.0, cooldown - dtSimSeconds);
                }
                else if (deltaTorque >= 40.0)
                {
                    int currentCount = _transientCycleCountByEngine.GetValueOrDefault(eng, 0) + 1;
                    _transientCycleCountByEngine[eng] = currentCount;
                    _transientCooldownByEngine[eng] = 2.0; // Cooldown de 2 s para a mesma oscilação

                    if (!_transientThresholdsByEngine.TryGetValue(eng, out var thresholdsEmitted))
                    {
                        thresholdsEmitted = new HashSet<double>();
                        _transientThresholdsByEngine[eng] = thresholdsEmitted;
                    }

                    int[] thresholds = { 3, 8, 15 };
                    foreach (var th in thresholds)
                    {
                        if (currentCount >= th && !thresholdsEmitted.Contains(th))
                        {
                            thresholdsEmitted.Add(th);
                            events.Add(new DerivedPartEvent(WearRuleMetrics.RapidPowerTransientCycles, th, eng));
                        }
                    }
                }
            }
        }

        private static bool TryGetGroundSpeed(SensorFrame frame, out double gsKnots)
        {
            if (frame.Values.TryGetValue("ctx-ground-velocity", out gsKnots) ||
                frame.Values.TryGetValue("gps-ground-speed", out gsKnots) ||
                frame.Values.TryGetValue("ground-velocity", out gsKnots))
            {
                return true;
            }

            gsKnots = 0.0;
            return false;
        }

        private static int DetectEngineCount(SensorFrame frame)
        {
            int maxEngine = 1;
            foreach (var key in frame.Values.Keys)
            {
                int colonIdx = key.LastIndexOf(':');
                if (colonIdx >= 0 && colonIdx < key.Length - 1)
                {
                    if (int.TryParse(key.AsSpan(colonIdx + 1), out int n) && n > maxEngine)
                    {
                        maxEngine = n;
                    }
                }
            }
            return maxEngine;
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
