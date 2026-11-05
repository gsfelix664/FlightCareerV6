using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.Infrastructure.Services
{
    public class MissionGenerator : IMissionGenerator
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new();

        public MissionGenerator(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Mission>> GenerateMissionsAsync(Airport currentAirport)
        {
            // 1. Limpar missões expiradas (que não foram aceitas)
            var expired = await _context.Missions
                .Where(m => !m.IsAccepted && m.ExpirationTime < DateTime.Now)
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
                        .ToListAsync();
                }
            }

            // 3. Verificar se precisamos repor o estoque
            if (!MissionRules.Rules.TryGetValue(currentAirport.Tier, out var tierConfig))
                return currentMktMissions;

            int targetCount = currentAirport.Tier switch
            {
                1 => 12, 2 => 10, 3 => 8, 4 => 6, _ => 6
            };

            if (currentMktMissions.Count < targetCount)
            {
                int needed = targetCount - currentMktMissions.Count;
                var newMissions = new List<Mission>();

                for (int i = 0; i < needed; i++)
                {
                    var category = PickWeighted(tierConfig.Categories);
                    if (category == null) continue;

                    var subCategory = PickWeighted(category.SubCategories);
                    if (subCategory == null) continue;

                    var mission = await CreateMissionFromSubCategory(currentAirport, subCategory, tierConfig.AllowedDestinationTiers);
                    if (mission != null) newMissions.Add(mission);
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
            query = query.Where(a => allowedTiers.Contains(a.Tier));
            if (sub.RequiresHardSurface) query = query.Where(a => a.Tier <= 3);

            if (sub.Id == "3.4.1") 
            {
                return await CreateMissionObject(origin, origin, 10.0, sub, paxCount, luggageWeightLbs, cargoWeight);
            }

            double maxDeg = (maxRange / 50.0); 
            var possibleDestinations = await query
                .Where(a => a.ICAO != origin.ICAO)
                .Where(a => a.Latitude >= origin.Latitude - maxDeg && a.Latitude <= origin.Latitude + maxDeg)
                .Where(a => a.Longitude >= origin.Longitude - maxDeg && a.Longitude <= origin.Longitude + maxDeg)
                .Take(500)
                .ToListAsync();

            var validDestinations = possibleDestinations
                .Select(d => new { Airport = d, Dist = CalculateDistance(origin.Latitude, origin.Longitude, d.Latitude, d.Longitude) })
                .Where(x => x.Dist >= minRange && x.Dist <= maxRange)
                .ToList();

            if (!validDestinations.Any()) return null;

            // 3. Priorizar ICAOs (Geral ou via Perfis G20)
            var preferredList = sub.PreferredDestinations.ToList();
            var profile = G20Destinations.GetProfile(origin.Country);
            
            if (profile != null)
            {
                var hubs = sub.Id switch
                {
                    // --- CATEGORIA 1: AVIAÇÃO COMERCIAL, CARGA E EXECUTIVA PESADA ---
                    "1.1.1" => profile.Value.DomesticHubs,
                    "1.1.2" => profile.Value.RegionalIntl,
                    "1.1.3" => profile.Value.LongHaul,

                    "1.2.1" => profile.Value.CargoHubs,
                    "1.2.2" => profile.Value.RegionalIntl,
                    "1.2.3" => profile.Value.LongHaul,

                    "1.3.1" => profile.Value.ExecutiveHubs,
                    "1.3.2" => profile.Value.DomesticHubs,
                    "1.3.3" => profile.Value.MaintenanceHubs,
                    "1.3.4" => profile.Value.DomesticHubs,

                    // --- CATEGORIA 2: AVIAÇÃO REGIONAL, LOGÍSTICA E INVESTIMENTOS ---
                    "2.1.1" => profile.Value.FeederHubs,
                    "2.1.2" => profile.Value.RegionalPoints,

                    "2.2.1" => profile.Value.AgroPoints,
                    "2.2.2" => profile.Value.AgroTechPoints,
                    "2.2.3" => profile.Value.InvestPoints,

                    "2.3.1" => profile.Value.EcommercePoints,
                    "2.3.2" => profile.Value.AgroPartsPoints,
                    "2.3.3" => profile.Value.DomesticHubs,

                    "2.4.1" => profile.Value.MaintenanceHubs,
                    "2.4.2" => profile.Value.GovPoints,

                    // --- CATEGORIA 3: TURISMO, SAÚDE E SERVIÇOS ---
                    "3.1.1" => profile.Value.RegionalPoints,
                    "3.1.2" => profile.Value.LeisurePoints,
                    "3.1.3" => profile.Value.RegionalPoints,

                    "3.2.1" => profile.Value.AgroPoints,
                    "3.2.2" => profile.Value.AgroTechPoints,
                    "3.2.3" => profile.Value.AgroPoints,

                    "3.3.1" => profile.Value.HealthHubs,
                    "3.3.2" => profile.Value.HealthHubs,
                    "3.3.3" => profile.Value.HealthHubs,

                    "3.4.1" => profile.Value.InstructionHubs ?? new List<string>(),
                    "3.4.2" => profile.Value.RegionalPoints,
                    "3.4.3" => profile.Value.InstructionHubs ?? new List<string>(),

                    // --- CATEGORIA 4: AGRO INTERIOR E OPERAÇÕES REMOTAS ---
                    "4.1.1" => profile.Value.AgroPoints,
                    "4.1.2" => profile.Value.RegionalPoints, // Patrão geralmente vem da cidade
                    "4.1.3" => profile.Value.AgroPoints,

                    "4.2.1" => profile.Value.AgroPoints,
                    "4.2.2" => profile.Value.AgroPoints,
                    "4.2.3" => profile.Value.AgroPoints,

                    "4.3.1" => profile.Value.HealthHubs,
                    "4.3.2" => profile.Value.AgroPoints,
                    _ => new List<string>()
                };
                preferredList.AddRange(hubs);
            }

            var preferredCandidates = validDestinations.Where(x => preferredList.Contains(x.Airport.ICAO)).ToList();
            var selected = preferredCandidates.Any() ? preferredCandidates[_random.Next(preferredCandidates.Count)] : validDestinations[_random.Next(validDestinations.Count)];

            return await CreateMissionObject(origin, selected.Airport, selected.Dist, sub, paxCount, luggageWeightLbs, cargoWeight);
        }

        private async Task<Mission> CreateMissionObject(Airport origin, Airport dest, double dist, SubCategoryConfig sub, int paxCount, double luggageWeightLbs, double cargoWeight)
        {
            double totalPayload = (paxCount * 170) + luggageWeightLbs + cargoWeight;

            return new Mission
            {
                Id = Guid.NewGuid(),
                Title = sub.Name,
                Description = sub.Description,
                OriginICAO = origin.ICAO,
                DestinationICAO = dest.ICAO,
                DistanceNM = Math.Round(dist, 0),
                PassengerCount = paxCount,
                LuggageWeightLbs = Math.Round(luggageWeightLbs, 0),
                CargoWeightLbs = Math.Round(cargoWeight, 0),
                PayloadLbs = Math.Round(totalPayload, 0),
                Urgency = sub.Urgency,
                RewardMoney = CalculateReward(dist, totalPayload, sub.Urgency),
                ExperienceGain = (int)(dist * 0.5 * sub.Urgency),
                ExpirationTime = DateTime.Now.AddHours(_random.Next(sub.MinExpHours, sub.MaxExpHours + 1))
            };
        }

        private T? PickWeighted<T>(IEnumerable<T> items) where T : class
        {
            if (items == null || !items.Any()) return null;
            double totalWeight = items.Sum(i => (double)i.GetType().GetProperty("Weight")?.GetValue(i)!);
            if (totalWeight <= 0) return items.FirstOrDefault();
            double roll = _random.NextDouble() * totalWeight;
            double current = 0;
            foreach (var item in items)
            {
                current += (double)item.GetType().GetProperty("Weight")?.GetValue(item)!;
                if (roll <= current) return item;
            }
            return items.LastOrDefault();
        }

        public double CalculateReward(double distance, double payload, double urgency)
        {
            double rate = 15.0 * urgency;
            return Math.Round((distance * rate) + (payload * 0.05), 2);
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
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
