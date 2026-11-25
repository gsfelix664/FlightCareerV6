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
        /// <summary>Etiqueta derivada: aeroporto com <c>linha_auto</c> de porte suficiente para jatos de linha.</summary>
        public const string LinhaAutoHubTag = "linha_auto:hub";
        /// <summary>
        /// Pontuação mínima (<see cref="Airport.TierScore"/>) para um tier 2 só com <c>linha_auto</c> (voo regular do
        /// OurAirports ou gates do simulador) contar como hub de linha aérea para jatos de 120+ lugares; tier 1 sempre
        /// conta (decisão do usuário, 2026-11-25): Sorocaba (58), Tefé (79) e Bethel (99) deixam de gerar ponte
        /// aérea/troncal; hubs sem curadoria como Ufa (105) mantêm.
        /// </summary>
        public const int LinhaAutoHubTier2MinScore = 100;

        /// <summary>Acrescenta as etiquetas que dependem do aeroporto (hoje só <see cref="LinhaAutoHubTag"/>).</summary>
        public static IReadOnlySet<string> WithDerivedTags(Airport airport, IReadOnlySet<string> tags)
        {
            if (airport == null || tags == null) return tags!;
            bool isHub = airport.Tier == 1 || (airport.Tier == 2 && airport.TierScore >= LinhaAutoHubTier2MinScore);
            if (isHub && tags.Contains("linha_auto") && !tags.Contains(LinhaAutoHubTag))
            {
                return new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase) { LinhaAutoHubTag };
            }
            return tags;
        }

        public static IReadOnlySet<string> AugmentOriginTags(Airport origin, IReadOnlySet<string> tags, IPlacesDataService? placesService)
        {
            if (origin == null) return tags;
            tags = WithDerivedTags(origin, tags);
            if (placesService == null) return tags;
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
            Runway? originRunway = null,
            BoardCache? cache = null)
        {
            var missions = new List<Mission>();
            if (origin == null || candidates == null || catalog == null) return missions;

            if (originRunway == null)
            {
                System.Diagnostics.Debug.WriteLine($"[MissionBoardCore] Origem {origin.ICAO} sem pista conhecida: não filtra perfis pela pista da origem.");
            }

            // 1. Filtrar missões habilitadas com peso de origem > 0 e que caibam na pista e etiquetas da origem
            var (eligibleMissions, totalOriginWeight) = cache != null
                ? cache.GetOrComputeEligibleMissions(origin, originRunway, originTags, catalog)
                : ComputeEligibleMissions(origin, originRunway, originTags, catalog);

            if (eligibleMissions.Count == 0) return missions;
            if (totalOriginWeight <= 0.0) return missions;

            int maxAttempts = targetCount * 4;
            int attempts = 0;

            // Sem cache externo, um com a vida desta chamada: distâncias e destinos são calculados uma vez por quadro
            var boardCache = cache ?? new BoardCache();
            var distances = boardCache.GetOrComputeDistances(origin, candidates);

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
                    originRunway,
                    boardCache);

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
            Runway? originRunway = null,
            BoardCache? cache = null)
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
                var nearbyAirports = cache?.Tier1And2Airports ?? candidates.Where(c => c.Airport.Tier <= 2).Select(c => c.Airport).ToList();
                return WithPersonLuggage(MissionGenerator.CreateLocalInstructionMissionSync(
                    origin, def, random, referenceTime, nearbyAirports, payMultiplier, payTable, textCatalog, placesService), def, random);
            }

            if (def.Generator == "checkride")
            {
                var nearbyAirports = cache?.Tier1And2Airports ?? candidates.Where(c => c.Airport.Tier <= 2).Select(c => c.Airport).ToList();
                return WithPersonLuggage(MissionGenerator.CreateCheckrideMissionSync(
                    origin, def, random, referenceTime, nearbyAirports, payMultiplier, payTable, textCatalog, placesService), def, random);
            }

            if (def.Generator == "scenic_tour")
            {
                var tour = MissionGenerator.CreateScenicTourMissionSync(
                    origin, def, random, referenceTime, payMultiplier, payTable, placesService, textCatalog);
                if (tour != null) return WithPersonLuggage(tour, def, random);
            }

            if (def.Generator == "survey")
            {
                return WithPersonLuggage(MissionGenerator.CreateSurveyMissionSync(
                    origin, def, random, referenceTime, payMultiplier, payTable, placesService, textCatalog), def, random);
            }

            if (def.Generator == "skydive")
            {
                return WithPersonLuggage(MissionGenerator.CreateSkydiveMissionSync(
                    origin, def, random, referenceTime, distanceNm, payMultiplier, payTable, placesService, textCatalog, paxOverride: paxCount), def, random);
            }

            if (def.Generator == "multi_stop")
            {
                var multi = MissionGenerator.CreateMultiStopMissionSync(
                    origin, def, random, referenceTime, candidates, payMultiplier, payTable, textCatalog, paxCount, cargoLbs, profile);
                return multi != null ? WithPersonLuggage(multi, def, random) : null;
            }

            if (def.Generator == "vfr_triangle")
            {
                var triangle = MissionGenerator.CreateVfrTriangleMissionSync(
                    origin, def, random, referenceTime, candidates, payMultiplier, payTable, textCatalog, profile);
                return triangle != null ? WithPersonLuggage(triangle, def, random) : null;
            }

            if (def.Generator == "stol_training")
            {
                var stol = MissionGenerator.CreateStolTrainingMissionSync(
                    origin, def, random, referenceTime, candidates, payMultiplier, payTable, textCatalog);
                return stol != null ? WithPersonLuggage(stol, def, random) : null;
            }

            if (def.Generator == "check_flight")
            {
                var check = MissionGenerator.CreateCheckFlightMissionSync(
                    origin, def, random, referenceTime, distanceNm, payMultiplier, payTable, textCatalog, paxCount);
                return WithPersonLuggage(check, def, random);
            }

            if (def.Generator == "ferry_permit")
            {
                var destCand = SelectDestination(origin, def, profile, candidates, getAirportTags, random, candidateDistances, cache);
                if (destCand == null) return null;
                double actualDistFerry = MissionGenerator.CalculateDistance(
                    origin.Latitude, origin.Longitude, destCand.Airport.Latitude, destCand.Airport.Longitude);
                return MissionGenerator.CreateFerryPermitMissionSync(
                    origin, destCand.Airport, actualDistFerry, def, random, referenceTime,
                    payMultiplier, payTable, textCatalog, destCand.BestRunway);
            }

            if (def.Generator == "patrol")
            {
                var patrol = MissionGenerator.CreatePatrolMissionSync(
                    origin, def, random, referenceTime, payMultiplier, payTable, placesService, textCatalog, paxCount, cargoLbs, profile);
                return patrol != null ? WithPersonLuggage(patrol, def, random) : null;
            }

            if (def.Generator == "line_inspection")
            {
                var line = MissionGenerator.CreateLineInspectionMissionSync(
                    origin, def, random, referenceTime, payMultiplier, payTable, textCatalog, paxCount, cargoLbs, profile);
                return line != null ? WithPersonLuggage(line, def, random) : null;
            }

            if (def.Generator == "traffic_orbit")
            {
                var orbit = MissionGenerator.CreateTrafficOrbitMissionSync(
                    origin, def, random, referenceTime, payMultiplier, payTable, placesService, textCatalog, paxCount, cargoLbs, profile);
                return orbit != null ? WithPersonLuggage(orbit, def, random) : null;
            }

            if (def.Generator == "sar_pattern")
            {
                var sar = MissionGenerator.CreateSarMissionSync(
                    origin, def, random, referenceTime, payMultiplier, payTable, textCatalog, paxCount, cargoLbs, profile);
                return sar != null ? WithPersonLuggage(sar, def, random) : null;
            }

            if (def.Generator == "round_trip")
            {
                var destCand = SelectDestination(origin, def, profile, candidates, getAirportTags, random, candidateDistances, cache);
                if (destCand == null) return null;
                double actualIdaDist = MissionGenerator.CalculateDistance(
                    origin.Latitude, origin.Longitude, destCand.Airport.Latitude, destCand.Airport.Longitude);
                var roundTrip = MissionGenerator.CreateRoundTripMissionSync(
                    origin, destCand.Airport, actualIdaDist, def, random, referenceTime,
                    payMultiplier, payTable, textCatalog, paxCount, cargoLbs, destCand.BestRunway);
                return roundTrip != null ? WithPersonLuggage(roundTrip, def, random) : null;
            }

            if (def.Generator == "tgl_circuit")
            {
                int tglCount = random.Next(3, 7);
                double tglDist = tglCount * 4.0;
                string title = $"Circuito de Tráfego ({tglCount} TGLs)";
                string desc = $"Treinamento em circuito de tráfego padrão em {origin.ICAO}. Exige {tglCount} toques e arremetidas (TGL) seguidos de pouso final completo.";

                return MissionGenerator.CreateMissionObjectSync(
                    origin, origin, tglDist, def, paxCount, luggageLbs, cargoLbs,
                    random, referenceTime, payMultiplier, payTable,
                    requiredTouchAndGoes: tglCount, overflyWaypoints: null,
                    titleOverride: title, descriptionOverride: desc, textCatalog: textCatalog);
            }

            if (returnsToOrigin)
            {
                return MissionGenerator.CreateMissionObjectSync(
                    origin, origin, distanceNm, def, paxCount, luggageLbs, cargoLbs,
                    random, referenceTime, payMultiplier, payTable, textCatalog: textCatalog);
            }

            // Destino padrão: filtrar candidatos e sortear ponderado por DestinationWeight
            var selectedCandidate = SelectDestination(origin, def, profile, candidates, getAirportTags, random, candidateDistances, cache);
            if (selectedCandidate == null) return null;

            double actualDist = MissionGenerator.CalculateDistance(
                origin.Latitude, origin.Longitude, selectedCandidate.Airport.Latitude, selectedCandidate.Airport.Longitude);

            return MissionGenerator.CreateMissionObjectSync(
                origin, selectedCandidate.Airport, actualDist, def, paxCount, luggageLbs, cargoLbs,
                random, referenceTime, payMultiplier, payTable,
                destRunway: selectedCandidate.BestRunway, textCatalog: textCatalog);
        }

        internal static (List<(MissionDefinition Mission, double Weight)> Missions, double TotalWeight) ComputeEligibleMissions(
            Airport origin,
            Runway? originRunway,
            IReadOnlySet<string> originTags,
            MissionCatalog catalog)
        {
            var eligible = new List<(MissionDefinition Mission, double Weight)>();
            foreach (var m in catalog.Missions.Values)
            {
                if (!m.Enabled) continue;

                bool anyProfileFits = m.Profiles.Any(p =>
                    (originRunway == null || (originRunway.LengthFt >= p.MinRunwayFt && (!p.Paved || !RunwaySurface.IsUnpaved(originRunway.Surface))))
                    && (p.RequireAnyTag == null || p.RequireAnyTag.Count == 0 || (originTags != null && p.RequireAnyTag.Any(pat => AirportProfileCatalog.MatchesAny(pat, originTags))))
                );
                if (!anyProfileFits) continue;

                double w = MissionWeighting.OriginWeight(m, origin.Tier, originTags);
                if (w > 0.0)
                {
                    eligible.Add((m, w));
                }
            }

            return (eligible, eligible.Sum(x => x.Weight));
        }

        private static AirportCandidate? SelectDestination(
            Airport origin,
            MissionDefinition def,
            MissionProfileDefinition profile,
            IReadOnlyList<AirportCandidate> candidates,
            Func<string, IReadOnlySet<string>> getAirportTags,
            Random random,
            IReadOnlyList<double>? candidateDistances,
            BoardCache? cache)
        {
            if (cache != null)
            {
                return cache.GetOrComputeDestinationGroup(origin, def, profile, candidates, getAirportTags, candidateDistances).Pick(random);
            }

            var valid = FilterCandidates(origin, def, profile, candidates, getAirportTags, candidateDistances);
            return PickDestination(def, valid, getAirportTags, random);
        }

        /// <summary>Etiquetas do candidato, com a etiqueta virtual "pista_curta" para casamento de destino.</summary>
        internal static IReadOnlySet<string> DestinationTags(AirportCandidate c, Func<string, IReadOnlySet<string>> getAirportTags)
        {
            var tags = getAirportTags(c.Airport.ICAO);
            bool isShortOrUnpaved = (c.BestRunway != null && (c.BestRunway.LengthFt <= 4000 || RunwaySurface.IsUnpaved(c.BestRunway.Surface))) || c.Airport.Tier == 4;
            if (isShortOrUnpaved && !tags.Contains("pista_curta"))
            {
                return new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase) { "pista_curta" };
            }
            return tags;
        }

        public static MissionProfileDefinition? PickProfile(
            MissionDefinition def,
            Airport origin,
            IReadOnlySet<string> originTags,
            Random random,
            Runway? originRunway = null)
        {
            if (def.Profiles == null || def.Profiles.Count == 0) return null;

            var availableProfiles = def.Profiles.Where(p =>
                (originRunway == null || (originRunway.LengthFt >= p.MinRunwayFt && (!p.Paved || !RunwaySurface.IsUnpaved(originRunway.Surface))))
                && (p.RequireAnyTag == null || p.RequireAnyTag.Count == 0 || (originTags != null && p.RequireAnyTag.Any(pat => AirportProfileCatalog.MatchesAny(pat, originTags))))
            ).ToList();

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

        internal static List<AirportCandidate> FilterCandidates(
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

                // Pista exigida pelo perfil sorteado (filtro barato em memória antes das tags)
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

                // Etiquetas exigidas pelo destino (nível da missão ou do perfil sorteado)
                if ((dest?.RequireAnyTag != null && dest.RequireAnyTag.Count > 0) ||
                    (profile.RequireAnyTag != null && profile.RequireAnyTag.Count > 0))
                {
                    var destTags = WithDerivedTags(a, getAirportTags(a.ICAO));
                    if (dest?.RequireAnyTag != null && dest.RequireAnyTag.Count > 0)
                    {
                        if (destTags == null || destTags.Count == 0 || !dest.RequireAnyTag.Any(pat => AirportProfileCatalog.MatchesAny(pat, destTags)))
                        {
                            continue;
                        }
                    }

                    if (profile.RequireAnyTag != null && profile.RequireAnyTag.Count > 0)
                    {
                        if (destTags == null || destTags.Count == 0 || !profile.RequireAnyTag.Any(pat => AirportProfileCatalog.MatchesAny(pat, destTags)))
                        {
                            continue;
                        }
                    }
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
            => new DestinationChoiceGroup(def, candidates, c => DestinationTags(c, getAirportTags)).Pick(random);

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
    }
}
