using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public enum ContractObjectiveState { Pending, Done, Failed }

    public sealed record ContractObjectiveLine(string Text, ContractObjectiveState State);

    public sealed record ContractProgressInputs(
        int TouchAndGoCount = 0,
        OverflyTracker? Overfly = null,
        SurveyLegTracker? Survey = null,
        SkydiveDropTracker? Skydive = null,
        MultiStopTracker? MultiStop = null,
        StolLandingTracker? Stol = null,
        CheckFlightTracker? CheckFlight = null,
        FerryRestrictionMonitor? Ferry = null,
        AerialWorkTracker? AerialWork = null);

    public static class ContractProgressFormatter
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public static IReadOnlyList<ContractObjectiveLine> BuildLines(IReadOnlyList<Mission>? missions, ContractProgressInputs inputs)
        {
            if (inputs == null) return Array.Empty<ContractObjectiveLine>();

            var lines = new List<ContractObjectiveLine>(8);

            // 1. TGL (se max(RequiredTouchAndGoes) das missões > 0)
            int reqTgl = (missions != null && missions.Count > 0)
                ? missions.Max(m => m.RequiredTouchAndGoes)
                : 0;
            if (reqTgl > 0)
            {
                var state = inputs.TouchAndGoCount >= reqTgl ? ContractObjectiveState.Done : ContractObjectiveState.Pending;
                lines.Add(new ContractObjectiveLine($"TGL {inputs.TouchAndGoCount}/{reqTgl}", state));
            }

            // 2. Sobrevoo (se Overfly?.HasWaypoints)
            if (inputs.Overfly != null && inputs.Overfly.HasWaypoints)
            {
                var targets = inputs.Overfly.Targets;
                int total = targets.Count;
                int visited = targets.Count(t => inputs.Overfly.IsPointVisited(t.Ident));

                if (visited >= total && total > 0)
                {
                    lines.Add(new ContractObjectiveLine($"Pontos {total}/{total}", ContractObjectiveState.Done));
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
                    lines.Add(new ContractObjectiveLine(line, ContractObjectiveState.Pending));
                }
            }

            // 3. Escalas (se MultiStop?.HasActiveContract)
            if (inputs.MultiStop != null && inputs.MultiStop.HasActiveContract)
            {
                var ms = inputs.MultiStop;
                if (ms.RefuelViolationDetected)
                {
                    lines.Add(new ContractObjectiveLine("Reabasteceu entre escalas", ContractObjectiveState.Failed));
                }
                else if (ms.OrderViolationDetected)
                {
                    lines.Add(new ContractObjectiveLine(ms.FailureReason ?? string.Empty, ContractObjectiveState.Failed));
                }
                else if (ms.AllStopsCompleted)
                {
                    lines.Add(new ContractObjectiveLine($"Escalas {ms.TotalStopsCount}/{ms.TotalStopsCount}", ContractObjectiveState.Done));
                }
                else
                {
                    lines.Add(new ContractObjectiveLine($"Escala {ms.CompletedStopsCount}/{ms.TotalStopsCount} · próxima {ms.NextStopIcao}", ContractObjectiveState.Pending));
                }
            }

            // 4. Voo de teste (se CheckFlight?.HasActiveContract)
            if (inputs.CheckFlight != null && inputs.CheckFlight.HasActiveContract)
            {
                var cf = inputs.CheckFlight;
                if (cf.IsCompleted)
                {
                    lines.Add(new ContractObjectiveLine("Voo de teste", ContractObjectiveState.Done));
                }
                else if (!cf.Step1Reached)
                {
                    double testAlt = cf.ContractData?.TestAltitudeFt ?? 0.0;
                    lines.Add(new ContractObjectiveLine($"Passo 1/3 · subir a {testAlt.ToString("N0", PtBr)} ft", ContractObjectiveState.Pending));
                }
                else if (!cf.Step2PowerChecked)
                {
                    double powerSec = cf.ContractData?.PowerHoldSeconds ?? 0.0;
                    lines.Add(new ContractObjectiveLine($"Passo 2/3 · potência máxima por {powerSec.ToString("N0", PtBr)} s", ContractObjectiveState.Pending));
                }
                else
                {
                    string mN = cf.HeldN ? "✓" : "·";
                    string mE = cf.HeldE ? "✓" : "·";
                    string mS = cf.HeldS ? "✓" : "·";
                    string mW = cf.HeldW ? "✓" : "·";
                    lines.Add(new ContractObjectiveLine($"Passo 3/3 · rumos N{mN} E{mE} S{mS} W{mW}", ContractObjectiveState.Pending));
                }
            }

            // 5. STOL
            if (inputs.Stol != null && inputs.Stol.HasActiveContract)
            {
                var stol = inputs.Stol;
                var state = stol.IsCompleted ? ContractObjectiveState.Done : ContractObjectiveState.Pending;
                lines.Add(new ContractObjectiveLine($"Pousos curtos {stol.ValidLandingsCount}/{stol.RequiredLandings}", state));
            }

            // 6. Levantamento
            if (inputs.Survey != null && inputs.Survey.HasActiveContract)
            {
                var survey = inputs.Survey;
                var state = survey.IsCompleted ? ContractObjectiveState.Done : ContractObjectiveState.Pending;
                lines.Add(new ContractObjectiveLine($"Pernas {survey.CompletedLegs}/{survey.RequiredLegs}", state));
            }

            // 7. Paraquedismo
            if (inputs.Skydive != null && inputs.Skydive.HasActiveContract)
            {
                var skydive = inputs.Skydive;
                if (skydive.DropCompleted)
                {
                    lines.Add(new ContractObjectiveLine("Lançamento", ContractObjectiveState.Done));
                }
                else
                {
                    lines.Add(new ContractObjectiveLine($"Lançamento · estável {skydive.StableSeconds.ToString("N0", PtBr)} s", ContractObjectiveState.Pending));
                }
            }

            // 8. Translado (se Ferry?.HasActiveContract)
            if (inputs.Ferry != null && inputs.Ferry.HasActiveContract)
            {
                var ferry = inputs.Ferry;
                if (ferry.HasViolation)
                {
                    lines.Add(new ContractObjectiveLine(ferry.FailureReason ?? string.Empty, ContractObjectiveState.Failed));
                }
                else if (ferry.ContractData?.Restriction == "max_altitude")
                {
                    lines.Add(new ContractObjectiveLine($"Restrição: máx. {ferry.ContractData.MaxAltitudeFt.ToString("N0", PtBr)} ft", ContractObjectiveState.Pending));
                }
                else if (ferry.ContractData?.Restriction == "flaps_fixed")
                {
                    lines.Add(new ContractObjectiveLine("Restrição: flaps fixos", ContractObjectiveState.Pending));
                }
            }

            // 9. Trabalho Aéreo (se inputs.AerialWork?.HasActiveContract)
            if (inputs.AerialWork != null && inputs.AerialWork.HasActiveContract)
            {
                var aw = inputs.AerialWork;
                string mode = aw.Mode.ToLowerInvariant();
                string line = string.Empty;
                var state = ContractObjectiveState.Pending;

                if (mode == "sectors")
                {
                    if (aw.IsCompleted)
                    {
                        line = $"Setores {aw.TotalPoints}/{aw.TotalPoints}";
                        state = ContractObjectiveState.Done;
                    }
                    else
                    {
                        line = $"Setores {aw.CompletedPoints}/{aw.TotalPoints}";
                        if (!string.IsNullOrEmpty(aw.NextPointIdent))
                        {
                            line += $" · {aw.NextPointIdent} {aw.DistanceToNextNm.ToString("N1", PtBr)} NM";
                            if (aw.ContractData != null)
                            {
                                double refElev = aw.ContractData.RefElevationFt;
                                double minAgl = aw.ContractData.MinAglFt;
                                double maxAgl = aw.ContractData.MaxAglFt;
                                if (minAgl > 0.0)
                                {
                                    string estado = aw.AltitudeState switch
                                    {
                                        AerialWorkAltitudeState.Below => "abaixo",
                                        AerialWorkAltitudeState.Above => "acima",
                                        _ => "na faixa"
                                    };
                                    line += $" · faixa {(refElev + minAgl).ToString("N0", PtBr)}–{(refElev + maxAgl).ToString("N0", PtBr)} ft: {estado}";
                                }
                                else
                                {
                                    string estado = aw.AltitudeState == AerialWorkAltitudeState.Above ? "acima" : "ok";
                                    line += $" · máx. {(refElev + maxAgl).ToString("N0", PtBr)} ft: {estado}";
                                }
                            }
                        }
                    }
                }
                else if (mode == "route")
                {
                    if (aw.IsCompleted)
                    {
                        line = $"Traçado {aw.TotalPoints}/{aw.TotalPoints}";
                        state = ContractObjectiveState.Done;
                    }
                    else
                    {
                        line = $"Traçado {aw.CompletedPoints}/{aw.TotalPoints} · {aw.NextPointIdent} {aw.DistanceToNextNm.ToString("N1", PtBr)} NM · desvio {aw.LateralDeviationNm.ToString("N1", PtBr)} NM";
                    }
                }
                else if (mode == "orbit")
                {
                    int totalMin = (int)(aw.ContractData?.OrbitMinutes ?? 0);
                    int accumMin = (int)Math.Floor(aw.OrbitSecondsAccumulated / 60.0);
                    if (aw.IsCompleted)
                    {
                        line = $"Órbita {totalMin}/{totalMin} min";
                        state = ContractObjectiveState.Done;
                    }
                    else
                    {
                        line = $"Órbita {accumMin}/{totalMin} min · {aw.DistanceToNextNm.ToString("N1", PtBr)} NM";
                    }
                }
                else if (mode == "search")
                {
                    if (aw.TargetFound)
                    {
                        line = "Alvo encontrado";
                        state = ContractObjectiveState.Done;
                    }
                    else
                    {
                        int leg = Math.Min(aw.CompletedPoints + 1, aw.TotalPoints);
                        line = $"Busca: perna {leg}/{aw.TotalPoints} · alvo não achado";
                    }
                }

                if (!string.IsNullOrEmpty(line))
                {
                    if (!aw.IsCompleted && aw.IsIasExceeded && aw.ContractData?.MaxIasKt > 0)
                    {
                        line += $" · reduza para {aw.ContractData.MaxIasKt.ToString("N0", PtBr)} kt";
                    }
                    lines.Add(new ContractObjectiveLine(line, state));
                }
            }

            return lines;
        }

        public static string FormatHud(IReadOnlyList<ContractObjectiveLine> lines)
        {
            if (lines == null || lines.Count == 0) return string.Empty;

            return string.Join("\n", lines.Take(3).Select(l => l.State switch
            {
                ContractObjectiveState.Done => l.Text + " ✓",
                ContractObjectiveState.Failed => "✖ " + l.Text,
                _ => l.Text
            }));
        }

        public static string Build(IReadOnlyList<Mission>? missions, ContractProgressInputs inputs)
            => FormatHud(BuildLines(missions, inputs));
    }
}
