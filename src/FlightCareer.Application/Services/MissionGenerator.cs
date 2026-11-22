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
                var newMissions = new List<Mission>();

                // Algumas subcategorias podem não encontrar destino válido na faixa sorteada.
                // Permite tentativas extras para garantir que a cota seja atingida.
                int maxAttempts = needed * 4;
                int attempts = 0;

                var calibratedCategories = GetCalibratedCategories(currentAirport, tierConfig);

                while (newMissions.Count < needed && attempts < maxAttempts)
                {
                    attempts++;

                    var category = PickWeighted(calibratedCategories);
                    if (category == null) continue;

                    var subCategory = PickWeighted(category.SubCategories);
                    if (subCategory == null) continue;

                    try
                    {
                        var mission = await CreateMissionFromSubCategory(currentAirport, subCategory, tierConfig.AllowedDestinationTiers);
                        if (mission != null) newMissions.Add(mission);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MissionGenerator] Falha ao gerar missão ({subCategory.Id}): {ex.Message}");
                    }
                }

                if (newMissions.Any())
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
            if (sub.Id == "1.7.1" || sub.Id == "2.7.1")
            {
                // Fretamento Executivo: capital/hub ou regional -> interior (tier 3 ou 4) do mesmo país
                query = query.Where(a => a.Country == origin.Country && (a.Tier == 3 || a.Tier == 4));
            }
            else if (sub.Id == "1.7.4" || sub.Id == "2.7.4")
            {
                // VIP Internacional Curto: país vizinho / diferente
                query = query.Where(a => a.Country != origin.Country && a.Country != "");
            }
            else if (sub.Id == "1.7.5")
            {
                // Ponte Aérea / Troncal: hub ↔ hub do mesmo país
                query = query.Where(a => a.Tier == 1 && a.Country == origin.Country);
            }
            else if (sub.Id == "1.7.6")
            {
                // Cargueiro Noturno: entre hubs
                query = query.Where(a => a.Tier == 1);
            }
            else if (sub.Id == "1.7.7")
            {
                // Long-Haul Internacional: entre hubs de países diferentes
                query = query.Where(a => a.Tier == 1 && a.Country != origin.Country && a.Country != "");
            }
            else if (sub.Id == "1.7.8")
            {
                // Fretamento Global / Carga de Alto Valor: aeroportos de grande porte
                query = query.Where(a => a.Tier <= 2);
            }

            if (sub.Id == "3.4.1") 
            {
                var nearbyTierAirports = await query.Where(a => a.Tier <= 2).ToListAsync();
                return CreateLocalInstructionMissionSync(
                    origin, sub, _random, _currentReferenceTime ?? DateTime.Now,
                    nearbyTierAirports, PayMultiplier, PayTable, _textCatalog);
            }

            if (sub.Id == "3.4.3")
            {
                var nearbyTierAirports = await query.Where(a => a.Tier <= 2).ToListAsync();
                return CreateCheckrideMissionSync(
                    origin, sub, _random, _currentReferenceTime ?? DateTime.Now,
                    nearbyTierAirports, PayMultiplier, PayTable, _textCatalog);
            }

            if (sub.Id == "3.5.1")
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

            if (sub.Id == "3.5.2")
            {
                int tglCount = _random.Next(3, 7);
                int tglPax = _random.Next(0, 2);
                double tglDist = tglCount * 4.0;
                string title = $"Circuito de Tráfego ({tglCount} TGLs)";
                string desc = $"Treinamento em circuito de tráfego padrão em {origin.ICAO}. Exige {tglCount} toques e arremetidas (TGL) seguidos de pouso final completo.";

                return await CreateMissionObject(origin, origin, tglDist, sub, tglPax, 0, 0,
                    requiredTouchAndGoes: tglCount, overflyWaypoints: null, titleOverride: title, descriptionOverride: desc);
            }

            if (sub.Id == "3.6.1")
            {
                return CreateSurveyMissionSync(origin, sub, _random, _currentReferenceTime ?? DateTime.Now, PayMultiplier, PayTable, _placesService, _textCatalog);
            }

            if (sub.Id == "3.6.2")
            {
                return CreateSkydiveMissionSync(origin, sub, _random, _currentReferenceTime ?? DateTime.Now, PayMultiplier, PayTable, _placesService, _textCatalog);
            }

            if (sub.Id == "3.5.3")
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

            if (sub.Id == "1.7.3" || sub.Id == "2.7.3" || sub.Id == "3.7.2")
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

            string? titleOverride = sub.Id switch
            {
                "3.5.3" => $"Translado Básico: {origin.ICAO} ➔ {selected.Airport.ICAO}",
                "1.7.6" => $"Cargueiro Noturno: {origin.ICAO} ➔ {selected.Airport.ICAO}",
                _ => null
            };
            string? descOverride = sub.Id switch
            {
                "3.5.3" => $"Translado de aeronave vazia para oficina ou manutenção em {selected.Airport.ICAO}. Apenas piloto a bordo e bagagem essencial.",
                "1.7.6" => $"Operação logística de carga pesada em voo noturno conectando grandes hubs ({origin.ICAO} ➔ {selected.Airport.ICAO}).",
                _ => null
            };
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
            var rendered = catalog.Render("3.4.1", random, values, fallbackTitle, fallbackDesc);

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
            var rendered = catalog.Render("3.4.3", random, values, fallbackTitle, fallbackDesc);

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
            var pois = places.FindPois(origin.Latitude, origin.Longitude, 5.0, 30.0);
            if (pois.Count < 2)
            {
                pois = places.FindPois(origin.Latitude, origin.Longitude, 5.0, 45.0);
            }

            if (pois.Count < 2) return null;

            int targetPoiCount = Math.Min(pois.Count, random.Next(2, 4));
            var pool = new List<TouristPoiInfo>(pois);
            var picked = new List<TouristPoiInfo>();

            int attempts = 0;
            while (picked.Count < targetPoiCount && pool.Count > 0 && attempts < 30)
            {
                attempts++;
                var candidate = PickWeightedPoi(pool, random);
                if (candidate == null) break;
                pool.Remove(candidate);

                bool tooClose = false;
                for (int j = 0; j < picked.Count; j++)
                {
                    double d = CalculateDistance(candidate.Lat, candidate.Lon, picked[j].Lat, picked[j].Lon);
                    if (d < 3.0)
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

            var poiNamesFormatted = picked.Select(FormatPoiWithKind).ToList();
            string poiListStr = poiNamesFormatted.Count == 2
                ? $"{poiNamesFormatted[0]} e {poiNamesFormatted[1]}"
                : $"{string.Join(", ", poiNamesFormatted.Take(poiNamesFormatted.Count - 1))} e {poiNamesFormatted[^1]}";

            int tourPax = random.Next(1, 4);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["origin"] = origin.ICAO,
                ["dest"] = origin.ICAO,
                ["poi_list"] = poiListStr,
                ["pax"] = tourPax.ToString(),
                ["distance_nm"] = totalTourDist.ToString("F0")
            };

            string fallbackTitle = $"Voo Panorâmico ({string.Join(" • ", picked.Select(p => p.Name))})";
            string fallbackDesc = $"Voo panorâmico sobrevoando {poiListStr}. Retorno para pouso em {origin.ICAO} (sobrevoo a ≤ 1,5 NM e abaixo de 3.000 ft AGL).";

            var catalog = textCatalog ?? MissionTextCatalog.Default;
            var rendered = catalog.Render("3.5.1", random, values, fallbackTitle, fallbackDesc);

            return CreateMissionObjectSync(
                origin, origin, totalTourDist, sub, tourPax, Math.Min(tourPax * 15.0, 30.0), 0,
                random, referenceTime, payMultiplier, payTable,
                requiredTouchAndGoes: 0,
                overflyWaypoints: wpString,
                titleOverride: rendered.Title,
                descriptionOverride: rendered.Description);
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
            var rendered = catalog.Render("3.6.1", random, values, fallbackTitle, fallbackDesc);

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
            var rendered = catalog.Render("3.6.2", random, values, fallbackTitle, fallbackDesc);

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
            return subId switch
            {
                "1.1.1" => profile.DomesticHubs ?? new List<string>(),
                "1.1.2" => profile.RegionalIntl ?? new List<string>(),
                "1.1.3" => profile.LongHaul ?? new List<string>(),

                "1.2.1" => profile.CargoHubs ?? new List<string>(),
                "1.2.2" => profile.RegionalIntl ?? new List<string>(),
                "1.2.3" => profile.LongHaul ?? new List<string>(),

                "1.3.1" => profile.ExecutiveHubs ?? new List<string>(),
                "1.3.2" => profile.DomesticHubs ?? new List<string>(),
                "1.3.3" => profile.MaintenanceHubs ?? new List<string>(),
                "1.3.4" => profile.DomesticHubs ?? new List<string>(),

                "2.1.1" => profile.FeederHubs ?? new List<string>(),
                "2.1.2" => profile.RegionalPoints ?? new List<string>(),

                "2.2.1" => profile.AgroPoints ?? new List<string>(),
                "2.2.2" => profile.AgroTechPoints ?? new List<string>(),
                "2.2.3" => profile.InvestPoints ?? new List<string>(),

                "2.3.1" => profile.EcommercePoints ?? new List<string>(),
                "2.3.2" => profile.AgroPartsPoints ?? new List<string>(),
                "2.3.3" => profile.DomesticHubs ?? new List<string>(),

                "2.4.1" => profile.MaintenanceHubs ?? new List<string>(),
                "2.4.2" => profile.GovPoints ?? new List<string>(),

                "3.1.1" => profile.RegionalPoints ?? new List<string>(),
                "3.1.2" => profile.LeisurePoints ?? new List<string>(),
                "3.1.3" => profile.RegionalPoints ?? new List<string>(),

                "3.2.1" => profile.AgroPoints ?? new List<string>(),
                "3.2.2" => profile.AgroTechPoints ?? new List<string>(),
                "3.2.3" => profile.AgroPoints ?? new List<string>(),

                "3.3.1" => profile.HealthHubs ?? new List<string>(),
                "3.3.2" => profile.HealthHubs ?? new List<string>(),
                "3.3.3" => profile.HealthHubs ?? new List<string>(),

                "3.4.1" => profile.InstructionHubs ?? new List<string>(),
                "3.4.2" => profile.RegionalPoints ?? new List<string>(),
                "3.4.3" => profile.InstructionHubs ?? new List<string>(),

                "4.1.1" => profile.AgroPoints ?? new List<string>(),
                "4.1.2" => profile.RegionalPoints ?? new List<string>(),
                "4.1.3" => profile.AgroPoints ?? new List<string>(),

                "4.2.1" => profile.AgroPoints ?? new List<string>(),
                "4.2.2" => profile.AgroPoints ?? new List<string>(),
                "4.2.3" => profile.AgroPoints ?? new List<string>(),

                "4.3.1" => profile.HealthHubs ?? new List<string>(),
                "4.3.2" => profile.AgroPoints ?? new List<string>(),
                _ => Array.Empty<string>()
            };
        }

        public static IReadOnlyList<string> ResolveDynamicHubs(string subId, string country, IHubDataService hubService)
        {
            if (hubService == null) return Array.Empty<string>();
            return subId switch
            {
                "1.1.1" => hubService.GetHubs(HubType.CommercialPax, country),
                "1.1.2" => hubService.GetHubs(HubType.CommercialPax, "GLOBAL"),
                "1.2.1" => hubService.GetHubs(HubType.Cargo, country),
                "1.2.2" => hubService.GetHubs(HubType.Cargo, "GLOBAL"),
                "1.3.1" => hubService.GetHubs(HubType.Executive, country),
                "1.3.3" or "2.4.1" => hubService.GetHubs(HubType.Maintenance, country),
                "2.1.1" => hubService.GetHubs(HubType.Feeder, country).Concat(hubService.GetHubs(HubType.CommercialPax, country)).ToList(),
                "2.1.2" => hubService.GetHubs(HubType.Feeder, country),
                "2.2.1" or "3.2.1" or "4.1.1" or "4.2.1" or "4.2.2" or "4.2.3" => hubService.GetHubs(HubType.Agro, country),
                "2.3.1" or "2.3.2" => hubService.GetHubs(HubType.Cargo, country),
                "3.1.1" or "3.1.2" => hubService.GetHubs(HubType.Executive, country),
                "3.3.1" or "3.3.2" or "3.3.3" or "4.3.1" => hubService.GetHubs(HubType.Health, country),
                "3.4.1" or "3.4.2" or "3.4.3" => hubService.GetHubs(HubType.Instruction, country),
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

            var calibratedCategories = GetCalibratedCategories(currentAirport, tierConfig, hubService, placesService);
            var newMissions = new List<Mission>();
            int maxAttempts = targetCount * 4;
            int attempts = 0;

            while (newMissions.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                var category = PickWeighted(calibratedCategories, random);
                if (category == null) continue;
                var subCategory = PickWeighted(category.SubCategories, random);
                if (subCategory == null) continue;

                var mission = CreateMissionFromSubCategoryInMemory(
                    currentAirport,
                    subCategory,
                    tierConfig.AllowedDestinationTiers,
                    allAirports,
                    hubService,
                    random,
                    referenceTime,
                    payMultiplier,
                    payTable,
                    allRunways,
                    placesService,
                    textCatalog);

                if (mission != null)
                {
                    newMissions.Add(mission);
                }
            }

            return newMissions;
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

            if (sub.Id == "3.4.1")
            {
                return CreateLocalInstructionMissionSync(
                    origin, sub, random, referenceTime,
                    nearbyTierAirports, payMultiplier, payTable, textCatalog);
            }

            if (sub.Id == "3.4.3")
            {
                return CreateCheckrideMissionSync(
                    origin, sub, random, referenceTime,
                    nearbyTierAirports, payMultiplier, payTable, textCatalog);
            }

            if (sub.Id == "3.5.1")
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

            if (sub.Id == "3.5.2")
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

            if (sub.Id == "3.6.1")
            {
                return CreateSurveyMissionSync(origin, sub, random, referenceTime, payMultiplier, payTable, placesService, textCatalog);
            }

            if (sub.Id == "3.6.2")
            {
                return CreateSkydiveMissionSync(origin, sub, random, referenceTime, payMultiplier, payTable, placesService, textCatalog);
            }

            if (sub.Id == "3.5.3")
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
                if (sub.Id == "1.7.1" || sub.Id == "2.7.1")
                {
                    if (!string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase) || (a.Tier != 3 && a.Tier != 4)) continue;
                }
                else if (sub.Id == "1.7.4" || sub.Id == "2.7.4")
                {
                    if (string.IsNullOrEmpty(a.Country) || string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase)) continue;
                }
                else if (sub.Id == "1.7.5")
                {
                    if (a.Tier != 1 || !string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase)) continue;
                }
                else if (sub.Id == "1.7.6")
                {
                    if (a.Tier != 1) continue;
                }
                else if (sub.Id == "1.7.7")
                {
                    if (a.Tier != 1 || string.IsNullOrEmpty(a.Country) || string.Equals(a.Country, origin.Country, StringComparison.OrdinalIgnoreCase)) continue;
                }
                else if (sub.Id == "1.7.8")
                {
                    if (a.Tier > 2) continue;
                }
                else if (sub.Id == "1.7.3" || sub.Id == "2.7.3" || sub.Id == "3.7.2")
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

            string? titleOverride = sub.Id switch
            {
                "3.5.3" => $"Translado Básico: {origin.ICAO} ➔ {selected.Airport.ICAO}",
                "1.7.6" => $"Cargueiro Noturno: {origin.ICAO} ➔ {selected.Airport.ICAO}",
                _ => null
            };
            string? descOverride = sub.Id switch
            {
                "3.5.3" => $"Translado de aeronave vazia para oficina ou manutenção em {selected.Airport.ICAO}. Apenas piloto a bordo e bagagem essencial.",
                "1.7.6" => $"Operação logística de carga pesada em voo noturno conectando grandes hubs ({origin.ICAO} ➔ {selected.Airport.ICAO}).",
                _ => null
            };

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
            var categories = tierConfig.Categories.Select(c => new CategoryConfig
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
                    RequiresHardSurface = s.RequiresHardSurface,
                    MinRunwayDest = s.MinRunwayDest,
                    LuggageWeightKg = s.LuggageWeightKg,
                    Urgency = s.Urgency,
                    PaxRules = new List<ProbRange>(s.PaxRules),
                    LbsRules = new List<ProbRange>(s.LbsRules),
                    RangeRules = new List<ProbRange>(s.RangeRules)
                }).ToList()
            }).ToList();

            var hubTypes = hubService?.GetHubTypesForAirport(airport.ICAO) ?? (IReadOnlyList<HubType>)Array.Empty<HubType>();
            bool isInstruction = hubTypes.Contains(HubType.Instruction) || (placesService != null && placesService.IsTrainingAerodrome(airport.ICAO));

            // Se for Hub de Instrução e o Tier não possui nativamente a Categoria 3.4 (ex: Tier 1),
            // importamos a definição de Categoria 3.4
            if (isInstruction && !categories.Any(c => c.Id == "3.4"))
            {
                foreach (var t in MissionRules.Rules.Values)
                {
                    var found = t.Categories.FirstOrDefault(c => c.Id == "3.4");
                    if (found != null)
                    {
                        categories.Add(new CategoryConfig
                        {
                            Id = found.Id,
                            Name = found.Name,
                            Weight = 10,
                            SubCategories = found.SubCategories.Select(s => new SubCategoryConfig
                            {
                                Id = s.Id,
                                Name = s.Name,
                                Description = s.Description,
                                Weight = s.Weight,
                                MinExpHours = s.MinExpHours,
                                MaxExpHours = s.MaxExpHours,
                                RequiresHardSurface = s.RequiresHardSurface,
                                MinRunwayDest = s.MinRunwayDest,
                                LuggageWeightKg = s.LuggageWeightKg,
                                Urgency = s.Urgency,
                                PaxRules = new List<ProbRange>(s.PaxRules),
                                LbsRules = new List<ProbRange>(s.LbsRules),
                                RangeRules = new List<ProbRange>(s.RangeRules)
                            }).ToList()
                        });
                        break;
                    }
                }
            }

            // Se for aeródromo com zona de salto e não tem Categoria 3.6, importa 3.6
            var dropZone = placesService?.GetDropZone(airport.ICAO);
            if (dropZone != null && !categories.Any(c => c.Id == "3.6"))
            {
                foreach (var t in MissionRules.Rules.Values)
                {
                    var found = t.Categories.FirstOrDefault(c => c.Id == "3.6");
                    if (found != null)
                    {
                        categories.Add(new CategoryConfig
                        {
                            Id = found.Id,
                            Name = found.Name,
                            Weight = 5,
                            SubCategories = found.SubCategories.Select(s => new SubCategoryConfig
                            {
                                Id = s.Id,
                                Name = s.Name,
                                Description = s.Description,
                                Weight = s.Weight,
                                MinExpHours = s.MinExpHours,
                                MaxExpHours = s.MaxExpHours,
                                RequiresHardSurface = s.RequiresHardSurface,
                                MinRunwayDest = s.MinRunwayDest,
                                LuggageWeightKg = s.LuggageWeightKg,
                                Urgency = s.Urgency,
                                PaxRules = new List<ProbRange>(s.PaxRules),
                                LbsRules = new List<ProbRange>(s.LbsRules),
                                RangeRules = new List<ProbRange>(s.RangeRules)
                            }).ToList()
                        });
                        break;
                    }
                }
            }

            // Se for Hub de Saúde / Aeromédico e o Tier não possui nativamente a Categoria 3.3 (ex: Tier 1/2),
            // importamos a definição de Categoria 3.3 (Saúde / Remoção UTI Aérea)
            if (hubTypes.Contains(HubType.Health) && !categories.Any(c => c.Id == "3.3"))
            {
                foreach (var t in MissionRules.Rules.Values)
                {
                    var found = t.Categories.FirstOrDefault(c => c.Id == "3.3");
                    if (found != null)
                    {
                        categories.Add(new CategoryConfig
                        {
                            Id = found.Id,
                            Name = found.Name,
                            Weight = 5,
                            SubCategories = found.SubCategories.Select(s => new SubCategoryConfig
                            {
                                Id = s.Id,
                                Name = s.Name,
                                Description = s.Description,
                                Weight = s.Weight,
                                MinExpHours = s.MinExpHours,
                                MaxExpHours = s.MaxExpHours,
                                RequiresHardSurface = s.RequiresHardSurface,
                                MinRunwayDest = s.MinRunwayDest,
                                LuggageWeightKg = s.LuggageWeightKg,
                                Urgency = s.Urgency,
                                PaxRules = new List<ProbRange>(s.PaxRules),
                                LbsRules = new List<ProbRange>(s.LbsRules),
                                RangeRules = new List<ProbRange>(s.RangeRules)
                            }).ToList()
                        });
                        break;
                    }
                }
            }

            // Ajuste de pesos por DropZone:
            var cat36 = categories.FirstOrDefault(c => c.Id == "3.6");
            if (dropZone != null)
            {
                AddCategoryWeight(categories, "3.6", 40);
                if (cat36 != null)
                {
                    var s361 = cat36.SubCategories.FirstOrDefault(s => s.Id == "3.6.1");
                    var s362 = cat36.SubCategories.FirstOrDefault(s => s.Id == "3.6.2");
                    if (s361 != null) s361.Weight = 20;
                    if (s362 != null) s362.Weight = 80;
                }
            }
            else if (cat36 != null)
            {
                var s361 = cat36.SubCategories.FirstOrDefault(s => s.Id == "3.6.1");
                var s362 = cat36.SubCategories.FirstOrDefault(s => s.Id == "3.6.2");
                if (s361 != null) s361.Weight = 85;
                if (s362 != null) s362.Weight = 15;
            }

            // Bônus para aeródromos de instrução:
            if (isInstruction)
            {
                AddCategoryWeight(categories, "3.4", 45);
                AddCategoryWeight(categories, "3.5", 15);
            }

            foreach (var hubType in hubTypes)
            {
                switch (hubType)
                {
                    case HubType.Instruction:
                        // Já tratado em isInstruction acima
                        break;

                    case HubType.Cargo:
                        AddCategoryWeight(categories, "1.2", 35);
                        AddCategoryWeight(categories, "2.3", 30);
                        break;

                    case HubType.CommercialPax:
                        AddCategoryWeight(categories, "1.1", 40);
                        AddCategoryWeight(categories, "2.1", 35);
                        break;

                    case HubType.Executive:
                        AddCategoryWeight(categories, "1.3", 30);
                        AddCategoryWeight(categories, "2.2", 25);
                        AddCategoryWeight(categories, "3.1", 35);
                        break;

                    case HubType.Maintenance:
                        AddCategoryWeight(categories, "1.3", 20);
                        AddCategoryWeight(categories, "2.4", 25);
                        break;

                    case HubType.Feeder:
                        AddCategoryWeight(categories, "2.1", 30);
                        AddCategoryWeight(categories, "4.1", 20);
                        break;

                    case HubType.Agro:
                        AddCategoryWeight(categories, "2.2", 25);
                        AddCategoryWeight(categories, "3.2", 35);
                        AddCategoryWeight(categories, "4.1", 25);
                        AddCategoryWeight(categories, "4.2", 30);
                        break;

                    case HubType.Health:
                        AddCategoryWeight(categories, "3.3", 35);
                        AddCategoryWeight(categories, "4.3", 25);
                        break;
                }
            }

            return categories;
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
