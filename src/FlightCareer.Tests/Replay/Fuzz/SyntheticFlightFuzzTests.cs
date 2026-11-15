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
    public class SyntheticFlightFuzzTests
    {
        private static readonly string[] TargetAircraft =
        {
            "Cessna C152",
            "C172SP G1000",
            "Beechcraft Baron G58",
            "Asobo TBM 930"
        };

        private static (PartCatalog PartsCatalog, WearRuleCatalog WearRuleCatalog, AircraftLimitResolver Resolver, SensorRegistry Registry) LoadCatalogs()
        {
            var parts = PartCatalog.LoadDefault();
            var wear = WearRuleCatalog.LoadDefault();
            var loader = new AircraftLimitCatalogLoader();
            var resolver = loader.CreateResolver();
            var registry = SensorRegistry.LoadDefault();
            return (parts, wear, resolver, registry);
        }

        public static IEnumerable<object[]> GetFuzzSeedsAndAircraft()
        {
            // 25 sementes por modelo = 100 corridas fixas totais cobrindo as 4 aeronaves do catálogo
            for (int i = 1; i <= 25; i++)
            {
                foreach (var ac in TargetAircraft)
                {
                    yield return new object[] { 1000 + i * 37, ac };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetFuzzSeedsAndAircraft))]
        public void Fuzz_Invariantes_DevemSerRespeitadasEmTodosOsVoos(int seed, string aircraftModel)
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById(aircraftModel) ?? resolver.FindProfile(aircraftModel, aircraftModel);
            Assert.NotNull(profile);

            var rnd = new SystemRandomSource(seed);
            int abuseCount = (seed % 4 == 0) ? 0 : 1 + (seed % 3);
            var plan = SyntheticFlightPlan.Random(rnd, profile, abuseCount);

            var scenario = new AircraftScenarioInput(
                profile,
                registry.Sensors,
                profile.Features,
                profile.EngineCount,
                profile.AircraftId);

            var generator = new SyntheticFlightGenerator(resolver);
            var flight = generator.Generate(scenario, plan, rnd);

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(
                flight.Frames,
                flight.Touchdowns,
                profile,
                profile.Features,
                profile.EngineCount,
                activationMode: ReplayActivationMode.GameLike);

            var violations = SyntheticFlightOracle.VerifyInvariants(flight, result);

            Assert.True(violations.Count == 0,
                $"Semente {seed} ({aircraftModel}) violou invariantes:\n" + string.Join("\n", violations));
        }

        [Fact]
        public void Fuzz_Determinismo_MesmaSementeProduzResultadoIdenticoBitABit()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById("Cessna C152");
            Assert.NotNull(profile);

            int seed = 98765;
            var plan = SyntheticFlightPlan.Random(new SystemRandomSource(seed), profile, abuseCount: 2);
            var scenario = new AircraftScenarioInput(profile, registry.Sensors, profile.Features, profile.EngineCount, profile.AircraftId);
            var generator = new SyntheticFlightGenerator(resolver);

            var flight1 = generator.Generate(scenario, plan, new SystemRandomSource(seed));
            var flight2 = generator.Generate(scenario, plan, new SystemRandomSource(seed));

            Assert.Equal(flight1.Frames.Count, flight2.Frames.Count);
            for (int i = 0; i < flight1.Frames.Count; i++)
            {
                Assert.Equal(flight1.Frames[i].ReceivedUtc, flight2.Frames[i].ReceivedUtc);
                Assert.Equal(flight1.Frames[i].SimulationRate, flight2.Frames[i].SimulationRate);
                Assert.Equal(flight1.Frames[i].Values.Count, flight2.Frames[i].Values.Count);
            }

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result1 = replayer.Run(flight1.Frames, flight1.Touchdowns, profile, profile.Features, profile.EngineCount);
            var result2 = replayer.Run(flight2.Frames, flight2.Touchdowns, profile, profile.Features, profile.EngineCount);

            Assert.Equal(result1.TotalSimHours, result2.TotalSimHours);
            Assert.Equal(result1.Parts.Count, result2.Parts.Count);
            for (int i = 0; i < result1.Parts.Count; i++)
            {
                Assert.Equal(result1.Parts[i].FinalHealth, result2.Parts[i].FinalHealth);
                Assert.Equal(result1.Parts[i].HoursSinceReplacement, result2.Parts[i].HoursSinceReplacement);
                Assert.Equal(result1.Parts[i].CyclesSinceReplacement, result2.Parts[i].CyclesSinceReplacement);
            }
        }

        [Fact]
        public void Fuzz_Gabarito_RpmOverspeed_GeraExcedenciaNoMotorCertoEPerda()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById("Cessna C152");
            Assert.NotNull(profile);

            var rnd = new SystemRandomSource(12345);
            var scenario = new AircraftScenarioInput(profile, registry.Sensors, profile.Features, profile.EngineCount, profile.AircraftId);
            var generator = new SyntheticFlightGenerator(resolver);

            // Voo controle normal
            var controlPlan = SyntheticFlightPlan.CreateNormal(durationSeconds: 300.0);
            var controlFlight = generator.Generate(scenario, controlPlan, rnd);

            // Voo com RPM overspeed no motor 1
            var abuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.RpmOverspeed,
                StartSeconds: 120.0,
                DurationSeconds: 25.0,
                Intensity: 0.12,
                EngineIndex: 1,
                TargetRuleId: "piston_rpm_overspeed",
                TargetPartId: "engine.cylinders");

            var abusedPlan = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 300.0,
                Abuses = new[] { abuse }
            };
            var abusedFlight = generator.Generate(scenario, abusedPlan, rnd);

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var controlResult = replayer.Run(controlFlight.Frames, controlFlight.Touchdowns, profile, profile.Features, profile.EngineCount);
            var abusedResult = replayer.Run(abusedFlight.Frames, abusedFlight.Touchdowns, profile, profile.Features, profile.EngineCount);

            var violations = SyntheticFlightOracle.VerifyGroundTruth(controlResult, abusedResult, abuse);
            Assert.Empty(violations);

            var rpmEx = abusedResult.Exceedances.FirstOrDefault(e => e.RuleId == "piston_rpm_overspeed");
            Assert.NotNull(rpmEx);
            Assert.Contains(":1", rpmEx.TargetId);
        }

        [Fact]
        public void Fuzz_Gabarito_HardLanding_GeraDanoEInspecoes()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById("Cessna C152");
            Assert.NotNull(profile);

            var rnd = new SystemRandomSource(54321);
            var scenario = new AircraftScenarioInput(profile, registry.Sensors, profile.Features, profile.EngineCount, profile.AircraftId);
            var generator = new SyntheticFlightGenerator(resolver);

            var abuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.HardLanding,
                StartSeconds: 270.0,
                DurationSeconds: 1.0,
                Intensity: 0.40,
                EngineIndex: 0,
                TargetRuleId: "touchdown_hard_landing",
                TargetPartId: "gear.main_struts");

            var plan = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 300.0,
                Abuses = new[] { abuse }
            };

            var flight = generator.Generate(scenario, plan, rnd);
            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(flight.Frames, flight.Touchdowns, profile, profile.Features, profile.EngineCount);

            // Regra de evento touchdownFpm gera inspeção mandante e dano direto nos amortecedores
            Assert.Contains(result.Inspections, i => i.DefectCode == "HARD_LANDING_INSPECTION" || i.RuleId == "touchdown_hard_landing");
            var gearStrut = result.Parts.First(p => p.PartId == "gear.main_struts");
            Assert.True(gearStrut.HealthLoss > 0.5, $"Esperado dano no trem de pouso após toque duro, obtido perda de {gearStrut.HealthLoss:F2}%");
        }

        [Fact]
        public void Fuzz_Gabarito_AvionicsMasterOnStart_GeraEventoDerivado()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById("C172SP G1000");
            Assert.NotNull(profile);

            var rnd = new SystemRandomSource(112233);
            var scenario = new AircraftScenarioInput(profile, registry.Sensors, profile.Features, profile.EngineCount, profile.AircraftId);
            var generator = new SyntheticFlightGenerator(resolver);

            var abuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.AvionicsMasterOnStart,
                StartSeconds: 15.0,
                DurationSeconds: 10.0,
                Intensity: 0.0,
                EngineIndex: 1,
                TargetRuleId: "avionicsMasterStartSpikes",
                TargetPartId: "electrical.avionics_bus");

            var plan = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 240.0,
                Abuses = new[] { abuse }
            };

            var flight = generator.Generate(scenario, plan, rnd);
            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(flight.Frames, flight.Touchdowns, profile, profile.Features, profile.EngineCount);

            var spikeEvent = result.DerivedEvents.FirstOrDefault(e => e.MetricKey == "avionicsMasterStartSpikes");
            Assert.NotNull(spikeEvent);
            Assert.True(spikeEvent.Value >= 1.0);
        }

        [Fact]
        public void Fuzz_Metamorfico_SimRate1xVs4x_ProduzPerdaSemelhante()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById("Cessna C152");
            Assert.NotNull(profile);

            var rnd = new SystemRandomSource(777);
            var scenario = new AircraftScenarioInput(profile, registry.Sensors, profile.Features, profile.EngineCount, profile.AircraftId);
            var generator = new SyntheticFlightGenerator(resolver);

            // Voo 1: 1x uniforme por 300 segundos
            var plan1x = SyntheticFlightPlan.CreateNormal(durationSeconds: 300.0);
            var flight1x = generator.Generate(scenario, plan1x, rnd);

            // Voo 2: trecho de cruzeiro acelerado a 4x (mesmo tempo simulado)
            var plan4x = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 300.0,
                SimRateSegments = new[] { (120.0, 200.0, 4.0) }
            };
            var flight4x = generator.Generate(scenario, plan4x, rnd);

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result1x = replayer.Run(flight1x.Frames, flight1x.Touchdowns, profile, profile.Features, profile.EngineCount);
            var result4x = replayer.Run(flight4x.Frames, flight4x.Touchdowns, profile, profile.Features, profile.EngineCount);

            // Invariantes devem passar em ambos
            Assert.Empty(SyntheticFlightOracle.VerifyInvariants(flight1x, result1x));
            Assert.Empty(SyntheticFlightOracle.VerifyInvariants(flight4x, result4x));

            // Propriedade metamórfica: taxa maior não gera perdas astronômicas nem NaN
            Assert.True(result4x.TotalSimHours >= result1x.TotalSimHours);
        }

        [Fact]
        public void Fuzz_Metamorfico_BimotorAbusoMotor2_Motor1MantemDesgasteControle()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById("Beechcraft Baron G58");
            Assert.NotNull(profile);

            var rnd = new SystemRandomSource(888);
            var scenario = new AircraftScenarioInput(profile, registry.Sensors, profile.Features, profile.EngineCount, profile.AircraftId);
            var generator = new SyntheticFlightGenerator(resolver);

            // Voo controle sem abuso
            var controlPlan = SyntheticFlightPlan.CreateNormal(durationSeconds: 300.0);
            var controlFlight = generator.Generate(scenario, controlPlan, rnd);

            // Voo com RPM overspeed exclusivo no Motor 2
            var abuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.RpmOverspeed,
                StartSeconds: 130.0,
                DurationSeconds: 20.0,
                Intensity: 0.15,
                EngineIndex: 2,
                TargetRuleId: "piston_rpm_overspeed",
                TargetPartId: "engine.cylinders");

            var abusedPlan = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 300.0,
                Abuses = new[] { abuse }
            };
            var abusedFlight = generator.Generate(scenario, abusedPlan, rnd);

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var controlResult = replayer.Run(controlFlight.Frames, controlFlight.Touchdowns, profile, profile.Features, profile.EngineCount);
            var abusedResult = replayer.Run(abusedFlight.Frames, abusedFlight.Touchdowns, profile, profile.Features, profile.EngineCount);

            var violations = SyntheticFlightOracle.VerifyMetamorphicTwinEngine(controlResult, abusedResult);
            Assert.Empty(violations);

            // Confere que o motor 2 teve excedência e o motor 1 não teve
            Assert.Contains(abusedResult.Exceedances, e => e.RuleId == "piston_rpm_overspeed" && (e.TargetId.Contains(":2") || e.Notes.Contains("Motor 2")));
            Assert.DoesNotContain(abusedResult.Exceedances, e => e.RuleId == "piston_rpm_overspeed" && (e.TargetId.Contains(":1") || e.Notes.Contains("Motor 1")));
        }

        [Fact]
        public void Fuzz_Metamorfico_Monotonicidade_AbusoMaiorGeraPerdaMaiorOuIgual()
        {
            var (partsCatalog, wearRuleCatalog, resolver, registry) = LoadCatalogs();
            var profile = resolver.FindById("Cessna C152");
            Assert.NotNull(profile);

            var rnd = new SystemRandomSource(999);
            var scenario = new AircraftScenarioInput(profile, registry.Sensors, profile.Features, profile.EngineCount, profile.AircraftId);
            var generator = new SyntheticFlightGenerator(resolver);

            // Abuso leve de RPM (intensidade 0.05, 15 s)
            var lowAbuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.RpmOverspeed,
                StartSeconds: 120.0,
                DurationSeconds: 15.0,
                Intensity: 0.05,
                EngineIndex: 1,
                TargetRuleId: "piston_rpm_overspeed",
                TargetPartId: "engine.cylinders");

            var lowPlan = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 300.0,
                Abuses = new[] { lowAbuse }
            };
            var lowFlight = generator.Generate(scenario, lowPlan, rnd);

            // Abuso severo de RPM (intensidade 0.20, 30 s)
            var highAbuse = new SyntheticAbuseRecord(
                SyntheticAbuseType.RpmOverspeed,
                StartSeconds: 120.0,
                DurationSeconds: 30.0,
                Intensity: 0.20,
                EngineIndex: 1,
                TargetRuleId: "piston_rpm_overspeed",
                TargetPartId: "engine.cylinders");

            var highPlan = new SyntheticFlightPlan
            {
                TotalDurationSeconds = 300.0,
                Abuses = new[] { highAbuse }
            };
            var highFlight = generator.Generate(scenario, highPlan, rnd);

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var lowResult = replayer.Run(lowFlight.Frames, lowFlight.Touchdowns, profile, profile.Features, profile.EngineCount);
            var highResult = replayer.Run(highFlight.Frames, highFlight.Touchdowns, profile, profile.Features, profile.EngineCount);

            var violations = SyntheticFlightOracle.VerifyMetamorphicMonotonicity(lowResult, highResult, "engine.cylinders", engineIndex: 1);
            Assert.Empty(violations);

            var lowLoss = lowResult.Parts.First(p => p.PartId == "engine.cylinders" && p.EngineIndex == 1).HealthLoss;
            var highLoss = highResult.Parts.First(p => p.PartId == "engine.cylinders" && p.EngineIndex == 1).HealthLoss;
            Assert.True(highLoss >= lowLoss);
        }
    }
}
