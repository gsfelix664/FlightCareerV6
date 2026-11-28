using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public sealed record HudRouteDisplay(
        string RouteText,
        string OriginIcao,
        double OriginLat,
        double OriginLon,
        string DestIcao,
        double DestLat,
        double DestLon);

    /// <summary>
    /// Resolve a rota exibida no mini-mapa do HUD (texto, origem e destino),
    /// priorizando missões ativas durante o monitoramento e missões aceitas antes de iniciar.
    /// </summary>
    public static class HudRouteResolver
    {
        public static HudRouteDisplay Resolve(
            bool isMonitoring,
            IReadOnlyList<Mission>? activeMissions,
            IReadOnlyList<Mission>? acceptedMissions,
            double aircraftLat,
            double aircraftLon,
            string currentOriginIcao,
            double currentOriginLat,
            double currentOriginLon,
            Func<string, AirportPosition?> airportLookup)
        {
            if (airportLookup == null) throw new ArgumentNullException(nameof(airportLookup));

            Mission? primaryMission = isMonitoring
                ? activeMissions?.FirstOrDefault()
                : acceptedMissions?.FirstOrDefault();

            if (primaryMission != null)
            {
                string originIcao = primaryMission.OriginICAO ?? string.Empty;
                string destIcao = primaryMission.DestinationICAO ?? string.Empty;

                var origApt = !string.IsNullOrWhiteSpace(originIcao) ? airportLookup(originIcao) : null;
                var destApt = !string.IsNullOrWhiteSpace(destIcao) ? airportLookup(destIcao) : null;

                double origLat = origApt != null
                    ? origApt.Latitude
                    : ((aircraftLat != 0 || aircraftLon != 0) ? aircraftLat : 0);
                double origLon = origApt != null
                    ? origApt.Longitude
                    : ((aircraftLat != 0 || aircraftLon != 0) ? aircraftLon : 0);

                double destLat = destApt?.Latitude ?? 0;
                double destLon = destApt?.Longitude ?? 0;

                string routeText = $"{originIcao} ➔ {destIcao} ({primaryMission.DistanceNM:N0} NM)";
                return new HudRouteDisplay(routeText, originIcao, origLat, origLon, destIcao, destLat, destLon);
            }

            if (isMonitoring)
            {
                bool hasValidAnchor = currentOriginIcao == "PARTIDA" && (currentOriginLat != 0 || currentOriginLon != 0);
                if (hasValidAnchor)
                {
                    return new HudRouteDisplay("VOO LOCAL / CIRCUITO", "PARTIDA", currentOriginLat, currentOriginLon, string.Empty, 0, 0);
                }

                if (aircraftLat != 0 || aircraftLon != 0)
                {
                    return new HudRouteDisplay("VOO LOCAL / CIRCUITO", "PARTIDA", aircraftLat, aircraftLon, string.Empty, 0, 0);
                }

                return new HudRouteDisplay("VOO LOCAL / CIRCUITO", string.Empty, 0, 0, string.Empty, 0, 0);
            }

            return new HudRouteDisplay("EM SOLO", string.Empty, 0, 0, string.Empty, 0, 0);
        }
    }
}
