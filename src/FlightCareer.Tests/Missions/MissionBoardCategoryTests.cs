using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MissionBoardCategoryTests
    {
        private static IEnumerable<string> AllSubIds() =>
            MissionRules.Rules.Values.SelectMany(t => t.Categories).SelectMany(c => c.SubCategories).Select(s => s.Id).Distinct();

        [Theory]
        [InlineData("instrucao_local", MissionBoardKind.Instrucao)]
        [InlineData("navegacao_cross_country", MissionBoardKind.Instrucao)]
        [InlineData("voo_de_cheque", MissionBoardKind.Instrucao)]
        [InlineData("circuito_tgl", MissionBoardKind.Instrucao)]
        [InlineData("voo_panoramico", MissionBoardKind.TaxiAereo)]
        [InlineData("aerolevantamento", MissionBoardKind.Sae)]
        [InlineData("paraquedismo", MissionBoardKind.Sae)]
        [InlineData("linha_domestica_hub", MissionBoardKind.LinhaRegular)]
        [InlineData("vip_internacional_curto", MissionBoardKind.TaxiAereo)]
        [InlineData("cargueiro_noturno", MissionBoardKind.Carga)]
        [InlineData("medevac_dinamico", MissionBoardKind.Medica)]
        [InlineData("translado_basico", MissionBoardKind.Geral)]
        [InlineData("3.4.1", MissionBoardKind.Instrucao)]
        [InlineData("3.5.1", MissionBoardKind.TaxiAereo)]
        [InlineData("3.6.1", MissionBoardKind.Sae)]
        [InlineData("1.1.1", MissionBoardKind.LinhaRegular)]
        [InlineData("1.7.3", MissionBoardKind.Medica)]
        [InlineData("3.5.3", MissionBoardKind.Geral)]
        public void Subtipo_vai_para_a_categoria_do_quadro(string subId, MissionBoardKind expected)
        {
            // O texto legado agora é o nome da fase e não pode influenciar a classificação.
            Assert.Equal(expected, MissionBoardCategory.Classify(subId, "Escola"));
        }

        [Fact]
        public void Todo_subtipo_do_gerador_tem_categoria_ou_esta_na_lista_de_geral()
        {
            var geralIntencional = new HashSet<string> { "translado_posicionamento_ferry", "governo_local_estadual", "translado_basico", "apoio_batedor", "translado_piloto_agricola" };
            var semCategoria = AllSubIds()
                .Where(id => MissionBoardCategory.Classify(id) == MissionBoardKind.Geral && !geralIntencional.Contains(id))
                .ToList();
            Assert.Empty(semCategoria);
        }

        [Fact]
        public void Filtro_instrucao_mostra_as_missoes_de_instrucao_do_gerador()
        {
            var instrucao = AllSubIds().Where(id => MissionBoardCategory.MatchesFilter(MissionBoardCategory.Classify(id), "INSTRUÇÃO")).ToList();
            Assert.Contains("instrucao_local", instrucao);
            Assert.Contains("circuito_tgl", instrucao);
        }

        [Theory]
        [InlineData("Translado / Ferry", MissionBoardKind.Geral)]
        [InlineData("Treinamento", MissionBoardKind.Instrucao)]
        [InlineData("Táxi Aéreo / Charter", MissionBoardKind.TaxiAereo)]
        public void Missao_sem_subtipo_usa_o_texto_legado(string legacy, MissionBoardKind expected)
        {
            Assert.Equal(expected, MissionBoardCategory.Classify(null, legacy));
        }

        [Fact]
        public void Filtro_carga_sae_junta_carga_e_sae()
        {
            Assert.True(MissionBoardCategory.MatchesFilter(MissionBoardKind.Carga, "CARGA / SAE"));
            Assert.True(MissionBoardCategory.MatchesFilter(MissionBoardKind.Sae, "CARGA / SAE"));
            Assert.False(MissionBoardCategory.MatchesFilter(MissionBoardKind.Instrucao, "CARGA / SAE"));
            Assert.True(MissionBoardCategory.MatchesFilter(MissionBoardKind.Geral, "TODOS"));
        }
    }
}
