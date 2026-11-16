using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class C152RulesTests
    {
        private static readonly PartCatalog PartsCatalog = PartCatalog.LoadDefault();
        private static readonly WearRuleCatalog WearRules = WearRuleCatalog.LoadDefault();
        private static readonly AircraftLimitCatalogLoader Loader = new();
        private static readonly AircraftLimitResolver Resolver = Loader.CreateResolver();

        private static AircraftLimitProfile CreatePistonProfileWithNegLimit152()
        {
            var limits = new List<AircraftLimit>
            {
                new(LimitKeys.GLimitNeg, -1.52, "g", LimitSource.ClassDefault, "Padrão da classe pistão"),
                new(LimitKeys.GLimitPos, 3.8, "g", LimitSource.ClassDefault, "Padrão da classe pistão")
            };

            return new AircraftLimitProfile(
                "C152_TEST",
                "A",
                1,
                new[] { "C152" },
                new[] { "C152" },
                limits,
                new[] { "piston", "singleEngine" },
                1);
        }

        [Fact]
        public void OverGNegative_GAbaixoDoLimite_AplicaFaixa()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreatePistonProfileWithNegLimit152();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var wingPart = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "airframe.wing_structure",
                EngineIndex = 0,
                Health = 100.0,
                HoursSinceReplacement = 0.0
            };

            // Teste 1: af-g-force = -1.45 (com gLimitNeg = -1.52, caution threshold 0.88 -> -1.3376)
            // -1.45 <= -1.3376 -> ativa faixa caution (multiplier = 2.5)
            var frameCaution = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["af-g-force"] = -1.45,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);

            var resultCaution = engine.ApplyTick(new[] { wingPart }, frameCaution, 1.0, profile.Features, resolver, profile);

            Assert.True(wingPart.Health < 100.0);
            var cautionExceedance = Assert.Single(resultCaution.Exceedances, e => e.TargetId == "airframe.wing_structure");
            Assert.Equal("airframe_over_g_negative", cautionExceedance.RuleId);
            Assert.Contains("caution", cautionExceedance.Notes, StringComparison.OrdinalIgnoreCase);

            // Teste 2: af-g-force = -1.60 (warning threshold 1.00 -> -1.52)
            // -1.60 <= -1.52 -> ativa faixa warning (multiplier = 8.0, lossPerHourPct = 5.0)
            wingPart.Health = 100.0;
            var frameWarning = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["af-g-force"] = -1.60,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);

            var resultWarning = engine.ApplyTick(new[] { wingPart }, frameWarning, 1.0, profile.Features, resolver, profile);

            var warningExceedance = Assert.Single(resultWarning.Exceedances, e => e.TargetId == "airframe.wing_structure");
            Assert.Equal("airframe_over_g_negative", warningExceedance.RuleId);
            Assert.Contains("warning", warningExceedance.Notes, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OverGNegative_GNormal_NaoAplica()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreatePistonProfileWithNegLimit152();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var wingPart = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "airframe.wing_structure",
                EngineIndex = 0,
                Health = 100.0,
                HoursSinceReplacement = 0.0
            };

            // Teste com G positivo normal (+1.0)
            var framePositive = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["af-g-force"] = 1.0,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);

            var resultPositive = engine.ApplyTick(new[] { wingPart }, framePositive, 1.0, profile.Features, resolver, profile);
            Assert.DoesNotContain(resultPositive.Exceedances, e => e.RuleId == "airframe_over_g_negative");

            // Teste com G negativo leve dentro do envelope (-0.5)
            var frameMildNegative = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["af-g-force"] = -0.5,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);

            var resultMild = engine.ApplyTick(new[] { wingPart }, frameMildNegative, 1.0, profile.Features, resolver, profile);
            Assert.DoesNotContain(resultMild.Exceedances, e => e.RuleId == "airframe_over_g_negative");
        }

        [Fact]
        public void LimiteNegativo_RegraAbove_Ignorada()
        {
            var customRule = new WearRule
            {
                Id = "test_above_negative_limit",
                Description = "Regra de teste Above com limite negativo",
                Kind = WearRuleKind.Continuous,
                LimitKey = "testLimitNeg",
                Direction = LimitDirection.Above,
                Sensors = new[] { "af-g-force" },
                Bands = new[]
                {
                    new WearRuleBand(
                        "warning",
                        1.0,
                        new[] { new WearRulePart("airframe.wing_structure", 5.0, 2.0) },
                        null)
                }
            };

            var customCatalog = new WearRuleCatalog(new[] { customRule });
            var customEngine = new PartWearEngine(PartsCatalog, customCatalog);

            var limits = new List<AircraftLimit>
            {
                new("testLimitNeg", -2.0, "g", LimitSource.POH, "Test POH")
            };

            var profile = new AircraftLimitProfile(
                "TEST_AC", "A", 1, new[] { "Test" }, new[] { "Test" },
                limits, new[] { "piston" }, 1);

            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var wingPart = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "airframe.wing_structure",
                EngineIndex = 0,
                Health = 100.0,
                HoursSinceReplacement = 0.0
            };

            var frame = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["af-g-force"] = 0.0
            }, DateTime.UtcNow, 1.0);

            var result = customEngine.ApplyTick(new[] { wingPart }, frame, 1.0, profile.Features, resolver, profile);

            Assert.Empty(result.Exceedances);
            Assert.Contains(result.DiagnosticMessages, d => d.Contains("test_above_negative_limit") && d.Contains("Above com limite negativo"));
        }

        [Fact]
        public void EgtExcess_AviaoSemEgtMax_UsaPadraoDaClasse()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var baronProfile = Resolver.FindById("Beechcraft Baron G58") ?? Resolver.FindProfile("Baron 58", "B58");
            Assert.NotNull(baronProfile);

            // Baron 58 não tem egtMax no seu JSON próprio
            Assert.DoesNotContain(baronProfile.Limits, l => l.Key.Equals(LimitKeys.EgtMax, StringComparison.OrdinalIgnoreCase));

            // Resolver deve retornar 893.52 da classe Piston
            var resolvedEgt = Resolver.Get(baronProfile, baronProfile.Class, LimitKeys.EgtMax);
            Assert.NotNull(resolvedEgt);
            Assert.Equal(893.52, resolvedEgt.Value, 2);
            Assert.Equal(LimitSource.ClassDefault, resolvedEgt.Source);

            var valvesPart = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.valves",
                EngineIndex = 1,
                Health = 100.0,
                HoursSinceReplacement = 0.0
            };

            // Regra piston_egt_excess: caution threshold = 0.95 -> 0.95 * 893.52 = 848.84 °C
            // Com EGT = 860 °C, deve ativar caution
            var frame = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-egt:1"] = 860.0,
                ["eng-max-egt"] = 893.52,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);

            var result = engine.ApplyTick(new[] { valvesPart }, frame, 1.0, baronProfile.Features, Resolver, baronProfile);

            var exceedance = Assert.Single(result.Exceedances, e => e.TargetId == "engine.valves:1");
            Assert.Equal("piston_egt_excess", exceedance.RuleId);
            Assert.Contains("caution", exceedance.Notes, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GearCycle_EscalaPercent_UmPorcentoNaoEhBaixado()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.RetractableGear };

            // Sequência: 0% (recolhido) -> 1% (em trânsito) -> 50% (em trânsito) -> 100% (baixado)
            // 0% -> _gearWasRetracted = true
            // 1% -> não conta como baixado (pois isDown exige gearPos >= 90.0)
            // 50% -> não conta como baixado
            // 100% -> baixado e travado -> emite exatamente 1 ciclo!

            var frame0 = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["gear-position"] = 0.0,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);
            var ev0 = deriver.Update(frame0, 1.0, features);
            Assert.DoesNotContain(ev0, e => e.MetricKey == WearRuleMetrics.GearCycles);

            var frame1 = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["gear-position"] = 1.0, // 1% não é baixado!
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);
            var ev1 = deriver.Update(frame1, 1.0, features);
            Assert.DoesNotContain(ev1, e => e.MetricKey == WearRuleMetrics.GearCycles);

            var frame50 = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["gear-position"] = 50.0,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);
            var ev50 = deriver.Update(frame50, 1.0, features);
            Assert.DoesNotContain(ev50, e => e.MetricKey == WearRuleMetrics.GearCycles);

            var frame100 = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["gear-position"] = 100.0,
                ["ctx-sim-on-ground"] = 0.0
            }, DateTime.UtcNow, 1.0);
            var ev100 = deriver.Update(frame100, 1.0, features);

            var gearCycleEvent = Assert.Single(ev100, e => e.MetricKey == WearRuleMetrics.GearCycles);
            Assert.Equal(1.0, gearCycleEvent.Value);
        }

        [Fact]
        public void FlightPhase_OnGroundPorRoda_Detecta()
        {
            // Cenário 1: frame apenas com gear-contact-point-on-ground-1 = 1 (roda esquerda no solo), sem sim-on-ground
            var frameOnGround = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["gear-contact-point-on-ground-0"] = 0.0,
                ["gear-contact-point-on-ground-1"] = 1.0,
                ["gear-contact-point-on-ground-2"] = 0.0
            }, DateTime.UtcNow, 1.0);

            // Invoca método privado TryGetFlightPhase via reflexão
            var method = typeof(PartEventDeriver).GetMethod("TryGetFlightPhase", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var args1 = new object?[] { frameOnGround, false };
            bool success1 = (bool)method.Invoke(null, args1)!;
            bool onGround1 = (bool)args1[1]!;

            Assert.True(success1);
            Assert.True(onGround1);

            // Cenário 2: frame com todos os pontos de contato em 0 -> no ar
            var frameInAir = new SensorFrame(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["gear-contact-point-on-ground-0"] = 0.0,
                ["gear-contact-point-on-ground-1"] = 0.0,
                ["gear-contact-point-on-ground-2"] = 0.0
            }, DateTime.UtcNow, 1.0);

            var args2 = new object?[] { frameInAir, false };
            bool success2 = (bool)method.Invoke(null, args2)!;
            bool onGround2 = (bool)args2[1]!;

            Assert.True(success2);
            Assert.False(onGround2);
        }

        [Fact]
        public void SensorLayout_PontosDeContato_NaoAlteramContagemDeMotores()
        {
            // Frame monomotor com chaves de pontos de contato usando sufixo de roda '-0', '-1', '-2'
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2400.0,
                ["eng-cht:1"] = 180.0,
                ["gear-contact-point-compression-0"] = 0.3,
                ["gear-contact-point-compression-1"] = 0.4,
                ["gear-contact-point-compression-2"] = 0.4,
                ["gear-contact-point-on-ground-0"] = 1.0,
                ["gear-contact-point-on-ground-1"] = 1.0,
                ["gear-contact-point-on-ground-2"] = 1.0,
                ["gear-contact-point-is-skidding-0"] = 0.0,
                ["gear-contact-point-is-skidding-1"] = 0.0,
                ["gear-contact-point-is-skidding-2"] = 0.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);

            int engineCount = PartWearEngine.DetectEngineCount(frame);
            Assert.Equal(1, engineCount);
        }

        [Fact]
        public void PistonAircraft_AllProfiles_ResolveEgtMax()
        {
            var profiles = Loader.LoadProfiles();
            var pistonProfiles = profiles
                .Where(p => string.Equals(p.Class, "Piston", StringComparison.OrdinalIgnoreCase) ||
                            (p.Features != null && p.Features.Any(f => string.Equals(f, "piston", StringComparison.OrdinalIgnoreCase))))
                .ToList();

            Assert.NotEmpty(pistonProfiles);

            foreach (var profile in pistonProfiles)
            {
                var resolved = Resolver.Get(profile, profile.Class, LimitKeys.EgtMax);
                Assert.NotNull(resolved);
                Assert.True(resolved.Value > 0, $"Perfil {profile.AircraftId} não resolveu egtMax válido ({resolved.Value}).");
            }
        }
    }
}
