using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Sensors;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartFlightIntegrationTests
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

        private static AircraftLimitResolver CreateResolver(AircraftLimitProfile? profile = null)
        {
            profile ??= CreateC152Profile();
            return new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());
        }

        [Fact]
        public void PartWearEngine_TouchdownMaisLandingCycle_ContaUmCiclo()
        {
            var engine = new PartWearEngine(PartsCatalog, WearRules);
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "gear.wheels_bearings",
                Health = 100.0,
                CyclesSinceReplacement = 0
            };
            var parts = new[] { part };

            // Aplica primeiro touchdownFpm (300 fpm)
            engine.ApplyEvent(parts, WearRuleMetrics.TouchdownFpm, 300.0, Array.Empty<string>());
            Assert.Equal(0, part.CyclesSinceReplacement); // bug antigo incrementava aqui

            // Aplica depois landingCycles (1)
            engine.ApplyEvent(parts, WearRuleMetrics.LandingCycles, 1.0, Array.Empty<string>());
            Assert.Equal(1, part.CyclesSinceReplacement); // agora incrementa apenas aqui (total = 1)
        }

        [Fact]
        public void PartEventDeriver_PartidaPistao_EmiteUmEngineStartCycle()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { "piston" };

            // Frame 1: starter ligado, sem combustão
            var f1 = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["eng-combustion:1"] = 0.0
            }, DateTime.UtcNow, 1.0);
            var ev1 = deriver.Update(f1, 1.0, features);
            Assert.DoesNotContain(ev1, e => e.MetricKey == WearRuleMetrics.EngineStartCycle);

            // Frame 2: starter ligado, combustão iniciou (0 -> 1)
            var f2 = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 1.0,
                ["eng-combustion:1"] = 1.0
            }, DateTime.UtcNow, 1.0);
            var ev2 = deriver.Update(f2, 1.0, features);
            var startEv = Assert.Single(ev2, e => e.MetricKey == WearRuleMetrics.EngineStartCycle);
            Assert.Equal(1, startEv.EngineIndex);

            // Frame 3: starter solto, motor continua rodando (combustão 1 -> 1)
            var f3 = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 0.0,
                ["eng-combustion:1"] = 1.0
            }, DateTime.UtcNow, 1.0);
            var ev3 = deriver.Update(f3, 1.0, features);
            Assert.DoesNotContain(ev3, e => e.MetricKey == WearRuleMetrics.EngineStartCycle);
        }

        [Fact]
        public void PartEventDeriver_Starter8sSimulados_EmiteStarterSeconds8()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { "piston" };

            // 4 frames de 2 segundos simulados cada (total = 8s simulados)
            for (int i = 0; i < 4; i++)
            {
                var f = new SensorFrame(new Dictionary<string, double>
                {
                    ["eng-starter:1"] = 1.0,
                    ["eng-combustion:1"] = 0.0
                }, DateTime.UtcNow, 2.0);
                var evs = deriver.Update(f, 2.0, features);
                Assert.DoesNotContain(evs, e => e.MetricKey == WearRuleMetrics.StarterSeconds);
            }

            // Solta o starter
            var fRelease = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-starter:1"] = 0.0,
                ["eng-combustion:1"] = 1.0
            }, DateTime.UtcNow, 1.0);
            var releaseEvents = deriver.Update(fRelease, 1.0, features);

            var starterEv = Assert.Single(releaseEvents, e => e.MetricKey == WearRuleMetrics.StarterSeconds);
            Assert.Equal(8.0, starterEv.Value, 1);
            Assert.Equal(1, starterEv.EngineIndex);
        }

        [Fact]
        public void PartEventDeriver_TremRecolheEBaixa_UmGearCycle()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.RetractableGear };

            // Frame 1: trem estendido (100%)
            var f1 = new SensorFrame(new Dictionary<string, double> { ["gear-position"] = 100.0 }, DateTime.UtcNow, 1.0);
            Assert.DoesNotContain(deriver.Update(f1, 1.0, features), e => e.MetricKey == WearRuleMetrics.GearCycles);

            // Frame 2: trem recolhido (0%)
            var f2 = new SensorFrame(new Dictionary<string, double> { ["gear-position"] = 0.0 }, DateTime.UtcNow, 1.0);
            Assert.DoesNotContain(deriver.Update(f2, 1.0, features), e => e.MetricKey == WearRuleMetrics.GearCycles);

            // Frame 3: trem baixado de novo (100%)
            var f3 = new SensorFrame(new Dictionary<string, double> { ["gear-position"] = 100.0 }, DateTime.UtcNow, 1.0);
            var evs = deriver.Update(f3, 1.0, features);
            Assert.Single(evs, e => e.MetricKey == WearRuleMetrics.GearCycles);
        }

        [Fact]
        public void PartEventDeriver_TremFixo_NenhumGearCycle()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { AircraftFeatures.FixedGear }; // Trem fixo

            var f1 = new SensorFrame(new Dictionary<string, double> { ["gear-position"] = 0.0 }, DateTime.UtcNow, 1.0);
            deriver.Update(f1, 1.0, features);

            var f2 = new SensorFrame(new Dictionary<string, double> { ["gear-position"] = 100.0 }, DateTime.UtcNow, 1.0);
            var evs = deriver.Update(f2, 1.0, features);
            Assert.DoesNotContain(evs, e => e.MetricKey == WearRuleMetrics.GearCycles);
        }

        [Fact]
        public void PartEventDeriver_FlightCycle_SoUmPorVoo()
        {
            var deriver = new PartEventDeriver();
            var features = new[] { "piston" };

            // 1. Decola: no ar (sim-on-ground = 0)
            var fAir = new SensorFrame(new Dictionary<string, double> { ["ctx-sim-on-ground"] = 0.0 }, DateTime.UtcNow, 1.0);
            deriver.Update(fAir, 1.0, features);

            // 2. Primeiro toque de pouso (sim-on-ground = 1) -> 1 ciclo
            var fLand1 = new SensorFrame(new Dictionary<string, double> { ["ctx-sim-on-ground"] = 1.0 }, DateTime.UtcNow, 1.0);
            var evs1 = deriver.Update(fLand1, 1.0, features);
            Assert.Single(evs1, e => e.MetricKey == WearRuleMetrics.FlightCycles);

            // 3. Bounce: sai brevemente do chão
            deriver.Update(fAir, 0.5, features);

            // 4. Segundo toque do bounce -> nenhum ciclo adicional (já emitiu no primeiro pouso)
            var evs2 = deriver.Update(fLand1, 0.5, features);
            Assert.DoesNotContain(evs2, e => e.MetricKey == WearRuleMetrics.FlightCycles);
        }

        [Fact]
        public void Reconcile_BarraReparadaNoHangar_SobePecasDoGrupo()
        {
            var p1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 40.0, IsBroken = true, BrokenAtUtc = DateTime.UtcNow };
            var p2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 60.0 };
            var parts = new[] { p1, p2 };

            var comp = new AircraftComponent { Kind = ComponentKind.Engine, Health = 100.0 }; // Reparado a 100 no Hangar
            var comps = new[] { comp };

            int mod = PartComponentBridge.ReconcilePartsFromComponents(comps, parts, PartsCatalog);
            Assert.True(mod > 0);

            Assert.Equal(100.0, p1.Health);
            Assert.False(p1.IsBroken);
            Assert.Null(p1.BrokenAtUtc);
            Assert.Equal(100.0, p2.Health);
        }

        [Fact]
        public void Reconcile_BarraZeradaForaDoVoo_ZeraPecas()
        {
            var p1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 80.0 };
            var p2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 90.0 };
            var parts = new[] { p1, p2 };

            var comp = new AircraftComponent { Kind = ComponentKind.Engine, Health = 0.0 }; // Perda total fora de voo
            var comps = new[] { comp };

            int mod = PartComponentBridge.ReconcilePartsFromComponents(comps, parts, PartsCatalog);
            Assert.True(mod > 0);
            Assert.Equal(0.0, p1.Health);
            Assert.Equal(0.0, p2.Health);
        }

        [Fact]
        public void Reconcile_BarraIgualAoAgregado_NaoMuda()
        {
            var p1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 70.0 };
            var p2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 70.0 };
            var parts = new[] { p1, p2 };

            var comp = new AircraftComponent { Kind = ComponentKind.Engine, Health = 70.0 };
            var comps = new[] { comp };

            int mod = PartComponentBridge.ReconcilePartsFromComponents(comps, parts, PartsCatalog);
            Assert.Equal(0, mod);
            Assert.Equal(70.0, p1.Health);
            Assert.Equal(70.0, p2.Health);
        }

        [Fact]
        public void NativeFailed_IdComRegraNativeOverstress_QuebraPecasDaRegra()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            // C152 motor a pistão: NativeOverstress 39 existe nas regras de RPM e CHT do pistão
            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 90.0 };
            var pTire = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.tires", Health = 95.0 };

            tracker.Begin(new[] { pCyl, pTire }, CreateC152Profile(), new[] { "piston" });

            int broken = tracker.ApplyNativeFailed(39, ComponentKind.Engine);
            Assert.Equal(1, broken);
            Assert.True(pCyl.IsBroken);
            Assert.Equal(0.0, pCyl.Health);
            Assert.False(pTire.IsBroken);
            Assert.Equal(95.0, pTire.Health);
        }

        [Fact]
        public void NativeFailed_IdSemRegra_QuebraPecaDeMenorSaude()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var pOil1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "oil.cooler", Health = 80.0 };
            var pOil2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "oil.filter", Health = 45.0 }; // menor saúde

            tracker.Begin(new[] { pOil1, pOil2 }, CreateC152Profile(), new[] { "piston" });

            // ID 999 não tem regra de nativeOverstress: cai no fallback de menor saúde do ComponentKind.Oil
            int broken = tracker.ApplyNativeFailed(999, ComponentKind.Oil);
            Assert.Equal(1, broken);
            Assert.True(pOil2.IsBroken);
            Assert.Equal(0.0, pOil2.Health);
            Assert.False(pOil1.IsBroken);
            Assert.Equal(80.0, pOil1.Health);
        }

        [Fact]
        public void NativeFailed_SegundaVez_Ignorado()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 90.0 };
            tracker.Begin(new[] { pCyl }, CreateC152Profile(), new[] { "piston" });

            int b1 = tracker.ApplyNativeFailed(39, ComponentKind.Engine);
            Assert.Equal(1, b1);
            Assert.True(pCyl.IsBroken);

            // Segunda tentativa: já quebrada, idempotente
            int b2 = tracker.ApplyNativeFailed(39, ComponentKind.Engine);
            Assert.Equal(0, b2);
        }

        [Fact]
        public void DamageGroup_SoAfetaPecasDoGrupo_IgnoraQuebradas()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 90.0 };
            var pBrokenValves = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 0.0, IsBroken = true };
            var pTires = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.tires", Health = 100.0 };

            tracker.Begin(new[] { pCyl, pBrokenValves, pTires }, CreateC152Profile(), new[] { "piston" });

            tracker.DamageGroup(ComponentKind.Engine, 15.0);

            Assert.Equal(75.0, pCyl.Health);
            Assert.Equal(0.0, pBrokenValves.Health); // quebrada permanece inalterada
            Assert.Equal(100.0, pTires.Health); // pneus pertencem a Tires, intocados
        }

        [Fact]
        public void ClampGroup_NaoSobeSaude()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var p1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 80.0 };
            var p2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 40.0 };

            tracker.Begin(new[] { p1, p2 }, CreateC152Profile(), new[] { "piston" });

            // Clamp a 60%
            tracker.ClampGroup(ComponentKind.Engine, 60.0);

            Assert.Equal(60.0, p1.Health); // desceu de 80 para 60
            Assert.Equal(40.0, p2.Health); // 40 não subiu para 60
        }

        [Fact]
        public void Integracao_UmaHoraSimuladaVerde_Perde70SobreVidaUtilEUmHoraAcumulada()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0, HoursSinceReplacement = 0.0 };
            var def = PartsCatalog.Get("engine.cylinders")!;
            Assert.True(def.LifeHours > 0);

            tracker.Begin(new[] { pCyl }, CreateC152Profile(), new[] { "piston" });

            // 1 h verde
            var frame = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-rpm"] = 2200.0,
                ["eng-cht"] = 180.0,
                ["oil-temperature"] = 80.0,
                ["oil-pressure"] = 40.0,
                ["af-airspeed-indicated"] = 95.0
            }, DateTime.UtcNow, 1.0);

            tracker.ProcessTick(frame, 1.0, DateTime.UtcNow);

            double expectedWear = 70.0 / def.LifeHours;
            Assert.Equal(100.0 - expectedWear, pCyl.Health, 2);
            Assert.Equal(1.0, pCyl.HoursSinceReplacement, 2);

            // Sincronização com barras derivadas
            var aircraft = new PilotAircraft { Id = Guid.NewGuid() };
            var comp = new AircraftComponent { Kind = ComponentKind.Engine, Health = 100.0 };
            tracker.SyncComponents(aircraft, new[] { comp });

            Assert.Equal(pCyl.Health, comp.Health, 2);
        }

        [Fact]
        public void Integracao_FrameVelho_NenhumaPerda()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0, HoursSinceReplacement = 0.0 };
            tracker.Begin(new[] { pCyl }, CreateC152Profile(), new[] { "piston" });

            // Frame com 10 segundos atrás (>5s)
            var oldFrame = new SensorFrame(new Dictionary<string, double> { ["eng-rpm"] = 2200.0 }, DateTime.UtcNow.AddSeconds(-10), 1.0);

            var res = tracker.ProcessTick(oldFrame, 1.0, DateTime.UtcNow);

            Assert.Equal(100.0, pCyl.Health);
            Assert.Equal(0.0, pCyl.HoursSinceReplacement);
            Assert.Contains(res.DiagnosticMessages, d => d.Contains("pausado"));
        }

        [Fact]
        public void Integracao_ChtVermelha10Min_CilindrosDesgastam3xEPneusNormal()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 100.0 };
            var pTires = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.tires", Health = 100.0 };
            tracker.Begin(new[] { pCyl, pTires }, CreateC152Profile(), new[] { "piston" });

            double dtHours = 10.0 / 60.0; // 10 min

            // CHT a 250°C (limite POH do C152 é 240°C -> faixa vermelha)
            var hotFrame = new SensorFrame(new Dictionary<string, double>
            {
                ["eng-rpm"] = 2200.0,
                ["eng-cht"] = 250.0,
                ["eng-max-cht"] = 250.0,
                ["oil-temperature"] = 80.0,
                ["oil-pressure"] = 40.0
            }, DateTime.UtcNow, 1.0);

            tracker.ProcessTick(hotFrame, dtHours, DateTime.UtcNow);

            double cylDefLife = PartsCatalog.Get("engine.cylinders")!.LifeHours;
            double normalWear = (70.0 / cylDefLife) * dtHours;
            double actualWear = 100.0 - pCyl.Health;

            // Desgaste de cilindros >= 3x o normal
            Assert.True(actualWear >= normalWear * 3.0, $"Desgaste {actualWear} deveria ser >= 3x normal {normalWear * 3}");

            // Pneus não são afetados por CHT (desgaste de pneus == 0 pois não há regra contínua ativa de pneus)
            Assert.Equal(100.0, pTires.Health);
        }

        [Fact]
        public void Integracao_Pouso700Fpm_AplicaDanoEUmCiclo()
        {
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(PartsCatalog, WearRules, resolver);

            var pTires = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 100.0, CyclesSinceReplacement = 0 };
            var pWheel = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.wheels_bearings", Health = 100.0, CyclesSinceReplacement = 0 };
            tracker.Begin(new[] { pTires, pWheel }, CreateC152Profile(), new[] { "piston" });

            // Pouso de 700 fpm (duro)
            var res = tracker.ProcessTouchdown(700.0, null, null, null, isBounce: false);

            Assert.True(pTires.Health < 100.0, "Deveria ter sofrido dano por impacto de 700 fpm");
            Assert.Equal(1, pWheel.CyclesSinceReplacement);
            Assert.True(res.TotalDamageApplied > 0);
        }

        [Fact]
        public async Task SaveAsync_CopiaCamposParaBanco()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using (var initContext = new AppDbContext(options))
            {
                initContext.Database.EnsureCreated();
            }

            var partId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();

            using (var setupContext = new AppDbContext(options))
            {
                var pilot = new Pilot { Id = Guid.NewGuid(), Name = "Test Pilot", CareerPhase = CareerPhase.Phase1_FlightSchool };
                setupContext.Pilots.Add(pilot);

                var type = new AircraftType
                {
                    Id = Guid.NewGuid(),
                    Model = "C152",
                    Type = "Single Engine Land"
                };
                setupContext.AircraftTypes.Add(type);

                var aircraft = new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilot.Id,
                    AircraftTypeId = type.Id,
                    Prefix = "PT-TEST"
                };
                setupContext.PilotAircrafts.Add(aircraft);

                var dbPart = new AircraftPart
                {
                    Id = partId,
                    PilotAircraftId = aircraftId,
                    PartId = "engine.cylinders",
                    Health = 100.0,
                    HoursSinceReplacement = 10.0,
                    CyclesSinceReplacement = 5,
                    IsBroken = false,
                    BrokenAtUtc = null
                };
                setupContext.AircraftParts.Add(dbPart);
                await setupContext.SaveChangesAsync();
            }

            // Simula voo que alterou a peça em memória desanexada
            var now = DateTime.UtcNow;
            var memPart = new AircraftPart
            {
                Id = partId,
                PilotAircraftId = aircraftId,
                PartId = "engine.cylinders",
                Health = 73.5,
                HoursSinceReplacement = 11.5,
                CyclesSinceReplacement = 6,
                IsBroken = true,
                BrokenAtUtc = now
            };

            using (var saveContext = new AppDbContext(options))
            {
                await PartStateWriter.CopyToDbAsync(saveContext, new[] { memPart });
                await saveContext.SaveChangesAsync();
            }

            // Verifica no banco
            using (var verifyContext = new AppDbContext(options))
            {
                var loaded = await verifyContext.AircraftParts.FindAsync(partId);
                Assert.NotNull(loaded);
                Assert.Equal(73.5, loaded.Health);
                Assert.Equal(11.5, loaded.HoursSinceReplacement);
                Assert.Equal(6, loaded.CyclesSinceReplacement);
                Assert.True(loaded.IsBroken);
                Assert.NotNull(loaded.BrokenAtUtc);
            }
        }

        [Fact]
        public void AircraftLimitResolver_FindById()
        {
            var profile = CreateC152Profile();
            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var found = resolver.FindById("C152");
            Assert.NotNull(found);
            Assert.Equal("C152", found.AircraftId);

            var notFound = resolver.FindById("Boeing777");
            Assert.Null(notFound);

            var nullArg = resolver.FindById(null);
            Assert.Null(nullArg);
        }
    }
}
