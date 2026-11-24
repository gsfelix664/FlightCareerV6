using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    /// <summary>Correções da revisão E2A: etiquetas da origem e limite de candidatos do caminho do jogo.</summary>
    public class MissionBoardReviewTests
    {
        private static readonly IReadOnlySet<string> NoTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static ScenicAerodromeInfo Scenic(string icao, string role) =>
            new(icao, icao, "BR", "nota", new List<ScenicPoint> { new("A", -22, -43), new("B", -22.1, -43.1) }, role);

        [Fact]
        public void AugmentOriginTags_TuristicoDestino_NaoViraBasePanoramica()
        {
            var places = new PlacesDataService(null, null, Array.Empty<TouristPoiInfo>(),
                new[] { Scenic("SBBASE", "panoramico"), Scenic("SBDEST", "destino") });

            var baseTags = MissionBoardCore.AugmentOriginTags(new Airport { ICAO = "SBBASE" }, NoTags, places);
            var destTags = MissionBoardCore.AugmentOriginTags(new Airport { ICAO = "SBDEST" }, NoTags, places);

            Assert.Contains("turistico_curado:base", baseTags);
            Assert.DoesNotContain("turistico_curado:base", destTags);
        }

        [Fact]
        public void LimitCandidates_MantemTodosOsProximos_ESorteiaOsDistantes()
        {
            var origin = new Airport { ICAO = "ORIG", Latitude = 0, Longitude = 0 };
            var airports = new List<Airport>();
            // 500 próximos (até ~300 NM) e 5.000 distantes (> 1.200 NM)
            for (int i = 0; i < 500; i++)
                airports.Add(new Airport { ICAO = $"N{i:D4}", Latitude = (i % 50) * 0.1, Longitude = (i / 50) * 0.5 });
            for (int i = 0; i < 5000; i++)
                airports.Add(new Airport { ICAO = $"F{i:D4}", Latitude = 20 + (i % 100) * 0.3, Longitude = 20 + (i / 100) * 0.3 });

            var result = MissionGenerator.LimitCandidates(origin, airports, new Random(7));

            Assert.Equal(MissionGenerator.MaxCandidates, result.Count);
            Assert.Equal(500, result.Count(a => a.ICAO.StartsWith("N")));
            Assert.Equal(MissionGenerator.MaxCandidates - 500, result.Count(a => a.ICAO.StartsWith("F")));

            // mesma semente → mesmo conjunto, mesmo com a lista de entrada embaralhada
            var shuffled = airports.OrderByDescending(a => a.ICAO).ToList();
            var again = MissionGenerator.LimitCandidates(origin, shuffled, new Random(7));
            Assert.Equal(result.Select(a => a.ICAO).OrderBy(x => x), again.Select(a => a.ICAO).OrderBy(x => x));
        }

        [Fact]
        public void LimitCandidates_AbaixoDoLimite_DevolveTodos()
        {
            var origin = new Airport { ICAO = "ORIG" };
            var airports = Enumerable.Range(0, 100).Select(i => new Airport { ICAO = $"A{i}", Latitude = i * 0.1 }).ToList();
            Assert.Same(airports, MissionGenerator.LimitCandidates(origin, airports, new Random(1)));
        }
    }
}
