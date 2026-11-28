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
            var places = PlacesDataService.FromData(null, null, Array.Empty<TouristPoiInfo>(),
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
        public void MatchTag_Exclamacao_CasaSoAEtiquetaExata()
        {
            Assert.True(FlightCareer.Domain.Missions.AirportProfileCatalog.MatchTag("militar!", "militar"));
            Assert.False(FlightCareer.Domain.Missions.AirportProfileCatalog.MatchTag("militar!", "militar:manutencao"));
            Assert.True(FlightCareer.Domain.Missions.AirportProfileCatalog.MatchTag("militar", "militar:manutencao"));
        }

        [Fact]
        public void MilitarCan_AeroportoMisto_PesaMenosQueBasePura()
        {
            var def = FlightCareer.Domain.Missions.MissionCatalog.Shared.Get("militar_can")!;
            var misto = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "militar_misto", "militar:manutencao", "militar:medico" };
            var pura = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "militar", "militar:caca" };
            double wMisto = FlightCareer.Domain.Missions.MissionWeighting.OriginWeight(def, 2, misto);
            double wPura = FlightCareer.Domain.Missions.MissionWeighting.OriginWeight(def, 2, pura);
            Assert.Equal(def.BaseByTier["2"] * 2, wMisto);
            Assert.Equal(def.BaseByTier["2"] * 6, wPura);
        }

        [Fact]
        public void MilitarCan_OrigemCivil_NaoSorteia()
        {
            var def = FlightCareer.Domain.Missions.MissionCatalog.Shared.Get("militar_can")!;
            var civil = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fazenda", "isolado" };
            var semEtiqueta = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(0.0, FlightCareer.Domain.Missions.MissionWeighting.OriginWeight(def, 4, civil));
            Assert.Equal(0.0, FlightCareer.Domain.Missions.MissionWeighting.OriginWeight(def, 3, semEtiqueta));
        }

        [Fact]
        public void NavegacaoVfr_TemDestinosNaFaixa()
        {
            var def = FlightCareer.Domain.Missions.MissionCatalog.Shared.Get("navegacao_vfr")!;
            var p = def.Profiles[0];
            double max = Math.Min(p.RangeNm[1], def.Destination?.MaxNm ?? double.MaxValue);
            Assert.True(max - p.RangeNm[0] >= 30, $"faixa útil de distância muito curta: {p.RangeNm[0]}–{max} NM");
        }

        [Fact]
        public void WithPersonLuggage_InstrucaoGanhaMochilaDoAlunoEInstrutor()
        {
            var def = FlightCareer.Domain.Missions.MissionCatalog.Shared.Get("instrucao_local")!;
            var m = new Mission { PassengerCount = 1, LuggageWeightLbs = 0, PayloadLbs = 170 };
            MissionBoardCore.WithPersonLuggage(m, def, new Random(3));
            Assert.InRange(m.LuggageWeightLbs, Math.Round(4 * 2.20462), Math.Round(12 * 2.20462));
            Assert.Equal(170 + m.LuggageWeightLbs, m.PayloadLbs);
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
