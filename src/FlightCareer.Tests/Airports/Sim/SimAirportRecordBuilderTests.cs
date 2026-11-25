using System;
using System.Collections.Generic;
using FlightCareer.Domain.Airports;
using FlightCareer.Domain.Airports.Sim;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Airports.Sim
{
    public class SimAirportRecordBuilderTests
    {
        private static readonly AirportTierRules s_rules = AirportTierRules.Default;
        private static IReadOnlySet<string> EmptyTags(string _) => new HashSet<string>();

        [Fact]
        public void Builder_FiltraAguaECurta()
        {
            // Pista 1: água (code 27), 2000m (~6561ft) -> descarta
            // Pista 2: asfalto (code 4), 200m (~656ft) < 800ft -> descarta
            // Pista 3: asfalto (code 4), 500m (~1640ft) >= 1000ft -> mantém
            var detail = new SimAirportDetail(
                Ident: "SBTE",
                Region: "SB",
                Name: "Teste Airport",
                Latitude: -23.0,
                Longitude: -46.0,
                AltitudeM: 700,
                Approaches: 2,
                Jetways: 0,
                TowerLatitude: 0,
                TowerLongitude: 0,
                Runways: new List<SimRunwayData>
                {
                    new(LengthM: 2000, WidthM: 50, SurfaceCode: 27, "", ""),
                    new(LengthM: 200, WidthM: 20, SurfaceCode: 4, "", ""),
                    new(LengthM: 500, WidthM: 30, SurfaceCode: 4, "", "")
                });

            var result = SimAirportRecordBuilder.Build(
                detail, "fp", existing: null, existingRunways: Array.Empty<Runway>(),
                rules: s_rules, tagsFor: EmptyTags, nowUtc: DateTime.UtcNow);

            Assert.NotNull(result);
            Assert.Single(result!.Runways);
            Assert.Equal("Asphalt", result.Runways[0].Surface);
            Assert.Equal(1640, result.Runways[0].LengthFt);
        }

        [Fact]
        public void Builder_SemPista1000ft_RetornaNulo()
        {
            // Pista com 270m (~886ft): passa o filtro >= 800ft, mas maior pista < 1000ft -> retorna null
            var detail = new SimAirportDetail(
                Ident: "SBCU",
                Region: "SB",
                Name: "Curto",
                Latitude: -23.0,
                Longitude: -46.0,
                AltitudeM: 700,
                Approaches: 0,
                Jetways: 0,
                TowerLatitude: 0,
                TowerLongitude: 0,
                Runways: new List<SimRunwayData>
                {
                    new(LengthM: 270, WidthM: 20, SurfaceCode: 4, "", "")
                });

            var result = SimAirportRecordBuilder.Build(
                detail, "fp", existing: null, existingRunways: Array.Empty<Runway>(),
                rules: s_rules, tagsFor: EmptyTags, nowUtc: DateTime.UtcNow);

            Assert.Null(result);
        }

        [Fact]
        public void Builder_PreservaCidadePaisEId()
        {
            var existingId = Guid.NewGuid();
            var existing = new Airport
            {
                Id = existingId,
                ICAO = "SBSP",
                Name = "Congonhas Antigo",
                City = "São Paulo",
                Country = "BR",
                Gates = 10,
                CargoParking = 3,
                IsMilitary = false,
                CsvType = "large_airport",
                ScheduledService = true
            };

            var detail = new SimAirportDetail(
                Ident: "SBSP",
                Region: "SB",
                Name: "Congonhas Novo",
                Latitude: -23.6261,
                Longitude: -46.6564,
                AltitudeM: 802,
                Approaches: 5,
                Jetways: 12,
                TowerLatitude: -23.626,
                TowerLongitude: -46.656,
                Runways: new List<SimRunwayData>
                {
                    new(LengthM: 1940, WidthM: 45, SurfaceCode: 4, "ISB", "")
                });

            var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
            var result = SimAirportRecordBuilder.Build(
                detail, "fp-sbsp", existing: existing, existingRunways: Array.Empty<Runway>(),
                rules: s_rules, tagsFor: EmptyTags, nowUtc: now);

            Assert.NotNull(result);
            Assert.Same(existing, result!.Airport);
            Assert.Equal(existingId, result.Airport.Id);
            Assert.Equal("São Paulo", result.Airport.City);
            Assert.Equal("BR", result.Airport.Country);
            Assert.Equal("Congonhas Novo", result.Airport.Name);
            Assert.Equal(12, result.Airport.Gates); // max(10, 12)
            Assert.Equal(3, result.Airport.CargoParking);
            Assert.Equal("large_airport", result.Airport.CsvType);
            Assert.True(result.Airport.ScheduledService);
            Assert.Equal("Sim", result.Airport.Source);
            Assert.Equal("fp-sbsp", result.Airport.SimFingerprint);
            Assert.Equal(now, result.Airport.SimCheckedUtc);
        }

        [Fact]
        public void Builder_IluminacaoDaPistaExistente()
        {
            // Existente: 5000 ft, IsLighted = true
            var existingRunways = new List<Runway>
            {
                new() { Id = Guid.NewGuid(), AirportIdent = "SBSP", LengthFt = 5000, WidthFt = 100, Surface = "Asphalt", IsLighted = true }
            };

            // Pista 1 nova: 5200 ft (~1585m). Diferença: |5000 - 5200| = 200 <= 520 (10%) -> deve herdar IsLighted = true
            // Pista 2 nova: 7000 ft (~2134m). Diferença > 10% -> false
            var detail = new SimAirportDetail(
                Ident: "SBSP",
                Region: "SB",
                Name: "Teste",
                Latitude: -23.0,
                Longitude: -46.0,
                AltitudeM: 500,
                Approaches: 0,
                Jetways: 0,
                TowerLatitude: 0,
                TowerLongitude: 0,
                Runways: new List<SimRunwayData>
                {
                    new(LengthM: 1585, WidthM: 45, SurfaceCode: 4, "", ""),
                    new(LengthM: 2134, WidthM: 45, SurfaceCode: 4, "", "")
                });

            var result = SimAirportRecordBuilder.Build(
                detail, "fp", existing: null, existingRunways: existingRunways,
                rules: s_rules, tagsFor: EmptyTags, nowUtc: DateTime.UtcNow);

            Assert.NotNull(result);
            Assert.Equal(2, result!.Runways.Count);

            var rw5200 = result.Runways[0];
            var rw7000 = result.Runways[1];

            Assert.True(rw5200.IsLighted);
            Assert.False(rw7000.IsLighted);
        }

        [Fact]
        public void Builder_ContaIlsETorre()
        {
            var detail = new SimAirportDetail(
                Ident: "SBGR",
                Region: "SB",
                Name: "Guarulhos",
                Latitude: -23.4356,
                Longitude: -46.4731,
                AltitudeM: 750,
                Approaches: 8,
                Jetways: 20,
                TowerLatitude: -23.43,
                TowerLongitude: -46.47,
                Runways: new List<SimRunwayData>
                {
                    new(LengthM: 3000, WidthM: 45, SurfaceCode: 4, "IGR", "IGL"),
                    new(LengthM: 3700, WidthM: 45, SurfaceCode: 0, "IGS", "")
                });

            var result = SimAirportRecordBuilder.Build(
                detail, "fp", existing: null, existingRunways: Array.Empty<Runway>(),
                rules: s_rules, tagsFor: EmptyTags, nowUtc: DateTime.UtcNow);

            Assert.NotNull(result);
            Assert.Equal(3, result!.Airport.IlsEnds); // 2 da primeira + 1 da segunda
            Assert.True(result.Airport.HasTower);
        }

        [Fact]
        public void Builder_CalculaTier()
        {
            var detail = new SimAirportDetail(
                Ident: "SBKP",
                Region: "SB",
                Name: "Viracopos",
                Latitude: -23.0074,
                Longitude: -47.1345,
                AltitudeM: 661,
                Approaches: 6,
                Jetways: 8,
                TowerLatitude: -23.0,
                TowerLongitude: -47.0,
                Runways: new List<SimRunwayData>
                {
                    new(LengthM: 3240, WidthM: 45, SurfaceCode: 4, "IVP", "")
                });

            var existing = new Airport
            {
                Id = Guid.NewGuid(),
                ICAO = "SBKP",
                Gates = 4,
                CargoParking = 10,
                CsvType = "large_airport",
                IsMilitary = false
            };

            var expectedInput = new AirportTierInput(
                Tags: EmptyTags("SBKP"),
                IlsEnds: 1,
                Approaches: 6,
                Gates: 8, // max(4, 8)
                Jetways: 0,
                CargoParking: 10,
                HasTower: true,
                LongestRunwayFt: 10630, // 3240 * 3.28084
                LongestRunwayPaved: true,
                CsvType: "large_airport",
                ScheduledService: null,
                IsMilitary: false
            );
            var expectedScore = AirportTierClassifier.Score(expectedInput, s_rules);

            var result = SimAirportRecordBuilder.Build(
                detail, "fp", existing: existing, existingRunways: Array.Empty<Runway>(),
                rules: s_rules, tagsFor: EmptyTags, nowUtc: DateTime.UtcNow);

            Assert.NotNull(result);
            Assert.Equal(expectedScore.Tier, result!.Airport.Tier);
            Assert.Equal(expectedScore.TotalScore, result.Airport.TierScore);
            Assert.Equal(s_rules.Version, result.Airport.TierRulesVersion);
        }
    }
}
