using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class HudHealthSummaryTests
    {
        private static readonly PartCatalog DefaultCatalog = PartCatalog.LoadDefault();

        [Fact]
        public void Build_KnownHealthAbove80_ReportsInOrder_WithoutLeakingRealHealth()
        {
            // Peça com Health 5 (real crítica/pane) e KnownHealth 90 visto há 0 h -> grupo "Em ordem"
            var part = new AircraftPart
            {
                PartId = "engine.cylinders",
                Health = 5.0,
                KnownHealth = 90.0,
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };

            var summary = HudHealthSummary.Build(new[] { part }, DefaultCatalog, airframeHours: 100.0, growthPerHour: 0.4);

            Assert.Equal(PartDisplayState.Green, summary.Engine.State);
            Assert.Equal("Em ordem", summary.Engine.StatusText);
        }

        [Fact]
        public void Build_KnownHealthInYellowBand_ReportsWarning()
        {
            // Grupo com uma peça amarela conhecida (ex.: 35%) -> "Atenção"
            var part = new AircraftPart
            {
                PartId = "engine.cylinders",
                Health = 35.0,
                KnownHealth = 35.0,
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };

            var summary = HudHealthSummary.Build(new[] { part }, DefaultCatalog, airframeHours: 100.0, growthPerHour: 0.4);

            Assert.Equal(PartDisplayState.Yellow, summary.Engine.State);
            Assert.Equal("Atenção", summary.Engine.StatusText);
        }

        [Fact]
        public void Build_UnknownPart_ReportsUncertain()
        {
            // Peça nunca vista (sem KnownHealth) -> "Incerta"
            var part = new AircraftPart
            {
                PartId = "engine.cylinders",
                Health = 95.0,
                KnownHealth = null,
                KnownAtAirframeHours = null,
                IsBroken = false
            };

            var summary = HudHealthSummary.Build(new[] { part }, DefaultCatalog, airframeHours: 100.0, growthPerHour: 0.4);

            Assert.Equal(PartDisplayState.Uncertain, summary.Engine.State);
            Assert.Equal("Incerta", summary.Engine.StatusText);
        }

        [Fact]
        public void Build_WornPart_ReportsInUse()
        {
            // Peça com KnownHealth entre 40 e 80 -> "Em uso"
            var part = new AircraftPart
            {
                PartId = "airframe.wing_structure",
                Health = 60.0,
                KnownHealth = 60.0,
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };

            var summary = HudHealthSummary.Build(new[] { part }, DefaultCatalog, airframeHours: 100.0, growthPerHour: 0.4);

            Assert.Equal(PartDisplayState.Worn, summary.Airframe.State);
            Assert.Equal("Em uso", summary.Airframe.StatusText);
        }

        [Fact]
        public void Build_BrokenPart_ReportsBroken()
        {
            var part = new AircraftPart
            {
                PartId = "tires.main",
                Health = 0.0,
                KnownHealth = 10.0,
                IsBroken = true
            };

            var summary = HudHealthSummary.Build(new[] { part }, DefaultCatalog, airframeHours: 100.0, growthPerHour: 0.4);

            Assert.Equal(PartDisplayState.Broken, summary.Tires.State);
            Assert.Equal("Quebrada", summary.Tires.StatusText);
        }

        [Fact]
        public void Build_WorstStateTakesPriority()
        {
            // Peça 1 boa (90%), peça 2 amarela (35%) -> Pior prevalece ("Atenção")
            var p1 = new AircraftPart
            {
                PartId = "engine.cylinders",
                Health = 90.0,
                KnownHealth = 90.0,
                KnownAtAirframeHours = 100.0
            };
            var p2 = new AircraftPart
            {
                PartId = "engine.fuel_pump_engine",
                Health = 35.0,
                KnownHealth = 35.0,
                KnownAtAirframeHours = 100.0
            };

            var summary = HudHealthSummary.Build(new[] { p1, p2 }, DefaultCatalog, airframeHours: 100.0, growthPerHour: 0.4);

            Assert.Equal(PartDisplayState.Yellow, summary.Engine.State);
            Assert.Equal("Atenção", summary.Engine.StatusText);
        }

        [Fact]
        public void BuildFromComponents_TranslatesDisplayedRanges()
        {
            var compAirframe = new AircraftComponent
            {
                Kind = ComponentKind.Airframe,
                Health = 100.0,
                LastInspectionAirframeHours = 100.0,
                InspectionPrecision = 0.0
            };
            var compEngine = new AircraftComponent
            {
                Kind = ComponentKind.Engine,
                Health = 30.0,
                LastInspectionAirframeHours = 100.0,
                InspectionPrecision = 0.0
            };
            var compTires = new AircraftComponent
            {
                Kind = ComponentKind.Tires,
                Health = 10.0,
                LastInspectionAirframeHours = 100.0,
                InspectionPrecision = 0.0
            };

            var summary = HudHealthSummary.BuildFromComponents(
                new[] { compAirframe, compEngine, compTires },
                airframeHours: 100.0,
                visibility: new VisibilitySettings());

            Assert.Equal(PartDisplayState.Green, summary.Airframe.State);
            Assert.Equal("Em ordem", summary.Airframe.StatusText);

            Assert.Equal(PartDisplayState.Yellow, summary.Engine.State);
            Assert.Equal("Atenção", summary.Engine.StatusText);

            Assert.Equal(PartDisplayState.Red, summary.Tires.State);
            Assert.Equal("Risco", summary.Tires.StatusText);
        }
    }
}
