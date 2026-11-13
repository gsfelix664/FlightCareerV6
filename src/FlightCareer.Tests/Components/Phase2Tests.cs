using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using Xunit;
using static FlightCareer.Tests.Components.TestData;

namespace FlightCareer.Tests.Components
{
    public class CatalogPhase2Tests
    {
        [Fact]
        public void Instrumentos_existem_e_vacuo_so_em_pistao()
        {
            var def = Catalog.Get(ComponentKind.Instruments);
            Assert.Contains(def.ModesFor(EngineClass.Piston), m => m.Effect == FailureEffect.VacuumFailure);
            Assert.DoesNotContain(def.ModesFor(EngineClass.Jet), m => m.Effect == FailureEffect.VacuumFailure);
        }

        [Fact]
        public void Panes_com_efeito_real_no_simulador_estao_mapeadas()
        {
            var effects = Catalog.Components.SelectMany(c => c.FailureModes).Select(m => m.Effect).ToHashSet();
            Assert.Contains(FailureEffect.EngineFailure, effects);
            Assert.Contains(FailureEffect.EngineFire, effects);
            Assert.Contains(FailureEffect.OilLeak, effects);
            Assert.Contains(FailureEffect.MagnetoLeft, effects);
            Assert.Contains(FailureEffect.AlternatorOff, effects);
            Assert.Contains(FailureEffect.ElectricalFailure, effects);
            Assert.Contains(FailureEffect.VacuumFailure, effects);
            Assert.Contains(FailureEffect.PitotBlockage, effects);
            Assert.Contains(FailureEffect.StaticBlockage, effects);
            Assert.Contains(FailureEffect.FlapJam, effects);
            Assert.Contains(FailureEffect.BrakeFailureOneSide, effects);
            Assert.Contains(FailureEffect.HydraulicFailure, effects);
        }

        [Fact]
        public void Overrides_por_modelo_ajustam_limites_e_classe()
        {
            var (c152, c152Airframe) = Catalog.ResolveLimits("Cessna C152", EngineClass.Piston);
            Assert.Equal(2550, c152.RedlineRpm);
            Assert.Equal(4.4, c152Airframe.MaxG);
            Assert.Equal(260, c152.MaxCht); // não sobrescrito: mantém o padrão da classe

            var (_, a320) = Catalog.ResolveLimits("A320neo", EngineClass.Jet);
            Assert.Equal(2.5, a320.MaxG);

            var (_, c172) = Catalog.ResolveLimits("C172SP G1000", EngineClass.Piston);
            Assert.Equal(3.8, c172.MaxG);

            Assert.Equal(EngineClass.Piston, Catalog.ResolveEngineClass(new AircraftType { Model = "Diamond DA40NG", Type = "Geral", FuelType = "Jet A-1" }));
            // O padrão do catálogo não é alterado pelos overrides
            Assert.Equal(2700, Catalog.LimitsFor(EngineClass.Piston).RedlineRpm);
        }

        [Fact]
        public void Redline_do_modelo_e_usado_no_desgaste()
        {
            var (limits, airframe) = Catalog.ResolveLimits("Cessna C152", EngineClass.Piston);
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston, limits, airframe);
            var data = PistonCruise();
            data.EngineRPM = 2650; // acima de 2550 × 1,02: vermelho no C152, normal no C172

            var events = wear.ApplyTick(comps, data, 1.0 / 3600.0);

