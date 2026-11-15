using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartWearEngineTests
    {
        private static readonly PartCatalog PartsCatalog = PartCatalog.LoadDefault();
        private static readonly WearRuleCatalog WearRules = WearRuleCatalog.LoadDefault();

        private static AircraftLimitProfile CreateC152Profile()
        {
            var limits = new List<AircraftLimit>
            {
                new(LimitKeys.RpmRedline, 2550, "rpm", LimitSource.POH, "C152 POH"),
                new(LimitKeys.ChtMax, 240, "celsius", LimitSource.POH, "C152 POH"),
                new(LimitKeys.OilTempMax, 118, "celsius", LimitSource.POH, "C152 POH"),
                new(LimitKeys.OilPressMin, 25, "psi", LimitSource.POH, "C152 POH"),
                new(LimitKeys.Vne, 149, "knots", LimitSource.POH, "C152 POH"),
                new("vfe:1", 85, "knots", LimitSource.POH, "C152 POH")
            };

            return new AircraftLimitProfile(
                "C152",
                "A",
                1,
                new[] { "Cessna 152", "C152" },
                new[] { "C152" },
                limits,
                new[] { "piston", "fixedPitchProp", "fixedGear", "tricycleGear", "carbureted", "naturallyAspirated", "unpressurized" },
                1);
        }

        [Fact]
        public void NormalWear_OneHourGreen_LosesExactNominalHealth()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var cylindersDef = PartsCatalog.Get("engine.cylinders")!;
            Assert.True(cylindersDef.LifeHours > 0);

            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 100.0,
                HoursSinceReplacement = 0.0
            };

            // 1 hora em voo de cruzeiro totalmente verde
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm"] = 2200.0,
                ["eng-max-reached-rpm"] = 2200.0,
                ["eng-cht"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["af-airspeed-indicated"] = 95.0,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            double dtHours = 1.0;

            var result = engine.ApplyTick(new[] { part }, frame, dtHours, profile.Features, resolver, profile);

            double expectedLoss = 70.0 / cylindersDef.LifeHours;
            double expectedHealth = 100.0 - expectedLoss;

            Assert.Equal(expectedHealth, part.Health, 4);
            Assert.Equal(1.0, part.HoursSinceReplacement, 4);
            Assert.Empty(result.Exceedances);
            Assert.Empty(result.Inspections);
        }

        [Fact]
        public void Overstress_ChtRedlineTenMinutes_WearsCylindersAtLeast3x_AndDoesNotTouchTires()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var cylPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0 };
            var valvesPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 100.0 };
            var tiresPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 100.0 };

            var parts = new[] { cylPart, valvesPart, tiresPart };

            // CHT a 265 °C (acima do limite de 240 °C -> faixa severa da regra piston_cht_overheat)
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm"] = 2300.0,
                ["eng-max-reached-rpm"] = 2300.0,
                ["eng-cht"] = 265.0,
                ["eng-max-cht"] = 265.0,
                ["oil-temperature"] = 85.0,
                ["oil-max-temperature"] = 85.0,
                ["oil-pressure"] = 50.0,
                ["oil-warning-pressure"] = 50.0,
                ["af-airspeed-indicated"] = 90.0,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            double tenMinutesHours = 10.0 / 60.0;

            var result = engine.ApplyTick(parts, frame, tenMinutesHours, profile.Features, resolver, profile);

            // Verifica que cilindros sofreram pelo menos 3x a taxa normal daquele tempo
            double normalCylLoss = (70.0 / PartsCatalog.Get("engine.cylinders")!.LifeHours) * tenMinutesHours;
            double actualCylLoss = 100.0 - cylPart.Health;

            Assert.True(actualCylLoss >= normalCylLoss * 3.0, $"Desgaste {actualCylLoss:F4} deveria ser >= {normalCylLoss * 3.0:F4}");

            // Pneus não sofrem com calor do CHT do motor
            double normalTiresLoss = (70.0 / PartsCatalog.Get("tires.main")!.LifeHours) * tenMinutesHours;
            double actualTiresLoss = 100.0 - tiresPart.Health;
            Assert.Equal(normalTiresLoss, actualTiresLoss, 4);

            Assert.NotEmpty(result.Exceedances);
        }

        [Fact]
        public void SimulationRate_4x_QuadruplesWearRatePerRealSecond()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var part1x = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0 };
            var part4x = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0 };

            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm"] = 2200.0,
                ["eng-max-reached-rpm"] = 2200.0,
                ["eng-cht"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["af-airspeed-indicated"] = 90.0,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0
            };

            // 1 segundo real com sim rate 1.0
            var frame1x = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            engine.ApplyTick(new[] { part1x }, frame1x, 1.0 / 3600.0, profile.Features, resolver, profile);

            // 1 segundo real com sim rate 4.0 (equivale a 4 segundos simulados)
            var frame4x = new SensorFrame(sensorValues, DateTime.UtcNow, 4.0);
            engine.ApplyTick(new[] { part4x }, frame4x, 4.0 / 3600.0, profile.Features, resolver, profile);

            double loss1x = 100.0 - part1x.Health;
            double loss4x = 100.0 - part4x.Health;

            Assert.Equal(loss1x * 4.0, loss4x, 5);
        }

        [Fact]
        public void IncompatibleRule_IttOnPiston_DoesNotAct()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile(); // C152 é piston
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var cylPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0 };

            // Sensores normais mas com ITT altíssimo (950 °C)
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm"] = 2200.0,
                ["eng-max-reached-rpm"] = 2200.0,
                ["eng-cht"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["turb-itt"] = 950.0,
                ["turb-max-itt"] = 950.0,
                ["af-airspeed-indicated"] = 90.0,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            var result = engine.ApplyTick(new[] { cylPart }, frame, 1.0, profile.Features, resolver, profile);

            // ITT não deve disparar nenhuma regra de turbina no C152
            Assert.DoesNotContain(result.Exceedances, e => e.RuleId.Contains("itt", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DynamicLimit_VfeCurrent_UsesFlapToothAndIgnoresWhenRetracted()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile(); // Vfe:1 = 85 kt
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var flapPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "wing.flaps_actuators", Health = 100.0 };

            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm"] = 2200.0,
                ["eng-max-reached-rpm"] = 2200.0,
                ["eng-cht"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["af-airspeed-indicated"] = 95.0, // Acima de Vfe:1 (85 kt), mas abaixo de Vne (149 kt)
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0 // Recolhido!
            };

            var frameRetracted = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            var resRetracted = engine.ApplyTick(new[] { flapPart }, frameRetracted, 0.1, profile.Features, resolver, profile);

            // Com flap recolhido, não há sobrevelocidade de flaps
            Assert.DoesNotContain(resRetracted.Exceedances, e => e.RuleId == "flap_overspeed_vfe");

            // Agora estende o flap (dente 1) a 95 kt
            sensorValues["af-flaps-handle-index"] = 1.0;
            var frameExtended = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            var resExtended = engine.ApplyTick(new[] { flapPart }, frameExtended, 0.1, profile.Features, resolver, profile);

            // Agora sim dispara a regra flap_overspeed_vfe
            Assert.Contains(resExtended.Exceedances, e => e.RuleId == "flap_overspeed_vfe");
        }

        [Fact]
        public void NativeReserve_OnlyActsWhenLimitSourceIsClassDefault()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);

            // Perfil onde RPM é POH (não deve aceitar reserva nativa se o sensor do addon estiver verde)
            var profileWithPoh = CreateC152Profile();
            var resolverPoh = new AircraftLimitResolver(new[] { profileWithPoh }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var part1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0 };

            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm"] = 2200.0, // Verde no addon
                ["eng-max-reached-rpm"] = 2200.0,
                ["eng-cht"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["af-airspeed-indicated"] = 90.0,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0,
                ["nw-damage-rate-39"] = 0.005, // Taxa nativa acusa overstress
                ["nw-level-39"] = 0.85
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);

            // 1. Com limite POH: taxa nativa é ignorada, addon permanece verde
            var resPoh = engine.ApplyTick(new[] { part1 }, frame, 0.1, profileWithPoh.Features, resolverPoh, profileWithPoh);
            Assert.DoesNotContain(resPoh.Exceedances, e => e.RuleId == "piston_rpm_overspeed");

            // 2. Agora perfil sem limite POH de RPM (apenas ClassDefault)
            var limitsClassDefault = new List<AircraftLimit>
            {
                new(LimitKeys.RpmRedline, 2550, "rpm", LimitSource.ClassDefault, "Padrão da classe"),
                new(LimitKeys.ChtMax, 240, "celsius", LimitSource.ClassDefault, "Padrão da classe"),
                new(LimitKeys.OilTempMax, 118, "celsius", LimitSource.ClassDefault, "Padrão da classe"),
                new(LimitKeys.OilPressMin, 25, "psi", LimitSource.ClassDefault, "Padrão da classe"),
                new(LimitKeys.Vne, 149, "knots", LimitSource.ClassDefault, "Padrão da classe")
            };
            var profileClassDefault = new AircraftLimitProfile("C152CD", "A", 1, new[] { "C152CD" }, new[] { "C152CD" }, limitsClassDefault, profileWithPoh.Features, 1);
            var resolverClassDefault = new AircraftLimitResolver(new[] { profileClassDefault }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var part2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0 };
            var resClassDefault = engine.ApplyTick(new[] { part2 }, frame, 0.1, profileClassDefault.Features, resolverClassDefault, profileClassDefault);

            // Com ClassDefault e taxa nativa alta, a reserva nativa atua e dispara a regra!
            Assert.Contains(resClassDefault.Exceedances, e => e.RuleId == "piston_rpm_overspeed");
        }

        [Fact]
        public void DiscreteEvent_HardLanding700Fpm_AppliesDamageAndGeneratesInspection()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile();

            var gearPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.main_struts", Health = 100.0 };
            var tiresPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 100.0 };

            var parts = new[] { gearPart, tiresPart };

            var eventResult = engine.ApplyEvent(parts, WearRuleMetrics.TouchdownFpm, 700.0, profile.Features);

            Assert.True(gearPart.Health < 100.0, "Trem de pouso deveria sofrer dano no toque duro.");
            Assert.True(tiresPart.Health < 100.0, "Pneus deveriam sofrer dano no toque duro.");
            Assert.NotEmpty(eventResult.Inspections);
            Assert.Contains(eventResult.Inspections, i => i.DefectCode.Contains("LANDING") || i.DefectCode.Contains("HARD"));
            // F2.4: touchdownFpm aplica dano por impacto, mas NÃO incrementa CyclesSinceReplacement (deve incrementar apenas em landingCycles)
            Assert.Equal(0, gearPart.CyclesSinceReplacement);
            Assert.Equal(0, tiresPart.CyclesSinceReplacement);
        }

        [Fact]
        public void ApplyNativeFailed_ZeroHealth_BreaksPart_AndIsIdempotent()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 85.0,
                IsBroken = false
            };

            // Primeiro evento de falha nativa
            bool brokeFirst = engine.ApplyNativeFailed(part);
            Assert.True(brokeFirst);
            Assert.Equal(0.0, part.Health);
            Assert.True(part.IsBroken);
            Assert.NotNull(part.BrokenAtUtc);

            var brokenAt = part.BrokenAtUtc;

            // Segundo evento no mesmo voo: idempotente, não altera nada
            bool brokeSecond = engine.ApplyNativeFailed(part);
            Assert.False(brokeSecond);
            Assert.Equal(0.0, part.Health);
            Assert.True(part.IsBroken);
            Assert.Equal(brokenAt, part.BrokenAtUtc);
        }
    }
}
