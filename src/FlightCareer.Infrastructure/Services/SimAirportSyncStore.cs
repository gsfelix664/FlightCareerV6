using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Domain.Airports;
using FlightCareer.Domain.Airports.Sim;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Infrastructure.Services
{
    public sealed record SimListApplyResult(
        bool NoChange,
        int Enqueued,
        int MarkedMissing,
        bool MissingSkippedSuspicious,
        bool VersionChanged);

    public sealed record SimDetailApplyResult(
        int Inserted,
        int Updated,
        int Excluded,
        int Failed,
        int Dropped);

    public class SimAirportSyncStore
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public SimAirportSyncStore(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task<SimSyncState> GetStateAsync()
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            var state = await db.SimSyncStates.FirstOrDefaultAsync(s => s.Id == 1);
            if (state == null)
            {
                state = new SimSyncState
                {
                    Id = 1,
                    SimVersion = string.Empty,
                    ListDigest = string.Empty,
                    LastListUtc = null,
                    ListCount = 0
                };
                db.SimSyncStates.Add(state);
                await db.SaveChangesAsync();
            }
            return state;
        }

        public async Task<SimListApplyResult> ApplyListAsync(
            IReadOnlyList<SimAirportListItem> list,
            string simVersion,
            DateTime nowUtc)
        {
            var digest = SimAirportFingerprint.Digest(list);

            await using var db = await _contextFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            var state = await db.SimSyncStates.FirstOrDefaultAsync(s => s.Id == 1);
            if (state == null)
            {
                state = new SimSyncState
                {
                    Id = 1,
                    SimVersion = string.Empty,
                    ListDigest = string.Empty,
                    LastListUtc = null,
                    ListCount = 0
                };
                db.SimSyncStates.Add(state);
            }

            if (string.Equals(state.ListDigest, digest, StringComparison.Ordinal) &&
                string.Equals(state.SimVersion, simVersion, StringComparison.Ordinal))
            {
                state.LastListUtc = nowUtc;
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new SimListApplyResult(
                    NoChange: true,
                    Enqueued: 0,
                    MarkedMissing: 0,
                    MissingSkippedSuspicious: false,
                    VersionChanged: false);
            }

            var dbAirports = await db.Airports
                .Select(a => new { a.ICAO, a.MsfsIdent, a.SimFingerprint, a.IsInSimulator })
                .ToListAsync();

            var known = dbAirports
                .Select(a => new KnownAirport(
                    !string.IsNullOrWhiteSpace(a.MsfsIdent) ? a.MsfsIdent : a.ICAO,
                    a.SimFingerprint,
                    a.IsInSimulator))
                .ToList();

            var diff = SimAirportDiff.Compute(list, known);

            var existingQueue = await db.SimAirportQueue.ToDictionaryAsync(q => q.Ident, StringComparer.OrdinalIgnoreCase);
            int enqueuedCount = 0;

            var toEnqueue = diff.New.Concat(diff.Changed).ToList();
            foreach (var item in toEnqueue)
            {
                var fp = SimAirportFingerprint.Of(item);
                if (existingQueue.TryGetValue(item.Ident, out var queueItem))
                {
                    queueItem.Region = item.Region ?? string.Empty;
                    queueItem.Latitude = item.Latitude;
                    queueItem.Longitude = item.Longitude;
                    queueItem.AltitudeM = item.AltitudeM;
                    queueItem.Fingerprint = fp;
                    queueItem.Priority = Math.Max(queueItem.Priority, 1);
                }
                else
                {
                    var newItem = new SimAirportQueueItem
                    {
                        Ident = item.Ident,
                        Region = item.Region ?? string.Empty,
                        Latitude = item.Latitude,
                        Longitude = item.Longitude,
                        AltitudeM = item.AltitudeM,
                        Fingerprint = fp,
                        Priority = 1,
                        Attempts = 0,
                        EnqueuedUtc = nowUtc
                    };
                    db.SimAirportQueue.Add(newItem);
                    existingQueue[item.Ident] = newItem;
                }
                enqueuedCount++;
            }

            int inSimCount = known.Count(k => k.IsInSimulator);
            bool missingSkippedSuspicious = list.Count < 0.5 * inSimCount;
            int markedMissing = 0;

            if (!missingSkippedSuspicious && diff.Missing.Count > 0)
            {
                var missingSet = new HashSet<string>(diff.Missing, StringComparer.OrdinalIgnoreCase);
                var airportsToUnmark = await db.Airports
                    .Where(a => a.IsInSimulator)
                    .ToListAsync();

                foreach (var a in airportsToUnmark)
                {
                    var ident = !string.IsNullOrWhiteSpace(a.MsfsIdent) ? a.MsfsIdent : a.ICAO;
                    if (missingSet.Contains(ident))
                    {
                        a.IsInSimulator = false;
                        // Sem impressão, o aeroporto volta como "mudou" se reaparecer no simulador
                        a.SimFingerprint = null;
                        markedMissing++;
                    }
                }
            }

            bool versionChanged = !string.IsNullOrEmpty(state.SimVersion) &&
                                  !string.Equals(state.SimVersion, simVersion, StringComparison.Ordinal);

            if (versionChanged)
            {
                var newOrChangedIdents = new HashSet<string>(toEnqueue.Select(i => i.Ident), StringComparer.OrdinalIgnoreCase);
                var seenListIdents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in list)
                {
                    if (string.IsNullOrWhiteSpace(item.Ident) || item.Ident.Trim().Length < 3) continue;
                    var ident = item.Ident.Trim();
                    if (!seenListIdents.Add(ident)) continue;

                    if (!newOrChangedIdents.Contains(ident))
                    {
                        var fp = SimAirportFingerprint.Of(item);
                        if (!existingQueue.TryGetValue(ident, out var queueItem))
                        {
                            var qItem = new SimAirportQueueItem
                            {
                                Ident = ident,
                                Region = item.Region ?? string.Empty,
                                Latitude = item.Latitude,
                                Longitude = item.Longitude,
                                AltitudeM = item.AltitudeM,
                                Fingerprint = fp,
                                Priority = 0,
                                Attempts = 0,
                                EnqueuedUtc = nowUtc
                            };
                            db.SimAirportQueue.Add(qItem);
                            existingQueue[ident] = qItem;
                            enqueuedCount++;
                        }
                    }
                }
            }

            state.SimVersion = simVersion;
            state.ListDigest = digest;
            state.LastListUtc = nowUtc;
            state.ListCount = list.Count;

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return new SimListApplyResult(
                NoChange: false,
                Enqueued: enqueuedCount,
                MarkedMissing: markedMissing,
                MissingSkippedSuspicious: missingSkippedSuspicious,
                VersionChanged: versionChanged);
        }

        public async Task<IReadOnlyList<SimAirportQueueItem>> TakeBatchAsync(
            int max,
            double? nearLat = null,
            double? nearLon = null)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            IQueryable<SimAirportQueueItem> query = db.SimAirportQueue.AsNoTracking();

            if (nearLat.HasValue && nearLon.HasValue)
            {
                double lat = nearLat.Value;
                double lon = nearLon.Value;
                return await query
                    .OrderByDescending(q => q.Priority)
                    .ThenBy(q => (q.Latitude - lat) * (q.Latitude - lat) + (q.Longitude - lon) * (q.Longitude - lon))
                    .ThenBy(q => q.EnqueuedUtc)
                    .Take(max)
                    .ToListAsync();
            }

            return await query
                .OrderByDescending(q => q.Priority)
                .ThenBy(q => q.EnqueuedUtc)
                .Take(max)
                .ToListAsync();
        }

        public async Task PrioritizeAsync(IEnumerable<string> idents, int priority)
        {
            if (idents == null) return;
            var identSet = new HashSet<string>(idents.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()), StringComparer.OrdinalIgnoreCase);
            if (identSet.Count == 0) return;

            await using var db = await _contextFactory.CreateDbContextAsync();
            var items = await db.SimAirportQueue
                .Where(q => identSet.Contains(q.Ident))
                .ToListAsync();

            bool changed = false;
            foreach (var item in items)
            {
                if (item.Priority < priority)
                {
                    item.Priority = priority;
                    changed = true;
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync();
            }
        }

        public async Task<SimDetailApplyResult> ApplyDetailsAsync(
            IReadOnlyList<(SimAirportQueueItem Item, SimAirportDetail? Detail)> batch,
            DateTime nowUtc)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            int inserted = 0;
            int updated = 0;
            int excluded = 0;
            int failed = 0;
            int dropped = 0;

            // Uma consulta por lote (não por aeroporto): a importação do mundo tem dezenas de milhares
            var batchIdents = batch
                .SelectMany(b => new[] { b.Item.Ident, b.Detail?.Ident })
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i!.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();
            var queueByIdent = (await db.SimAirportQueue.Where(q => batchIdents.Contains(q.Ident.ToUpper())).ToListAsync())
                .GroupBy(q => q.Ident, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var candidates = await db.Airports
                .Where(a => batchIdents.Contains(a.ICAO.ToUpper()) || (a.MsfsIdent != null && batchIdents.Contains(a.MsfsIdent.ToUpper())))
                .ToListAsync();
            var byMsfs = candidates.Where(a => !string.IsNullOrWhiteSpace(a.MsfsIdent))
                .GroupBy(a => a.MsfsIdent!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var byIcao = candidates
                .GroupBy(a => a.ICAO, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var candidateIcaos = candidates.Select(a => a.ICAO).ToList();
            var runwaysByAirport = (await db.Runways.Where(r => candidateIcaos.Contains(r.AirportIdent)).ToListAsync())
                .GroupBy(r => r.AirportIdent, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var (item, detail) in batch)
            {
                queueByIdent.TryGetValue(item.Ident, out var qItem);

                if (detail != null)
                {
                    // Existente: MsfsIdent igual (case insensitive), senão ICAO igual
                    if (!byMsfs.TryGetValue(detail.Ident, out var existing))
                    {
                        byIcao.TryGetValue(detail.Ident, out existing);
                    }

                    var existingRunways = existing != null && runwaysByAirport.TryGetValue(existing.ICAO, out var rw)
                        ? rw
                        : new List<Runway>();

                    var buildResult = SimAirportRecordBuilder.Build(
                        detail,
                        item.Fingerprint,
                        existing,
                        existingRunways,
                        AirportTierRules.Default,
                        AirportProfileCatalog.Shared.TagsFor,
                        nowUtc);

                    if (buildResult != null)
                    {
                        if (existing == null)
                        {
                            db.Airports.Add(buildResult.Airport);
                            inserted++;
                        }
                        else
                        {
                            updated++;
                        }

                        db.Runways.RemoveRange(existingRunways);
                        db.Runways.AddRange(buildResult.Runways);
                        if (existing == null)
                        {
                            byMsfs[detail.Ident] = buildResult.Airport;
                            byIcao[buildResult.Airport.ICAO] = buildResult.Airport;
                        }
                        runwaysByAirport[buildResult.Airport.ICAO] = buildResult.Runways.ToList();

                        if (qItem != null)
                        {
                            db.SimAirportQueue.Remove(qItem);
                        }
                    }
                    else
                    {
                        if (existing != null)
                        {
                            existing.IsInSimulator = false;
                            existing.SimFingerprint = item.Fingerprint;
                            existing.SimCheckedUtc = nowUtc;
                        }
                        excluded++;

                        if (qItem != null)
                        {
                            db.SimAirportQueue.Remove(qItem);
                        }
                    }
                }
                else
                {
                    failed++;
                    if (qItem != null)
                    {
                        qItem.Attempts++;
                        if (qItem.Attempts >= 3)
                        {
                            db.SimAirportQueue.Remove(qItem);
                            Debug.WriteLine($"[SimAirportSyncStore] Aeroporto {qItem.Ident} descartado da fila após 3 falhas.");
                            dropped++;
                        }
                    }
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return new SimDetailApplyResult(inserted, updated, excluded, failed, dropped);
        }

        public async Task<int> EnqueueRotationAsync(int count, DateTime nowUtc)
        {
            if (count <= 0) return 0;

            await using var db = await _contextFactory.CreateDbContextAsync();
            var queuedIdents = await db.SimAirportQueue.Select(q => q.Ident).ToListAsync();
            var queuedSet = new HashSet<string>(queuedIdents, StringComparer.OrdinalIgnoreCase);

            var candidates = await db.Airports
                .Where(a => a.IsInSimulator)
                .OrderBy(a => a.SimCheckedUtc.HasValue ? 1 : 0)
                .ThenBy(a => a.SimCheckedUtc)
                .ToListAsync();

            int enqueued = 0;
            foreach (var a in candidates)
            {
                var ident = !string.IsNullOrWhiteSpace(a.MsfsIdent) ? a.MsfsIdent : a.ICAO;
                if (string.IsNullOrWhiteSpace(ident) || queuedSet.Contains(ident))
                {
                    continue;
                }

                var region = a.SimRegion ?? string.Empty;
                var altM = a.ElevationFt / 3.28084;
                var fp = !string.IsNullOrEmpty(a.SimFingerprint)
                    ? a.SimFingerprint
                    : SimAirportFingerprint.Of(new SimAirportListItem(ident, region, a.Latitude, a.Longitude, altM));

                var item = new SimAirportQueueItem
                {
                    Ident = ident,
                    Region = region,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    AltitudeM = altM,
                    Fingerprint = fp,
                    Priority = 0,
                    Attempts = 0,
                    EnqueuedUtc = nowUtc
                };

                db.SimAirportQueue.Add(item);
                queuedSet.Add(ident);
                enqueued++;

                if (enqueued >= count)
                {
                    break;
                }
            }

            if (enqueued > 0)
            {
                await db.SaveChangesAsync();
            }

            return enqueued;
        }

        public async Task<int> PendingCountAsync()
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SimAirportQueue.CountAsync();
        }
    }
}
