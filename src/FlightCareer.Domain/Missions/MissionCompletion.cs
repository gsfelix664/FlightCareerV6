using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed record MissionCompletionResult(bool IsCompleted, string? Reason, string? Warning = null);

    /// <summary>
    /// Avalia a conclusão operacional de missões:
    /// - Padrão e circuitos TGL;
    /// - Sobrevoos panorâmicos e aerofotogrametria (survey);
    /// - Paraquedismo (skydive) e choque térmico;
    /// - Pinga-pinga regional (multi_stop) e proibição de reabastecimento;
    /// - Navegação VFR em triângulo com reporte e escalas;
    /// - Treinamento bush / STOL com rampa e parada curta;
    /// - Voo de teste pós-manutenção (check_flight) por etapas;
    /// - Translado com restrição técnica (ferry_permit).
    /// </summary>
    public static class MissionCompletion
    {
        public static MissionCompletionResult Evaluate(
            Mission mission,
            string landedIcao,
            IReadOnlySet<string>? visitedWaypoints = null,
            int touchAndGoCount = 0,
            int surveyLegsCompleted = 0,
            bool skydiveDropCompleted = false,
            bool skydiveThermalShock = false,
            double skydiveMaxCoolingRate = 0.0,
            MissionTrackerResults? trackers = null)
        {
            if (mission == null)
            {
                return new MissionCompletionResult(false, "Missão inválida (nula).");
            }

            // Integração dos resultados agregados de trackers
            if (trackers != null)
            {
                visitedWaypoints = trackers.VisitedWaypoints ?? visitedWaypoints;
                if (trackers.TouchAndGoCount > 0) touchAndGoCount = trackers.TouchAndGoCount;
                if (trackers.SurveyLegsCompleted > 0) surveyLegsCompleted = trackers.SurveyLegsCompleted;
                if (trackers.SkydiveDropCompleted) skydiveDropCompleted = true;
                if (trackers.SkydiveThermalShock) skydiveThermalShock = true;
                if (trackers.SkydiveMaxCoolingRate > 0) skydiveMaxCoolingRate = trackers.SkydiveMaxCoolingRate;
            }

            if (!string.Equals(mission.DestinationICAO, landedIcao, StringComparison.OrdinalIgnoreCase))
            {
                return new MissionCompletionResult(
                    false,
                    $"Pousou em {landedIcao}; destino exigido era {mission.DestinationICAO}.");
            }

            if (mission.RequiredTouchAndGoes > 0 && touchAndGoCount < mission.RequiredTouchAndGoes)
            {
                return new MissionCompletionResult(
                    false,
                    $"Circuito incompleto: {touchAndGoCount} de {mission.RequiredTouchAndGoes} TGL(s) realizados.");
            }

            if (!string.IsNullOrWhiteSpace(mission.OverflyWaypoints))
            {
                var waypoints = OverflyWaypointCodec.Parse(mission.OverflyWaypoints);

                var missing = new List<string>();
                foreach (var wp in waypoints)
                {
                    if (visitedWaypoints == null || !visitedWaypoints.Contains(wp.Ident))
                    {
                        missing.Add(wp.Ident);
                    }
                }

                if (missing.Count > 0)
                {
                    return new MissionCompletionResult(
                        false,
                        $"Sobrevoo incompleto: ponto(s) não visitado(s): {string.Join(", ", missing)}.");
                }
            }

            var def = MissionCatalog.Shared.Get(mission.SubId);
            string generator = def?.Generator ?? string.Empty;

            // Levantamento / Fotografia Aérea (3.6.1)
            if (generator == "survey")
            {
                int requiredLegs = 3;
                if (!string.IsNullOrWhiteSpace(mission.ContractData))
                {
                    try
                    {
                        var survey = JsonSerializer.Deserialize<SurveyContractData>(mission.ContractData);
                        if (survey != null && survey.RequiredLegs > 0)
                        {
                            requiredLegs = survey.RequiredLegs;
                        }
                    }
                    catch { }
                }

                if (surveyLegsCompleted < requiredLegs)
                {
                    return new MissionCompletionResult(
                        false,
                        $"Levantamento incompleto: {surveyLegsCompleted} de {requiredLegs} pernas concluídas.");
                }
            }

            // Lançamento de Paraquedistas (3.6.2)
            if (generator == "skydive")
            {
                if (!skydiveDropCompleted)
                {
                    return new MissionCompletionResult(
                        false,
                        "Lançamento incompleto: não estabilizou na altitude de salto sobre a zona.");
                }

                string? warning = null;
                if (skydiveThermalShock)
                {
                    warning = $"Choque térmico na descida: taxa máxima de resfriamento {skydiveMaxCoolingRate:F1} °C/min (limite: 28 °C/min).";
                }

                return new MissionCompletionResult(true, null, warning);
            }

            // Pinga-Pinga Regional (multi_stop)
            if (generator == "multi_stop")
            {
                if (trackers?.MultiStop != null)
                {
                    if (trackers.MultiStop.RefuelViolationDetected)
                    {
                        return new MissionCompletionResult(false, trackers.MultiStop.FailureReason ?? "Reabasteceu entre escalas.");
                    }

                    if (!trackers.MultiStop.AllStopsCompleted)
                    {
                        return new MissionCompletionResult(false, trackers.MultiStop.FailureReason ?? $"Escala {trackers.MultiStop.NextStopIcao} não visitada.");
                    }
                }
            }

            // Navegação VFR em Triângulo (vfr_triangle)
            if (generator == "vfr_triangle")
            {
                if (trackers?.MultiStop != null)
                {
                    if (!trackers.MultiStop.AllStopsCompleted)
                    {
                        return new MissionCompletionResult(false, trackers.MultiStop.FailureReason ?? $"Escala {trackers.MultiStop.NextStopIcao} não visitada.");
                    }
                }
            }

            // Treinamento STOL / Bush (stol_training)
            if (generator == "stol_training")
            {
                int requiredLandings = 3;
                if (!string.IsNullOrWhiteSpace(mission.ContractData))
                {
                    try
                    {
                        var stolData = JsonSerializer.Deserialize<StolContractData>(mission.ContractData);
                        if (stolData != null && stolData.RequiredLandings > 0)
                        {
                            requiredLandings = stolData.RequiredLandings;
                        }
                    }
                    catch { }
                }

                int validLandings = trackers?.Stol?.ValidLandingsCount ?? 0;
                if (validLandings < requiredLandings)
                {
                    return new MissionCompletionResult(
                        false,
                        $"Treinamento STOL incompleto: {validLandings} de {requiredLandings} pouso(s) curto(s) válido(s).");
                }

                string? stolWarning = null;
                if (trackers?.Stol?.HadCrosswindBonus == true)
                {
                    stolWarning = "Pouso curto executado com vento de través relevante (+bônus operacional).";
                }

                return new MissionCompletionResult(true, null, stolWarning);
            }

            // Voo de Teste Pós-Manutenção (check_flight)
            if (generator == "check_flight")
            {
                if (trackers?.CheckFlight != null && !trackers.CheckFlight.IsCompleted)
                {
                    return new MissionCompletionResult(
                        false,
                        trackers.CheckFlight.GetFailureReason() ?? "Voo de teste pós-manutenção incompleto.");
                }
            }

            // Translado com Restrição Técnica (ferry_permit)
            if (generator == "ferry_permit")
            {
                if (trackers?.Ferry != null && trackers.Ferry.HasViolation)
                {
                    return new MissionCompletionResult(
                        false,
                        trackers.Ferry.FailureReason ?? "Restrição técnica do translado violada.");
                }
            }

            return new MissionCompletionResult(true, null);
        }
    }
}
