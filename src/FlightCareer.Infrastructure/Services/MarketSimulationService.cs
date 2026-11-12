using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Infrastructure.Services
{
    public class MarketSimulationService : IMarketSimulationService
    {
        private readonly AppDbContext _context;
        private static DateTime _lastTickUtc = DateTime.UtcNow.AddHours(-1);
        private static readonly SemaphoreSlim _simulationLock = new(1, 1);
        private readonly Random _random = new();

        public MarketSimulationService(AppDbContext context)
        {
            _context = context;
        }

        public double CalculateBaseMarketValue(
            AircraftType type,
            double engineCond,
            double tiresCond,
            double airframeCond,
            double avionicsCond,
            int year,
            double airframeHours)
        {
            if (type == null) return 0.0;

            double cMotor = Math.Clamp(engineCond, 0.0, 100.0) / 100.0;
            double cAvionics = Math.Clamp(avionicsCond, 0.0, 100.0) / 100.0;
            double cEstrutura = ((Math.Clamp(airframeCond, 0.0, 100.0) + Math.Clamp(tiresCond, 0.0, 100.0)) / 2.0) / 100.0;

            int currentYear = DateTime.UtcNow.Year;
            int safeYear = year > 1900 ? year : 1995;
            double fAno = Math.Clamp(1.0 - ((currentYear - safeYear) * 0.005), 0.70, 1.05);

            double weightedCondition = (0.45 * cMotor) + (0.30 * cAvionics) + (0.25 * cEstrutura);
            double baseValue = type.PurchasePrice * fAno * weightedCondition;

            // Bônus Overhaul Zero Hora
            if (engineCond >= 95.0)
            {
                baseValue *= 1.12; // +12%
            }

            // Penalidade AOG / Sucata
            if (engineCond < 25.0 || tiresCond < 25.0 || airframeCond < 25.0 || avionicsCond < 25.0)
            {
                baseValue *= 0.75; // -25%
            }

            return Math.Round(Math.Max(5000.0, baseValue), 2);
        }

        public async Task EnsureMarketInitializedAsync()
        {
            await _simulationLock.WaitAsync();
            try
            {
                int activeCount = await _context.MarketListings.CountAsync(l => !l.IsSold);
                if (activeCount > 0)
                {
                    return;
                }

                var aircraftTypes = await _context.AircraftTypes.ToListAsync();
                if (aircraftTypes.Count == 0)
                {
                    return;
                }

                int targetCount = _random.Next(90, 111); // entre 90 e 110
                var newListings = new List<MarketListing>();

                for (int i = 0; i < targetCount; i++)
                {
                    var type = aircraftTypes[_random.Next(aircraftTypes.Count)];
                    var listing = CreateProceduralListing(type);
                    newListings.Add(listing);
                }

                await _context.MarketListings.AddRangeAsync(newListings);
                await _context.SaveChangesAsync();
                _lastTickUtc = DateTime.UtcNow;
            }
            finally
            {
                _simulationLock.Release();
            }
        }

        public async Task ProcessSimulationTicksAsync()
        {
            await _simulationLock.WaitAsync();
            try
            {
                DateTime nowUtc = DateTime.UtcNow;
                TimeSpan elapsed = nowUtc - _lastTickUtc;
                // Limita o processamento acumulado a no máximo 72 horas (3 dias), evitando reset total do mercado ou travamento se o jogador ficar 30 dias sem abrir o app
                int simulationHours = (int)Math.Min(elapsed.TotalHours, 72);
                if (simulationHours < 1)
                {
                    return;
                }

                // Se o tempo decorrido exceder 72h (ex: jogador ficou semanas sem abrir o app),
                // alinha _lastTickUtc para o limite da janela de 72h em relação a nowUtc,
                // garantindo que não fique eternamente atrasado e preserve os minutos quebrados.
                if (elapsed.TotalHours > 72)
                {
                    _lastTickUtc = nowUtc.AddHours(-simulationHours);
                }

                var aircraftTypes = await _context.AircraftTypes.ToListAsync();
                if (aircraftTypes.Count == 0)
                {
                    _lastTickUtc = _lastTickUtc.AddHours(simulationHours);
                    return;
                }

                for (int h = 0; h < simulationHours; h++)
                {
                    var activeListings = await _context.MarketListings
                        .Include(l => l.AircraftType)
                        .Where(l => !l.IsSold)
                        .ToListAsync();

                    // 1. Saídas por Compra NPC
                    foreach (var listing in activeListings)
                    {
                        double asking = listing.AskingPrice > 0 ? listing.AskingPrice : 1.0;
                        double dealRatio = listing.BaseMarketValue / asking;

                        double baseChance = dealRatio switch
                        {
                            >= 1.15 => 0.08,    // 8% por hora
                            >= 0.95 => 0.025,   // 2.5% por hora
                            _ => 0.003          // 0.3% por hora
                        };

                        double categoryMultiplier = GetCategoryMultiplier(listing.CategoryBadge);
                        double finalChance = baseChance * categoryMultiplier;

                        if (_random.NextDouble() < finalChance)
                        {
                            listing.IsSold = true;
                            listing.SoldAtUtc = DateTime.UtcNow;

                            if (listing.IsPlayerOwned && listing.PilotId.HasValue)
                            {
                                var pilot = await _context.Pilots
                                    .Include(p => p.Hangar)
                                    .FirstOrDefaultAsync(p => p.Id == listing.PilotId.Value);

                                if (pilot != null)
                                {
                                    double netAmount = Math.Round(listing.AskingPrice * 0.92, 2); // 8% taxa da corretora
                                    pilot.Balance += netAmount;

                                    _context.FinancialTransactions.Add(new FinancialTransaction
                                    {
                                        Id = Guid.NewGuid(),
                                        PilotId = pilot.Id,
                                        Date = DateTime.Now,
                                        Amount = netAmount,
                                        Type = TransactionType.Income,
                                        Category = "Venda de Aeronave",
                                        Description = $"Venda de Aeronave no Mercado: {listing.AircraftType?.Model ?? "Aeronave"} ({listing.Prefix})"
                                    });

                                    if (listing.PlayerAircraftId.HasValue)
                                    {
                                        var pa = await _context.PilotAircrafts
                                            .FirstOrDefaultAsync(a => a.Id == listing.PlayerAircraftId.Value);

                                        if (pa != null)
                                        {
                                            if (pilot.ActiveAircraftId == pa.Id)
                                            {
                                                pilot.ActiveAircraftId = null;
                                            }
                                            _context.PilotAircrafts.Remove(pa);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 2. Vendedores NPC Impacientes (Redução de Preço)
                    foreach (var listing in activeListings.Where(l => !l.IsSold && !l.IsPlayerOwned))
                    {
                        double ageHours = (DateTime.UtcNow - listing.ListedAtUtc).TotalHours;

                        if (ageHours >= 72.0 && listing.PriceDropsCount < 3)
                        {
                            bool canDrop = listing.LastPriceDropAtUtc == null ||
                                           (DateTime.UtcNow - listing.LastPriceDropAtUtc.Value).TotalHours >= 24.0;

                            if (canDrop)
                            {
                                double dropPercent = 0.03 + (_random.NextDouble() * 0.04); // 3% a 7%
                                listing.AskingPrice = Math.Round(listing.AskingPrice * (1.0 - dropPercent), 2);
                                listing.PriceDropsCount++;
                                listing.LastPriceDropAtUtc = DateTime.UtcNow;
                            }
                        }
                        else if (listing.PriceDropsCount >= 3 && ageHours >= 360.0) // 15 dias encalhado
                        {
                            _context.MarketListings.Remove(listing);
                        }
                    }

                    // 3. Reposição de Estoque (Manter entre 90 e 110)
                    int currentActive = activeListings.Count(l => !l.IsSold);
                    if (currentActive < 95)
                    {
                        int toAdd = _random.Next(95 - currentActive, 110 - currentActive + 1);
                        var replenishments = new List<MarketListing>();
                        for (int i = 0; i < toAdd; i++)
                        {
                            var type = aircraftTypes[_random.Next(aircraftTypes.Count)];
                            replenishments.Add(CreateProceduralListing(type));
                        }
                        await _context.MarketListings.AddRangeAsync(replenishments);
                    }
                }

                // 4. Limpeza de Fantasmas Antigos
                // Manter apenas até 5 aeronaves vendidas nas últimas 24h para exibição na UI; deletar vendidas mais antigas
                var cutoff24h = DateTime.UtcNow.AddHours(-24);
                var oldSold = await _context.MarketListings
                    .Where(l => l.IsSold && (l.SoldAtUtc == null || l.SoldAtUtc < cutoff24h))
                    .ToListAsync();

                if (oldSold.Count > 0)
                {
                    _context.MarketListings.RemoveRange(oldSold);
                }

                var recentSold = await _context.MarketListings
                    .Where(l => l.IsSold && l.SoldAtUtc >= cutoff24h)
                    .OrderByDescending(l => l.SoldAtUtc)
                    .ToListAsync();

                if (recentSold.Count > 5)
                {
                    var excessSold = recentSold.Skip(5).ToList();
                    _context.MarketListings.RemoveRange(excessSold);
                }

                await _context.SaveChangesAsync();
                // Avança o LastTickUtc apenas pelas horas inteiras consumidas (preserva os minutos quebrados para o próximo tick)
                _lastTickUtc = _lastTickUtc.AddHours(simulationHours);
            }
            finally
            {
                _simulationLock.Release();
            }
        }

        public async Task<List<MarketListing>> GetActiveListingsAsync()
        {
            return await _context.MarketListings
                .Include(l => l.AircraftType)
                .Where(l => !l.IsSold)
                .OrderBy(l => l.AskingPrice)
                .ToListAsync();
        }

        public async Task<List<MarketListing>> GetRecentlySoldListingsAsync(int count = 5)
        {
            var cutoff24h = DateTime.UtcNow.AddHours(-24);
            return await _context.MarketListings
                .Include(l => l.AircraftType)
                .Where(l => l.IsSold && l.SoldAtUtc >= cutoff24h)
                .OrderByDescending(l => l.SoldAtUtc)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<MarketListing>> GetPlayerActiveListingsAsync(string pilotName)
        {
            var pilot = await _context.Pilots.FirstOrDefaultAsync(p => p.Name == pilotName);
            if (pilot == null) return new List<MarketListing>();

            return await _context.MarketListings
                .Include(l => l.AircraftType)
                .Where(l => l.PilotId == pilot.Id && !l.IsSold)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, PilotAircraft? Aircraft)> BuyMarketListingAsync(Guid marketListingId, string pilotName)
        {
            await _simulationLock.WaitAsync();
            try
            {
                var listing = await _context.MarketListings
                    .Include(l => l.AircraftType)
                    .FirstOrDefaultAsync(l => l.Id == marketListingId);

                if (listing == null)
                {
                    return (false, "Anúncio não encontrado.", null);
                }

                if (listing.IsSold)
                {
                    return (false, "Esta aeronave já foi vendida por outro comprador!", null);
                }

                var pilot = await _context.Pilots
                    .Include(p => p.Hangar)
                    .FirstOrDefaultAsync(p => p.Name == pilotName);

                if (pilot == null)
                {
                    return (false, "Piloto não encontrado.", null);
                }

                if (listing.IsPlayerOwned && listing.PilotId == pilot.Id)
                {
                    return (false, "Você não pode comprar sua própria aeronave anunciada.", null);
                }

                if (pilot.Balance < listing.AskingPrice)
                {
                    return (false, $"Saldo insuficiente. Você tem ${pilot.Balance:N2} e o valor é ${listing.AskingPrice:N2}.", null);
                }

                // 1. Deduzir saldo
                pilot.Balance -= listing.AskingPrice;

                // 2. Criar e adicionar aeronave ao hangar do comprador
                var newAircraft = new PilotAircraft
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    AircraftTypeId = listing.AircraftTypeId,
                    PurchaseDate = DateTime.Now,
                    Prefix = listing.Prefix,
                    EngineCondition = listing.EngineCondition,
                    TiresCondition = listing.TiresCondition,
                    AirframeCondition = listing.AirframeCondition,
                    AvionicsCondition = listing.AvionicsCondition,
                    CurrentAirframeHours = listing.AirframeHours,
                    FuelAmount = listing.FuelAmount,
                    CurrentLocationICAO = pilot.CurrentAirportICAO,
                    EngineTboRemainingHours = Math.Max(100.0, listing.TboHours - listing.TsmoHours)
                };

                _context.PilotAircrafts.Add(newAircraft);

                // 3. Registrar transação financeira de despesa para o comprador
                _context.FinancialTransactions.Add(new FinancialTransaction
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    Date = DateTime.Now,
                    Amount = listing.AskingPrice,
                    Type = TransactionType.Expense,
                    Category = "Aeronave",
                    Description = $"Compra de Aeronave: {listing.AircraftType?.Model ?? "Aeronave"} ({listing.Prefix})"
                });

                // 4. Se pertencia a outro jogador, transferir recursos e remover do vendedor
                if (listing.IsPlayerOwned && listing.PilotId.HasValue)
                {
                    var seller = await _context.Pilots
                        .Include(p => p.Hangar)
                        .FirstOrDefaultAsync(p => p.Id == listing.PilotId.Value);

                    if (seller != null)
                    {
                        double netSellerAmount = Math.Round(listing.AskingPrice * 0.92, 2);
                        seller.Balance += netSellerAmount;

                        _context.FinancialTransactions.Add(new FinancialTransaction
                        {
                            Id = Guid.NewGuid(),
                            PilotId = seller.Id,
                            Date = DateTime.Now,
                            Amount = netSellerAmount,
                            Type = TransactionType.Income,
                            Category = "Venda de Aeronave",
                            Description = $"Venda de Aeronave para Jogador: {listing.AircraftType?.Model ?? "Aeronave"} ({listing.Prefix})"
                        });

                        if (listing.PlayerAircraftId.HasValue)
                        {
                            var sellerAircraft = await _context.PilotAircrafts
                                .FirstOrDefaultAsync(a => a.Id == listing.PlayerAircraftId.Value);

                            if (sellerAircraft != null)
                            {
                                if (seller.ActiveAircraftId == sellerAircraft.Id)
                                {
                                    seller.ActiveAircraftId = null;
                                }
                                _context.PilotAircrafts.Remove(sellerAircraft);
                            }
                        }
                    }
                }

                // 5. Marcar listing como vendida
                listing.IsSold = true;
                listing.SoldAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return (true, $"Aeronave {listing.Prefix} adquirida com sucesso por ${listing.AskingPrice:N2}!", newAircraft);
            }
            finally
            {
                _simulationLock.Release();
            }
        }

        public async Task<(bool Success, string Message, MarketListing? Listing)> ListPlayerAircraftAsync(Guid playerAircraftId, string pilotName, double askingPrice)
        {
            await _simulationLock.WaitAsync();
            try
            {
                var pilot = await _context.Pilots.FirstOrDefaultAsync(p => p.Name == pilotName);
                if (pilot == null)
                {
                    return (false, "Piloto não encontrado.", null);
                }

                var aircraft = await _context.PilotAircrafts
                    .Include(a => a.AircraftType)
                    .FirstOrDefaultAsync(a => a.Id == playerAircraftId);

                if (aircraft == null || aircraft.AircraftType == null)
                {
                    return (false, "Aeronave não encontrada no hangar.", null);
                }

                if (aircraft.PilotId != pilot.Id)
                {
                    return (false, "Esta aeronave não pertence ao piloto.", null);
                }

                var existingListing = await _context.MarketListings
                    .FirstOrDefaultAsync(l => l.PlayerAircraftId == playerAircraftId && !l.IsSold);

                if (existingListing != null)
                {
                    return (false, "Esta aeronave já possui um anúncio ativo no mercado.", null);
                }

                int year = aircraft.PurchaseDate != default && aircraft.PurchaseDate.Year > 1900 ? aircraft.PurchaseDate.Year : 2018;
                double baseVal = CalculateBaseMarketValue(
                    aircraft.AircraftType,
                    aircraft.EngineCondition,
                    aircraft.TiresCondition,
                    aircraft.AirframeCondition,
                    aircraft.AvionicsCondition,
                    year,
                    aircraft.CurrentAirframeHours);

                double finalAskingPrice = askingPrice > 0 ? askingPrice : baseVal;

                var listing = new MarketListing
                {
                    Id = Guid.NewGuid(),
                    AircraftTypeId = aircraft.AircraftTypeId,
                    Prefix = aircraft.Prefix,
                    EngineCondition = aircraft.EngineCondition,
                    TiresCondition = aircraft.TiresCondition,
                    AirframeCondition = aircraft.AirframeCondition,
                    AvionicsCondition = aircraft.AvionicsCondition,
                    AirframeHours = aircraft.CurrentAirframeHours,
                    YearOfManufacture = year,
                    TboHours = (aircraft.AircraftType.FuelType?.Contains("JET", StringComparison.OrdinalIgnoreCase) == true ||
                                aircraft.AircraftType.Type?.Contains("Turboprop", StringComparison.OrdinalIgnoreCase) == true) ? 3600.0 : 1800.0,
                    TsmoHours = Math.Max(0, 1800.0 - aircraft.EngineTboRemainingHours),
                    StructuralIncidentHistory = "Aeronave operada pela frota da companhia",
                    FuelAmount = aircraft.FuelAmount,
                    BaseMarketValue = baseVal,
                    AskingPrice = finalAskingPrice,
                    OriginalAskingPrice = finalAskingPrice,
                    PriceDropsCount = 0,
                    ListedAtUtc = DateTime.UtcNow,
                    IsSold = false,
                    IsPlayerOwned = true,
                    PlayerAircraftId = playerAircraftId,
                    PilotId = pilot.Id,
                    CategoryBadge = DetermineCategoryBadge(aircraft.AircraftType)
                };

                _context.MarketListings.Add(listing);
                await _context.SaveChangesAsync();

                return (true, $"Aeronave {aircraft.Prefix} anunciada no mercado com sucesso!", listing);
            }
            finally
            {
                _simulationLock.Release();
            }
        }

        public async Task<(bool Success, string Message)> CancelPlayerListingAsync(Guid marketListingId, string pilotName)
        {
            await _simulationLock.WaitAsync();
            try
            {
                var pilot = await _context.Pilots.FirstOrDefaultAsync(p => p.Name == pilotName);
                if (pilot == null)
                {
                    return (false, "Piloto não encontrado.");
                }

                var listing = await _context.MarketListings.FirstOrDefaultAsync(l => l.Id == marketListingId);
                if (listing == null)
                {
                    return (false, "Anúncio não encontrado.");
                }

                if (listing.PilotId != pilot.Id)
                {
                    return (false, "Você não tem permissão para cancelar este anúncio.");
                }

                if (listing.IsSold)
                {
                    return (false, "A aeronave já foi vendida e não pode ser cancelada.");
                }

                _context.MarketListings.Remove(listing);
                await _context.SaveChangesAsync();

                return (true, "Anúncio cancelado com sucesso. A aeronave permanece em seu hangar.");
            }
            finally
            {
                _simulationLock.Release();
            }
        }

        private MarketListing CreateProceduralListing(AircraftType type)
        {
            double engine = _random.Next(40, 101);
            double tires = _random.Next(35, 101);
            double airframe = _random.Next(50, 101);
            double avionics = _random.Next(45, 101);

            int year = _random.Next(1975, DateTime.UtcNow.Year - 1);
            double hours = _random.Next(120, 9500);

            double tbo = (type.FuelType?.Contains("JET", StringComparison.OrdinalIgnoreCase) == true ||
                          type.Type?.Contains("Turboprop", StringComparison.OrdinalIgnoreCase) == true) ? 3600.0 : 1800.0;
            double tsmo = Math.Round(_random.NextDouble() * Math.Min(tbo, hours % tbo == 0 ? tbo : hours % tbo), 1);

            double fuel = Math.Round(_random.NextDouble() * type.FuelTankGallons, 1);

            string incident = airframe switch
            {
                < 65 => "Histórico de pouso duro e substituição de longarina registrado em diário",
                < 80 => "Pequeno reparo estrutural de bordo de ataque registrado em caderneta",
                _ => "Sem histórico de incidentes estruturais"
            };

            double baseValue = CalculateBaseMarketValue(type, engine, tires, airframe, avionics, year, hours);

            // Variação de ágio/deságio (0.82 a 1.25)
            double markupRatio = 0.82 + (_random.NextDouble() * (1.25 - 0.82));
            double askingPrice = Math.Round(baseValue * markupRatio, 2);

            return new MarketListing
            {
                Id = Guid.NewGuid(),
                AircraftTypeId = type.Id,
                AircraftType = type,
                Prefix = GeneratePrefix(),
                EngineCondition = engine,
                TiresCondition = tires,
                AirframeCondition = airframe,
                AvionicsCondition = avionics,
                AirframeHours = hours,
                YearOfManufacture = year,
                TboHours = tbo,
                TsmoHours = tsmo,
                StructuralIncidentHistory = incident,
                FuelAmount = fuel,
                BaseMarketValue = baseValue,
                AskingPrice = askingPrice,
                OriginalAskingPrice = askingPrice,
                PriceDropsCount = 0,
                ListedAtUtc = DateTime.UtcNow.AddHours(-_random.Next(1, 120)),
                IsSold = false,
                SoldAtUtc = null,
                IsPlayerOwned = false,
                PlayerAircraftId = null,
                PilotId = null,
                CategoryBadge = DetermineCategoryBadge(type)
            };
        }

        private string DetermineCategoryBadge(AircraftType type)
        {
            var model = type.Model?.ToUpperInvariant() ?? string.Empty;
            var typeDesc = type.Type?.ToUpperInvariant() ?? string.Empty;
            var fuel = type.FuelType?.ToUpperInvariant() ?? string.Empty;

            if (fuel.Contains("JET") && (type.CruiseSpeedKts > 320 || model.Contains("CITATION") || model.Contains("A320") || model.Contains("737") || model.Contains("CJ4") || typeDesc.Contains("JET")))
            {
                return "Jato";
            }

            if (typeDesc.Contains("TURBOPROP") || model.Contains("KING AIR") || model.Contains("CARAVAN") || model.Contains("TBM") || model.Contains("PC-12"))
            {
                return "Turboélice";
            }

            if (typeDesc.Contains("MEP") || model.Contains("BARON") || model.Contains("SENECA") || model.Contains("TWIN") || model.Contains("DA62") || model.Contains("DUCHESS"))
            {
                return "Bimotor Pistão";
            }

            if (model.Contains("X-CUB") || model.Contains("SAVANNAH") || model.Contains("STOL") || type.MinRunwayLength < 800)
            {
                return "STOL / Utilitário";
            }

            return "Monomotor Pistão";
        }

        private double GetCategoryMultiplier(string categoryBadge)
        {
            if (string.IsNullOrWhiteSpace(categoryBadge)) return 1.0;

            if (categoryBadge.Contains("Monomotor", StringComparison.OrdinalIgnoreCase) ||
                categoryBadge.Contains("STOL", StringComparison.OrdinalIgnoreCase))
            {
                return 1.3;
            }

            if (categoryBadge.Contains("Bimotor", StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }

            if (categoryBadge.Contains("Turboélice", StringComparison.OrdinalIgnoreCase))
            {
                return 0.8;
            }

            if (categoryBadge.Contains("Jato", StringComparison.OrdinalIgnoreCase))
            {
                return 0.6;
            }

            return 1.0;
        }

        private string GeneratePrefix()
        {
            string[] brPrefixes = { "PP-", "PT-", "PR-", "PS-" };
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string letters = new string(Enumerable.Repeat(chars, 3).Select(s => s[_random.Next(s.Length)]).ToArray());
            return brPrefixes[_random.Next(brPrefixes.Length)] + letters;
        }
    }
}
