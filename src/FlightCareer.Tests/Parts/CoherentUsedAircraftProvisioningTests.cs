using System;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class CoherentUsedAircraftProvisioningTests
    {
        private static PartCatalog CreateCatalog() => PartCatalog.LoadDefault();

        [Fact]
        public void ProvisionPurchasedAircraft_UsedPlane_EnginePartsLimitedByTsmo_AndKnownHealthMatchesHealth()
        {
            var catalog = CreateCatalog();
            var migrator = new PartMigrationService(catalog);

            // Cenário do achado real: PP-UZJ com 7.221 h de célula e motor recém-revisado (TSMO = 0.8 h)
            var plane = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                Prefix = "PP-UZJ",
                CurrentAirframeHours = 7221.0,
                LandingsCount = 7221,
                EngineCondition = 55.0,     // Saúde ~55%
                AirframeCondition = 55.0,
                TiresCondition = 55.0,
                AvionicsCondition = 55.0
            };

            migrator.ProvisionPurchasedAircraft(
                aircraft: plane,
                features: new[] { "piston", "tricycleGear", "flaps" },
                engineCount: 1,
                engineCondition: 55.0,
                tiresCondition: 55.0,
                airframeCondition: 55.0,
                avionicsCondition: 55.0,
                tsmoHours: 0.8
            );

            Assert.NotEmpty(plane.Parts);

            // 1. Todas as peças devem ter KnownHealth preenchido e igual à Health (comprador fez pré-compra)
            Assert.All(plane.Parts, p =>
            {
                Assert.NotNull(p.KnownHealth);
                Assert.Equal(p.Health, p.KnownHealth.Value);
            });

            // 2. Peças do motor devem ser limitadas pelo TSMO (<= 0.8 h)
            var mapping = PartComponentMapping.CreateDefault();
            var engineParts = plane.Parts.Where(p =>
            {
                var def = catalog.Get(p.PartId);
                if (def == null) return false;
                var kind = mapping.ResolveComponentKind(def.Group);
                return kind is ComponentKind.Engine or ComponentKind.HotSection or ComponentKind.Oil or ComponentKind.Ignition;
            }).ToList();

            Assert.NotEmpty(engineParts);
            Assert.All(engineParts, ep =>
            {
                Assert.True(ep.HoursSinceReplacement <= 0.8,
                    $"Peça do motor '{ep.PartId}' tem {ep.HoursSinceReplacement}h, maior que o TSMO de 0.8h.");
            });

            // 3. Peças fora do motor (ex: célula, superfícies de comando) com vida útil de 2.000 h e saúde ~55%
            // Cálculo: (100 - 55) / 30 * 2000 = 3000 h (limitado pelas horas de célula 7.221 h)
            var airframePart = plane.Parts.FirstOrDefault(p =>
            {
                var def = catalog.Get(p.PartId);
                return def != null && def.LifeHours >= 2000 && mapping.ResolveComponentKind(def.Group) == ComponentKind.Airframe;
            });

            if (airframePart != null)
            {
                // Deve ter envelhecido conforme a saúde e não estar travado em 0 ou 0.8h
                Assert.True(airframePart.HoursSinceReplacement > 100.0,
                    $"Peça estrutural '{airframePart.PartId}' deveria ter horas acumuladas, mas tem {airframePart.HoursSinceReplacement}h.");
                Assert.True(airframePart.HoursSinceReplacement <= plane.CurrentAirframeHours,
                    $"Peça estrutural '{airframePart.PartId}' não pode exceder as horas de célula.");
            }
        }
    }
}
