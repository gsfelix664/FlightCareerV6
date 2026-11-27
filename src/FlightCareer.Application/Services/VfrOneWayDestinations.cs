using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Application.Services
{
    /// <summary>
    /// Lógica pura para seleção e ponderação de destinos de navegação VFR só de ida.
    /// </summary>
    public static class VfrOneWayDestinations
    {
        /// <summary>Comprimento mínimo de pista pavimentada para o degrau inicial/fácil de iniciante.</summary>
        public const int EasyRunwayMinFt = 2500;

        /// <summary>Comprimento mínimo absoluto de pista para qualquer destino elegível.</summary>
        public const int MinRunwayFt = 1800;

        /// <summary>Limite de pista considerada curta na ponderação intermediária.</summary>
        public const int ShortRunwayMaxFt = 3000;

        /// <summary>Diferença de elevação em relação à origem para considerar relevo diferenciado.</summary>
        public const double TerrainDiffFt = 1000.0;

        /// <summary>Peso adicional por característica desafiadora na etapa intermediária.</summary>
        public const double FeatureWeight = 2.0;

        /// <summary>
        /// Critério de elegibilidade básico para candidatos a destino VFR só de ida.
        /// </summary>
        public static bool Eligible(Airport origin, AirportCandidate c, int minNm, int maxNm)
        {
            if (origin == null || c == null || c.Airport == null) return false;
            if (string.Equals(c.Airport.ICAO, origin.ICAO, StringComparison.OrdinalIgnoreCase)) return false;

            double dist = MissionGenerator.CalculateDistance(
                origin.Latitude, origin.Longitude, c.Airport.Latitude, c.Airport.Longitude);

            if (dist < minNm || dist > maxNm) return false;
            if (!string.Equals(c.Airport.Country, origin.Country, StringComparison.OrdinalIgnoreCase)) return false;
            if (c.Airport.Tier != 2 && c.Airport.Tier != 3) return false;
            if (c.BestRunway == null || c.BestRunway.LengthFt < MinRunwayFt) return false;

            return true;
        }

        /// <summary>
        /// Gera uma lista determinística de destinos preferenciais para instrução inicial (iniciante).
        /// </summary>
        public static IReadOnlyList<AirportCandidate> BeginnerPool(
            Airport origin,
            IReadOnlyList<AirportCandidate> candidates,
            int minNm,
            int maxNm,
            int poolSize)
        {
            if (origin == null || candidates == null || candidates.Count == 0)
                return Array.Empty<AirportCandidate>();

            int effectivePoolSize = poolSize < 1 ? 3 : poolSize;

            // 1. Base = elegíveis com Tier == 3
            var baseCandidates = candidates
                .Where(c => c.Airport.Tier == 3 && Eligible(origin, c, minNm, maxNm))
                .ToList();

            if (baseCandidates.Count == 0)
                return Array.Empty<AirportCandidate>();

            // 2. Escada (primeiro degrau não vazio)
            // (a) !HasTower e pista pavimentada com LengthFt >= EasyRunwayMinFt
            var degrauA = baseCandidates
                .Where(c => !c.Airport.HasTower
                            && c.BestRunway != null
                            && !RunwaySurface.IsUnpaved(c.BestRunway.Surface)
                            && c.BestRunway.LengthFt >= EasyRunwayMinFt)
                .ToList();

            List<AirportCandidate> selectedPoolCandidates;
            if (degrauA.Count > 0)
            {
                selectedPoolCandidates = degrauA;
            }
            else
            {
                // (b) pavimentada com LengthFt >= EasyRunwayMinFt (com ou sem torre)
                var degrauB = baseCandidates
                    .Where(c => c.BestRunway != null
                                && !RunwaySurface.IsUnpaved(c.BestRunway.Surface)
                                && c.BestRunway.LengthFt >= EasyRunwayMinFt)
                    .ToList();

                if (degrauB.Count > 0)
                {
                    selectedPoolCandidates = degrauB;
                }
                else
                {
                    // (c) toda a base
                    selectedPoolCandidates = baseCandidates;
                }
            }

            // 3. Ordena por distância crescente e depois ICAO (ordinal) e pega os poolSize primeiros
            return selectedPoolCandidates
                .Select(c => new
                {
                    Candidate = c,
                    Distance = MissionGenerator.CalculateDistance(origin.Latitude, origin.Longitude, c.Airport.Latitude, c.Airport.Longitude)
                })
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.Candidate.Airport.ICAO, StringComparer.Ordinal)
                .Take(effectivePoolSize)
                .Select(x => x.Candidate)
                .ToList();
        }

        /// <summary>
        /// Calcula o peso de atratividade para a etapa intermediária com base em características desafiadoras.
        /// </summary>
        public static double IntermediateWeight(Airport origin, AirportCandidate c)
        {
            if (origin == null || c == null || c.Airport == null) return 1.0;

            int n = 0;
            if (c.Airport.HasTower) n++;
            if (c.BestRunway != null && (c.BestRunway.LengthFt <= ShortRunwayMaxFt || RunwaySurface.IsUnpaved(c.BestRunway.Surface))) n++;
            if (Math.Abs(c.Airport.ElevationFt - origin.ElevationFt) >= TerrainDiffFt) n++;

            return 1.0 + (FeatureWeight * n);
        }

        /// <summary>
        /// Sorteia um destino intermediário ponderado pelo grau de desafio (torre, pista curta/não pavimentada, desnível).
        /// </summary>
        public static AirportCandidate? PickIntermediate(
            Airport origin,
            IReadOnlyList<AirportCandidate> candidates,
            int minNm,
            int maxNm,
            Random random)
        {
            if (origin == null || candidates == null || candidates.Count == 0) return null;

            var eligible = candidates.Where(c => Eligible(origin, c, minNm, maxNm)).ToList();
            if (eligible.Count == 0) return null;

            var weighted = new List<(AirportCandidate Candidate, double Weight)>(eligible.Count);
            double totalWeight = 0.0;

            foreach (var c in eligible)
            {
                double w = IntermediateWeight(origin, c);
                weighted.Add((c, w));
                totalWeight += w;
            }

            if (totalWeight <= 0.0) return eligible[0];

            double roll = random.NextDouble() * totalWeight;
            double acc = 0.0;
            foreach (var (cand, w) in weighted)
            {
                acc += w;
                if (roll <= acc) return cand;
            }

            return weighted[^1].Candidate;
        }
    }
}
