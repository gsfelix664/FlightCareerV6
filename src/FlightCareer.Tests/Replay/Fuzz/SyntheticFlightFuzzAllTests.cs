using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Replay;
using FlightCareer.Domain.Replay.Synthetic;
using FlightCareer.Domain.Sensors;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Replay.Fuzz
{
    [Trait("Category", "Fuzz")]
    public class SyntheticFlightFuzzAllTests
    {
        private static (PartCatalog PartsCatalog, WearRuleCatalog WearRuleCatalog, AircraftLimitResolver Resolver, SensorRegistry Registry) LoadCatalogs()
        {
            var parts = PartCatalog.LoadDefault();
            var wear = WearRuleCatalog.LoadDefault();
            var loader = new AircraftLimitCatalogLoader();
            var resolver = loader.CreateResolver();
            var registry = SensorRegistry.LoadDefault();
            return (parts, wear, resolver, registry);
        }

        [Fact]
        public void Random_AircraftWithNoFlaps_NeverPicksFlapOverSpeed()
        {
            var profile = new AircraftLimitProfile(
                "Test-NoFlaps",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<AircraftLimit>(),
                new[] { AircraftFeatures.Piston, AircraftFeatures.NoFlaps },
                1);

            for (int seed = 1; seed <= 500; seed++)
            {
                var rnd = new SystemRandomSource(seed);
                var plan = SyntheticFlightPlan.Random(rnd, profile, abuseCount: 3);
                Assert.DoesNotContain(plan.Abuses, a => a.Type == SyntheticAbuseType.FlapOverSpeed);
            }
        }

        [Fact]
        public void VerifyGroundTruth_FlapOverSpeedOnNoFlapsAircraft_NoMissingLimitNoViolation()
        {
            var profile = new AircraftLimitProfile(
                "Test-NoFlaps",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<AircraftLimit>(),
                new[] { AircraftFeatures.NoFlaps },
                1);

            var controlResult = new FlightReplayResult(
                Array.Empty<ReplayPartItem>(),
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                0.5,
                10,
                TimeSpan.FromSeconds(1));

            var abusedResult = new FlightReplayResult(
                Array.Empty<ReplayPartItem>(),
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                0.5,
                10,
                TimeSpan.FromSeconds(1));

            var abuse = new SyntheticAbuseRecord(SyntheticAbuseType.FlapOverSpeed, StartSeconds: 10, DurationSeconds: 10, Intensity: 0.1, EngineIndex: 0);

            var violations = SyntheticFlightOracle.VerifyGroundTruth(controlResult, abusedResult, abuse, profile);
            Assert.Empty(violations);
        }

        [Fact]
        public void VerifyGroundTruth_FlapOverSpeedWithoutVfeOnFlappedAircraft_StillReportsMissingLimit()
        {
            var profile = new AircraftLimitProfile(
                "Test-Flapped-NoVfe",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<AircraftLimit>(),
                new[] { AircraftFeatures.Piston },
                1);

            var controlResult = new FlightReplayResult(
                Array.Empty<ReplayPartItem>(),
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                0.5,
                10,
                TimeSpan.FromSeconds(1));

            var abusedResult = new FlightReplayResult(
                Array.Empty<ReplayPartItem>(),
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                0.5,
                10,
                TimeSpan.FromSeconds(1));

            var abuse = new SyntheticAbuseRecord(SyntheticAbuseType.FlapOverSpeed, StartSeconds: 10, DurationSeconds: 10, Intensity: 0.1, EngineIndex: 0);

            var violations = SyntheticFlightOracle.VerifyGroundTruth(controlResult, abusedResult, abuse, profile);
            Assert.Single(violations);
            Assert.Equal("limite ausente: vfe", violations[0]);
        }

        [Fact]
        public void VerifyGroundTruth_ShorterAbusedFlight_ScalesControlByRealRate()
        {
            var controlParts = new[]
            {
                new ReplayPartItem("engine.cylinders", 0, 100.0, 99.50, 1.0, 1, false)
            };
            var abusedParts = new[]
            {
                new ReplayPartItem("engine.cylinders", 0, 100.0, 99.58, 0.8, 1, false)
            };

            var controlResult = new FlightReplayResult(
                controlParts,
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                1.0,
                10,
                TimeSpan.FromSeconds(1));

            var abusedResult = new FlightReplayResult(
                abusedParts,
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                0.8,
                10,
                TimeSpan.FromSeconds(1));

            var abuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.OilTempOverheat,
                StartSeconds: 10,
                DurationSeconds: 10,
                Intensity: 0.1,
                EngineIndex: 0,
                TargetPartId: "engine.cylinders");

            var violations = SyntheticFlightOracle.VerifyGroundTruth(controlResult, abusedResult, abuse);
            Assert.Empty(violations);
        }

        [Fact]
        public void VerifyGroundTruth_ShorterAbusedFlight_NoExtraLoss_Violation()
        {
            var controlParts = new[]
            {
                new ReplayPartItem("engine.cylinders", 0, 100.0, 99.50, 1.0, 1, false)
            };
            var abusedParts = new[]
            {
                new ReplayPartItem("engine.cylinders", 0, 100.0, 99.64, 0.8, 1, false)
            };

            var controlResult = new FlightReplayResult(
                controlParts,
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                1.0,
                10,
                TimeSpan.FromSeconds(1));

            var abusedResult = new FlightReplayResult(
                abusedParts,
                Array.Empty<PartWearExceedance>(),
                Array.Empty<PartWearInspectionEvent>(),
                Array.Empty<ReplayDerivedEventItem>(),
                Array.Empty<string>(),
                0.8,
                10,
                TimeSpan.FromSeconds(1));

            var abuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.OilTempOverheat,
                StartSeconds: 10,
                DurationSeconds: 10,
                Intensity: 0.1,
                EngineIndex: 0,
                TargetPartId: "engine.cylinders");

            var violations = SyntheticFlightOracle.VerifyGroundTruth(controlResult, abusedResult, abuse);
            Assert.Single(violations);
            Assert.Contains("comparação escalada", violations[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FuzzRunSummary_RecordsAndAggregatesCorrectly()
        {
            var summary = new FuzzRunSummary();

            summary.RecordRun("Cessna C152", Array.Empty<string>());
            summary.RecordRun("Cessna C152", new[] { "limite ausente: vfe" });
            summary.RecordRun("Cessna C152", new[] { "Gabarito violado: Perda insuficiente" });

            Assert.Equal(3, summary.TotalRuns);
            Assert.Equal(1, summary.TotalPassed);
            Assert.Equal(1, summary.TotalMissingLimits);
            Assert.Equal(1, summary.TotalFailed);

            var acSummary = summary.GetAircraftSummary("Cessna C152");
            Assert.NotNull(acSummary);
            Assert.Equal(3, acSummary.TotalRuns);
            Assert.Equal(1, acSummary.Passed);
            Assert.Equal(1, acSummary.MissingLimits);
            Assert.Equal(1, acSummary.Failed);
            Assert.Equal("Gabarito violado", acSummary.TopViolationCategory);
        }

        [Fact]
        public void FuzzRunSummary_GetOrderedAircraftSummaries_OrdersFailuresFirst()
        {
            var summary = new FuzzRunSummary();

            // Aircraft A: 10 voos, 10 aprovados
            for (int i = 0; i < 10; i++) summary.RecordRun("Aircraft A", Array.Empty<string>());

            // Aircraft B: 10 voos, 2 falhas
            for (int i = 0; i < 8; i++) summary.RecordRun("Aircraft B", Array.Empty<string>());
            summary.RecordRun("Aircraft B", new[] { "Invariante violada: NaN" });
            summary.RecordRun("Aircraft B", new[] { "Gabarito violado: Dano" });

            // Aircraft C: 10 voos, 1 falha
            for (int i = 0; i < 9; i++) summary.RecordRun("Aircraft C", Array.Empty<string>());
            summary.RecordRun("Aircraft C", new[] { "Invariante violada: NaN" });

            // Aircraft D: 10 voos, 1 limite ausente
            for (int i = 0; i < 9; i++) summary.RecordRun("Aircraft D", Array.Empty<string>());
            summary.RecordRun("Aircraft D", new[] { "limite ausente: vfe" });

            var ordered = summary.GetOrderedAircraftSummaries();
            Assert.Equal(4, ordered.Count);
            Assert.Equal("Aircraft B", ordered[0].AircraftId); // 2 falhas
            Assert.Equal("Aircraft C", ordered[1].AircraftId); // 1 falha
            Assert.Equal("Aircraft D", ordered[2].AircraftId); // 0 falhas, 1 limite ausente
            Assert.Equal("Aircraft A", ordered[3].AircraftId); // 0 falhas, 0 limites ausentes
        }

        [Fact]
        public void AircraftCatalog_NoFlapsModels_LoadWithFeature()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            var j3 = profiles.FirstOrDefault(p => p.AircraftId.Equals("Piper J-3 Cub", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(j3);
            Assert.Contains(AircraftFeatures.NoFlaps, j3.Features);

            var cap4 = profiles.FirstOrDefault(p => p.AircraftId.Equals("CAP-4", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(cap4);
            Assert.Contains(AircraftFeatures.NoFlaps, cap4.Features);

            var others = profiles.Where(p => !p.AircraftId.Equals("Piper J-3 Cub", StringComparison.OrdinalIgnoreCase) &&
                                             !p.AircraftId.Equals("CAP-4", StringComparison.OrdinalIgnoreCase));
            foreach (var other in others)
            {
                Assert.DoesNotContain(AircraftFeatures.NoFlaps, other.Features);
            }
        }

        [Fact]
        public void Fuzz_AllProfiles_QuickInvariantsValidation()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var loader = new AircraftLimitCatalogLoader();
            var allProfiles = loader.LoadProfiles();
            var generator = new SyntheticFlightGenerator(resolver);
            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);

            var allViolations = new List<string>();

            foreach (var profile in allProfiles)
            {
                for (int run = 0; run < 2; run++)
                {
                    int seed = 5000 + run * 100;
                    var rnd = new SystemRandomSource(seed);
                    int abuseCount = (run == 0) ? 0 : 2;
                    var plan = SyntheticFlightPlan.Random(rnd, profile, abuseCount);

                    var scenario = new AircraftScenarioInput(
                        profile,
                        registry.Sensors,
                        profile.Features,
                        profile.EngineCount,
                        profile.AircraftId);

                    var flight = generator.Generate(scenario, plan, rnd);
                    var result = replayer.Run(
                        flight.Frames,
                        flight.Touchdowns,
                        profile,
                        profile.Features,
                        profile.EngineCount,
                        activationMode: ReplayActivationMode.GameLike);

                    var violations = SyntheticFlightOracle.VerifyInvariants(flight, result);
                    foreach (var v in violations)
                    {
                        allViolations.Add($"[{profile.AircraftId} Seed {seed}] {v}");
                    }
                }
            }

            Assert.True(allViolations.Count == 0,
                "Violações encontradas no teste de invariantes rápidas para todos os perfis:\n" + string.Join("\n", allViolations));
        }
    }
}
