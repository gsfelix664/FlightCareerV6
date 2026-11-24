using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;

namespace FlightCareer.Application.Services
{
    public sealed record AirportCandidate(Airport Airport, Runway? BestRunway);

    public static class MissionBoardCore
    {
        /// <summary>
        /// Etiquetas da origem com os dados de lugares carregados no jogo: zona de salto e base de
        /// voo panorâmico (só aeródromo turístico com papel "panoramico", não os de papel "destino").
        /// </summary>
        public static IReadOnlySet<string> AugmentOriginTags(Airport origin, IReadOnlySet<string> tags, IPlacesDataService? placesService)
        {
            if (placesService == null || origin == null) return tags;
            HashSet<string>? augmented = null;
            if (placesService.GetDropZone(origin.ICAO) != null && !tags.Contains("zona_salto"))
            {
                augmented ??= new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
                augmented.Add("zona_salto");
            }
            var scenic = placesService.GetScenicAerodrome(origin.ICAO);
            if (scenic != null && scenic.IsScenicBase && !tags.Contains("turistico_curado:base"))
            {
                augmented ??= new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
                augmented.Add("turistico_curado:base");
            }
            return augmented ?? tags;
        }

        public static List<Mission> GenerateBoard(
            Airport origin,
            IReadOnlyList<AirportCandidate> candidates,
            IReadOnlySet<string> originTags,
            Func<string, IReadOnlySet<string>> getAirportTags,
            MissionCatalog catalog,
            Random random,
            DateTime referenceTime,
            int targetCount,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            MissionTextCatalog? textCatalog = null,
            IPlacesDataService? placesService = null,
            Runway? originRunway = null)
        {
            var missions = new List<Mission>();
            if (origin == null || candidates == null || catalog == null) return missions;

            if (originRunway == null)
            {
                System.Diagnostics.Debug.WriteLine($"[MissionBoardCore] Origem {origin.ICAO} sem pista conhecida: não filtra perfis pela pista da origem.");
            }

            // 1. Filtrar missões habilitadas com peso de origem > 0 e que caibam na pista da origem
            var eligibleMissions = new List<(MissionDefinition Mission, double Weight)>();
            foreach (var m in catalog.Missions.Values)
            {
                if (!m.Enabled) continue;

                if (originRunway != null)
                {
                    bool anyProfileFits = m.Profiles.Any(p =>
                        originRunway.LengthFt >= p.MinRunwayFt && (!p.Paved || !RunwaySurface.IsUnpaved(originRunway.Surface)));
                    if (!anyProfileFits) continue;
                }

                double w = MissionWeighting.OriginWeight(m, origin.Tier, originTags);
                if (w > 0.0)
                {
                    eligibleMissions.Add((m, w));
                }
            }

            if (eligibleMissions.Count == 0) return missions;

            double totalOriginWeight = eligibleMissions.Sum(x => x.Weight);
            if (totalOriginWeight <= 0.0) return missions;

            int maxAttempts = targetCount * 4;
            int attempts = 0;

            // Distância origem → candidato calculada uma vez por quadro (antes era recalculada a cada missão).
            var distances = new double[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var a = candidates[i].Airport;
                distances[i] = MissionGenerator.CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude);
            }

            while (missions.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;

                // Sorteio de missão num único nível
                var def = PickWeighted(eligibleMissions, totalOriginWeight, random);
                if (def == null) continue;

                var mission = CreateSingleMission(
                    origin,
                    def,
                    candidates,
                    originTags,
                    getAirportTags,
                    random,
                    referenceTime,
                    payMultiplier,
                    payTable,
                    textCatalog,
                    placesService,
                    distances,
                    originRunway);

                if (mission != null)
                {
                    missions.Add(mission);
                }
            }

            return missions;
        }

