using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Sensors;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Gerador puro de voos sintéticos procedurais com física coerente e injeção controlada de abusos e gabarito.
    /// Utiliza exclusivamente as chaves do layout real (SensorLayout.Build) e determinismo via IRandomSource.
    /// </summary>
    public sealed class SyntheticFlightGenerator
    {
        private readonly AircraftLimitResolver? _resolver;

        public SyntheticFlightGenerator(AircraftLimitResolver? resolver = null)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Gera um voo sintético completo a partir dos dados do cenário e plano de voo.
        /// </summary>
        public SyntheticFlight Generate(AircraftScenarioInput input, SyntheticFlightPlan plan, IRandomSource rnd)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (rnd == null) throw new ArgumentNullException(nameof(rnd));

            var profile = input.Profile ?? throw new ArgumentException("Perfil da aeronave é obrigatório no cenário.", nameof(input));
            int engineCount = Math.Max(1, input.EngineCount ?? profile.EngineCount);
            var features = input.Features ?? profile.Features ?? Array.Empty<string>();
            var featSet = new HashSet<string>(features, StringComparer.OrdinalIgnoreCase);

            bool isRetractable = featSet.Contains("retractableGear") || featSet.Contains("hydraulicGear");
            string aircraftClass = profile.Class ?? "Piston";

            // 1. Constrói o layout real determinístico de sensores
            var sensors = input.Sensors ?? SensorRegistry.LoadDefault().Sensors;
            var layout = SensorLayout.Build(sensors, engineCount, includeCalibration: false);
            var allSlots = layout.Blocks
                .SelectMany(b => b.Slots)
                .Where(s => !s.Key.StartsWith(SensorLayout.PaddingKeyPrefix))
                .ToList();

            var sensorKeys = allSlots.Select(s => s.Key).ToList();
            var slotLookup = allSlots.ToDictionary(s => s.Key, s => s, StringComparer.OrdinalIgnoreCase);

            // 2. Extrai os limites da aeronave com fallbacks seguros por classe
            double rpmRedline = ResolveLimit(LimitKeys.RpmRedline, aircraftClass == "Turboprop" ? 2200.0 : 2700.0);
            double chtMax = ResolveLimit(LimitKeys.ChtMax, 240.0);
            double oilTempMax = ResolveLimit(LimitKeys.OilTempMax, 118.0);
            double oilPressMin = ResolveLimit(LimitKeys.OilPressMin, 20.0);
            double vne = ResolveLimit(LimitKeys.Vne, 160.0);
            double vno = ResolveLimit(LimitKeys.Vno, 130.0);
            double va = ResolveLimit(LimitKeys.Va, 105.0);
            double gLimitPos = ResolveLimit(LimitKeys.GLimitPos, 3.8);
            double gLimitNeg = ResolveLimit(LimitKeys.GLimitNeg, -1.52);
            double maxTouchdownFpm = ResolveLimit(LimitKeys.MaxTouchdownFpm, 600.0);
            double elecSystemVolts = ResolveLimit(LimitKeys.ElecSystemVolts, 28.0);
            double alternatorRatedAmps = ResolveLimit(LimitKeys.AlternatorRatedAmps, 60.0);
            double chtCoolingRateMax = ResolveLimit(LimitKeys.ChtCoolingRateMax, 28.0);

            double vfe1 = ResolveLimit("vfe:1", 110.0);
            double vfe2 = ResolveLimit("vfe:2", 85.0);
            int avionicsCircuitIdx = (int)Math.Round(ResolveLimit("elecAvionicsCircuitIndex", 0.0));

            // Velocidades nominais calibradas com base nos limites para voo normal sem excedências
            double normalCruiseIas = Math.Min(vno * 0.72, va * 0.80);
            if (normalCruiseIas <= 40.0) normalCruiseIas = 85.0;
            double normalClimbIas = Math.Min(normalCruiseIas * 0.75, 75.0);
            double normalDescentIas = normalCruiseIas * 0.90;
            double normalApproachIas = Math.Min(vfe2 * 0.85, normalCruiseIas * 0.65);

            // 3. Fases e linha de tempo do voo
            double totalDuration = Math.Max(120.0, plan.TotalDurationSeconds);
            double dtStep = 0.43; // ~2.32 Hz nominal

            double tCold = 15.0;
            double tStart = 22.0;
            double tTaxiOut = 45.0;
            double tTakeoff = 75.0;
            double tClimb = totalDuration * 0.40;
            double tCruise = totalDuration * 0.65;
            double tDescent = totalDuration * 0.80;
            double tTouchdown = totalDuration * 0.90;
            double tShutdown = totalDuration;

            var touchdowns = new List<ReplayTouchdown>();
            var frames = new List<SensorFrame>();
            DateTime baseUtc = new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);
            DateTime currentUtc = baseUtc;

            bool isCancelled = false;
            bool hasDataCorruptions = false;
            int expectedLandingCycles = 0;

            // Encontra abusos específicos para interceptação
            var hardLandingAbuse = plan.Abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.HardLanding);
            var longStartAbuse = plan.Abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.LongStart);
            var avionicsStartAbuse = plan.Abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.AvionicsMasterOnStart);
            var thermalShockAbuse = plan.Abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.ThermalShock);
            var cancelAbuse = plan.Abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.DataCancelledMidFlight);

            if (cancelAbuse != null)
            {
                isCancelled = true;
            }

            if (plan.Abuses.Any(a => a.Type is SyntheticAbuseType.DataNanValues
                                           or SyntheticAbuseType.DataMissingSensor
                                           or SyntheticAbuseType.DataFrameGap
                                           or SyntheticAbuseType.DataSimPause))
            {
                hasDataCorruptions = true;
            }

            // Estado corrente da aeronave
            bool onGround = true;
            bool parkingBrake = true;
            bool masterBattery = false;
            bool avionicsMaster = false;
            var combustion = new bool[engineCount + 1];
            var starter = new bool[engineCount + 1];
            var alternator = new bool[engineCount + 1];

            var rpm = new double[engineCount + 1];
            var cht = new double[engineCount + 1];
            var egt = new double[engineCount + 1];
            var oilTemp = new double[engineCount + 1];
            var oilPress = new double[engineCount + 1];

            for (int e = 1; e <= engineCount; e++)
            {
                cht[e] = 20.0;
                oilTemp[e] = 20.0;
                oilPress[e] = 0.0;
            }

            double ias = 0.0;
            double altitude = 500.0;
            double vsFpm = 0.0;
            double gForce = 1.0;
            int flapIndex = 0;
            double flapPercent = 0.0;
            double gearPosition = 1.0; // 1 = baixado e travado

            double busVoltage = 0.0;
            double loadAmps = 0.0;

            bool touchdownScheduled = false;

            for (double t = 0.0; t <= totalDuration; t += dtStep)
            {
                // Verifica cancelamento prematuro
                if (cancelAbuse != null && t >= cancelAbuse.StartSeconds)
                {
                    break;
                }

                // Determina taxa de simulação para este instante
                double simRate = 1.0;
                foreach (var seg in plan.SimRateSegments)
                {
                    if (t >= seg.FromSec && t <= seg.ToSec)
                    {
                        simRate = seg.SimRate;
                        break;
                    }
                }

                // -------------------------------------------------------------------------
                // FÍSICA PROCEDURAL COERENTE POR FASE
                // -------------------------------------------------------------------------
                if (t < tCold)
                {
                    // Fase 1: Cold & Dark
                    onGround = true;
                    parkingBrake = true;
                    masterBattery = false;
                    avionicsMaster = false;
                    for (int e = 1; e <= engineCount; e++)
                    {
                        starter[e] = false;
                        combustion[e] = false;
                        alternator[e] = false;
                        rpm[e] = 0.0;
                        oilPress[e] = 0.0;
                    }
                    ias = 0.0;
                    vsFpm = 0.0;
                    busVoltage = 0.0;
                    loadAmps = 0.0;
                }
                else if (t < tStart)
                {
                    // Fase 2: Partida do motor
                    masterBattery = true;
                    busVoltage = elecSystemVolts * 0.95;
                    loadAmps = 12.0;

                    // Aviônicos normalmente desligados durante partida, a menos de abuso
                    if (avionicsStartAbuse != null && t >= avionicsStartAbuse.StartSeconds)
                    {
                        avionicsMaster = true;
                    }
                    else
                    {
                        avionicsMaster = false;
                    }

                    bool longStartActive = longStartAbuse != null && t >= longStartAbuse.StartSeconds && t <= longStartAbuse.StartSeconds + longStartAbuse.DurationSeconds;

                    for (int e = 1; e <= engineCount; e++)
                    {
                        starter[e] = true;
                        if (longStartActive)
                        {
                            // Motor fica manivelando sem pegar
                            combustion[e] = false;
                            rpm[e] = 220.0 + (rnd.NextDouble() - 0.5) * 10.0;
                            oilPress[e] = 8.0;
                        }
                        else
                        {
                            if (t >= tCold + 3.0)
                            {
                                combustion[e] = true;
                                starter[e] = false;
                                alternator[e] = true;
                                rpm[e] = 950.0 + (rnd.NextDouble() - 0.5) * 20.0;
                                oilPress[e] = 35.0;
                            }
                            else
                            {
                                rpm[e] = 220.0;
                            }
                        }
                    }
                }
                else if (t < tTaxiOut)
                {
                    // Fase 3: Aquecimento e Táxi
                    masterBattery = true;
                    avionicsMaster = true;
                    parkingBrake = t < 25.0; // solta freio aos 25 s
                    onGround = true;
                    ias = parkingBrake ? 0.0 : 12.0 + (rnd.NextDouble() - 0.5) * 2.0;
                    vsFpm = 0.0;

                    for (int e = 1; e <= engineCount; e++)
                    {
                        starter[e] = false;
                        combustion[e] = true;
                        alternator[e] = true;
                        rpm[e] = 1050.0 + (rnd.NextDouble() - 0.5) * 15.0;
                        oilPress[e] = 45.0 + (rnd.NextDouble() - 0.5) * 2.0;
                        cht[e] = Math.Min(120.0, cht[e] + 0.35);
                        oilTemp[e] = Math.Min(70.0, oilTemp[e] + 0.20);
                        egt[e] = 600.0;
                    }

                    busVoltage = elecSystemVolts;
                    loadAmps = 18.0;
                }
                else if (t < tTakeoff)
                {
                    // Fase 4: Corrida de Decolagem
                    parkingBrake = false;
                    onGround = t < tTakeoff - 3.0; // decola aos (tTakeoff - 3s)
                    double takeoffProgress = (t - tTaxiOut) / (tTakeoff - tTaxiOut);
                    ias = 15.0 + takeoffProgress * (normalClimbIas - 15.0);

                    for (int e = 1; e <= engineCount; e++)
                    {
                        combustion[e] = true;
                        alternator[e] = true;
                        rpm[e] = rpmRedline * 0.94 + (rnd.NextDouble() - 0.5) * 10.0;
                        oilPress[e] = 52.0;
                        cht[e] = Math.Min(165.0, cht[e] + 0.5);
                        oilTemp[e] = Math.Min(85.0, oilTemp[e] + 0.3);
                        egt[e] = 720.0;
                    }

                    if (!onGround)
                    {
                        vsFpm = 600.0;
                        altitude += vsFpm * (dtStep / 60.0);
                        if (isRetractable) gearPosition = 0.0; // trem recolhe
                    }
                }
                else if (t < tClimb)
                {
                    // Fase 5: Subida inicial
                    onGround = false;
                    parkingBrake = false;
                    if (isRetractable) gearPosition = 0.0;
                    ias = normalClimbIas + (rnd.NextDouble() - 0.5) * 2.0;
                    vsFpm = 750.0;
                    altitude += vsFpm * (dtStep / 60.0);

                    for (int e = 1; e <= engineCount; e++)
                    {
                        combustion[e] = true;
                        alternator[e] = true;
                        rpm[e] = rpmRedline * 0.90 + (rnd.NextDouble() - 0.5) * 10.0;
                        oilPress[e] = 50.0;
                        cht[e] = Math.Min(175.0, cht[e] + 0.2);
                        oilTemp[e] = Math.Min(90.0, oilTemp[e] + 0.15);
                        egt[e] = 730.0;
                    }
                }
                else if (t < tCruise)
                {
                    // Fase 6: Cruzeiro Nivelado
                    onGround = false;
                    vsFpm = 0.0;
                    ias = normalCruiseIas + (rnd.NextDouble() - 0.5) * 2.0;
                    if (isRetractable) gearPosition = 0.0;

                    for (int e = 1; e <= engineCount; e++)
                    {
                        combustion[e] = true;
                        alternator[e] = true;
                        rpm[e] = rpmRedline * 0.75 + (rnd.NextDouble() - 0.5) * 10.0;
                        oilPress[e] = 48.0;
                        cht[e] = 155.0 + (rnd.NextDouble() - 0.5) * 1.0;
                        oilTemp[e] = 85.0 + (rnd.NextDouble() - 0.5) * 0.5;
                        egt[e] = 710.0;
                    }
                }
                else if (t < tDescent)
                {
                    // Fase 7: Descida
                    onGround = false;
                    vsFpm = -600.0;
                    altitude = Math.Max(1000.0, altitude + vsFpm * (dtStep / 60.0));
                    ias = normalDescentIas + (rnd.NextDouble() - 0.5) * 2.0;

                    for (int e = 1; e <= engineCount; e++)
                    {
                        combustion[e] = true;
                        alternator[e] = true;
                        rpm[e] = rpmRedline * 0.60;
                        oilPress[e] = 42.0;
                        cht[e] = Math.Max(130.0, cht[e] - 0.15);
                        oilTemp[e] = Math.Max(75.0, oilTemp[e] - 0.10);
                        egt[e] = 650.0;
                    }
                }
                else if (t < tTouchdown)
                {
                    // Fase 8: Aproximação final
                    onGround = false;
                    vsFpm = -450.0;
                    altitude = Math.Max(50.0, altitude + vsFpm * (dtStep / 60.0));
                    ias = normalApproachIas + (rnd.NextDouble() - 0.5) * 2.0;
                    flapIndex = 2;
                    flapPercent = 1.0;
                    if (isRetractable) gearPosition = 1.0;

                    for (int e = 1; e <= engineCount; e++)
                    {
                        combustion[e] = true;
                        alternator[e] = true;
                        rpm[e] = rpmRedline * 0.55;
                        oilPress[e] = 40.0;
                    }
                }
                else
                {
                    // Fase 9: Pouso, Táxi de retorno e corte
                    onGround = true;
                    if (!touchdownScheduled)
                    {
                        touchdownScheduled = true;
                        expectedLandingCycles++;

                        double touchFpm = 140.0 + rnd.NextDouble() * 40.0; // pouso suave normal
                        if (hardLandingAbuse != null)
                        {
                            touchFpm = maxTouchdownFpm + hardLandingAbuse.Intensity * 300.0;
                        }

                        touchdowns.Add(new ReplayTouchdown(
                            Fpm: touchFpm,
                            PitchDeg: 6.5,
                            BankDeg: 0.2,
                            LateralFps: 0.1,
                            IsBounce: false,
                            ElapsedSeconds: t,
                            Utc: currentUtc));
                    }

                    flapIndex = 0;
                    flapPercent = 0.0;
                    double taxiProgress = (t - tTouchdown) / (tShutdown - tTouchdown);

                    if (taxiProgress < 0.70)
                    {
                        // Táxi no solo
                        parkingBrake = false;
                        ias = Math.Max(0.0, 35.0 * (1.0 - taxiProgress));
                        for (int e = 1; e <= engineCount; e++)
                        {
                            combustion[e] = true;
                            alternator[e] = true;
                            rpm[e] = 950.0;
                            oilPress[e] = 38.0;
                        }
                    }
                    else
                    {
                        // Parado e corte
                        parkingBrake = true;
                        ias = 0.0;
                        masterBattery = false;
                        avionicsMaster = false;
                        for (int e = 1; e <= engineCount; e++)
                        {
                            combustion[e] = false;
                            starter[e] = false;
                            alternator[e] = false;
                            rpm[e] = 0.0;
                            oilPress[e] = 0.0;
                        }
                    }
                }

                // -------------------------------------------------------------------------
                // INJEÇÃO CONTROLADA DE ABUSOS PLANEJADOS
                // -------------------------------------------------------------------------
                foreach (var abuse in plan.Abuses)
                {
                    if (t < abuse.StartSeconds || t > abuse.StartSeconds + abuse.DurationSeconds)
                    {
                        continue;
                    }

                    int eTarget = abuse.EngineIndex > 0 ? Math.Clamp(abuse.EngineIndex, 1, engineCount) : 0;

                    switch (abuse.Type)
                    {
                        case SyntheticAbuseType.RpmOverspeed:
                            ApplyToEngines(eTarget, eng => rpm[eng] = rpmRedline * (1.0 + Math.Max(0.05, abuse.Intensity)));
                            break;

                        case SyntheticAbuseType.ChtOverheat:
                            ApplyToEngines(eTarget, eng => cht[eng] = chtMax + Math.Max(5.0, abuse.Intensity * 30.0));
                            break;

                        case SyntheticAbuseType.OilTempOverheat:
                            ApplyToEngines(eTarget, eng => oilTemp[eng] = oilTempMax + Math.Max(5.0, abuse.Intensity * 25.0));
                            break;

                        case SyntheticAbuseType.OilPressureLow:
                            ApplyToEngines(eTarget, eng => oilPress[eng] = oilPressMin * 0.35);
                            break;

                        case SyntheticAbuseType.ThermalShock:
                            // Esfria drasticamente a CHT a uma taxa > chtCoolingRateMax (ex.: -45 °C/min)
                            ApplyToEngines(eTarget, eng =>
                            {
                                double shockRatePerSec = (chtCoolingRateMax + 15.0) / 60.0;
                                cht[eng] = Math.Max(50.0, cht[eng] - shockRatePerSec * dtStep);
                            });
                            break;

                        case SyntheticAbuseType.EgtHigh:
                            ApplyToEngines(eTarget, eng => egt[eng] = 880.0 + abuse.Intensity * 50.0);
                            break;

                        case SyntheticAbuseType.FlapOverSpeed:
                            flapIndex = 2;
                            flapPercent = 1.0;
                            ias = vfe2 + 15.0 + abuse.Intensity * 20.0;
                            break;

                        case SyntheticAbuseType.VneExceeded:
                            ias = vne * (1.0 + Math.Max(0.05, abuse.Intensity));
                            break;

                        case SyntheticAbuseType.PositiveGExceeded:
                            gForce = gLimitPos + Math.Max(0.5, abuse.Intensity * 2.0);
                            break;

                        case SyntheticAbuseType.NegativeGExceeded:
                            gForce = gLimitNeg - Math.Max(0.5, abuse.Intensity * 1.5);
                            break;

                        case SyntheticAbuseType.BatteryDischarging:
                            ApplyToEngines(eTarget, eng => alternator[eng] = false);
                            busVoltage = elecSystemVolts <= 14.0 ? 10.5 : 21.0;
                            break;

                        case SyntheticAbuseType.ElectricalOverload:
                            ApplyToEngines(eTarget, eng =>
                            {
                                alternator[eng] = true;
                                combustion[eng] = true;
                            });
                            loadAmps = alternatorRatedAmps * (1.10 + abuse.Intensity);
                            // Sag de tensão no barramento na faixa [0.88, 0.95) * elecSystemVolts
                            busVoltage = elecSystemVolts * 0.91;
                            break;

                        case SyntheticAbuseType.GearCycles:
                            if (isRetractable)
                            {
                                // Alterna rapidamente o trem a cada 3 segundos
                                int cyclePhase = (int)Math.Floor(t / 3.0) % 2;
                                gearPosition = cyclePhase == 0 ? 0.0 : 1.0;
                            }
                            break;
                    }
                }

                // -------------------------------------------------------------------------
                // MAPEAMENTO NO FRAME COM BASE NO LAYOUT REAL (SEM CHAVES À MÃO)
                // -------------------------------------------------------------------------
                var frameValues = new Dictionary<string, double>(slotLookup.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var slot in allSlots)
                {
                    double val = 0.0;
                    string key = slot.Key;

                    if (key.Equals(SensorLayout.SimulationRateKey, StringComparison.OrdinalIgnoreCase))
                    {
                        val = simRate;
                    }
                    else if (key.Equals("sim-is-paused", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isPaused = plan.Abuses.Any(a => a.Type == SyntheticAbuseType.DataSimPause &&
                                                             t >= a.StartSeconds && t <= a.StartSeconds + a.DurationSeconds);
                        val = isPaused ? 1.0 : 0.0;
                    }
                    else if (key.Equals("brake-parking-position", StringComparison.OrdinalIgnoreCase))
                    {
                        val = parkingBrake ? 1.0 : 0.0;
                    }
                    else if (key.Equals("af-airspeed-indicated", StringComparison.OrdinalIgnoreCase))
                    {
                        val = ias;
                    }
                    else if (key.Equals("af-airspeed-true", StringComparison.OrdinalIgnoreCase))
                    {
                        val = ias * 1.05;
                    }
                    else if (key.Equals("af-g-force", StringComparison.OrdinalIgnoreCase))
                    {
                        val = gForce;
                    }
                    else if (key.Equals("af-flaps-handle-index", StringComparison.OrdinalIgnoreCase))
                    {
                        val = flapIndex;
                    }
                    else if (key.Equals("af-flaps-handle-percent", StringComparison.OrdinalIgnoreCase))
                    {
                        val = flapPercent;
                    }
                    else if (key.Equals("gear-handle-position", StringComparison.OrdinalIgnoreCase) ||
                             key.Equals("gear-position", StringComparison.OrdinalIgnoreCase))
                    {
                        val = gearPosition;
                    }
                    else if (key.StartsWith("gear-contact-point-compression", StringComparison.OrdinalIgnoreCase))
                    {
                        val = onGround ? 0.35 : 0.0;
                    }
                    else if (key.Equals("elec-master-battery", StringComparison.OrdinalIgnoreCase))
                    {
                        val = masterBattery ? 1.0 : 0.0;
                    }
                    else if (key.Equals("elec-avionics-master-switch", StringComparison.OrdinalIgnoreCase) ||
                             (avionicsCircuitIdx > 0 && key.Equals($"elec-circuit-on-{avionicsCircuitIdx}", StringComparison.OrdinalIgnoreCase)))
                    {
                        val = avionicsMaster ? 1.0 : 0.0;
                    }
                    else if (key.StartsWith("elec-master-alternator", StringComparison.OrdinalIgnoreCase))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = alternator[engIdx] ? 1.0 : 0.0;
                    }
                    else if (key.StartsWith("elec-main-bus-voltage", StringComparison.OrdinalIgnoreCase))
                    {
                        val = busVoltage;
                    }
                    else if (key.Equals("elec-total-load-amps", StringComparison.OrdinalIgnoreCase))
                    {
                        val = loadAmps;
                    }
                    else if (key.StartsWith("eng-combustion", StringComparison.OrdinalIgnoreCase))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = combustion[engIdx] ? 1.0 : 0.0;
                    }
                    else if (key.StartsWith("eng-starter", StringComparison.OrdinalIgnoreCase) && !key.Contains("torque"))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = starter[engIdx] ? 1.0 : 0.0;
                    }
                    else if (key.StartsWith("eng-rpm", StringComparison.OrdinalIgnoreCase) && !key.Contains("max"))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = rpm[engIdx];
                    }
                    else if (key.StartsWith("eng-cht", StringComparison.OrdinalIgnoreCase) && !key.Contains("max"))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = cht[engIdx];
                    }
                    else if (key.StartsWith("eng-egt", StringComparison.OrdinalIgnoreCase) && !key.Contains("max"))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = egt[engIdx];
                    }
                    else if (key.StartsWith("oil-temperature", StringComparison.OrdinalIgnoreCase) && !key.Contains("max"))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = oilTemp[engIdx];
                    }
                    else if (key.StartsWith("oil-pressure", StringComparison.OrdinalIgnoreCase) && !key.Contains("warning"))
                    {
                        int engIdx = ExtractEngineIndex(key);
                        val = oilPress[engIdx];
                    }

                    frameValues[key] = val;
                }

                // -------------------------------------------------------------------------
                // ANOMALIAS E FALHAS DE DADOS INTENCIONAIS
                // -------------------------------------------------------------------------
                foreach (var abuse in plan.Abuses)
                {
                    if (t < abuse.StartSeconds || t > abuse.StartSeconds + abuse.DurationSeconds)
                    {
                        continue;
                    }

                    if (abuse.Type == SyntheticAbuseType.DataNanValues)
                    {
                        string targetKey = abuse.EngineIndex > 0 ? $"eng-rpm:{abuse.EngineIndex}" : "eng-rpm:1";
                        if (frameValues.ContainsKey(targetKey)) frameValues[targetKey] = double.NaN;
                        if (frameValues.ContainsKey("af-airspeed-indicated")) frameValues["af-airspeed-indicated"] = double.NaN;
                    }
                    else if (abuse.Type == SyntheticAbuseType.DataMissingSensor)
                    {
                        string targetKey = abuse.EngineIndex > 0 ? $"eng-rpm:{abuse.EngineIndex}" : "eng-rpm:1";
                        frameValues.Remove(targetKey);
                    }
                    else if (abuse.Type == SyntheticAbuseType.DataFrameGap)
                    {
                        // Salta 15 segundos para a frente no relógio real uma vez
                        currentUtc = currentUtc.AddSeconds(15.0);
                    }
                }

                frames.Add(new SensorFrame(frameValues, currentUtc, simRate));
                currentUtc = currentUtc.AddSeconds(dtStep);
            }

            var groundTruth = new SyntheticGroundTruth(
                InjectedAbuses: plan.Abuses,
                PlannedDurationSeconds: totalDuration,
                ExpectedLandingCycles: expectedLandingCycles,
                IsCancelled: isCancelled,
                HasDataCorruptions: hasDataCorruptions);

            return new SyntheticFlight(frames, touchdowns, groundTruth, profile, sensorKeys);

            void ApplyToEngines(int targetEng, Action<int> action)
            {
                if (targetEng > 0)
                {
                    action(targetEng);
                }
                else
                {
                    for (int e = 1; e <= engineCount; e++)
                    {
                        action(e);
                    }
                }
            }

            int ExtractEngineIndex(string k)
            {
                int colonIdx = k.LastIndexOf(':');
                if (colonIdx >= 0 && int.TryParse(k.AsSpan(colonIdx + 1), out int parsed) && parsed >= 1 && parsed <= engineCount)
                {
                    return parsed;
                }
                return 1;
            }

            double ResolveLimit(string limitKey, double fallback)
            {
                if (_resolver != null)
                {
                    var lim = _resolver.Get(profile, aircraftClass, limitKey);
                    if (lim != null && lim.Value > 0) return lim.Value;
                }

                var pLim = profile.Limits?.FirstOrDefault(l => string.Equals(l.Key, limitKey, StringComparison.OrdinalIgnoreCase));
                if (pLim != null && pLim.Value > 0) return pLim.Value;

                return fallback;
            }
        }
    }
}
