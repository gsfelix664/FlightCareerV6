using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Airports.Sim
{
    public sealed record KnownAirport(string Ident, string? Fingerprint, bool IsInSimulator);

    public sealed record SimAirportDiffResult(
        IReadOnlyList<SimAirportListItem> New,
        IReadOnlyList<SimAirportListItem> Changed,
        IReadOnlyList<string> Missing,
        int Unchanged,
        int Duplicates);

    public static class SimAirportDiff
    {
        public static SimAirportDiffResult Compute(IReadOnlyList<SimAirportListItem> sim, IReadOnlyList<KnownAirport> known)
        {
            var knownDict = new Dictionary<string, KnownAirport>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in known)
            {
                if (!string.IsNullOrWhiteSpace(k.Ident) && !knownDict.ContainsKey(k.Ident))
                {
                    knownDict[k.Ident] = k;
                }
            }

            var seenSimIdents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newItems = new List<SimAirportListItem>();
            var changedItems = new List<SimAirportListItem>();
            int unchangedCount = 0;
            int duplicatesCount = 0;

            foreach (var item in sim)
            {
                if (string.IsNullOrWhiteSpace(item.Ident) || item.Ident.Trim().Length < 3)
                {
                    continue;
                }

                var ident = item.Ident.Trim();
                if (!seenSimIdents.Add(ident))
                {
                    duplicatesCount++;
                    continue;
                }

                var fp = SimAirportFingerprint.Of(item);
                if (!knownDict.TryGetValue(ident, out var k))
                {
                    newItems.Add(item);
                }
                else
                {
                    if (string.IsNullOrEmpty(k.Fingerprint) || !string.Equals(k.Fingerprint, fp, StringComparison.Ordinal))
                    {
                        changedItems.Add(item);
                    }
                    else
                    {
                        unchangedCount++;
                    }
                }
            }

            var missingIdents = new List<string>();
            foreach (var k in knownDict.Values)
            {
                if (k.IsInSimulator && !seenSimIdents.Contains(k.Ident))
                {
                    missingIdents.Add(k.Ident);
                }
            }

            return new SimAirportDiffResult(newItems, changedItems, missingIdents, unchangedCount, duplicatesCount);
        }
    }
}
