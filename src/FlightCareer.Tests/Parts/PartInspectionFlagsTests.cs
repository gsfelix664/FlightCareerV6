using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartInspectionFlagsTests
    {
        private static PartDefinition CreateDef(
            string id = "engine.cylinders",
            string name = "Cilindros",
            string group = PartGroup.Motor,
            double lifeHours = 100.0,
            int? lifeCycles = null,
            int? calendarMonths = null,
            bool visibleOnWalkaround = false)
        {
            return new PartDefinition(
                id: id,
                name: name,
                group: group,
                appliesTo: new PartAppliesTo(),
                lifeHours: lifeHours,
                lifeCycles: lifeCycles,
                visibleOnWalkaround: visibleOnWalkaround,
                blocksFlightWhenBroken: true,
                partPrice: 1000m,
                laborHours: 5.0,
                sensors: Array.Empty<string>(),
                lifeSource: "POH",
                calendarLifeMonths: calendarMonths);
        }

        [Fact]
        public void Evaluate_Quebrada_Vermelho()
        {
            var part = new AircraftPart { Health = 0, IsBroken = true };
            var def = CreateDef();

            var result = PartInspectionFlags.Evaluate(part, def, DateTime.UtcNow);

            Assert.Equal(PartFlag.Broken, result.Level);
            Assert.Equal("Quebrada", result.Reason);
        }

        [Fact]
        public void Evaluate_Horas90Pct_Amarelo()
        {
            var part = new AircraftPart { Health = 80, HoursSinceReplacement = 90.0 };
            var def = CreateDef(lifeHours: 100.0);

            var result = PartInspectionFlags.Evaluate(part, def, DateTime.UtcNow);
            Assert.Equal(PartFlag.None, result.Level);

            var lifeNote = PartInspectionFlags.LifeNote(part, def, DateTime.UtcNow);
            Assert.Equal("Vida 400 h (vida real) · faltam 10 h de voo", lifeNote);
        }

        [Fact]
        public void Evaluate_VidaVencida_AmareloComTexto()
        {
            var part = new AircraftPart { Health = 65, HoursSinceReplacement = 260.0 };
            var def = CreateDef(lifeHours: 250.0);

            var result = PartInspectionFlags.Evaluate(part, def, DateTime.UtcNow);
            Assert.Equal(PartFlag.None, result.Level);

            var lifeNote = PartInspectionFlags.LifeNote(part, def, DateTime.UtcNow);
            Assert.Equal("Vida 1000 h (vida real) · vencida há 10 h de voo", lifeNote);
        }

        [Fact]
        public void Evaluate_Ciclos90Pct_Amarelo()
        {
            var part = new AircraftPart { Health = 85, CyclesSinceReplacement = 90 };
            var def = CreateDef(lifeHours: 0, lifeCycles: 100);

            var result = PartInspectionFlags.Evaluate(part, def, DateTime.UtcNow);
            Assert.Equal(PartFlag.None, result.Level);

            var lifeNote = PartInspectionFlags.LifeNote(part, def, DateTime.UtcNow);
            Assert.Equal("Ciclos 400 (vida real) · faltam 10 ciclos de voo", lifeNote);
        }

        [Fact]
        public void Evaluate_Calendario90Pct_Amarelo()
        {
            var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var nowUtc = baseDate.AddDays(335); // ~11 meses (91.6% de 12 meses)
            var part = new AircraftPart { Health = 90, LastReplacedAtUtc = baseDate };
            var def = CreateDef(lifeHours: 0, calendarMonths: 12);

            var result = PartInspectionFlags.Evaluate(part, def, nowUtc);
            Assert.Equal(PartFlag.None, result.Level);

            var lifeNote = PartInspectionFlags.LifeNote(part, def, nowUtc);
            Assert.NotNull(lifeNote);
            Assert.Contains("11 / 12 meses", lifeNote);
        }

        [Fact]
        public void Evaluate_SaudeBaixa_Amarelo()
        {
            var part = new AircraftPart { Health = 35.0, HoursSinceReplacement = 10.0 };
            var def = CreateDef(lifeHours: 100.0);

            var result = PartInspectionFlags.Evaluate(part, def, DateTime.UtcNow);

            Assert.Equal(PartFlag.Suspect, result.Level);
            Assert.Equal("Faixa amarela", result.Reason);
        }

        [Fact]
        public void Evaluate_Ok_None()
        {
            var part = new AircraftPart { Health = 95.0, HoursSinceReplacement = 10.0 };
            var def = CreateDef(lifeHours: 100.0);

            var result = PartInspectionFlags.Evaluate(part, def, DateTime.UtcNow);

            Assert.Equal(PartFlag.None, result.Level);
            Assert.Equal(string.Empty, result.Reason);
        }

        [Fact]
        public void ApplyInspection_SoEscopo()
        {
            var defInScope = CreateDef("engine.cylinders", "Cilindros", PartGroup.Motor, lifeHours: 100.0);
            var defOutOfScope = CreateDef("gear.tires", "Pneus", PartGroup.Pneus, lifeHours: 100.0);
            var catalog = new PartCatalog(new[] { defInScope, defOutOfScope });

            var p1 = new AircraftPart { PartId = "engine.cylinders", Health = 30.0, Flag = PartFlag.None };
            var p2 = new AircraftPart { PartId = "gear.tires", Health = 30.0, Flag = PartFlag.None, FlaggedBy = PartFlagSource.None };

            int marked = PartInspectionFlags.ApplyInspection(
                new[] { p1, p2 },
                catalog,
                def => def.Group == PartGroup.Motor,
                PartFlagSource.Inspection,
                150.0,
                DateTime.UtcNow);

            Assert.Equal(1, marked);
            Assert.Equal(PartFlag.Suspect, p1.Flag);
            Assert.Equal(PartFlagSource.Inspection, p1.FlaggedBy);
            Assert.Equal(150.0, p1.FlaggedAirframeHours);

            // Peça fora do escopo não foi alterada
            Assert.Equal(PartFlag.None, p2.Flag);
            Assert.Equal(PartFlagSource.None, p2.FlaggedBy);
            Assert.Null(p2.FlaggedAirframeHours);
        }

        [Fact]
        public void ApplyInspection_LimpaPecaOk()
        {
            var def = CreateDef("engine.cylinders", "Cilindros", PartGroup.Motor, lifeHours: 100.0);
            var catalog = new PartCatalog(new[] { def });

            // Peça anteriormente suspeita, mas agora com saúde 100% e 0 horas
            var part = new AircraftPart
            {
                PartId = "engine.cylinders",
                Health = 100.0,
                HoursSinceReplacement = 0.0,
                Flag = PartFlag.Suspect,
                FlaggedBy = PartFlagSource.Inspection,
                FlaggedAirframeHours = 120.0,
                FlaggedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            int marked = PartInspectionFlags.ApplyInspection(
                new[] { part },
                catalog,
                _ => true,
                PartFlagSource.Inspection,
                150.0,
                DateTime.UtcNow);

            Assert.Equal(0, marked);
            Assert.Equal(PartFlag.None, part.Flag);
            Assert.Equal(PartFlagSource.None, part.FlaggedBy);
            Assert.Null(part.FlaggedAirframeHours);
            Assert.Null(part.FlaggedAtUtc);
        }

        [Fact]
        public void ApplyInspection_Walkaround_SoVisiveis()
        {
            var defVis = CreateDef("airframe.pitot", "Tubo de Pitot", PartGroup.Asa, lifeHours: 100.0, visibleOnWalkaround: true);
            var defOculta = CreateDef("engine.bearings", "Bronzinas", PartGroup.Motor, lifeHours: 100.0, visibleOnWalkaround: false);
            var catalog = new PartCatalog(new[] { defVis, defOculta });

            var p1 = new AircraftPart { PartId = "airframe.pitot", Health = 20.0, Flag = PartFlag.None };
            var p2 = new AircraftPart { PartId = "engine.bearings", Health = 20.0, Flag = PartFlag.None };

            int marked = PartInspectionFlags.ApplyInspection(
                new[] { p1, p2 },
                catalog,
                def => def.VisibleOnWalkaround,
                PartFlagSource.Walkaround,
                50.0,
                DateTime.UtcNow);

            Assert.Equal(1, marked);
            Assert.Equal(PartFlag.Suspect, p1.Flag);
            Assert.Equal(PartFlagSource.Walkaround, p1.FlaggedBy);

            Assert.Equal(PartFlag.None, p2.Flag);
        }

        [Fact]
        public void IsVisible_DepoisDoVoo_Some()
        {
            var part = new AircraftPart
            {
                Flag = PartFlag.Suspect,
                FlaggedAirframeHours = 100.0
            };

            // Mesmo valor de horas totais da aeronave: visível
            Assert.True(PartInspectionFlags.IsVisible(part, 100.0));
            Assert.True(PartInspectionFlags.IsVisible(part, 100.005));

            // Aeronave realizou um voo e acumulou horas: não visível
            Assert.False(PartInspectionFlags.IsVisible(part, 100.5));
            Assert.False(PartInspectionFlags.IsVisible(part, 101.0));

            // Peça sem flag: não visível
            part.Flag = PartFlag.None;
            Assert.False(PartInspectionFlags.IsVisible(part, 100.0));
        }

        [Fact]
        public void ClearForKinds_LimpaSoOsKinds()
        {
            var defEngine = CreateDef("engine.cylinders", "Cilindros", PartGroup.Motor);
            var defTires = CreateDef("gear.tires", "Pneus", PartGroup.Pneus);
            var catalog = new PartCatalog(new[] { defEngine, defTires });

            var pEngine = new AircraftPart
            {
                PartId = "engine.cylinders",
                Flag = PartFlag.Suspect,
                FlaggedBy = PartFlagSource.Inspection,
                FlaggedAirframeHours = 100.0
            };
            var pTires = new AircraftPart
            {
                PartId = "gear.tires",
                Flag = PartFlag.Suspect,
                FlaggedBy = PartFlagSource.Walkaround,
                FlaggedAirframeHours = 100.0
            };

            var kindsToClear = new HashSet<ComponentKind> { ComponentKind.Engine };
            PartInspectionFlags.ClearForKinds(new[] { pEngine, pTires }, catalog, kindsToClear);

            // Engine foi limpo
            Assert.Equal(PartFlag.None, pEngine.Flag);
            Assert.Equal(PartFlagSource.None, pEngine.FlaggedBy);
            Assert.Null(pEngine.FlaggedAirframeHours);

            // Tires permaneceu intacto
            Assert.Equal(PartFlag.Suspect, pTires.Flag);
            Assert.Equal(PartFlagSource.Walkaround, pTires.FlaggedBy);
            Assert.Equal(100.0, pTires.FlaggedAirframeHours);
        }
    }
}
