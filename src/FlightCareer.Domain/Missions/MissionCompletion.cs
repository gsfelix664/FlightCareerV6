using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed record MissionCompletionResult(bool IsCompleted, string? Reason);

    /// <summary>
    /// Avalia a conclusão operacional de missões normais, circuitos de TGL e voos panorâmicos com sobrevoos.
    /// </summary>
    public static class MissionCompletion
    {
        public static MissionCompletionResult Evaluate(
            Mission mission,
            string landedIcao,
            IReadOnlySet<string>? visitedWaypoints,
            int touchAndGoCount)
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
                var waypoints = mission.OverflyWaypoints
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var missing = new List<string>();
                foreach (var wp in waypoints)
                {
                    if (visitedWaypoints == null || !visitedWaypoints.Contains(wp))
                    {
                        missing.Add(wp);
                    }
                }

                if (missing.Count > 0)
                {
                    return new MissionCompletionResult(
                        false,
                        $"Sobrevoo incompleto: ponto(s) não visitado(s): {string.Join(", ", missing)}.");
                }
            }

            return new MissionCompletionResult(true, null);
        }
    }
}
