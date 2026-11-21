using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Plano de voo sintético definindo duração, perfil de taxas de simulação e abusos injetados.
    /// </summary>
    public sealed class SyntheticFlightPlan
    {
        public double TotalDurationSeconds { get; init; } = 420.0;
        public IReadOnlyList<SyntheticAbuseRecord> Abuses { get; init; } = Array.Empty<SyntheticAbuseRecord>();
        public IReadOnlyList<(double FromSec, double ToSec, double SimRate)> SimRateSegments { get; init; } = Array.Empty<(double, double, double)>();

        public static SyntheticFlightPlan CreateNormal(double durationSeconds = 420.0)
        {
            return new SyntheticFlightPlan
            {
                TotalDurationSeconds = Math.Max(120.0, durationSeconds),
                Abuses = Array.Empty<SyntheticAbuseRecord>(),
                SimRateSegments = Array.Empty<(double, double, double)>()
            };
        }

        /// <summary>
        /// Sorteia um plano de voo sintético plausível e determinístico com base em uma fonte de números aleatórios.
        /// </summary>
        public static SyntheticFlightPlan Random(IRandomSource rnd, AircraftLimitProfile profile, int abuseCount = 1)
        {
            if (rnd == null) throw new ArgumentNullException(nameof(rnd));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            double duration = 360.0 + rnd.NextDouble() * 240.0; // 360 a 600 segundos (6 a 10 minutos)

            var featSet = new HashSet<string>(profile.Features ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            bool isPiston = string.Equals(profile.Class, "Piston", StringComparison.OrdinalIgnoreCase) || featSet.Contains("piston");
            bool isRetractable = featSet.Contains("retractableGear") || featSet.Contains("hydraulicGear");
            bool hasFlaps = !featSet.Contains(AircraftFeatures.NoFlaps);
            bool hasElectrical = !featSet.Contains(AircraftFeatures.NoElectrical);
            int engineCount = Math.Max(1, profile.EngineCount);

            // Sim rate: 80% dos voos 1x fixo; 20% com trecho acelerado durante cruzeiro
            var simRateSegments = new List<(double FromSec, double ToSec, double SimRate)>();
            if (rnd.NextDouble() < 0.20)
            {
                double cruiseStart = duration * 0.40;
                double cruiseEnd = duration * 0.65;
                double[] candidateRates = { 2.0, 4.0, 16.0 };
                int rateIdx = (int)Math.Floor(rnd.NextDouble() * candidateRates.Length);
                double selectedRate = candidateRates[Math.Clamp(rateIdx, 0, candidateRates.Length - 1)];
                simRateSegments.Add((cruiseStart, cruiseEnd, selectedRate));
            }

            var abuses = new List<SyntheticAbuseRecord>();
            if (abuseCount > 0)
            {
                var candidateTypes = new List<SyntheticAbuseType>
                {
                    SyntheticAbuseType.OilTempOverheat,
                    SyntheticAbuseType.OilPressureLow,
                    SyntheticAbuseType.PositiveGExceeded,
                    SyntheticAbuseType.NegativeGExceeded,
                    SyntheticAbuseType.HardLanding,
                    SyntheticAbuseType.LongStart,
                    SyntheticAbuseType.DataMissingSensor,
                    SyntheticAbuseType.DataNanValues,
                    SyntheticAbuseType.DataFrameGap,
                    SyntheticAbuseType.DataSimPause,
                    SyntheticAbuseType.DataCancelledMidFlight,
                    SyntheticAbuseType.VneExceeded,
                    SyntheticAbuseType.FlapOverSpeed,
                    SyntheticAbuseType.BatteryDischarging,
                    SyntheticAbuseType.ElectricalOverload,
                    SyntheticAbuseType.AvionicsMasterOnStart
                };

                if (!hasFlaps)
                {
                    candidateTypes.Remove(SyntheticAbuseType.FlapOverSpeed);
                }

                if (!hasElectrical)
                {
                    candidateTypes.Remove(SyntheticAbuseType.BatteryDischarging);
                    candidateTypes.Remove(SyntheticAbuseType.ElectricalOverload);
                    candidateTypes.Remove(SyntheticAbuseType.AvionicsMasterOnStart);
                }

                if (isPiston)
                {
                    candidateTypes.Add(SyntheticAbuseType.RpmOverspeed);
                    candidateTypes.Add(SyntheticAbuseType.ChtOverheat);
                    candidateTypes.Add(SyntheticAbuseType.ThermalShock);
                    candidateTypes.Add(SyntheticAbuseType.EgtHigh);
                }

                if (isRetractable)
                {
                    candidateTypes.Add(SyntheticAbuseType.GearCycles);
                }

                for (int i = 0; i < abuseCount; i++)
                {
                    int typeIdx = (int)Math.Floor(rnd.NextDouble() * candidateTypes.Count);
                    var abuseType = candidateTypes[Math.Clamp(typeIdx, 0, candidateTypes.Count - 1)];

                    // Evita conflitos físicos diretos no mesmo plano sintético:
                    // 1. Descarga profunda e sobrecarga elétrica são estados de barramento opostos
                    if (abuseType == SyntheticAbuseType.ElectricalOverload && abuses.Any(a => a.Type == SyntheticAbuseType.BatteryDischarging))
                        continue;
                    if (abuseType == SyntheticAbuseType.BatteryDischarging && abuses.Any(a => a.Type == SyntheticAbuseType.ElectricalOverload))
                        continue;

                    // 2. No máximo um cancelamento prematuro por voo
                    if (abuseType == SyntheticAbuseType.DataCancelledMidFlight && abuses.Any(a => a.Type == SyntheticAbuseType.DataCancelledMidFlight))
                        continue;

                    bool isEngineAbuse = abuseType switch
                    {
                        SyntheticAbuseType.RpmOverspeed => true,
                        SyntheticAbuseType.ChtOverheat => true,
                        SyntheticAbuseType.OilTempOverheat => true,
                        SyntheticAbuseType.OilPressureLow => true,
                        SyntheticAbuseType.ThermalShock => true,
                        SyntheticAbuseType.EgtHigh => true,
                        SyntheticAbuseType.LongStart => true,
                        SyntheticAbuseType.AvionicsMasterOnStart => true,
                        _ => false
                    };

                    int targetEngine = 0;
                    if (isEngineAbuse)
                    {
                        targetEngine = 1;
                        if (engineCount > 1)
                        {
                            targetEngine = 1 + (int)Math.Floor(rnd.NextDouble() * engineCount);
                            targetEngine = Math.Clamp(targetEngine, 1, engineCount);
                        }
                    }

                    var record = CreateAbuseRecord(abuseType, duration, targetEngine, rnd);
                    abuses.Add(record);
                }

                // Garante duração suficiente para acomodar os abusos sorteados (ex.: descarga ou sobrecarga longa)
                foreach (var ab in abuses)
                {
                    if (ab.StartSeconds + ab.DurationSeconds + 30.0 > duration)
                    {
                        duration = ab.StartSeconds + ab.DurationSeconds + 30.0;
                    }
                }

                // Se houver abusos concorrentes de G (PositiveG e NegativeG), separa seus instantes no tempo
                var posG = abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.PositiveGExceeded);
                var negG = abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.NegativeGExceeded);
                if (posG != null && negG != null)
                {
                    double minNegGStart = posG.StartSeconds + posG.DurationSeconds + 3.0;
                    if (negG.StartSeconds < minNegGStart)
                    {
                        abuses.Remove(negG);
                        abuses.Add(new SyntheticAbuseRecord(
                            negG.Type,
                            minNegGStart,
                            negG.DurationSeconds,
                            negG.Intensity,
                            negG.EngineIndex,
                            negG.TargetRuleId,
                            negG.TargetPartId,
                            negG.Notes));

                        if (minNegGStart + negG.DurationSeconds + 30.0 > duration)
                        {
                            duration = minNegGStart + negG.DurationSeconds + 30.0;
                        }
                    }
                }

                // Garante que o pouso duro coincida com o momento exato do toque da aeronave (0.90 * duration)
                var hl = abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.HardLanding);
                if (hl != null)
                {
                    abuses.Remove(hl);
                    abuses.Add(new SyntheticAbuseRecord(
                        hl.Type,
                        duration * 0.90,
                        hl.DurationSeconds,
                        hl.Intensity,
                        hl.EngineIndex,
                        hl.TargetRuleId,
                        hl.TargetPartId,
                        hl.Notes));
                }

                // Garante que o cancelamento prematuro de voo ocorra após o término de abusos físicos anteriores
                var cancelAbuse = abuses.FirstOrDefault(a => a.Type == SyntheticAbuseType.DataCancelledMidFlight);
                if (cancelAbuse != null)
                {
                    double latestAbuseEnd = 0.0;
                    foreach (var ab in abuses)
                    {
                        if (ab.Type != SyntheticAbuseType.DataCancelledMidFlight)
                        {
                            latestAbuseEnd = Math.Max(latestAbuseEnd, ab.StartSeconds + ab.DurationSeconds);
                        }
                    }

                    abuses.RemoveAll(a => a.Type == SyntheticAbuseType.DataCancelledMidFlight);
                    if (latestAbuseEnd > 0)
                    {
                        double cancelTime = latestAbuseEnd + 15.0;
                        abuses.Add(new SyntheticAbuseRecord(
                            cancelAbuse.Type,
                            cancelTime,
                            0.0,
                            cancelAbuse.Intensity,
                            cancelAbuse.EngineIndex,
                            cancelAbuse.TargetRuleId,
                            cancelAbuse.TargetPartId,
                            cancelAbuse.Notes));

                        if (cancelTime + 10.0 > duration)
                        {
                            duration = cancelTime + 10.0;
                        }
                    }
                }
            }

            return new SyntheticFlightPlan
            {
                TotalDurationSeconds = duration,
                Abuses = abuses,
                SimRateSegments = simRateSegments
            };
        }

        private static SyntheticAbuseRecord CreateAbuseRecord(SyntheticAbuseType type, double totalDuration, int engine, IRandomSource rnd)
        {
            double intensity = 0.05 + rnd.NextDouble() * 0.20; // 5% a 25% de intensidade
            double startSec;
            double durationSec;
            string? ruleId = null;
            string? partId = null;

            switch (type)
            {
                case SyntheticAbuseType.LongStart:
                    startSec = 15.0;
                    durationSec = 38.0 + rnd.NextDouble() * 5.0; // > 35 s para disparar warning
                    ruleId = "engine_starter_duty_cycle";
                    partId = "engine.starter";
                    break;

                case SyntheticAbuseType.AvionicsMasterOnStart:
                    startSec = 15.0;
                    durationSec = 10.0;
                    ruleId = "avionics_start_with_avionics_master_on";
                    partId = "avionics.com_nav_radios";
                    break;

                case SyntheticAbuseType.RpmOverspeed:
                    startSec = totalDuration * 0.45;
                    durationSec = 18.0 + rnd.NextDouble() * 15.0;
                    ruleId = "piston_rpm_overspeed";
                    partId = "engine.cylinders";
                    break;

                case SyntheticAbuseType.ChtOverheat:
                    startSec = totalDuration * 0.40;
                    durationSec = 25.0 + rnd.NextDouble() * 25.0;
                    ruleId = "piston_cht_overheat";
                    partId = "engine.cylinders";
                    break;

                case SyntheticAbuseType.OilTempOverheat:
                    startSec = totalDuration * 0.40;
                    durationSec = 25.0 + rnd.NextDouble() * 25.0;
                    ruleId = "oil_temperature_high";
                    partId = "oil.cooler";
                    break;

                case SyntheticAbuseType.OilPressureLow:
                    startSec = totalDuration * 0.45; // em cruzeiro, bem após os 30s de motor ligado
                    durationSec = 25.0 + rnd.NextDouble() * 25.0;
                    ruleId = "oil_pressure_low";
                    partId = "engine.crankshaft";
                    break;

                case SyntheticAbuseType.ThermalShock:
                    startSec = totalDuration * 0.65; // em descida
                    durationSec = 70.0;
                    ruleId = "piston_shock_cooling";
                    partId = "engine.cylinders";
                    break;

                case SyntheticAbuseType.EgtHigh:
                    startSec = totalDuration * 0.50;
                    durationSec = 25.0 + rnd.NextDouble() * 20.0;
                    ruleId = "piston_egt_excess";
                    partId = "engine.valves";
                    break;

                case SyntheticAbuseType.FlapOverSpeed:
                    startSec = totalDuration * 0.75; // aproximação com flap
                    durationSec = 20.0 + rnd.NextDouble() * 15.0;
                    ruleId = "flap_overspeed_vfe";
                    partId = "airframe.flaps";
                    break;

                case SyntheticAbuseType.VneExceeded:
                    startSec = totalDuration * 0.60;
                    durationSec = 15.0 + rnd.NextDouble() * 15.0;
                    ruleId = "vne_airspeed_exceedance";
                    partId = "airframe.wing_structure";
                    break;

                case SyntheticAbuseType.PositiveGExceeded:
                    startSec = totalDuration * 0.46;
                    durationSec = 6.0 + rnd.NextDouble() * 8.0;
                    ruleId = "airframe_over_g_positive";
                    partId = "airframe.wing_structure";
                    break;

                case SyntheticAbuseType.NegativeGExceeded:
                    startSec = totalDuration * 0.54;
                    durationSec = 5.0 + rnd.NextDouble() * 6.0;
                    ruleId = "airframe_over_g_negative";
                    partId = "airframe.wing_structure";
                    break;

                case SyntheticAbuseType.HardLanding:
                    startSec = totalDuration * 0.90;
                    durationSec = 1.0;
                    intensity = 0.40 + rnd.NextDouble() * 0.40; // 40% a 80% acima do limite de fpm
                    ruleId = "touchdown_hard_landing";
                    partId = "gear.main_struts";
                    break;

                case SyntheticAbuseType.BatteryDischarging:
                    startSec = 30.0;
                    durationSec = 650.0; // longo para atingir > 10 min de descarga profunda
                    ruleId = "battery_deep_discharge_damage";
                    partId = "electrical.battery";
                    break;

                case SyntheticAbuseType.ElectricalOverload:
                    startSec = 40.0;
                    durationSec = 350.0; // > 5 min de sobrecarga
                    ruleId = "electrical_system_overload_amps";
                    partId = "electrical.alternator";
                    break;

                case SyntheticAbuseType.GearCycles:
                    startSec = totalDuration * 0.50;
                    durationSec = 30.0;
                    ruleId = "gear_cycle_stress";
                    partId = "gear.actuator";
                    break;

                case SyntheticAbuseType.DataCancelledMidFlight:
                    startSec = totalDuration * 0.50;
                    durationSec = 0.0;
                    break;

                case SyntheticAbuseType.DataFrameGap:
                    startSec = totalDuration * 0.30;
                    durationSec = 15.0; // > 10 s
                    break;

                case SyntheticAbuseType.DataSimPause:
                    startSec = totalDuration * 0.40;
                    durationSec = 20.0;
                    break;

                case SyntheticAbuseType.DataMissingSensor:
                case SyntheticAbuseType.DataNanValues:
                default:
                    startSec = totalDuration * 0.35;
                    durationSec = 30.0;
                    break;
            }

            return new SyntheticAbuseRecord(
                type,
                startSec,
                durationSec,
                intensity,
                engine,
                ruleId,
                partId,
                $"Abuso {type} no motor {engine} com intensidade {intensity:P1}");
        }
    }
}
