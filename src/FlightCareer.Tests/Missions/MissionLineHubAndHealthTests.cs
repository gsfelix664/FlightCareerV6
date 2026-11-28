using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    /// <summary>
    /// Decisões do usuário de 2026-11-24: jato de linha (120+ lugares) só sai de/para aeroporto com "linha"
    /// curada ou "linha_auto" de tier 1 (ou tier 2 com 100+ pontos, decisão de 2026-11-25); missões de saúde preferem fortemente o hospital de referência;
    /// medevac remoto leva o paciente da pista remota para a cidade (tier 2–3).
    /// </summary>
    public class MissionLineHubAndHealthTests
    {
        private static readonly string[] LineJetMissions = { "ponte_aerea", "troncal_domestico", "regional_internacional", "longo_curso" };

        private static IReadOnlySet<string> Tags(params string[] t) => new HashSet<string>(t, StringComparer.OrdinalIgnoreCase);

        [Theory]
        [InlineData(1, 0, true)]
        [InlineData(2, 99, false)]
        [InlineData(2, 100, true)]
        [InlineData(3, 110, false)]
        [InlineData(4, 0, false)]
        public void WithDerivedTags_LinhaAuto_ViraHubPorTierEPontuacao(int tier, int score, bool esperaHub)
        {
            var airport = new Airport { ICAO = "TEST", Tier = tier, TierScore = score };
            var tags = MissionBoardCore.WithDerivedTags(airport, Tags("linha_auto"));
            Assert.Equal(esperaHub, tags.Contains(MissionBoardCore.LinhaAutoHubTag));
        }

        [Fact]
        public void WithDerivedTags_SemLinhaAuto_NaoCriaHub()
        {
            var airport = new Airport { ICAO = "TEST", Tier = 1 };
            var tags = MissionBoardCore.WithDerivedTags(airport, Tags("linha", "carga"));
            Assert.DoesNotContain(MissionBoardCore.LinhaAutoHubTag, tags);
        }

        [Theory]
        [InlineData("ponte_aerea")]
        [InlineData("troncal_domestico")]
        [InlineData("regional_internacional")]
        [InlineData("longo_curso")]
        public void JatoDeLinha_PesoNaOrigem_CuradaOuHubAutomatico(string id)
        {
            var def = MissionCatalog.Shared.Get(id);
            Assert.NotNull(def);
            Assert.All(def!.Profiles, p => Assert.True(p.Pax.Length > 1 && p.Pax[0] >= 120, $"{id}: perfil abaixo de 120 pax"));

            double Weight(int tier, params string[] tags)
            {
                var origin = new Airport { ICAO = "TEST", Tier = tier, TierScore = MissionBoardCore.LinhaAutoHubTier2MinScore };
                return MissionWeighting.OriginWeight(def, tier, MissionBoardCore.AugmentOriginTags(origin, Tags(tags), null));
            }

            int tierBase = id == "longo_curso" ? 1 : 2;
            Assert.True(Weight(tierBase, "linha") > 0, $"{id}: linha curada deve gerar");
            Assert.True(Weight(tierBase, "linha_auto") > 0, $"{id}: linha_auto tier {tierBase} (hub) deve gerar");
            Assert.Equal(0.0, Weight(3, "linha_auto"));
        }

        [Fact]
        public void JatoDeLinha_DestinoExigeLinhaOuHubAutomatico()
        {
            foreach (var id in LineJetMissions)
            {
                var req = MissionCatalog.Shared.Get(id)!.Destination.RequireAnyTag;
                Assert.NotNull(req);
                Assert.Contains("linha", req!);
                Assert.Contains(MissionBoardCore.LinhaAutoHubTag, req!);
                Assert.DoesNotContain("linha_auto", req!);
            }
        }

        [Theory]
        [InlineData("orgaos")]
        [InlineData("uti_longa_distancia")]
        [InlineData("medevac_remoto")]
        public void Saude_DestinoHospitalDeReferencia_TemPesoForte(string id)
        {
            var profiles = MissionCatalog.Shared.Get(id)!.Destination.Profiles;
            Assert.True(profiles.TryGetValue("saude:hospital_referencia", out var w) && w >= 100,
                $"{id}: peso do hospital de referência deve ser >= 100");
        }

        [Fact]
        public void MedevacRemoto_DestinoECidadeTier2ou3_QualquerPista()
        {
            var dest = MissionCatalog.Shared.Get("medevac_remoto")!.Destination;
            Assert.Equal(new[] { 2, 3 }, dest.Tiers!.OrderBy(t => t).ToArray());
            Assert.Equal("any", dest.Runway);
        }
    }
}
