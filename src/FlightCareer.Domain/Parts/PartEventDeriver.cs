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
    /// Deriva eventos discretos operacionais (partida de motor, tempo de starter, ciclos de trem, ciclo de voo)
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

        public void Reset()
        {
            _wasCombustion.Clear();
            _wasStarter.Clear();
            _starterAccumulated.Clear();
            _gearWasRetracted = false;
            _wasInAir = false;
            _flightCycleEmitted = false;
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

            // 2. Ciclos de trem de pouso retrátil (recolhido e baixado de novo)
            if (isRetractableGear)
            {
                if (TryGetGearPosition(frame, out double gearPos))
                {
                    // msfs: 0 = recolhido (up), 100 = baixado (down); ou alavanca 0=up, 1=down
                    bool isUp = gearPos <= 10.0 || (gearPos <= 0.1 && gearPos >= 0.0);
                    bool isDown = gearPos >= 90.0 || (gearPos >= 0.9 && gearPos <= 1.01);

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

            // 3. Ciclo de voo (decolagem até pouso, emitido no primeiro pouso após estar no ar)
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