        public static Mission? CreateSingleMission(
            Airport origin,
            MissionDefinition def,
            IReadOnlyList<AirportCandidate> candidates,
            IReadOnlySet<string> originTags,
            Func<string, IReadOnlySet<string>> getAirportTags,
            Random random,
            DateTime referenceTime,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            MissionTextCatalog? textCatalog = null,
            IPlacesDataService? placesService = null,
            IReadOnlyList<double>? candidateDistances = null,
            Runway? originRunway = null)
        {
            if (def == null || def.Profiles == null || def.Profiles.Count == 0) return null;

            // 1. Sortear perfil correlacionado com ajuste de bigAirport e compatível com a origem
            var profile = PickProfile(def, origin, originTags, random, originRunway);
            if (profile == null) return null;

            // 2. Sortear dentro do perfil: distância, pax e carga uniformes inteiros
            int minRange = profile.RangeNm.Length > 0 ? profile.RangeNm[0] : 10;
            int maxRange = profile.RangeNm.Length > 1 ? profile.RangeNm[1] : minRange;
            double distanceNm = minRange == maxRange ? minRange : random.Next(minRange, maxRange + 1);

            int minPax = profile.Pax.Length > 0 ? profile.Pax[0] : 0;
            int maxPax = profile.Pax.Length > 1 ? profile.Pax[1] : minPax;
            int paxCount = minPax == maxPax ? minPax : random.Next(minPax, maxPax + 1);

            int minCargo = profile.CargoLbs.Length > 0 ? (int)profile.CargoLbs[0] : 0;
            int maxCargo = profile.CargoLbs.Length > 1 ? (int)profile.CargoLbs[1] : minCargo;
            double cargoLbs = minCargo == maxCargo ? minCargo : random.Next(minCargo, maxCargo + 1);

            // 3. Bagagem por pessoa individual + tripulação de apoio
            double luggageLbs = CalculateLuggageLbs(def, paxCount, random);

            // 4. Seleção de destino
            bool returnsToOrigin = def.Destination != null && string.Equals(def.Destination.Rule, "return_to_origin", StringComparison.OrdinalIgnoreCase);

            // Geradores especiais
            if (def.Generator == "local_instruction")
            {
                var nearbyAirports = candidates.Where(c => c.Airport.Tier <= 2).Select(c => c.Airport).ToList();
                var subAdapter = ToSubCategoryConfig(def);
                return WithPersonLuggage(MissionGenerator.CreateLocalInstructionMissionSync(
                    origin, subAdapter, random, referenceTime, nearbyAirports, payMultiplier, payTable, textCatalog), def, random);
            }

            if (def.Generator == "checkride")
            {
                var nearbyAirports = candidates.Where(c => c.Airport.Tier <= 2).Select(c => c.Airport).ToList();
                var subAdapter = ToSubCategoryConfig(def);
                return WithPersonLuggage(MissionGenerator.CreateCheckrideMissionSync(
                    origin, subAdapter, random, referenceTime, nearbyAirports, payMultiplier, payTable, textCatalog), def, random);
            }

            if (def.Generator == "scenic_tour")
            {
                var subAdapter = ToSubCategoryConfig(def);
                var tour = MissionGenerator.CreateScenicTourMissionSync(
                    origin, subAdapter, random, referenceTime, payMultiplier, payTable, placesService, textCatalog);
                if (tour != null) return WithPersonLuggage(tour, def, random);
            }

            if (def.Generator == "survey")
            {
                var subAdapter = ToSubCategoryConfig(def);
                return WithPersonLuggage(MissionGenerator.CreateSurveyMissionSync(
                    origin, subAdapter, random, referenceTime, payMultiplier, payTable, placesService, textCatalog), def, random);
            }

            if (def.Generator == "skydive")
            {
                var subAdapter = ToSubCategoryConfig(def);
                return WithPersonLuggage(MissionGenerator.CreateSkydiveMissionSync(
                    origin, subAdapter, random, referenceTime, payMultiplier, payTable, placesService, textCatalog), def, random);
            }

            if (def.Generator == "tgl_circuit")
            {
                int tglCount = random.Next(3, 7);
                double tglDist = tglCount * 4.0;
                string title = $"Circuito de Tráfego ({tglCount} TGLs)";
                string desc = $"Treinamento em circuito de tráfego padrão em {origin.ICAO}. Exige {tglCount} toques e arremetidas (TGL) seguidos de pouso final completo.";

                var subAdapter = ToSubCategoryConfig(def);
                return MissionGenerator.CreateMissionObjectSync(
                    origin, origin, tglDist, subAdapter, paxCount, luggageLbs, cargoLbs,
                    random, referenceTime, payMultiplier, payTable,
                    requiredTouchAndGoes: tglCount, overflyWaypoints: null,
                    titleOverride: title, descriptionOverride: desc, textCatalog: textCatalog);
            }

            if (returnsToOrigin)
            {
                var subAdapter = ToSubCategoryConfig(def);
                return MissionGenerator.CreateMissionObjectSync(
                    origin, origin, distanceNm, subAdapter, paxCount, luggageLbs, cargoLbs,
                    random, referenceTime, payMultiplier, payTable, textCatalog: textCatalog);
            }

            // Destino padrão: filtrar candidatos e sortear ponderado por DestinationWeight
            var validCandidates = FilterCandidates(origin, def, profile, candidates, getAirportTags, candidateDistances);
            if (validCandidates.Count == 0) return null;

            var selectedCandidate = PickDestination(def, validCandidates, getAirportTags, random);
            if (selectedCandidate == null) return null;

            double actualDist = MissionGenerator.CalculateDistance(
                origin.Latitude, origin.Longitude, selectedCandidate.Airport.Latitude, selectedCandidate.Airport.Longitude);

            var subCfg = ToSubCategoryConfig(def);
            return MissionGenerator.CreateMissionObjectSync(
                origin, selectedCandidate.Airport, actualDist, subCfg, paxCount, luggageLbs, cargoLbs,
                random, referenceTime, payMultiplier, payTable,
                destRunway: selectedCandidate.BestRunway, textCatalog: textCatalog);
        }

