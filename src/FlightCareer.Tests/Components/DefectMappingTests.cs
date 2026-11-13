using FlightCareer.Domain.Components;
using FlightCareer.Domain.Services;
using Xunit;

namespace FlightCareer.Tests.Components
{
    /// <summary>Pane do sistema de componentes → pane persistente (discrepância entre voos).</summary>
    public class DefectMappingTests
    {
        [Theory]
        [InlineData(FailureEffect.EngineFailure, ComponentKind.Engine, true, DefectCatalog.EngineFailure)]
        [InlineData(FailureEffect.EngineFailure, ComponentKind.Oil, true, DefectCatalog.EngineFailure)] // fusão de bronzina
        [InlineData(FailureEffect.EngineFire, ComponentKind.HotSection, true, DefectCatalog.EngineFire)]
        [InlineData(FailureEffect.OilLeak, ComponentKind.Oil, false, DefectCatalog.OilLeak)]
        [InlineData(FailureEffect.MagnetoLeft, ComponentKind.Ignition, false, DefectCatalog.MagnetoLeft)]
        [InlineData(FailureEffect.MagnetoRight, ComponentKind.Ignition, false, DefectCatalog.MagnetoRight)]
        [InlineData(FailureEffect.AlternatorOff, ComponentKind.Electrical, false, DefectCatalog.Alternator)]
        [InlineData(FailureEffect.ElectricalFailure, ComponentKind.Electrical, true, DefectCatalog.Electrical)]
        [InlineData(FailureEffect.VacuumFailure, ComponentKind.Instruments, false, DefectCatalog.Vacuum)]
        [InlineData(FailureEffect.PitotBlockage, ComponentKind.Instruments, true, DefectCatalog.Pitot)]
        [InlineData(FailureEffect.StaticBlockage, ComponentKind.Instruments, false, DefectCatalog.StaticPort)]
        [InlineData(FailureEffect.FlapJam, ComponentKind.Airframe, false, DefectCatalog.FlapLock)]
        [InlineData(FailureEffect.BrakeFailureOneSide, ComponentKind.Brakes, true, DefectCatalog.BrakeFailure)]
        [InlineData(FailureEffect.BrakeFailureOneSide, ComponentKind.Tires, true, DefectCatalog.TireBlowout)]
        [InlineData(FailureEffect.HydraulicFailure, ComponentKind.LandingGear, true, DefectCatalog.Hydraulic)]
        [InlineData(FailureEffect.None, ComponentKind.LandingGear, true, DefectCatalog.GearCollapse)]
        [InlineData(FailureEffect.None, ComponentKind.Airframe, true, DefectCatalog.Structural)]
        [InlineData(FailureEffect.None, ComponentKind.Engine, true, DefectCatalog.EngineDamage)]
        public void Pane_com_efeito_ou_critica_vira_discrepancia(FailureEffect effect, ComponentKind kind, bool critical, string expected)
        {
            Assert.Equal(expected, DefectCatalog.CodeFor(effect, kind, critical));
        }

        [Theory]
        [InlineData(ComponentKind.Engine)]      // perda parcial de potência, funcionamento irregular
        [InlineData(ComponentKind.Ignition)]    // velas encharcadas
        [InlineData(ComponentKind.Brakes)]      // fading
        [InlineData(ComponentKind.Tires)]       // pneu murcho
        [InlineData(ComponentKind.Electrical)]  // bateria fraca
        public void Pane_leve_sem_efeito_fica_so_no_componente(ComponentKind kind)
        {
            Assert.Null(DefectCatalog.CodeFor(FailureEffect.None, kind, critical: false));
        }

        [Fact]
        public void Todo_codigo_mapeado_existe_no_catalogo()
        {
            foreach (FailureEffect effect in System.Enum.GetValues(typeof(FailureEffect)))
            foreach (ComponentKind kind in System.Enum.GetValues(typeof(ComponentKind)))
            foreach (bool critical in new[] { true, false })
            {
                var code = DefectCatalog.CodeFor(effect, kind, critical);
                if (code == null) continue;
                Assert.Equal(code, DefectCatalog.Get(code).Code);
                Assert.NotEqual(code, DefectCatalog.Get(code).Title); // título real, não o fallback
            }
        }
    }
}
