using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions;

public enum BriefingLegKind
{
    Navigation,
    ContractStop,
    TouchAndGo,
    Local
}

public sealed record BriefingLegPlan(
    string From,
    string To,
    BriefingLegKind Kind,
    Mission? Mission,
    string StopMode,            // "full_stop" | "touch_and_go" (só relevante em ContractStop; senão "full_stop")
    bool IsFinalLegOfMission,   // true na última perna de cada missão (TouchAndGo/Local: true)
    double? ToLat,
    double? ToLon               // coordenada da escala vinda do StopPoint (null se não houver)
);

public static class BriefingLegPlanner
{
    public static IReadOnlyList<StopPoint> ReadStops(Mission m)
    {
        if (m == null || string.IsNullOrWhiteSpace(m.ContractData))
            return Array.Empty<StopPoint>();

        try
        {
            var data = JsonSerializer.Deserialize<MultiStopContractData>(m.ContractData);
            if (data?.Stops != null && data.Stops.Count > 0)
            {
                return data.Stops;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BriefingLegPlanner] Erro ao ler Stops ({m.Id}): {ex.Message}");
        }

        return Array.Empty<StopPoint>();
    }

    private static string ReadStopMode(Mission m)
    {
        if (m == null || string.IsNullOrWhiteSpace(m.ContractData))
            return "full_stop";

        try
        {
            var data = JsonSerializer.Deserialize<MultiStopContractData>(m.ContractData);
            if (!string.IsNullOrWhiteSpace(data?.StopMode))
            {
                return data.StopMode;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BriefingLegPlanner] Erro ao ler StopMode ({m.Id}): {ex.Message}");
        }

        return "full_stop";
    }

