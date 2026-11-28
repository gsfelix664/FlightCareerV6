using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public sealed record DefectRepairQuote(
        IReadOnlyList<AircraftPart> PartsToReplace,      // união (sem repetir Id) das peças ligadas
        IReadOnlyList<AircraftDefect> DefectsCharged,    // panes sem peça ligada: cobram RepairCost
        IReadOnlyList<AircraftDefect> DefectsIncluded,   // panes com peça ligada: custo 0, resolvidas pela troca
        double DefectsCost);                             // soma de RepairCost de DefectsCharged

    /// <summary>
    /// Vincula panes persistentes a peças físicas quebradas correspondentes e orça reparos unificados.
    /// </summary>
    public static class DefectPartLink
    {
        /// <summary>
        /// Peças instaladas e quebradas ligadas à pane. Pane nativa: candidatas do ID (ComponentIdFromCause) que estão IsBroken.
        /// Pane do addon ou sem ID: lista vazia (continua com o RepairCost próprio).
        /// </summary>
        public static IReadOnlyList<AircraftPart> LinkedBrokenParts(AircraftDefect defect, IEnumerable<AircraftPart> installedParts)
        {
            if (defect == null || defect.IsResolved || installedParts == null)
            {
                return Array.Empty<AircraftPart>();
            }

            int? nativeId = NativeWearFailureMap.ComponentIdFromCause(defect.Cause);
            if (!nativeId.HasValue)
            {
                return Array.Empty<AircraftPart>();
            }

            var candidates = NativeFailurePartMap.CandidatesFor(nativeId.Value);
            if (candidates.Count == 0)
            {
                return Array.Empty<AircraftPart>();
            }

            var candidateSet = candidates.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return installedParts
                .Where(p => p.IsBroken && candidateSet.Contains(p.PartId))
                .ToList();
        }

        /// <summary>
        /// Gera orçamento unificado de reparo para as panes abertas fornecidas.
        /// Panes com peças quebradas vinculadas são incluídas na troca física (custo da pane zero).
        /// Panes sem peças quebradas vinculadas cobram seu RepairCost normal.
        /// </summary>
        public static DefectRepairQuote Quote(IEnumerable<AircraftDefect> defects, IEnumerable<AircraftPart> installedParts)
        {
            if (defects == null)
            {
                return new DefectRepairQuote(Array.Empty<AircraftPart>(), Array.Empty<AircraftDefect>(), Array.Empty<AircraftDefect>(), 0.0);
            }

            var openDefects = defects.Where(d => !d.IsResolved).ToList();
            var partsList = installedParts != null ? installedParts.ToList() : new List<AircraftPart>();

            var partsToReplace = new List<AircraftPart>();
            var seenPartIds = new HashSet<Guid>();
            var defectsCharged = new List<AircraftDefect>();
            var defectsIncluded = new List<AircraftDefect>();
            double defectsCost = 0.0;

            foreach (var defect in openDefects)
            {
                var linked = LinkedBrokenParts(defect, partsList);
                if (linked.Count > 0)
                {
                    defectsIncluded.Add(defect);
                    foreach (var part in linked)
                    {
                        if (seenPartIds.Add(part.Id))
                        {
                            partsToReplace.Add(part);
                        }
                    }
                }
                else
                {
                    defectsCharged.Add(defect);
                    defectsCost += defect.RepairCost;
                }
            }

            return new DefectRepairQuote(partsToReplace, defectsCharged, defectsIncluded, Math.Round(defectsCost, 2));
        }

        /// <summary>
        /// Identifica quais panes abertas são resolvidas após a troca de um conjunto de peças.
        /// Uma pane é resolvida se possuir peças vinculadas quebradas e todas elas estiverem em <paramref name="replacedPartIds"/>.
        /// </summary>
        public static IReadOnlyList<AircraftDefect> ResolvedBy(IEnumerable<AircraftDefect> openDefects, IEnumerable<AircraftPart> installedParts, IReadOnlySet<Guid> replacedPartIds)
        {
            if (openDefects == null || installedParts == null || replacedPartIds == null || replacedPartIds.Count == 0)
            {
                return Array.Empty<AircraftDefect>();
            }

            var partsList = installedParts.ToList();
            var resolved = new List<AircraftDefect>();

            foreach (var defect in openDefects.Where(d => !d.IsResolved))
            {
                var linked = LinkedBrokenParts(defect, partsList);
                if (linked.Count > 0 && linked.All(p => replacedPartIds.Contains(p.Id)))
                {
                    resolved.Add(defect);
                }
            }

            return resolved;
        }
    }
}
