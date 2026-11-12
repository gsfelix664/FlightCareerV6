using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Domain.Services;
using FlightCareer.Domain.Utils;
using FlightCareer.Application.DTOs;
using FlightCareer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.Application.Services
{
    public class FlightService
    {
        private readonly IAppDbContext _context;
        private readonly IPilotRepository _pilotRepository;
        private readonly IMissionGenerator _missionGenerator;

        public FlightService(IAppDbContext context, IPilotRepository pilotRepository, IMissionGenerator missionGenerator)
        {
            _context = context;
            _pilotRepository = pilotRepository;
            _missionGenerator = missionGenerator;
        }

        public async Task<FlightOutcome> ProcessFlightFinishAsync(
            Guid pilotId,
            Guid aircraftId,
            string originIcao,
            string destinationIcao,
            DateTime startTime,
            double initialFuel,
            double finalFuel,
            double distanceNM,
            List<Mission> activeMissions,
            FlightQualityService qualityService,
            double engineCondition = 100.0,
            double tiresCondition = 100.0,
            double airframeCondition = 100.0,
            double avionicsCondition = 100.0,
            int landingsCount = 0,
            bool isOffFieldLanding = false,
            double nearestDistance = 0.0,
            double flightDurationMinutes = 0.0,
            double? magnetosCondition = null,
            double? oilCondition = null)
        {
            int durationMinutes = flightDurationMinutes > 0
                ? Math.Max(1, (int)Math.Round(flightDurationMinutes))
                : Math.Max(1, (int)(DateTime.Now - startTime).TotalMinutes);

            var outcome = new FlightOutcome
            {
                Origin = originIcao,
                Destination = destinationIcao,
                DistanceNM = distanceNM,
                FlightTimeMinutes = durationMinutes,
                FuelUsed = Math.Max(0, initialFuel - finalFuel)
            };

            if (distanceNM < 0.01) 
            {
                outcome.Success = false;
                outcome.Message = "Voo muito curto para registro.";
                return outcome;
            }

            try
            {
                var pilot = await _context.Pilots
                    .Include(p => p.Hangar)
                    .FirstOrDefaultAsync(p => p.Id == pilotId);
                
                if (pilot == null) throw new Exception("Piloto não encontrado.");

                var aircraft = pilot.Hangar.FirstOrDefault(a => a.Id == aircraftId);
                if (aircraft == null) throw new Exception("Aeronave não encontrada.");

                var destAirport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == destinationIcao);
                int airportTier = destAirport?.Tier ?? 1;

                // 1. Atualizar Aeronave e Persistência de Danos / Condições
                aircraft.FuelAmount = finalFuel;
                aircraft.CurrentLocationICAO = destinationIcao;
                double hours = outcome.FlightTimeMinutes / 60.0;
                aircraft.CurrentAirframeHours += hours;

                aircraft.EngineCondition = engineCondition;
                aircraft.TiresCondition = tiresCondition;
                aircraft.AirframeCondition = airframeCondition;
                aircraft.AvionicsCondition = avionicsCondition;
                aircraft.LandingsCount = landingsCount;

                if (magnetosCondition.HasValue)
                {
                    aircraft.MagnetosCondition = magnetosCondition.Value;
                }
                if (oilCondition.HasValue)
                {
                    aircraft.OilCondition = oilCondition.Value;
                }

                // Consumo de TBO e Inspeções Regulatórias
                aircraft.EngineTboRemainingHours = Math.Max(0, aircraft.EngineTboRemainingHours - hours);
                aircraft.HoursUntilNext50hInspection = Math.Max(0, aircraft.HoursUntilNext50hInspection - hours);
                aircraft.HoursUntilNext100hInspection = Math.Max(0, aircraft.HoursUntilNext100hInspection - hours);
                aircraft.HoursUntilNext500hInspection = Math.Max(0, aircraft.HoursUntilNext500hInspection - hours);

                if (aircraft.EngineTboRemainingHours <= 0)
                {
                    aircraft.IsInterdicted = true;
                    aircraft.InterdictionReason = "TBO Vencido (Retífica Obrigatória de Motor)";
                }
                else if (aircraft.HoursUntilNext100hInspection <= 0)
                {
                    aircraft.IsInterdicted = true;
                    aircraft.InterdictionReason = "Inspeção Regulatória de 100 Horas Vencida";
                }
                
                // 2. Atualizar Piloto
                pilot.CurrentAirportICAO = destinationIcao;
                
                double qualityScore = qualityService.GetQualityScore();
                double qualityMultiplier = qualityScore / 100.0;
                bool isHardLanding = qualityService.TouchdownFPM > 500;
                bool isCrash = qualityService.TouchdownFPM >= 900;

                if (isCrash)
                {
                    // CASO DE CRASH NO POUSO (Impacto violento >= 900 FPM com colapso de trem/célula)
                    aircraft.EngineCondition = 0;
                    aircraft.TiresCondition = 0;
                    aircraft.AirframeCondition = 0;
                    aircraft.AvionicsCondition = 0;
                    aircraft.MagnetosCondition = 0;
                    aircraft.OilCondition = 0;
                    aircraft.IsInterdicted = true;
                    aircraft.InterdictionReason = "Aeronave destruída em acidente com colapso estrutural";

                    foreach (var mission in activeMissions)
                    {
                        if (mission.DestinationICAO.ToUpper() == destinationIcao.ToUpper())
                        {
                            var dbMission = await _context.Missions.FindAsync(mission.Id);
                            if (dbMission != null) _context.Missions.Remove(dbMission);
                        }
                    }
                    await _context.SaveChangesAsync();
                    
                    outcome.Success = false;
                    outcome.Message = $"CRASH NO POUSO ({qualityService.TouchdownFPM:F0} FPM)! Aeronave destruída e carga perdida.";
                    return outcome;
                }

                // CASO DE POUSO FORÇADO FORA DE PISTA (EMERGÊNCIA)
                if (isOffFieldLanding)
                {
                    // Dano Adicional de Terreno Despreparado
                    aircraft.TiresCondition = Math.Max(0, aircraft.TiresCondition - 35.0);
                    aircraft.AirframeCondition = Math.Max(0, aircraft.AirframeCondition - 20.0);

                    // NÃO CONCLUIR MISSÕES: Atualizar OriginICAO para destinationIcao (resgate)
                    int missionsRemainingOff = 0;
                    foreach (var mission in activeMissions)
                    {
                        mission.OriginICAO = destinationIcao;
                        var dbMission = await _context.Missions.FindAsync(mission.Id);
                        if (dbMission != null)
                        {
                            dbMission.OriginICAO = destinationIcao;
                            var destAirp = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == dbMission.DestinationICAO);
                            var curAirp = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == destinationIcao);
                            if (destAirp != null && curAirp != null)
                            {
                                double newDist = LocationUtils.CalculateDistance(curAirp.Latitude, curAirp.Longitude, destAirp.Latitude, destAirp.Longitude);
                                dbMission.DistanceNM = Math.Round(newDist, 0);
                                mission.DistanceNM = dbMission.DistanceNM;
                            }
                        }
                        missionsRemainingOff++;
                    }

                    // Custos de Combustível Consumido
                    double fuelUsedOff = outcome.FuelUsed;
                    string fuelTypeOff = aircraft.AircraftType?.FuelType ?? "AVGAS";
                    double fuelPriceOff = FuelPriceService.GetCurrentPrice(fuelTypeOff);
                    double fuelCostOff = Math.Round(fuelUsedOff * fuelPriceOff, 2);

                    if (fuelCostOff > 0)
                    {
                        pilot.Balance -= fuelCostOff;
                        _context.FinancialTransactions.Add(new FinancialTransaction
                        {
                            Id = Guid.NewGuid(),
                            PilotId = pilot.Id,
                            Date = DateTime.Now,
                            Type = TransactionType.Expense,
                            Category = "Combustível",
                            Amount = fuelCostOff,
                            Description = $"Combustível consumido ({fuelUsedOff:F1} GAL {fuelTypeOff} @ {fuelPriceOff:C2}/gal)"
                        });
                    }

                    pilot.TotalHours += hours;

                    // Registrar Voo no Logbook
                    var flightOff = new Flight
                    {
                        Id = Guid.NewGuid(),
                        PilotId = pilot.Id,
                        OriginICAO = originIcao,
                        DestinationICAO = destinationIcao,
                        DurationHours = hours,
                        DistanceNM = distanceNM,
                        Date = DateTime.Now,
                        Revenue = 0,
                        XP = 0
                    };
                    _context.Flights.Add(flightOff);

                    await _context.SaveChangesAsync();

                    outcome.Success = false;
                    outcome.Revenue = 0;
                    outcome.FuelCost = fuelCostOff;
                    outcome.LandingFee = 0;
                    outcome.XP = 0;
                    outcome.CompletedMissions = 0;
                    outcome.RemainingMissions = missionsRemainingOff;
                    outcome.Message = $"POUSO FORÇADO FORA DE PISTA! Aeronave pousou a {nearestDistance:F1} NM de {destinationIcao}. Carga e passageiros aguardam resgate.";
                    return outcome;
                }

                double totalRevenue = 0;
                // XP Baseado em Tempo + Distância + Tier (Afetado pela Qualidade)
                int baseXP = outcome.FlightTimeMinutes + (int)(distanceNM * 0.5);
                int tierBonusXP = (int)(baseXP * (airportTier * 0.2)); 
                int totalXP = (int)((baseXP + tierBonusXP) * qualityMultiplier);

                int missionsDone = 0;
                int missionsRemaining = 0;

                foreach (var mission in activeMissions)
                {
                    if (mission.DestinationICAO.ToUpper() == destinationIcao.ToUpper())
                    {
                        // PAGAMENTO INTEGRAL (Independente da qualidade, exceto se crashar)
                        double missionReward = mission.RewardMoney;
                        totalRevenue += missionReward;
                        missionsDone++;
                        
                        var dbMission = await _context.Missions.FindAsync(mission.Id);
                        if (dbMission != null) _context.Missions.Remove(dbMission);

                        _context.FinancialTransactions.Add(new FinancialTransaction
                        {
                            Id = Guid.NewGuid(),
                            PilotId = pilot.Id,
                            Date = DateTime.Now,
                            Type = TransactionType.Income,
                            Category = "Missão",
                            Amount = missionReward,
                            Description = $"Entrega em {destinationIcao} ({mission.Title}) - Rating: {qualityService.GetRating()}",
                            RelatedMissionId = mission.Id.ToString()
                        });
                    }
                    else
                    {
                        mission.OriginICAO = destinationIcao;
                        var dbMission = await _context.Missions.FindAsync(mission.Id);
                        if (dbMission != null)
                        {
                            dbMission.OriginICAO = destinationIcao;
                            var destAirp = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == dbMission.DestinationICAO);
                            var curAirp = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == destinationIcao);
                            if (destAirp != null && curAirp != null)
                            {
                                double newDist = LocationUtils.CalculateDistance(curAirp.Latitude, curAirp.Longitude, destAirp.Latitude, destAirp.Longitude);
                                dbMission.DistanceNM = Math.Round(newDist, 0);
                                mission.DistanceNM = dbMission.DistanceNM;
                            }
                        }
                        missionsRemaining++;
                    }
                }

                // 3. Custos Operacionais: Combustível Consumido (SimConnect) e Taxa de Pouso (MTOW & Classificação)
                double fuelUsed = outcome.FuelUsed;
                string fuelType = aircraft.AircraftType?.FuelType ?? "AVGAS";
                double fuelPrice = FuelPriceService.GetCurrentPrice(fuelType);
                double fuelCost = Math.Round(fuelUsed * fuelPrice, 2);

                if (fuelCost > 0)
                {
                    pilot.Balance -= fuelCost;
                    _context.FinancialTransactions.Add(new FinancialTransaction
                    {
                        Id = Guid.NewGuid(),
                        PilotId = pilot.Id,
                        Date = DateTime.Now,
                        Type = TransactionType.Expense,
                        Category = "Combustível",
                        Amount = fuelCost,
                        Description = $"Combustível consumido ({fuelUsed:F1} GAL {fuelType} @ {fuelPrice:C2}/gal)"
                    });
                }

                // Cálculo da Taxa de Pouso baseada no MTOW e classificação do aeroporto
                double mtowLbs = AircraftWeightSpecs.GetMtow(aircraft.AircraftType?.Model, aircraft.AircraftType);

                double landingFee = CalculateLandingFee(destAirport, mtowLbs);
                if (landingFee > 0)
                {
                    pilot.Balance -= landingFee;
                    _context.FinancialTransactions.Add(new FinancialTransaction
                    {
                        Id = Guid.NewGuid(),
                        PilotId = pilot.Id,
                        Date = DateTime.Now,
                        Type = TransactionType.Expense,
                        Category = "Taxa de Pouso",
                        Amount = landingFee,
                        Description = $"Taxa de Pouso em {destinationIcao} (Tier {airportTier}, MTOW: {mtowLbs:N0} lbs)"
                    });
                }

                pilot.Balance += totalRevenue;
                pilot.ExperiencePoints += totalXP;
                pilot.TotalHours += hours;

                // 4. Registrar Voo no Logbook
                var flight = new Flight
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    OriginICAO = originIcao,
                    DestinationICAO = destinationIcao,
                    DurationHours = hours,
                    DistanceNM = distanceNM,
                    Date = DateTime.Now,
                    Revenue = totalRevenue,
                    XP = totalXP
                };
                _context.Flights.Add(flight);

                await _context.SaveChangesAsync();

                outcome.Success = true;
                outcome.Revenue = totalRevenue;
                outcome.FuelCost = fuelCost;
                outcome.LandingFee = landingFee;
                outcome.XP = totalXP;
                outcome.CompletedMissions = missionsDone;
                outcome.RemainingMissions = missionsRemaining;
                outcome.Message = $"Rating: {qualityService.GetRating()} ({qualityScore:F0}%) | Lucro Líquido: {outcome.NetProfit:C2}";
                return outcome;
            }
            catch (Exception ex)
            {
                outcome.Success = false;
                outcome.Message = ex.Message;
                return outcome;
            }
        }
        public static double CalculateLandingFee(Airport? airport, double mtowLbs)
        {
            if (airport == null || airport.Tier == 4)
            {
                // Pistas remotas, aeródromos rurais e fazendas cobram taxa ZERO
                return 0.0;
            }

            return airport.Tier switch
            {
                // Tier 1: Grandes Hubs Internacionais (taxas elevadas proporcionais ao porte)
                1 => Math.Round(250.0 + (mtowLbs * 0.035), 2),

                // Tier 2: Regionais Médios / Capitais Estaduais
                2 => Math.Round(80.0 + (mtowLbs * 0.015), 2),

                // Tier 3: Aeródromos Municipais / Interior Pavimentado
                3 => Math.Round(25.0 + (mtowLbs * 0.005), 2),

                _ => 0.0
            };
        }
    }
}
