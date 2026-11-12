using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using Xunit;
using static FlightCareer.Tests.Components.TestData;

namespace FlightCareer.Tests.Components
{
    public class MaintenanceTests
    {
        [Fact]
        public void Todo_componente_tem_servico_e_custo_no_catalogo()
        {
            Assert.All(Catalog.Components, d =>
            {
                Assert.False(string.IsNullOrWhiteSpace(d.ServiceName));
                Assert.True(d.MinFullRepairCost > 0);
            });
        }

        [Fact]
        public void Orcamento_e_proporcional_ao_que_falta_e_respeita_o_minimo()
        {
            var def = Catalog.Get(ComponentKind.Tires); // mínimo 800 para 0 → 100%
            var tires = new AircraftComponent { Kind = ComponentKind.Tires, Health = 75 };

            Assert.Equal(200, ComponentMaintenance.RepairCost(def, tires, aircraftPurchasePrice: 300_000), 2);   // 0,0008 × 300k = 240 < 800
            Assert.Equal(1000, ComponentMaintenance.RepairCost(def, tires, aircraftPurchasePrice: 5_000_000), 2); // 0,0008 × 5M = 4.000 × 25%
            tires.Health = 100;
            Assert.Equal(0, ComponentMaintenance.RepairCost(def, tires, 300_000));
        }

        [Fact]
        public void Reparo_completo_zera_uso_e_revela_valor_exato()
        {
            var c = new AircraftComponent { Kind = ComponentKind.Brakes, Health = 35, CyclesSinceOverhaul = 400, LastInspectionAirframeHours = 0 };

            ComponentMaintenance.Repair(c, 250, new FixedRandom(0.5), Catalog.Visibility);

            Assert.Equal(100, c.Health);
            Assert.Equal(0, c.CyclesSinceOverhaul);
            Assert.True(HealthVisibility.GetDisplayedRange(c, 250, Catalog.Visibility).IsExact);
        }

        [Fact]
        public void Vida_restante_usa_horas_ou_ciclos()
        {
            var oil = new AircraftComponent { Kind = ComponentKind.Oil, HoursSinceOverhaul = 60 };
            var tires = new AircraftComponent { Kind = ComponentKind.Tires, CyclesSinceOverhaul = 100 };

            var (oilLeft, _, oilCycles) = ComponentMaintenance.RemainingLife(Catalog.Get(ComponentKind.Oil), oil, EngineClass.Piston);
            var (tireLeft, _, tireCycles) = ComponentMaintenance.RemainingLife(Catalog.Get(ComponentKind.Tires), tires, EngineClass.Piston);

            Assert.Equal(-10, oilLeft);
            Assert.False(oilCycles);
            Assert.Equal(150, tireLeft);
            Assert.True(tireCycles);
        }

        private static (PilotAircraft Aircraft, List<AircraftComponent> Components) Fresh()
        {
            var aircraft = new PilotAircraft { CurrentAirframeHours = 100, HoursUntilNext50hInspection = 30, HoursUntilNext100hInspection = 80, HoursUntilNext500hInspection = 400, EngineTboRemainingHours = 1500 };
            var comps = NewComponents(EngineClass.Piston);
            foreach (var c in comps) c.LastInspectionAirframeHours = 100; // valores exatos
            return (aircraft, comps);
        }

        [Fact]
        public void Status_pronta_atencao_risco_interditada_e_perda_total()
        {
            var (aircraft, comps) = Fresh();
            Assert.Equal(AircraftStatusLevel.Ready, AircraftStatusEvaluator.Evaluate(aircraft, comps, Catalog, EngineClass.Piston).Level);

            comps.Get(ComponentKind.Brakes).Health = 25;
            Assert.Equal(AircraftStatusLevel.Attention, AircraftStatusEvaluator.Evaluate(aircraft, comps, Catalog, EngineClass.Piston).Level);

            comps.Get(ComponentKind.Oil).Health = 12;
            var risk = AircraftStatusEvaluator.Evaluate(aircraft, comps, Catalog, EngineClass.Piston);
            Assert.Equal(AircraftStatusLevel.FailureRisk, risk.Level);
            Assert.Contains("Óleo", risk.Detail);

            aircraft.IsInterdicted = true;
            aircraft.InterdictionReason = "Inspeção de 100 h vencida";
            Assert.Equal(AircraftStatusLevel.Interdicted, AircraftStatusEvaluator.Evaluate(aircraft, comps, Catalog, EngineClass.Piston).Level);

            comps.Get(ComponentKind.Engine).Health = 0;
            comps.Get(ComponentKind.Airframe).Health = 0;
            Assert.Equal(AircraftStatusLevel.Destroyed, AircraftStatusEvaluator.Evaluate(aircraft, comps, Catalog, EngineClass.Piston).Level);
        }

        [Fact]
        public void Status_avisa_manutencao_proxima()
        {
            var (aircraft, comps) = Fresh();
            aircraft.HoursUntilNext50hInspection = 3;

            var status = AircraftStatusEvaluator.Evaluate(aircraft, comps, Catalog, EngineClass.Piston);

            Assert.Equal(AircraftStatusLevel.MaintenanceDue, status.Level);
            Assert.Contains("50 h", status.Detail);
        }

        [Fact]
        public void Status_usa_a_faixa_exibida_e_nao_revela_o_valor_real()
        {
            var (aircraft, comps) = Fresh();
            var oil = comps.Get(ComponentKind.Oil);
            oil.Health = 15;                         // real: risco de pane
            oil.LastInspectionAirframeHours = 0;     // 100 h sem inspeção: faixa de ±25%
            oil.InspectionBias = 0.6;                // centro da faixa deslocado para cima (15 + 15 = 30)

            var status = AircraftStatusEvaluator.Evaluate(aircraft, comps, Catalog, EngineClass.Piston);

            Assert.NotEqual(AircraftStatusLevel.FailureRisk, status.Level);
        }
    }
}