            Assert.Contains(events, e => e.Parameter == "RPM");
        }
    }

    public class ExternalEventTests
    {
        private static SimTelemetry LowAndFast() => new()
        {
            OnGround = 0, EngineRunning = 1, Altitude = 1500, Airspeed = 100, GroundSpeed = 100, ThrottlePercent = 70
        };

        [Fact]
        public void Bird_strike_nao_acontece_alto_ou_no_solo()
        {
            var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, new FixedRandom(0.0));
            var comps = NewComponents(EngineClass.Piston);
            foreach (var c in comps) { c.HoursSinceOverhaul = 500; c.CyclesSinceOverhaul = 200; } // sem janela pós-manutenção

            var high = LowAndFast(); high.Altitude = 9000;
            var ground = LowAndFast(); ground.OnGround = 1; ground.ThrottlePercent = 10;

            Assert.Empty(engine.Evaluate(comps, high, 1.0));
            Assert.Empty(engine.Evaluate(comps, ground, 1.0));
        }

        [Fact]
        public void Cada_tipo_de_evento_acontece_no_maximo_uma_vez_por_voo()
        {
            var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, new FixedRandom(0.0));
            var comps = NewComponents(EngineClass.Piston);
            foreach (var c in comps) { c.HoursSinceOverhaul = 500; c.CyclesSinceOverhaul = 200; } // sem janela pós-manutenção

            var first = engine.Evaluate(comps, LowAndFast(), 1.0);
            var second = engine.Evaluate(comps, LowAndFast(), 1.0);

            Assert.Single(first, e => e.Source == ExternalEventEngine.BirdStrike);
            Assert.Empty(second);
            engine.Reset();
            Assert.Single(engine.Evaluate(comps, LowAndFast(), 1.0));
        }

        [Fact]
        public void Monte_Carlo_bird_strike_proximo_de_1_a_cada_1000_horas()
        {
            var random = new SystemRandomSource(2024);
            var comps = NewComponents(EngineClass.Piston);
            foreach (var c in comps) { c.HoursSinceOverhaul = 500; c.CyclesSinceOverhaul = 200; }
            int strikes = 0;
            const int flights = 100000; // 100.000 h de voo baixo a 100 kt

            for (int i = 0; i < flights; i++)
            {
                var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, random);
                if (engine.Evaluate(comps, LowAndFast(), 1.0).Count > 0) strikes++;
            }

            Assert.InRange(strikes, 80, 120); // esperado ≈ 100
        }

        [Fact]
        public void Falha_pos_manutencao_so_em_componente_recem_revisado()
        {
            var cruise = PistonCruise();
            cruise.Altitude = 8000; // sem bird strike
            var comps = NewComponents(EngineClass.Piston);
            foreach (var c in comps) { c.HoursSinceOverhaul = 500; c.CyclesSinceOverhaul = 200; }

            var none = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, new FixedRandom(0.0));
            Assert.Empty(none.Evaluate(comps, cruise, 1.0));

            comps.Get(ComponentKind.Oil).HoursSinceOverhaul = 0.5;
            var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, new FixedRandom(0.0));
            var ev = Assert.Single(engine.Evaluate(comps, cruise, 1.0));
            Assert.Equal(ComponentKind.Oil, ev.Outcome.Kind);
            Assert.Equal(FailureEffect.OilLeak, ev.Outcome.Effect);
        }

        [Fact]
        public void Freio_usado_nao_conta_como_recem_revisado_mesmo_com_zero_horas()
        {
            var taxi = new SimTelemetry { OnGround = 1, EngineRunning = 1, GroundSpeed = 15, ThrottlePercent = 20 };
            var comps = NewComponents(EngineClass.Piston);
            foreach (var c in comps) { c.HoursSinceOverhaul = 500; c.CyclesSinceOverhaul = 200; }
            comps.Get(ComponentKind.Brakes).HoursSinceOverhaul = 0; // por ciclos: horas nunca acumulam

            var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, new FixedRandom(0.0));
            Assert.Empty(engine.Evaluate(comps, taxi, 1.0));

            comps.Get(ComponentKind.Brakes).CyclesSinceOverhaul = 1; // recém-trocados
            var fresh = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, new FixedRandom(0.0));
            Assert.Equal(ComponentKind.Brakes, Assert.Single(fresh.Evaluate(comps, taxi, 1.0)).Outcome.Kind);
        }

        [Fact]
        public void FOD_so_na_corrida_com_potencia_e_mais_provavel_em_pista_molhada()
        {
            var roll = new SimTelemetry { OnGround = 1, EngineRunning = 1, GroundSpeed = 50, ThrottlePercent = 100 };
            var wet = roll; wet.SurfaceCondition = 1;
            var taxi = roll; taxi.ThrottlePercent = 20;
            var comps = NewComponents(EngineClass.Piston);
            foreach (var c in comps) { c.HoursSinceOverhaul = 500; c.CyclesSinceOverhaul = 200; }

            Assert.Empty(new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, new FixedRandom(0.0)).Evaluate(comps, taxi, 1.0));

            int dry = 0, wetCount = 0;
            var random = new SystemRandomSource(5);
            for (int i = 0; i < 20000; i++)
            {
                if (new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, random).Evaluate(comps, roll, 1.0).Count > 0) dry++;
                if (new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, random).Evaluate(comps, wet, 1.0).Count > 0) wetCount++;
            }
            Assert.True(wetCount > dry * 2, $"seco={dry}, molhado={wetCount}");
        }
    }

    public class CarbIcingTests
    {
        private static SimTelemetry Descent(bool carbHeat) => new()
        {
            OnGround = 0, EngineRunning = 1, CarbHeatAvailable = 1, CarbTemperature = -2,
            ThrottlePercent = 20, AmbientTemperature = 12, EngAntiIce = carbHeat ? 1 : 0
        };

        [Fact]
        public void Gelo_avisa_em_30s_apaga_em_90s_e_recupera_com_carb_heat()
        {
            var monitor = new CarbIcingMonitor(Catalog.CarbIcing);
            var events = new List<CarbIcingEvent>();
            for (int i = 0; i < 90; i++) events.Add(monitor.Update(Descent(false), 1.0));

            Assert.Equal(CarbIcingEvent.IceForming, events[29]);
            Assert.Equal(CarbIcingEvent.EngineStopped, events[89]);
            Assert.True(monitor.EngineStoppedByIce);

            var recovery = new List<CarbIcingEvent>();
            for (int i = 0; i < 30; i++) recovery.Add(monitor.Update(Descent(true), 1.0));

            Assert.Equal(CarbIcingEvent.Recovered, recovery[^1]);
            Assert.False(monitor.EngineStoppedByIce);
        }

        [Fact]
        public void Carb_heat_ligado_impede_o_gelo()
        {
            var monitor = new CarbIcingMonitor(Catalog.CarbIcing);
            for (int i = 0; i < 300; i++)
                Assert.Equal(CarbIcingEvent.None, monitor.Update(Descent(true), 1.0));
        }

        [Fact]
        public void Aeronave_sem_carburador_nao_tem_gelo()
        {
            var monitor = new CarbIcingMonitor(Catalog.CarbIcing);
            var injected = Descent(false); injected.CarbHeatAvailable = 0;
            for (int i = 0; i < 300; i++)
                Assert.Equal(CarbIcingEvent.None, monitor.Update(injected, 1.0));
        }
    }

    public class LegacyBridgeTests
    {
        [Fact]
        public void Coluna_antiga_recebe_o_pior_componente_do_grupo()
        {
            var aircraft = new PilotAircraft();
            var comps = NewComponents(EngineClass.Piston);
            comps.Get(ComponentKind.Brakes).Health = 40;
            comps.Get(ComponentKind.Tires).Health = 70;
            comps.Get(ComponentKind.Instruments).Health = 55;

            LegacyConditionBridge.WriteToLegacy(aircraft, comps);

            Assert.Equal(40, aircraft.TiresCondition);
            Assert.Equal(55, aircraft.AvionicsCondition);
            Assert.Equal(100, aircraft.EngineCondition);
        }

        [Fact]
        public void Reparo_no_hangar_sobe_o_grupo_e_zera_horas_em_100()
        {
            var aircraft = new PilotAircraft { CurrentAirframeHours = 300, HoursUntilNext100hInspection = 40, HoursUntilNext50hInspection = 20 };
            var comps = NewComponents(EngineClass.Piston);
            var tires = comps.Get(ComponentKind.Tires);
            var brakes = comps.Get(ComponentKind.Brakes);
            tires.Health = 70; tires.CyclesSinceOverhaul = 100;
            brakes.Health = 40; brakes.CyclesSinceOverhaul = 300;
            LegacyConditionBridge.WriteToLegacy(aircraft, comps);

            aircraft.TiresCondition = 100; // Hangar: reparo de pneus
            int changed = LegacyConditionBridge.ReconcileFromLegacy(aircraft, comps, new FixedRandom(0.5), Catalog.Visibility);

            Assert.Equal(2, changed);
            Assert.Equal(100, tires.Health);
            Assert.Equal(100, brakes.Health);
            Assert.Equal(0, brakes.CyclesSinceOverhaul);
            Assert.True(HealthVisibility.GetDisplayedRange(brakes, 300, Catalog.Visibility).IsExact);
        }

        [Fact]
        public void Perda_total_gravada_nas_colunas_zera_os_componentes()
        {
            var aircraft = new PilotAircraft { HoursUntilNext100hInspection = 40, HoursUntilNext50hInspection = 20 };
            var comps = NewComponents(EngineClass.Piston, health: 80);
            LegacyConditionBridge.WriteToLegacy(aircraft, comps);

            aircraft.EngineCondition = 0;
            aircraft.AirframeCondition = 0;
            LegacyConditionBridge.ReconcileFromLegacy(aircraft, comps, new FixedRandom(0.5), Catalog.Visibility);

            Assert.Equal(0, comps.Get(ComponentKind.Engine).Health);
            Assert.Equal(0, comps.Get(ComponentKind.LandingGear).Health);
            Assert.Equal(80, comps.Get(ComponentKind.Tires).Health);
        }

        [Fact]
        public void Sem_mudanca_externa_nada_e_alterado()
        {
            var aircraft = new PilotAircraft { HoursUntilNext100hInspection = 40, HoursUntilNext50hInspection = 20 };
            var comps = NewComponents(EngineClass.Piston, health: 63.2);
            LegacyConditionBridge.WriteToLegacy(aircraft, comps);

            Assert.Equal(0, LegacyConditionBridge.ReconcileFromLegacy(aircraft, comps, new FixedRandom(0.5), Catalog.Visibility));
        }
    }
}
