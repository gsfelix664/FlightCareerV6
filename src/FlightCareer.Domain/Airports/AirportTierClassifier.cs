using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Airports
{
    public sealed record AirportTierInput(
        IReadOnlySet<string> Tags,
        int IlsEnds,
        int Approaches,
        int Gates,
        int Jetways,
        int CargoParking,
        bool HasTower,
        int LongestRunwayFt,
        bool LongestRunwayPaved,
        string? CsvType,
        bool? ScheduledService);

    public sealed record AirportTierScore(
        int TotalScore,
        int Tier,
        int TagScore,
        int StructureScore,
        int OurAirportsScore,
        int IlsScore,
        int ApproachScore,
        int GateJetwayScore,
        int CargoParkingScore,
        int TowerScore,
        int RunwayScore,
        int CsvTypeScore,
        int ScheduledServiceScore,
        string? DominantTag
    );

    public static class AirportTierClassifier
    {
        private static readonly HashSet<string> HeuristicTagsToIgnore = new(StringComparer.OrdinalIgnoreCase)
        {
            "isolado", "interior_distante", "fazenda", "altitude", "turistico_poi"
        };

        public static int Classify(AirportTierInput input, AirportTierRules rules)
        {
            return Score(input, rules).Tier;
        }

        public static AirportTierScore Score(AirportTierInput input, AirportTierRules rules)
        {
            // 1. Etiquetas (vale a maior entre as etiquetas válidas curadas)
            int bestTagScore = 0;
            string? dominantTag = null;

            if (input.Tags != null)
            {
                foreach (var rawTag in input.Tags)
                {
                    if (string.IsNullOrWhiteSpace(rawTag)) continue;
                    var tag = rawTag.Trim();

                    // Ignorar explicitamente heurísticas que dependem do tier ou não são curadas
                    if (IsHeuristicTag(tag)) continue;

                    int tagScore = EvaluateTag(tag, rules.Tags);
                    if (tagScore > bestTagScore)
                    {
                        bestTagScore = tagScore;
                        dominantTag = tag;
                    }
                }
            }

            // 2. Estrutura no simulador
            var structRules = rules.Structure;
            int ilsScore = Math.Min(Math.Max(0, input.IlsEnds) * structRules.IlsPerEnd, structRules.MaxIls);
            int approachScore = Math.Min(Math.Max(0, input.Approaches) * structRules.ApproachEach, structRules.MaxApproaches);
            int gateJetwayScore = Math.Min((Math.Max(0, input.Gates) + Math.Max(0, input.Jetways)) * structRules.GateJetwayEach, structRules.MaxGateJetways);
            int cargoScore = Math.Min(Math.Max(0, input.CargoParking) * structRules.CargoParkingEach, structRules.MaxCargoParking);
            int towerScore = input.HasTower ? structRules.Tower : 0;

            int runwayScore = 0;
            if (input.LongestRunwayPaved)
            {
                if (input.LongestRunwayFt >= structRules.RunwayPavedTier1MinFt)
                {
                    runwayScore = structRules.RunwayPavedTier1Points;
                }
                else if (input.LongestRunwayFt >= structRules.RunwayPavedTier2MinFt)
                {
                    runwayScore = structRules.RunwayPavedTier2Points;
                }
                else
                {
                    runwayScore = structRules.RunwayPavedAnyPoints;
                }
            }

            int structureTotal = ilsScore + approachScore + gateJetwayScore + cargoScore + towerScore + runwayScore;

            // 3. OurAirports
            var oaRules = rules.OurAirports;
            int csvTypeScore = 0;
            if (string.Equals(input.CsvType, "large_airport", StringComparison.OrdinalIgnoreCase))
            {
                csvTypeScore = oaRules.LargeAirport;
            }
            else if (string.Equals(input.CsvType, "medium_airport", StringComparison.OrdinalIgnoreCase))
            {
                csvTypeScore = oaRules.MediumAirport;
            }

            int schedScore = input.ScheduledService == true ? oaRules.ScheduledService : 0;
            int ourAirportsTotal = csvTypeScore + schedScore;

            // Pontuação total
            int total = bestTagScore + structureTotal + ourAirportsTotal;

            // 4. Corte do Tier
            int tier;
            if (input.LongestRunwayPaved)
            {
                if (total >= rules.Thresholds.Tier1)
                {
                    tier = 1;
                }
                else if (total >= rules.Thresholds.Tier2)
                {
                    tier = 2;
                }
                else
                {
                    // >= Tier3 (15) ou qualquer pavimentada garante no mínimo Tier 3
                    tier = 3;
                }
            }
            else
            {
                // Sem pavimento: no máximo tier 3 (e só com >= Tier2 corte, ex. 45)
                if (total >= rules.Thresholds.Tier2)
                {
                    tier = 3;
                }
                else
                {
                    tier = 4;
                }
            }

            return new AirportTierScore(
                TotalScore: total,
                Tier: tier,
                TagScore: bestTagScore,
                StructureScore: structureTotal,
                OurAirportsScore: ourAirportsTotal,
                IlsScore: ilsScore,
                ApproachScore: approachScore,
                GateJetwayScore: gateJetwayScore,
                CargoParkingScore: cargoScore,
                TowerScore: towerScore,
                RunwayScore: runwayScore,
                CsvTypeScore: csvTypeScore,
                ScheduledServiceScore: schedScore,
                DominantTag: dominantTag
            );
        }

        private static bool IsHeuristicTag(string tag)
        {
            foreach (var h in HeuristicTagsToIgnore)
            {
                if (tag.Equals(h, StringComparison.OrdinalIgnoreCase) ||
                    tag.StartsWith(h + ":", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static int EvaluateTag(string tag, TagTierScores tagRules)
        {
            if (tag.Equals("linha:mega_hub", StringComparison.OrdinalIgnoreCase))
            {
                return tagRules.MegaHub;
            }

            if (tag.Equals("linha:hub", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("linha:ponte_aerea", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("linha:internacional", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("linha:domestico", StringComparison.OrdinalIgnoreCase))
            {
                return tagRules.HubLinha;
            }

            if (tag.Equals("carga:cargueiros", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("carga:internacional", StringComparison.OrdinalIgnoreCase))
            {
                return tagRules.CargaPesada;
            }

            if (tag.Equals("linha", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("linha:", StringComparison.OrdinalIgnoreCase))
            {
                return tagRules.Linha;
            }

            if (tag.Equals("alimentador", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("alimentador:", StringComparison.OrdinalIgnoreCase))
            {
                return tagRules.Alimentador;
            }

            if (tag.Equals("executivo", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("executivo:", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("saude", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("saude:", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("mro", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("mro:", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("carga", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("carga:", StringComparison.OrdinalIgnoreCase))
            {
                return tagRules.ExecutivoSaudeMroCarga;
            }

            if (tag.Equals("instrucao", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("instrucao:", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("aeroclube", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("aeroclube:", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("agro", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("agro:", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("zona_salto", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("zona_salto:", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("turistico_curado", StringComparison.OrdinalIgnoreCase) ||
                tag.StartsWith("turistico_curado:", StringComparison.OrdinalIgnoreCase))
            {
                return tagRules.InstrucaoAgroOutros;
            }

            return 0;
        }
    }
}
