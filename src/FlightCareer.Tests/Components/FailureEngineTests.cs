using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using Xunit;
using static FlightCareer.Tests.Components.TestData;

namespace FlightCareer.Tests.Components
{
    public class FailureEngineTests
    {
        [Theory]
        [InlineData(100.0)]
        [InlineData(50.0)]
        [InlineData(20.0)]
        [InlineData(20.01)]
        public void Acima_ou_no_limiar_de_20_nao_ha_risco(double health)
        {
            Assert.Equal(0.0, FailureEngine.HazardPerHour(health, Catalog.Hazard));
        }

        [Theory]
        [InlineData(19.999, 0.02)]
        [InlineData(10.0, 0.2)]
        [InlineData(0.0, 2.0)]
        public void Curva_de_risco_suave_abaixo_de_20(double health, double expectedPerHour)
        {
            Assert.Equal(expectedPerHour, FailureEngine.HazardPerHour(health, Catalog.Hazard), 3);
        }

        [Fact]
        public void Risco_cresce_continuamente_sem_degrau()
        {
            double previous = 0.0;
            for (double h = 19.9; h >= 0.0; h -= 0.1)
            {
                double lambda = FailureEngine.HazardPerHour(h, Catalog.Hazard);
                Assert.True(lambda > previous);
                Assert.True(previous == 0.0 || lambda / previous < 1.05, $"salto em {h:F1}%");
                previous = lambda;
            }
        }

        [Theory]
        [InlineData(20.0, 0.000)]
        [InlineData(15.0, 0.061)]
        [InlineData(10.0, 0.181)]
        [InlineData(5.0, 0.469)]
        [InlineData(0.0, 0.865)]
        public void Monte_Carlo_chance_de_pane_em_uma_hora_de_voo(double health, double expected)
        {
            const int flights = 20000;
            const double dt = 1.0 / 3600.0; // ticks de 1 s
            var random = new SystemRandomSource(seed: 12345);
            int failures = 0;

            for (int f = 0; f < flights; f++)
            {
                var engine = new FailureEngine(Catalog, EngineClass.Piston, random);
                var comps = new List<AircraftComponent> { new() { Kind = ComponentKind.Engine, Health = health } };
                for (int s = 0; s < 3600; s += 60)
                {
                    // Avalia em blocos de 60 s para manter o teste rápido (p é exata para qualquer dt)
                    if (engine.Evaluate(comps, dt * 60).Count > 0) { failures++; break; }
                }
            }

            double rate = failures / (double)flights;
            Assert.InRange(rate, expected - 0.012, expected + 0.012);
        }

        [Fact]
        public void Monte_Carlo_dez_mil_horas_acima_de_20_sem_nenhuma_pane()
        {
            var engine = new FailureEngine(Catalog, EngineClass.Piston, new SystemRandomSource(seed: 7));
            var comps = NewComponents(EngineClass.Piston, health: 20.5);

            for (int i = 0; i < 10000; i++)
                Assert.Empty(engine.Evaluate(comps, 1.0));
        }

        [Fact]
        public void Modo_de_falha_segue_os_pesos_do_catalogo()
        {
            var random = new SystemRandomSource(seed: 99);
            var counts = new Dictionary<string, int>();
            const int trials = 20000;

            for (int i = 0; i < trials; i++)
            {
                var engine = new FailureEngine(Catalog, EngineClass.Piston, random);
                var comps = new List<AircraftComponent> { new() { Kind = ComponentKind.Engine, Health = 0.0 } };
                var failure = engine.Evaluate(comps, 100.0).Single(); // p ≈ 1
                counts[failure.Mode.Id] = counts.GetValueOrDefault(failure.Mode.Id) + 1;
            }

            var def = Catalog.Get(ComponentKind.Engine);
            double totalWeight = def.FailureModes.Sum(m => m.Weight);
            foreach (var mode in def.FailureModes)
            {
                double expected = mode.Weight / totalWeight;
                double actual = counts.GetValueOrDefault(mode.Id) / (double)trials;
                Assert.InRange(actual, expected - 0.015, expected + 0.015);
            }
        }

        [Fact]
        public void Componente_falha_no_maximo_uma_vez_por_voo()
        {
            var engine = new FailureEngine(Catalog, EngineClass.Piston, new FixedRandom(0.0));
            var comps = new List<AircraftComponent> { new() { Kind = ComponentKind.Tires, Health = 5.0 } };
            var rolling = new SimTelemetry { OnGround = 1, GroundSpeed = 40 };

            Assert.Single(engine.Evaluate(comps, rolling, 0.01));
            Assert.Empty(engine.Evaluate(comps, rolling, 0.01));

            engine.Reset();
            Assert.Single(engine.Evaluate(comps, rolling, 0.01));
        }

        [Fact]
        public void So_ha_exposicao_quando_o_componente_esta_em_uso()
        {
            var engine = new FailureEngine(Catalog, EngineClass.Piston, new FixedRandom(0.0));
            var comps = NewComponents(EngineClass.Piston, health: 0.0);
            var parked = new SimTelemetry { OnGround = 1, EngineRunning = 0, GroundSpeed = 0 };

            Assert.Empty(engine.Evaluate(comps, parked, 1.0));
        }

        [Fact]
        public void Modos_de_falha_respeitam_a_classe_do_motor()
        {
            var engine = new FailureEngine(Catalog, EngineClass.Turboprop, new SystemRandomSource(1));
            var comps = NewComponents(EngineClass.Turboprop, health: 0.0);

            var failures = engine.Evaluate(comps, 100.0);

            Assert.DoesNotContain(failures, f => f.Kind == ComponentKind.Ignition);
            Assert.Contains(failures, f => f.Kind == ComponentKind.HotSection);
        }
    }
}
