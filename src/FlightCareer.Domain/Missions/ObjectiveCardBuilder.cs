using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed record ObjectiveCardMission(string Title, string Route);

    public sealed record ObjectiveCardContent(
        IReadOnlyList<ObjectiveCardMission> Missions,
        IReadOnlyList<ContractObjectiveLine> Objectives);

    public static class ObjectiveCardBuilder
    {
        public static ObjectiveCardContent Build(
            IReadOnlyList<Mission>? missions,
            IReadOnlyList<ContractObjectiveLine>? lines)
        {
            if (missions == null || missions.Count == 0)
            {
                return new ObjectiveCardContent(
                    Array.Empty<ObjectiveCardMission>(),
                    Array.Empty<ContractObjectiveLine>());
            }

            var missionList = missions.Select(m => new ObjectiveCardMission(
                m.Title ?? string.Empty,
                $"{m.OriginICAO} → {m.DestinationICAO}"
            )).ToList();

            var objectives = new List<ContractObjectiveLine>(lines ?? Array.Empty<ContractObjectiveLine>());

            var distinctDestinations = missions
                .Select(m => m.DestinationICAO)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct();

            foreach (var dest in distinctDestinations)
            {
                objectives.Add(new ContractObjectiveLine($"Pouso final em {dest}", ContractObjectiveState.Pending));
            }

            return new ObjectiveCardContent(missionList, objectives);
        }
    }
}
