using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed record ContractProgressInputs(
        int TouchAndGoCount = 0,
        OverflyTracker? Overfly = null,
        SurveyLegTracker? Survey = null,
        SkydiveDropTracker? Skydive = null,
        MultiStopTracker? MultiStop = null,
        StolLandingTracker? Stol = null,
        CheckFlightTracker? CheckFlight = null,
        FerryRestrictionMonitor? Ferry = null);

    public static class ContractProgressFormatter
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public static string Build(IReadOnlyList<Mission>? missions, ContractProgressInputs inputs)
        {
            if (inputs == null) return string.Empty;

            var lines = new List<string>(8);

            // 1. TGL (se max(RequiredTouchAndGoes) das missões > 0)
            int reqTgl = (missions != null && missions.Count > 0)
                ? missions.Max(m => m.RequiredTouchAndGoes)
                : 0;
            if (reqTgl > 0)
            {
                string check = inputs.TouchAndGoCount >= reqTgl ? " ✓" : "";
                lines.Add($"TGL {inputs.TouchAndGoCount}/{reqTgl}{check}");
            }

            // 2. Sobrevoo (se Overfly?.HasWaypoints)
            if (inputs.Overfly != null && inputs.Overfly.HasWaypoints)
            {
                var targets = inputs.Overfly.Targets;
                int total = targets.Count;
                int visited = targets.Count(t => inputs.Overfly.IsPointVisited(t.Ident));

                if (visited >= total && total > 0)
                {
                    lines.Add($"Pontos {total}/{total} ✓");
                }
                else
                {
                    string line = $"Pontos {visited}/{total}";
                    if (inputs.Overfly.LastLatitude.HasValue &&
                        inputs.Overfly.LastLongitude.HasValue &&
                        inputs.Overfly.LastAltitudeFt.HasValue)
                    {
                        double lat = inputs.Overfly.LastLatitude.Value;
                        double lon = inputs.Overfly.LastLongitude.Value;
                        double alt = inputs.Overfly.LastAltitudeFt.Value;

                        var unvisited = targets.Where(t => !inputs.Overfly.IsPointVisited(t.Ident)).ToList();
                        if (unvisited.Count > 0)
                        {
                            var nearest = unvisited
                                .OrderBy(t => OverflyTracker.CalculateDistanceNM(lat, lon, t.Latitude, t.Longitude))
                                .First();

                            double dist = OverflyTracker.CalculateDistanceNM(lat, lon, nearest.Latitude, nearest.Longitude);
                            var altState = OverflyTracker.EvaluateAltitude(nearest, alt);

                            if (nearest.MinAglFt > 0.0)
                            {
                                string estado = altState switch
                                {
                                    OverflyAltitudeState.Below => "abaixo",
                                    OverflyAltitudeState.Above => "acima",
                                    _ => "na faixa"
                                };
                                line += $" · {nearest.Ident} {dist.ToString("N1", PtBr)} NM · faixa {(nearest.ElevationFt + nearest.MinAglFt).ToString("N0", PtBr)}–{(nearest.ElevationFt + nearest.MaxAglFt).ToString("N0", PtBr)} ft: {estado}";
                            }
                            else
                            {
                                string estado = altState == OverflyAltitudeState.Above ? "acima" : "ok";
                                line += $" · {nearest.Ident} {dist.ToString("N1", PtBr)} NM · máx. {(nearest.ElevationFt + nearest.MaxAglFt).ToString("N0", PtBr)} ft: {estado}";
                            }
                        }
                    }
                    lines.Add(line);
                }
            }

            // 3. Escalas (se MultiStop?.HasActiveContract)
            if (inputs.MultiStop != null && inputs.MultiStop.HasActiveContract)
            {
                var ms = inputs.MultiStop;
                if (ms.RefuelViolationDetected)
                {
                    lines.Add("✖ Reabasteceu entre escalas");
                }
                else if (ms.OrderViolationDetected)
                {
                    lines.Add($"✖ {ms.FailureReason}");
                }
                else if (ms.AllStopsCompleted)
                {
                    lines.Add($"Escalas {ms.TotalStopsCount}/{ms.TotalStopsCount} ✓");
                }
                else
                {
                    lines.Add($"Escala {ms.CompletedStopsCount}/{ms.TotalStopsCount} · próxima {ms.NextStopIcao}");
                }
            }

            // 4. Voo de teste (se CheckFlight?.HasActiveContract)
            if (inputs.CheckFlight != null && inputs.CheckFlight.HasActiveContract)
            {
                var cf = inputs.CheckFlight;
                if (cf.IsCompleted)
                {
                    lines.Add("Voo de teste ✓");
                }
                else if (!cf.Step1Reached)
                {
                    double testAlt = cf.ContractData?.TestAltitudeFt ?? 0.0;
                    lines.Add($"Passo 1/3 · subir a {testAlt.ToString("N0", PtBr)} ft");
                }
                else if (!cf.Step2PowerChecked)
                {
                    double powerSec = cf.ContractData?.PowerHoldSeconds ?? 0.0;
                    lines.Add($"Passo 2/3 · potência máxima por {powerSec.ToString("N0", PtBr)} s");
                }
                else
                {
                    string mN = cf.HeldN ? "✓" : "·";
                    string mE = cf.HeldE ? "✓" : "·";
                    string mS = cf.HeldS ? "✓" : "·";
                    string mW = cf.HeldW ? "✓" : "·";
                    lines.Add($"Passo 3/3 · rumos N{mN} E{mE} S{mS} W{mW}");
                }
            }

            // 5. STOL
            if (inputs.Stol != null && inputs.Stol.HasActiveContract)
            {
                var stol = inputs.Stol;
                string check = stol.IsCompleted ? " ✓" : "";
                lines.Add($"Pousos curtos {stol.ValidLandingsCount}/{stol.RequiredLandings}{check}");
            }

            // 6. Levantamento
            if (inputs.Survey != null && inputs.Survey.HasActiveContract)
            {
                var survey = inputs.Survey;
                string check = survey.IsCompleted ? " ✓" : "";
                lines.Add($"Pernas {survey.CompletedLegs}/{survey.RequiredLegs}{check}");
            }

            // 7. Paraquedismo
            if (inputs.Skydive != null && inputs.Skydive.HasActiveContract)
            {
                var skydive = inputs.Skydive;
                if (skydive.DropCompleted)
                {
                    lines.Add("Lançamento ✓");
                }
                else
                {
                    lines.Add($"Lançamento · estável {skydive.StableSeconds.ToString("N0", PtBr)} s");
                }
            }

            // 8. Translado (se Ferry?.HasActiveContract)
            if (inputs.Ferry != null && inputs.Ferry.HasActiveContract)
            {
                var ferry = inputs.Ferry;
                if (ferry.HasViolation)
                {
                    lines.Add($"✖ {ferry.FailureReason}");
                }
                else if (ferry.ContractData?.Restriction == "max_altitude")
                {
                    lines.Add($"Restrição: máx. {ferry.ContractData.MaxAltitudeFt.ToString("N0", PtBr)} ft");
                }
                else if (ferry.ContractData?.Restriction == "flaps_fixed")
                {
                    lines.Add("Restrição: flaps fixos");
                }
            }

            if (lines.Count == 0) return string.Empty;

            return string.Join("\n", lines.Take(3));
        }
    }
}
