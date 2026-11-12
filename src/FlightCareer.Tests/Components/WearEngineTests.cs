using FlightCareer.Domain.Components;
using Xunit;
using static FlightCareer.Tests.Components.TestData;

namespace FlightCareer.Tests.Components
{
    public class WearEngineTests
    {
        private static void Fly(WearEngine wear, List<Domain.Entities.AircraftComponent> comps, Domain.Entities.SimTelemetry data, double hours, double dt = 0.05)
        {
            for (double t = 0; t < hours - 1e-9; t += dt)
                wear.ApplyTick(comps, data, dt);
        }

        [Theory]
        [InlineData(ComponentKind.Engine, 1800.0)]
        [InlineData(ComponentKind.Oil, 50.0)]
        [InlineData(ComponentKind.Ignition, 500.0)]
        [InlineData(ComponentKind.Electrical, 1500.0)]
        public void Desgaste_natural_na_faixa_verde_chega_a_30_no_fim_da_vida(ComponentKind kind, double lifeHours)
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);

            Fly(wear, comps, PistonCruise(), lifeHours);

            Assert.InRange(comps.Get(kind).Health, 29.0, 31.0);
            Assert.InRange(comps.Get(kind).HoursSinceOverhaul, lifeHours - 0.1, lifeHours + 0.1);
        }

        [Fact]
        public void Operacao_normal_dentro_da_vida_util_nunca_entra_na_zona_de_pane()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);

            // 49 h com o óleo em dia: o óleo ainda deve estar bem acima de 20% (bug da curva antiga)
            Fly(wear, comps, PistonCruise(), 49.0);

            Assert.True(comps.Get(ComponentKind.Oil).Health > 30.0);
        }

        [Fact]
        public void Manutencao_vencida_acelera_o_desgaste_e_leva_abaixo_de_20()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);

            Fly(wear, comps, PistonCruise(), 50.0);          // vida útil do óleo
            double atLife = comps.Get(ComponentKind.Oil).Health;
            Fly(wear, comps, PistonCruise(), 5.0);           // 5 h vencido

            double lossOverdue = atLife - comps.Get(ComponentKind.Oil).Health;
            double naturalPerHour = 70.0 / 50.0;
            Assert.InRange(lossOverdue, naturalPerHour * 5 * 3 - 0.5, naturalPerHour * 5 * 3 + 0.5);
            Assert.True(comps.Get(ComponentKind.Oil).Health < 20.0);
        }

        [Fact]
        public void Faixa_vermelha_de_CHT_desgasta_muito_mais_e_registra_excedencia_uma_vez()
        {
            var green = NewComponents(EngineClass.Piston);
            var red = NewComponents(EngineClass.Piston);
            var wearGreen = new WearEngine(Catalog, EngineClass.Piston);
            var wearRed = new WearEngine(Catalog, EngineClass.Piston);

            var hot = PistonCruise();
            hot.EngineCHT = 275; // acima do máximo de 260 °C

            var events = new List<ExceedanceEvent>();
            for (int i = 0; i < 60; i++) // 1 minuto em ticks de 1 s
            {
                wearGreen.ApplyTick(green, PistonCruise(), 1.0 / 3600.0);
                events.AddRange(wearRed.ApplyTick(red, hot, 1.0 / 3600.0));
            }

            double greenLoss = 100 - green.Get(ComponentKind.Engine).Health;
            double redLoss = 100 - red.Get(ComponentKind.Engine).Health;
            Assert.True(redLoss > greenLoss * 100, $"verde={greenLoss}, vermelho={redLoss}");
            Assert.InRange(redLoss, 0.9, 1.5); // ~60%/h × (1 + 5 × 5,8%) ≈ 1,3% em 1 min
            Assert.Single(events, e => e.Kind == ComponentKind.Engine && e.Parameter.StartsWith("CHT"));
        }

        [Fact]
        public void Faixa_amarela_desgasta_mais_que_verde_mas_menos_que_vermelha()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);
            var warm = PistonCruise();
            warm.EngineCHT = 250; // entre 240 e 260: amarelo

            Fly(wear, comps, warm, 1.0, 1.0 / 3600.0);

            double loss = 100 - comps.Get(ComponentKind.Engine).Health;
            Assert.InRange(loss, 3.0, 3.1); // 3%/h amarelo + 70/1800 natural
        }

        [Fact]
        public void Shock_cooling_em_descida_desgasta_o_motor()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);
            var data = PistonCruise();

            var events = new List<ExceedanceEvent>();
            for (int i = 0; i < 30; i++) // CHT caindo 1,5 °C/s = 90 °C/min
            {
                data.EngineCHT = 220 - i * 1.5;
                events.AddRange(wear.ApplyTick(comps, data, 1.0 / 3600.0));
            }

            Assert.Contains(events, e => e.Parameter.StartsWith("Shock cooling"));
            Assert.True(comps.Get(ComponentKind.Engine).Health < 99.5);
        }

        [Fact]
        public void Turbina_com_ITT_acima_do_limite_desgasta_secao_quente()
        {
            var comps = NewComponents(EngineClass.Turboprop);
            var wear = new WearEngine(Catalog, EngineClass.Turboprop);
            var data = TurbopropCruise();
            data.TurboITT = 840;

            Fly(wear, comps, data, 1.0 / 60.0, 1.0 / 3600.0); // 1 minuto

            Assert.True(comps.Get(ComponentKind.HotSection).Health < 98.5);
            Assert.DoesNotContain(comps, c => c.Kind == ComponentKind.Ignition);
        }

        [Fact]
        public void Partida_de_motor_conta_ciclo()
        {
            var comps = NewComponents(EngineClass.Turboprop);
            var wear = new WearEngine(Catalog, EngineClass.Turboprop);
            var off = TurbopropCruise();
            off.EngineRunning = 0;
            off.OnGround = 1;

            wear.ApplyTick(comps, off, 0.001);
            wear.ApplyTick(comps, TurbopropCruise(), 0.001);

            Assert.Equal(1, comps.Get(ComponentKind.HotSection).CyclesSinceOverhaul);
            Assert.Equal(1, comps.Get(ComponentKind.Engine).CyclesSinceOverhaul);
        }

        [Fact]
        public void Pneus_chegam_a_30_apos_250_pousos_normais()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);

            for (int i = 0; i < 250; i++) wear.ApplyLanding(comps, 150, 1.0);

            Assert.InRange(comps.Get(ComponentKind.Tires).Health, 29.5, 30.5);
            Assert.Equal(250, comps.Get(ComponentKind.Tires).CyclesSinceOverhaul);
            Assert.True(comps.Get(ComponentKind.LandingGear).Health > 95.0);
        }

        [Fact]
        public void Pouso_duro_danifica_trem_pneus_e_celula()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);

            var events = wear.ApplyLanding(comps, 800, 9.0);

            // Trem: 4×1,5 + 2×5 + 4×1,2 = 20,8 (+ ciclo)
            Assert.InRange(100 - comps.Get(ComponentKind.LandingGear).Health, 20.8, 21.0);
            Assert.True(comps.Get(ComponentKind.Airframe).Health < 96.5);
            Assert.NotEmpty(events);
        }

        [Fact]
        public void Frenagem_forte_desgasta_freios_mais_que_pneus()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);
            var rollout = PistonCruise();
            rollout.OnGround = 1;
            rollout.GroundSpeed = 60;
            rollout.BrakeLeftPosition = 1.0;
            rollout.BrakeRightPosition = 1.0;

            Fly(wear, comps, rollout, 15.0 / 3600.0, 1.0 / 3600.0); // 15 s de freio total a 60 kt

            double brakeLoss = 100 - comps.Get(ComponentKind.Brakes).Health;
            double tireLoss = 100 - comps.Get(ComponentKind.Tires).Health;
            Assert.InRange(brakeLoss, 0.25, 0.35);
            Assert.True(tireLoss < brakeLoss);
        }

        [Fact]
        public void Sobrecarga_G_desgasta_celula()
        {
            var comps = NewComponents(EngineClass.Piston);
            var wear = new WearEngine(Catalog, EngineClass.Piston);
            var data = PistonCruise();
            data.GForce = 4.5;

            var events = wear.ApplyTick(comps, data, 10.0 / 3600.0);

            Assert.True(comps.Get(ComponentKind.Airframe).Health < 99.0);
            Assert.Contains(events, e => e.Kind == ComponentKind.Airframe);
        }
    }
}
