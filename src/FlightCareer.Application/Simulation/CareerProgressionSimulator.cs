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
using FlightCareer.Domain.Xp;
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
        public CareerPhase CareerPhase { get; init; }
        [Obsolete("Utilize CareerPhase")]
        public PilotCategory PilotCategory => (PilotCategory)(int)CareerPhase;
        public int? FirstPurchaseDay { get; init; }
        public double TotalHours { get; init; }
        public int TotalFlights { get; init; }
        public double RevenuePerHour { get; init; }
        public double FuelCostPerHour { get; init; }
        public double FeesPerHour { get; init; }
        public double MaintCostPerHour { get; init; }
        public double PartsCostPerHour { get; init; }
        public double InsuranceCostPerHour { get; init; }
        public double TotalCostPerHour { get; init; }
        public double MarginPct { get; init; }
        public double MaintPer100h { get; init; }
        public double PartsReplacedPer100h { get; init; }
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

        public sealed record LadderStep(string ClassKey, string ModelSubstring, CareerPhase RequiredPhase, AircraftType Type);

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
            var missionCache = new Dictionary<(string Icao, int Day, DifficultyMode Difficulty), List<Mission>>();

            int groupIndex = 0;
            foreach (var difficulty in difficulties)
            {
                foreach (var policy in policies)
                {
                    groupIndex++;
                    options.LogCallback?.Invoke($"[ECON-STAB] Simulando grupo {groupIndex}/{difficulties.Count * policies.Count}: {difficulty} / {policy} ({options.BotsPerGroup} bots)...");

                    for (int botNum = 1; botNum <= options.BotsPerGroup; botNum++)
                    {
                        int botSeed = DeriveBotSeed(options.Seed, difficulty, policy, botNum);
                        var rnd = new SystemRandomSource(botSeed);
                        var sysRnd = new Random(botSeed);

                        string botId = $"Bot-{difficulty}-{policy}-{botNum}";
                        var diffEcon = DifficultyEconomy.For(difficulty);
                        double balance = CareerStart.GetInitialBalance(difficulty);
                        var phase = CareerPhase.Phase1_FlightSchool;
                        double totalHours = 0.0;
                        int totalFlights = 0;
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
                        double cumPartsCost = 0.0;
                        double cumInsuranceCost = 0.0;
                        int maintEvents = 0;
                        int replacedPartsCount = 0;
                        int failureEvents = 0;
                        int daysGrounded = 0;

                        // Componentes virtuais e peças individuais provisionadas
                        var mapping = PartComponentMapping.CreateDefault();
                        (var features, int engineCount, var engineClass) = ResolveAircraftConfiguration(currentAircraftType, _componentCatalog, _limitResolver);
                        var failureEngine = new FailureEngine(_componentCatalog, engineClass, rnd) { HazardMultiplier = diffEcon.FailureMultiplier };
                        var partFailureEngine = new PartFailureEngine(_componentCatalog, _partCatalog, engineClass, rnd, mapping) { HazardMultiplier = diffEcon.FailureMultiplier };
                        var components = _componentCatalog.Components
                            .Where(c => c.AppliesTo.Contains(engineClass))
                            .Select(c => new AircraftComponent
                            {
                                Id = Guid.NewGuid(),
                                Kind = c.Kind,
                                Health = 100.0,
                                HoursSinceOverhaul = 0.0
                            }).ToList();

                        var parts = ProvisionPartsForAircraft(
                            _partCatalog,
                            features,
                            engineCount,
                            initialHealth: 100.0,
                            startDate: startDate,
                            airframeHours: airframeHours);

                        SyncComponents(components, parts, _partCatalog, mapping);

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
                                        phase,
                                        firstPurchaseDay,
                                        totalHours,
                                        totalFlights,
                                        cumRevenue,
                                        cumFuelCost,
                                        cumFees,
                                        cumMaintCost,
                                        cumInsuranceCost,
                                        cumPartsCost,
                                        replacedPartsCount,
                                        maintEvents,
                                        failureEvents,
                                        daysGrounded));
                                }
                                continue;
                            }

                            // 1. Seguro a cada 30 dias (Dia 1, 31, 61...)
                            if ((day - 1) % 30 == 0)
                            {
                                double premium = EconomyServices.CalculateMonthlyPremium(currentAircraftType.PurchasePrice) * diffEcon.CostMultiplier;
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
                                bool attemptedFerryToday = false;

                                while (dayHoursFlown < options.HoursPerDay && !isBankrupt)
                                {
                                    // Obter missões geradas para o aeroporto neste dia (com semente determinística do aeródromo)
                                    var cacheKey = (currentAirport.ICAO, day, difficulty);
                                    if (!missionCache.TryGetValue(cacheKey, out var availableMissions))
                                    {
                                        int apSeed = unchecked((StableHash(currentAirport.ICAO) * 397) ^ (day * 101) ^ (options.Seed * 17));
                                        var apRnd = new Random(apSeed & 0x7FFFFFFF);
                                        availableMissions = MissionGenerator.GenerateMissionsInMemory(
                                            currentAirport,
                                            allAirports,
                                            _hubDataService,
                                            apRnd,
                                            simulatedDate,
                                            countOverride: 80,
                                            payMultiplier: diffEcon.PayMultiplier);
                                        missionCache[cacheKey] = availableMissions;
                                    }

                                    // Filtra missões permitidas para a categoria do piloto e capacidade do avião
                                    // Mesmo filtro de compatibilidade usado para escolher o destino do reposicionamento
                                    bool IsCompatibleMission(Mission mm)
                                    {
                                        if (mm.RequiredPhase > phase) return false;
                                        if (mm.PayloadLbs > currentAircraftType.MaxPayloadLbs) return false;
                                        if (mm.PassengerCount > currentAircraftType.MaxPax) return false;
                                        if (mm.DistanceNM > currentAircraftType.RangeNM) return false;
                                        if (!airportLookup.TryGetValue(mm.DestinationICAO, out var dAp)) return false;
                                        if (currentAircraftType.MinRunwayLength >= 7000 && dAp.Tier > 1) return false;
                                        if (currentAircraftType.MinRunwayLength >= 4500 && dAp.Tier > 2) return false;
                                        if (currentAircraftType.MinRunwayLength >= 2500 && dAp.Tier > 3) return false;
                                        return true;
                                    }

                                    List<Mission> MissionsAt(Airport ap)
                                    {
                                        var key = (ap.ICAO, day, difficulty);
                                        if (!missionCache.TryGetValue(key, out var list))
                                        {
                                            int seedAp = unchecked((StableHash(ap.ICAO) * 397) ^ (day * 101) ^ (options.Seed * 17));
                                            list = MissionGenerator.GenerateMissionsInMemory(ap, allAirports, _hubDataService, new Random(seedAp & 0x7FFFFFFF), simulatedDate, countOverride: 80, payMultiplier: diffEcon.PayMultiplier);
                                            missionCache[key] = list;
                                        }
                                        return list;
                                    }

                                    var compatible = new List<Mission>();
                                    foreach (var m in availableMissions)
                                    {
                                        if (m.RequiredPhase > phase) continue;
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
                                        if (attemptedFerryToday)
                                        {
                                            // Já reposicionou ou treinou hoje; encerra o dia
                                            break;
                                        }
                                        attemptedFerryToday = true;

                                        // Voo de reposicionamento (Ferry, sem receita, pagando combustível e taxas) para o hub mais próximo com missões
                                        var candidateHubs = allAirports
                                            .Where(a => a.Tier <= 2 &&
                                                        !string.Equals(a.ICAO, currentAirport.ICAO, StringComparison.OrdinalIgnoreCase) &&
                                                        (currentAircraftType.MinRunwayLength < 2500 || a.Tier <= 3) &&
                                                        (currentAircraftType.MinRunwayLength < 4500 || a.Tier <= 2) &&
                                                        (currentAircraftType.MinRunwayLength < 7000 || a.Tier == 1))
                                            .Select(a => (Hub: a, Dist: MissionGenerator.CalculateDistance(currentAirport.Latitude, currentAirport.Longitude, a.Latitude, a.Longitude)))
                                            .OrderBy(x => x.Dist)
                                            .ToList();

                                        // Revisor (2026-11-19): reposicionar para onde HÁ trabalho, como um jogador faria —
                                        // a base inicial primeiro, depois os aeroportos mais próximos (Tier ≤ 3) com missão compatível hoje.
                                        Airport? bestHub = null;
                                        var probe = new List<Airport>();
                                        if (defaultHub != null && !string.Equals(defaultHub.ICAO, currentAirport.ICAO, StringComparison.OrdinalIgnoreCase)) probe.Add(defaultHub);
                                        probe.AddRange(allAirports
                                            .Where(a => a.Tier <= 3 && !string.Equals(a.ICAO, currentAirport.ICAO, StringComparison.OrdinalIgnoreCase))
                                            .Select(a => (Ap: a, Dist: MissionGenerator.CalculateDistance(currentAirport.Latitude, currentAirport.Longitude, a.Latitude, a.Longitude)))
                                            .Where(x => x.Dist <= currentAircraftType.RangeNM)
                                            .OrderBy(x => x.Dist)
                                            .Take(12)
                                            .Select(x => x.Ap));
                                        foreach (var cand in probe)
                                        {
                                            if (MissionsAt(cand).Any(IsCompatibleMission)) { bestHub = cand; break; }
                                        }
                                        bestHub ??= candidateHubs.FirstOrDefault(x => x.Dist <= currentAircraftType.RangeNM).Hub
                                                      ?? candidateHubs.FirstOrDefault().Hub
                                                      ?? defaultHub;

                                        if (string.Equals(bestHub.ICAO, currentAirport.ICAO, StringComparison.OrdinalIgnoreCase))
                                        {
                                            // Se já está no hub e não há missões comerciais hoje, realiza voo local de instrução/treinamento
                                            double localDuration = Math.Min(1.0, Math.Max(0.5, options.HoursPerDay - dayHoursFlown));
                                            if (localDuration < 0.25) break;

                                            double fuelUsedLocal = currentAircraftType.FuelConsumptionGPH * localDuration;
                                            double fuelPriceLocal = FuelPriceService.GetCurrentPrice(currentAircraftType.FuelType ?? "AVGAS", currentAirport.Tier, simulatedDate);
                                            double fuelCostLocal = Math.Round(fuelUsedLocal * fuelPriceLocal * diffEcon.CostMultiplier, 2);

                                            double mtowLocal = AircraftWeightSpecs.GetMtow(currentAircraftType.Model, currentAircraftType);
                                            var feeCtxLocal = new FeeContext(currentAirport.Tier, currentAirport.ICAO, mtowLocal, passengers: 0, hasMission: false, parkedGameHours: 0.0);
                                            double totalFeesLocal = GroundServiceCalculator.Calculate(feeCtxLocal).Sum(f => f.Total) * diffEcon.CostMultiplier;

                                            double lessonIncome = Math.Round(localDuration * 220.0 * diffEcon.PayMultiplier, 2);
                                            balance += lessonIncome - fuelCostLocal - totalFeesLocal;
                                            cumRevenue += lessonIncome;
                                            cumFuelCost += fuelCostLocal;
                                            cumFees += totalFeesLocal;
                                            dayHoursFlown += localDuration;
                                            totalHours += localDuration;
                                            totalFlights++;
                                            clock = CareerClock.AdvanceFlight(clock, localDuration);

                                            airframeHours += localDuration;
                                            hoursUntil50h -= localDuration;
                                            hoursUntil100h -= localDuration;
                                            if (engineClass == EngineClass.Piston) hoursUntil500h -= localDuration;
                                            tboRemaining -= localDuration;

                                            double localFlightMin = localDuration * 60.0;
                                            RunwayFacts? localRwyFacts = currentAirport != null
                                                ? new RunwayFacts(3000.0, 2000.0, currentAirport.Tier >= 4, currentAirport.ElevationFt)
                                                : null;
                                            var localXpInputs = new XpFlightInputs(
                                                AirborneMinutes: localFlightMin,
                                                DistanceNm: 15.0,
                                                LegsCompleted: 1,
                                                MissionFamilyId: "instrucao",
                                                IsFerryOrNoMission: false,
                                                UsedTimeAcceleration: false,
                                                CrashDetectionOn: true,
                                                AutoCoordinationOn: false,
                                                RealismPct: 100.0,
                                                AutoStartUsed: false,
                                                WorstTouchdownFpm: 150.0,
                                                WorstTouchdownG: 1.2,
                                                TouchdownCount: 1,
                                                VneOrStallInfractions: 0,
                                                GExcessInfractionsWithPax: 0,
                                                LightsOrSpeedInfractions: 0,
                                                OffRunwayOrExcursion: false,
                                                Origin: localRwyFacts,
                                                Destination: localRwyFacts,
                                                Landing: null,
                                                BlockMinutes: localFlightMin,
                                                EteMinutes: localFlightMin,
                                                IsUrgentMission: false
                                            );
                                            totalXP += XpCalculator.Compute(localXpInputs).Total;

                                            ApplyFlightWear(parts, _partCatalog, localDuration, _componentCatalog.PartBands);
                                            SyncComponents(components, parts, _partCatalog, mapping);

                                            partFailureEngine.Reset();
                                            var localFailures = partFailureEngine.Evaluate(parts, localDuration, _ => true, Array.Empty<ComponentKind>());
                                            if (localFailures.Count > 0)
                                            {
                                                failureEvents++;
                                                foreach (var fail in localFailures)
                                                {
                                                    fail.Part.Health = 0.0;
                                                    fail.Part.IsBroken = true;
                                                    fail.Part.BrokenAtUtc = simulatedDate;
                                                    ApplyFailureDamage(fail.Kind, fail.Mode.Damage, parts, _partCatalog, mapping);
                                                }
                                                SyncComponents(components, parts, _partCatalog, mapping);
                                            }

                                            continue;
                                        }

                                        double ferryDist = Math.Max(15.0, MissionGenerator.CalculateDistance(currentAirport.Latitude, currentAirport.Longitude, bestHub.Latitude, bestHub.Longitude));
                                        double speedFerry = currentAircraftType.CruiseSpeedKts > 0 ? currentAircraftType.CruiseSpeedKts : 110.0;
                                        double ferryDuration = Math.Max(0.25, (ferryDist / speedFerry) + 0.25);

                                        double fuelUsedFerry = currentAircraftType.FuelConsumptionGPH * ferryDuration;
                                        double fuelPriceFerry = FuelPriceService.GetCurrentPrice(currentAircraftType.FuelType ?? "AVGAS", bestHub.Tier, simulatedDate);
                                        double fuelCostFerry = Math.Round(fuelUsedFerry * fuelPriceFerry * diffEcon.CostMultiplier, 2);

                                        double mtowFerry = AircraftWeightSpecs.GetMtow(currentAircraftType.Model, currentAircraftType);
                                        var feeCtxFerry = new FeeContext(bestHub.Tier, bestHub.ICAO, mtowFerry, passengers: 0, hasMission: false, parkedGameHours: 0.0);
                                        double totalFeesFerry = GroundServiceCalculator.Calculate(feeCtxFerry).Sum(f => f.Total) * diffEcon.CostMultiplier;

                                        if (balance < fuelCostFerry && balance < 0)
                                        {
                                            isBankrupt = true;
                                            bankruptcyDay = day;
                                            break;
                                        }

                                        balance -= (fuelCostFerry + totalFeesFerry);
                                        cumFuelCost += fuelCostFerry;
                                        cumFees += totalFeesFerry;
                                        dayHoursFlown += ferryDuration;
                                        totalHours += ferryDuration;
                                        totalFlights++;
                                        clock = CareerClock.AdvanceFlight(clock, ferryDuration);

                                        airframeHours += ferryDuration;
                                        hoursUntil50h -= ferryDuration;
                                        hoursUntil100h -= ferryDuration;
                                        if (engineClass == EngineClass.Piston) hoursUntil500h -= ferryDuration;
                                        tboRemaining -= ferryDuration;

                                        double ferryFlightMin = ferryDuration * 60.0;
                                        double ferryCruise = Math.Max(1.0, currentAircraftType?.CruiseSpeedKts ?? 100.0);
                                        double ferryEte = (ferryDist / ferryCruise) * 60.0 + 15.0;
                                        RunwayFacts? ferryOrigRwy = currentAirport != null ? new RunwayFacts(3000.0, 2000.0, currentAirport.Tier >= 4, currentAirport.ElevationFt) : null;
                                        RunwayFacts? ferryDestRwy = bestHub != null ? new RunwayFacts(3000.0, 2000.0, bestHub.Tier >= 4, bestHub.ElevationFt) : null;
                                        var ferryXpInputs = new XpFlightInputs(
                                            AirborneMinutes: ferryFlightMin,
                                            DistanceNm: ferryDist,
                                            LegsCompleted: 1,
                                            MissionFamilyId: null,
                                            IsFerryOrNoMission: true,
                                            UsedTimeAcceleration: false,
                                            CrashDetectionOn: true,
                                            AutoCoordinationOn: false,
                                            RealismPct: 100.0,
                                            AutoStartUsed: false,
                                            WorstTouchdownFpm: 150.0,
                                            WorstTouchdownG: 1.2,
                                            TouchdownCount: 1,
                                            VneOrStallInfractions: 0,
                                            GExcessInfractionsWithPax: 0,
                                            LightsOrSpeedInfractions: 0,
                                            OffRunwayOrExcursion: false,
                                            Origin: ferryOrigRwy,
                                            Destination: ferryDestRwy,
                                            Landing: null,
                                            BlockMinutes: ferryFlightMin,
                                            EteMinutes: ferryEte,
                                            IsUrgentMission: false
                                        );
                                        totalXP += XpCalculator.Compute(ferryXpInputs).Total;

                                        ApplyFlightWear(parts, _partCatalog, ferryDuration, _componentCatalog.PartBands);
                                        SyncComponents(components, parts, _partCatalog, mapping);

                                        partFailureEngine.Reset();
                                        var ferryFailures = partFailureEngine.Evaluate(parts, ferryDuration, _ => true, Array.Empty<ComponentKind>());
                                        if (ferryFailures.Count > 0)
                                        {
                                            failureEvents++;
                                            foreach (var fail in ferryFailures)
                                            {
                                                fail.Part.Health = 0.0;
                                                fail.Part.IsBroken = true;
                                                fail.Part.BrokenAtUtc = simulatedDate;
                                                ApplyFailureDamage(fail.Kind, fail.Mode.Damage, parts, _partCatalog, mapping);
                                            }
                                            SyncComponents(components, parts, _partCatalog, mapping);
                                        }

                                        currentAirport = bestHub;
                                        continue;
                                    }

                                    // Escolha da melhor missão (diversificada pelo gerador de cada bot)
                                    double speed = currentAircraftType.CruiseSpeedKts > 0 ? currentAircraftType.CruiseSpeedKts : 110.0;
                                    var ranked = compatible
                                        .OrderByDescending(m => m.RewardMoney / Math.Max(0.2, (m.DistanceNM / speed) + 0.25))
                                        .ToList();

                                    Mission chosen;
                                    if (ranked.Count == 1)
                                    {
                                        chosen = ranked[0];
                                    }
                                    else
                                    {
                                        int topN = Math.Min(3, ranked.Count);
                                        double roll = sysRnd.NextDouble();
                                        if (roll < 0.60 || topN < 2) chosen = ranked[0];
                                        else if (roll < 0.85 || topN < 3) chosen = ranked[1];
                                        else chosen = ranked[2];
                                    }

                                    double flightDuration = Math.Max(0.2, (chosen.DistanceNM / speed) + 0.25);
                                    var destAirport = airportLookup[chosen.DestinationICAO];

                                    // Walkaround antes de cada voo (política cautelosa troca visíveis amarelas e vermelhas)
                                    if (policy.Equals("cauteloso", StringComparison.OrdinalIgnoreCase))
                                    {
                                        PartKnowledge.Reveal(parts, def => def.VisibleOnWalkaround, _partCatalog, airframeHours);
                                        var walkaroundBands = _componentCatalog.PartBands;
                                        var toFix = parts.Where(p =>
                                        {
                                            var def = _partCatalog.Get(p.PartId);
                                            return def != null && def.VisibleOnWalkaround &&
                                                   (p.IsBroken || (p.KnownHealth.HasValue && p.KnownHealth.Value < walkaroundBands.YellowBelowPct));
                                        }).ToList();

                                        if (toFix.Count > 0)
                                        {
                                            var plan = PartReplacementPlanner.Plan(toFix, _partCatalog, currentClassKey, simulatedDate, bands: walkaroundBands);
                                            if (plan.Items.Count > 0)
                                            {
                                                double planCost = plan.TotalCost * diffEcon.CostMultiplier;
                                                balance -= planCost;
                                                cumPartsCost += planCost;
                                                cumMaintCost += planCost;
                                                replacedPartsCount += plan.Items.Count;
                                                maintEvents++;
                                                remainingDowntimeHours += plan.DowntimeHours;
                                                PartReplacementPlanner.Apply(plan, airframeHours, simulatedDate);
                                                SyncComponents(components, parts, _partCatalog, mapping);
                                            }
                                        }
                                    }

                                    // Custos operacionais do voo
                                    double fuelUsed = currentAircraftType.FuelConsumptionGPH * flightDuration;
                                    double fuelPrice = FuelPriceService.GetCurrentPrice(currentAircraftType.FuelType ?? "AVGAS", destAirport.Tier, simulatedDate);
                                    double fuelCost = Math.Round(fuelUsed * fuelPrice * diffEcon.CostMultiplier, 2);

                                    double mtowLbs = AircraftWeightSpecs.GetMtow(currentAircraftType.Model, currentAircraftType);
                                    var feeCtx = new FeeContext(
                                        destAirport.Tier,
                                        destAirport.ICAO,
                                        mtowLbs,
                                        chosen.PassengerCount,
                                        hasMission: true,
                                        parkedGameHours: 0.0);
                                    var feeLines = GroundServiceCalculator.Calculate(feeCtx);
                                    double totalFees = feeLines.Sum(f => f.Total) * diffEcon.CostMultiplier;

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
                                    totalFlights++;
                                    clock = CareerClock.AdvanceFlight(clock, flightDuration);

                                    airframeHours += flightDuration;
                                    hoursUntil50h -= flightDuration;
                                    hoursUntil100h -= flightDuration;
                                    if (engineClass == EngineClass.Piston) hoursUntil500h -= flightDuration;
                                    tboRemaining -= flightDuration;

                                    // XP do voo (XP-NOVO)
                                    double botFlightMin = flightDuration * 60.0;
                                    double botCruise = Math.Max(1.0, currentAircraftType?.CruiseSpeedKts ?? 100.0);
                                    double botEte = (chosen.DistanceNM / botCruise) * 60.0 + 15.0;
                                    double botMinReqFt = currentAircraftType?.MinRunwayLength ?? 2000.0;
                                    RunwayFacts? botOrigRwy = currentAirport != null ? new RunwayFacts(Math.Max(botMinReqFt * 1.1, 3000.0), botMinReqFt, currentAirport.Tier >= 4, currentAirport.ElevationFt) : null;
                                    RunwayFacts? botDestRwy = destAirport != null ? new RunwayFacts(Math.Max(botMinReqFt * 1.1, 3000.0), botMinReqFt, destAirport.Tier >= 4, destAirport.ElevationFt) : null;
                                    string? botFamilyId = MissionPayTable.Shared.Resolve(chosen.SubId)?.Id;
                                    bool botIsUrgent = chosen.Urgency > 1.0 || (chosen.SubId != null && (chosen.SubId.StartsWith("3.3") || chosen.SubId.StartsWith("4.3.1")));
                                    var botXpInputs = new XpFlightInputs(
                                        AirborneMinutes: botFlightMin,
                                        DistanceNm: chosen.DistanceNM,
                                        LegsCompleted: 1,
                                        MissionFamilyId: botFamilyId,
                                        IsFerryOrNoMission: false,
                                        UsedTimeAcceleration: false,
                                        CrashDetectionOn: true,
                                        AutoCoordinationOn: false,
                                        RealismPct: 100.0,
                                        AutoStartUsed: false,
                                        WorstTouchdownFpm: 150.0,
                                        WorstTouchdownG: 1.2,
                                        TouchdownCount: 1,
                                        VneOrStallInfractions: 0,
                                        GExcessInfractionsWithPax: 0,
                                        LightsOrSpeedInfractions: 0,
                                        OffRunwayOrExcursion: false,
                                        Origin: botOrigRwy,
                                        Destination: botDestRwy,
                                        Landing: null,
                                        BlockMinutes: botFlightMin,
                                        EteMinutes: botEte,
                                        IsUrgentMission: botIsUrgent
                                    );
                                    totalXP += XpCalculator.Compute(botXpInputs).Total;

                                    // 1. Desgaste natural das peças neste voo (--fast)
                                    ApplyFlightWear(parts, _partCatalog, flightDuration, _componentCatalog.PartBands);

                                    // 2. Barra dos componentes = agregado das peças antes de FailureEngine.Evaluate
                                    SyncComponents(components, parts, _partCatalog, mapping);

                                    // 3. Avaliação de panes via PartFailureEngine
                                    partFailureEngine.Reset();
                                    var failures = partFailureEngine.Evaluate(parts, flightDuration, _ => true, Array.Empty<ComponentKind>());
                                    bool hadGroundingFailure = false;
                                    if (failures.Count > 0)
                                    {
                                        failureEvents++;
                                        foreach (var fail in failures)
                                        {
                                            fail.Part.Health = 0.0;
                                            fail.Part.IsBroken = true;
                                            fail.Part.BrokenAtUtc = simulatedDate;

                                            // Roteia dano do modo de falha para as peças do grupo (DamageGroup)
                                            ApplyFailureDamage(fail.Kind, fail.Mode.Damage, parts, _partCatalog, mapping);

                                            string? defectCode = DefectCatalog.CodeFor(fail.Mode.Effect, fail.Kind, critical: true);
                                            if (!string.IsNullOrEmpty(defectCode))
                                            {
                                                var defectSpec = DefectCatalog.Get(defectCode);
                                                if (defectSpec.IsGrounding)
                                                {
                                                    hadGroundingFailure = true;
                                                }
                                            }
                                        }

                                        // Sincroniza barras após o dano
                                        SyncComponents(components, parts, _partCatalog, mapping);
                                    }

                                    // Checa também se peças NO-GO quebraram
                                    if (parts.Any(p => p.IsBroken && (_partCatalog.Get(p.PartId)?.BlocksFlightWhenBroken == true)))
                                    {
                                        hadGroundingFailure = true;
                                    }

                                    if (hadGroundingFailure)
                                    {
                                        // Reparo de interdição via substituição de peças NO-GO ou quebradas
                                        var partsToFix = parts.Where(p => p.IsBroken || (_partCatalog.Get(p.PartId)?.BlocksFlightWhenBroken == true && p.Health < _componentCatalog.PartBands.RedBelowPct)).ToList();
                                        if (partsToFix.Count > 0)
                                        {
                                            var fixPlan = PartReplacementPlanner.Plan(partsToFix, _partCatalog, currentClassKey, simulatedDate, bands: _componentCatalog.PartBands);
                                            double fixCost = fixPlan.TotalCost * diffEcon.CostMultiplier;
                                            if (balance < fixCost && balance < 0)
                                            {
                                                isBankrupt = true;
                                                bankruptcyDay = day;
                                                break;
                                            }

                                            balance -= fixCost;
                                            cumPartsCost += fixCost;
                                            cumMaintCost += fixCost;
                                            replacedPartsCount += fixPlan.Items.Count;
                                            maintEvents++;
                                            remainingDowntimeHours += Math.Max(12.0, fixPlan.DowntimeHours);
                                            PartReplacementPlanner.Apply(fixPlan, airframeHours, simulatedDate);
                                            SyncComponents(components, parts, _partCatalog, mapping);
                                        }
                                        else
                                        {
                                            remainingDowntimeHours += 12.0;
                                        }

                                        currentAirport = destAirport;
                                        break;
                                    }

                                    currentAirport = destAirport;
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
                                        phase,
                                        firstPurchaseDay,
                                        totalHours,
                                        totalFlights,
                                        cumRevenue,
                                        cumFuelCost,
                                        cumFees,
                                        cumMaintCost,
                                        cumInsuranceCost,
                                        cumPartsCost,
                                        replacedPartsCount,
                                        maintEvents,
                                        failureEvents,
                                        daysGrounded));
                                }
                                continue;
                            }

                            // 4. Manutenção de fim de dia conforme a política e regras do Hangar
                            ApplyPolicyMaintenance(
                                policy,
                                ref balance,
                                ref cumMaintCost,
                                ref cumPartsCost,
                                ref replacedPartsCount,
                                ref maintEvents,
                                ref remainingDowntimeHours,
                                ref hoursUntil50h,
                                ref hoursUntil100h,
                                ref hoursUntil500h,
                                ref tboRemaining,
                                currentAircraftType,
                                currentClassKey,
                                engineClass,
                                parts,
                                components,
                                _partCatalog,
                                mapping,
                                airframeHours,
                                simulatedDate,
                                day,
                                ref isBankrupt,
                                ref bankruptcyDay,
                                _componentCatalog.PartBands,
                                costMultiplier: diffEcon.CostMultiplier);

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
                                        phase,
                                        firstPurchaseDay,
                                        totalHours,
                                        totalFlights,
                                        cumRevenue,
                                        cumFuelCost,
                                        cumFees,
                                        cumMaintCost,
                                        cumInsuranceCost,
                                        cumPartsCost,
                                        replacedPartsCount,
                                        maintEvents,
                                        failureEvents,
                                        daysGrounded));
                                }
                                continue;
                            }

                            // 5. Promoção de fase de carreira (se cumpriu nível + horas + saldo com reserva)
                            CheckPhasePromotion(
                                ref phase,
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
                                parts,
                                ref components,
                                ref failureEngine,
                                ref partFailureEngine,
                                ref engineClass,
                                _partCatalog,
                                _componentCatalog,
                                _limitResolver,
                                mapping,
                                rnd,
                                policy,
                                phase,
                                simulatedDate,
                                day,
                                failureMultiplier: diffEcon.FailureMultiplier);

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
                                    phase,
                                    firstPurchaseDay,
                                    totalHours,
                                    totalFlights,
                                    cumRevenue,
                                    cumFuelCost,
                                    cumFees,
                                    cumMaintCost,
                                    cumInsuranceCost,
                                    cumPartsCost,
                                    replacedPartsCount,
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

        private static (IReadOnlyList<string> Features, int EngineCount, EngineClass EngineClass) ResolveAircraftConfiguration(
            AircraftType type,
            ComponentCatalog compCatalog,
            AircraftLimitResolver resolver)
        {
            var profile = resolver.FindById(type.Model) ?? resolver.FindProfile(type.Model, type.Model);
            var engineClass = compCatalog.ResolveEngineClass(type);
            int engineCount = profile?.EngineCount ?? 1;

            if (profile != null && profile.Features != null && profile.Features.Count > 0)
            {
                return (profile.Features, engineCount, engineClass);
            }

            var features = new List<string>();
            if (engineClass == EngineClass.Piston)
            {
                features.Add("piston");
                if (type.Model.Contains("172", StringComparison.OrdinalIgnoreCase))
                {
                    features.Add("variablePitchProp");
                    features.Add("fuelInjected");
                }
                else if (type.Model.Contains("Baron", StringComparison.OrdinalIgnoreCase) ||
                         type.Model.Contains("DA62", StringComparison.OrdinalIgnoreCase))
                {
                    features.Add("variablePitchProp");
                    features.Add("fuelInjected");
                    engineCount = 2;
                }
            }
            else if (engineClass == EngineClass.Turboprop)
            {
                features.Add("turboprop");
                if (type.Model.Contains("King Air", StringComparison.OrdinalIgnoreCase) ||
                    type.Model.Contains("ATR", StringComparison.OrdinalIgnoreCase))
                {
                    engineCount = 2;
                    features.Add("pressurized");
                    features.Add("retractableGear");
                }
            }
            else if (engineClass == EngineClass.Jet)
            {
                features.Add("jet");
                features.Add("pressurized");
                features.Add("retractableGear");
                engineCount = 2;
            }

            return (features, engineCount, engineClass);
        }

        private static List<AircraftPart> ProvisionPartsForAircraft(
            PartCatalog partCatalog,
            IReadOnlyCollection<string> features,
            int engineCount,
            double initialHealth,
            DateTime startDate,
            double airframeHours)
        {
            var applicableDefs = partCatalog.PartsFor(features);
            var parts = new List<AircraftPart>();
            foreach (var def in applicableDefs)
            {
                int instances = def.PerEngine ? Math.Max(1, engineCount) : 1;
                for (int eng = 1; eng <= instances; eng++)
                {
                    parts.Add(new AircraftPart
                    {
                        Id = Guid.NewGuid(),
                        PartId = def.Id,
                        EngineIndex = eng,
                        Health = initialHealth,
                        HoursSinceReplacement = 0.0,
                        CyclesSinceReplacement = 0,
                        IsBroken = false,
                        LastReplacedAtUtc = startDate,
                        LastReplacedAirframeHours = airframeHours
                    });
                }
            }
            return parts;
        }

        private static void SyncComponents(
            IReadOnlyList<AircraftComponent> components,
            IReadOnlyList<AircraftPart> parts,
            PartCatalog catalog,
            PartComponentMapping mapping)
        {
            foreach (var comp in components)
            {
                var groupParts = parts.Where(p =>
                {
                    var def = catalog.Get(p.PartId);
                    return def != null && mapping.ResolveComponentKind(def.Group) == comp.Kind;
                }).ToList();

                if (groupParts.Count > 0)
                {
                    comp.Health = PartComponentBridge.AggregateHealth(groupParts, catalog, mapping.Aggregation);
                }
            }
        }

        private static void ApplyFlightWear(
            IReadOnlyList<AircraftPart> parts,
            PartCatalog catalog,
            double flightDuration,
            PartBandSettings? bands = null)
        {
            bands ??= new PartBandSettings();
            foreach (var part in parts)
            {
                if (part.IsBroken) continue;
                var def = catalog.Get(part.PartId);
                if (def == null) continue;

                double usageFraction = 1.0;
                if (!string.IsNullOrEmpty(def.UsageSensor))
                {
                    if (def.UsageSensor.Contains("nav", StringComparison.OrdinalIgnoreCase) ||
                        def.UsageSensor.Contains("strobe", StringComparison.OrdinalIgnoreCase) ||
                        def.UsageSensor.Contains("beacon", StringComparison.OrdinalIgnoreCase))
                    {
                        usageFraction = 1.0;
                    }
                    else if (def.UsageSensor.Contains("landing", StringComparison.OrdinalIgnoreCase) ||
                             def.UsageSensor.Contains("taxi", StringComparison.OrdinalIgnoreCase))
                    {
                        usageFraction = 0.15;
                    }
                    else if (def.UsageSensor.Contains("panel", StringComparison.OrdinalIgnoreCase))
                    {
                        usageFraction = 0.0;
                    }
                    else
                    {
                        usageFraction = 1.0;
                    }
                }

                double wearMult = PartHealthBands.LowHealthWearMultiplier(part.Health, bands);

                if (def.LifeHours > 0)
                {
                    double effectiveHours = flightDuration * usageFraction;
                    part.HoursSinceReplacement += effectiveHours;
                    double wear = (70.0 / def.LifeHours) * effectiveHours * 1.15 * wearMult;
                    part.Health = Math.Clamp(part.Health - wear, 0.0, 100.0);
                }
                else if (def.LifeCycles.HasValue && def.LifeCycles.Value > 0)
                {
                    part.CyclesSinceReplacement += 1;
                    double wear = (70.0 / def.LifeCycles.Value) * wearMult;
                    part.Health = Math.Clamp(part.Health - wear, 0.0, 100.0);
                }

                if (part.Health <= 0.0)
                {
                    part.IsBroken = true;
                    part.Health = 0.0;
                }
            }
        }

        private static void ApplyFailureDamage(
            ComponentKind kind,
            double amount,
            IReadOnlyList<AircraftPart> parts,
            PartCatalog catalog,
            PartComponentMapping mapping)
        {
            if (amount <= 0) return;
            foreach (var p in parts)
            {
                if (p.IsBroken) continue;
                var def = catalog.Get(p.PartId);
                if (def == null || def.MinorPart) continue;
                if (mapping.ResolveComponentKind(def.Group) == kind)
                {
                    p.Health = Math.Clamp(p.Health - amount, 0.0, 100.0);
                    if (p.Health <= 0.0)
                    {
                        p.IsBroken = true;
                    }
                }
            }
        }

        private static void ApplyPolicyMaintenance(
            string policy,
            ref double balance,
            ref double cumMaintCost,
            ref double cumPartsCost,
            ref int replacedPartsCount,
            ref int maintEvents,
            ref double remainingDowntimeHours,
            ref double hoursUntil50h,
            ref double hoursUntil100h,
            ref double hoursUntil500h,
            ref double tboRemaining,
            AircraftType aircraftType,
            string classKey,
            EngineClass engineClass,
            List<AircraftPart> parts,
            List<AircraftComponent> components,
            PartCatalog partCatalog,
            PartComponentMapping mapping,
            double airframeHours,
            DateTime simulatedDate,
            int day,
            ref bool isBankrupt,
            ref int? bankruptcyDay,
            PartBandSettings? bands = null,
            double costMultiplier = 1.0)
        {
            bands ??= new PartBandSettings();
            bool isPiston = engineClass == EngineClass.Piston;

            if (policy.Equals("agressivo", StringComparison.OrdinalIgnoreCase))
            {
                // Política Agressiva:
                // 1. Só a inspeção anual (25h / 100h regulatória mandatória): paga taxa e inspeciona
                if (hoursUntil100h <= 0)
                {
                    double cost = MaintenancePricing.Cost100hInspection * costMultiplier;
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

                    // Anual SÓ inspeciona: revela todas as peças e marca achados vermelhos/quebrados
                    PartKnowledge.Reveal(parts, _ => true, partCatalog, airframeHours);
                    PartKnowledge.MarkAnnualFindings(parts, partCatalog, bands, airframeHours);
                    // Agressivo NÃO troca voluntariamente as peças reveladas nem troca óleo
                }

                // 2. Quando o avião fica parado por quebra, só troca o que está quebrado ou é NO-GO
                if (remainingDowntimeHours > 0 || parts.Any(p => p.IsBroken || (partCatalog.Get(p.PartId)?.BlocksFlightWhenBroken == true && p.Health < bands.RedBelowPct)))
                {
                    var brokenOrNoGo = parts.Where(p => p.IsBroken || (partCatalog.Get(p.PartId)?.BlocksFlightWhenBroken == true && p.Health < bands.RedBelowPct)).ToList();
                    if (brokenOrNoGo.Count > 0)
                    {
                        var plan = PartReplacementPlanner.Plan(brokenOrNoGo, partCatalog, classKey, simulatedDate, bands: bands);
                        if (plan.Items.Count > 0)
                        {
                            double planCost = plan.TotalCost * costMultiplier;
                            if (balance < planCost && balance < 0)
                            {
                                isBankrupt = true;
                                bankruptcyDay = day;
                                return;
                            }
                            balance -= planCost;
                            cumPartsCost += planCost;
                            cumMaintCost += planCost;
                            replacedPartsCount += plan.Items.Count;
                            remainingDowntimeHours += plan.DowntimeHours;
                            maintEvents++;
                            PartReplacementPlanner.Apply(plan, airframeHours, simulatedDate);
                            SyncComponents(components, parts, partCatalog, mapping);
                        }
                    }
                }
                return;
            }

            // Políticas Cauteloso e Equilibrado:
            bool isCauteloso = policy.Equals("cauteloso", StringComparison.OrdinalIgnoreCase);

            // 1. Inspeção anual (100h / 25h): SÓ inspeciona (não troca óleo nem zera contador de 12h)
            if (hoursUntil100h <= 0)
            {
                double cost = MaintenancePricing.Cost100hInspection * costMultiplier;
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

                // Revela todas as peças e marca achados da anual
                PartKnowledge.Reveal(parts, _ => true, partCatalog, airframeHours);
                PartKnowledge.MarkAnnualFindings(parts, partCatalog, bands, airframeHours);

                // Cauteloso: troca amarelas + vermelhas reveladas. Equilibrado: troca só vermelhas reveladas.
                var toFix = parts.Where(p =>
                {
                    if (p.IsBroken) return true;
                    if (!p.KnownHealth.HasValue) return false;
                    return isCauteloso
                        ? p.KnownHealth.Value < bands.YellowBelowPct
                        : p.KnownHealth.Value < bands.RedBelowPct;
                }).ToList();

                if (toFix.Count > 0)
                {
                    var plan = PartReplacementPlanner.Plan(toFix, partCatalog, classKey, simulatedDate, bands: bands);
                    if (plan.Items.Count > 0)
                    {
                        double planCost = plan.TotalCost * costMultiplier;
                        if (balance < planCost && balance < 0)
                        {
                            isBankrupt = true;
                            bankruptcyDay = day;
                            return;
                        }
                        balance -= planCost;
                        cumPartsCost += planCost;
                        cumMaintCost += planCost;
                        replacedPartsCount += plan.Items.Count;
                        remainingDowntimeHours += plan.DowntimeHours;
                        PartReplacementPlanner.Apply(plan, airframeHours, simulatedDate);
                        SyncComponents(components, parts, partCatalog, mapping);
                    }
                }
            }

            // 2. Troca de óleo (50h / 12h): troca óleo e filtro
            if (hoursUntil50h <= 0)
            {
                double serviceCost = MaintenancePricing.Cost50hInspection * costMultiplier;
                if (balance >= serviceCost || balance >= 0)
                {
                    balance -= serviceCost;
                    cumMaintCost += serviceCost;
                    maintEvents++;
                    hoursUntil50h = MaintenancePricing.OilChangeIntervalHours;
                    remainingDowntimeHours += CareerClock.Inspection50hDowntimeHours;

                    // Revela motor e óleo
                    PartKnowledge.Reveal(parts, def => mapping.ResolveComponentKind(def.Group) is ComponentKind.Engine or ComponentKind.Oil, partCatalog, airframeHours);

                    var oilParts = parts.Where(p =>
                    {
                        var def = partCatalog.Get(p.PartId);
                        return def != null && mapping.ResolveComponentKind(def.Group) == ComponentKind.Oil
                            && (p.PartId.Contains("lubricant") || p.PartId.Contains("filter") || p.PartId.Contains("oil"));
                    }).ToList();

                    if (oilParts.Count > 0)
                    {
                        // As peças da troca de óleo estão incluídas no preço do pacote (igual ao Hangar)
                        replacedPartsCount += oilParts.Count;
                        PartReplacementPlanner.ApplyToParts(oilParts, airframeHours, simulatedDate);
                        SyncComponents(components, parts, partCatalog, mapping);
                    }

                    // Se após a revisão de óleo houver peças reveladas de motor/óleo que precisam de troca:
                    var oilGroupFix = parts.Where(p =>
                    {
                        var def = partCatalog.Get(p.PartId);
                        if (def == null || mapping.ResolveComponentKind(def.Group) is not (ComponentKind.Engine or ComponentKind.Oil)) return false;
                        if (p.IsBroken) return true;
                        if (!p.KnownHealth.HasValue) return false;
                        return isCauteloso ? p.KnownHealth.Value < bands.YellowBelowPct : p.KnownHealth.Value < bands.RedBelowPct;
                    }).ToList();

                    if (oilGroupFix.Count > 0)
                    {
                        var extraPlan = PartReplacementPlanner.Plan(oilGroupFix, partCatalog, classKey, simulatedDate, bands: bands);
                        if (extraPlan.Items.Count > 0)
                        {
                            double extraCost = extraPlan.TotalCost * costMultiplier;
                            balance -= extraCost;
                            cumPartsCost += extraCost;
                            cumMaintCost += extraCost;
                            replacedPartsCount += extraPlan.Items.Count;
                            remainingDowntimeHours += extraPlan.DowntimeHours;
                            PartReplacementPlanner.Apply(extraPlan, airframeHours, simulatedDate);
                            SyncComponents(components, parts, partCatalog, mapping);
                        }
                    }
                }
            }

            // 3. Revisão de ignição (pistão a cada 125h)
            if (isPiston && hoursUntil500h <= 0)
            {
                double cost = MaintenancePricing.Cost500hInspection * costMultiplier;
                if (balance >= cost || balance >= 0)
                {
                    balance -= cost;
                    cumMaintCost += cost;
                    maintEvents++;
                    hoursUntil500h = MaintenancePricing.IgnitionRevisionIntervalHours;
                    remainingDowntimeHours += CareerClock.Inspection500hDowntimeHours;

                    PartKnowledge.Reveal(parts, def => mapping.ResolveComponentKind(def.Group) == ComponentKind.Ignition, partCatalog, airframeHours);

                    var ignParts = parts.Where(p =>
                    {
                        var def = partCatalog.Get(p.PartId);
                        return def != null && mapping.ResolveComponentKind(def.Group) == ComponentKind.Ignition;
                    }).ToList();

                    if (ignParts.Count > 0)
                    {
                        // As peças da revisão de ignição estão incluídas no preço do pacote (igual ao Hangar)
                        replacedPartsCount += ignParts.Count;
                        PartReplacementPlanner.ApplyToParts(ignParts, airframeHours, simulatedDate);
                        SyncComponents(components, parts, partCatalog, mapping);
                    }
                }
            }

            // 4. Overhaul (TBO)
            if (tboRemaining <= 0)
            {
                double cost = MaintenancePricing.CalculateOverhaulCost(aircraftType.PurchasePrice) * costMultiplier;
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

                PartKnowledge.Reveal(parts, def =>
                {
                    var k = mapping.ResolveComponentKind(def.Group);
                    return k is ComponentKind.Engine or ComponentKind.HotSection or ComponentKind.Oil;
                }, partCatalog, airframeHours);

                var overhaulParts = parts.Where(p =>
                {
                    var def = partCatalog.Get(p.PartId);
                    if (def == null) return false;
                    var kind = mapping.ResolveComponentKind(def.Group);
                    return kind is ComponentKind.Engine or ComponentKind.HotSection or ComponentKind.Oil;
                }).ToList();

                if (overhaulParts.Count > 0)
                {
                    // As peças da retífica estão incluídas no preço do pacote (igual ao Hangar)
                    replacedPartsCount += overhaulParts.Count;
                    PartReplacementPlanner.ApplyToParts(overhaulParts, airframeHours, simulatedDate);
                    SyncComponents(components, parts, partCatalog, mapping);
                }
            }
        }

        private static void CheckPhasePromotion(
            ref CareerPhase currentPhase,
            ref double balance,
            double totalHours,
            int totalXP,
            string policy,
            AircraftType currentType,
            string currentClassKey)
        {
            if (currentPhase >= CareerPhase.Phase7_InternationalAirline) return;

            var nextPhase = (CareerPhase)((int)currentPhase + 1);
            int reqLevel = nextPhase.GetEntryLevel();
            double promoCost = nextPhase.GetPromotionCost();
            double reqHours = nextPhase.GetMinHours();
            int currentLevel = PilotLevel.ForXp(totalXP);

            if (currentLevel >= reqLevel && totalHours >= reqHours)
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
                    currentPhase = nextPhase;
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
            List<AircraftPart> parts,
            ref List<AircraftComponent> components,
            ref FailureEngine failureEngine,
            ref PartFailureEngine partFailureEngine,
            ref EngineClass engineClass,
            PartCatalog partCatalog,
            ComponentCatalog componentCatalog,
            AircraftLimitResolver limitResolver,
            PartComponentMapping mapping,
            IRandomSource rnd,
            string policy,
            CareerPhase pilotPhase,
            DateTime simulatedDate,
            int day,
            double failureMultiplier = 1.0)
        {
            if (currentLadderIdx + 1 >= ladder.Count) return;

            var nextStep = ladder[currentLadderIdx + 1];
            if (pilotPhase < nextStep.RequiredPhase)
            {
                // Fase regulatória do piloto não autoriza o próximo avião
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

                // Reprovisiona peças e componentes para a nova aeronave
                var config = ResolveAircraftConfiguration(targetType, componentCatalog, limitResolver);
                engineClass = config.EngineClass;
                var currentEngClass = engineClass;
                failureEngine = new FailureEngine(componentCatalog, currentEngClass, rnd) { HazardMultiplier = failureMultiplier };
                partFailureEngine = new PartFailureEngine(componentCatalog, partCatalog, currentEngClass, rnd, mapping) { HazardMultiplier = failureMultiplier };
                components = componentCatalog.Components
                    .Where(c => c.AppliesTo.Contains(currentEngClass))
                    .Select(c => new AircraftComponent
                    {
                        Id = Guid.NewGuid(),
                        Kind = c.Kind,
                        Health = 100.0,
                        HoursSinceOverhaul = 0.0
                    }).ToList();

                parts.Clear();
                double initialHealth = (chosenBuyPrice == newPrice) ? 100.0 : 80.0;
                parts.AddRange(ProvisionPartsForAircraft(partCatalog, config.Features, config.EngineCount, initialHealth, simulatedDate, airframeHours));
                SyncComponents(components, parts, partCatalog, mapping);
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

        /// <summary>
        /// Hash FNV-1a estável entre execuções. string.GetHashCode() é aleatório por processo no .NET,
        /// o que tornava a ECON-STAB não reprodutível com a mesma semente.
        /// </summary>

        public static int StableHash(string? text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }

        public static int DeriveBotSeed(int baseSeed, DifficultyMode difficulty, string policy, int botIndex)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + baseSeed;
                hash = hash * 31 + (int)difficulty * 1009;
                hash = hash * 31 + (policy != null ? StableHash(policy.ToLowerInvariant()) : 0);
                hash = hash * 31 + botIndex * 503;
                int seed = hash & 0x7FFFFFFF;
                return seed == 0 ? 1 : seed;
            }
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
            CareerPhase phase,
            int? firstPurchaseDay,
            double totalHours,
            int totalFlights,
            double cumRevenue,
            double cumFuel,
            double cumFees,
            double cumMaint,
            double cumInsurance,
            double cumPartsCost,
            int replacedPartsCount,
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
            double partsH = totalHours > 0 ? cumPartsCost / totalHours : 0.0;
            double insH = totalHours > 0 ? cumInsurance / totalHours : 0.0;
            double totalCostH = totalHours > 0 ? totalExpenses / totalHours : 0.0;
            double marginPct = cumRevenue > 0 ? ((cumRevenue - totalExpenses) / cumRevenue) * 100.0 : 0.0;

            double maint100h = totalHours > 0 ? (maintEvents / totalHours) * 100.0 : 0.0;
            double parts100h = totalHours > 0 ? (replacedPartsCount / totalHours) * 100.0 : 0.0;
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
                CareerPhase = phase,
                FirstPurchaseDay = firstPurchaseDay,
                TotalHours = Math.Round(totalHours, 1),
                TotalFlights = totalFlights,
                RevenuePerHour = Math.Round(revH, 2),
                FuelCostPerHour = Math.Round(fuelH, 2),
                FeesPerHour = Math.Round(feesH, 2),
                MaintCostPerHour = Math.Round(maintH, 2),
                PartsCostPerHour = Math.Round(partsH, 2),
                InsuranceCostPerHour = Math.Round(insH, 2),
                TotalCostPerHour = Math.Round(totalCostH, 2),
                MarginPct = Math.Round(marginPct, 1),
                MaintPer100h = Math.Round(maint100h, 2),
                PartsReplacedPer100h = Math.Round(parts100h, 2),
                FailuresPer100h = Math.Round(fail100h, 2),
                DaysGrounded = daysGrounded
            };
        }

        private static List<LadderStep> BuildAircraftLadder(IReadOnlyList<AircraftType> allTypes)
        {
            var list = new List<LadderStep>();

            // Escada A -> B -> E -> F -> G -> H -> I -> J mapeada para CareerPhase
            var c152 = allTypes.FirstOrDefault(t => t.Model.Contains("152"))
                ?? throw new InvalidOperationException("C152 não encontrado.");
            list.Add(new LadderStep("A", "Cessna C152", CareerPhase.Phase1_FlightSchool, c152));

            var c172 = allTypes.FirstOrDefault(t => t.Model.Contains("172"))
                ?? c152;
            list.Add(new LadderStep("B", "C172SP G1000", CareerPhase.Phase1_FlightSchool, c172));

            var baron = allTypes.FirstOrDefault(t => t.Model.Contains("Baron"))
                ?? allTypes.FirstOrDefault(t => t.Model.Contains("DA62"))
                ?? c172;
            list.Add(new LadderStep("E", "Beechcraft Baron G58", CareerPhase.Phase3_AirTaxi, baron));

            var c208 = allTypes.FirstOrDefault(t => t.Model.Contains("208"))
                ?? allTypes.FirstOrDefault(t => t.Model.Contains("TBM"))
                ?? baron;
            list.Add(new LadderStep("F", "C208B", CareerPhase.Phase3_AirTaxi, c208));

            var kingAir = allTypes.FirstOrDefault(t => t.Model.Contains("King Air 250") || t.Model.Contains("King Air"))
                ?? allTypes.FirstOrDefault(t => t.Model.Contains("ATR 42"))
                ?? c208;
            list.Add(new LadderStep("G", "Beechcraft King Air 250", CareerPhase.Phase4_HighPerformanceAndMedevac, kingAir));

            var cj4 = allTypes.FirstOrDefault(t => t.Model.Contains("CJ4") || t.Model.Contains("Citation"))
                ?? kingAir;
            list.Add(new LadderStep("H", "Cessna Citation CJ4", CareerPhase.Phase4_HighPerformanceAndMedevac, cj4));

            var a320 = allTypes.FirstOrDefault(t => t.Model.Contains("320") || t.Model.Contains("737"))
                ?? cj4;
            list.Add(new LadderStep("I", "A320neo", CareerPhase.Phase6_DomesticAirline, a320));

            var a330 = allTypes.FirstOrDefault(t => t.Model.Contains("330") || t.Model.Contains("747") || t.Model.Contains("787"))
                ?? a320;
            list.Add(new LadderStep("J", "A330-200", CareerPhase.Phase7_InternationalAirline, a330));

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
            var standard = new[] { 30, 60, 90, 180, 365, 540, 730, 1095 };
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
            sb.AppendLine("dificuldade,politica,bot,dia,falido,dia_falencia,saldo,patrimonio,classe_aviao,categoria_piloto,dia_primeira_compra,horas_voadas,total_voos,voos_dia,receita_hora,custo_hora,custo_pecas_hora,margem_pct,manutencoes_100h,pecas_trocadas_100h,panes_100h,dias_parados");
            foreach (var s in snapshots)
            {
                double flightsPerDay = s.Day > 0 ? (double)s.TotalFlights / s.Day : 0.0;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6:F2},{7:F2},{8},{9},{10},{11:F1},{12},{13:F2},{14:F2},{15:F2},{16:F2},{17:F1},{18:F2},{19:F2},{20:F2},{21}",
                    s.Difficulty,
                    s.Policy,
                    s.BotId,
                    s.Day,
                    s.IsBankrupt ? "SIM" : "NAO",
                    s.BankruptcyDay?.ToString() ?? "",
                    s.Balance,
                    s.NetWorth,
                    s.AircraftClassKey,
                    (int)s.CareerPhase,
                    s.FirstPurchaseDay?.ToString() ?? "",
                    s.TotalHours,
                    s.TotalFlights,
                    flightsPerDay,
                    s.RevenuePerHour,
                    s.TotalCostPerHour,
                    s.PartsCostPerHour,
                    s.MarginPct,
                    s.MaintPer100h,
                    s.PartsReplacedPer100h,
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
            var alerts = EvaluateAlerts(snapshots, milestones, options);
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
                sb.AppendLine("| Dificuldade | Política | Falidos (%) | Dia Falência | Saldo Mediano ($) | Patrimônio Mediano ($) [Mín - Máx] | Classe | Fase | 1ª Compra (Dia) | Horas Voadas | Voos/Dia | Rec/h ($) | Custo/h ($) | Peças/h ($) | Margem (%) | Manut/100h | Peças/100h | Panes/100h | Parado (dias) |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");

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
                    string topPhase = active.Count > 0 ? active.Max(a => (int)a.CareerPhase).ToString() : "-";

                    var purchaseDays = items.Where(i => i.FirstPurchaseDay.HasValue).Select(i => (double)i.FirstPurchaseDay!.Value).ToList();
                    string avgPurchaseDay = purchaseDays.Count > 0 ? $"{purchaseDays.Average():F0}" : "-";

                    double medianHours = Median(items.Select(i => i.TotalHours).ToList());
                    double avgFlightsPerDay = items.Average(i => i.Day > 0 ? (double)i.TotalFlights / i.Day : 0.0);

                    double avgRevH = items.Average(i => i.RevenuePerHour);
                    double avgCostH = items.Average(i => i.TotalCostPerHour);
                    double avgPartsH = items.Average(i => i.PartsCostPerHour);
                    double avgMargin = items.Average(i => i.MarginPct);
                    double avgMaint100h = items.Average(i => i.MaintPer100h);
                    double avgParts100h = items.Average(i => i.PartsReplacedPer100h);
                    double avgFail100h = items.Average(i => i.FailuresPer100h);
                    double avgGrounded = items.Average(i => i.DaysGrounded);

                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "| {0} | {1} | {2:F0}% | {3} | ${4:N0} | ${5:N0} [${6:N0} - ${7:N0}] | {8} | {9} | {10} | {11:F1} h | {12:F2} | ${13:N0} | ${14:N0} | ${15:N0} | {16:F1}% | {17:F1} | {18:F1} | {19:F1} | {20:F1} |",
                        g.Key.Difficulty,
                        g.Key.Policy,
                        bankruptPct,
                        avgBankruptDay,
                        medianBalance,
                        medianNetWorth,
                        minNetWorth,
                        maxNetWorth,
                        topClass,
                        topPhase,
                        avgPurchaseDay,
                        medianHours,
                        avgFlightsPerDay,
                        avgRevH,
                        avgCostH,
                        avgPartsH,
                        avgMargin,
                        avgMaint100h,
                        avgParts100h,
                        avgFail100h,
                        avgGrounded));
                }
                sb.AppendLine();

                // Detalhamento do status de compra e categoria no marco
                sb.AppendLine("**Status de Progressão de Aeronave e Categoria:**");
                foreach (var g in groups.OrderBy(x => x.Key.Difficulty).ThenBy(x => x.Key.Policy))
                {
                    var activeBots = g.Where(i => !i.IsBankrupt).ToList();
                    if (activeBots.Count == 0)
                    {
                        sb.AppendLine($"- *{g.Key.Difficulty} / {g.Key.Policy}:* Todos os bots faliram.");
                        continue;
                    }
                    var sampleBot = activeBots.First();
                    string statusText;
                    if (sampleBot.FirstPurchaseDay.HasValue)
                    {
                        statusText = $"Aeronave adquirida (Classe {sampleBot.AircraftClassKey}, 1ª compra no Dia {sampleBot.FirstPurchaseDay.Value}).";
                    }
                    else
                    {
                        if (sampleBot.CareerPhase < CareerPhase.Phase2_SAE)
                        {
                            statusText = $"Bloqueado por Fase: piloto em Fase 1 ({sampleBot.TotalHours:F1}/40.0 h voadas para homologar Fase 2 e liberar novas operações).";
                        }
                        else
                        {
                            statusText = $"Aguardando capital: saldo atual ${sampleBot.Balance:N0} com política '{sampleBot.Policy}'.";
                        }
                    }
                    sb.AppendLine($"- *{g.Key.Difficulty} / {g.Key.Policy}:* {statusText}");
                }
                sb.AppendLine();
            }

            // 3. Detalhamento dos Custos Operacionais por Hora
            sb.AppendLine("## 3. Detalhamento dos Custos Operacionais por Hora");
            sb.AppendLine();
            foreach (var mDay in milestones)
            {
                sb.AppendLine($"### Marco: Dia {mDay}");
                sb.AppendLine();
                sb.AppendLine("| Dificuldade | Política | Combustível/h ($) | Taxas/h ($) | Troca Peças/h ($) | Manutenção Geral/h ($) | Seguro/h ($) | Custo Total/h ($) |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|");

                var daySnaps = snapshots.Where(s => s.Day == mDay).ToList();
                var groups = daySnaps.GroupBy(s => (s.Difficulty, s.Policy));

                foreach (var g in groups.OrderBy(x => x.Key.Difficulty).ThenBy(x => x.Key.Policy))
                {
                    var items = g.ToList();
                    double avgFuel = items.Average(i => i.FuelCostPerHour);
                    double avgFees = items.Average(i => i.FeesPerHour);
                    double avgParts = items.Average(i => i.PartsCostPerHour);
                    double avgMaint = items.Average(i => i.MaintCostPerHour);
                    double avgIns = items.Average(i => i.InsuranceCostPerHour);
                    double avgTot = items.Average(i => i.TotalCostPerHour);

                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "| {0} | {1} | ${2:F2} | ${3:F2} | ${4:F2} | ${5:F2} | ${6:F2} | ${7:F2} |",
                        g.Key.Difficulty,
                        g.Key.Policy,
                        avgFuel,
                        avgFees,
                        avgParts,
                        avgMaint,
                        avgIns,
                        avgTot));
                }
                sb.AppendLine();
            }

            // 4. Efeito da Inflação do Combustível sobre as Margens
            if (milestones.Count >= 2)
            {
                int firstMilestone = milestones.First();
                int lastMilestone = milestones.Last();
                sb.AppendLine($"## 4. Impacto da Inflação do Combustível (Dia {firstMilestone} vs Dia {lastMilestone})");
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
            IReadOnlyList<int> milestones,
            CareerProgressionOptions options)
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

            // 5. Bot parado: bot sobrevivente voando < 50% das horas/dia configuradas
            foreach (var mDay in milestones)
            {
                var activeBotsAtMilestone = snapshots.Where(s => s.Day == mDay && !s.IsBankrupt).ToList();
                var stalled = activeBotsAtMilestone.Where(s => (s.TotalHours / s.Day) < (0.5 * options.HoursPerDay)).ToList();
                if (stalled.Count > 0)
                {
                    var sample = stalled[0];
                    double actualHpd = sample.TotalHours / sample.Day;
                    alerts.Add(new EconomicAlert("Bot Parado", $"Detectado(s) {stalled.Count} bot(s) ativo(s) com baixa atividade de voo no Dia {mDay} (ex.: '{sample.BotId}' com {sample.TotalHours:F1} h voadas, média {actualHpd:F2} h/dia < {0.5 * options.HoursPerDay:F2} h/dia tolerado)."));
                    break;
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
