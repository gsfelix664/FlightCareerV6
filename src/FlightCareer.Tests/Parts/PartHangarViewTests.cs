using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartHangarViewTests
    {
        private readonly PartCatalog _catalog = PartCatalog.LoadDefault();

        [Fact]
        public void Build_NuncaInspecionada_UnknownETracoENuncaInspecionada()
        {
            var def = _catalog.Get("engine.cylinders")!;
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 80.0,
                KnownHealth = null,
                KnownAtAirframeHours = null,
                IsBroken = false
            };

            var row = PartHangarView.Build(part, def, engineCount: 1, airframeHours: 50.0, careerNow: DateTime.UtcNow);

            Assert.Equal(PartKnownBand.Unknown, row.Band);
            Assert.Equal("—", row.HealthText);
            Assert.Equal("nunca inspecionada", row.SeenText);
            Assert.Null(row.KnownHealth);
        }

        [Fact]
        public void Build_VistaHa3h_TextoComHoras()
        {
            var def = _catalog.Get("engine.cylinders")!;
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 62.0,
                KnownHealth = 62.0,
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };

            var row = PartHangarView.Build(part, def, engineCount: 1, airframeHours: 103.0, careerNow: DateTime.UtcNow);

            Assert.Equal(PartKnownBand.Green, row.Band);
            Assert.Equal("Boa", row.HealthText);
            Assert.Equal("visto há 3 h", row.SeenText);
        }

        [Fact]
        public void Build_VistaAgora()
        {
            var def = _catalog.Get("engine.cylinders")!;
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 70.0,
                KnownHealth = 70.0,
                KnownAtAirframeHours = 150.0,
                IsBroken = false
            };

            var row = PartHangarView.Build(part, def, engineCount: 1, airframeHours: 150.0, careerNow: DateTime.UtcNow);

            Assert.Equal("visto agora", row.SeenText);
        }

        [Fact]
        public void Build_FaixasPeloConhecidoNaoPeloReal()
        {
            var def = _catalog.Get("engine.cylinders")!;
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 15.0, // real vermelho
                KnownHealth = 55.0, // conhecido verde (>= 40%)
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };

            var row = PartHangarView.Build(part, def, engineCount: 1, airframeHours: 105.0, careerNow: DateTime.UtcNow);

            Assert.Equal(PartKnownBand.Green, row.Band);
            Assert.Equal("Boa", row.HealthText);
        }

        [Fact]
        public void Build_QuebradaVenceConhecido()
        {
            var def = _catalog.Get("engine.cylinders")!;
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 0.0,
                KnownHealth = 90.0, // conhecido alto, mas quebrada
                KnownAtAirframeHours = 100.0,
                IsBroken = true
            };

            var row = PartHangarView.Build(part, def, engineCount: 1, airframeHours: 105.0, careerNow: DateTime.UtcNow);

            Assert.Equal(PartKnownBand.Broken, row.Band);
        }

        [Fact]
        public void Build_YellowERed()
        {
            var def = _catalog.Get("engine.cylinders")!;
            var partYellow = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 30.0,
                KnownHealth = 30.0,
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };
            var partRed = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 10.0,
                KnownHealth = 10.0,
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };

            var rowYellow = PartHangarView.Build(partYellow, def, engineCount: 1, airframeHours: 100.0, careerNow: DateTime.UtcNow);
            var rowRed = PartHangarView.Build(partRed, def, engineCount: 1, airframeHours: 100.0, careerNow: DateTime.UtcNow);

            Assert.Equal(PartKnownBand.Yellow, rowYellow.Band);
            Assert.Equal(PartKnownBand.Red, rowRed.Band);
        }

        [Fact]
        public void Build_NomeComMotor()
        {
            var def = _catalog.Get("engine.cylinders")!; // PerEngine = true
            var partMotor2 = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                EngineIndex = 2
            };
            var partMotor1 = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                EngineIndex = 1
            };

            // 2 motores
            string nameBimotor = PartHangarView.DisplayName(partMotor2, def, engineCount: 2);
            Assert.Equal($"{def.Name} (motor 2)", nameBimotor);

            // 1 motor
            string nameMonomotor = PartHangarView.DisplayName(partMotor1, def, engineCount: 1);
            Assert.Equal(def.Name, nameMonomotor);
        }

        [Fact]
        public void Build_LifeNotePropagada()
        {
            var def = _catalog.Get("oil.lubricant")!;
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "oil.lubricant",
                Health = 90.0,
                KnownHealth = 90.0,
                HoursSinceReplacement = def.LifeHours + 1.0,
                IsBroken = false
            };

            var row = PartHangarView.Build(part, def, engineCount: 1, airframeHours: 100.0, careerNow: DateTime.UtcNow);

            Assert.NotNull(row.LifeNote);
            Assert.Contains("vencida", row.LifeNote);
        }

        [Fact]
        public void BuildList_OrdemPorGravidade()
        {
            var pBroken = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", IsBroken = true, KnownHealth = 50.0 };
            var pRed = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.carburetor", KnownHealth = 15.0 };
            var pYellow = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.valves", KnownHealth = 35.0 };
            var pUnknown = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.starter", KnownHealth = null };
            var pGreen = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.crankshaft", KnownHealth = 80.0 };

            var list = new List<AircraftPart> { pGreen, pUnknown, pYellow, pBroken, pRed };

            var result = PartHangarView.BuildList(list, _catalog, engineCount: 1, airframeHours: 100.0, careerNow: DateTime.UtcNow);

            Assert.Equal(5, result.Count);
            Assert.Equal(PartKnownBand.Broken, result[0].Band);
            Assert.Equal(PartKnownBand.Red, result[1].Band);
            Assert.Equal(PartKnownBand.Yellow, result[2].Band);
            Assert.Equal(PartKnownBand.Unknown, result[3].Band);
            Assert.Equal(PartKnownBand.Green, result[4].Band);
        }

        [Fact]
        public void SelectRecommendedKnown_IgnoraSaudeReal()
        {
            var pHealth5KnownNull = new AircraftPart { Id = Guid.NewGuid(), PartId = "p1", Health = 5.0, KnownHealth = null, IsBroken = false };
            var pHealth50Known10 = new AircraftPart { Id = Guid.NewGuid(), PartId = "p2", Health = 50.0, KnownHealth = 10.0, IsBroken = false };
            var pBroken = new AircraftPart { Id = Guid.NewGuid(), PartId = "p3", Health = 90.0, KnownHealth = 90.0, IsBroken = true };
            var pGood = new AircraftPart { Id = Guid.NewGuid(), PartId = "p4", Health = 90.0, KnownHealth = 90.0, IsBroken = false };

            var selected = PartReplacementPlanner.SelectRecommendedKnown(new[] { pHealth5KnownNull, pHealth50Known10, pBroken, pGood });

            Assert.Equal(2, selected.Count);
            Assert.Contains(pHealth50Known10, selected);
            Assert.Contains(pBroken, selected);
            Assert.DoesNotContain(pHealth5KnownNull, selected);
            Assert.DoesNotContain(pGood, selected);
        }

        [Fact]
        public void Plan_MotivoPeloConhecido()
        {
            var pNull = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 5.0, KnownHealth = null, IsBroken = false };
            var pYellow = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.carburetor", Health = 80.0, KnownHealth = 30.0, IsBroken = false };

            var plan = PartReplacementPlanner.Plan(new[] { pNull, pYellow }, _catalog, "A", DateTime.UtcNow);

            Assert.Equal(2, plan.Items.Count);
            Assert.Equal("Troca por escolha do piloto", plan.Items[0].Reason);
            Assert.Equal("Faixa amarela", plan.Items[1].Reason);
        }
    }
}
