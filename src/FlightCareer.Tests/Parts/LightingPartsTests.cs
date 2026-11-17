using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class LightingPartsTests
    {
        private static readonly PartCatalog DefaultPartsCatalog = PartCatalog.LoadDefault();
        private static readonly WearRuleCatalog DefaultWearRules = WearRuleCatalog.LoadDefault();
        private static readonly WearRuleCatalog EmptyWearRules = new(Array.Empty<WearRule>());
        private static readonly SensorRegistry DefaultSensors = SensorRegistry.LoadDefault("sensor_registry.json");

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly Queue<double> _values;

            public FixedRandomSource(params double[] values)
            {
                _values = new Queue<double>(values);
            }

            public double NextDouble()
            {
                return _values.Count > 0 ? _values.Dequeue() : 0.5;
            }

            public int Next(int max) => (int)(NextDouble() * max);
            public int Next(int min, int max) => min + (int)(NextDouble() * (max - min));
        }

        private static SensorFrame CreateFrame(Dictionary<string, double>? values = null)
        {
            return new SensorFrame(
                values ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
                DateTime.UtcNow,
                1.0);
        }

        private static AircraftLimitProfile CreateSimpleProfile()
        {
            return new AircraftLimitProfile(
                "C152",
                "A",
                1,
                new[] { "C152" },
                new[] { "C152" },
                new List<AircraftLimit>(),
                new[] { "piston" },
                1);
        }

        [Fact]
        public void Lampada_HorasAcesas_DesgastaPorTempoSimulado()
        {
            var engine = new PartWearEngine(DefaultPartsCatalog, DefaultWearRules);
            var part = new AircraftPart { PartId = "lights.landing", Health = 100.0, HoursSinceReplacement = 0.0 };

            // Com light-landing-on = 1.0 (aceso) e 1h simulada:
            // Perda esperada: 70.0 / 300.0 = 0.23333333333333334 %
            var frameAceso = CreateFrame(new Dictionary<string, double>
            {
                ["light-landing-on"] = 1.0
            });

            engine.ApplyTick(new[] { part }, frameAceso, 1.0, Array.Empty<string>(), null, null);

            double expectedLoss = 70.0 / 300.0;
            Assert.Equal(100.0 - expectedLoss, part.Health, precision: 5);
            Assert.Equal(1.0, part.HoursSinceReplacement, precision: 5);

            // Com light-landing-on = 0.0 (apagado) e 1h simulada:
            // Perda esperada: 0 %
            double healthBefore = part.Health;
            double hoursBefore = part.HoursSinceReplacement;

            var frameApagado = CreateFrame(new Dictionary<string, double>
            {
                ["light-landing-on"] = 0.0
            });

            engine.ApplyTick(new[] { part }, frameApagado, 1.0, Array.Empty<string>(), null, null);

            Assert.Equal(healthBefore, part.Health);
            Assert.Equal(hoursBefore, part.HoursSinceReplacement);
        }

        [Fact]
        public void Lampada_SensorAusente_NaoDesgastaNemLanca()
        {
            var engine = new PartWearEngine(DefaultPartsCatalog, DefaultWearRules);
            var part = new AircraftPart { PartId = "lights.landing", Health = 100.0, HoursSinceReplacement = 0.0 };

            var emptyFrame = CreateFrame();

            var exception = Record.Exception(() =>
                engine.ApplyTick(new[] { part }, emptyFrame, 1.0, Array.Empty<string>(), null, null));

            Assert.Null(exception);
            Assert.Equal(100.0, part.Health);
            Assert.Equal(0.0, part.HoursSinceReplacement);
        }

        [Fact]
        public void Lampada_CicloLiga_AplicaDanoPorTransicao()
        {
            // Criamos uma peça com dano de ciclo = 0.05 e lifeHours muito grande para isolar o ciclo
            var customDef = new PartDefinition(
                "lights.landing",
                "Farol de pouso",
                PartGroup.Iluminacao,
                new PartAppliesTo(),
                lifeHours: 1_000_000.0,
                lifeCycles: null,
                visibleOnWalkaround: true,
                blocksFlightWhenBroken: false,
                partPrice: 60,
                laborHours: 0.5,
                sensors: new[] { "light-landing-on" },
                lifeSource: "teste",
                damagePerOnCyclePct: 0.05,
                usageSensor: "light-landing-on");

            var customCatalog = new PartCatalog(new[] { customDef });
            var engine = new PartWearEngine(customCatalog, DefaultWearRules);
            var part = new AircraftPart { PartId = "lights.landing", Health = 100.0 };

            double dtSmall = 1e-6; // dt quase nulo para desgaste natural ser desprezível (< 1e-10)

            // 1ª leitura com 1: primeira leitura do voo não conta ciclo
            var frameOn = CreateFrame(new Dictionary<string, double> { ["light-landing-on"] = 1.0 });
            var frameOff = CreateFrame(new Dictionary<string, double> { ["light-landing-on"] = 0.0 });

            engine.ApplyTick(new[] { part }, frameOn, dtSmall, Array.Empty<string>(), null, null);
            Assert.Equal(100.0, part.Health, precision: 4);

            // Leitura com 0: apagou
            engine.ApplyTick(new[] { part }, frameOff, dtSmall, Array.Empty<string>(), null, null);

            // Leitura com 1: 1ª transição 0 -> 1 (+1 ciclo)
            engine.ApplyTick(new[] { part }, frameOn, dtSmall, Array.Empty<string>(), null, null);
            Assert.Equal(99.95, part.Health, precision: 4);

            // Leitura com 0: apagou
            engine.ApplyTick(new[] { part }, frameOff, dtSmall, Array.Empty<string>(), null, null);

            // Leitura com 1: 2ª transição 0 -> 1 (+1 ciclo)
            engine.ApplyTick(new[] { part }, frameOn, dtSmall, Array.Empty<string>(), null, null);
            Assert.Equal(99.90, part.Health, precision: 4);
        }

        [Fact]
        public void Lampada_Falha_PorTaxaPorHora()
        {
            // 1. Saúde 50% (> 20%): λ = 0, nunca queima
            var rngZero = new FixedRandomSource(0.0000001); // sorteio baixíssimo
            var engineSafe = new PartWearEngine(DefaultPartsCatalog, EmptyWearRules, rngZero);
            var partSafe = new AircraftPart { PartId = "lights.landing", Health = 50.0 };

            var frameOn = CreateFrame(new Dictionary<string, double> { ["light-landing-on"] = 1.0 });
            var resSafe = engineSafe.ApplyTick(new[] { partSafe }, frameOn, 0.1, Array.Empty<string>(), null, null);

            Assert.False(partSafe.IsBroken);
            Assert.Empty(resSafe.DiagnosticMessages);

            // 2. Saúde 10% (< 20%): λ > 0. Sorteio abaixo de p -> queima
            double lambda = UsageFailureModel.LambdaPerHour(10.0);
            double dt = 0.5;
            double p = 1.0 - Math.Exp(-lambda * dt);

            // Sorteio abaixo de p
            var rngFail = new FixedRandomSource(p * 0.5);
            var engineFail = new PartWearEngine(DefaultPartsCatalog, EmptyWearRules, rngFail);
            var partFail = new AircraftPart { PartId = "lights.landing", Health = 10.0 };

            var resFail = engineFail.ApplyTick(new[] { partFail }, frameOn, dt, Array.Empty<string>(), null, null);

            Assert.True(partFail.IsBroken);
            Assert.Equal(0.0, partFail.Health);
            Assert.Single(resFail.DiagnosticMessages);
            Assert.Equal("Lâmpada queimada: Farol de pouso", resFail.DiagnosticMessages[0]);

            // No próximo tick não deve gerar diagnóstico adicional
            var resNext = engineFail.ApplyTick(new[] { partFail }, frameOn, dt, Array.Empty<string>(), null, null);
            Assert.Empty(resNext.DiagnosticMessages);

            // 3. Saúde 10% mas lâmpada apagada: não sorteia nem queima
            var rngApagada = new FixedRandomSource(0.000001);
            var engineApagada = new PartWearEngine(DefaultPartsCatalog, EmptyWearRules, rngApagada);
            var partApagada = new AircraftPart { PartId = "lights.landing", Health = 10.0 };

            var frameOff = CreateFrame(new Dictionary<string, double> { ["light-landing-on"] = 0.0 });
            var resApagada = engineApagada.ApplyTick(new[] { partApagada }, frameOff, dt, Array.Empty<string>(), null, null);

            Assert.False(partApagada.IsBroken);
            Assert.Equal(10.0, partApagada.Health);
            Assert.Empty(resApagada.DiagnosticMessages);
        }

        [Fact]
        public void Lampada_SaudeZeroAcesa_Queima()
        {
            var engine = new PartWearEngine(DefaultPartsCatalog, EmptyWearRules);
            var part = new AircraftPart { PartId = "lights.landing", Health = 0.0, IsBroken = false };

            var frameOn = CreateFrame(new Dictionary<string, double> { ["light-landing-on"] = 1.0 });
            var result = engine.ApplyTick(new[] { part }, frameOn, 0.01, Array.Empty<string>(), null, null);

            Assert.True(part.IsBroken);
            Assert.Equal(0.0, part.Health);
            Assert.Contains(result.DiagnosticMessages, d => d.Contains("Farol de pouso"));
        }

        [Fact]
        public void Lampada_Queimada_NaoDesgastaMais()
        {
            var engine = new PartWearEngine(DefaultPartsCatalog, EmptyWearRules);
            var part = new AircraftPart { PartId = "lights.landing", Health = 0.0, IsBroken = true, HoursSinceReplacement = 15.0 };

            var frameOn = CreateFrame(new Dictionary<string, double> { ["light-landing-on"] = 1.0 });
            var result = engine.ApplyTick(new[] { part }, frameOn, 5.0, Array.Empty<string>(), null, null);

            Assert.True(part.IsBroken);
            Assert.Equal(0.0, part.Health);
            Assert.Equal(15.0, part.HoursSinceReplacement);
            Assert.Empty(result.DiagnosticMessages);
        }

        [Fact]
        public void MinorPart_ForaDoMinimo_DaAgregacao()
        {
            // Bateria (saúde 90, não é minorPart) e Lâmpada (saúde 0, minorPart = true)
            var parts = new List<AircraftPart>
            {
                new() { PartId = "electrical.battery", Health = 90.0 },
                new() { PartId = "lights.landing", Health = 0.0 }
            };

            var settings = new PartAggregationSettings { MinWeight = 0.7, AverageWeight = 0.3 };

            // min apenas das não-minor = 90.0
            // avg de todas = (90 + 0) / 2 = 45.0
            // ponderado = 0.7 * 90 + 0.3 * 45 = 63 + 13.5 = 76.5
            double aggregated = PartComponentBridge.AggregateHealth(parts, DefaultPartsCatalog, settings);

            Assert.Equal(76.5, aggregated);
        }

        [Fact]
        public void DamageGroup_Eletrico_NaoAtingeLampadas()
        {
            var profile = CreateSimpleProfile();
            var tracker = new PartFlightTracker(
                DefaultPartsCatalog,
                DefaultWearRules,
                new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>()));

            var parts = new List<AircraftPart>
            {
                new() { PartId = "electrical.battery", Health = 100.0 },
                new() { PartId = "lights.landing", Health = 100.0 }
            };

            tracker.Begin(parts, profile, new[] { "piston" });

            tracker.DamageGroup(ComponentKind.Electrical, 20.0);

            var battery = tracker.Parts.First(p => p.PartId == "electrical.battery");
            var light = tracker.Parts.First(p => p.PartId == "lights.landing");

            Assert.Equal(80.0, battery.Health);
            Assert.Equal(100.0, light.Health); // Lâmpada poupada de dano do componente
        }

        [Fact]
        public void ClampGroup_Eletrico_AtingeLampadas()
        {
            var profile = CreateSimpleProfile();
            var tracker = new PartFlightTracker(
                DefaultPartsCatalog,
                DefaultWearRules,
                new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>()));

            var parts = new List<AircraftPart>
            {
                new() { PartId = "electrical.battery", Health = 100.0 },
                new() { PartId = "lights.landing", Health = 100.0 }
            };

            tracker.Begin(parts, profile, new[] { "piston" });

            tracker.ClampGroup(ComponentKind.Electrical, 40.0);

            var battery = tracker.Parts.First(p => p.PartId == "electrical.battery");
            var light = tracker.Parts.First(p => p.PartId == "lights.landing");

            Assert.Equal(40.0, battery.Health);
            Assert.Equal(40.0, light.Health); // Clamp (perda total / dano fora de voo) atinge lâmpadas
        }

        [Fact]
        public void Mel_NavQueimada_NoGoNoturno()
        {
            var parts = new[] { new AircraftPart { PartId = "lights.nav", IsBroken = true } };
            var mel = LightingMel.Evaluate(parts, DefaultPartsCatalog);

            var item = Assert.Single(mel);
            Assert.Equal(LightingMelCategory.NoGoNight, item.Category);
            Assert.Contains("voo noturno não permitido", item.Message);
        }

        [Fact]
        public void Mel_BeaconQueimadoComStrobeOk_DayOnly()
        {
            var parts = new[]
            {
                new AircraftPart { PartId = "lights.beacon", IsBroken = true },
                new AircraftPart { PartId = "lights.strobe", IsBroken = false }
            };

            var mel = LightingMel.Evaluate(parts, DefaultPartsCatalog);

            var item = Assert.Single(mel);
            Assert.Equal("lights.beacon", item.PartId);
            Assert.Equal(LightingMelCategory.DayOnly, item.Category);
        }

        [Fact]
        public void Mel_BeaconQueimadoSemStrobe_NoGoNoturno()
        {
            // Sem strobe instalada ou com strobe quebrada
            var partsSemStrobe = new[] { new AircraftPart { PartId = "lights.beacon", IsBroken = true } };
            var melSemStrobe = LightingMel.Evaluate(partsSemStrobe, DefaultPartsCatalog);

            Assert.Equal(LightingMelCategory.NoGoNight, Assert.Single(melSemStrobe).Category);

            var partsStrobeQuebrada = new[]
            {
                new AircraftPart { PartId = "lights.beacon", IsBroken = true },
                new AircraftPart { PartId = "lights.strobe", IsBroken = true }
            };
            var melStrobeQuebrada = LightingMel.Evaluate(partsStrobeQuebrada, DefaultPartsCatalog);

            var beaconItem = melStrobeQuebrada.First(i => i.PartId == "lights.beacon");
            Assert.Equal(LightingMelCategory.NoGoNight, beaconItem.Category);
        }

        [Fact]
        public void Mel_PousoQueimado_DayOnly()
        {
            var parts = new[] { new AircraftPart { PartId = "lights.landing", IsBroken = true } };
            var mel = LightingMel.Evaluate(parts, DefaultPartsCatalog);

            var item = Assert.Single(mel);
            Assert.Equal(LightingMelCategory.DayOnly, item.Category);
        }

        [Fact]
        public void Mel_TudoOk_Vazio()
        {
            var parts = new[]
            {
                new AircraftPart { PartId = "lights.landing", IsBroken = false },
                new AircraftPart { PartId = "lights.nav", IsBroken = false },
                new AircraftPart { PartId = "lights.beacon", IsBroken = false }
            };

            var mel = LightingMel.Evaluate(parts, DefaultPartsCatalog);
            Assert.Empty(mel);
        }

        [Fact]
        public void Catalogo_Luzes_NaoProvisionadasEmNoElectrical()
        {
            var cubParts = DefaultPartsCatalog.PartsFor(new[] { "noElectrical", "piston" });
            Assert.DoesNotContain(cubParts, p => p.Id.StartsWith("lights.", StringComparison.OrdinalIgnoreCase));

            var c152Parts = DefaultPartsCatalog.PartsFor(new[] { "piston" });
            Assert.Contains(c152Parts, p => p.Id == "lights.landing");
            Assert.Contains(c152Parts, p => p.Id == "lights.taxi");
            Assert.Contains(c152Parts, p => p.Id == "lights.nav");
            Assert.Contains(c152Parts, p => p.Id == "lights.strobe");
            Assert.Contains(c152Parts, p => p.Id == "lights.beacon");
            Assert.Contains(c152Parts, p => p.Id == "lights.panel");
        }

        [Fact]
        public void Catalogo_UsageSensorInexistente_Lanca()
        {
            const string jsonInexistente = """
            {
              "parts": [
                {
                  "id": "lights.fake",
                  "name": "Luz fake",
                  "group": "Iluminação",
                  "lifeHours": 100,
                  "lifeSource": "teste",
                  "failsByUsageRate": true,
                  "usageSensor": "sensor-inexistente-no-registro"
                }
              ]
            }
            """;

            var catalog = PartCatalog.LoadFromJson(jsonInexistente);
            var validSensors = new HashSet<string>(StringComparer.Ordinal) { "light-landing-on" };

            var exInexistente = Assert.Throws<InvalidDataException>(() => catalog.Validate(validSensors));
            Assert.Contains("não existe no registro de sensores", exInexistente.Message);

            const string jsonSemSensor = """
            {
              "parts": [
                {
                  "id": "lights.fake2",
                  "name": "Luz fake 2",
                  "group": "Iluminação",
                  "lifeHours": 100,
                  "lifeSource": "teste",
                  "failsByUsageRate": true
                }
              ]
            }
            """;

            var catalogSemSensor = PartCatalog.LoadFromJson(jsonSemSensor);
            var exSemSensor = Assert.Throws<InvalidDataException>(() => catalogSemSensor.Validate());
            Assert.Contains("requer 'usageSensor'", exSemSensor.Message);
        }

        [Fact]
        public void Catalogo_LuzesUsamSensoresDoRegistro()
        {
            var validSensors = DefaultSensors.Sensors.Select(s => s.Key).ToHashSet(StringComparer.Ordinal);

            foreach (var part in DefaultPartsCatalog.Parts)
            {
                if (!string.IsNullOrWhiteSpace(part.UsageSensor))
                {
                    Assert.Contains(part.UsageSensor, validSensors);
                }
            }
        }
    }
}
