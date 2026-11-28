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

        private static AircraftLimitProfile CreateTwinProfile()
        {
            var limits = new List<AircraftLimit>
            {
                new(LimitKeys.RpmRedline, 2550, "rpm", LimitSource.POH, "Twin POH"),
                new(LimitKeys.ChtMax, 240, "celsius", LimitSource.POH, "Twin POH"),
                new(LimitKeys.OilTempMax, 118, "celsius", LimitSource.POH, "Twin POH"),
                new(LimitKeys.OilPressMin, 25, "psi", LimitSource.POH, "Twin POH"),
                new(LimitKeys.Vne, 200, "knots", LimitSource.POH, "Twin POH")
            };

            return new AircraftLimitProfile(
                "BARON58",
                "B",
                2,
                new[] { "Baron 58", "B58" },
                new[] { "B58" },
                limits,
                new[] { "piston", "constantSpeedProp", "retractableGear", "twinEngine", "naturallyAspirated", "unpressurized" },
                2);
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
                EngineIndex = 1,
                Health = 100.0,
                HoursSinceReplacement = 0.0
            };

            // 1 hora em voo de cruzeiro totalmente verde (com chaves reais do motor 1)
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2200.0,
                ["eng-max-reached-rpm:1"] = 2200.0,
                ["eng-cht:1"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature:1"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure:1"] = 55.0,
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

            var cylPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
            var valvesPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", EngineIndex = 1, Health = 100.0 };
            var tiresPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", EngineIndex = 0, Health = 100.0 };

            var parts = new[] { cylPart, valvesPart, tiresPart };

            // CHT a 265 °C (acima do limite de 240 °C -> faixa severa da regra piston_cht_overheat)
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2300.0,
                ["eng-max-reached-rpm:1"] = 2300.0,
                ["eng-cht:1"] = 265.0,
                ["eng-max-cht"] = 265.0,
                ["oil-temperature:1"] = 85.0,
                ["oil-max-temperature"] = 85.0,
                ["oil-pressure:1"] = 50.0,
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
            var tireDef = PartsCatalog.Get("tires.main")!;
            double normalTiresLoss = tireDef.LifeHours > 0 ? (70.0 / tireDef.LifeHours) * tenMinutesHours : 0.0;
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

            var part1x = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
            var part4x = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };

            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2200.0,
                ["eng-max-reached-rpm:1"] = 2200.0,
                ["eng-cht:1"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature:1"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure:1"] = 55.0,
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

            var cylPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };

            // Sensores normais mas com ITT altíssimo (950 °C)
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2200.0,
                ["eng-max-reached-rpm:1"] = 2200.0,
                ["eng-cht:1"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature:1"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure:1"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["turb-itt:1"] = 950.0,
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

            var part1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };

            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2200.0, // Verde no addon
                ["eng-max-reached-rpm:1"] = 2200.0,
                ["eng-cht:1"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature:1"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure:1"] = 55.0,
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

            var part2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
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

        [Fact]
        public void ApplyTick_RpmPorMotor_EngRpm1AcimaDoRedline_DesgastaPecasDoMotor()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var cylPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };

            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2600.0, // Acima de rpmRedline (2550)
                ["eng-max-reached-rpm:1"] = 2600.0,
                ["eng-cht:1"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature:1"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure:1"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["af-airspeed-indicated"] = 90.0,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            double dtHours = 0.1;

            var result = engine.ApplyTick(new[] { cylPart }, frame, dtHours, profile.Features, resolver, profile);

            double normalLoss = (70.0 / PartsCatalog.Get("engine.cylinders")!.LifeHours) * dtHours;
            double actualLoss = 100.0 - cylPart.Health;

            Assert.True(actualLoss > normalLoss, $"Desgaste com sobrevelocidade ({actualLoss}) deveria superar o normal ({normalLoss}).");
            Assert.Contains(result.Exceedances, e => e.RuleId == "piston_rpm_overspeed" && e.TargetId.Contains(":1") && e.Notes.Contains("Motor 1"));
        }

        [Fact]
        public void ApplyTick_ChaveGlobal_ContinuaFuncionando()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var wingPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "airframe.wing_structure", EngineIndex = 0, Health = 100.0 };

            // Vne = 149 kt no C152 POH; sensor global af-airspeed-indicated a 160 kt
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2200.0,
                ["eng-max-reached-rpm:1"] = 2200.0,
                ["af-airspeed-indicated"] = 160.0,
                ["af-airspeed-mach"] = 0.25,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            var result = engine.ApplyTick(new[] { wingPart }, frame, 0.1, profile.Features, resolver, profile);

            Assert.Contains(result.Exceedances, e => e.RuleId == "vne_airspeed_exceedance");
            Assert.True(wingPart.Health < 100.0);
        }

        [Fact]
        public void ApplyTick_Bimotor_SoMotor2EmVermelho_DesgastaSoPecasDoMotor2()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateTwinProfile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var cyl1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
            var cyl2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 2, Health = 100.0 };
            // Peça compartilhada com EngineIndex 0 presente na regra piston_rpm_overspeed
            var sharedPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "propeller.governor", EngineIndex = 0, Health = 100.0 };

            var parts = new[] { cyl1, cyl2, sharedPart };

            // Motor 1 a 2200 RPM (verde), Motor 2 a 2700 RPM (vermelho)
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2200.0,
                ["eng-max-reached-rpm:1"] = 2200.0,
                ["eng-rpm:2"] = 2700.0,
                ["eng-max-reached-rpm:2"] = 2700.0,
                ["eng-cht:1"] = 180.0,
                ["eng-cht:2"] = 180.0,
                ["eng-max-cht"] = 180.0,
                ["oil-temperature:1"] = 80.0,
                ["oil-temperature:2"] = 80.0,
                ["oil-max-temperature"] = 80.0,
                ["oil-pressure:1"] = 55.0,
                ["oil-pressure:2"] = 55.0,
                ["oil-warning-pressure"] = 55.0,
                ["af-airspeed-indicated"] = 120.0,
                ["af-g-force"] = 1.0,
                ["af-max-g-force"] = 1.0,
                ["af-min-g-force"] = 1.0,
                ["af-flaps-handle-index"] = 0.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            double dtHours = 0.1;

            var result = engine.ApplyTick(parts, frame, dtHours, profile.Features, resolver, profile);

            double normalCylLoss = (70.0 / PartsCatalog.Get("engine.cylinders")!.LifeHours) * dtHours;
            double actualCyl1Loss = 100.0 - cyl1.Health;
            double actualCyl2Loss = 100.0 - cyl2.Health;

            // Motor 1 perdeu apenas o normal
            Assert.Equal(normalCylLoss, actualCyl1Loss, 5);

            // Motor 2 perdeu muito mais que o normal (faixa severa/warning)
            Assert.True(actualCyl2Loss > normalCylLoss * 2.0);

            // Peça compartilhada (EngineIndex 0) recebeu estresse do pior motor (motor 2) uma vez só (sem somar)
            double normalSharedLoss = (70.0 / PartsCatalog.Get("propeller.governor")!.LifeHours) * dtHours;
            double actualSharedLoss = 100.0 - sharedPart.Health;
            Assert.True(actualSharedLoss > normalSharedLoss);
        }

        [Fact]
        public void ApplyTick_Bimotor_ExcedenciaRastreadaPorMotor()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateTwinProfile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var cyl1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
            var cyl2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 2, Health = 100.0 };

            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng-rpm:1"] = 2600.0,
                ["eng-max-reached-rpm:1"] = 2600.0,
                ["eng-rpm:2"] = 2700.0,
                ["eng-max-reached-rpm:2"] = 2700.0,
                ["af-airspeed-indicated"] = 120.0
            };

            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);
            var result = engine.ApplyTick(new[] { cyl1, cyl2 }, frame, 0.1, profile.Features, resolver, profile);

            var ex1 = result.Exceedances.FirstOrDefault(e => e.RuleId == "piston_rpm_overspeed" && e.TargetId == "engine.cylinders:1");
            var ex2 = result.Exceedances.FirstOrDefault(e => e.RuleId == "piston_rpm_overspeed" && e.TargetId == "engine.cylinders:2");

            Assert.NotNull(ex1);
            Assert.NotNull(ex2);
            Assert.Equal(2600.0, ex1.PeakValue);
            Assert.Equal(2700.0, ex2.PeakValue);
            Assert.Contains("(Motor 1)", ex1.Notes);
            Assert.Contains("(Motor 2)", ex2.Notes);
        }

        [Fact]
        public void ApplyEvent_BimotorPecasRepetidas_NaoLancaExcecao_EAplicaEmTodas()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var profile = CreateTwinProfile();

            var gear1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.main_struts", EngineIndex = 1, Health = 100.0 };
            var gear2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.main_struts", EngineIndex = 2, Health = 100.0 };

            var parts = new[] { gear1, gear2 };

            // Toque duro de 700 fpm com duas peças do mesmo PartId
            var res = engine.ApplyEvent(parts, WearRuleMetrics.TouchdownFpm, 700.0, profile.Features);

            Assert.True(gear1.Health < 100.0);
            Assert.True(gear2.Health < 100.0);
            Assert.Equal(gear1.Health, gear2.Health);
            Assert.NotEmpty(res.Inspections);
        }

        [Fact]
        public void Tracker_DiagnosticoDeSensorAusente_SaiUmaVezPorVoo()
        {
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var cyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
            tracker.Begin(new[] { cyl }, profile, profile.Features);

            // Frame sem sensores de motor (ausentes)
            var sensorValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["af-airspeed-indicated"] = 90.0
            };
            var frame = new SensorFrame(sensorValues, DateTime.UtcNow, 1.0);

            // Tick 1: deve conter diagnósticos de sensores ausentes citando a tentativa com :1
            var tick1 = tracker.ProcessTick(frame, 1.0 / 3600.0, DateTime.UtcNow);
            Assert.NotEmpty(tick1.DiagnosticMessages);
            Assert.Contains(tick1.DiagnosticMessages, m => m.Contains("'eng-rpm' (nem 'eng-rpm:1')"));

            // Ticks 2 a 10: os mesmos diagnósticos NÃO devem se repetir
            for (int i = 2; i <= 10; i++)
            {
                var tickN = tracker.ProcessTick(frame, 1.0 / 3600.0, DateTime.UtcNow);
                Assert.Empty(tickN.DiagnosticMessages);
            }

            // Após Reset e novo Begin, diagnósticos devem sair mais uma vez
            tracker.Reset();
            tracker.Begin(new[] { cyl }, profile, profile.Features);

            var tickNovoVoo = tracker.ProcessTick(frame, 1.0 / 3600.0, DateTime.UtcNow);
            Assert.NotEmpty(tickNovoVoo.DiagnosticMessages);
            Assert.Contains(tickNovoVoo.DiagnosticMessages, m => m.Contains("'eng-rpm' (nem 'eng-rpm:1')"));
        }

        [Fact]
        public void WearRules_TodoSensorExisteNoLayoutReal_PorPerfil()
        {
            var registry = SensorRegistry.LoadDefault();
            var loader = new Infrastructure.Services.AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            Assert.NotEmpty(profiles);

            foreach (var profile in profiles)
            {
                int engineCount = Math.Max(1, profile.EngineCount);
                var layout = SensorLayout.Build(registry.Sensors, engineCount, includeCalibration: false);
                var layoutKeys = layout.Blocks.SelectMany(b => b.Slots).Select(s => s.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var featureSet = new HashSet<string>(profile.Features, StringComparer.OrdinalIgnoreCase);

                foreach (var rule in WearRules.Rules)
                {
                    if (!rule.AppliesTo.Matches(featureSet)) continue;

                    foreach (var sensorKey in rule.Sensors)
                    {
                        bool isDerived = PartFlightTracker.DerivedSensorKeys.Contains(sensorKey);
                        bool hasExact = layoutKeys.Contains(sensorKey);
                        bool hasEngine = Enumerable.Range(1, engineCount).Any(e => layoutKeys.Contains($"{sensorKey}:{e}"));

                        Assert.True(hasExact || hasEngine || isDerived,
                            $"Aeronave '{profile.AircraftId}' (motores={engineCount}): sensor '{sensorKey}' da regra '{rule.Id}' não existe no layout (nem exato nem por motor).");
                    }
                }
            }
        }

        [Fact]
        public void RequiresEngineRunning_DentroDoAtraso_NaoAvalia()
        {
            var profile = CreateC152Profile();
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var crank = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.crankshaft", EngineIndex = 1, Health = 100.0 };
            var parts = new[] { crank };

            // 1. Motor cortado (eng-combustion = 0) com pressão de óleo 0 psi
            var fParado = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-combustion:1"] = 0.0,
                ["oil-pressure:1"] = 0.0,
                ["oil-warning-pressure:1"] = 20.0,
                ["ctx-simulation-rate"] = 1.0
            }, DateTime.UtcNow, 1.0);

            var res1 = engine.ApplyTick(parts, fParado, 10.0 / 3600.0, profile.Features, null, profile);
            Assert.DoesNotContain(res1.Exceedances, e => e.RuleId == "oil_pressure_low");

            // 2. Motor ligou (eng-combustion = 1), mas decorreram apenas 15 s (< atraso de 30 s)
            var fPartida = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-combustion:1"] = 1.0,
                ["oil-pressure:1"] = 0.0,
                ["oil-warning-pressure:1"] = 20.0,
                ["ctx-simulation-rate"] = 1.0
            }, DateTime.UtcNow, 1.0);

            var res2 = engine.ApplyTick(parts, fPartida, 15.0 / 3600.0, profile.Features, null, profile);
            Assert.DoesNotContain(res2.Exceedances, e => e.RuleId == "oil_pressure_low");

            // 3. Após mais 20 s (total 35 s simulados > 30 s de atraso), continua a 0 psi -> agora AVALIA e gera excedência
            var res3 = engine.ApplyTick(parts, fPartida, 20.0 / 3600.0, profile.Features, null, profile);
            Assert.Contains(res3.Exceedances, e => e.RuleId == "oil_pressure_low");
        }

        [Fact]
        public void ChtCoolingRate_JanelaSimulada_IndependeDoSimRate()
        {
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            // Teste 1: SimRate 1x (60 passos de 1 s real = 60 s simulados, CHT caindo de 200 para 160 °C -> taxa esperada 40 °C/min)
            var tracker1X = new PartFlightTracker(PartsCatalog, WearRules, resolver);
            var cyl1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
            tracker1X.Begin(new[] { cyl1 }, profile, profile.Features);

            var baseTime = DateTime.UtcNow;
            for (int i = 0; i <= 60; i++)
            {
                double currentCht = 200.0 - (40.0 / 60.0) * i;
                var values = new Dictionary<string, double>
                {
                    ["eng-combustion:1"] = 1.0,
                    ["eng-cht:1"] = currentCht,
                    ["ctx-simulation-rate"] = 1.0
                };
                var f = new SensorFrame(values, baseTime.AddSeconds(i), 1.0);
                tracker1X.ProcessTick(f, 1.0 / 3600.0, f.ReceivedUtc);
            }

            // Teste 2: SimRate 4x (15 passos de 1 s real = 60 s simulados, mesma queda de CHT de 200 para 160 °C no tempo simulado)
            var tracker4X = new PartFlightTracker(PartsCatalog, WearRules, resolver);
            var cyl4 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };
            tracker4X.Begin(new[] { cyl4 }, profile, profile.Features);

            for (int i = 0; i <= 15; i++)
            {
                double simSeconds = i * 4.0;
                double currentCht = 200.0 - (40.0 / 60.0) * simSeconds;
                var values = new Dictionary<string, double>
                {
                    ["eng-combustion:1"] = 1.0,
                    ["eng-cht:1"] = currentCht,
                    ["ctx-simulation-rate"] = 4.0
                };
                var f = new SensorFrame(values, baseTime.AddSeconds(i), 4.0);
                tracker4X.ProcessTick(f, 4.0 / 3600.0, f.ReceivedUtc);
            }

            // Ao final de 60s simulados, ambos atingiram a taxa de 40 °C/min e sofreram a mesma perda percentual por choque térmico
            Assert.Equal(cyl1.Health, cyl4.Health, precision: 3);
            Assert.True(cyl1.Health < 100.0);
        }

        private static AircraftLimitProfile CreateC172Profile()
        {
            var limits = new List<AircraftLimit>
            {
                new(LimitKeys.ElecSystemVolts, 28, "volts", LimitSource.POH, "C172 POH"),
                new(LimitKeys.ElecAvionicsCircuitIndex, 3, "number", LimitSource.POH, "C172 POH"),
                new(LimitKeys.ElecAvionicsBusAmpsIndex, 4, "number", LimitSource.POH, "C172 POH"),
                new(LimitKeys.ElecMainBusIndex, 1, "number", LimitSource.POH, "C172 POH"),
                new(LimitKeys.AlternatorRatedAmps, 60, "amperes", LimitSource.POH, "C172 POH"),
                new(LimitKeys.ElecDeepDischargeVolts, 22, "volts", LimitSource.POH, "C172 POH")
            };

            return new AircraftLimitProfile(
                "C172SP G1000",
                "A",
                1,
                new[] { "piston", "singleEngine" },
                new[] { "Cessna 172SP" },
                limits
            );
        }

        [Fact]
        public void Tracker_ObservePreflight_NaoAplicaDesgasteNatural_NemHoras()
        {
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var cyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0, HoursSinceReplacement = 0.0 };
            tracker.Begin(new[] { cyl }, profile, profile.Features);

            var frame = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-combustion:1"] = 1.0,
                ["eng-rpm:1"] = 1000.0,
                ["ctx-simulation-rate"] = 1.0
            }, DateTime.UtcNow, 1.0);

            // 1 hora simulada em preflight com motor funcionando em marcha lenta
            var res = tracker.ObservePreflight(frame, 1.0, DateTime.UtcNow);

            Assert.Equal(100.0, cyl.Health);
            Assert.Equal(0.0, cyl.HoursSinceReplacement);
            Assert.Empty(res.Exceedances);
            Assert.False(tracker.HasPreflightChanges);
        }

        [Fact]
        public void Tracker_PartidaNaPreparacao_NaoContaDeNovoNoVoo()
        {
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var starter = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.starter", EngineIndex = 1, Health = 100.0 };
            tracker.Begin(new[] { starter }, profile, profile.Features);

            var now = DateTime.UtcNow;

            // 1. Preflight: starter ligado por 2 segundos
            var f1 = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["eng-combustion:1"] = 0.0,
                ["ctx-simulation-rate"] = 1.0
            }, now, 1.0);
            tracker.ObservePreflight(f1, 2.0 / 3600.0, now);

            // 2. Preflight: motor pega (combustion = 1, starter = 0)
            var f2 = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 0.0,
                ["eng-combustion:1"] = 1.0,
                ["ctx-simulation-rate"] = 1.0
            }, now.AddSeconds(2), 1.0);
            var preflightRes = tracker.ObservePreflight(f2, 1.0 / 3600.0, now.AddSeconds(2));

            var startEvents = tracker.LastDerivedEvents.Where(e => e.MetricKey == "engineStartCycle").ToList();
            Assert.Single(startEvents);

            // 3. Voo ativo inicia: chama ProcessTick com motor já funcionando
            var f3 = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 0.0,
                ["eng-combustion:1"] = 1.0,
                ["ctx-simulation-rate"] = 1.0
            }, now.AddSeconds(15), 1.0);
            var tickRes = tracker.ProcessTick(f3, 1.0 / 3600.0, now.AddSeconds(15));

            var flightStartEvents = tracker.LastDerivedEvents.Where(e => e.MetricKey == "engineStartCycle").ToList();
            Assert.Empty(flightStartEvents);
        }

        [Fact]
        public void Tracker_PartidaComAvionicsLigadoNaPreparacao_EmiteSpike()
        {
            var profile = CreateC172Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var display = new AircraftPart { Id = Guid.NewGuid(), PartId = "avionics.glass_displays", EngineIndex = 0, Health = 100.0 };
            tracker.Begin(new[] { display }, profile, profile.Features);

            var now = DateTime.UtcNow;

            // Starter ligado com avionics master ligado (elec-circuit-on-3 = 1) -> borda 0->1 do starter emite spike
            var f1 = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["elec-circuit-on-3"] = 1.0,
                ["eng-combustion:1"] = 0.0,
                ["ctx-simulation-rate"] = 1.0
            }, now, 1.0);
            var res1 = tracker.ObservePreflight(f1, 2.0 / 3600.0, now);

            var spikes = tracker.LastDerivedEvents.Where(e => e.MetricKey == WearRuleMetrics.AvionicsMasterStartSpikes).ToList();
            Assert.Single(spikes);
            Assert.Equal(1.0, spikes[0].Value);
            Assert.True(tracker.HasPreflightChanges);
            Assert.True(display.Health < 100.0);
        }

        [Fact]
        public void ApplyTick_FaixaSemEfeito_NaoRegistraExcedencia()
        {
            // Regra contínua sintética com uma faixa sem efeito (multiplier: 1.0, lossPerHourPct: 0.0, sem inspeção)
            var ruleNoEffect = new WearRule(
                "test_rule_no_effect",
                "Regra de teste sem efeito na faixa",
                WearRuleKind.Continuous,
                new PartAppliesTo(new[] { "piston" }, Array.Empty<string>(), Array.Empty<string>()),
                new[] { "test-sensor" },
                "testLimit",
                LimitDirection.Above,
                null,
                new[]
                {
                    new WearRuleBand(
                        "caution_zero_effect",
                        1.0,
                        new[] { new WearRulePart("engine.cylinders", 1.0, 0.0, 0.0) },
                        null
                    )
                },
                null,
                null,
                "teste",
                false,
                0.0
            );

            var wearCatalog = new WearRuleCatalog(new[] { ruleNoEffect });
            var engine = new PartWearEngine(PartsCatalog, wearCatalog);

            var limit = new AircraftLimit("testLimit", 100.0, "units", LimitSource.POH, "test");
            var profile = new AircraftLimitProfile("TEST", "A", 1, new[] { "piston" }, new[] { "TEST" }, new[] { limit });
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var cyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", EngineIndex = 1, Health = 100.0 };

            var frame = new SensorFrame(new Dictionary<string, double>
            {
                ["test-sensor"] = 120.0, // Acima do limite de 100.0
                ["ctx-simulation-rate"] = 1.0
            }, DateTime.UtcNow, 1.0);

            var result = engine.ApplyTick(new[] { cyl }, frame, 1.0 / 3600.0, profile.Features, resolver, profile);

            // Faixa sem efeito não deve ir para a lista de excedências do blackbox
            Assert.Empty(result.Exceedances);
        }
    }
}
