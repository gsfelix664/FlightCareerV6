using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Replay;
using FlightCareer.Domain.Replay.Synthetic;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Limits
{
    public class CatalogEstimatesTests
    {
        [Fact]
        public void CatalogEstimates_AllAddedLimits_AreClassDefaultWithPohNote()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            const string expectedPrefix = "estimativa — atualizar pelo POH real";
            int estimateCount = 0;

            foreach (var profile in profiles)
            {
                foreach (var limit in profile.Limits)
                {
                    if (limit.Reference != null && limit.Reference.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        estimateCount++;
                        Assert.Equal(LimitSource.ClassDefault, limit.Source);
                    }
                }
            }

            Assert.True(estimateCount > 0, "Deveria haver limites estimados com a nota do POH no catálogo.");

            // Todos os 44 aviões com sistema elétrico têm elecSystemVolts
            var electricAircraft = profiles.Where(p =>
                p.Features == null || !p.Features.Any(f => string.Equals(f, AircraftFeatures.NoElectrical, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.Equal(44, electricAircraft.Count);

            foreach (var profile in electricAircraft)
            {
                var voltsLimit = profile.Limits.FirstOrDefault(l => string.Equals(l.Key, LimitKeys.ElecSystemVolts, StringComparison.OrdinalIgnoreCase));
                Assert.True(voltsLimit != null, $"Aeronave '{profile.AircraftId}' deveria ter elecSystemVolts.");
                Assert.True(voltsLimit.Value is 14.0 or 28.0, $"Aeronave '{profile.AircraftId}' tem voltagem inválida: {voltsLimit.Value}");
                Assert.Equal("volts", voltsLimit.Unit);
            }
        }

        [Fact]
        public void NoElectricalModels_LoadWithFeature()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            var noElectricalAircraft = profiles.Where(p =>
                p.Features != null && p.Features.Any(f => string.Equals(f, AircraftFeatures.NoElectrical, StringComparison.OrdinalIgnoreCase)))
                .Select(p => p.AircraftId)
                .OrderBy(id => id)
                .ToList();

            Assert.Equal(2, noElectricalAircraft.Count);
            Assert.Contains("CAP-4", noElectricalAircraft);
            Assert.Contains("Piper J-3 Cub", noElectricalAircraft);

            var j3 = profiles.First(p => p.AircraftId == "Piper J-3 Cub");
            Assert.DoesNotContain(j3.Limits, l => string.Equals(l.Key, LimitKeys.ElecSystemVolts, StringComparison.OrdinalIgnoreCase));

            var cap4 = profiles.First(p => p.AircraftId == "CAP-4");
            Assert.DoesNotContain(cap4.Limits, l => string.Equals(l.Key, LimitKeys.ElecSystemVolts, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Random_NoElectricalAircraft_NeverPicksElectricalAbuses()
        {
            var profile = new AircraftLimitProfile(
                "Test-NoElectrical",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<AircraftLimit>(),
                new[] { AircraftFeatures.Piston, AircraftFeatures.NoElectrical },
                1);

            for (int seed = 1; seed <= 500; seed++)
            {
                var rnd = new SystemRandomSource(seed);
                var plan = SyntheticFlightPlan.Random(rnd, profile, abuseCount: 5);

                Assert.DoesNotContain(plan.Abuses, a =>
                    a.Type is SyntheticAbuseType.BatteryDischarging or
                             SyntheticAbuseType.ElectricalOverload or
                             SyntheticAbuseType.AvionicsMasterOnStart);
            }
        }

        [Fact]
        public void VerifyGroundTruth_ElectricalAbuseOnNoElectrical_NoViolation()
        {
            var profile = new AircraftLimitProfile(
                "Test-NoElectrical",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<AircraftLimit>(),
                new[] { AircraftFeatures.NoElectrical },
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

            var abuses = new[]
            {
                new SyntheticAbuseRecord(SyntheticAbuseType.BatteryDischarging, StartSeconds: 10, DurationSeconds: 10, Intensity: 0.1, EngineIndex: 0),
                new SyntheticAbuseRecord(SyntheticAbuseType.ElectricalOverload, StartSeconds: 10, DurationSeconds: 10, Intensity: 0.1, EngineIndex: 0),
                new SyntheticAbuseRecord(SyntheticAbuseType.AvionicsMasterOnStart, StartSeconds: 10, DurationSeconds: 10, Intensity: 0.1, EngineIndex: 0)
            };

            foreach (var abuse in abuses)
            {
                var violations = SyntheticFlightOracle.VerifyGroundTruth(controlResult, abusedResult, abuse, profile);
                Assert.Empty(violations);
            }
        }

        [Fact]
        public void WithTestElectricalIndices_AddsOnlyMissingIndices()
        {
            var initialLimits = new List<AircraftLimit>
            {
                new AircraftLimit(LimitKeys.ElecMainBusIndex, 2.0, "index", LimitSource.Cfg, "manual-test")
            };

            var profile = new AircraftLimitProfile(
                "Test-Plane",
                "Jet",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                initialLimits,
                Array.Empty<string>(),
                1);

            var enhanced = FuzzTestDefaults.WithTestElectricalIndices(profile);

            Assert.Equal(3, enhanced.Limits.Count);

            var mainBus = enhanced.Limits.First(l => l.Key == LimitKeys.ElecMainBusIndex);
            Assert.Equal(2.0, mainBus.Value);
            Assert.Equal(LimitSource.Cfg, mainBus.Source);
            Assert.Equal("manual-test", mainBus.Reference);

            var avionicsCircuit = enhanced.Limits.First(l => l.Key == LimitKeys.ElecAvionicsCircuitIndex);
            Assert.Equal(3.0, avionicsCircuit.Value);
            Assert.Equal(LimitSource.ClassDefault, avionicsCircuit.Source);
            Assert.Equal(FuzzTestDefaults.TestReference, avionicsCircuit.Reference);

            var avionicsBusAmps = enhanced.Limits.First(l => l.Key == LimitKeys.ElecAvionicsBusAmpsIndex);
            Assert.Equal(4.0, avionicsBusAmps.Value);
            Assert.Equal(LimitSource.ClassDefault, avionicsBusAmps.Source);
            Assert.Equal(FuzzTestDefaults.TestReference, avionicsBusAmps.Reference);
        }

        [Fact]
        public void WithTestElectricalIndices_NoElectrical_ReturnsUnchanged()
        {
            var profile = new AircraftLimitProfile(
                "Test-Piper-Cub",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<AircraftLimit>(),
                new[] { AircraftFeatures.NoElectrical },
                1);

            var enhanced = FuzzTestDefaults.WithTestElectricalIndices(profile);

            Assert.Same(profile, enhanced);
            Assert.Empty(enhanced.Limits);
        }

        [Fact]
        public void CatalogProfiles_NeverContainFuzzTestDefaults()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            foreach (var profile in profiles)
            {
                foreach (var limit in profile.Limits)
                {
                    Assert.False(
                        string.Equals(limit.Reference, FuzzTestDefaults.TestReference, StringComparison.OrdinalIgnoreCase),
                        $"Perfil '{profile.AircraftId}' contém indevidamente o padrão de teste do fuzz: {limit.Key}");
                }
            }
        }
    }
}
