using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Application.Simulation
{
    public sealed class CareerProgressionOptions
    {
        public int Days { get; set; } = 720;
        public int BotsPerGroup { get; set; } = 5;
        public int Seed { get; set; } = 1;
        public string Difficulty { get; set; } = "all"; // "easy", "medium", "hard", "all"
        public string Policy { get; set; } = "all"; // "cauteloso", "equilibrado", "agressivo", "all"
        public double HoursPerDay { get; set; } = 2.0;
        public string? InitialHub { get; set; }
        public string? OutputDirectory { get; set; }
        public bool FastMode { get; set; } = true;
        public Action<string>? LogCallback { get; set; }
    }

    public sealed class BotMilestoneSnapshot
    {
        public string Difficulty { get; init; } = "";
        public string Policy { get; init; } = "";
        public string BotId { get; init; } = "";
        public int Day { get; init; }
        public bool IsBankrupt { get; init; }
        public int? BankruptcyDay { get; init; }
        public double Balance { get; init; }
        public double AircraftMarketValue { get; init; }
        public double NetWorth { get; init; } // Saldo + valor de mercado
        public string AircraftModel { get; init; } = "";
        public string AircraftClassKey { get; init; } = "";
        public PilotCategory PilotCategory { get; init; }
        public int? FirstPurchaseDay { get; init; }
        public double TotalHours { get; init; }
        public double RevenuePerHour { get; init; }
        public double FuelCostPerHour { get; init; }
        public double FeesPerHour { get; init; }
        public double MaintCostPerHour { get; init; }
        public double InsuranceCostPerHour { get; init; }
        public double TotalCostPerHour { get; init; }
        public double MarginPct { get; init; }
        public double MaintPer100h { get; init; }
        public double FailuresPer100h { get; init; }
        public int DaysGrounded { get; init; }
    }

    public sealed class CareerProgressionResult
    {
        public IReadOnlyList<BotMilestoneSnapshot> Snapshots { get; init; } = Array.Empty<BotMilestoneSnapshot>();
        public string SummaryMarkdown { get; init; } = string.Empty;
        public string CsvContent { get; init; } = string.Empty;
        public string OutputDirectory { get; init; } = string.Empty;
        public TimeSpan ElapsedTime { get; init; }
    }

    public sealed class CareerProgressionSimulator
    {
        private readonly Func<IAppDbContext> _dbFactory;
        private readonly ComponentCatalog _componentCatalog;
        private readonly PartCatalog _partCatalog;
        private readonly WearRuleCatalog _wearRuleCatalog;
        private readonly AircraftLimitResolver _limitResolver;
        private readonly IHubDataService _hubDataService;

        public CareerProgressionSimulator(
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

        public sealed record LadderStep(string ClassKey, string ModelSubstring, PilotCategory RequiredCategory, AircraftType Type);

        public async Task<CareerProgressionResult> RunAsync(CareerProgressionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var startTime = DateTime.UtcNow;

            var outDir = options.OutputDirectory;
            if (string.IsNullOrWhiteSpace(outDir))
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                outDir = Path.Combine(localAppData, "FlightCareer", "career-sim", $"stab-{DateTime.Now:yyyyMMdd-HHmmss}");
            }
            Directory.CreateDirectory(outDir);

            // 1. Pré-carrega aeroportos e tipos de aeronave da base
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

            // Hub inicial padrão (Tier 2 representativo)
            Airport defaultHub;
            if (!string.IsNullOrWhiteSpace(options.InitialHub) && airportLookup.TryGetValue(options.InitialHub, out var customHub))
            {
                defaultHub = customHub;
            }
            else
            {
                defaultHub = allAirports.FirstOrDefault(a => a.Tier == 2 && (a.ICAO.StartsWith("SB") || a.ICAO.StartsWith("SD")))
                    ?? allAirports.FirstOrDefault(a => a.Tier == 2)
                    ?? allAirports.First(a => a.Tier <= 2);
            }

            // 2. Monta a escada de aeronaves A -> B -> E -> F -> G -> H -> I -> J
            var ladder = BuildAircraftLadder(allTypes);

            // 3. Determina as dificuldades e políticas a simular
            var difficulties = ParseDifficulties(options.Difficulty);
            var policies = ParsePolicies(options.Policy);

            var milestones = DetermineMilestones(options.Days);

            options.LogCallback?.Invoke($"[ECON-STAB] Iniciando simulação: {difficulties.Count} dificuldade(s), {policies.Count} política(s), {options.BotsPerGroup} bot(s)/grupo, {options.Days} dias.");
            options.LogCallback?.Invoke($"[ECON-STAB] Marcos de medição definidos: {string.Join(", ", milestones.Select(m => $"Dia {m}"))}");

            var snapshots = new List<BotMilestoneSnapshot>();
            var startDate = new DateTime(2024, 1, 1);

            // Cache de missões por (ICAO, Dia) para evitar recomputações
            var missionCache = new Dictionary<(string Icao, int Day), List<Mission>>();

            int groupIndex = 0;
            foreach (var difficulty in difficulties)
            {
                foreach (var policy in policies)
                {
                    groupIndex++;
                    options.LogCallback?.Invoke($"[ECON-STAB] Simulando grupo {groupIndex}/{difficulties.Count * policies.Count}: {difficulty} / {policy} ({options.BotsPerGroup} bots)...");

                    for (int botNum = 1; botNum <= options.BotsPerGroup; botNum++)
                    {
                        int botSeed = options.Seed + (groupIndex * 1000) + botNum;
                        var rnd = new SystemRandomSource(botSeed);
                        var sysRnd = new Random(botSeed);

                        string botId = $"Bot-{difficulty}-{policy}-{botNum}";
                        double balance = CareerStart.GetInitialBalance(difficulty);
                        var category = PilotCategory.Category1_FlightSchool;
                        double totalHours = 0.0;
                        int totalXP = 0;
                        bool isBankrupt = false;
                        int? bankruptcyDay = null;
                        int? firstPurchaseDay = null;
                        var clock = startDate;

                        int ladderIdx = 0;
                        var currentAircraftType = ladder[ladderIdx].Type;
                        string currentClassKey = ladder[ladderIdx].ClassKey;
                        double airframeHours = CareerStart.DefaultStartingAirframeHours;
                        double hoursUntil50h = CareerStart.DefaultStarting50hHours;
                        double hoursUntil100h = CareerStart.DefaultStarting100hHours;
                        double hoursUntil500h = MaintenancePricing.IgnitionRevisionIntervalHours;
                        double tboRemaining = CareerStart.DefaultStartingTboRemainingHours;
                        double remainingDowntimeHours = 0.0;
                        var currentAirport = defaultHub;

                        double cumRevenue = 0.0;
                        double cumFuelCost = 0.0;
                        double cumFees = 0.0;
                        double cumMaintCost = 0.0;
                        double cumInsuranceCost = 0.0;
                        int maintEvents = 0;
                        int failureEvents = 0;
                        int daysGrounded = 0;

                        // Componentes virtuais para avaliação de falhas
                        var engineClass = _componentCatalog.ResolveEngineClass(currentAircraftType);
                        var failureEngine = new FailureEngine(_componentCatalog, engineClass, rnd);
                        var components = _componentCatalog.Components
                            .Where(c => c.AppliesTo.Contains(engineClass))
                            .Select(c => new AircraftComponent
                            {
                                Id = Guid.NewGuid(),
                                Kind = c.Kind,
                                Health = 100.0,
                                HoursSinceOverhaul = 0.0
                            }).ToList();

                        for (int day = 1; day <= options.Days; day++)
                        {
                            var simulatedDate = startDate.AddDays(day - 1);

                            if (isBankrupt)
                            {
                                // Bot falido: verifica se hoje é um marco para registrar
                                if (milestones.Contains(day))
                                {
                                    snapshots.Add(CreateSnapshot(
                                        difficulty.ToString(),
                                        policy,
                                        botId,
                                        day,
                                        isBankrupt: true,
                                        bankruptcyDay,
                                        balance,
                                        currentAircraftType,
                                        currentClassKey,
                                        category,
                                        firstPurchaseDay,
                                        totalHours,
                                        cumRevenue,
                                        cumFuelCost,
                                        cumFees,
                                        cumMaintCost,
                                        cumInsuranceCost,
                                        maintEvents,
                                        failureEvents,
                                        daysGrounded));
                                }
                                continue;
                            }

                            // 1. Seguro a cada 30 dias (Dia 1, 31, 61...)
                            if ((day - 1) % 30 == 0)
                            {
                                double premium = EconomyServices.CalculateMonthlyPremium(currentAircraftType.PurchasePrice);
                                balance -= premium;
                                cumInsuranceCost += premium;
                            }

                            // 2. Downtime de manutenção/parada
                            bool isAvailable = true;
                            if (remainingDowntimeHours >= 24.0)
                            {
                                remainingDowntimeHours -= 24.0;
                                isAvailable = false;
                                daysGrounded++;
                            }
                            else if (remainingDowntimeHours > 0)
                            {
                                remainingDowntimeHours = 0.0;
                            }

                            // 3. Voos do dia (se disponível)
                            if (isAvailable)
                            {
                                double dayHoursFlown = 0.0;

                                while (dayHoursFlown < options.HoursPerDay && !isBankrupt)
                                {
                                    // Obter missões geradas para o aeroporto neste dia
                                    var cacheKey = (currentAirport.ICAO, day);
                                    if (!missionCache.TryGetValue(cacheKey, out var availableMissions))
                                    {
                                        availableMissions = MissionGenerator.GenerateMissionsInMemory(
                                            currentAirport,
                                            allAirports,
                                            _hubDataService,
                                            sysRnd,
                                            simulatedDate,
                                            countOverride: 40);
                                        missionCache[cacheKey] = availableMissions;
                                    }

                                    // Filtra missões permitidas para a categoria do piloto e capacidade do avião
                                    var compatible = new List<Mission>();
                                    foreach (var m in availableMissions)
                                    {
                                        if (m.RequiredCategory > category) continue;
                                        if (m.PayloadLbs > currentAircraftType.MaxPayloadLbs) continue;
                                        if (m.PassengerCount > currentAircraftType.MaxPax) continue;
                                        if (m.DistanceNM > currentAircraftType.RangeNM) continue;

                                        if (!airportLookup.TryGetValue(m.DestinationICAO, out var destAp)) continue;

                                        // Pista / Tier do destino
                                        if (currentAircraftType.MinRunwayLength >= 7000 && destAp.Tier > 1) continue;
                                        if (currentAircraftType.MinRunwayLength >= 4500 && destAp.Tier > 2) continue;
                                        if (currentAircraftType.MinRunwayLength >= 2500 && destAp.Tier > 3) continue;

                                        compatible.Add(m);
                                    }

                                    if (compatible.Count == 0)
                                    {
                                        // Sem missões compatíveis hoje; repouso
                                        break;
                                    }

                                    // Escolha da melhor missão (maior remuneração/hora)
                                    double speed = currentAircraftType.CruiseSpeedKts > 0 ? currentAircraftType.CruiseSpeedKts : 110.0;
                                    var chosen = compatible
                                        .OrderByDescending(m => m.RewardMoney / Math.Max(0.2, (m.DistanceNM / speed) + 0.25))
                                        .First();

                                    double flightDuration = Math.Max(0.2, (chosen.DistanceNM / speed) + 0.25);
                                    var destAirport = airportLookup[chosen.DestinationICAO];

                                    // Custos operacionais do voo
                                    double fuelUsed = currentAircraftType.FuelConsumptionGPH * flightDuration;
                                    double fuelPrice = FuelPriceService.GetCurrentPrice(currentAircraftType.FuelType ?? "AVGAS", destAirport.Tier, simulatedDate);
                                    double fuelCost = Math.Round(fuelUsed * fuelPrice, 2);

                                    double mtowLbs = AircraftWeightSpecs.GetMtow(currentAircraftType.Model, currentAircraftType);
                                    var feeCtx = new FeeContext(
                                        destAirport.Tier,
                                        destAirport.ICAO,
                                        mtowLbs,
                                        chosen.PassengerCount,
                                        hasMission: true,
                                        parkedGameHours: 0.0);
                                    var feeLines = GroundServiceCalculator.Calculate(feeCtx);
                                    double totalFees = feeLines.Sum(f => f.Total);

                                    // Checagem de falência antes do voo: sem dinheiro para o combustível
                                    if (balance < fuelCost && balance < 0)
                                    {
                                        isBankrupt = true;
                                        bankruptcyDay = day;
                                        break;
                                    }

                                    // Executa o voo
                                    balance += chosen.RewardMoney - fuelCost - totalFees;
                                    cumRevenue += chosen.RewardMoney;
                                    cumFuelCost += fuelCost;
                                    cumFees += totalFees;
                                    dayHoursFlown += flightDuration;
                                    totalHours += flightDuration;
                                    clock = CareerClock.AdvanceFlight(clock, flightDuration);

                                    airframeHours += flightDuration;
                                    hoursUntil50h -= flightDuration;
                                    hoursUntil100h -= flightDuration;
                                    if (engineClass == EngineClass.Piston) hoursUntil500h -= flightDuration;
                                    tboRemaining -= flightDuration;

                                    // XP do voo
                                    int baseXP = (int)(flightDuration * 60) + (int)(chosen.DistanceNM * 0.5);
                                    int tierBonusXP = (int)(baseXP * (destAirport.Tier * 0.2));
                                    totalXP += baseXP + tierBonusXP;

                                    // Avaliação de panes via FailureEngine
                                    var failures = failureEngine.Evaluate(components, flightDuration);
                                    bool hadGroundingFailure = false;
                                    if (failures.Count > 0)
                                    {
                                        failureEvents++;
                                        var firstFail = failures[0];
                                        string? defectCode = DefectCatalog.CodeFor(firstFail.Mode.Effect, firstFail.Kind, critical: true);
                                        if (!string.IsNullOrEmpty(defectCode))
                                        {
                                            var defectSpec = DefectCatalog.Get(defectCode);
                                            if (defectSpec.IsGrounding)
                                            {
                                                hadGroundingFailure = true;
                                                double repCost = DefectCatalog.CalculateRepairCost(defectCode, currentAircraftType.PurchasePrice);
                                                double repDowntime = 12.0;

                                                // Sem dinheiro para o reparo NO-GO com saldo negativo = falência
                                                if (balance < repCost && balance < 0)
                                                {
                                                    isBankrupt = true;
                                                    bankruptcyDay = day;
                                                    break;
                                                }

                                                balance -= repCost;
                                                cumMaintCost += repCost;
                                                maintEvents++;
                                                remainingDowntimeHours += repDowntime;
                                            }
                                        }
                                    }

                                    currentAirport = destAirport;

                                    if (hadGroundingFailure)
                                    {
                                        break;
                                    }
                                }
                            }

                            if (isBankrupt)
                            {
                                if (milestones.Contains(day))
                                {
                                    snapshots.Add(CreateSnapshot(
                                        difficulty.ToString(),
                                        policy,
                                        botId,
                                        day,
                                        isBankrupt: true,
                                        bankruptcyDay,
                                        balance,
                                        currentAircraftType,
                                        currentClassKey,
                                        category,
                                        firstPurchaseDay,
                                        totalHours,
                                        cumRevenue,
                                        cumFuelCost,
                                        cumFees,
                                        cumMaintCost,
                                        cumInsuranceCost,
                                        maintEvents,
                                        failureEvents,
                                        daysGrounded));
                                }
                                continue;
                            }

                            // 4. Manutenção de fim de dia conforme a política
                            ApplyPolicyMaintenance(
                                policy,
                                ref balance,
                                ref cumMaintCost,
                                ref maintEvents,
                                ref remainingDowntimeHours,
                                ref hoursUntil50h,
                                ref hoursUntil100h,
                                ref hoursUntil500h,
                                ref tboRemaining,
                                currentAircraftType,
                                engineClass,
                                day,
                                ref isBankrupt,
                                ref bankruptcyDay);

                            if (isBankrupt)
                            {
                                if (milestones.Contains(day))
                                {
                                    snapshots.Add(CreateSnapshot(
                                        difficulty.ToString(),
                                        policy,
                                        botId,
                                        day,
                                        isBankrupt: true,
                                        bankruptcyDay,
                                        balance,
                                        currentAircraftType,
                                        currentClassKey,
                                        category,
                                        firstPurchaseDay,
                                        totalHours,
                                        cumRevenue,
                                        cumFuelCost,
                                        cumFees,
                                        cumMaintCost,
                                        cumInsuranceCost,
                                        maintEvents,
                                        failureEvents,
                                        daysGrounded));
                                }
                                continue;
                            }

                            // 5. Promoção de categoria regulatória (se cumpriu horas + XP + saldo com reserva)
                            CheckCategoryPromotion(
                                ref category,
                                ref balance,
                                totalHours,
                                totalXP,
                                policy,
                                currentAircraftType,
                                currentClassKey);

                            // 6. Progressão de aeronave (compra do próximo avião da escada)
                            CheckAircraftPurchase(
                                ladder,
                                ref ladderIdx,
                                ref currentAircraftType,
                                ref currentClassKey,
                                ref airframeHours,
                                ref hoursUntil50h,
                                ref hoursUntil100h,
                                ref hoursUntil500h,
                                ref tboRemaining,
                                ref balance,
                                ref firstPurchaseDay,
                                policy,
                                category,
                                day);

                            // 7. Registro do marco se o dia atual for um dos marcos
                            if (milestones.Contains(day))
                            {
                                snapshots.Add(CreateSnapshot(
                                    difficulty.ToString(),
                                    policy,
                                    botId,
                                    day,
                                    isBankrupt: false,
                                    bankruptcyDay: null,
                                    balance,
                                    currentAircraftType,
                                    currentClassKey,
                                    category,
                                    firstPurchaseDay,
                                    totalHours,
                                    cumRevenue,
                                    cumFuelCost,
                                    cumFees,
                                    cumMaintCost,
                                    cumInsuranceCost,
                                    maintEvents,
                                    failureEvents,
                                    daysGrounded));
                            }
                        }
                    }
                }
            }

            var elapsedTime = DateTime.UtcNow - startTime;
            options.LogCallback?.Invoke($"[ECON-STAB] Simulação completa em {elapsedTime.TotalSeconds:F1} s ({snapshots.Count} snapshots coletados).");

            // Gera arquivos stability.csv e stability.md
            string csvContent = GenerateCsv(snapshots);
            await File.WriteAllTextAsync(Path.Combine(outDir, "stability.csv"), csvContent, Encoding.UTF8);

            string markdown = GenerateMarkdownReport(snapshots, milestones, options, elapsedTime);
            await File.WriteAllTextAsync(Path.Combine(outDir, "stability.md"), markdown, Encoding.UTF8);

            options.LogCallback?.Invoke($"[ECON-STAB] Relatórios salvos em: {outDir}");

            return new CareerProgressionResult
            {
                Snapshots = snapshots,
                SummaryMarkdown = markdown,
                CsvContent = csvContent,
                OutputDirectory = outDir,
                ElapsedTime = elapsedTime
            };
        }

        private static void ApplyPolicyMaintenance(
            string policy,
            ref double balance,
            ref double cumMaintCost,
            ref int maintEvents,
            ref double remainingDowntimeHours,
            ref double hoursUntil50h,
            ref double hoursUntil100h,
            ref double hoursUntil500h,
            ref double tboRemaining,
            AircraftType aircraftType,
            EngineClass engineClass,
            int day,
            ref bool isBankrupt,
            ref int? bankruptcyDay)
        {
            bool isPiston = engineClass == EngineClass.Piston;

            // 1. Manutenções obrigatórias por interdição regulatória (100h e TBO Overhaul)
            if (hoursUntil100h <= 0)
            {
                double cost = MaintenancePricing.Cost100hInspection;
                if (balance < cost && balance < 0)
                {
                    isBankrupt = true;
                    bankruptcyDay = day;
                    return;
                }
                balance -= cost;
                cumMaintCost += cost;
                maintEvents++;
                hoursUntil100h = MaintenancePricing.AnnualInspectionIntervalHours;
                remainingDowntimeHours += CareerClock.Inspection100hDowntimeHours;
            }

            if (tboRemaining <= 0)
            {
                double cost = MaintenancePricing.CalculateOverhaulCost(aircraftType.PurchasePrice);
                if (balance < cost && balance < 0)
                {
                    isBankrupt = true;
                    bankruptcyDay = day;
                    return;
                }
                balance -= cost;
                cumMaintCost += cost;
                maintEvents++;
                tboRemaining = MaintenancePricing.TboHoursFor(EngineClassResolver.FromAircraftType(aircraftType));
                remainingDowntimeHours += CareerClock.OverhaulDowntimeHours;
            }

            // 2. Políticas preventivas:
            // "agressivo" não faz inspeções preventivas opcionais (50h/500h).
            if (policy.Equals("agressivo", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // "cauteloso" e "equilibrado" realizam 50h e 500h quando atingem a janela
            if (hoursUntil50h <= 0)
            {
                double cost = MaintenancePricing.Cost50hInspection;
                if (balance >= cost || balance >= 0)
                {
                    balance -= cost;
                    cumMaintCost += cost;
                    maintEvents++;
                    hoursUntil50h = MaintenancePricing.OilChangeIntervalHours;
                    remainingDowntimeHours += CareerClock.Inspection50hDowntimeHours;
                }
            }

            if (isPiston && hoursUntil500h <= 0)
            {
                double cost = MaintenancePricing.Cost500hInspection;
                if (balance >= cost || balance >= 0)
                {
                    balance -= cost;
                    cumMaintCost += cost;
                    maintEvents++;
                    hoursUntil500h = MaintenancePricing.IgnitionRevisionIntervalHours;
                    remainingDowntimeHours += CareerClock.Inspection500hDowntimeHours;
                }
            }
        }

        private static void CheckCategoryPromotion(
            ref PilotCategory currentCat,
            ref double balance,
            double totalHours,
            int totalXP,
            string policy,
            AircraftType currentType,
            string currentClassKey)
        {
            if (currentCat >= PilotCategory.Category5_MajorAirline) return;

            var nextCat = (PilotCategory)((int)currentCat + 1);
            double promoCost = currentCat.GetPromotionCost();
            double reqHours = currentCat.GetRequiredHours();
            int reqXP = currentCat.GetRequiredXP();

            if (totalHours >= reqHours && totalXP >= reqXP)
            {
                double monthlyCost = CalculateEstimatedMonthlyOperatingCost(currentType, currentClassKey);
                double reserveNeeded = policy.ToLowerInvariant() switch
                {
                    "cauteloso" => 3.0 * monthlyCost,
                    "equilibrado" => 1.0 * monthlyCost,
                    _ => 0.0 // agressivo
                };

                if (balance >= promoCost + reserveNeeded)
                {
                    balance -= promoCost;
                    currentCat = nextCat;
                }
            }
        }

        private static void CheckAircraftPurchase(
            IReadOnlyList<LadderStep> ladder,
            ref int currentLadderIdx,
            ref AircraftType currentType,
            ref string currentClassKey,
            ref double airframeHours,
            ref double hoursUntil50h,
            ref double hoursUntil100h,
            ref double hoursUntil500h,
            ref double tboRemaining,
            ref double balance,
            ref int? firstPurchaseDay,
            string policy,
            PilotCategory pilotCategory,
            int day)
        {
            if (currentLadderIdx + 1 >= ladder.Count) return;

            var nextStep = ladder[currentLadderIdx + 1];
            if (pilotCategory < nextStep.RequiredCategory)
            {
                // Categoria regulatória do piloto não autoriza o próximo avião
                return;
            }

            var targetType = nextStep.Type;
            // Usado estimado via CalculateBaseMarketValue
            double usedPrice = AircraftValuation.CalculateBaseMarketValue(targetType, 80.0, 80.0, 80.0, 80.0, DateTime.UtcNow.Year - 5, 2500.0);
            double newPrice = targetType.PurchasePrice;

            // Venda do anterior com 8% de corretagem
            double grossOld = AircraftValuation.CalculateBaseMarketValue(currentType, 80.0, 80.0, 80.0, 80.0, DateTime.UtcNow.Year - 5, airframeHours);
            double brokerFee = Math.Round(grossOld * 0.08, 2);
            double netOldProceeds = grossOld - brokerFee;

            double monthlyCost = CalculateEstimatedMonthlyOperatingCost(targetType, nextStep.ClassKey);
            double reserveRequired = policy.ToLowerInvariant() switch
            {
                "cauteloso" => 3.0 * monthlyCost,
                "equilibrado" => 1.0 * monthlyCost,
                _ => 0.0 // agressivo: compra assim que saldo + venda cobre o avião
            };

            // Prefere novo se tiver caixa sobrando com a reserva, senão usado
            double chosenBuyPrice = (balance + netOldProceeds >= newPrice + reserveRequired) ? newPrice : usedPrice;
            double netCashNeeded = chosenBuyPrice - netOldProceeds;

            if (balance >= netCashNeeded + reserveRequired)
            {
                balance += netOldProceeds;
                balance -= chosenBuyPrice;
                currentType = targetType;
                currentClassKey = nextStep.ClassKey;
                currentLadderIdx++;

                if (!firstPurchaseDay.HasValue)
                {
                    firstPurchaseDay = day;
                }

                airframeHours = (chosenBuyPrice == newPrice) ? 0.0 : 2500.0;
                hoursUntil50h = MaintenancePricing.OilChangeIntervalHours;
                hoursUntil100h = MaintenancePricing.AnnualInspectionIntervalHours;
                hoursUntil500h = MaintenancePricing.IgnitionRevisionIntervalHours;
                tboRemaining = MaintenancePricing.TboHoursFor(EngineClassResolver.FromAircraftType(targetType));
            }
        }

        public static double CalculateEstimatedMonthlyOperatingCost(AircraftType type, string classKey)
        {
            double monthlyInsurance = InsuranceService.CalculateMonthlyPremium(type.PurchasePrice);
            double monthlyFuel = type.FuelConsumptionGPH * 7.5 * 60.0; // 60h voadas/mês a ~$7.5/gal
            double hourlyMaint = classKey switch
            {
                "A" => 75.0,
                "B" or "C" or "D" => 165.0,
                "E" => 350.0,
                "F" => 575.0,
                "G" => 1475.0,
                "H" => 1400.0,
                "I" => 1750.0,
                "J" => 4250.0,
                _ => 200.0
            };
            double monthlyMaintAndFees = (hourlyMaint + 40.0) * 60.0;
            return monthlyInsurance + monthlyFuel + monthlyMaintAndFees;
        }

        private static BotMilestoneSnapshot CreateSnapshot(
            string difficulty,
            string policy,
            string botId,
            int day,
            bool isBankrupt,
            int? bankruptcyDay,
            double balance,
            AircraftType currentType,
            string classKey,
            PilotCategory category,
            int? firstPurchaseDay,
            double totalHours,
            double cumRevenue,
            double cumFuel,
            double cumFees,
            double cumMaint,
            double cumInsurance,
            int maintEvents,
            int failureEvents,
            int daysGrounded)
        {
            double marketVal = isBankrupt ? 0.0 : AircraftValuation.CalculateBaseMarketValue(currentType, 80.0, 80.0, 80.0, 80.0, DateTime.UtcNow.Year - 5, 2500.0);
            double netWorth = balance + marketVal;
            double totalExpenses = cumFuel + cumFees + cumMaint + cumInsurance;

            double revH = totalHours > 0 ? cumRevenue / totalHours : 0.0;
            double fuelH = totalHours > 0 ? cumFuel / totalHours : 0.0;
            double feesH = totalHours > 0 ? cumFees / totalHours : 0.0;
            double maintH = totalHours > 0 ? cumMaint / totalHours : 0.0;
            double insH = totalHours > 0 ? cumInsurance / totalHours : 0.0;
            double totalCostH = totalHours > 0 ? totalExpenses / totalHours : 0.0;
            double marginPct = cumRevenue > 0 ? ((cumRevenue - totalExpenses) / cumRevenue) * 100.0 : 0.0;

            double maint100h = totalHours > 0 ? (maintEvents / totalHours) * 100.0 : 0.0;
            double fail100h = totalHours > 0 ? (failureEvents / totalHours) * 100.0 : 0.0;

            return new BotMilestoneSnapshot
            {
                Difficulty = difficulty,
                Policy = policy,
                BotId = botId,
                Day = day,
                IsBankrupt = isBankrupt,
                BankruptcyDay = bankruptcyDay,
                Balance = Math.Round(balance, 2),
                AircraftMarketValue = Math.Round(marketVal, 2),
                NetWorth = Math.Round(netWorth, 2),
                AircraftModel = currentType.Model,
                AircraftClassKey = classKey,
                PilotCategory = category,
                FirstPurchaseDay = firstPurchaseDay,
                TotalHours = Math.Round(totalHours, 1),
                RevenuePerHour = Math.Round(revH, 2),
                FuelCostPerHour = Math.Round(fuelH, 2),
                FeesPerHour = Math.Round(feesH, 2),
                MaintCostPerHour = Math.Round(maintH, 2),
                InsuranceCostPerHour = Math.Round(insH, 2),
                TotalCostPerHour = Math.Round(totalCostH, 2),
                MarginPct = Math.Round(marginPct, 1),
                MaintPer100h = Math.Round(maint100h, 2),
                FailuresPer100h = Math.Round(fail100h, 2),
                DaysGrounded = daysGrounded
            };
        }

        private static List<LadderStep> BuildAircraftLadder(IReadOnlyList<AircraftType> allTypes)
        {
            var list = new List<LadderStep>();

            // Escada A -> B -> E -> F -> G -> H -> I -> J
            var c152 = allTypes.FirstOrDefault(t => t.Model.Contains("152"))
                ?? throw new InvalidOperationException("C152 não encontrado.");
            list.Add(new LadderStep("A", "Cessna C152", PilotCategory.Category1_FlightSchool, c152));

            var c172 = allTypes.FirstOrDefault(t => t.Model.Contains("172"))
                ?? c152;
            list.Add(new LadderStep("B", "C172SP G1000", PilotCategory.Category2_SAE, c172));

            var baron = allTypes.FirstOrDefault(t => t.Model.Contains("Baron"))
                ?? allTypes.FirstOrDefault(t => t.Model.Contains("DA62"))
                ?? c172;
            list.Add(new LadderStep("E", "Beechcraft Baron G58", PilotCategory.Category3_AirTaxi, baron));

            var c208 = allTypes.FirstOrDefault(t => t.Model.Contains("208"))
                ?? allTypes.FirstOrDefault(t => t.Model.Contains("TBM"))
                ?? baron;
            list.Add(new LadderStep("F", "C208B", PilotCategory.Category3_AirTaxi, c208));

            var kingAir = allTypes.FirstOrDefault(t => t.Model.Contains("King Air 250") || t.Model.Contains("King Air"))
                ?? allTypes.FirstOrDefault(t => t.Model.Contains("ATR 42"))
                ?? c208;
            list.Add(new LadderStep("G", "Beechcraft King Air 250", PilotCategory.Category4_Regional, kingAir));

            var cj4 = allTypes.FirstOrDefault(t => t.Model.Contains("CJ4") || t.Model.Contains("Citation"))
                ?? kingAir;
            list.Add(new LadderStep("H", "Cessna Citation CJ4", PilotCategory.Category4_Regional, cj4));

            var a320 = allTypes.FirstOrDefault(t => t.Model.Contains("320") || t.Model.Contains("737"))
                ?? cj4;
            list.Add(new LadderStep("I", "A320neo", PilotCategory.Category5_MajorAirline, a320));

            var a330 = allTypes.FirstOrDefault(t => t.Model.Contains("330") || t.Model.Contains("747") || t.Model.Contains("787"))
                ?? a320;
            list.Add(new LadderStep("J", "A330-200", PilotCategory.Category5_MajorAirline, a330));

            return list;
        }

        private static List<DifficultyMode> ParseDifficulties(string difficulty)
        {
            if (string.Equals(difficulty, "easy", StringComparison.OrdinalIgnoreCase))
                return new List<DifficultyMode> { DifficultyMode.Easy };
            if (string.Equals(difficulty, "medium", StringComparison.OrdinalIgnoreCase))
                return new List<DifficultyMode> { DifficultyMode.Medium };
            if (string.Equals(difficulty, "hard", StringComparison.OrdinalIgnoreCase))
                return new List<DifficultyMode> { DifficultyMode.Hard };

            return new List<DifficultyMode> { DifficultyMode.Easy, DifficultyMode.Medium, DifficultyMode.Hard };
        }

        private static List<string> ParsePolicies(string policy)
        {
            if (string.Equals(policy, "cauteloso", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "cauteloso" };
            if (string.Equals(policy, "equilibrado", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "equilibrado" };
            if (string.Equals(policy, "agressivo", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "agressivo" };

            return new List<string> { "cauteloso", "equilibrado", "agressivo" };
        }

        private static List<int> DetermineMilestones(int totalDays)
        {
            var standard = new[] { 30, 180, 720 };
            var list = standard.Where(m => m <= totalDays).ToList();
            if (!list.Contains(totalDays) && totalDays > 0)
            {
                list.Add(totalDays);
            }
            return list.OrderBy(x => x).ToList();
        }

        private static string GenerateCsv(IReadOnlyList<BotMilestoneSnapshot> snapshots)
        {
            var sb = new StringBuilder();
            sb.AppendLine("dificuldade,politica,bot,dia,falido,dia_falencia,saldo,patrimonio,classe_aviao,categoria_piloto,dia_primeira_compra,horas_voadas,receita_hora,custo_hora,margem_pct,manutencoes_100h,panes_100h,dias_parados");
            foreach (var s in snapshots)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6:F2},{7:F2},{8},{9},{10},{11:F1},{12:F2},{13:F2},{14:F1},{15:F2},{16:F2},{17}",
                    s.Difficulty,
                    s.Policy,
                    s.BotId,
                    s.Day,
                    s.IsBankrupt ? "SIM" : "NAO",
                    s.BankruptcyDay?.ToString() ?? "",
                    s.Balance,
                    s.NetWorth,
                    s.AircraftClassKey,
                    (int)s.PilotCategory,
                    s.FirstPurchaseDay?.ToString() ?? "",
                    s.TotalHours,
                    s.RevenuePerHour,
                    s.TotalCostPerHour,
                    s.MarginPct,
                    s.MaintPer100h,
                    s.FailuresPer100h,
                    s.DaysGrounded));
            }
            return sb.ToString();
        }

        private static string GenerateMarkdownReport(
            IReadOnlyList<BotMilestoneSnapshot> snapshots,
            IReadOnlyList<int> milestones,
            CareerProgressionOptions options,
            TimeSpan elapsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Relatório de Estabilidade da Carreira (ECON-STAB)");
            sb.AppendLine();
            sb.AppendLine($"**Executado em:** {DateTime.Now:yyyy-MM-dd HH:mm:ss} | **Duração:** {elapsed.TotalSeconds:F1} s");
            sb.AppendLine($"**Parâmetros:** {options.Days} dias, {options.BotsPerGroup} bots/grupo, modo {(options.FastMode ? "--fast" : "completo")}, semente {options.Seed}");
            sb.AppendLine();

            // 1. Alertas automáticos
            sb.AppendLine("## 1. Alertas Automáticos de Estabilidade");
            sb.AppendLine();
            var alerts = EvaluateAlerts(snapshots, milestones);
            if (alerts.Count == 0)
            {
                sb.AppendLine("✅ **Nenhum desbalanceamento crítico detectado:** a economia é estável a curto, médio e longo prazo.");
            }
            else
            {
                foreach (var alert in alerts)
                {
                    sb.AppendLine($"- ⚠️ **{alert.Title}:** {alert.Description}");
                }
            }
            sb.AppendLine();

            // 2. Resumo por Marco (Dia 30, 180, 720...)
            sb.AppendLine("## 2. Indicadores Consolidados por Marco e Configuração");
            sb.AppendLine();

            foreach (var mDay in milestones)
            {
                sb.AppendLine($"### Marco: Dia {mDay}");
                sb.AppendLine();
                sb.AppendLine("| Dificuldade | Política | Falidos (%) | Dia Falência | Saldo Mediano ($) | Patrimônio Mediano ($) [Mín - Máx] | Classe | Cat. | 1ª Compra (Dia) | Rec/h ($) | Custo/h ($) | Margem (%) | Manut/100h | Panes/100h | Parado (dias) |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");

                var daySnaps = snapshots.Where(s => s.Day == mDay).ToList();
                var groups = daySnaps.GroupBy(s => (s.Difficulty, s.Policy));

                foreach (var g in groups.OrderBy(x => x.Key.Difficulty).ThenBy(x => x.Key.Policy))
                {
                    var items = g.ToList();
                    int totalBots = items.Count;
                    int bankruptCount = items.Count(i => i.IsBankrupt);
                    double bankruptPct = (double)bankruptCount / totalBots * 100.0;
                    var bankruptDays = items.Where(i => i.BankruptcyDay.HasValue).Select(i => (double)i.BankruptcyDay!.Value).ToList();
                    string avgBankruptDay = bankruptDays.Count > 0 ? $"{bankruptDays.Average():F0}" : "-";

                    double medianBalance = Median(items.Select(i => i.Balance).ToList());
                    double medianNetWorth = Median(items.Select(i => i.NetWorth).ToList());
                    double minNetWorth = items.Min(i => i.NetWorth);
                    double maxNetWorth = items.Max(i => i.NetWorth);

                    // Maior classe atingida entre os sobreviventes
                    var active = items.Where(i => !i.IsBankrupt).ToList();
                    string topClass = active.Count > 0 ? active.OrderByDescending(a => a.AircraftClassKey).First().AircraftClassKey : "-";
                    string topCat = active.Count > 0 ? active.Max(a => (int)a.PilotCategory).ToString() : "-";

                    var purchaseDays = items.Where(i => i.FirstPurchaseDay.HasValue).Select(i => (double)i.FirstPurchaseDay!.Value).ToList();
                    string avgPurchaseDay = purchaseDays.Count > 0 ? $"{purchaseDays.Average():F0}" : "-";

                    double avgRevH = items.Average(i => i.RevenuePerHour);
                    double avgCostH = items.Average(i => i.TotalCostPerHour);
                    double avgMargin = items.Average(i => i.MarginPct);
                    double avgMaint100h = items.Average(i => i.MaintPer100h);
                    double avgFail100h = items.Average(i => i.FailuresPer100h);
                    double avgGrounded = items.Average(i => i.DaysGrounded);

                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "| {0} | {1} | {2:F0}% | {3} | ${4:N0} | ${5:N0} [${6:N0} - ${7:N0}] | {8} | {9} | {10} | ${11:N0} | ${12:N0} | {13:F1}% | {14:F1} | {15:F1} | {16:F1} |",
                        g.Key.Difficulty,
                        g.Key.Policy,
                        bankruptPct,
                        avgBankruptDay,
                        medianBalance,
                        medianNetWorth,
                        minNetWorth,
                        maxNetWorth,
                        topClass,
                        topCat,
                        avgPurchaseDay,
                        avgRevH,
                        avgCostH,
                        avgMargin,
                        avgMaint100h,
                        avgFail100h,
                        avgGrounded));
                }
                sb.AppendLine();
            }

            // 3. Efeito da Inflação do Combustível sobre as Margens
            if (milestones.Count >= 2)
            {
                int firstMilestone = milestones.First();
                int lastMilestone = milestones.Last();
                sb.AppendLine($"## 3. Impacto da Inflação do Combustível (Dia {firstMilestone} vs Dia {lastMilestone})");
                sb.AppendLine();
                sb.AppendLine("| Dificuldade | Política | Combustível/h Início ($) | Combustível/h Fim ($) | Margem Início (%) | Margem Fim (%) | Variação Margem |");
                sb.AppendLine("|---|---|---|---|---|---|---|");

                var groups = snapshots.GroupBy(s => (s.Difficulty, s.Policy));
                foreach (var g in groups.OrderBy(x => x.Key.Difficulty).ThenBy(x => x.Key.Policy))
                {
                    var initSnaps = g.Where(s => s.Day == firstMilestone).ToList();
                    var endSnaps = g.Where(s => s.Day == lastMilestone).ToList();

                    double initFuelH = initSnaps.Count > 0 ? initSnaps.Average(s => s.FuelCostPerHour) : 0.0;
                    double endFuelH = endSnaps.Count > 0 ? endSnaps.Average(s => s.FuelCostPerHour) : 0.0;
                    double initMargin = initSnaps.Count > 0 ? initSnaps.Average(s => s.MarginPct) : 0.0;
                    double endMargin = endSnaps.Count > 0 ? endSnaps.Average(s => s.MarginPct) : 0.0;
                    double diffMargin = endMargin - initMargin;

                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "| {0} | {1} | ${2:F2} | ${3:F2} | {4:F1}% | {5:F1}% | {6:+0.0;-0.0}% |",
                        g.Key.Difficulty,
                        g.Key.Policy,
                        initFuelH,
                        endFuelH,
                        initMargin,
                        endMargin,
                        diffMargin));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private sealed record EconomicAlert(string Title, string Description);

        private static List<EconomicAlert> EvaluateAlerts(
            IReadOnlyList<BotMilestoneSnapshot> snapshots,
            IReadOnlyList<int> milestones)
        {
            var alerts = new List<EconomicAlert>();

            // 1. Falência precoce: > 10% dos cautelosos falidos até o dia 30
            var day30Cautelosos = snapshots.Where(s => s.Day == 30 && s.Policy.Equals("cauteloso", StringComparison.OrdinalIgnoreCase)).ToList();
            if (day30Cautelosos.Count > 0)
            {
                int bankruptCount = day30Cautelosos.Count(s => s.IsBankrupt);
                double pct = (double)bankruptCount / day30Cautelosos.Count * 100.0;
                if (pct > 10.0)
                {
                    alerts.Add(new EconomicAlert("Falência Precoce", $"{pct:F0}% dos bots cautelosos faliram até o dia 30 (limite tolerado: 10%). Jogadores cautelosos estão sendo punidos por azar operacional."));
                }
            }

            // 2. Enriquecimento rápido: patrimônio > 20x o inicial até o dia 30 no Médio/Difícil
            var day30MediumHard = snapshots.Where(s => s.Day == 30 && (s.Difficulty.Equals("medium", StringComparison.OrdinalIgnoreCase) || s.Difficulty.Equals("hard", StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var s in day30MediumHard)
            {
                double initBal = s.Difficulty.Equals("medium", StringComparison.OrdinalIgnoreCase) ? CareerStart.MediumInitialBalance : CareerStart.HardInitialBalance;
                if (s.NetWorth > (20.0 * initBal))
                {
                    alerts.Add(new EconomicAlert("Enriquecimento Rápido", $"Bot '{s.BotId}' atingiu patrimônio de ${s.NetWorth:N0} no Dia 30 (> 20x saldo inicial de ${initBal:N0}). A economia concede lucros excessivos precocemente."));
                    break;
                }
            }

            // 3. Estagnação: sem 1ª compra até o dia 180 no Médio
            if (milestones.Contains(180))
            {
                var day180Medium = snapshots.Where(s => s.Day == 180 && s.Difficulty.Equals("medium", StringComparison.OrdinalIgnoreCase) && !s.IsBankrupt).ToList();
                if (day180Medium.Count > 0 && day180Medium.All(s => !s.FirstPurchaseDay.HasValue))
                {
                    alerts.Add(new EconomicAlert("Estagnação", "Nenhum bot sobrevivente na dificuldade Médio realizou a 1ª compra de aeronave até o dia 180. A progressão está excessivamente travada."));
                }
            }

            // 4. Inflação come a margem: margem do dia 720 < 50% da margem do dia 30
            if (milestones.Contains(30) && milestones.Contains(720))
            {
                var d30Margin = snapshots.Where(s => s.Day == 30 && !s.IsBankrupt).Select(s => s.MarginPct).DefaultIfEmpty(0.0).Average();
                var d720Margin = snapshots.Where(s => s.Day == 720 && !s.IsBankrupt).Select(s => s.MarginPct).DefaultIfEmpty(0.0).Average();
                if (d30Margin > 0 && d720Margin < (0.50 * d30Margin))
                {
                    alerts.Add(new EconomicAlert("Inflação Come a Margem", $"A margem operacional caiu de {d30Margin:F1}% no Dia 30 para {d720Margin:F1}% no Dia 720 (< 50% da margem inicial) devido à inflação de combustível sem correção de contratos."));
                }
            }

            return alerts;
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0) return 0.0;
            var sorted = values.OrderBy(x => x).ToList();
            int mid = sorted.Count / 2;
            if (sorted.Count % 2 == 0)
            {
                return (sorted[mid - 1] + sorted[mid]) / 2.0;
            }
            return sorted[mid];
        }
    }
}
