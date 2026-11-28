using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Utils;

namespace FlightCareer.Domain.Missions
{
    public sealed record AirportPosition(string Icao, double Latitude, double Longitude);

    public sealed record ObjectiveCardMission(string Title, string Route);

    public sealed record ObjectiveCardContent(
        IReadOnlyList<ObjectiveCardMission> Missions,
        IReadOnlyList<ContractObjectiveLine> Objectives);

    public static class ObjectiveCardBuilder
    {
        public const double MaxLandingAirportRadiusNm = 3.0;

        /// <summary>
        /// Determina o ICAO do aeroporto mais próximo quando a aeronave já realizou o toque
        /// e permanece no solo dentro do raio de tolerância, sem invalidação do voo.
        /// </summary>
        public static string? ResolveLandedIcao(
            bool isOnGround,
            bool hasTouchedDown,
            bool isFlightInvalidated,
            double latitude,
            double longitude,
            IReadOnlyList<AirportPosition>? candidateAirports)
        {
            if (!isOnGround || !hasTouchedDown || isFlightInvalidated || candidateAirports == null || candidateAirports.Count == 0)
            {
                return null;
            }

            string? nearestIcao = null;
            double minDistance = double.MaxValue;

            for (int i = 0; i < candidateAirports.Count; i++)
            {
                var airport = candidateAirports[i];
                if (string.IsNullOrWhiteSpace(airport.Icao)) continue;

                double distance = LocationUtils.CalculateDistance(latitude, longitude, airport.Latitude, airport.Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIcao = airport.Icao;
                }
            }

            return nearestIcao != null && minDistance <= MaxLandingAirportRadiusNm
                ? nearestIcao
                : null;
        }

        public static ObjectiveCardContent Build(
            IReadOnlyList<Mission>? missions,
            IReadOnlyList<ContractObjectiveLine>? lines,
            string? landedIcao = null,
            MissionTrackerResults? trackers = null)
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
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var dest in distinctDestinations)
            {
                var state = ContractObjectiveState.Pending;
                if (!string.IsNullOrWhiteSpace(landedIcao) &&
                    string.Equals(dest, landedIcao, StringComparison.OrdinalIgnoreCase))
                {
                    var destMissions = missions
                        .Where(m => string.Equals(m.DestinationICAO, dest, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (destMissions.Count > 0 &&
                        destMissions.All(m => MissionCompletion.Evaluate(m, landedIcao!, trackers: trackers).IsCompleted))
                    {
                        state = ContractObjectiveState.Done;
                    }
                }

                objectives.Add(new ContractObjectiveLine($"Pouso final em {dest}", state));
            }

            return new ObjectiveCardContent(missionList, objectives);
        }
    }
}