        public static MissionProfileDefinition? PickProfile(
            MissionDefinition def,
            Airport origin,
            IReadOnlySet<string> originTags,
            Random random,
            Runway? originRunway = null)
        {
            if (def.Profiles == null || def.Profiles.Count == 0) return null;

            var availableProfiles = originRunway != null
                ? def.Profiles.Where(p => originRunway.LengthFt >= p.MinRunwayFt && (!p.Paved || !RunwaySurface.IsUnpaved(originRunway.Surface))).ToList()
                : def.Profiles;

            if (availableProfiles.Count == 0) return null;
            if (availableProfiles.Count == 1) return availableProfiles[0];

            bool isBigAirport = origin.Tier <= 2 || (def.OriginProfiles != null && def.OriginProfiles.Keys.Any(k => AirportProfileCatalog.MatchesAny(k, originTags)));

            var weightedProfiles = new List<(MissionProfileDefinition Profile, double Weight)>();
            double totalWeight = 0.0;

            foreach (var p in availableProfiles)
            {
                double w = p.Weight;
                if (p.BigAirport)
                {
                    w *= isBigAirport ? 1.5 : 0.5;
                }
                weightedProfiles.Add((p, w));
                totalWeight += w;
            }

            if (totalWeight <= 0.0) return availableProfiles[0];

            double roll = random.NextDouble() * totalWeight;
            double acc = 0.0;
            foreach (var (p, w) in weightedProfiles)
            {
                acc += w;
                if (roll <= acc) return p;
            }

            return weightedProfiles[^1].Profile;
        }

        /// <summary>
        /// Os geradores especiais (instrução, cheque, panorâmico, levantamento, paraquedismo) escolhem os
        /// próprios passageiros e não sabiam da bagagem por pessoa: aplica a bagagem do catálogo pelo
        /// número de pessoas da missão gerada e corrige o peso total.
        /// </summary>
        public static Mission? WithPersonLuggage(Mission? mission, MissionDefinition def, Random random)
        {
            if (mission == null) return null;
            double luggage = CalculateLuggageLbs(def, mission.PassengerCount, random);
            mission.PayloadLbs = Math.Round(mission.PayloadLbs - mission.LuggageWeightLbs + luggage, 0);
            mission.LuggageWeightLbs = luggage;
            return mission;
        }

        public static double CalculateLuggageLbs(MissionDefinition def, int paxCount, Random random)
        {
            double totalKg = 0.0;

            // Cada passageiro sorteia individualmente
            if (paxCount > 0 && def.LuggagePerPersonKg != null && def.LuggagePerPersonKg.Length >= 2)
            {
                int minKg = (int)def.LuggagePerPersonKg[0];
                int maxKg = (int)def.LuggagePerPersonKg[1];
                for (int i = 0; i < paxCount; i++)
                {
                    totalKg += minKg == maxKg ? minKg : random.Next(minKg, maxKg + 1);
                }
            }

            // Tripulação de apoio
            if (def.Crew != null && def.Crew.Count > 0 && def.Crew.LuggageKg != null && def.Crew.LuggageKg.Length >= 1)
            {
                int minKg = (int)def.Crew.LuggageKg[0];
                int maxKg = def.Crew.LuggageKg.Length >= 2 ? (int)def.Crew.LuggageKg[1] : minKg;
                for (int i = 0; i < def.Crew.Count; i++)
                {
                    totalKg += minKg == maxKg ? minKg : random.Next(minKg, maxKg + 1);
                }
            }

            return Math.Round(totalKg * 2.20462, 0);
        }

