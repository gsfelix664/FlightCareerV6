using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    /// <summary>
    /// Peças de vida em ciclos (pneus, freios, rodas) envelhecem a cada pouso no jogo, como no simulador de carreira.
    /// Achado do voo de SNEJ (2026-11-21): os pneus ficavam com 0 ciclos e sem desgaste natural por pouso.
    /// </summary>
    public class LandingCycleWearTests
    {
        private static readonly PartCatalog Parts = PartCatalog.LoadDefault();
        private static readonly WearRuleCatalog Rules = WearRuleCatalog.LoadDefault();

        private static AircraftPart Part(string id, double health = 80.0) =>
            new() { Id = Guid.NewGuid(), PartId = id, Health = health };

        [Fact]
        public void Pouso_PneuPrincipal_ContaCicloEGastaSetentaPorVida()
        {
            var engine = new PartWearEngine(Parts, Rules);
            var tire = Part("tires.main", 80.0);
            int life = Parts.Get("tires.main")!.LifeCycles!.Value;

            engine.ApplyLandingCycle(new List<AircraftPart> { tire }, Array.Empty<string>());

            Assert.Equal(1, tire.CyclesSinceReplacement);
            Assert.Equal(80.0 - 70.0 / life, tire.Health, 6);
        }

        [Fact]
        public void Pouso_PecaDaRegraDeCiclo_ContaUmCicloSo()
        {
            var engine = new PartWearEngine(Parts, Rules);
            var trunnion = Part("gear.trunnions_side_stays");

            engine.ApplyLandingCycle(new List<AircraftPart> { trunnion }, Array.Empty<string>());

            Assert.Equal(1, trunnion.CyclesSinceReplacement);
        }

        [Fact]
        public void Pouso_PecaDeVidaEmHoras_NaoMuda()
        {
            var engine = new PartWearEngine(Parts, Rules);
            var cylinders = Part("engine.cylinders", 90.0);

            engine.ApplyLandingCycle(new List<AircraftPart> { cylinders }, Array.Empty<string>());

            Assert.Equal(0, cylinders.CyclesSinceReplacement);
            Assert.Equal(90.0, cylinders.Health, 6);
        }

        [Fact]
        public void Pouso_PecaQuebrada_Ignorada()
        {
            var engine = new PartWearEngine(Parts, Rules);
            var tire = Part("tires.main", 0.0);
            tire.IsBroken = true;

            engine.ApplyLandingCycle(new List<AircraftPart> { tire }, Array.Empty<string>());

            Assert.Equal(0, tire.CyclesSinceReplacement);
        }

        [Fact]
        public void Pouso_PneuNaFaixaAmarela_GastaMais()
        {
            var engine = new PartWearEngine(Parts, Rules);
            var green = Part("tires.main", 80.0);
            var yellow = Part("tires.main", 25.0);

            engine.ApplyLandingCycle(new List<AircraftPart> { green, yellow }, Array.Empty<string>());

            Assert.True(25.0 - yellow.Health > 80.0 - green.Health);
        }
    }
}
