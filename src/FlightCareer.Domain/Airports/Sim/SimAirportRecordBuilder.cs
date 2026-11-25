using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Airports;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Airports.Sim
{
    public sealed record SimAirportBuildResult(Airport Airport, IReadOnlyList<Runway> Runways);

    public static class SimAirportRecordBuilder
    {
        public static SimAirportBuildResult? Build(
            SimAirportDetail d,
            string fingerprint,
            Airport? existing,
            IReadOnlyList<Runway> existingRunways,
            AirportTierRules rules,
            Func<string, IReadOnlySet<string>> tagsFor,
            DateTime nowUtc)
        {
            // 1. Filtrar pistas recebidas: descartar água e pista < 800 ft
            var rawValidRunways = new List<(SimRunwayData Raw, int LengthFt, int WidthFt)>();
            foreach (var r in d.Runways)
            {
                if (SimSurface.IsWater(r.SurfaceCode))
                {
                    continue;
                }

                double lenFtDouble = r.LengthM * 3.28084;
                if (lenFtDouble < 800.0)
                {
                    continue;
                }

                int lenFt = (int)Math.Round(lenFtDouble);
                int widthFt = (int)Math.Round(r.WidthM * 3.28084);
                rawValidRunways.Add((r, lenFt, widthFt));
            }

            if (rawValidRunways.Count == 0)
            {
                return null;
            }

            int longestRunwayFt = rawValidRunways.Max(x => x.LengthFt);
            if (longestRunwayFt < 1000)
            {
                return null;
            }

            var longestItem = rawValidRunways.OrderByDescending(x => x.LengthFt).First();
            bool longestRunwayPaved = SimSurface.IsPaved(longestItem.Raw.SurfaceCode);

            // Casamento de iluminação com pistas existentes
            var unusedExisting = (existingRunways ?? Array.Empty<Runway>()).ToList();
            var airportIdent = existing?.ICAO ?? d.Ident;
            var runways = new List<Runway>(rawValidRunways.Count);

            foreach (var (raw, lenFt, widthFt) in rawValidRunways)
            {
                Runway? bestMatch = null;
                double bestDiff = double.MaxValue;

                foreach (var ex in unusedExisting)
                {
                    double diff = Math.Abs(ex.LengthFt - lenFt);
                    if (diff <= 0.10 * lenFt && diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestMatch = ex;
                    }
                }

                bool isLighted = false;
                if (bestMatch != null)
                {
                    isLighted = bestMatch.IsLighted;
                    unusedExisting.Remove(bestMatch);
                }

                runways.Add(new Runway
                {
                    Id = Guid.NewGuid(),
                    AirportIdent = airportIdent,
                    LengthFt = lenFt,
                    WidthFt = widthFt,
                    Surface = SimSurface.ToSurfaceName(raw.SurfaceCode),
                    IsLighted = isLighted
                });
            }

            // 2. Instância do Aeroporto (mesma se existing != null)
            var airport = existing ?? new Airport
            {
                Id = Guid.NewGuid(),
                ICAO = d.Ident,
                City = string.Empty,
                Country = string.Empty
            };

            string finalName = !string.IsNullOrWhiteSpace(d.Name)
                ? d.Name.Trim()
                : (!string.IsNullOrWhiteSpace(existing?.Name) ? existing!.Name : "Aeródromo MSFS");
            airport.Name = finalName;

            airport.Latitude = d.Latitude;
            airport.Longitude = d.Longitude;
            airport.ElevationFt = (int)Math.Round(d.AltitudeM * 3.28084);
            airport.MsfsIdent = d.Ident;
            airport.Approaches = d.Approaches;

            airport.IlsEnds = d.Runways.Sum(r =>
                (!string.IsNullOrWhiteSpace(r.PrimaryIlsIcao) ? 1 : 0) +
                (!string.IsNullOrWhiteSpace(r.SecondaryIlsIcao) ? 1 : 0));

            bool hasTowerCoord = Math.Abs(d.TowerLatitude) > 1e-6 || Math.Abs(d.TowerLongitude) > 1e-6;
            airport.HasTower = hasTowerCoord || (existing?.HasTower ?? false);

            airport.Gates = Math.Max(existing?.Gates ?? 0, d.Jetways);
            airport.LongestRunwayFt = longestRunwayFt;
            airport.LongestRunwayPaved = longestRunwayPaved;

            airport.IsInSimulator = true;
            airport.Source = "Sim";
            airport.SimRegion = d.Region;
            airport.SimFingerprint = fingerprint;
            airport.SimCheckedUtc = nowUtc;

            // 3. Cálculo de Tier
            var input = new AirportTierInput(
                Tags: tagsFor(airport.ICAO),
                IlsEnds: airport.IlsEnds,
                Approaches: airport.Approaches,
                Gates: airport.Gates,
                Jetways: 0,
                CargoParking: airport.CargoParking,
                HasTower: airport.HasTower,
                LongestRunwayFt: airport.LongestRunwayFt,
                LongestRunwayPaved: airport.LongestRunwayPaved,
                CsvType: airport.CsvType,
                ScheduledService: null,
                IsMilitary: airport.IsMilitary
            );

            var tierScore = AirportTierClassifier.Score(input, rules);
            airport.Tier = tierScore.Tier;
            airport.TierScore = tierScore.TotalScore;
            airport.TierRulesVersion = rules.Version;

            return new SimAirportBuildResult(airport, runways);
        }
    }
}