        private static List<AirportCandidate> FilterCandidates(
            Airport origin,
            MissionDefinition def,
            MissionProfileDefinition profile,
            IReadOnlyList<AirportCandidate> candidates,
            Func<string, IReadOnlySet<string>> getAirportTags,
            IReadOnlyList<double>? candidateDistances = null)
        {
            var list = new List<AirportCandidate>();
            bool hasDistances = candidateDistances != null && candidateDistances.Count == candidates.Count;
            double minRange = profile.RangeNm.Length > 0 ? profile.RangeNm[0] : 0;
            double maxRange = profile.RangeNm.Length > 1 ? profile.RangeNm[1] : 50000;

            if (def.Destination?.MaxNm != null)
            {
                maxRange = Math.Min(maxRange, def.Destination.MaxNm.Value);
            }

            var dest = def.Destination;
            bool sameCountry = dest != null && string.Equals(dest.Country, "same", StringComparison.OrdinalIgnoreCase);
            bool otherCountry = dest != null && string.Equals(dest.Country, "other", StringComparison.OrdinalIgnoreCase);
            bool requirePaved = def.RequiresHardSurface || (dest != null && string.Equals(dest.Runway, "paved", StringComparison.OrdinalIgnoreCase));
            bool requireLighted = dest != null && string.Equals(dest.Runway, "lighted", StringComparison.OrdinalIgnoreCase);

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                var a = c.Airport;
                if (string.Equals(a.ICAO, origin.ICAO, StringComparison.OrdinalIgnoreCase)) continue;

                // Distância primeiro (filtro mais seletivo e mais barato com a distância pré-calculada)
                double dist = hasDistances
                    ? candidateDistances![i]
                    : MissionGenerator.CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude);
                if (dist < minRange || dist > maxRange)
                {
                    continue;
                }

                // Tier do destino
                if (dest?.Tiers != null && dest.Tiers.Count > 0 && !dest.Tiers.Contains(a.Tier))
                {
                    continue;
                }

                // País
                if (sameCountry && !string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (otherCountry && (string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(a.Country)))
                {
                    continue;
                }

                // Pista exigida pelo perfil sorteado
                if (profile.MinRunwayFt > 0)
                {
                    if (c.BestRunway == null)
                    {
                        continue;
                    }
                    if (c.BestRunway.LengthFt < profile.MinRunwayFt)
                    {
                        continue;
                    }
                    if (profile.Paved && RunwaySurface.IsUnpaved(c.BestRunway.Surface))
                    {
                        continue;
                    }
                }

                // Pista (regras adicionais da definição)
                if (requirePaved)
                {
                    bool isPaved = c.BestRunway != null ? !RunwaySurface.IsUnpaved(c.BestRunway.Surface) : a.Tier <= 3;
                    if (!isPaved) continue;
                }

                if (requireLighted)
                {
                    bool isLighted = c.BestRunway != null && c.BestRunway.IsLighted;
                    if (!isLighted) continue;
                }

                if (def.MinRunwayDest > 0 && c.BestRunway != null && c.BestRunway.LengthFt < def.MinRunwayDest)
                {
                    continue;
                }

                list.Add(c);
            }

            return list;
        }

        public static AirportCandidate? PickDestination(
            MissionDefinition def,
            IReadOnlyList<AirportCandidate> candidates,
            Func<string, IReadOnlySet<string>> getAirportTags,
            Random random)
        {
            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0];

            var weighted = new List<(AirportCandidate Candidate, double Weight)>(candidates.Count);
            double totalWeight = 0.0;

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                var tags = getAirportTags(c.Airport.ICAO);

                // Se o candidato tiver pista curta/não pavimentada, adiciona tag virtual para casamento
                bool isShortOrUnpaved = (c.BestRunway != null && (c.BestRunway.LengthFt <= 4000 || RunwaySurface.IsUnpaved(c.BestRunway.Surface))) || c.Airport.Tier == 4;
                if (isShortOrUnpaved && !tags.Contains("pista_curta"))
                {
                    var extTags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase) { "pista_curta" };
                    tags = extTags;
                }

                double w = MissionWeighting.DestinationWeight(def, tags);
                weighted.Add((c, w));
                totalWeight += w;
            }

            if (totalWeight <= 0.0) return candidates[random.Next(candidates.Count)];

            double roll = random.NextDouble() * totalWeight;
            double acc = 0.0;
            for (int i = 0; i < weighted.Count; i++)
            {
                acc += weighted[i].Weight;
                if (roll <= acc) return weighted[i].Candidate;
            }

            return weighted[^1].Candidate;
        }

        private static MissionDefinition? PickWeighted(List<(MissionDefinition Mission, double Weight)> items, double totalWeight, Random random)
        {
            double roll = random.NextDouble() * totalWeight;
            double acc = 0.0;
            for (int i = 0; i < items.Count; i++)
            {
                acc += items[i].Weight;
                if (roll <= acc) return items[i].Mission;
            }
            return items.Count > 0 ? items[^1].Mission : null;
        }

        private static SubCategoryConfig ToSubCategoryConfig(MissionDefinition def)
        {
            return new SubCategoryConfig
            {
                Id = def.Id,
                Name = def.Name,
                Description = def.Description,
                MinExpHours = def.MinExpHours,
                MaxExpHours = def.MaxExpHours,
                Urgency = def.Urgency,
                RequiresHardSurface = def.RequiresHardSurface,
                MinRunwayDest = def.MinRunwayDest,
                LuggageWeightKg = def.LuggageWeightKg,
                Definition = def
            };
        }
    }
}
