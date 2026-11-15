using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Limits;

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
                    SyntheticAbuseType.RpmOverspeed,
                    SyntheticAbuseType.OilTempOverheat,
                    SyntheticAbuseType.OilPressureLow,
                    SyntheticAbuseType.FlapOverSpeed,
                    SyntheticAbuseType.VneExceeded,
                    SyntheticAbuseType.PositiveGExceeded,
                    SyntheticAbuseType.NegativeGExceeded,
                    SyntheticAbuseType.HardLanding,
                    SyntheticAbuseType.LongStart,
                    SyntheticAbuseType.AvionicsMasterOnStart,
                    SyntheticAbuseType.BatteryDischarging,
                    SyntheticAbuseType.ElectricalOverload,
                    SyntheticAbuseType.DataMissingSensor,
                    SyntheticAbuseType.DataNanValues,
                    SyntheticAbuseType.DataFrameGap,
                    SyntheticAbuseType.DataSimPause,
                    SyntheticAbuseType.DataCancelledMidFlight
                };

                if (isPiston)
                {
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

                    int targetEngine = 1;
                    if (engineCount > 1)
                    {
                        // Sorteia motor 1..engineCount
                        targetEngine = 1 + (int)Math.Floor(rnd.NextDouble() * engineCount);
                        targetEngine = Math.Clamp(targetEngine, 1, engineCount);
                    }

                    var record = CreateAbuseRecord(abuseType, duration, targetEngine, rnd);
                    abuses.Add(record);
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
                    durationSec = 35.0 + rnd.NextDouble() * 10.0; // > 30 s
                    ruleId = "starter_crank_time";
                    partId = "eng.starter";
                    break;

                case SyntheticAbuseType.AvionicsMasterOnStart:
                    startSec = 15.0;
                    durationSec = 10.0;
                    ruleId = "avionics_start_with_avionics_master_on";
                    partId = "elec.avionics";
                    break;

                case SyntheticAbuseType.RpmOverspeed:
                    startSec = totalDuration * 0.45;
                    durationSec = 15.0 + rnd.NextDouble() * 20.0;
                    ruleId = "piston_rpm_overspeed";
                    partId = "eng.pistons";
                    break;

                case SyntheticAbuseType.ChtOverheat:
                    startSec = totalDuration * 0.40;
                    durationSec = 20.0 + rnd.NextDouble() * 30.0;
                    ruleId = "cylinder_cht_overheat";
                    partId = "eng.cylinders";
                    break;

                case SyntheticAbuseType.OilTempOverheat:
                    startSec = totalDuration * 0.40;
                    durationSec = 25.0 + rnd.NextDouble() * 25.0;
                    ruleId = "engine_oil_temperature_high";
                    partId = "oil.lines";
                    break;

                case SyntheticAbuseType.OilPressureLow:
                    startSec = totalDuration * 0.45; // em cruzeiro, bem após os 30s de motor ligado
                    durationSec = 20.0 + rnd.NextDouble() * 25.0;
                    ruleId = "oil_pressure_low";
                    partId = "eng.bearings";
                    break;

                case SyntheticAbuseType.ThermalShock:
                    startSec = totalDuration * 0.65; // em descida
                    durationSec = 60.0;
                    ruleId = "piston_shock_cooling";
                    partId = "eng.cylinders";
                    break;

                case SyntheticAbuseType.EgtHigh:
                    startSec = totalDuration * 0.50;
                    durationSec = 20.0 + rnd.NextDouble() * 20.0;
                    ruleId = "exhaust_egt_overtemp";
                    partId = "eng.exhaust";
                    break;

                case SyntheticAbuseType.FlapOverSpeed:
                    startSec = totalDuration * 0.75; // aproximação com flap
                    durationSec = 15.0 + rnd.NextDouble() * 15.0;
                    ruleId = "flaps_overspeed_band";
                    partId = "airframe.flaps";
                    break;

                case SyntheticAbuseType.VneExceeded:
                    startSec = totalDuration * 0.60;
                    durationSec = 10.0 + rnd.NextDouble() * 15.0;
                    ruleId = "airframe_overspeed_vne";
                    partId = "airframe.structure";
                    break;

                case SyntheticAbuseType.PositiveGExceeded:
                    startSec = totalDuration * 0.50;
                    durationSec = 5.0 + rnd.NextDouble() * 8.0;
                    ruleId = "airframe_positive_g_exceedance";
                    partId = "airframe.wings";
                    break;

                case SyntheticAbuseType.NegativeGExceeded:
                    startSec = totalDuration * 0.50;
                    durationSec = 4.0 + rnd.NextDouble() * 6.0;
                    ruleId = "airframe_negative_g_exceedance";
                    partId = "airframe.wings";
                    break;

                case SyntheticAbuseType.HardLanding:
                    startSec = totalDuration * 0.85;
                    durationSec = 1.0;
                    intensity = 0.30 + rnd.NextDouble() * 0.50; // 30% a 80% acima do limite de fpm
                    ruleId = "touchdown_hard_landing";
                    partId = "gear.main_struts";
                    break;

                case SyntheticAbuseType.BatteryDischarging:
                    startSec = totalDuration * 0.20;
                    durationSec = 600.0; // longo para atingir minutos de descarga profunda se duração permitir
                    ruleId = "battery_deep_discharge";
                    partId = "elec.battery";
                    break;

                case SyntheticAbuseType.ElectricalOverload:
                    startSec = totalDuration * 0.25;
                    durationSec = 350.0;
                    ruleId = "electrical_system_overload_amps";
                    partId = "elec.alternator";
                    break;

                case SyntheticAbuseType.GearCycles:
                    startSec = totalDuration * 0.50;
                    durationSec = 30.0;
                    ruleId = "gear_excessive_cycling";
                    partId = "gear.actuators";
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
