using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed record MissionCompletionResult(bool IsCompleted, string? Reason, string? Warning = null);

    /// <summary>
    /// Avalia a conclusão operacional de missões normais, circuitos de TGL, voos panorâmicos com sobrevoos,
    /// levantamento fotográfico aéreo (3.6.1) e lançamento de paraquedistas (3.6.2).
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
            double skydiveMaxCoolingRate = 0.0)
        {
            if (mission == null)
            {
                return new MissionCompletionResult(false, "Missão inválida (nula).");
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

            // Levantamento / Fotografia Aérea (3.6.1)
            if (def?.Generator == "survey")
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
            if (def?.Generator == "skydive")
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

            return new MissionCompletionResult(true, null);
        }
    }
}
