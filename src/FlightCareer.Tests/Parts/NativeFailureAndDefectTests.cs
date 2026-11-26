using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class NativeFailureAndDefectTests
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

        // 1. NativeFailurePartMap_TodasAsCandidatas_ExistemNoCatalogo
        [Fact]
        public void NativeFailurePartMap_TodasAsCandidatas_ExistemNoCatalogo()
        {
            var catalog = PartsCatalog;
            for (int id = 0; id <= 49; id++)
            {
                var candidates = NativeFailurePartMap.CandidatesFor(id);
                foreach (var partId in candidates)
                {
                    var def = catalog.Get(partId);
                    Assert.True(def != null, $"PartId '{partId}' mapeado para ID {id} não existe no catálogo.");
                }
            }
        }

        // 2. ApplyNativeFailed_Id39_QuebraSoUmaPeca
        [Fact]
        public void ApplyNativeFailed_Id39_QuebraSoUmaPeca()
        {
            var catalog = PartsCatalog;
            var rules = WearRules;
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(catalog, rules, resolver);

            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 90.0 };
            var pValves = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 70.0 };
            var pCrank = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.crankshaft", Health = 80.0 };
            var pAlt = new AircraftPart { Id = Guid.NewGuid(), PartId = "electrical.alternator", Health = 50.0 };
            var pMag = new AircraftPart { Id = Guid.NewGuid(), PartId = "ignition.magnetos", Health = 40.0 };
            var pProp = new AircraftPart { Id = Guid.NewGuid(), PartId = "propeller.blades", Health = 60.0 };

            var all = new[] { pCyl, pValves, pCrank, pAlt, pMag, pProp };
            tracker.Begin(all, CreateC152Profile(), new[] { "piston" });

            int broken = tracker.ApplyNativeFailed(39, ComponentKind.Engine);
            Assert.Equal(1, broken);

            // Apenas engine.valves quebrou (menor saúde entre as candidatas do ID 39: cylinders, valves, crankshaft)
            Assert.True(pValves.IsBroken);
            Assert.Equal(0.0, pValves.Health);

            Assert.False(pCyl.IsBroken);
            Assert.Equal(90.0, pCyl.Health);
            Assert.False(pCrank.IsBroken);
            Assert.Equal(80.0, pCrank.Health);
            Assert.False(pAlt.IsBroken);
            Assert.Equal(50.0, pAlt.Health);
            Assert.False(pMag.IsBroken);
            Assert.Equal(40.0, pMag.Health);
            Assert.False(pProp.IsBroken);
            Assert.Equal(60.0, pProp.Health);
        }

        // 3. ApplyNativeFailed_Id35_QuebraSoUmaPecaDoTrem
        [Fact]
        public void ApplyNativeFailed_Id35_QuebraSoUmaPecaDoTrem()
        {
            var catalog = PartsCatalog;
            var rules = WearRules;
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(catalog, rules, resolver);

            var pMainStruts = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.main_struts", Health = 95.0 };
            var pNoseStrut = new AircraftPart { Id = Guid.NewGuid(), PartId = "gear.nose_strut", Health = 85.0 };
            var pWing = new AircraftPart { Id = Guid.NewGuid(), PartId = "airframe.wings", Health = 30.0 };
            var pTires = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 20.0 };

            var all = new[] { pMainStruts, pNoseStrut, pWing, pTires };
            tracker.Begin(all, CreateC152Profile(), new[] { "piston" });

            int broken = tracker.ApplyNativeFailed(35, ComponentKind.LandingGear);
            Assert.Equal(1, broken);

            Assert.True(pNoseStrut.IsBroken);
            Assert.Equal(0.0, pNoseStrut.Health);

            Assert.False(pMainStruts.IsBroken);
            Assert.Equal(95.0, pMainStruts.Health);
            Assert.False(pWing.IsBroken);
            Assert.Equal(30.0, pWing.Health);
            Assert.False(pTires.IsBroken);
            Assert.Equal(20.0, pTires.Health);
        }

        // 4. ApplyNativeFailed_CandidataJaQuebrada_QuebraAProxima
        [Fact]
        public void ApplyNativeFailed_CandidataJaQuebrada_QuebraAProxima()
        {
            var catalog = PartsCatalog;
            var rules = WearRules;
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(catalog, rules, resolver);

            var pValves = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 0.0, IsBroken = true };
            var pCyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 90.0 };
            var pCrank = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.crankshaft", Health = 80.0 };

            var all = new[] { pValves, pCyl, pCrank };
            tracker.Begin(all, CreateC152Profile(), new[] { "piston" });

            // 1ª chamada: valves já quebrada; entre cylinders (90) e crankshaft (80), quebra crankshaft (80)
            int b1 = tracker.ApplyNativeFailed(39, null);
            Assert.Equal(1, b1);
            Assert.True(pCrank.IsBroken);
            Assert.Equal(0.0, pCrank.Health);
            Assert.False(pCyl.IsBroken);

            // Quebra a última candidata
            int b2 = tracker.ApplyNativeFailed(39, null);
            Assert.Equal(1, b2);
            Assert.True(pCyl.IsBroken);

            // 3ª chamada: todas as candidatas quebradas e sem fallback -> 0
            int b3 = tracker.ApplyNativeFailed(39, null);
            Assert.Equal(0, b3);
        }

        // 5. ApplyNativeFailed_SemCandidataInstalada_UsaFallbackDoGrupo
        [Fact]
        public void ApplyNativeFailed_SemCandidataInstalada_UsaFallbackDoGrupo()
        {
            var catalog = PartsCatalog;
            var rules = WearRules;
            var resolver = CreateResolver();
            var tracker = new PartFlightTracker(catalog, rules, resolver);

            var pOil1 = new AircraftPart { Id = Guid.NewGuid(), PartId = "oil.cooler", Health = 80.0 };
            var pOil2 = new AircraftPart { Id = Guid.NewGuid(), PartId = "oil.filter", Health = 45.0 };

            tracker.Begin(new[] { pOil1, pOil2 }, CreateC152Profile(), new[] { "piston" });

            // ID 999 não existe na tabela de candidatas -> usa fallback de menor saúde do grupo ComponentKind.Oil
            int broken = tracker.ApplyNativeFailed(999, ComponentKind.Oil);
            Assert.Equal(1, broken);
            Assert.True(pOil2.IsBroken);
            Assert.Equal(0.0, pOil2.Health);
            Assert.False(pOil1.IsBroken);
            Assert.Equal(80.0, pOil1.Health);
        }

        // 6. DefectPartLink_PaneNativaComPecaQuebrada_IncluiPecaECustoZero
        [Fact]
        public void DefectPartLink_PaneNativaComPecaQuebrada_IncluiPecaECustoZero()
        {
            var valvesPart = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 0.0, IsBroken = true };
            var defect = new AircraftDefect
            {
                Id = Guid.NewGuid(),
                Code = "ENG_FAIL",
                Title = "Falha do Motor",
                Cause = NativeWearFailureMap.CauseFor(39, "PISTON ENGINE"),
                RepairCost = 8000.0,
                IsResolved = false
            };

            var quote = DefectPartLink.Quote(new[] { defect }, new[] { valvesPart });

            Assert.Single(quote.PartsToReplace);
            Assert.Equal(valvesPart.Id, quote.PartsToReplace[0].Id);
            Assert.Single(quote.DefectsIncluded);
            Assert.Equal(defect.Id, quote.DefectsIncluded[0].Id);
            Assert.Empty(quote.DefectsCharged);
            Assert.Equal(0.0, quote.DefectsCost);
        }

        // 7. DefectPartLink_PaneDoAddon_CobraRepairCost
        [Fact]
        public void DefectPartLink_PaneDoAddon_CobraRepairCost()
        {
            var defect = new AircraftDefect
            {
                Id = Guid.NewGuid(),
                Code = "RADIO_FAIL",
                Title = "Falha no Rádio",
                Cause = "Pane gerada pelo addon por vibração",
                RepairCost = 900.0,
                IsResolved = false
            };

            var quote = DefectPartLink.Quote(new[] { defect }, Array.Empty<AircraftPart>());

            Assert.Empty(quote.PartsToReplace);
            Assert.Empty(quote.DefectsIncluded);
            Assert.Single(quote.DefectsCharged);
            Assert.Equal(defect.Id, quote.DefectsCharged[0].Id);
            Assert.Equal(900.0, quote.DefectsCost);
        }

        // 8. DefectPartLink_DuasPanesMesmaPeca_PecaUmaVez
        [Fact]
        public void DefectPartLink_DuasPanesMesmaPeca_PecaUmaVez()
        {
            var tirePart = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 0.0, IsBroken = true };
            var d1 = new AircraftDefect
            {
                Id = Guid.NewGuid(),
                Code = "TIRE_BLOWOUT",
                Cause = NativeWearFailureMap.CauseFor(37, "TIRE"),
                RepairCost = 500.0,
                IsResolved = false
            };
            var d2 = new AircraftDefect
            {
                Id = Guid.NewGuid(),
                Code = "TIRE_FLAT",
                Cause = NativeWearFailureMap.CauseFor(48, "TIRE"),
                RepairCost = 400.0,
                IsResolved = false
            };

            var quote = DefectPartLink.Quote(new[] { d1, d2 }, new[] { tirePart });

            Assert.Single(quote.PartsToReplace);
            Assert.Equal(tirePart.Id, quote.PartsToReplace[0].Id);
            Assert.Equal(2, quote.DefectsIncluded.Count);
            Assert.Empty(quote.DefectsCharged);
            Assert.Equal(0.0, quote.DefectsCost);
        }

        // 9. DefectPartLink_ResolvedBy_SoComTodasAsPecasTrocadas
        [Fact]
        public void DefectPartLink_ResolvedBy_SoComTodasAsPecasTrocadas()
        {
            var valves = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 0.0, IsBroken = true };
            var cyl = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 90.0, IsBroken = false };
            var parts = new[] { valves, cyl };

            var defectNative = new AircraftDefect
            {
                Id = Guid.NewGuid(),
                Code = "ENG_FAIL",
                Cause = NativeWearFailureMap.CauseFor(39, "PISTON ENGINE"),
                IsResolved = false
            };

            var defectAddon = new AircraftDefect
            {
                Id = Guid.NewGuid(),
                Code = "CUSTOM_FAIL",
                Cause = "Causa customizada",
                IsResolved = false
            };

            var openDefects = new[] { defectNative, defectAddon };

            // Caso 1: trocou somente cilindros (que nem estavam quebrados) -> native não resolve
            var res1 = DefectPartLink.ResolvedBy(openDefects, parts, new HashSet<Guid> { cyl.Id });
            Assert.Empty(res1);

            // Caso 2: trocou válvulas (a única peça quebrada vinculada) -> native resolve!
            var res2 = DefectPartLink.ResolvedBy(openDefects, parts, new HashSet<Guid> { valves.Id });
            Assert.Single(res2);
            Assert.Equal(defectNative.Id, res2[0].Id);

            // Pane do addon nunca é resolvida automaticamente por troca de peças
            Assert.DoesNotContain(defectAddon, res2);
        }

        // 10. PartAirworthiness_PecaQuebradaQueBloqueia_Interdita
        [Fact]
        public void PartAirworthiness_PecaQuebradaQueBloqueia_Interdita()
        {
            var catalog = PartsCatalog;

            var pValves = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", Health = 0.0, IsBroken = true };
            var pLandingLight = new AircraftPart { Id = Guid.NewGuid(), PartId = "lights.landing", Health = 0.0, IsBroken = true };
            var pCylZeroHealth = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 0.0, IsBroken = false };

            var defValves = catalog.Get(pValves.PartId);
            var defLight = catalog.Get(pLandingLight.PartId);
            Assert.NotNull(defValves);
            Assert.NotNull(defLight);
            Assert.True(defValves.BlocksFlightWhenBroken);
            Assert.False(defLight.BlocksFlightWhenBroken);

            var grounding1 = PartAirworthiness.GroundingParts(new[] { pValves }, catalog);
            Assert.Single(grounding1);
            Assert.Equal(pValves.Id, grounding1[0].Id);

            var grounding2 = PartAirworthiness.GroundingParts(new[] { pLandingLight }, catalog);
            Assert.Empty(grounding2);

            var grounding3 = PartAirworthiness.GroundingParts(new[] { pCylZeroHealth }, catalog);
            Assert.Empty(grounding3);
        }
    }
}