    public static IReadOnlyList<BriefingLegPlan> Plan(IReadOnlyList<Mission> accepted, string originIcao)
    {
        if (accepted == null || accepted.Count == 0)
            return Array.Empty<BriefingLegPlan>();

        originIcao ??= string.Empty;

        // 1. Grupo local na origem: missão com OriginICAO == originIcao e sem escalas
        //    e (DestinationICAO == originIcao ou categoria contém "Local", ignorando maiúsculas).
        //    Vira uma perna (originIcao, originIcao): TouchAndGo se RequiredTouchAndGoes > 0, senão Local.
        var localMissionsAtOrigin = accepted
            .Where(m => string.Equals(m.OriginICAO, originIcao, StringComparison.OrdinalIgnoreCase)
                     && ReadStops(m).Count == 0
                     && (string.Equals(m.DestinationICAO, originIcao, StringComparison.OrdinalIgnoreCase)
                         || (!string.IsNullOrEmpty(m.MissionCategoryType) && m.MissionCategoryType.Contains("Local", StringComparison.OrdinalIgnoreCase))))
            .ToList();

        var legs = new List<BriefingLegPlan>();

        foreach (var lm in localMissionsAtOrigin)
        {
            var kind = lm.RequiredTouchAndGoes > 0 ? BriefingLegKind.TouchAndGo : BriefingLegKind.Local;
            legs.Add(new BriefingLegPlan(
                From: originIcao,
                To: originIcao,
                Kind: kind,
                Mission: lm,
                StopMode: "full_stop",
                IsFinalLegOfMission: true,
                ToLat: null,
                ToLon: null
            ));
        }

        // 2. Demais missões: encadeamento
        var remainingCrossMissions = accepted
            .Where(m => !localMissionsAtOrigin.Contains(m))
            .ToList();

        string currentHop = originIcao;
        int safetyGuard = 0;

        while (remainingCrossMissions.Any() && safetyGuard++ < 50)
        {
            // 1. Encontrar missão que decola do aeroporto atual
            var nextMission = remainingCrossMissions.FirstOrDefault(m =>
                string.Equals(m.OriginICAO, currentHop, StringComparison.OrdinalIgnoreCase) &&
                (!string.Equals(m.DestinationICAO, currentHop, StringComparison.OrdinalIgnoreCase) || ReadStops(m).Count > 0));

            if (nextMission == null && !string.Equals(currentHop, originIcao, StringComparison.OrdinalIgnoreCase))
            {
                // 2. Se currentHop != originIcao, verificar se há outra missão para o mesmo destino
                var sameDestMission = remainingCrossMissions.FirstOrDefault(m =>
                    string.Equals(m.DestinationICAO, currentHop, StringComparison.OrdinalIgnoreCase));
                if (sameDestMission != null)
                {
                    legs.Add(new BriefingLegPlan(
                        From: originIcao,
                        To: currentHop,
                        Kind: BriefingLegKind.Navigation,
                        Mission: sameDestMission,
                        StopMode: "full_stop",
                        IsFinalLegOfMission: true,
                        ToLat: null,
                        ToLon: null
                    ));
                    remainingCrossMissions.Remove(sameDestMission);
                    continue;
                }

                // 3. Se não, procurar missão carregada na base
                nextMission = remainingCrossMissions.FirstOrDefault(m =>
                    string.Equals(m.OriginICAO, originIcao, StringComparison.OrdinalIgnoreCase));
            }

            // 4. Fallback se ainda for nulo
            if (nextMission == null)
            {
                nextMission = remainingCrossMissions.First();
            }

            var stops = ReadStops(nextMission);
            if (stops.Count == 0)
            {
                legs.Add(new BriefingLegPlan(
                    From: currentHop,
                    To: nextMission.DestinationICAO,
                    Kind: BriefingLegKind.Navigation,
                    Mission: nextMission,
                    StopMode: "full_stop",
                    IsFinalLegOfMission: true,
                    ToLat: null,
                    ToLon: null
                ));
                remainingCrossMissions.Remove(nextMission);
                currentHop = nextMission.DestinationICAO;
            }
            else
            {
                string stopMode = ReadStopMode(nextMission);

                // hops = ICAOs das Stops em ordem e por fim DestinationICAO;
                // descarte um hop igual ao anterior (inclusive a primeira escala igual ao hop atual e a última escala igual ao destino).
                var candidates = new List<(string Icao, double? Lat, double? Lon)>();
                foreach (var s in stops)
                {
                    candidates.Add((s.Icao, s.Lat, s.Lon));
                }
                candidates.Add((nextMission.DestinationICAO, null, null));

                string prevIcao = currentHop;
                var filtered = new List<(string Icao, double? Lat, double? Lon)>();
                foreach (var c in candidates)
                {
                    if (string.Equals(c.Icao, prevIcao, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    filtered.Add(c);
                    prevIcao = c.Icao;
                }

                if (filtered.Count == 0)
                {
                    filtered.Add((nextMission.DestinationICAO, null, null));
                }

                string legFrom = currentHop;
                for (int i = 0; i < filtered.Count; i++)
                {
                    var target = filtered[i];
                    bool isLast = (i == filtered.Count - 1);
                    if (isLast)
                    {
                        legs.Add(new BriefingLegPlan(
                            From: legFrom,
                            To: target.Icao,
                            Kind: BriefingLegKind.Navigation,
                            Mission: nextMission,
                            StopMode: "full_stop",
                            IsFinalLegOfMission: true,
                            ToLat: null,
                            ToLon: null
                        ));
                    }
                    else
                    {
                        legs.Add(new BriefingLegPlan(
                            From: legFrom,
                            To: target.Icao,
                            Kind: BriefingLegKind.ContractStop,
                            Mission: nextMission,
                            StopMode: stopMode,
                            IsFinalLegOfMission: false,
                            ToLat: target.Lat,
                            ToLon: target.Lon
                        ));
                    }
                    legFrom = target.Icao;
                }

                remainingCrossMissions.Remove(nextMission);
                currentHop = nextMission.DestinationICAO;
            }
        }

        return legs;
    }
}
