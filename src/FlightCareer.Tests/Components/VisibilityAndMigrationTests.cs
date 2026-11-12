using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using Xunit;
using static FlightCareer.Tests.Components.TestData;

namespace FlightCareer.Tests.Components
{
    public class VisibilityAndMigrationTests
    {
        [Fact]
        public void Catalogo_tem_8_componentes_por_classe_de_motor()
        {
            var piston = Catalog.For(EngineClass.Piston).Select(d => d.Kind).ToList();
            var turboprop = Catalog.For(EngineClass.Turboprop).Select(d => d.Kind).ToList();

            Assert.Equal(8, piston.Count);
            Assert.Equal(8, turboprop.Count);
            Assert.Contains(ComponentKind.Ignition, piston);
            Assert.DoesNotContain(ComponentKind.HotSection, piston);
            Assert.Contains(ComponentKind.HotSection, turboprop);
            Assert.DoesNotContain(ComponentKind.Ignition, turboprop);
        }

        [Fact]
        public void Logo_apos_inspecao_completa_o_valor_e_exato()
        {
            var c = new AircraftComponent { Kind = ComponentKind.Engine, Health = 63.4 };
            HealthVisibility.RecordFullInspection(c, 120.0, new FixedRandom(0.9), Catalog.Visibility);

            var range = HealthVisibility.GetDisplayedRange(c, 120.0, Catalog.Visibility);

            Assert.True(range.IsExact);
            Assert.Equal(63.4, range.Min, 3);
        }

        [Fact]
        public void Faixa_abre_2_porcento_a_cada_5_horas_e_tem_teto_de_25()
        {
            var c = new AircraftComponent { Kind = ComponentKind.Engine, Health = 60.0, LastInspectionAirframeHours = 100.0 };

            Assert.Equal(2.0, HealthVisibility.HalfWidth(c, 105.0, Catalog.Visibility), 6);
            Assert.Equal(20.0, HealthVisibility.HalfWidth(c, 150.0, Catalog.Visibility), 6);
            Assert.Equal(25.0, HealthVisibility.HalfWidth(c, 1000.0, Catalog.Visibility), 6);
        }

        [Fact]
        public void Faixa_exibida_sempre_contem_o_valor_real()
        {
            var random = new SystemRandomSource(3);
            for (int i = 0; i < 5000; i++)
            {
                var c = new AircraftComponent { Health = random.NextDouble() * 100.0 };
                HealthVisibility.RecordFullInspection(c, 0.0, random, Catalog.Visibility);
                var range = HealthVisibility.GetDisplayedRange(c, random.NextDouble() * 200.0, Catalog.Visibility);
                Assert.InRange(c.Health, range.Min - 1e-9, range.Max + 1e-9);
            }
        }

        [Fact]
        public void Walkaround_estreita_so_componentes_visiveis_por_fora()
        {
            var tires = new AircraftComponent { Kind = ComponentKind.Tires, Health = 50, LastInspectionAirframeHours = 0 };
            var engine = new AircraftComponent { Kind = ComponentKind.Engine, Health = 50, LastInspectionAirframeHours = 0 };
            var random = new FixedRandom(0.5);

            Assert.True(HealthVisibility.RecordWalkaround(tires, Catalog.Get(ComponentKind.Tires), 100.0, random, Catalog.Visibility));
            Assert.False(HealthVisibility.RecordWalkaround(engine, Catalog.Get(ComponentKind.Engine), 100.0, random, Catalog.Visibility));

            Assert.Equal(10.0, HealthVisibility.HalfWidth(tires, 100.0, Catalog.Visibility), 6);
            Assert.Equal(25.0, HealthVisibility.HalfWidth(engine, 100.0, Catalog.Visibility), 6);
        }

        [Fact]
        public void Aeronavegabilidade_e_o_pior_componente_critico_e_condicao_e_media_ponderada()
        {
            var comps = NewComponents(EngineClass.Piston, health: 100.0);
            comps.Get(ComponentKind.Oil).Health = 15.0;      // crítico
            comps.Get(ComponentKind.Tires).Health = 5.0;     // não crítico
            foreach (var c in comps) c.LastInspectionAirframeHours = 10.0;

            var summary = AircraftHealthSummary.Compute(comps, Catalog, EngineClass.Piston, 10.0);

            Assert.Equal(15.0, summary.Airworthiness, 6);
            Assert.Equal(ComponentKind.Oil, summary.WorstCriticalComponent);
            double totalWeight = Catalog.For(EngineClass.Piston).Sum(d => d.Weight);
            double expected = (100.0 * totalWeight - 85.0 * Catalog.Get(ComponentKind.Oil).Weight - 95.0 * Catalog.Get(ComponentKind.Tires).Weight) / totalWeight;
            Assert.Equal(expected, summary.Condition, 6);
            Assert.True(summary.DisplayedCondition.IsExact);
        }

        [Fact]
        public void Migracao_preserva_as_colunas_antigas()
        {
            var aircraft = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                EngineCondition = 72.0,
                OilCondition = 40.0,
                MagnetosCondition = 88.0,
                TiresCondition = 55.0,
                AirframeCondition = 91.0,
                AvionicsCondition = 80.0,
                EngineTboRemainingHours = 1300.0,
                HoursUntilNext50hInspection = 20.0,
                HoursUntilNext500hInspection = 450.0,
                CurrentAirframeHours = 2500.0
            };

            var comps = LegacyComponentMigrator.CreateComponents(aircraft, EngineClass.Piston, Catalog, new FixedRandom(0.5));

            Assert.Equal(8, comps.Count);
            Assert.All(comps, c => Assert.Equal(aircraft.Id, c.PilotAircraftId));
            Assert.Equal(72.0, comps.Get(ComponentKind.Engine).Health);
            Assert.Equal(500.0, comps.Get(ComponentKind.Engine).HoursSinceOverhaul, 6);
            Assert.Equal(30.0, comps.Get(ComponentKind.Oil).HoursSinceOverhaul, 6);
            Assert.Equal(50.0, comps.Get(ComponentKind.Ignition).HoursSinceOverhaul, 6);
            Assert.Equal(55.0, comps.Get(ComponentKind.Brakes).Health);
            Assert.Equal(2470.0, comps.Get(ComponentKind.Tires).LastInspectionAirframeHours, 6);
            // Pneus a 55%: 45% de desgaste ÷ 0,28% por pouso ≈ 161 ciclos
            Assert.InRange(comps.Get(ComponentKind.Tires).CyclesSinceOverhaul, 160, 162);
        }

        [Theory]
        [InlineData("C172 Skyhawk", "Geral", "Avgas", EngineClass.Piston)]
        [InlineData("Diamond DA62", "Geral", "Jet A-1", EngineClass.Piston)]
        [InlineData("PC-12 NGX", "Geral", "Jet A-1", EngineClass.Turboprop)]
        [InlineData("Pilatus PC-24", "Geral", "Jet A-1", EngineClass.Jet)]
        [InlineData("A320neo", "Comercial", "Jet A-1", EngineClass.Jet)]
        public void Classe_do_motor_a_partir_do_cadastro(string model, string type, string fuel, EngineClass expected)
        {
            var aircraftType = new AircraftType { Model = model, Type = type, FuelType = fuel };
            Assert.Equal(expected, EngineClassResolver.FromAircraftType(aircraftType));
        }
    }
}
