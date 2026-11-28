using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Services;
using Xunit;

namespace FlightCareer.Tests.Components
{
    /// <summary>F4.2c: pane do wear and tear nativo (WEAR AND TEAR IS FAILED:&lt;id&gt;) vira pane persistente.</summary>
    public class NativeWearFailureTests
    {
        private static Dictionary<string, double> Frame(params (int id, double v)[] items) =>
            items.ToDictionary(i => $"nw-is-failed-{i.id}", i => i.v);

        [Fact]
        public void Detector_RisingEdge_FiresOnce()
        {
            var d = new NativeWearFailureDetector();
            Assert.Empty(d.Update(Frame((39, 0))));          // referência
            var events = d.Update(Frame((39, 1)));
            var e = Assert.Single(events);
            Assert.Equal(39, e.ComponentId);
            Assert.Equal(DefectCatalog.EngineFailure, e.DefectCode);
            Assert.Equal(ComponentKind.Engine, e.Kind);
            Assert.Empty(d.Update(Frame((39, 1))));          // continua 1: não repete
        }

        [Fact]
        public void Detector_FirstReadingAlreadyFailed_IsReferenceOnly()
        {
            var d = new NativeWearFailureDetector();
            Assert.Empty(d.Update(Frame((39, 1))));
            Assert.Empty(d.Update(Frame((39, 1))));
        }

        [Fact]
        public void Detector_RestartToZeroAndBack_DoesNotFireTwiceInSameFlight()
        {
            var d = new NativeWearFailureDetector();
            d.Update(Frame((39, 0)));
            Assert.Single(d.Update(Frame((39, 1))));
            Assert.Empty(d.Update(Frame((39, 0))));          // voo reiniciado no sim: nada é reparado
            Assert.Empty(d.Update(Frame((39, 1))));          // um evento por componente por voo
        }

        [Fact]
        public void Detector_Reset_StartsNewFlight()
        {
            var d = new NativeWearFailureDetector();
            d.Update(Frame((37, 0)));
            Assert.Single(d.Update(Frame((37, 1))));
            d.Reset();
            d.Update(Frame((37, 0)));
            Assert.Single(d.Update(Frame((37, 1))));
        }

        [Fact]
        public void Detector_IgnoresOtherKeysAndNaN()
        {
            var d = new NativeWearFailureDetector();
            var first = new Dictionary<string, double> { ["eng-rpm:1"] = 2500, ["nw-level-39"] = 1, ["nw-is-failed-39"] = double.NaN };
            Assert.Empty(d.Update(first));
            var second = new Dictionary<string, double> { ["eng-rpm:1"] = 0, ["nw-level-39"] = 0, ["nw-is-failed-39"] = 1 };
            Assert.Empty(d.Update(second));                  // NaN não conta como referência
            Assert.Empty(d.Update(Frame((39, 1))));
        }

        [Fact]
        public void Detector_SeveralComponents_SortedById()
        {
            var d = new NativeWearFailureDetector();
            d.Update(Frame((41, 0), (28, 0), (39, 0)));
            var events = d.Update(Frame((41, 1), (28, 1), (39, 1)));
            Assert.Equal(new[] { 28, 39, 41 }, events.Select(e => e.ComponentId));
        }

        [Theory]
        [InlineData(39, DefectCatalog.EngineFailure, ComponentKind.Engine)]
        [InlineData(40, DefectCatalog.EngineFailure, ComponentKind.Engine)]
        [InlineData(41, DefectCatalog.OilLeak, ComponentKind.Oil)]
        [InlineData(28, DefectCatalog.Alternator, ComponentKind.Electrical)]
        [InlineData(29, DefectCatalog.Battery, ComponentKind.Electrical)]
        [InlineData(35, DefectCatalog.GearDamage, ComponentKind.LandingGear)]
        [InlineData(36, DefectCatalog.BrakeFailure, ComponentKind.Brakes)]
        [InlineData(37, DefectCatalog.TireBlowout, ComponentKind.Tires)]
        [InlineData(38, DefectCatalog.TireFlat, ComponentKind.Tires)]
        [InlineData(5, DefectCatalog.FlapDamage, ComponentKind.Airframe)]
        [InlineData(7, DefectCatalog.FlapLock, ComponentKind.Airframe)]
        [InlineData(13, DefectCatalog.FlightControl, ComponentKind.Airframe)]
        [InlineData(22, DefectCatalog.Hydraulic, ComponentKind.LandingGear)]
        public void Map_MatchesApprovedTable(int id, string code, ComponentKind kind)
        {
            var spec = NativeWearFailureMap.Get(id);
            Assert.NotNull(spec);
            Assert.Equal(code, spec!.DefectCode);
            Assert.Equal(kind, spec.Kind);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(46)]
        public void Map_LogOnlyComponents_HaveNoDefect(int id)
        {
            Assert.Null(NativeWearFailureMap.Get(id)!.DefectCode);
        }

        [Fact]
        public void Map_EveryDefectCodeExistsInCatalog()
        {
            foreach (var spec in NativeWearFailureMap.All.Where(s => s.DefectCode != null))
            {
                var catalogSpec = DefectCatalog.Get(spec.DefectCode!);
                Assert.NotEqual(spec.DefectCode, catalogSpec.Title); // Get devolve o código como título quando não existe
            }
        }

        [Fact]
        public void Map_CoversAllIdsFrom0To49_ExceptUnassigned()
        {
            var unassigned = new[] { 15, 16, 17, 18 }; // sem componente na lista do SDK usada no registro
            for (int id = 0; id <= 49; id++)
            {
                if (unassigned.Contains(id)) continue;
                Assert.NotNull(NativeWearFailureMap.Get(id));
            }
        }

        [Fact]
        public void Cause_RoundTripsComponentId()
        {
            var cause = NativeWearFailureMap.CauseFor(39, "PISTON ENGINE");
            Assert.Equal("WEAR AND TEAR IS FAILED:39 passou de 0 para 1 (PISTON ENGINE)", cause);
            Assert.Equal(39, NativeWearFailureMap.ComponentIdFromCause(cause));
            Assert.Null(NativeWearFailureMap.ComponentIdFromCause("ENG FAILED:1 passou de 0 para 1"));
            Assert.Null(NativeWearFailureMap.ComponentIdFromCause(null));
        }

        [Theory]
        [InlineData("nw-is-failed-39", 39)]
        [InlineData("nw-is-failed-0", 0)]
        public void ComponentIdFromKey_Parses(string key, int id) =>
            Assert.Equal(id, NativeWearFailureMap.ComponentIdFromKey(key));

        [Theory]
        [InlineData("nw-level-39")]
        [InlineData("nw-is-failed-")]
        [InlineData("nw-is-failed-x")]
        public void ComponentIdFromKey_RejectsOthers(string key) =>
            Assert.Null(NativeWearFailureMap.ComponentIdFromKey(key));
    }
}
