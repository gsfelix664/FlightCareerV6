using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Tests.Components
{
    internal static class TestData
    {
        private static ComponentCatalog? _catalog;

        public static ComponentCatalog Catalog => _catalog ??= ComponentCatalog.LoadFromFile(
            Path.Combine(AppContext.BaseDirectory, "Data", "component_catalog.json"));

        public static List<AircraftComponent> NewComponents(EngineClass cls, double health = 100.0)
        {
            return Catalog.For(cls).Select(d => new AircraftComponent
            {
                Id = Guid.NewGuid(),
                Kind = d.Kind,
                Health = health
            }).ToList();
        }

        public static AircraftComponent Get(this List<AircraftComponent> list, ComponentKind kind) =>
            list.Single(c => c.Kind == kind);

        /// <summary>Cruzeiro de um monomotor a pistão totalmente dentro dos parâmetros (faixa verde).</summary>
        public static SimTelemetry PistonCruise() => new()
        {
            EngineRunning = 1.0,
            OnGround = 0.0,
            EngineType = 0.0,
            EngineRPM = 2400,
            EngineCHT = 200,
            EngineOilTemp = 90,
            EngineOilPressure = 60,
            ThrottlePercent = 70,
            MixturePercent = 70,
            GForce = 1.0,
            GroundSpeed = 110,
            Airspeed = 110,
            Altitude = 5500
        };

        /// <summary>Cruzeiro de turboélice dentro dos parâmetros.</summary>
        public static SimTelemetry TurbopropCruise() => new()
        {
            EngineRunning = 1.0,
            OnGround = 0.0,
            EngineType = 5.0,
            EngineN1 = 95,
            TurboITT = 700,
            TurboTorque = 85,
            EngineOilTemp = 80,
            EngineOilPressure = 100,
            ThrottlePercent = 80,
            GForce = 1.0,
            GroundSpeed = 250,
            Airspeed = 220,
            Altitude = 20000
        };

        /// <summary>Fonte aleatória que sempre retorna o mesmo valor (para forçar ou impedir sorteios).</summary>
        public sealed class FixedRandom : IRandomSource
        {
            private readonly double _value;
            public FixedRandom(double value) => _value = value;
            public double NextDouble() => _value;
        }
    }
}
