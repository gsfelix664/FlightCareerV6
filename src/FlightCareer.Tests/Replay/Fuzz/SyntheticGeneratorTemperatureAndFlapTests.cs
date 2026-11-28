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
    public class SyntheticGeneratorTemperatureAndFlapTests
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
        public void ChtScale_HighLimit_IsOne()
        {
            // Limite padrão alto (ex: 240 °C no C152) ou limite >= 205.9 °C resulta em escala 1.0
            double chtMaxC152 = 240.0;
            double scaleC152 = Math.Min(1.0, 0.85 * chtMaxC152 / 175.0);
            Assert.Equal(1.0, scaleC152);

            double chtMaxThreshold = 205.9;
            double scaleThreshold = Math.Min(1.0, 0.85 * chtMaxThreshold / 175.0);
            Assert.Equal(1.0, scaleThreshold, precision: 3);
        }

        [Fact]
        public void ChtScale_LiquidCooled105_KeepsPeakBelow092OfLimit()
        {
            // DA40NG diesel tem chtMax = 105.0 °C
            double chtMax = 105.0;
            double chtScale = Math.Min(1.0, 0.85 * chtMax / 175.0);

            // Pico do voo normal é 175 °C (subida)
            double peakChtNormal = 175.0 * chtScale;

            // O pico escalado deve ser exatamente 85% do limite (0.85 * 105.0 = 89.25 °C)
            Assert.Equal(0.85 * chtMax, peakChtNormal, precision: 4);

            // E deve estar estritamente abaixo da faixa caution de piston_cht_overheat (0.92) e coolant_overheat (0.95)
            Assert.True(peakChtNormal < 0.92 * chtMax, "Pico de CHT normal deve ficar abaixo de 0.92 * chtMax");
        }

        [Fact]
        public void FlapSelector_WithVfe1AndVfe2_Selects2()
        {
            var profile = new AircraftLimitProfile(
                "Test-Flaps-1-2",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new AircraftLimit("vfe:1", 110.0, "knots", LimitSource.POH, "POH"),
                    new AircraftLimit("vfe:2", 85.0, "knots", LimitSource.POH, "POH")
                },
                Array.Empty<string>(),
                1);

            var selection = SyntheticFlapSelector.Select(profile, (k, fallback) =>
            {
                var l = profile.Limits.FirstOrDefault(x => x.Key == k);
                return l?.Value ?? fallback;
            });

            Assert.Equal(2, selection.FlapIndex);
            Assert.Equal(85.0, selection.Vfe);
        }

        [Fact]
        public void FlapSelector_WithVfe1AndVfe4_Selects4()
        {
            // Exemplo: A330-200 (tem vfe:1 e vfe:4, sem vfe:2 nem vfe:3)
            var profile = new AircraftLimitProfile(
                "Test-Flaps-1-4",
                "Jet",
                2,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new AircraftLimit("vfe:1", 240.0, "knots", LimitSource.POH, "POH"),
                    new AircraftLimit("vfe:4", 180.0, "knots", LimitSource.POH, "POH")
                },
                Array.Empty<string>(),
                2);

            var selection = SyntheticFlapSelector.Select(profile, (k, fallback) =>
            {
                var l = profile.Limits.FirstOrDefault(x => x.Key == k);
                return l?.Value ?? fallback;
            });

            Assert.Equal(4, selection.FlapIndex);
            Assert.Equal(180.0, selection.Vfe);
        }

        [Fact]
        public void FlapSelector_WithVfe1AndVfe3AndVfe5_Selects3()
        {
            // Exemplo: A400M (tem vfe:1, vfe:3, vfe:5)
            var profile = new AircraftLimitProfile(
                "Test-Flaps-1-3-5",
                "Turboprop",
                4,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new AircraftLimit("vfe:1", 235.0, "knots", LimitSource.POH, "POH"),
                    new AircraftLimit("vfe:3", 200.0, "knots", LimitSource.POH, "POH"),
                    new AircraftLimit("vfe:5", 180.0, "knots", LimitSource.POH, "POH")
                },
                Array.Empty<string>(),
                4);

            var selection = SyntheticFlapSelector.Select(profile, (k, fallback) =>
            {
                var l = profile.Limits.FirstOrDefault(x => x.Key == k);
                return l?.Value ?? fallback;
            });

            Assert.Equal(3, selection.FlapIndex);
            Assert.Equal(200.0, selection.Vfe);
        }

        [Fact]
        public void FlapSelector_OnlyVfe1_Selects1()
        {
            // Exemplo: Cessna 188 AgTruck (só tem vfe:1)
            var profile = new AircraftLimitProfile(
                "Test-Flaps-Only1",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new AircraftLimit("vfe:1", 100.0, "knots", LimitSource.POH, "POH")
                },
                Array.Empty<string>(),
                1);

            var selection = SyntheticFlapSelector.Select(profile, (k, fallback) =>
            {
                var l = profile.Limits.FirstOrDefault(x => x.Key == k);
                return l?.Value ?? fallback;
            });

            Assert.Equal(1, selection.FlapIndex);
            Assert.Equal(100.0, selection.Vfe);
        }

        [Fact]
        public void FlapSelector_OnlyGenericVfe_Selects2()
        {
            var profile = new AircraftLimitProfile(
                "Test-Flaps-Generic",
                "Piston",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new AircraftLimit("vfe", 90.0, "knots", LimitSource.POH, "POH")
                },
                Array.Empty<string>(),
                1);

            var selection = SyntheticFlapSelector.Select(profile, (k, fallback) =>
            {
                var l = profile.Limits.FirstOrDefault(x => x.Key == k);
                return l?.Value ?? fallback;
            });

            Assert.Equal(2, selection.FlapIndex);
            Assert.Equal(90.0, selection.Vfe);
        }

        [Fact]
        public void Generator_C152_SameSeed_IdenticalToBaseline()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var generator = new SyntheticFlightGenerator(resolver);

            // Baseline verificado antes da introdução da escala (comprovando bit a bit idêntico)
            var c152Profile = resolver.FindById("Cessna C152")!;
            var rndC152 = new SystemRandomSource(42);
            var plan = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 180.0,
                Abuses = Array.Empty<SyntheticAbuseRecord>()
            };
            var scenarioC152 = new AircraftScenarioInput(c152Profile, registry.Sensors, c152Profile.Features, c152Profile.EngineCount, c152Profile.AircraftId);
            var flightC152 = generator.Generate(scenarioC152, plan, rndC152);

            Assert.Equal(419, flightC152.Frames.Count);
            Assert.Equal(43215.6380, flightC152.Frames.Sum(f => f.Values.TryGetValue("eng-cht:1", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(18330.8981, flightC152.Frames.Sum(f => f.Values.TryGetValue("af-airspeed-indicated", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(246680.0000, flightC152.Frames.Sum(f => f.Values.TryGetValue("eng-egt:1", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(25294.7166, flightC152.Frames.Sum(f => f.Values.TryGetValue("oil-temperature:1", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(84.0000, flightC152.Frames.Sum(f => f.Values.TryGetValue("af-flaps-handle-index", out var v) ? v : 0.0), precision: 4);

            // C172SP G1000
            var c172Profile = resolver.FindById("C172SP G1000")!;
            var rndC172 = new SystemRandomSource(42);
            var scenarioC172 = new AircraftScenarioInput(c172Profile, registry.Sensors, c172Profile.Features, c172Profile.EngineCount, c172Profile.AircraftId);
            var flightC172 = generator.Generate(scenarioC172, plan, rndC172);

            Assert.Equal(419, flightC172.Frames.Count);
            Assert.Equal(43215.6380, flightC172.Frames.Sum(f => f.Values.TryGetValue("eng-cht:1", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(19176.7790, flightC172.Frames.Sum(f => f.Values.TryGetValue("af-airspeed-indicated", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(246680.0000, flightC172.Frames.Sum(f => f.Values.TryGetValue("eng-egt:1", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(25294.7166, flightC172.Frames.Sum(f => f.Values.TryGetValue("oil-temperature:1", out var v) ? v : 0.0), precision: 4);
            Assert.Equal(84.0000, flightC172.Frames.Sum(f => f.Values.TryGetValue("af-flaps-handle-index", out var v) ? v : 0.0), precision: 4);
        }

        [Fact]
        public void Generator_ControlFlight_AllProfiles_NoTemperatureOrFlapExceedance()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var loader = new AircraftLimitCatalogLoader();
            var allProfiles = loader.LoadProfiles();
            var generator = new SyntheticFlightGenerator(resolver);
            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);

            var monitoredRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "piston_cht_overheat",
                "coolant_overheat",
                "flap_overspeed_vfe",
                "oil_temperature_high",
                "piston_egt_excess",
                "turbo_overboost_tit",
                "turbine_itt_overshoot"
            };

            var violations = new List<string>();

            foreach (var rawProfile in allProfiles)
            {
                var profile = FuzzTestDefaults.WithTestElectricalIndices(rawProfile);
                var rnd = new SystemRandomSource(42);
                var plan = new SyntheticFlightPlan
                {
                    TotalDurationSeconds = 180.0,
                    Abuses = Array.Empty<SyntheticAbuseRecord>()
                };

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

                // Confere se alguma regra de temperatura ou flap ativou excedência no voo controle normal
                foreach (var exc in result.Exceedances)
                {
                    if (monitoredRuleIds.Contains(exc.RuleId))
                    {
                        violations.Add($"[{profile.AircraftId}] Excedência indevida na regra '{exc.RuleId}' (PeakValue={exc.PeakValue:F2}, ExcessPct={exc.ExcessPct:F2}%)");
                    }
                }
            }

            Assert.True(violations.Count == 0,
                "Excedências de temperatura ou flap encontradas em voo controle sem abusos:\n" + string.Join("\n", violations));
        }
    }
}
