using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Career;
using FlightCareer.Domain.Missions;
using FlightCareer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.Application.Services
{
    public class MissionGenerator : IMissionGenerator
    {
        private readonly IAppDbContext _context;
        private readonly IHubDataService _hubService;
        private readonly IPlacesDataService _placesService;
        private readonly MissionTextCatalog _textCatalog;
        private readonly Dictionary<string, Runway?> _runwayCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random _random;
        private bool? _hasSimScenery;
        private DateTime? _currentReferenceTime;

        public double PayMultiplier { get; set; } = 1.0;
        public MissionPayTable PayTable { get; set; } = MissionPayTable.Shared;

        public MissionGenerator(
            IAppDbContext context,
            IHubDataService? hubService = null,
            Random? random = null,
            IPlacesDataService? placesService = null,
            MissionTextCatalog? textCatalog = null)
        {
            _context = context;
            _hubService = hubService ?? new HubDataService();
            _random = random ?? new Random();
            _placesService = placesService ?? new PlacesDataService();
            _textCatalog = textCatalog ?? MissionTextCatalog.Default;
        }

        public MissionGenerator(
            IAppDbContext context,
            IHubDataService? hubService,
            FlightCareer.Domain.Components.IRandomSource randomSource,
            IPlacesDataService? placesService = null,
            MissionTextCatalog? textCatalog = null)
            : this(context, hubService, new RandomAdapter(randomSource), placesService, textCatalog)
        {
        }

        private sealed class RandomAdapter : Random
        {
            private readonly FlightCareer.Domain.Components.IRandomSource _source;
            public RandomAdapter(FlightCareer.Domain.Components.IRandomSource source) => _source = source ?? throw new ArgumentNullException(nameof(source));
            public override double NextDouble() => _source.NextDouble();
            public override int Next() => (int)(_source.NextDouble() * int.MaxValue);
            public override int Next(int maxValue)
            {
                if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
                if (maxValue == 0) return 0;
                return (int)(_source.NextDouble() * maxValue);
            }
            public override int Next(int minValue, int maxValue)
            {
                if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
                if (minValue == maxValue) return minValue;
                long range = (long)maxValue - minValue;
                return (int)(minValue + (_source.NextDouble() * range));
            }
        }

        private async Task<bool> HasSimSceneryAsync()
        {
            if (!_hasSimScenery.HasValue)
            {
                _hasSimScenery = await _context.Airports.AnyAsync(a => a.IsInSimulator);
            }
            return _hasSimScenery.Value;
        }

        public async Task<IEnumerable<Mission>> GenerateMissionsAsync(Airport currentAirport, DateTime? referenceTime = null)
        {
            _context.ChangeTracker.Clear(); // Contexto de longa duração: descarta entidades em cache para ler o estado atual do banco
            _runwayCache.Clear();
            var now = referenceTime ?? DateTime.Now;
            _currentReferenceTime = now;
            // 1. Limpar missões expiradas (que não foram aceitas)
            var expired = await _context.Missions
                .Where(m => !m.IsAccepted && m.ExpirationTime < now)
                .ToListAsync();
            
            if (expired.Any())
            {
                _context.Missions.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }

            // 2. Simular Tráfego Virtual (Outros pilotos aceitando missões)
            // Se as missões existentes forem muito antigas, removemos algumas aleatoriamente
            var currentMktMissions = await _context.Missions
                .Where(m => m.OriginICAO == currentAirport.ICAO && !m.IsAccepted)
                .OrderBy(m => m.DestinationICAO)
                .ThenBy(m => m.PayloadLbs)
                .ThenBy(m => m.RewardMoney)
                .ToListAsync();

            if (currentMktMissions.Any())
            {
                var oldestMission = currentMktMissions.OrderBy(m => m.Id).First(); // Usamos o Id/GUID como proxy de criação se não tivermos data de criação
                // Como não temos CreatedAt, vamos usar a ExpirationTime para estimar
                // Se o tempo para expirar de muitas missões for curto, significa que elas estão lá há tempo
                int toRemoveCount = 0;
                foreach(var m in currentMktMissions)
                {
                    // 10% de chance de uma missão sumir a cada "verificação" se ela já estiver na metade da vida
                    // Isso simula o fluxo do mercado
                    if (_random.NextDouble() < 0.05) toRemoveCount++; 
                }

                if (toRemoveCount > 0)
                {
                    var toRemove = currentMktMissions.OrderBy(x => _random.Next()).Take(toRemoveCount);
                    _context.Missions.RemoveRange(toRemove);
                    await _context.SaveChangesAsync();
                    
                    // Atualiza a lista local após a remoção
                    currentMktMissions = await _context.Missions
                        .Where(m => m.OriginICAO == currentAirport.ICAO && !m.IsAccepted)
                        .OrderBy(m => m.DestinationICAO)
                        .ThenBy(m => m.PayloadLbs)
                        .ThenBy(m => m.RewardMoney)
                        .ToListAsync();
                }
            }

            // 3. Verificar se precisamos repor o estoque
            if (!MissionRules.Rules.TryGetValue(currentAirport.Tier, out var tierConfig))
                return currentMktMissions;

            // Cota de missões visíveis proporcional ao porte do aeroporto
            int targetCount = currentAirport.Tier switch
            {
                1 => 200, // Grandes Hubs Internacionais (mínimo de 200 missões ativas)
                2 => 120, // Regionais Médios / Capitais Estaduais / Polos de Instrução
                3 => 60,  // Municipais / Interior / Aeroclubes Locais
                4 => 25,  // Pistas Remotas / Não Pavimentadas / Fazendas
                _ => 25
            };

            if (currentMktMissions.Count < targetCount)
            {
                int needed = targetCount - currentMktMissions.Count;
                var originTags = MissionBoardCore.AugmentOriginTags(currentAirport, AirportProfileCatalog.Shared.TagsFor(currentAirport.ICAO), _placesService);

                // Calcular maior alcance entre missões elegíveis na origem
                double maxRangeNm = 500.0;
                foreach (var m in MissionCatalog.Shared.Missions.Values)
                {
                    if (m.Enabled && MissionWeighting.OriginWeight(m, currentAirport.Tier, originTags) > 0.0)
                    {
                        foreach (var p in m.Profiles)
                        {
                            if (p.RangeNm.Length > 1 && p.RangeNm[1] > maxRangeNm)
                            {
                                maxRangeNm = p.RangeNm[1];
                            }
                        }
                    }
                }

                double latDelta = maxRangeNm / 60.0;
                double minLat = currentAirport.Latitude - latDelta;
                double maxLat = currentAirport.Latitude + latDelta;
                double cosLat = Math.Max(0.01, Math.Cos(currentAirport.Latitude * Math.PI / 180.0));
                double lonDelta = maxRangeNm / (60.0 * cosLat);
                double minLon = currentAirport.Longitude - lonDelta;
                double maxLon = currentAirport.Longitude + lonDelta;

                var query = _context.Airports.AsQueryable();
                if (await HasSimSceneryAsync())
                {
                    query = query.Where(a => a.IsInSimulator);
                }
                if (minLat > -90.0) query = query.Where(a => a.Latitude >= minLat);
                if (maxLat < 90.0) query = query.Where(a => a.Latitude <= maxLat);
                if (lonDelta < 180.0)
                {
                    query = query.Where(a => a.Longitude >= minLon && a.Longitude <= maxLon);
                }

                var candidateAirports = await query.ToListAsync();
                candidateAirports = LimitCandidates(currentAirport, candidateAirports, _random);

                var candidateIcaos = candidateAirports.Select(a => a.ICAO).ToHashSet(StringComparer.OrdinalIgnoreCase);
                candidateIcaos.Add(currentAirport.ICAO);
                var candidateRunways = await _context.Runways
                    .Where(r => candidateIcaos.Contains(r.AirportIdent))
                    .ToListAsync();

                var runwaysByIcao = candidateRunways
                    .GroupBy(r => r.AirportIdent, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(r => !RunwaySurface.IsUnpaved(r.Surface)).ThenByDescending(r => r.LengthFt).FirstOrDefault(),
                        StringComparer.OrdinalIgnoreCase);

                var originRunway = runwaysByIcao.GetValueOrDefault(currentAirport.ICAO);

                var candidates = candidateAirports
                    .Select(a => new AirportCandidate(a, runwaysByIcao.GetValueOrDefault(a.ICAO)))
                    .ToList();

                var newMissions = MissionBoardCore.GenerateBoard(
                    currentAirport,
                    candidates,
                    originTags,
                    icao => AirportProfileCatalog.Shared.TagsFor(icao),
                    MissionCatalog.Shared,
                    _random,
                    now,
                    needed,
                    PayMultiplier,
                    PayTable,
                    _textCatalog,
                    _placesService,
                    originRunway);

                if (newMissions.Count > 0)
                {
                    await _context.Missions.AddRangeAsync(newMissions);
                    await _context.SaveChangesAsync();
                }

                // Retorna a união das existentes com as novas
                return await _context.Missions
                    .Where(m => m.OriginICAO == currentAirport.ICAO && !m.IsAccepted)
                    .ToListAsync();
            }

            return currentMktMissions;
        }

        /// <summary>Raio em que todos os candidatos são mantidos (missões curtas e regionais).</summary>
        public const double NearCandidateRadiusNm = 600.0;
        /// <summary>Máximo de candidatos carregados do banco por quadro.</summary>
        public const int MaxCandidates = 3000;

        /// <summary>
        /// Limita os candidatos sem prejudicar as missões curtas: mantém todos os aeroportos até
        /// <see cref="NearCandidateRadiusNm"/> (amostra aleatória só se passarem de 5/6 do limite) e completa
        /// com uma amostra aleatória dos distantes (longo curso). Ordena por ICAO antes de sortear para que a
        /// mesma semente dê o mesmo resultado independentemente da ordem do banco.
        /// </summary>
        public static List<Airport> LimitCandidates(Airport origin, List<Airport> airports, Random random)
        {
            if (airports.Count <= MaxCandidates) return airports;

            var ordered = airports.OrderBy(a => a.ICAO, StringComparer.Ordinal).ToList();
            var near = new List<Airport>();
            var far = new List<Airport>();
            foreach (var a in ordered)
            {
                double d = CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude);
                (d <= NearCandidateRadiusNm ? near : far).Add(a);
            }

            int nearQuota = Math.Min(near.Count, MaxCandidates * 5 / 6);
            var result = near.Count > nearQuota
                ? near.OrderBy(_ => random.Next()).Take(nearQuota).ToList()
                : near;
            int farQuota = MaxCandidates - result.Count;
            if (farQuota > 0 && far.Count > 0)
            {
                result.AddRange(far.Count > farQuota ? far.OrderBy(_ => random.Next()).Take(farQuota) : far);
            }
            return result;
        }

        private async Task<Mission?> CreateMissionFromSubCategory(Airport origin, SubCategoryConfig sub, List<int> allowedTiers)
        {
            // 1. Sortear parâmetros básicos
            var rangeRule = PickWeighted(sub.RangeRules);
            var paxRule = PickWeighted(sub.PaxRules);
            var lbsRule = sub.LbsRules.Any() ? PickWeighted(sub.LbsRules) : null;

            if (rangeRule == null) return null;

            int minRange = rangeRule.Min;
            int maxRange = rangeRule.Max;
            int paxCount = paxRule != null ? _random.Next(paxRule.Min, paxRule.Max + 1) : 0;
            
            // Lógica Unificada e Simplificada de Pesos
            double luggageWeightLbs = paxCount > 0 ? paxCount * _random.Next(0, 51) : 0;
            double cargoWeight = lbsRule != null ? _random.Next(lbsRule.Min, lbsRule.Max + 1) : 0;

            var query = _context.Airports.AsQueryable();
            // Filtrar estritamente apenas aeroportos verificados e existentes no MSFS se houver sincronização ativa
            if (await HasSimSceneryAsync())
            {
                query = query.Where(a => a.IsInSimulator);
            }
            query = query.Where(a => allowedTiers.Contains(a.Tier));
            if (sub.RequiresHardSurface) 
            {
                // Missões que exigem pista rígida (Linha Aérea / Jatos) não aceitam pistas de terra/grama de fazenda (Tier 4)
                query = query.Where(a => a.Tier <= 3);
            }
            if (sub.MinRunwayDest >= 7000)
            {
                // Jatos pesados / Linha Regular de Alta Densidade (B737/A320/B777): Apenas grandes Hubs internacionais
                query = query.Where(a => a.Tier == 1);
            }
            else if (sub.MinRunwayDest >= 4500)
            {
                // Aviação Regional Pesada / Turboprops / Jatos médios: Tiers 1 e 2
                query = query.Where(a => a.Tier <= 2);
            }
            else if (sub.MinRunwayDest >= 2500)
            {
                // Táxi Aéreo / Charter: Tiers 1, 2 e 3
                query = query.Where(a => a.Tier <= 3);
            }

            // Filtros específicos por contrato das Fases 3 a 7
            switch (sub.Destination.Rule)
            {
                case "same_country_tier_3_4":
                    // Fretamento Executivo: capital/hub ou regional -> interior (tier 3 ou 4) do mesmo país
                    query = query.Where(a => a.Country == origin.Country && (a.Tier == 3 || a.Tier == 4));
                    break;
                case "other_country":
                    // VIP Internacional Curto: país vizinho / diferente
                    query = query.Where(a => a.Country != origin.Country && a.Country != "");
                    break;
                case "tier1_same_country":
                    // Ponte Aérea / Troncal: hub ↔ hub do mesmo país
                    query = query.Where(a => a.Tier == 1 && a.Country == origin.Country);
                    break;
                case "tier1":
                    // Cargueiro Noturno: entre hubs
                    query = query.Where(a => a.Tier == 1);
                    break;
                case "tier1_other_country":
                    // Long-Haul Internacional: entre hubs de países diferentes
                    query = query.Where(a => a.Tier == 1 && a.Country != origin.Country && a.Country != "");
                    break;
                case "tier_max_2":
                    // Fretamento Global / Carga de Alto Valor: aeroportos de grande porte
                    query = query.Where(a => a.Tier <= 2);
                    break;
            }

            if (sub.Generator == "local_instruction") 
            {
                var nearbyTierAirports = await query.Where(a => a.Tier <= 2).ToListAsync();
                return CreateLocalInstructionMissionSync(
                    origin, sub, _random, _currentReferenceTime ?? DateTime.Now,
                    nearbyTierAirports, PayMultiplier, PayTable, _textCatalog);
            }

            if (sub.Generator == "checkride")
            {
                var nearbyTierAirports = await query.Where(a => a.Tier <= 2).ToListAsync();
                return CreateCheckrideMissionSync(
                    origin, sub, _random, _currentReferenceTime ?? DateTime.Now,
                    nearbyTierAirports, PayMultiplier, PayTable, _textCatalog);
            }

            if (sub.Generator == "scenic_tour")
            {
                var tourMission = CreateScenicTourMissionSync(
                    origin, sub, _random, _currentReferenceTime ?? DateTime.Now,
                    PayMultiplier, PayTable, _placesService, _textCatalog);
                if (tourMission != null) return tourMission;

                // Voo panorâmico fallback: 2 a 3 aeródromos a 8-30 NM da origem, percurso total, retorno à origem
                double maxDegWp = 35.0 / 50.0;
                var wpCandidates = await query
                    .Where(a => a.ICAO != origin.ICAO)
                    .Where(a => a.Latitude >= origin.Latitude - maxDegWp && a.Latitude <= origin.Latitude + maxDegWp)
                    .Where(a => a.Longitude >= origin.Longitude - maxDegWp && a.Longitude <= origin.Longitude + maxDegWp)
                    .ToListAsync();

                var validWps = wpCandidates
                    .Select(a => new { Airport = a, Dist = CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude) })
                    .Where(x => x.Dist >= 8.0 && x.Dist <= 30.0)
                    .ToList();

                if (validWps.Count < 2)
                {
                    validWps = wpCandidates
                        .Select(a => new { Airport = a, Dist = CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude) })
                        .Where(x => x.Dist >= 5.0 && x.Dist <= 45.0)
                        .ToList();
                }

                if (validWps.Count < 2) return null;

                int pointsCount = Math.Min(validWps.Count, _random.Next(2, 4));
                var picked = validWps.OrderBy(_ => _random.Next()).Take(pointsCount).Select(x => x.Airport).ToList();
                // Ordenação por azimute angular em relação à origem para rota circular contínua
                picked = picked.OrderBy(a => Math.Atan2(a.Latitude - origin.Latitude, a.Longitude - origin.Longitude)).ToList();

                double totalTourDist = CalculateDistance(origin.Latitude, origin.Longitude, picked[0].Latitude, picked[0].Longitude);
                for (int i = 0; i < picked.Count - 1; i++)
                {
                    totalTourDist += CalculateDistance(picked[i].Latitude, picked[i].Longitude, picked[i + 1].Latitude, picked[i + 1].Longitude);
                }
                totalTourDist += CalculateDistance(picked[^1].Latitude, picked[^1].Longitude, origin.Latitude, origin.Longitude);

                string wpString = string.Join(";", picked.Select(p => p.ICAO));
                int tourPax = Math.Clamp(paxCount > 0 ? paxCount : _random.Next(1, 4), 1, 3);
                string title = $"Voo Panorâmico ({string.Join(" • ", picked.Select(p => p.ICAO))})";
                string desc = $"Voo panorâmico sobrevoando os aeródromos da região: {string.Join(", ", picked.Select(p => p.ICAO))}. Retorno para pouso em {origin.ICAO} (sobrevoo a ≤ 2 NM e abaixo de 3.000 ft AGL).";

                return await CreateMissionObject(origin, origin, totalTourDist, sub, tourPax, Math.Min(luggageWeightLbs, 30.0), 0,
                    requiredTouchAndGoes: 0, overflyWaypoints: wpString, titleOverride: title, descriptionOverride: desc);
            }

            if (sub.Generator == "tgl_circuit")
            {
                int tglCount = _random.Next(3, 7);
                int tglPax = _random.Next(0, 2);
                double tglDist = tglCount * 4.0;
                string title = $"Circuito de Tráfego ({tglCount} TGLs)";
                string desc = $"Treinamento em circuito de tráfego padrão em {origin.ICAO}. Exige {tglCount} toques e arremetidas (TGL) seguidos de pouso final completo.";

                return await CreateMissionObject(origin, origin, tglDist, sub, tglPax, 0, 0,
                    requiredTouchAndGoes: tglCount, overflyWaypoints: null, titleOverride: title, descriptionOverride: desc);
            }

            if (sub.Generator == "survey")
            {
                return CreateSurveyMissionSync(origin, sub, _random, _currentReferenceTime ?? DateTime.Now, PayMultiplier, PayTable, _placesService, _textCatalog);
            }

            if (sub.Generator == "skydive")
            {
                return CreateSkydiveMissionSync(origin, sub, _random, _currentReferenceTime ?? DateTime.Now, PayMultiplier, PayTable, _placesService, _textCatalog);
            }

            if (sub.Generator == "basic_ferry")
            {
                paxCount = 0;
                luggageWeightLbs = 0;
                cargoWeight = Math.Min(cargoWeight, 50.0);
            }

            double maxDeg = (maxRange / 50.0); 
            var possibleDestinations = await query
                .Where(a => a.ICAO != origin.ICAO)
                .Where(a => a.Latitude >= origin.Latitude - maxDeg && a.Latitude <= origin.Latitude + maxDeg)
                .Where(a => a.Longitude >= origin.Longitude - maxDeg && a.Longitude <= origin.Longitude + maxDeg)
                .OrderBy(a => a.ICAO)
                .Take(500)
                .ToListAsync();

            var validDestinations = possibleDestinations
                .Select(d => new { Airport = d, Dist = CalculateDistance(origin.Latitude, origin.Longitude, d.Latitude, d.Longitude) })
                .Where(x => x.Dist >= minRange && x.Dist <= maxRange)
                .ToList();

            if (sub.Destination.Rule == "short_or_unpaved")
            {
                // MEDEVAC Dinâmico: destino com maior pista <= 4.000 ft ou não pavimentada
                var candIcaos = validDestinations.Select(x => x.Airport.ICAO).ToList();
                var rwys = await _context.Runways
                    .Where(r => candIcaos.Contains(r.AirportIdent))
                    .ToListAsync();
                var rwyByIcao = rwys.GroupBy(r => r.AirportIdent, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                validDestinations = validDestinations.Where(x =>
                {
                    if (rwyByIcao.TryGetValue(x.Airport.ICAO, out var destRwys) && destRwys.Count > 0)
                    {
                        return destRwys.Max(r => r.LengthFt) <= 4000 || destRwys.Any(r => RunwaySurface.IsUnpaved(r.Surface));
                    }
                    return x.Airport.Tier >= 3;
                }).ToList();
            }

            if (!validDestinations.Any()) return null;

            // 3. Priorizar ICAOs (Geral ou via Perfis G20)
            var preferredList = sub.PreferredDestinations.ToList();
            var profile = G20Destinations.GetProfile(origin.Country);
            
            if (profile != null)
            {
                preferredList.AddRange(ResolveG20Hubs(sub.Id, profile.Value));
            }

            // 4. Incorporar Hubs dinâmicos via HubDataService (JSON) de acordo com o perfil de AERODROMOS_PRINCIPAIS.txt
            var dynamicHubs = ResolveDynamicHubs(sub.Id, origin.Country, _hubService);
            if (dynamicHubs.Any())
            {
                preferredList.AddRange(dynamicHubs);
            }

            var preferredCandidates = validDestinations.Where(x => preferredList.Contains(x.Airport.ICAO)).ToList();
            var selected = preferredCandidates.Any() ? preferredCandidates[_random.Next(preferredCandidates.Count)] : validDestinations[_random.Next(validDestinations.Count)];

            string? titleOverride = sub.FixedTexts != null
                ? sub.FixedTexts.Title.Replace("{origin}", origin.ICAO).Replace("{dest}", selected.Airport.ICAO)
                : null;
            string? descOverride = sub.FixedTexts != null
                ? sub.FixedTexts.Description.Replace("{origin}", origin.ICAO).Replace("{dest}", selected.Airport.ICAO)
                : null;
            var destRunway = await GetDestinationRunwayAsync(selected.Airport.ICAO);

            return await CreateMissionObject(origin, selected.Airport, selected.Dist, sub, paxCount, luggageWeightLbs, cargoWeight,
                requiredTouchAndGoes: 0, overflyWaypoints: null, titleOverride: titleOverride, descriptionOverride: descOverride, destRunway: destRunway);
        }

        private async Task<Runway?> GetDestinationRunwayAsync(string icao)
        {
            if (_runwayCache.TryGetValue(icao, out var cached))
                return cached;

            var rwys = await _context.Runways
                .Where(r => r.AirportIdent == icao)
                .ToListAsync();

            var best = rwys.OrderByDescending(r => r.LengthFt).FirstOrDefault();
            _runwayCache[icao] = best;
            return best;
        }

        private Task<Mission> CreateMissionObject(
            Airport origin, 
            Airport dest, 
            double dist, 
            SubCategoryConfig sub, 
            int paxCount, 
            double luggageWeightLbs, 
            double cargoWeight,
            int requiredTouchAndGoes = 0,
            string? overflyWaypoints = null,
            string? titleOverride = null,
            string? descriptionOverride = null,
            Runway? destRunway = null,
            string? contractData = null)
        {
            var m = CreateMissionObjectSync(
                origin, dest, dist, sub, paxCount, luggageWeightLbs, cargoWeight, 
                _random, _currentReferenceTime ?? DateTime.Now, PayMultiplier, PayTable,
                requiredTouchAndGoes, overflyWaypoints, titleOverride, descriptionOverride, destRunway, contractData);
            return Task.FromResult(m);
        }

        public static Mission CreateMissionObjectSync(
            Airport origin, 
            Airport dest, 
            double dist, 
            SubCategoryConfig sub, 
            int paxCount, 
            double luggageWeightLbs, 
            double cargoWeight, 
            Random random, 
            DateTime referenceTime,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            int requiredTouchAndGoes = 0,
            string? overflyWaypoints = null,
            string? titleOverride = null,
            string? descriptionOverride = null,
            Runway? destRunway = null,
            string? contractData = null,
            MissionTextCatalog? textCatalog = null)
        {
            double totalPayload = (paxCount * 170) + luggageWeightLbs + cargoWeight;

            // Determinar a Fase Regulatória Requerida da Missão (Fases 1 a 7 conforme catálogo e subtipo)
            var requiredPhase = CareerPhaseCatalog.Instance.ResolvePhase(sub.Id, fallback: CareerPhase.Phase1_FlightSchool);
            string categoryType = requiredPhase.GetDisplayName();
            bool? isDifficult = destRunway != null ? IsDifficultRunway(destRunway) : null;

            string title = titleOverride ?? sub.Name;
            string desc = descriptionOverride ?? sub.Description;

            if (titleOverride == null && descriptionOverride == null)
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["origin"] = origin.ICAO,
                    ["dest"] = dest.ICAO,
                    ["pax"] = paxCount.ToString(),
                    ["cargo_lbs"] = Math.Round(cargoWeight, 0).ToString(),
                    ["distance_nm"] = Math.Round(dist, 0).ToString(),
                    ["deadline"] = referenceTime.AddHours((random ?? new Random()).Next(sub.MinExpHours, sub.MaxExpHours + 1)).ToString("dd/MM HH:mm")
                };
                var rendered = (textCatalog ?? MissionTextCatalog.Default).Render(sub.Id, random, values, sub.Name, sub.Description);
                title = rendered.Title;
                desc = rendered.Description;
            }

            return new Mission
            {
                Id = Guid.NewGuid(),
                SubId = sub.Id,
                Title = title,
                Description = desc,
                OriginICAO = origin.ICAO,
                DestinationICAO = dest.ICAO,
                OriginMsfsIdent = origin.MsfsIdent ?? origin.ICAO,
                DestinationMsfsIdent = dest.MsfsIdent ?? dest.ICAO,
                DistanceNM = Math.Round(dist, 0),
                PassengerCount = paxCount,
                LuggageWeightLbs = Math.Round(luggageWeightLbs, 0),
                CargoWeightLbs = Math.Round(cargoWeight, 0),
                PayloadLbs = Math.Round(totalPayload, 0),
                Urgency = sub.Urgency,
                RewardMoney = CalculateReward(dist, totalPayload, sub.Urgency, sub.Id, paxCount, origin.Tier, dest.Tier, random, payMultiplier, payTable, isDifficult),
                ExperienceGain = (int)(dist * 0.5 * sub.Urgency),
                ExpirationTime = referenceTime.AddHours((random ?? new Random()).Next(sub.MinExpHours, sub.MaxExpHours + 1)),
                RequiredPhase = requiredPhase,
                RequiredCategory = (PilotCategory)(int)requiredPhase,
                RequiredTier = (CompanyTier)(int)requiredPhase,
                MissionCategoryType = categoryType,
                RequiredTouchAndGoes = requiredTouchAndGoes,
                OverflyWaypoints = overflyWaypoints,
                ContractData = contractData
            };
        }

        public static Mission CreateLocalInstructionMissionSync(
            Airport origin,
            SubCategoryConfig sub,
            Random random,
            DateTime referenceTime,
            IReadOnlyList<Airport>? nearbyTierAirports = null,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            MissionTextCatalog? textCatalog = null)
        {
            double bestDistToTier = -1.0;
            double areaLat = origin.Latitude;
            double areaLon = origin.Longitude;
            double distToArea = 10.0;
            string dir = "N";

            for (int attempt = 0; attempt < 8; attempt++)
            {
                double headingDeg = random.NextDouble() * 360.0;
                double d = 5.0 + (random.NextDouble() * 10.0);
                double azRad = headingDeg * Math.PI / 180.0;
                double cLat = origin.Latitude + ((d / 60.0) * Math.Cos(azRad));
                double cosLat = Math.Max(0.01, Math.Cos(origin.Latitude * Math.PI / 180.0));
                double cLon = origin.Longitude + ((d / (60.0 * cosLat)) * Math.Sin(azRad));

                double minDist = double.MaxValue;
                if (nearbyTierAirports != null && nearbyTierAirports.Count > 0)
                {
                    for (int j = 0; j < nearbyTierAirports.Count; j++)
                    {
                        var a = nearbyTierAirports[j];
                        double distToA = CalculateDistance(cLat, cLon, a.Latitude, a.Longitude);
                        if (distToA < minDist) minDist = distToA;
                    }
                }
                else
                {
                    minDist = 100.0;
                }

                if (minDist >= 5.0 || minDist > bestDistToTier)
                {
                    bestDistToTier = minDist;
                    areaLat = cLat;
                    areaLon = cLon;
                    distToArea = d;
                    dir = GetCardinalDirection(headingDeg);
                    if (minDist >= 5.0) break;
                }
            }

            int altBase = 2500 + (random.Next(0, 3) * 500);
            int altTop = Math.Min(4500, altBase + 1000 + (random.Next(0, 2) * 500));
            string altBand = $"{altBase:N0} a {altTop:N0} ft AGL";

            string areaIdent = $"Área de instrução {dir}";
            string overflyWp = OverflyWaypointCodec.FormatCoordinatePoint(areaIdent, areaLat, areaLon, 3.0, 6000.0);

            int tgl = random.Next(1, 4);
            double totalDist = Math.Round((2.0 * distToArea) + 10.0, 0);
            string maneuvers = PickManeuvers(random);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["origin"] = origin.ICAO,
                ["dest"] = origin.ICAO,
                ["area_dir"] = dir,
                ["area_nm"] = distToArea.ToString("F0"),
                ["alt_band"] = altBand,
                ["maneuvers"] = maneuvers,
                ["tgl"] = tgl.ToString(),
                ["distance_nm"] = totalDist.ToString("F0")
            };

            string fallbackTitle = $"Instrução Local: Setor {dir} ({tgl} TGL)";
            string fallbackDesc = $"Voo de instrução no setor {dir} a {distToArea:F0} NM ({altBand}). Pratique {maneuvers}. Retorne para {tgl} TGL(s) e pouso completo em {origin.ICAO}.";

            var catalog = textCatalog ?? MissionTextCatalog.Default;
            var rendered = catalog.Render(sub.Id, random, values, fallbackTitle, fallbackDesc);

            return CreateMissionObjectSync(
                origin, origin, totalDist, sub, 1, 0, 0,
                random, referenceTime, payMultiplier, payTable,
                requiredTouchAndGoes: tgl,
                overflyWaypoints: overflyWp,
                titleOverride: rendered.Title,
                descriptionOverride: rendered.Description);
        }

        public static Mission CreateCheckrideMissionSync(
            Airport origin,
            SubCategoryConfig sub,
            Random random,
            DateTime referenceTime,
            IReadOnlyList<Airport>? nearbyTierAirports = null,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            MissionTextCatalog? textCatalog = null)
        {
            double bestDistToTier = -1.0;
            double areaLat = origin.Latitude;
            double areaLon = origin.Longitude;
            double distToArea = 10.0;
            string dir = "N";

            for (int attempt = 0; attempt < 8; attempt++)
            {
                double headingDeg = random.NextDouble() * 360.0;
                double d = 5.0 + (random.NextDouble() * 10.0);
                double azRad = headingDeg * Math.PI / 180.0;
                double cLat = origin.Latitude + ((d / 60.0) * Math.Cos(azRad));
                double cosLat = Math.Max(0.01, Math.Cos(origin.Latitude * Math.PI / 180.0));
                double cLon = origin.Longitude + ((d / (60.0 * cosLat)) * Math.Sin(azRad));

                double minDist = double.MaxValue;
                if (nearbyTierAirports != null && nearbyTierAirports.Count > 0)
                {
                    for (int j = 0; j < nearbyTierAirports.Count; j++)
                    {
                        var a = nearbyTierAirports[j];
                        double distToA = CalculateDistance(cLat, cLon, a.Latitude, a.Longitude);
                        if (distToA < minDist) minDist = distToA;
                    }
                }
                else
                {
                    minDist = 100.0;
                }

                if (minDist >= 5.0 || minDist > bestDistToTier)
                {
                    bestDistToTier = minDist;
                    areaLat = cLat;
                    areaLon = cLon;
                    distToArea = d;
                    dir = GetCardinalDirection(headingDeg);
                    if (minDist >= 5.0) break;
                }
            }

            int altBase = 2500 + (random.Next(0, 3) * 500);
            int altTop = Math.Min(4500, altBase + 1000 + (random.Next(0, 2) * 500));
            string altBand = $"{altBase:N0} a {altTop:N0} ft AGL";

            string areaIdent = $"Área de instrução {dir}";
            string overflyWp = OverflyWaypointCodec.FormatCoordinatePoint(areaIdent, areaLat, areaLon, 3.0, 6000.0);

            int tgl = 2;
            double totalDist = Math.Round((2.0 * distToArea) + 10.0, 0);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["origin"] = origin.ICAO,
                ["dest"] = origin.ICAO,
                ["pax"] = "1",
                ["area_dir"] = dir,
                ["area_nm"] = distToArea.ToString("F0"),
                ["alt_band"] = altBand,
                ["tgl"] = "2",
                ["distance_nm"] = totalDist.ToString("F0")
            };

            string fallbackTitle = $"Voo de Cheque Prático (2 TGL)";
            string fallbackDesc = $"Avaliação prática de voo com examinador a bordo em {origin.ICAO}. Área no setor {dir} a {distToArea:F0} NM ({altBand}). Pane simulada (falha de motor simulada pelo examinador, pouso de precisão) e 2 TGLs com pouso completo na origem.";

            var catalog = textCatalog ?? MissionTextCatalog.Default;
            var rendered = catalog.Render(sub.Id, random, values, fallbackTitle, fallbackDesc);

            return CreateMissionObjectSync(
                origin, origin, totalDist, sub, 1, 0, 0,
                random, referenceTime, payMultiplier, payTable,
                requiredTouchAndGoes: tgl,
                overflyWaypoints: overflyWp,
                titleOverride: rendered.Title,
                descriptionOverride: rendered.Description);
        }

        public static Mission? CreateScenicTourMissionSync(
            Airport origin,
            SubCategoryConfig sub,
            Random random,
            DateTime referenceTime,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            IPlacesDataService? placesService = null,
            MissionTextCatalog? textCatalog = null)
        {
            var places = placesService ?? new PlacesDataService();
            var scenicAerodrome = places.GetScenicAerodrome(origin.ICAO);

            if (scenicAerodrome != null && scenicAerodrome.Points.Count >= 2)
            {
                int targetCount = Math.Min(scenicAerodrome.Points.Count, random.Next(2, 5));
                var picked = new List<ScenicPoint>();

                for (int setAttempt = 0; setAttempt < 10 && picked.Count < 2; setAttempt++)
                {
                    var pool = new List<ScenicPoint>(scenicAerodrome.Points);
                    picked.Clear();
                    int attempts = 0;
                    while (picked.Count < targetCount && pool.Count > 0 && attempts < 30)
                    {
                        attempts++;
                        int idx = random.Next(pool.Count);
                        var candidate = pool[idx];
                        pool.RemoveAt(idx);

                        bool tooClose = false;
                        for (int j = 0; j < picked.Count; j++)
                        {
                            double d = CalculateDistance(candidate.Lat, candidate.Lon, picked[j].Lat, picked[j].Lon);
                            if (d < 2.0)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            picked.Add(candidate);
                        }
                    }
                }

                if (picked.Count < 2 && scenicAerodrome.Points.Count >= 2)
                {
                    // Fallback de maior separação se a malha de pontos for densa (< 2 NM entre pares sorteados)
                    ScenicPoint bestA = scenicAerodrome.Points[0];
                    ScenicPoint bestB = scenicAerodrome.Points[1];
                    double maxD = 0;
                    for (int i = 0; i < scenicAerodrome.Points.Count; i++)
                    {
                        for (int j = i + 1; j < scenicAerodrome.Points.Count; j++)
                        {
                            double d = CalculateDistance(scenicAerodrome.Points[i].Lat, scenicAerodrome.Points[i].Lon,
                                                         scenicAerodrome.Points[j].Lat, scenicAerodrome.Points[j].Lon);
                            if (d > maxD)
                            {
                                maxD = d;
                                bestA = scenicAerodrome.Points[i];
                                bestB = scenicAerodrome.Points[j];
                            }
                        }
                    }
                    picked = new List<ScenicPoint> { bestA, bestB };
                }

                if (picked.Count < 2) return null;

                picked = picked.OrderBy(p => Math.Atan2(p.Lat - origin.Latitude, p.Lon - origin.Longitude)).ToList();

                double totalTourDist = CalculateDistance(origin.Latitude, origin.Longitude, picked[0].Lat, picked[0].Lon);
                for (int i = 0; i < picked.Count - 1; i++)
                {
                    totalTourDist += CalculateDistance(picked[i].Lat, picked[i].Lon, picked[i + 1].Lat, picked[i + 1].Lon);
                }
                totalTourDist += CalculateDistance(picked[^1].Lat, picked[^1].Lon, origin.Latitude, origin.Longitude);
                totalTourDist = Math.Round(totalTourDist, 0);

                var wpItems = picked.Select(p => $"P:{OverflyWaypointCodec.SanitizeName(p.Name)}@{p.Lat.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)},{p.Lon.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)},1.5,3000");
                string wpString = string.Join(";", wpItems);

                var poiNames = picked.Select(p => p.Name).ToList();
                string poiListStr = poiNames.Count == 2
                    ? $"{poiNames[0]} e {poiNames[1]}"
                    : $"{string.Join(", ", poiNames.Take(poiNames.Count - 1))} e {poiNames[^1]}";

                int tourPax = random.Next(1, 4);

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["origin"] = origin.ICAO,
                    ["dest"] = origin.ICAO,
                    ["poi_list"] = poiListStr,
                    ["pax"] = tourPax.ToString(),
                    ["distance_nm"] = totalTourDist.ToString("F0"),
                    ["scenic_name"] = scenicAerodrome.Name,
                    ["scenic_note"] = scenicAerodrome.Note
                };

                string fallbackTitle = $"{scenicAerodrome.Name} ({string.Join(" • ", picked.Select(p => p.Name))})";
                string fallbackDesc = $"{scenicAerodrome.Note} Voo panorâmico sobrevoando {poiListStr}. Retorno para pouso em {origin.ICAO} (sobrevoo a ≤ 1,5 NM e abaixo de 3.000 ft AGL).";

                // Variantes com {scenic_name}/{scenic_note} só nos aeródromos turísticos (em outros o título ficaria cortado).
                var catalog = textCatalog ?? MissionTextCatalog.Default;
                var rendered = catalog.Render("voo_panoramico-turistico", random, values, fallbackTitle, fallbackDesc);

                return CreateMissionObjectSync(
                    origin, origin, totalTourDist, sub, tourPax, Math.Min(tourPax * 15.0, 30.0), 0,
                    random, referenceTime, payMultiplier, payTable,
                    requiredTouchAndGoes: 0,
                    overflyWaypoints: wpString,
                    titleOverride: rendered.Title,
                    descriptionOverride: rendered.Description);
            }

            var pois = places.FindPois(origin.Latitude, origin.Longitude, 5.0, 30.0);
            if (pois.Count < 2)
            {
                pois = places.FindPois(origin.Latitude, origin.Longitude, 5.0, 45.0);
            }

            if (pois.Count < 2) return null;

            int targetPoiCount = Math.Min(pois.Count, random.Next(2, 4));
            var poolLegacy = new List<TouristPoiInfo>(pois);
            var pickedLegacy = new List<TouristPoiInfo>();

            int attemptsLegacy = 0;
            while (pickedLegacy.Count < targetPoiCount && poolLegacy.Count > 0 && attemptsLegacy < 30)
            {
                attemptsLegacy++;
                var candidate = PickWeightedPoi(poolLegacy, random);
                if (candidate == null) break;
                poolLegacy.Remove(candidate);

                bool tooClose = false;
                for (int j = 0; j < pickedLegacy.Count; j++)
                {
                    double d = CalculateDistance(candidate.Lat, candidate.Lon, pickedLegacy[j].Lat, pickedLegacy[j].Lon);
                    if (d < 3.0)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    pickedLegacy.Add(candidate);
                }
            }

            if (pickedLegacy.Count < 2) return null;

            pickedLegacy = pickedLegacy.OrderBy(p => Math.Atan2(p.Lat - origin.Latitude, p.Lon - origin.Longitude)).ToList();

            double totalTourDistLegacy = CalculateDistance(origin.Latitude, origin.Longitude, pickedLegacy[0].Lat, pickedLegacy[0].Lon);
            for (int i = 0; i < pickedLegacy.Count - 1; i++)
            {
                totalTourDistLegacy += CalculateDistance(pickedLegacy[i].Lat, pickedLegacy[i].Lon, pickedLegacy[i + 1].Lat, pickedLegacy[i + 1].Lon);
            }
            totalTourDistLegacy += CalculateDistance(pickedLegacy[^1].Lat, pickedLegacy[^1].Lon, origin.Latitude, origin.Longitude);
            totalTourDistLegacy = Math.Round(totalTourDistLegacy, 0);

            var wpItemsLegacy = pickedLegacy.Select(p => $"P:{OverflyWaypointCodec.SanitizeName(p.Name)}@{p.Lat.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)},{p.Lon.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)},1.5,3000");
            string wpStringLegacy = string.Join(";", wpItemsLegacy);

            var poiNamesFormatted = pickedLegacy.Select(FormatPoiWithKind).ToList();
            string poiListStrLegacy = poiNamesFormatted.Count == 2
                ? $"{poiNamesFormatted[0]} e {poiNamesFormatted[1]}"
                : $"{string.Join(", ", poiNamesFormatted.Take(poiNamesFormatted.Count - 1))} e {poiNamesFormatted[^1]}";

            int tourPaxLegacy = random.Next(1, 4);

            var valuesLegacy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["origin"] = origin.ICAO,
                ["dest"] = origin.ICAO,
                ["poi_list"] = poiListStrLegacy,
                ["pax"] = tourPaxLegacy.ToString(),
                ["distance_nm"] = totalTourDistLegacy.ToString("F0")
            };

            string fallbackTitleLegacy = $"Voo Panorâmico ({string.Join(" • ", pickedLegacy.Select(p => p.Name))})";
            string fallbackDescLegacy = $"Voo panorâmico sobrevoando {poiListStrLegacy}. Retorno para pouso em {origin.ICAO} (sobrevoo a ≤ 1,5 NM e abaixo de 3.000 ft AGL).";

            var catalogLegacy = textCatalog ?? MissionTextCatalog.Default;
            var renderedLegacy = catalogLegacy.Render(sub.Id, random, valuesLegacy, fallbackTitleLegacy, fallbackDescLegacy);

            return CreateMissionObjectSync(
                origin, origin, totalTourDistLegacy, sub, tourPaxLegacy, Math.Min(tourPaxLegacy * 15.0, 30.0), 0,
                random, referenceTime, payMultiplier, payTable,
                requiredTouchAndGoes: 0,
                overflyWaypoints: wpStringLegacy,
                titleOverride: renderedLegacy.Title,
                descriptionOverride: renderedLegacy.Description);
        }

        public static Mission CreateSurveyMissionSync(
            Airport origin,
            SubCategoryConfig sub,
            Random random,
            DateTime referenceTime,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            IPlacesDataService? placesService = null,
            MissionTextCatalog? textCatalog = null)
        {
            var places = placesService ?? new PlacesDataService();
            var eligibleKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "barragem", "porto", "ponte", "ilha", "lago", "estadio", "parque_nacional", "praia", "reserva_natural"
            };
            var pois = places.FindPois(origin.Latitude, origin.Longitude, 5.0, 25.0, eligibleKinds);

            double centerLat;
            double centerLon;
            double distToCenter;
            string targetName;
            string clientName;

            if (pois.Count > 0)
            {
                var poi = PickWeightedPoi(pois, random) ?? pois[0];
                centerLat = poi.Lat;
                centerLon = poi.Lon;
                distToCenter = CalculateDistance(origin.Latitude, origin.Longitude, centerLat, centerLon);
                targetName = FormatPoiWithKind(poi);
                clientName = GetClientNameForKind(poi.Kind);
            }
            else
            {
                distToCenter = 5.0 + (random.NextDouble() * 20.0);
                double azRad = random.NextDouble() * 2.0 * Math.PI;
                centerLat = origin.Latitude + ((distToCenter / 60.0) * Math.Cos(azRad));
                double cosOriginLat = Math.Max(0.01, Math.Cos(origin.Latitude * Math.PI / 180.0));
                centerLon = origin.Longitude + ((distToCenter / (60.0 * cosOriginLat)) * Math.Sin(azRad));
                targetName = $"área demarcada a {distToCenter:F0} NM de {origin.ICAO}";
                clientName = "empresa de geoprocessamento";
            }

            double widthNM = Math.Round(2.0 + (random.NextDouble() * 2.0), 1);
            double lengthNM = Math.Round(2.0 + (random.NextDouble() * 2.0), 1);
            double orientationDeg = random.Next(0, 36) * 10.0;
            int requiredLegs = random.Next(3, 6);

            double minAlt = Math.Max(2000.0, origin.ElevationFt + 1000.0);
            double maxAlt = Math.Max(minAlt + 500.0, 6000.0);
            double targetAltitudeFt = Math.Round((minAlt + (random.NextDouble() * (maxAlt - minAlt))) / 100.0) * 100.0;
            double targetSpeedKt = 80.0 + (random.Next(0, 4) * 5.0);

            double estimatedDist = (2.0 * distToCenter) + (requiredLegs * lengthNM) + ((requiredLegs - 1) * (widthNM / requiredLegs));
            estimatedDist = Math.Round(estimatedDist, 0);

            var surveyData = new SurveyContractData
            {
                CenterLatitude = Math.Round(centerLat, 4),
                CenterLongitude = Math.Round(centerLon, 4),
                WidthNM = widthNM,
                LengthNM = lengthNM,
                OrientationDeg = orientationDeg,
                RequiredLegs = requiredLegs,
                TargetAltitudeFt = targetAltitudeFt,
                TargetSpeedKt = targetSpeedKt
            };
            string contractJson = System.Text.Json.JsonSerializer.Serialize(surveyData);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["origin"] = origin.ICAO,
                ["dest"] = origin.ICAO,
                ["target"] = targetName,
                ["client"] = clientName,
                ["legs"] = requiredLegs.ToString(),
                ["alt"] = targetAltitudeFt.ToString("F0"),
                ["speed"] = targetSpeedKt.ToString("F0"),
                ["heading"] = ((int)orientationDeg).ToString("D3"),
                ["distance_nm"] = estimatedDist.ToString("F0")
            };

            string fallbackTitle = $"Levantamento Aéreo ({requiredLegs} pernas • {targetAltitudeFt:F0} ft)";
            string fallbackDesc = $"Levantamento fotográfico para {clientName} sobre {targetName}. Exige {requiredLegs} pernas a {targetAltitudeFt:F0} ft e {targetSpeedKt:F0} kt, rumo {(int)orientationDeg:D3}°. Retorno para pouso em {origin.ICAO}.";

            var catalog = textCatalog ?? MissionTextCatalog.Default;
            var rendered = catalog.Render(sub.Id, random, values, fallbackTitle, fallbackDesc);

            return CreateMissionObjectSync(
                origin, origin, estimatedDist, sub, 0, 0, 80.0,
                random, referenceTime, payMultiplier, payTable,
                requiredTouchAndGoes: 0,
                overflyWaypoints: null,
                titleOverride: rendered.Title,
                descriptionOverride: rendered.Description,
                destRunway: null,
                contractData: contractJson);
        }

        public static Mission CreateSkydiveMissionSync(
            Airport origin,
            SubCategoryConfig sub,
            Random random,
            DateTime referenceTime,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            IPlacesDataService? placesService = null,
            MissionTextCatalog? textCatalog = null,
            string? engineClass = null)
        {
            var places = placesService ?? new PlacesDataService();
            var dz = places.GetDropZone(origin.ICAO);

            double dropLat = dz != null ? dz.Lat : origin.Latitude;
            double dropLon = dz != null ? dz.Lon : origin.Longitude;
            string dzName = dz != null ? dz.Name : origin.ICAO;

            bool isTurboprop = engineClass != null && engineClass.Contains("turboprop", StringComparison.OrdinalIgnoreCase);
            double targetAglFt = isTurboprop
                ? 12000.0 + (random.Next(0, 5) * 500.0)
                : 10000.0 + (random.Next(0, 5) * 500.0);

            int paxCount = random.Next(4, 11);
            double maxDropSpeedKt = 90.0;
            double estimatedDist = 25.0;

            var skydiveData = new SkydiveContractData
            {
                DropZoneLatitude = dropLat,
                DropZoneLongitude = dropLon,
                DropZoneElevationFt = origin.ElevationFt,
                TargetAltitudeAglFt = targetAglFt,
                MaxDropSpeedKt = maxDropSpeedKt
            };
            string contractJson = System.Text.Json.JsonSerializer.Serialize(skydiveData);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["origin"] = origin.ICAO,
                ["dest"] = origin.ICAO,
                ["dz"] = dzName,
                ["dz_note"] = dz?.Note ?? string.Empty,
                ["alt"] = targetAglFt.ToString("N0"),
                ["speed"] = maxDropSpeedKt.ToString("F0"),
                ["pax"] = paxCount.ToString(),
                ["distance_nm"] = estimatedDist.ToString("F0")
            };

            string fallbackTitle = $"Lançamento de Paraquedistas ({paxCount} saltadores)";
            string fallbackDesc = $"Lançamento de {paxCount} paraquedistas sobre {dzName}. Subida para salto a {targetAglFt:N0} ft AGL sobre a zona (≤ 2 NM), estabilização a velocidade segura (≤ {maxDropSpeedKt:F0} kt) por ≥ 30 s e descida sem choque térmico. Retorno para pouso em {origin.ICAO}.";

            var catalog = textCatalog ?? MissionTextCatalog.Default;
            var rendered = catalog.Render(sub.Id, random, values, fallbackTitle, fallbackDesc);

            return CreateMissionObjectSync(
                origin, origin, estimatedDist, sub, paxCount, paxCount * 25.0, 0.0,
                random, referenceTime, payMultiplier, payTable,
                requiredTouchAndGoes: 0,
                overflyWaypoints: null,
                titleOverride: rendered.Title,
                descriptionOverride: rendered.Description,
                destRunway: null,
                contractData: contractJson);
        }

        public static string GetCardinalDirection(double headingDeg)
        {
            headingDeg = (headingDeg % 360.0 + 360.0) % 360.0;
            if (headingDeg >= 337.5 || headingDeg < 22.5) return "N";
            if (headingDeg < 67.5) return "NE";
            if (headingDeg < 112.5) return "E";
            if (headingDeg < 157.5) return "SE";
            if (headingDeg < 202.5) return "S";
            if (headingDeg < 247.5) return "SW";
            if (headingDeg < 292.5) return "W";
            return "NW";
        }

        private static readonly string[] s_instructionManeuvers =
        {
            "curvas de grande inclinação 45°",
            "estol com e sem motor",
            "voo lento",
            "planeio para pouso simulado",
            "curvas em S sobre referência"
        };

        public static string PickManeuvers(Random random)
        {
            var shuffled = s_instructionManeuvers.OrderBy(_ => random.Next()).ToList();
            int count = random.Next(2, 5);
            var picked = shuffled.Take(count).ToList();
            if (picked.Count == 2)
                return $"{picked[0]} e {picked[1]}";
            return $"{string.Join(", ", picked.Take(picked.Count - 1))} e {picked[^1]}";
        }

        public static string FormatPoiWithKind(TouristPoiInfo poi)
        {
            return poi.Kind.ToLowerInvariant() switch
            {
                "cachoeira" => $"a cachoeira {poi.Name}",
                "farol" => $"o farol {poi.Name}",
                "ponte" => $"a ponte {poi.Name}",
                "barragem" => $"a barragem {poi.Name}",
                "parque_nacional" => $"o parque nacional {poi.Name}",
                "reserva_natural" => $"a reserva natural {poi.Name}",
                "ilha" => $"a ilha {poi.Name}",
                "lago" => $"o lago {poi.Name}",
                "estadio" => $"o estádio {poi.Name}",
                "castelo" => $"o castelo {poi.Name}",
                "montanha" or "pico" => $"o pico {poi.Name}",
                "praia" => $"a praia {poi.Name}",
                _ => poi.Name
            };
        }

        public static string GetClientNameForKind(string kind)
        {
            return kind.ToLowerInvariant() switch
            {
                "barragem" or "lago" => "concessionária de energia e saneamento",
                "porto" or "ponte" => "autoridade portuária e de infraestrutura de transportes",
                "parque_nacional" or "reserva_natural" => "instituto ambiental",
                "ilha" or "praia" => "secretaria municipal de turismo",
                "estadio" => "empresa de engenharia e construtora",
                _ => "empresa de engenharia e geoprocessamento"
            };
        }

        private static TouristPoiInfo? PickWeightedPoi(IReadOnlyList<TouristPoiInfo> pois, Random random)
        {
            if (pois == null || pois.Count == 0) return null;
            double totalWeight = 0;
            for (int i = 0; i < pois.Count; i++) totalWeight += Math.Max(1, pois[i].Popularity);
            double roll = random.NextDouble() * totalWeight;
            double running = 0;
            for (int i = 0; i < pois.Count; i++)
            {
                running += Math.Max(1, pois[i].Popularity);
                if (roll <= running) return pois[i];
            }
            return pois[^1];
        }

        public static IReadOnlyList<string> ResolveG20Hubs(string subId, DestinationProfile profile)
        {
            subId = FlightCareer.Domain.Missions.MissionCatalog.Shared.Normalize(subId);
            return subId switch
            {
                "linha_domestica_hub" or "logistica_apoio_crew_parts" or "diplomatico_militar" or "malotes_documentos" => profile.DomesticHubs ?? new List<string>(),
                "linha_regional_internacional" or "carga_heavy_internacional" => profile.RegionalIntl ?? new List<string>(),
                "linha_long_haul" or "carga_heavy_long_haul" => profile.LongHaul ?? new List<string>(),

                "carga_heavy_nacional" => profile.CargoHubs ?? new List<string>(),
                "executiva_business" => profile.ExecutiveHubs ?? new List<string>(),
                "translado_posicionamento_ferry" or "manutencao_regional" => profile.MaintenanceHubs ?? new List<string>(),

                "alimentacao_hub" => profile.FeederHubs ?? new List<string>(),
                "regional_direto" or "executivo_regional" or "equipes_urgentes" or "navegacao_cross_country" or "transporte_patrao" => profile.RegionalPoints ?? new List<string>(),

                "agro_legacy" or "aog_agro_suporte" or "agro_suprimento_campanha" or "abastecimento_rancho" or "troca_equipe" or "apoio_batedor" or "translado_piloto_agricola" or "insumos_urgentes_agro" or "aog_maquina_campo" => profile.AgroPoints ?? new List<string>(),
                "agro_tech" or "agro_insumos_biotecnologia" => profile.AgroTechPoints ?? new List<string>(),
                "agro_investidores" => profile.InvestPoints ?? new List<string>(),

                "ecommerce_varejo" => profile.EcommercePoints ?? new List<string>(),
                "agro_pecas_insumos" => profile.AgroPartsPoints ?? new List<string>(),
                "governo_local_estadual" => profile.GovPoints ?? new List<string>(),
                "familia_alto_padrao" => profile.LeisurePoints ?? new List<string>(),

                "remocao_urgencia" or "transferencia_eletiva" or "transporte_orgaos" or "medivac_bush" => profile.HealthHubs ?? new List<string>(),
                "instrucao_local" or "voo_de_cheque" => profile.InstructionHubs ?? new List<string>(),

                _ => Array.Empty<string>()
            };
        }

        public static IReadOnlyList<string> ResolveDynamicHubs(string subId, string country, IHubDataService hubService)
        {
            if (hubService == null) return Array.Empty<string>();
            subId = FlightCareer.Domain.Missions.MissionCatalog.Shared.Normalize(subId);
            return subId switch
            {
                "linha_domestica_hub" => hubService.GetHubs(HubType.CommercialPax, country),
                "linha_regional_internacional" => hubService.GetHubs(HubType.CommercialPax, "GLOBAL"),
                "carga_heavy_nacional" => hubService.GetHubs(HubType.Cargo, country),
                "carga_heavy_internacional" => hubService.GetHubs(HubType.Cargo, "GLOBAL"),
                "executiva_business" => hubService.GetHubs(HubType.Executive, country),
                "translado_posicionamento_ferry" or "manutencao_regional" => hubService.GetHubs(HubType.Maintenance, country),
                "alimentacao_hub" => hubService.GetHubs(HubType.Feeder, country).Concat(hubService.GetHubs(HubType.CommercialPax, country)).ToList(),
                "regional_direto" => hubService.GetHubs(HubType.Feeder, country),
                "agro_legacy" or "aog_agro_suporte" or "abastecimento_rancho" or "apoio_batedor" or "translado_piloto_agricola" or "insumos_urgentes_agro" => hubService.GetHubs(HubType.Agro, country),
                "ecommerce_varejo" or "agro_pecas_insumos" => hubService.GetHubs(HubType.Cargo, country),
                "executivo_regional" or "familia_alto_padrao" => hubService.GetHubs(HubType.Executive, country),
                "remocao_urgencia" or "transferencia_eletiva" or "transporte_orgaos" or "medivac_bush" => hubService.GetHubs(HubType.Health, country),
                "instrucao_local" or "navegacao_cross_country" or "voo_de_cheque" => hubService.GetHubs(HubType.Instruction, country),
                _ => (IReadOnlyList<string>)Array.Empty<string>()
            };
        }

        public static List<Mission> GenerateMissionsInMemory(
            Airport currentAirport,
            IReadOnlyList<Airport> allAirports,
            IHubDataService? hubService,
            Random random,
            DateTime referenceTime,
            int? countOverride = null,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            IReadOnlyList<Runway>? allRunways = null,
            IPlacesDataService? placesService = null,
            MissionTextCatalog? textCatalog = null)
        {
            if (currentAirport == null) return new List<Mission>();
            if (!MissionRules.Rules.TryGetValue(currentAirport.Tier, out var tierConfig))
                return new List<Mission>();

            hubService ??= new HubDataService();
            int targetCount = countOverride ?? currentAirport.Tier switch
            {
                1 => 200,
                2 => 120,
                3 => 60,
                4 => 25,
                _ => 25
            };

            Dictionary<string, Runway?> runwaysByIcao;
            if (allRunways != null && allRunways.Count > 0)
            {
                runwaysByIcao = allRunways
                    .GroupBy(r => r.AirportIdent, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(r => !RunwaySurface.IsUnpaved(r.Surface)).ThenByDescending(r => r.LengthFt).FirstOrDefault(),
                        StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                runwaysByIcao = new Dictionary<string, Runway?>(StringComparer.OrdinalIgnoreCase);
            }

            var originRunway = runwaysByIcao.GetValueOrDefault(currentAirport.ICAO);

            var candidates = allAirports
                .Select(a => new AirportCandidate(a, runwaysByIcao.GetValueOrDefault(a.ICAO)))
                .ToList();

            var originTags = MissionBoardCore.AugmentOriginTags(currentAirport, AirportProfileCatalog.Shared.TagsFor(currentAirport.ICAO), placesService);

            return MissionBoardCore.GenerateBoard(
                currentAirport,
                candidates,
                originTags,
                icao => AirportProfileCatalog.Shared.TagsFor(icao),
                MissionCatalog.Shared,
                random,
                referenceTime,
                targetCount,
                payMultiplier,
                payTable,
                textCatalog,
                placesService,
                originRunway);
        }

        private static Mission? CreateMissionFromSubCategoryInMemory(
            Airport origin,
            SubCategoryConfig sub,
            List<int> allowedTiers,
            IReadOnlyList<Airport> allAirports,
            IHubDataService hubService,
            Random random,
            DateTime referenceTime,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            IReadOnlyList<Runway>? allRunways = null,
            IPlacesDataService? placesService = null,
            MissionTextCatalog? textCatalog = null)
        {
            var rangeRule = PickWeighted(sub.RangeRules, random);
            var paxRule = PickWeighted(sub.PaxRules, random);
            var lbsRule = sub.LbsRules.Any() ? PickWeighted(sub.LbsRules, random) : null;

            if (rangeRule == null) return null;

            int minRange = rangeRule.Min;
            int maxRange = rangeRule.Max;
            int paxCount = paxRule != null ? random.Next(paxRule.Min, paxRule.Max + 1) : 0;
            double luggageWeightLbs = paxCount > 0 ? paxCount * random.Next(0, 51) : 0;
            double cargoWeight = lbsRule != null ? random.Next(lbsRule.Min, lbsRule.Max + 1) : 0;

            var nearbyTierAirports = allAirports.Where(a => a.Tier <= 2).ToList();

            if (sub.Generator == "local_instruction")
            {
                return CreateLocalInstructionMissionSync(
                    origin, sub, random, referenceTime,
                    nearbyTierAirports, payMultiplier, payTable, textCatalog);
            }

            if (sub.Generator == "checkride")
            {
                return CreateCheckrideMissionSync(
                    origin, sub, random, referenceTime,
                    nearbyTierAirports, payMultiplier, payTable, textCatalog);
            }

            if (sub.Generator == "scenic_tour")
            {
                var tourMission = CreateScenicTourMissionSync(
                    origin, sub, random, referenceTime,
                    payMultiplier, payTable, placesService, textCatalog);
                if (tourMission != null) return tourMission;

                // Voo panorâmico: 2 a 3 aeródromos a 8-30 NM da origem, percurso total, retorno à origem
                var validWps = new List<(Airport Airport, double Dist)>();
                for (int i = 0; i < allAirports.Count; i++)
                {
                    var a = allAirports[i];
                    if (string.Equals(a.ICAO, origin.ICAO, StringComparison.OrdinalIgnoreCase)) continue;
                    double dist = CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude);
                    if (dist >= 8.0 && dist <= 30.0)
                    {
                        validWps.Add((a, dist));
                    }
                }

                if (validWps.Count < 2)
                {
                    for (int i = 0; i < allAirports.Count; i++)
                    {
                        var a = allAirports[i];
                        if (string.Equals(a.ICAO, origin.ICAO, StringComparison.OrdinalIgnoreCase)) continue;
                        double dist = CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude);
                        if (dist >= 5.0 && dist <= 45.0 && !validWps.Any(x => x.Airport.ICAO == a.ICAO))
                        {
                            validWps.Add((a, dist));
                        }
                    }
                }

                if (validWps.Count < 2) return null;

                int pointsCount = Math.Min(validWps.Count, random.Next(2, 4));
                var picked = validWps.OrderBy(_ => random.Next()).Take(pointsCount).Select(x => x.Airport).ToList();
                // Ordenação por azimute angular em relação à origem para rota circular contínua
                picked = picked.OrderBy(a => Math.Atan2(a.Latitude - origin.Latitude, a.Longitude - origin.Longitude)).ToList();

                double totalTourDist = CalculateDistance(origin.Latitude, origin.Longitude, picked[0].Latitude, picked[0].Longitude);
                for (int i = 0; i < picked.Count - 1; i++)
                {
                    totalTourDist += CalculateDistance(picked[i].Latitude, picked[i].Longitude, picked[i + 1].Latitude, picked[i + 1].Longitude);
                }
                totalTourDist += CalculateDistance(picked[^1].Latitude, picked[^1].Longitude, origin.Latitude, origin.Longitude);

                string wpString = string.Join(";", picked.Select(p => p.ICAO));
                int tourPax = Math.Clamp(paxCount > 0 ? paxCount : random.Next(1, 4), 1, 3);
                string title = $"Voo Panorâmico ({string.Join(" • ", picked.Select(p => p.ICAO))})";
                string desc = $"Voo panorâmico sobrevoando os aeródromos da região: {string.Join(", ", picked.Select(p => p.ICAO))}. Retorno para pouso em {origin.ICAO} (sobrevoo a ≤ 2 NM e abaixo de 3.000 ft AGL).";

                return CreateMissionObjectSync(origin, origin, totalTourDist, sub, tourPax, Math.Min(luggageWeightLbs, 30.0), 0,
                    random, referenceTime, payMultiplier, payTable,
                    requiredTouchAndGoes: 0, overflyWaypoints: wpString, titleOverride: title, descriptionOverride: desc);
            }

            if (sub.Generator == "tgl_circuit")
            {
                int tglCount = random.Next(3, 7);
                int tglPax = random.Next(0, 2);
                double tglDist = tglCount * 4.0;
                string title = $"Circuito de Tráfego ({tglCount} TGLs)";
                string desc = $"Treinamento em circuito de tráfego padrão em {origin.ICAO}. Exige {tglCount} toques e arremetidas (TGL) seguidos de pouso final completo.";

                return CreateMissionObjectSync(origin, origin, tglDist, sub, tglPax, 0, 0,
                    random, referenceTime, payMultiplier, payTable,
                    requiredTouchAndGoes: tglCount, overflyWaypoints: null, titleOverride: title, descriptionOverride: desc);
            }

            if (sub.Generator == "survey")
            {
                return CreateSurveyMissionSync(origin, sub, random, referenceTime, payMultiplier, payTable, placesService, textCatalog);
            }

            if (sub.Generator == "skydive")
            {
                return CreateSkydiveMissionSync(origin, sub, random, referenceTime, payMultiplier, payTable, placesService, textCatalog);
            }

            if (sub.Generator == "basic_ferry")
            {
                paxCount = 0;
                luggageWeightLbs = 0;
                cargoWeight = Math.Min(cargoWeight, 50.0);
            }

            double maxDeg = maxRange / 50.0;
            var candidates = new List<(Airport Airport, double Dist)>();

            for (int i = 0; i < allAirports.Count; i++)
            {
                var a = allAirports[i];
                if (string.Equals(a.ICAO, origin.ICAO, StringComparison.OrdinalIgnoreCase)) continue;
                if (!allowedTiers.Contains(a.Tier)) continue;
                if (sub.RequiresHardSurface && a.Tier > 3) continue;
                if (sub.MinRunwayDest >= 7000 && a.Tier != 1) continue;
                else if (sub.MinRunwayDest >= 4500 && a.Tier > 2) continue;
                else if (sub.MinRunwayDest >= 2500 && a.Tier > 3) continue;

                // Filtros específicos por contrato das Fases 3 a 7
                switch (sub.Destination.Rule)
                {
                    case "same_country_tier_3_4":
                        if (!string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase) || (a.Tier != 3 && a.Tier != 4)) continue;
                        break;
                    case "other_country":
                        if (string.IsNullOrEmpty(a.Country) || string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase)) continue;
                        break;
                    case "tier1_same_country":
                        if (a.Tier != 1 || !string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase)) continue;
                        break;
                    case "tier1":
                        if (a.Tier != 1) continue;
                        break;
                    case "tier1_other_country":
                        if (a.Tier != 1 || string.IsNullOrEmpty(a.Country) || string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase)) continue;
                        break;
                    case "tier_max_2":
                        if (a.Tier > 2) continue;
                        break;
                }

                if (sub.Destination.Rule == "short_or_unpaved")
                {
                    if (allRunways != null)
                    {
                        var destRwys = allRunways.Where(r => string.Equals(r.AirportIdent, a.ICAO, StringComparison.OrdinalIgnoreCase)).ToList();
                        if (destRwys.Count > 0)
                        {
                            bool ok = destRwys.Max(r => r.LengthFt) <= 4000 || destRwys.Any(r => RunwaySurface.IsUnpaved(r.Surface));
                            if (!ok) continue;
                        }
                        else if (a.Tier != 3 && a.Tier != 4) continue;
                    }
                    else if (a.Tier != 3 && a.Tier != 4) continue;
                }

                if (a.Latitude < origin.Latitude - maxDeg || a.Latitude > origin.Latitude + maxDeg) continue;
                if (a.Longitude < origin.Longitude - maxDeg || a.Longitude > origin.Longitude + maxDeg) continue;

                double dist = CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude);
                if (dist >= minRange && dist <= maxRange)
                {
                    candidates.Add((a, dist));
                    if (candidates.Count >= 200) break;
                }
            }

            if (candidates.Count == 0) return null;

            var preferredList = sub.PreferredDestinations.ToList();
            var profile = G20Destinations.GetProfile(origin.Country);
            if (profile != null)
            {
                preferredList.AddRange(ResolveG20Hubs(sub.Id, profile.Value));
            }
            var dynamicHubs = ResolveDynamicHubs(sub.Id, origin.Country, hubService);
            if (dynamicHubs.Any())
            {
                preferredList.AddRange(dynamicHubs);
            }

            var preferredCandidates = candidates.Where(x => preferredList.Contains(x.Airport.ICAO)).ToList();
            var selected = preferredCandidates.Count > 0
                ? preferredCandidates[random.Next(preferredCandidates.Count)]
                : candidates[random.Next(candidates.Count)];

            string? titleOverride = sub.FixedTexts != null
                ? sub.FixedTexts.Title.Replace("{origin}", origin.ICAO).Replace("{dest}", selected.Airport.ICAO)
                : null;
            string? descOverride = sub.FixedTexts != null
                ? sub.FixedTexts.Description.Replace("{origin}", origin.ICAO).Replace("{dest}", selected.Airport.ICAO)
                : null;

            return CreateMissionObjectSync(origin, selected.Airport, selected.Dist, sub, paxCount, luggageWeightLbs, cargoWeight, random, referenceTime, payMultiplier, payTable,
                requiredTouchAndGoes: 0, overflyWaypoints: null, titleOverride: titleOverride, descriptionOverride: descOverride);
        }

        private List<CategoryConfig> GetCalibratedCategories(Airport airport, TierConfig tierConfig)
        {
            return GetCalibratedCategories(airport, tierConfig, _hubService, _placesService);
        }

        public static List<CategoryConfig> GetCalibratedCategories(Airport airport, TierConfig tierConfig, IHubDataService hubService)
        {
            return GetCalibratedCategories(airport, tierConfig, hubService, null);
        }

        public static List<CategoryConfig> GetCalibratedCategories(Airport airport, TierConfig tierConfig, IHubDataService hubService, IPlacesDataService? placesService)
        {
            var baseCats = tierConfig?.Categories ?? (MissionRules.Rules.TryGetValue(airport.Tier, out var tc) ? tc.Categories : new List<CategoryConfig>());
            var result = new List<CategoryConfig>();
            bool isScenic = placesService?.GetScenicAerodrome(airport.ICAO) != null;

            foreach (var c in baseCats)
            {
                var clone = new CategoryConfig
                {
                    Id = c.Id,
                    Name = c.Name,
                    Weight = c.Weight,
                    SubCategories = c.SubCategories.Select(s => new SubCategoryConfig
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Weight = s.Weight,
                        MinExpHours = s.MinExpHours,
                        MaxExpHours = s.MaxExpHours,
                        Urgency = s.Urgency,
                        RequiresHardSurface = s.RequiresHardSurface,
                        MinRunwayDest = s.MinRunwayDest,
                        LuggageWeightKg = s.LuggageWeightKg,
                        RangeRules = s.RangeRules,
                        PaxRules = s.PaxRules,
                        LbsRules = s.LbsRules,
                        Definition = s.Definition
                    }).ToList()
                };

                if (c.Id == "aeroclube" && isScenic)
                {
                    clone.Weight = 40.0;
                    foreach (var s in clone.SubCategories)
                    {
                        if (s.Id == "voo_panoramico" || s.Id == "panoramico") s.Weight = 70.0;
                        else if (s.Id == "circuito_tgl") s.Weight = 15.0;
                        else if (s.Id == "translado_basico" || s.Id == "ferry_posicionamento") s.Weight = 15.0;
                    }
                }
                result.Add(clone);
            }
            return result;
        }

        private static void AddCategoryWeight(List<CategoryConfig> categories, string categoryId, double bonus)
        {
            var cat = categories.FirstOrDefault(c => c.Id == categoryId);
            if (cat != null)
            {
                cat.Weight += bonus;
            }
        }

        private T? PickWeighted<T>(IEnumerable<T> items) where T : class
        {
            return PickWeighted(items, _random);
        }

        private static T? PickWeighted<T>(IEnumerable<T> items, Random random) where T : class
        {
            if (items == null || !items.Any()) return null;
            double totalWeight = items.Sum(i => (double)i.GetType().GetProperty("Weight")?.GetValue(i)!);
            if (totalWeight <= 0) return items.FirstOrDefault();
            double roll = (random ?? new Random()).NextDouble() * totalWeight;
            double current = 0;
            foreach (var item in items)
            {
                current += (double)item.GetType().GetProperty("Weight")?.GetValue(item)!;
                if (roll <= current) return item;
            }
            return items.LastOrDefault();
        }

        public static bool IsDifficultRunway(Runway? runway)
        {
            if (runway == null) return false;
            return RunwaySurface.IsUnpaved(runway.Surface) || (runway.LengthFt > 0 && runway.LengthFt < 2500);
        }

        public double CalculateReward(
            double distance, 
            double payload, 
            double urgency, 
            string subId = "", 
            int paxCount = 0, 
            int originTier = 3, 
            int destTier = 3,
            Runway? destRunway = null)
        {
            bool? isDifficult = destRunway != null ? IsDifficultRunway(destRunway) : null;
            return CalculateReward(distance, payload, urgency, subId, paxCount, originTier, destTier, _random, PayMultiplier, PayTable, isDifficult);
        }

        public static double CalculateReward(
            double distance, 
            double payload, 
            double urgency, 
            string subId, 
            int paxCount, 
            int originTier, 
            int destTier, 
            Runway? destRunway,
            Random? random = null,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null)
        {
            bool? isDifficult = destRunway != null ? IsDifficultRunway(destRunway) : null;
            return CalculateReward(distance, payload, urgency, subId, paxCount, originTier, destTier, random, payMultiplier, payTable, isDifficult);
        }

        public static double CalculateReward(
            double distance, 
            double payload, 
            double urgency, 
            string subId, 
            int paxCount, 
            int originTier, 
            int destTier, 
            Random? random = null,
            double payMultiplier = 1.0,
            MissionPayTable? payTable = null,
            bool? isDifficultRunway = null)
        {
            payTable ??= MissionPayTable.Shared;
            var family = payTable.Resolve(subId);

            double effectivePax = family.PaxAtLeastOne ? Math.Max(1, paxCount) : paxCount;
            double ratePerMile = family.RatePerMile + (effectivePax * family.RatePerPaxMile);
            double baseFee = family.BaseFee + (effectivePax * family.BaseFeePerPax);
            double ratePerPayloadLb = family.RatePerPayloadLb;

            // 2. Base Bruta multiplicada pela Urgência da Missão
            double urgencyMultiplier = Math.Max(1.0, urgency);
            double grossBase = (baseFee + (distance * ratePerMile) + (payload * ratePerPayloadLb)) * urgencyMultiplier;

            // 3. Fator Econômico do Aeródromo de Origem
            double originFactor = originTier switch
            {
                1 => 1.20, // Hubs internacionais (maior poder aquisitivo e taxas de solo)
                2 => 1.08, // Regionais médios
                3 => 1.00, // Municipais (base)
                4 => 0.95, // Remotos rurais
                _ => 1.00
            };

            // 4. Adicional de Pista Desafiadora / Não Pavimentada (Bush Premium: +25% de taxa de risco)
            // Se houver informação de pista: não pavimentada ou maior pista < 2.500 ft; sem dado de pista: fallback no tier 4
            double bushBonus = isDifficultRunway.HasValue 
                ? (isDifficultRunway.Value ? 1.25 : 1.00) 
                : ((destTier == 4) ? 1.25 : 1.00);

            // 5. Flutuação de Mercado Orgânica (+/- 14% de variação estocástica realista)
            double marketSpread = 0.88 + ((random ?? new Random()).NextDouble() * 0.26);

            double finalReward = grossBase * originFactor * bushBonus * marketSpread * family.Scale * payMultiplier;

            return Math.Round(finalReward, 2);
        }

        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = lon2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3))) / 1852.0;
        }
    }
}
