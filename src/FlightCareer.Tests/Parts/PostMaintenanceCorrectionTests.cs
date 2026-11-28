using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PostMaintenanceCorrectionTests
    {
        private static readonly ComponentCatalog Catalog = ComponentCatalog.LoadDefault();

        private sealed class FixedRandom : IRandomSource
        {
            private readonly double _val;
            public FixedRandom(double val = 0.0) => _val = val; // 0.0 garante que qualquer chance > 0 dispare
            public double NextDouble() => _val;
        }

        [Fact]
        public void BrandNewAircraft_WithoutShopService_NeverFiresPostMaintenance()
        {
            var random = new FixedRandom(0.0); // Força probabilidade máxima
            var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, random);

            // Componente de avião novo: 0 horas de uso, mas sem LastShopServiceAtUtc (recém-saído de fábrica)
            var alternator = new AircraftComponent
            {
                Id = Guid.NewGuid(),
                Kind = ComponentKind.Electrical,
                Health = 100.0,
                HoursSinceOverhaul = 0.0,
                CyclesSinceOverhaul = 0,
                LastShopServiceAtUtc = null // Sem serviço de oficina!
            };

            var telemetry = new SimTelemetry
            {
                OnGround = 1,
                EngineRunning = 1,
                ThrottlePercent = 50.0,
                GroundSpeed = 10.0
            };

            var events = engine.Evaluate(new[] { alternator }, telemetry, 1.0);

            // NUNCA deve disparar PostMaintenance em avião sem serviço
            Assert.DoesNotContain(events, e => e.Source == ExternalEventEngine.PostMaintenance);
        }

        [Fact]
        public void AircraftAfterShopRepair_InsideWindow_FiresPostMaintenance()
        {
            var random = new FixedRandom(0.0); // Força disparo
            var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, random);

            var alternator = new AircraftComponent
            {
                Id = Guid.NewGuid(),
                Kind = ComponentKind.Electrical,
                Health = 90.0,
                HoursSinceOverhaul = 100.0,
                CyclesSinceOverhaul = 20
            };

            // Realiza reparo no hangar (oficina)
            ComponentMaintenance.Repair(alternator, 500.0, random, Catalog.Visibility, 100.0);

            Assert.NotNull(alternator.LastShopServiceAtUtc);
            Assert.Equal(0.0, alternator.HoursSinceOverhaul);

            var telemetry = new SimTelemetry
            {
                OnGround = 1,
                EngineRunning = 1,
                ThrottlePercent = 50.0,
                GroundSpeed = 10.0
            };

            // Primeiro voo logo após a oficina: dentro da janela (0 h < WindowHours)
            var events = engine.Evaluate(new[] { alternator }, telemetry, 1.0);

            Assert.Contains(events, e => e.Source == ExternalEventEngine.PostMaintenance);
        }

        [Fact]
        public void AircraftAfterShopRepair_OutsideWindow_DoesNotFirePostMaintenance()
        {
            var random = new FixedRandom(0.0);
            var engine = new ExternalEventEngine(Catalog.ExternalEvents, EngineClass.Piston, random);

            var alternator = new AircraftComponent
            {
                Id = Guid.NewGuid(),
                Kind = ComponentKind.Electrical,
                Health = 100.0,
                HoursSinceOverhaul = 10.0, // Janela padrão costuma ser 2h
                CyclesSinceOverhaul = 5,
                LastShopServiceAtUtc = DateTime.UtcNow.AddHours(-10)
            };

            var telemetry = new SimTelemetry
            {
                OnGround = 1,
                EngineRunning = 1,
                ThrottlePercent = 50.0,
                GroundSpeed = 10.0
            };

            var events = engine.Evaluate(new[] { alternator }, telemetry, 1.0);

            // Fora da janela: não dispara pós-manutenção
            Assert.DoesNotContain(events, e => e.Source == ExternalEventEngine.PostMaintenance);
        }
    }
}
