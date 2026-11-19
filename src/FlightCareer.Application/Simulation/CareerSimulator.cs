using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Replay;
using FlightCareer.Domain.Replay.Synthetic;
using FlightCareer.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Application.Simulation
{
    public sealed class CareerSimulationOptions
    {
        public IReadOnlyList<string> AircraftModels { get; set; } = new List<string>();
        public int BotsPerAircraft { get; set; } = 5;
        public int Days { get; set; } = 90;
        public double HoursPerDay { get; set; } = 2.0;
        public int Seed { get; set; } = 1;
        public string? InitialHub { get; set; }
        public string Policy { get; set; } = "best"; // "best" ou "random"
        public bool FastMode { get; set; } = false;
        public string? OutputDirectory { get; set; }
        public Action<string>? LogCallback { get; set; }
    }

    public sealed record FlightRecord(
        string Bot,
        string Aircraft,
        int Day,
        string Origin,
        string Destination,
        int DestTier,
        string Subcategory,
        double DistanceNM,
        double Hours,
        int Passengers,
        double PayloadLbs,
        double Revenue,
        double FuelCost,
        double LandingFee,
        double PassengerFee,
        double HandlingFee,
        double NavFee,
        double TotalFees,
        double EstimatedOvernightParking,
        string Failure);

    public sealed record MaintenanceRecord(
        string Bot,
        string Aircraft,
        int Day,
        double AirframeHours,
        string Service,
        double Cost,
        double DowntimeHours);

    public sealed record DailyRecord(
        string Bot,
        string Aircraft,
        int Day,
        double HoursFlown,
        int Flights,
        double Revenue,
        double FuelCost,
        double Fees,
        double MaintenanceCost,
        double InsuranceCost,
        double TotalExpenses,
        double DailyProfit,
        double Balance,
        bool IsAvailable,
        bool HasBankrupted);

    public sealed class CareerSimulationResult
    {
        public IReadOnlyList<FlightRecord> Flights { get; init; } = Array.Empty<FlightRecord>();
        public IReadOnlyList<MaintenanceRecord> Maintenance { get; init; } = Array.Empty<MaintenanceRecord>();
        public IReadOnlyList<DailyRecord> Daily { get; init; } = Array.Empty<DailyRecord>();
        public string SummaryMarkdown { get; init; } = string.Empty;
        public string OutputDirectory { get; init; } = string.Empty;
    }

    /// <summary>
    /// Simulador puro de carreira com bots para análise de economia e balanceamento (ECON-SIM).
    /// </summary>
    public sealed class CareerSimulator
    {
        private readonly Func<IAppDbContext> _dbFactory;
        private readonly ComponentCatalog _componentCatalog;
        private readonly PartCatalog _partCatalog;
        private readonly WearRuleCatalog _wearRuleCatalog;
        private readonly AircraftLimitResolver _limitResolver;
        private readonly IHubDataService _hubDataService;

        public CareerSimulator(
            Func<IAppDbContext> dbFactory,
            ComponentCatalog? componentCatalog = null,
            PartCatalog? partCatalog = null,
            WearRuleCatalog? wearRuleCatalog = null,
            AircraftLimitResolver? limitResolver = null,
            IHubDataService? hubDataService = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _componentCatalog = componentCatalog ?? ComponentCatalog.LoadDefault();
            _partCatalog = partCatalog ?? PartCatalog.LoadDefault();
            _wearRuleCatalog = wearRuleCatalog ?? WearRuleCatalog.LoadDefault();
            _limitResolver = limitResolver ?? new AircraftLimitResolver(Array.Empty<AircraftLimitProfile>(), new Dictionary<string, IReadOnlyList<AircraftLimit>>());
            _hubDataService = hubDataService ?? new HubDataService();
        }

        public async Task<CareerSimulationResult> RunAsync(CareerSimulationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.AircraftModels == null || options.AircraftModels.Count == 0)
                throw new ArgumentException("Nenhuma aeronave informada para a simulação.");

            var outDir = options.OutputDirectory;
            if (string.IsNullOrWhiteSpace(outDir))
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                outDir = Path.Combine(localAppData, "FlightCareer", "career-sim", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            }
            Directory.CreateDirectory(outDir);

            var rnd = new SystemRandomSource(options.Seed);
            var startDate = new DateTime(2024, 1, 1);

            var flightsList = new List<FlightRecord>();
            var maintenanceList = new List<MaintenanceRecord>();
            var dailyList = new List<DailyRecord>();

            // Pré-carrega aeroportos e tipos
            List<Airport> allAirports;
            List<AircraftType> allTypes;
            var loadDb = _dbFactory();
            try
            {
                allAirports = await loadDb.Airports.ToListAsync();
                allTypes = await loadDb.AircraftTypes.ToListAsync();
            }
            finally
            {
                (loadDb as IDisposable)?.Dispose();
            }

            var airportLookup = allAirports.ToDictionary(a => a.ICAO, a => a, StringComparer.OrdinalIgnoreCase);

            // Determina hub inicial padrão se não fornecido
            Airport defaultHub;
            if (!string.IsNullOrWhiteSpace(options.InitialHub) && airportLookup.TryGetValue(options.InitialHub, out var customHub))
            {
                defaultHub = customHub;
            }
            else
            {
                // Busca um aeroporto Tier 2 representativo (ex.: SBRP, SBLO, SBMG, SBJD, SBJR, SBUL, SBPA)
                defaultHub = allAirports.FirstOrDefault(a => a.Tier == 2 && (a.ICAO.StartsWith("SB") || a.ICAO.StartsWith("SD")))
                    ?? allAirports.FirstOrDefault(a => a.Tier == 2)
                    ?? allAirports.First(a => a.Tier <= 2);
            }

            options.LogCallback?.Invoke($"[ECON-SIM] Iniciando simulação: {options.AircraftModels.Count} modelo(s), {options.BotsPerAircraft} bot(s)/modelo, {options.Days} dia(s), {options.HoursPerDay} h/dia, modo {(options.FastMode ? "RÁPIDO (--fast)" : "COMPLETO")}");

            foreach (var modelName in options.AircraftModels)
            {
                var aType = allTypes.FirstOrDefault(t => string.Equals(t.Model, modelName, StringComparison.OrdinalIgnoreCase))
                    ?? allTypes.FirstOrDefault(t => t.Model.Contains(modelName, StringComparison.OrdinalIgnoreCase));

                if (aType == null)
                {
                    options.LogCallback?.Invoke($"[ECON-SIM] AVISO: Modelo '{modelName}' não encontrado no banco semeado; ignorado.");
                    continue;
                }

                var profile = _limitResolver.FindById(aType.Model) ?? _limitResolver.FindProfile(aType.Model, aType.Model);
                var features = profile?.Features ?? Array.Empty<string>();
                int engineCount = profile?.EngineCount ?? 1;
                var engineClass = _componentCatalog.ResolveEngineClass(aType);
                bool isPiston = engineClass == EngineClass.Piston;
                double defaultTbo = _componentCatalog.Get(ComponentKind.Engine).LifeHoursFor(engineClass);

                // Hub inicial para este modelo (se a aeronave exige pista longa como B737, garante Tier compatível)
                Airport botStartAirport = defaultHub;
                if (aType.MinRunwayLength >= 4500 && botStartAirport.Tier > 2)
                {
                    botStartAirport = allAirports.FirstOrDefault(a => a.Tier <= 2) ?? defaultHub;
                }
                if (aType.MinRunwayLength >= 7000 && botStartAirport.Tier > 1)
                {
                    botStartAirport = allAirports.FirstOrDefault(a => a.Tier == 1) ?? botStartAirport;
                }

                for (int botIdx = 1; botIdx <= options.BotsPerAircraft; botIdx++)
                {
                    string botName = $"Bot-{aType.Model}-{botIdx}";
                    double balance = 1_000_000.0; // Saldo inicial generoso
                    bool bankrupted = false;
                    int? bankruptcyDay = null;

                    var currentAirport = botStartAirport;
                    double airframeHours = 0.0;
                    double hoursUntil50h = 12.0;
                    double hoursUntil100h = 25.0;
                    double hoursUntil500h = 125.0;
                    double tboRemaining = defaultTbo;
                    int totalLandings = 0;
                    double remainingDowntimeHours = 0.0;

                    // Cria componentes
                    var components = _componentCatalog.Components
                        .Where(c => c.AppliesTo.Contains(engineClass))
                        .Select(c => new AircraftComponent
                        {
                            Id = Guid.NewGuid(),
                            Kind = c.Kind,
                            Health = 100.0,
                            HoursSinceOverhaul = 0.0
                        }).ToList();

                    // Cria peças aplicáveis
                    var applicableDefs = _partCatalog.PartsFor(features);
                    var parts = new List<AircraftPart>();
                    foreach (var pDef in applicableDefs)
                    {
                        int instances = pDef.PerEngine ? Math.Max(1, engineCount) : 1;
                        for (int eng = 1; eng <= instances; eng++)
                        {
                            parts.Add(new AircraftPart
                            {
                                Id = Guid.NewGuid(),
                                PartId = pDef.Id,
                                EngineIndex = eng,
                                Health = 100.0,
                                HoursSinceReplacement = 0.0,
                                CyclesSinceReplacement = 0,
                                IsBroken = false
                            });
                        }
                    }

                    var pilotAircraft = new PilotAircraft
                    {
                        Id = Guid.NewGuid(),
                        AircraftTypeId = aType.Id,
                        AircraftType = aType,
                        CurrentAirframeHours = 0.0,
                        EngineTboRemainingHours = defaultTbo,
                        HoursUntilNext50hInspection = 12.0,
                        HoursUntilNext100hInspection = 25.0,
                        HoursUntilNext500hInspection = 125.0
                    };

                    // Sincroniza componentes a partir das peças se houver peças
                    if (parts.Count > 0)
                    {
                        PartComponentBridge.SyncComponentsFromParts(pilotAircraft, components, parts, _partCatalog);
                    }

                    var failureEngine = new FailureEngine(_componentCatalog, engineClass, rnd);

                    for (int day = 1; day <= options.Days; day++)
                    {
                        var simulatedDate = startDate.AddDays(day - 1);
                        double dayRevenue = 0.0;
                        double dayFuelCost = 0.0;
                        double dayFeesCost = 0.0;
                        double dayMaintCost = 0.0;
                        double dayInsuranceCost = 0.0;
                        double dayHoursFlown = 0.0;
                        int dayFlights = 0;

                        // 1. Seguro a cada 30 dias (dias 1, 31, 61...)
                        if ((day - 1) % 30 == 0)
                        {
                            double premium = EconomyServices.CalculateMonthlyPremium(aType.PurchasePrice);
                            balance -= premium;
                            dayInsuranceCost += premium;
                        }

                        // 2. Avalia disponibilidade por downtime prévio
                        bool isAvailable = true;
                        if (remainingDowntimeHours >= 24.0)
                        {
                            remainingDowntimeHours -= 24.0;
                            isAvailable = false;
                        }
                        else if (remainingDowntimeHours > 0)
                        {
                            // Consome parte do dia em manutenção
                            remainingDowntimeHours = 0.0;
                        }

                        if (isAvailable)
                        {
                            // Loop de voos do dia
                            while (dayHoursFlown < options.HoursPerDay)
                            {
                                // Gera missões a partir do aeroporto atual
                                IEnumerable<Mission> availableMissions;
                                var mgDb = _dbFactory();
                                try
                                {
                                    var mg = new MissionGenerator(mgDb, _hubDataService, rnd);
                                    availableMissions = await mg.GenerateMissionsAsync(currentAirport, simulatedDate);
                                }
                                finally
                                {
                                    (mgDb as IDisposable)?.Dispose();
                                }

                                // Filtra missões compatíveis com a aeronave e configuração de cabine
                                var compatible = new List<Mission>();
                                foreach (var m in availableMissions)
                                {
                                    if (!CabinConversion.IsMissionCompatible(pilotAircraft.Configuration, aType.MaxPax, aType.MaxPayloadLbs, m.PassengerCount, m.PayloadLbs, m.LuggageWeightLbs, m.CargoWeightLbs)) continue;
                                    if (m.DistanceNM > aType.RangeNM) continue;

                                    if (!airportLookup.TryGetValue(m.DestinationICAO, out var destAp)) continue;

                                    // Pista / Tier do destino
                                    if (aType.MinRunwayLength >= 7000 && destAp.Tier > 1) continue;
                                    if (aType.MinRunwayLength >= 4500 && destAp.Tier > 2) continue;
                                    if (aType.MinRunwayLength >= 2500 && destAp.Tier > 3) continue;

                                    compatible.Add(m);
                                }

                                if (compatible.Count == 0)
                                {
                                    // Sem contratos compatíveis neste aeroporto hoje
                                    break;
                                }

                                // Escolha pela política
                                double speed = aType.CruiseSpeedKts > 0 ? aType.CruiseSpeedKts : 110.0;
                                Mission chosen;
                                if (options.Policy.Equals("random", StringComparison.OrdinalIgnoreCase))
                                {
                                    int idx = (int)(rnd.NextDouble() * compatible.Count);
                                    if (idx >= compatible.Count) idx = compatible.Count - 1;
                                    chosen = compatible[idx];
                                }
                                else // "best"
                                {
                                    chosen = compatible
                                        .OrderByDescending(m => m.RewardMoney / Math.Max(0.2, (m.DistanceNM / speed) + 0.25))
                                        .First();
                                }

                                double flightDuration = Math.Max(0.2, (chosen.DistanceNM / speed) + 0.25);
                                var destAirport = airportLookup[chosen.DestinationICAO];

                                // Marca missão como aceita no banco
                                var accDb = _dbFactory();
                                try
                                {
                                    var dbMission = await accDb.Missions.FindAsync(chosen.Id);
                                    if (dbMission != null)
                                    {
                                        dbMission.IsAccepted = true;
                                        await accDb.SaveChangesAsync();
                                    }
                                }
                                finally
                                {
                                    (accDb as IDisposable)?.Dispose();
                                }

                                // Combustível
                                double fuelUsed = aType.FuelConsumptionGPH * flightDuration;
                                double fuelPrice = FuelPriceService.GetCurrentPrice(aType.FuelType ?? "AVGAS", destAirport.Tier, simulatedDate);
                                double fuelCost = Math.Round(fuelUsed * fuelPrice, 2);

                                // Taxas no destino via GroundServiceCalculator
                                double mtowLbs = AircraftWeightSpecs.GetMtow(aType.Model, aType);
                                var feeCtx = new FeeContext(
                                    destAirport.Tier,
                                    destAirport.ICAO,
                                    mtowLbs,
                                    chosen.PassengerCount,
                                    hasMission: true,
                                    parkedGameHours: 0.0);

                                var feeLines = GroundServiceCalculator.Calculate(feeCtx);
                                double landingFee = feeLines.FirstOrDefault(f => f.Code == "LANDING")?.Total ?? 0.0;
                                double paxFee = feeLines.FirstOrDefault(f => f.Code == "PAX_FEE")?.Total ?? 0.0;
                                double handlingFee = feeLines.FirstOrDefault(f => f.Code == "HANDLING")?.Total ?? 0.0;
                                double navFee = feeLines.FirstOrDefault(f => f.Code == "NAV_TERMINAL")?.Total ?? 0.0;
                                double totalFees = landingFee + paxFee + handlingFee + navFee;

                                // Estacionamento estimado de pernoite (sem deduzir do saldo do voo)
                                double overnightHours = Math.Clamp(24.0 - flightDuration, 0.0, 24.0);
                                var parkCtx = new FeeContext(
                                    destAirport.Tier,
                                    destAirport.ICAO,
                                    mtowLbs,
                                    0,
                                    hasMission: false,
                                    parkedGameHours: overnightHours);
                                double estimatedParking = GroundServiceCalculator.Calculate(parkCtx).FirstOrDefault(f => f.Code == "PARKING")?.Total ?? 0.0;

                                // Aplica desgaste
                                if (options.FastMode || parts.Count == 0)
                                {
                                    // Modo rápido (--fast): desgaste natural das peças/componentes
                                    foreach (var p in parts)
                                    {
                                        var pDef = _partCatalog.Get(p.PartId);
                                        if (pDef != null && pDef.LifeHours > 0)
                                        {
                                            double ratePerHour = (70.0 / pDef.LifeHours) * PartEconomySimulator.TypicalStressMultiplier;
                                            p.HoursSinceReplacement += flightDuration;
                                            p.Health = Math.Clamp(p.Health - (ratePerHour * flightDuration), 0.0, 100.0);
                                        }
                                        if (pDef != null && pDef.LifeCycles > 0)
                                        {
                                            p.CyclesSinceReplacement += 1;
                                            double cycleLoss = 70.0 / pDef.LifeCycles.Value;
                                            p.Health = Math.Clamp(p.Health - cycleLoss, 0.0, 100.0);
                                        }
                                    }

                                    if (parts.Count > 0)
                                    {
                                        PartComponentBridge.SyncComponentsFromParts(pilotAircraft, components, parts, _partCatalog);
                                    }
                                    else
                                    {
                                        // Componentes puros para modelos sem peças
                                        foreach (var c in components)
                                        {
                                            var cDef = _componentCatalog.Components.FirstOrDefault(d => d.Kind == c.Kind);
                                            double life = cDef?.LifeHoursFor(engineClass) ?? 2000.0;
                                            double rate = (70.0 / life) * PartEconomySimulator.TypicalStressMultiplier;
                                            c.Health = Math.Clamp(c.Health - (rate * flightDuration), 0.0, 100.0);
                                            c.HoursSinceOverhaul += flightDuration;
                                        }
                                    }
                                }
                                else
                                {
                                    // Modo completo: executa simulação via SyntheticFlightGenerator e FlightReplayer
                                    var effProfile = profile ?? new AircraftLimitProfile(
                                        aType.Model,
                                        engineClass.ToString(),
                                        1,
                                        new[] { aType.Model },
                                        new[] { aType.Model },
                                        Array.Empty<AircraftLimit>(),
                                        features,
                                        engineCount);
                                    var synthPlan = SyntheticFlightPlan.CreateNormal(Math.Clamp(flightDuration * 3600.0, 180.0, 600.0));
                                    var synthScenario = new AircraftScenarioInput(effProfile, null, features, engineCount, aType.Model);
                                    var synthGen = new SyntheticFlightGenerator(_limitResolver);
                                    var synthFlight = synthGen.Generate(synthScenario, synthPlan, rnd);

                                    var replayer = new FlightReplayer(_partCatalog, _wearRuleCatalog, _limitResolver);
                                    var repResult = replayer.Run(
                                        synthFlight.Frames,
                                        synthFlight.Touchdowns,
                                        effProfile,
                                        features,
                                        engineCount,
                                        initialParts: parts,
                                        activationMode: ReplayActivationMode.FromStart);

                                    // Atualiza estado das peças
                                    var repLookup = repResult.Parts.ToDictionary(p => (p.PartId, p.EngineIndex));
                                    foreach (var p in parts)
                                    {
                                        if (repLookup.TryGetValue((p.PartId, p.EngineIndex), out var repPart))
                                        {
                                            p.Health = repPart.FinalHealth;
                                            p.HoursSinceReplacement = repPart.HoursSinceReplacement;
                                            p.CyclesSinceReplacement = repPart.CyclesSinceReplacement;
                                            p.IsBroken = repPart.IsBroken;
                                        }
                                    }

                                    PartComponentBridge.SyncComponentsFromParts(pilotAircraft, components, parts, _partCatalog);
                                }

                                // Sorteio de panes
                                string failureDesc = "Nao";
                                bool hadGroundingFailure = false;
                                var failures = failureEngine.Evaluate(components, flightDuration);
                                if (failures.Count > 0)
                                {
                                    var firstFail = failures[0];
                                    failureDesc = $"Sim: {firstFail.Mode.Name}";
                                    string? defectCode = DefectCatalog.CodeFor(firstFail.Mode.Effect, firstFail.Kind, critical: true);
                                    if (!string.IsNullOrEmpty(defectCode))
                                    {
                                        var defectSpec = DefectCatalog.Get(defectCode);
                                        if (defectSpec.IsGrounding)
                                        {
                                            hadGroundingFailure = true;
                                            // Reparação imediata da discrepância NO-GO
                                            double repCost = DefectCatalog.CalculateRepairCost(defectCode, aType.PurchasePrice);
                                            double repDowntime = 12.0;

                                            balance -= repCost;
                                            dayMaintCost += repCost;
                                            remainingDowntimeHours += repDowntime;

                                            maintenanceList.Add(new MaintenanceRecord(
                                                botName,
                                                aType.Model,
                                                day,
                                                airframeHours + flightDuration,
                                                $"Pane NO-GO: {defectSpec.Title}",
                                                repCost,
                                                repDowntime));
                                        }
                                    }
                                }

                                // Contabilidade e registros do voo
                                double flightRevenue = chosen.RewardMoney;
                                balance += flightRevenue - fuelCost - totalFees;
                                dayRevenue += flightRevenue;
                                dayFuelCost += fuelCost;
                                dayFeesCost += totalFees;
                                dayHoursFlown += flightDuration;
                                dayFlights++;

                                airframeHours += flightDuration;
                                hoursUntil50h -= flightDuration;
                                hoursUntil100h -= flightDuration;
                                if (isPiston) hoursUntil500h -= flightDuration;
                                tboRemaining -= flightDuration;
                                totalLandings++;

                                flightsList.Add(new FlightRecord(
                                    botName,
                                    aType.Model,
                                    day,
                                    currentAirport.ICAO,
                                    destAirport.ICAO,
                                    destAirport.Tier,
                                    chosen.MissionCategoryType ?? "Geral",
                                    chosen.DistanceNM,
                                    Math.Round(flightDuration, 2),
                                    chosen.PassengerCount,
                                    chosen.PayloadLbs,
                                    Math.Round(flightRevenue, 2),
                                    fuelCost,
                                    landingFee,
                                    paxFee,
                                    handlingFee,
                                    navFee,
                                    Math.Round(totalFees, 2),
                                    Math.Round(estimatedParking, 2),
                                    failureDesc));

                                currentAirport = destAirport;

                                if (hadGroundingFailure)
                                {
                                    // Avião parado por pane no restante do dia
                                    break;
                                }
                            }
                        }

                        // 3. Manutenções programadas ao término do dia
                        // Inspeção 12h (Troca de óleo)
                        if (hoursUntil50h <= 0)
                        {
                            double cost = MaintenancePricing.Cost50hInspection;
                            balance -= cost;
                            dayMaintCost += cost;
                            hoursUntil50h = 12.0;
                            var oilComp = components.FirstOrDefault(c => c.Kind == ComponentKind.Oil);
                            if (oilComp != null) oilComp.Health = 100.0;
                            remainingDowntimeHours += 4.0;
                            maintenanceList.Add(new MaintenanceRecord(botName, aType.Model, day, airframeHours, "Troca de óleo (12h)", cost, 4.0));
                        }

                        // Inspeção 25h (Inspeção anual)
                        if (hoursUntil100h <= 0)
                        {
                            double cost = MaintenancePricing.Cost100hInspection;
                            balance -= cost;
                            dayMaintCost += cost;
                            hoursUntil100h = 25.0;
                            remainingDowntimeHours += 16.0;
                            maintenanceList.Add(new MaintenanceRecord(botName, aType.Model, day, airframeHours, "Inspeção anual (25h)", cost, 16.0));
                        }

                        // Revisão 125h (ignição / magnetos)
                        if (isPiston && hoursUntil500h <= 0)
                        {
                            double cost = MaintenancePricing.Cost500hInspection;
                            balance -= cost;
                            dayMaintCost += cost;
                            hoursUntil500h = 125.0;
                            var ignComp = components.FirstOrDefault(c => c.Kind == ComponentKind.Ignition);
                            if (ignComp != null) ignComp.Health = 100.0;
                            remainingDowntimeHours += 24.0;
                            maintenanceList.Add(new MaintenanceRecord(botName, aType.Model, day, airframeHours, "Revisão de ignição (125h)", cost, 24.0));
                        }

                        // Retífica Geral (Overhaul TBO)
                        if (tboRemaining <= 0)
                        {
                            double cost = MaintenancePricing.CalculateOverhaulCost(aType.PurchasePrice);
                            balance -= cost;
                            dayMaintCost += cost;
                            tboRemaining = defaultTbo;
                            var engComp = components.FirstOrDefault(c => c.Kind == ComponentKind.Engine);
                            if (engComp != null) engComp.Health = 100.0;
                            var hotComp = components.FirstOrDefault(c => c.Kind == ComponentKind.HotSection);
                            if (hotComp != null) hotComp.Health = 100.0;
                            var oilComp = components.FirstOrDefault(c => c.Kind == ComponentKind.Oil);
                            if (oilComp != null) oilComp.Health = 100.0;
                            remainingDowntimeHours += 72.0;
                            maintenanceList.Add(new MaintenanceRecord(botName, aType.Model, day, airframeHours, "Retífica Geral / Overhaul TBO", cost, 72.0));
                        }

                        // Reparo de componentes com saúde < 60%
                        foreach (var comp in components)
                        {
                            if (comp.Health < 60.0)
                            {
                                if (parts.Count > 0)
                                {
                                    // Substitui peças desgastadas pertencentes a este componente
                                    var mapping = PartComponentMapping.CreateDefault();
                                    var compParts = parts
                                        .Where(p =>
                                        {
                                            var def = _partCatalog.Get(p.PartId);
                                            return def != null && mapping.ResolveComponentKind(def.Group) == comp.Kind && (p.Health < 60.0 || p.IsBroken);
                                        })
                                        .ToList();

                                    foreach (var cp in compParts)
                                    {
                                        var pDef = _partCatalog.Get(cp.PartId);
                                        if (pDef == null) continue;
                                        string acClass = AircraftEconomyClass.Resolve(aType.Model, engineClass.ToString(), engineCount, features, aType.PurchasePrice);
                                        double pCost = PartPricing.CalculatePartPrice(pDef, acClass) + PartPricing.CalculateLaborCost(pDef, 100.0);
                                        double pDown = PartPricing.CalculateDowntimeHours(pDef, rnd);

                                        cp.Health = 100.0;
                                        cp.IsBroken = false;
                                        cp.HoursSinceReplacement = 0.0;
                                        cp.CyclesSinceReplacement = 0;

                                        balance -= pCost;
                                        dayMaintCost += pCost;
                                        remainingDowntimeHours += pDown;

                                        maintenanceList.Add(new MaintenanceRecord(botName, aType.Model, day, airframeHours, $"Substituição de Peça: {pDef.Name}", pCost, pDown));
                                    }

                                    PartComponentBridge.SyncComponentsFromParts(pilotAircraft, components, parts, _partCatalog);
                                }
                                else
                                {
                                    var cDef = _componentCatalog.Components.FirstOrDefault(d => d.Kind == comp.Kind);
                                    if (cDef != null)
                                    {
                                        double rCost = ComponentMaintenance.RepairCost(cDef, comp, aType.PurchasePrice);
                                        comp.Health = 100.0;
                                        balance -= rCost;
                                        dayMaintCost += rCost;
                                        remainingDowntimeHours += 8.0;

                                        maintenanceList.Add(new MaintenanceRecord(botName, aType.Model, day, airframeHours, $"Reparo Componente: {comp.Kind}", rCost, 8.0));
                                    }
                                }
                            }
                        }

                        // Confere falência
                        if (!bankrupted && balance < 0)
                        {
                            bankrupted = true;
                            bankruptcyDay = day;
                        }

                        double dayExpenses = dayFuelCost + dayFeesCost + dayMaintCost + dayInsuranceCost;
                        double dayProfit = dayRevenue - dayExpenses;

                        dailyList.Add(new DailyRecord(
                            botName,
                            aType.Model,
                            day,
                            Math.Round(dayHoursFlown, 2),
                            dayFlights,
                            Math.Round(dayRevenue, 2),
                            Math.Round(dayFuelCost, 2),
                            Math.Round(dayFeesCost, 2),
                            Math.Round(dayMaintCost, 2),
                            Math.Round(dayInsuranceCost, 2),
                            Math.Round(dayExpenses, 2),
                            Math.Round(dayProfit, 2),
                            Math.Round(balance, 2),
                            isAvailable,
                            bankrupted));
                    }
                }
            }

            // Grava arquivos de saída
            await WriteFlightsCsvAsync(Path.Combine(outDir, "flights.csv"), flightsList);
            await WriteMaintenanceCsvAsync(Path.Combine(outDir, "maintenance.csv"), maintenanceList);
            await WriteDailyCsvAsync(Path.Combine(outDir, "daily.csv"), dailyList);

            string summaryMd = GenerateSummaryMarkdown(flightsList, maintenanceList, dailyList, allTypes, options);
            await File.WriteAllTextAsync(Path.Combine(outDir, "summary.md"), summaryMd, Encoding.UTF8);

            options.LogCallback?.Invoke($"[ECON-SIM] Simulação concluída com sucesso! Relatórios salvos em: {outDir}");

            return new CareerSimulationResult
            {
                Flights = flightsList,
                Maintenance = maintenanceList,
                Daily = dailyList,
                SummaryMarkdown = summaryMd,
                OutputDirectory = outDir
            };
        }

        private static async Task WriteFlightsCsvAsync(string path, IReadOnlyList<FlightRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("bot,aviao,dia,origem,destino,tier,subcategoria,distancia,horas,pax,carga,receita,combustivel,taxa_pouso,taxa_embarque,taxa_handling,taxa_navegacao,taxas_total,estacionamento_estimado,pane");
            foreach (var r in records)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7:F1},{8:F2},{9},{10:F0},{11:F2},{12:F2},{13:F2},{14:F2},{15:F2},{16:F2},{17:F2},{18:F2},{19}",
                    EscapeCsv(r.Bot),
                    EscapeCsv(r.Aircraft),
                    r.Day,
                    r.Origin,
                    r.Destination,
                    r.DestTier,
                    EscapeCsv(r.Subcategory),
                    r.DistanceNM,
                    r.Hours,
                    r.Passengers,
                    r.PayloadLbs,
                    r.Revenue,
                    r.FuelCost,
                    r.LandingFee,
                    r.PassengerFee,
                    r.HandlingFee,
                    r.NavFee,
                    r.TotalFees,
                    r.EstimatedOvernightParking,
                    EscapeCsv(r.Failure)));
            }
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        private static async Task WriteMaintenanceCsvAsync(string path, IReadOnlyList<MaintenanceRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("bot,aviao,dia,horas_celula,servico,custo,parada_h");
            foreach (var r in records)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F2},{4},{5:F2},{6:F1}",
                    EscapeCsv(r.Bot),
                    EscapeCsv(r.Aircraft),
                    r.Day,
                    r.AirframeHours,
                    EscapeCsv(r.Service),
                    r.Cost,
                    r.DowntimeHours));
            }
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        private static async Task WriteDailyCsvAsync(string path, IReadOnlyList<DailyRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("bot,aviao,dia,horas_voadas,voos,receita,combustivel,taxas,manutencao,seguro,despesas_total,lucro_diario,saldo,disponivel,faliu");
            foreach (var r in records)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F2},{4},{5:F2},{6:F2},{7:F2},{8:F2},{9:F2},{10:F2},{11:F2},{12:F2},{13},{14}",
                    EscapeCsv(r.Bot),
                    EscapeCsv(r.Aircraft),
                    r.Day,
                    r.HoursFlown,
                    r.Flights,
                    r.Revenue,
                    r.FuelCost,
                    r.Fees,
                    r.MaintenanceCost,
                    r.InsuranceCost,
                    r.TotalExpenses,
                    r.DailyProfit,
                    r.Balance,
                    r.IsAvailable ? 1 : 0,
                    r.HasBankrupted ? 1 : 0));
            }
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
            {
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            return val;
        }

        private static string GenerateSummaryMarkdown(
            IReadOnlyList<FlightRecord> flights,
            IReadOnlyList<MaintenanceRecord> maintenance,
            IReadOnlyList<DailyRecord> daily,
            IReadOnlyList<AircraftType> types,
            CareerSimulationOptions options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Relatório Consolidado de Balanceamento Econômico (ECON-SIM)");
            sb.AppendLine();
            sb.AppendLine($"- **Data da Simulação:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Parâmetros:** {options.Days} dias, {options.HoursPerDay} h de voo/dia, {options.BotsPerAircraft} bots/modelo, semente {options.Seed}, política `{options.Policy}`, modo `{(options.FastMode ? "Fast" : "Completo")}`");
            sb.AppendLine();

            var groupedFlights = flights.GroupBy(f => f.Aircraft).ToDictionary(g => g.Key, g => g.ToList());
            var groupedMaint = maintenance.GroupBy(m => m.Aircraft).ToDictionary(g => g.Key, g => g.ToList());
            var groupedDaily = daily.GroupBy(d => d.Aircraft).ToDictionary(g => g.Key, g => g.ToList());

            var modelList = groupedDaily.Keys.OrderBy(k => k).ToList();

            foreach (var model in modelList)
            {
                var mFlights = groupedFlights.GetValueOrDefault(model, new List<FlightRecord>());
                var mMaint = groupedMaint.GetValueOrDefault(model, new List<MaintenanceRecord>());
                var mDaily = groupedDaily.GetValueOrDefault(model, new List<DailyRecord>());

                var type = types.FirstOrDefault(t => string.Equals(t.Model, model, StringComparison.OrdinalIgnoreCase))
                    ?? types.FirstOrDefault(t => t.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

                double purchasePrice = type?.PurchasePrice ?? 0.0;

                // Agrupamento por bot para cálculo de média, mín e máx
                var botDailies = mDaily.GroupBy(d => d.Bot).ToList();
                var botSummaries = new List<(
                    double Hours,
                    int Flights,
                    double Revenue,
                    double Fuel,
                    double Fees,
                    double Maint,
                    double MaintInsp,
                    double MaintRep,
                    double MaintDisc,
                    double MaintOverhaul,
                    double Insurance,
                    double Margin,
                    int MaintEvents,
                    int Failures,
                    int DaysStopped
                )>();

                foreach (var bg in botDailies)
                {
                    string bName = bg.Key;
                    var bFlights = mFlights.Where(f => f.Bot == bName).ToList();
                    var bMaint = mMaint.Where(m => m.Bot == bName).ToList();

                    double bHours = bFlights.Sum(f => f.Hours);
                    int bCount = bFlights.Count;
                    double bRev = bFlights.Sum(f => f.Revenue);
                    double bFuel = bFlights.Sum(f => f.FuelCost);
                    double bFees = bFlights.Sum(f => f.TotalFees);
                    double bMaintTot = bMaint.Sum(m => m.Cost);

                    double bMaintInsp = bMaint.Where(m => m.Service.Contains("Inspeção", StringComparison.OrdinalIgnoreCase) || m.Service.Contains("Revisão", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Cost);
                    double bMaintRep = bMaint.Where(m => m.Service.Contains("Peça", StringComparison.OrdinalIgnoreCase) || m.Service.Contains("Componente", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Cost);
                    double bMaintDisc = bMaint.Where(m => m.Service.Contains("Pane", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Cost);
                    double bMaintOverhaul = bMaint.Where(m => m.Service.Contains("Overhaul", StringComparison.OrdinalIgnoreCase) || m.Service.Contains("Retífica", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Cost);

                    double bIns = bg.Sum(d => d.InsuranceCost);
                    double bExp = bFuel + bFees + bMaintTot + bIns;
                    double bMargin = bRev - bExp;

                    int bEvents = bMaint.Count;
                    int bFailures = bFlights.Count(f => f.Failure != "Nao");
                    int bStopped = bg.Count(d => !d.IsAvailable);

                    botSummaries.Add((bHours, bCount, bRev, bFuel, bFees, bMaintTot, bMaintInsp, bMaintRep, bMaintDisc, bMaintOverhaul, bIns, bMargin, bEvents, bFailures, bStopped));
                }

                double avgHours = botSummaries.Average(b => b.Hours);
                double minHours = botSummaries.Min(b => b.Hours);
                double maxHours = botSummaries.Max(b => b.Hours);

                double avgFlights = botSummaries.Average(b => b.Flights);
                double avgRev = botSummaries.Average(b => b.Revenue);
                double avgFuel = botSummaries.Average(b => b.Fuel);
                double avgFees = botSummaries.Average(b => b.Fees);
                double avgMaint = botSummaries.Average(b => b.Maint);
                double avgMaintInsp = botSummaries.Average(b => b.MaintInsp);
                double avgMaintRep = botSummaries.Average(b => b.MaintRep);
                double avgMaintDisc = botSummaries.Average(b => b.MaintDisc);
                double avgMaintOverhaul = botSummaries.Average(b => b.MaintOverhaul);
                double avgIns = botSummaries.Average(b => b.Insurance);
                double avgMargin = botSummaries.Average(b => b.Margin);

                double revPerHour = avgHours > 0 ? avgRev / avgHours : 0.0;
                double fuelPerHour = avgHours > 0 ? avgFuel / avgHours : 0.0;
                double feesPerHour = avgHours > 0 ? avgFees / avgHours : 0.0;
                double maintPerHour = avgHours > 0 ? avgMaint / avgHours : 0.0;
                double inspPerHour = avgHours > 0 ? avgMaintInsp / avgHours : 0.0;
                double repPerHour = avgHours > 0 ? avgMaintRep / avgHours : 0.0;
                double discPerHour = avgHours > 0 ? avgMaintDisc / avgHours : 0.0;
                double overhaulPerHour = avgHours > 0 ? avgMaintOverhaul / avgHours : 0.0;
                double insPerHour = avgHours > 0 ? avgIns / avgHours : 0.0;
                double marginPerHour = avgHours > 0 ? avgMargin / avgHours : 0.0;
                double marginPct = avgRev > 0 ? (avgMargin / avgRev) * 100.0 : 0.0;

                double hoursToPay = (purchasePrice > 0 && marginPerHour > 0) ? purchasePrice / marginPerHour : 0.0;

                double maintPer100h = avgHours > 0 ? (botSummaries.Average(b => b.MaintEvents) / avgHours) * 100.0 : 0.0;
                double avgMaintIntervalHours = botSummaries.Average(b => b.MaintEvents) > 0 ? avgHours / botSummaries.Average(b => b.MaintEvents) : avgHours;
                double avgMaintIntervalDays = botSummaries.Average(b => b.MaintEvents) > 0 ? (double)options.Days / botSummaries.Average(b => b.MaintEvents) : options.Days;
                double panesPer100h = avgHours > 0 ? (botSummaries.Average(b => b.Failures) / avgHours) * 100.0 : 0.0;
                double avgDaysStopped = botSummaries.Average(b => b.DaysStopped);

                double totalExpenses = avgFuel + avgFees + avgMaint + avgIns;
                double fuelPct = totalExpenses > 0 ? (avgFuel / totalExpenses) * 100.0 : 0.0;
                double feesPct = totalExpenses > 0 ? (avgFees / totalExpenses) * 100.0 : 0.0;
                double maintPct = totalExpenses > 0 ? (avgMaint / totalExpenses) * 100.0 : 0.0;
                double insPct = totalExpenses > 0 ? (avgIns / totalExpenses) * 100.0 : 0.0;

                // Subcategorias aceitas
                var subcatCounts = mFlights.GroupBy(f => f.Subcategory)
                    .Select(g => (Subcat: g.Key, Count: g.Count(), Pct: (double)g.Count() / Math.Max(1, mFlights.Count) * 100.0))
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var (targetMin, targetMax, targetClass) = ResolveTargetRange(model);
                string targetStatus = "N/A";
                if (targetMin.HasValue && targetMax.HasValue)
                {
                    if (maintPerHour < targetMin.Value) targetStatus = $"Abaixo ({maintPerHour:C2} < {targetMin.Value:C2})";
                    else if (maintPerHour > targetMax.Value) targetStatus = $"Acima ({maintPerHour:C2} > {targetMax.Value:C2})";
                    else targetStatus = $"Dentro da Faixa [{targetMin.Value:C0} - {targetMax.Value:C0}]";
                }

                sb.AppendLine($"## Modelo: {model}");
                sb.AppendLine($"- **Preço de Aquisição:** {purchasePrice:C0}");
                sb.AppendLine($"- **Horas Voadas:** média {avgHours:F1} h (mín {minHours:F1} h, máx {maxHours:F1} h) · {avgFlights:F1} voos");
                sb.AppendLine($"- **Receita / h:** {revPerHour:C2}");
                sb.AppendLine($"- **Custos / h:** Combustível: {fuelPerHour:C2} | Taxas: {feesPerHour:C2} | Seguro: {insPerHour:C2} | Manutenção: {maintPerHour:C2}");
                sb.AppendLine($"  - *Detalhe da Manutenção / h:* Inspeções: {inspPerHour:C2} | Reparos Peças: {repPerHour:C2} | Panes: {discPerHour:C2} | Retífica: {overhaulPerHour:C2}");
                sb.AppendLine($"- **Margem Operacional:** {marginPerHour:C2}/h ({marginPct:F1}%)");
                sb.AppendLine($"- **Horas para Pagar a Aeronave:** {(hoursToPay > 0 ? $"{hoursToPay:N0} h voadas" : "Inviável / Margem Negativa")}");
                sb.AppendLine($"- **Confiabilidade:** {panesPer100h:F1} panes / 100 h | {maintPer100h:F1} manutenções / 100 h (intervalo médio {avgMaintIntervalHours:F1} h / {avgMaintIntervalDays:F1} dias)");
                sb.AppendLine($"- **Indisponibilidade:** média {avgDaysStopped:F1} dias parados em {options.Days} dias");
                sb.AppendLine($"- **Composição do Custo Operacional:** Combustível {fuelPct:F1}% | Taxas {feesPct:F1}% | Manutenção {maintPct:F1}% | Seguro {insPct:F1}%");
                sb.AppendLine($"- **Avaliação vs economy_targets.json:** {targetStatus} (Classe: {targetClass})");
                sb.AppendLine($"- **Mix de Missões Realizadas:**");
                foreach (var sc in subcatCounts.Take(5))
                {
                    sb.AppendLine($"  - {sc.Subcat}: {sc.Count} voos ({sc.Pct:F1}%)");
                }
                sb.AppendLine();
            }

            // Tabela final consolidada
            sb.AppendLine("## Tabela Resumo Comparativa");
            sb.AppendLine();
            sb.AppendLine("| Modelo | Preço | Horas | Receita/h | Combust./h | Taxas/h | Manut./h | Seguro/h | Margem/h | Margem % | Pagar (h) | Alvo Manut. |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");

            foreach (var model in modelList)
            {
                var mFlights = groupedFlights.GetValueOrDefault(model, new List<FlightRecord>());
                var mMaint = groupedMaint.GetValueOrDefault(model, new List<MaintenanceRecord>());
                var mDaily = groupedDaily.GetValueOrDefault(model, new List<DailyRecord>());
                var type = types.FirstOrDefault(t => string.Equals(t.Model, model, StringComparison.OrdinalIgnoreCase))
                    ?? types.FirstOrDefault(t => t.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

                double price = type?.PurchasePrice ?? 0.0;
                double totHours = mFlights.Sum(f => f.Hours);
                double totRev = mFlights.Sum(f => f.Revenue);
                double totFuel = mFlights.Sum(f => f.FuelCost);
                double totFees = mFlights.Sum(f => f.TotalFees);
                double totMaint = mMaint.Sum(m => m.Cost);
                double totIns = mDaily.Sum(d => d.InsuranceCost);
                double totExp = totFuel + totFees + totMaint + totIns;
                double totMargin = totRev - totExp;

                double revH = totHours > 0 ? totRev / totHours : 0;
                double fuelH = totHours > 0 ? totFuel / totHours : 0;
                double feesH = totHours > 0 ? totFees / totHours : 0;
                double maintH = totHours > 0 ? totMaint / totHours : 0;
                double insH = totHours > 0 ? totIns / totHours : 0;
                double marginH = totHours > 0 ? totMargin / totHours : 0;
                double marginP = totRev > 0 ? (totMargin / totRev) * 100.0 : 0;
                double payH = (price > 0 && marginH > 0) ? price / marginH : 0;

                var (tMin, tMax, _) = ResolveTargetRange(model);
                string alvo = (tMin.HasValue && tMax.HasValue) ? $"[{tMin.Value:C0} - {tMax.Value:C0}]" : "N/A";

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1:C0} | {2:F1} | {3:C2} | {4:C2} | {5:C2} | {6:C2} | {7:C2} | {8:C2} | {9:F1}% | {10} | {11} |",
                    model,
                    price,
                    totHours,
                    revH,
                    fuelH,
                    feesH,
                    maintH,
                    insH,
                    marginH,
                    marginP,
                    payH > 0 ? $"{payH:N0}" : "Inv.",
                    alvo));
            }

            return sb.ToString();
        }

        private static (double? Min, double? Max, string ClassName) ResolveTargetRange(string model)
        {
            // Mapeamento alinhado ao economy_targets.json
            string m = model.ToUpperInvariant();
            if (m.Contains("152") || m.Contains("CUB") || m.Contains("DR400") || m.Contains("CAP-4"))
                return (30.0, 120.0, "Classe A - Pistão Leve");
            if (m.Contains("172") || m.Contains("BONANZA") || m.Contains("SR22") || m.Contains("DA40"))
                return (80.0, 250.0, "Classe B/D - Pistão Injetado / FADEC");
            if (m.Contains("BARON") || m.Contains("DA62") || m.Contains("TITAN"))
                return (150.0, 550.0, "Classe E - Bimotor Pistão");
            if (m.Contains("TBM") || m.Contains("208") || m.Contains("PC-12"))
                return (250.0, 900.0, "Classe F - Turboélice Monomotor");
            if (m.Contains("KING AIR") || m.Contains("ATR") || m.Contains("DHC-6"))
                return (450.0, 2500.0, "Classe G - Turboélice Bimotor/Regional");
            if (m.Contains("CJ4") || m.Contains("CITATION") || m.Contains("PHENOM") || m.Contains("HONDAJET"))
                return (800.0, 2000.0, "Classe H - Jato Executivo");
            if (m.Contains("737") || m.Contains("320") || m.Contains("321"))
                return (1000.0, 2500.0, "Classe I - Jato Narrowbody");
            if (m.Contains("747") || m.Contains("330") || m.Contains("787"))
                return (2500.0, 6000.0, "Classe J - Jato Widebody");

            return (null, null, "Classe Desconhecida");
        }
    }
}
