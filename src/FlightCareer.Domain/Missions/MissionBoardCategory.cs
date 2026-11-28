using System;

namespace FlightCareer.Domain.Missions
{
    /// <summary>Categoria da missão no quadro de contratos (filtros e etiqueta).</summary>
    public enum MissionBoardKind
    {
        Geral,
        Instrucao,
        TaxiAereo,
        LinhaRegional,
        LinhaRegular,
        Carga,
        Sae,
        Medica
    }

    /// <summary>
    /// Classifica a missão pelo subtipo do gerador (<c>Mission.SubId</c>). Desde as fases de carreira,
    /// <c>MissionCategoryType</c> guarda o nome da fase e não serve mais para filtrar; ele só é usado
    /// como reserva para missões sem subtipo (banco antigo, ferry manual).
    /// </summary>
    public static class MissionBoardCategory
    {
        public static MissionBoardKind Classify(string? subId, string? legacyType = null)
        {
            if (!string.IsNullOrWhiteSpace(subId))
                return FromSubId(subId.Trim());
            return FromLegacyText(legacyType);
        }

        private static MissionBoardKind FromSubId(string id)
        {
            var def = MissionCatalog.Shared.Get(id);
            if (def != null)
                return def.Board;

            return MissionBoardKind.Geral;
        }

        private static MissionBoardKind FromLegacyText(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return MissionBoardKind.Geral;
            bool Has(string s) => t.Contains(s, StringComparison.OrdinalIgnoreCase);
            if (Has("Instrução") || Has("Treinamento") || Has("Instrucao")) return MissionBoardKind.Instrucao;
            if (Has("Táxi") || Has("Taxi") || Has("Charter")) return MissionBoardKind.TaxiAereo;
            if (Has("Linha Regional")) return MissionBoardKind.LinhaRegional;
            if (Has("Linha Regular") || Has("Heavy")) return MissionBoardKind.LinhaRegular;
            if (Has("SAE")) return MissionBoardKind.Sae;
            if (Has("Carga")) return MissionBoardKind.Carga;
            if (Has("Saúde") || Has("Remoção") || Has("Médica")) return MissionBoardKind.Medica;
            return MissionBoardKind.Geral;
        }

        /// <summary>Etiqueta curta do quadro.</summary>
        public static string ShortTag(MissionBoardKind kind) => kind switch
        {
            MissionBoardKind.Instrucao => "INSTRUÇÃO",
            MissionBoardKind.TaxiAereo => "TÁXI AÉREO",
            MissionBoardKind.LinhaRegional => "REGIONAL",
            MissionBoardKind.LinhaRegular => "REGULAR",
            MissionBoardKind.Carga => "CARGA",
            MissionBoardKind.Sae => "SAE",
            MissionBoardKind.Medica => "MÉDICA",
            _ => "GERAL"
        };

        /// <summary>Se a missão entra no botão de filtro do quadro (texto do <c>CommandParameter</c>).</summary>
        public static bool MatchesFilter(MissionBoardKind kind, string? filter) => filter switch
        {
            null or "" or "TODOS" => true,
            "TÁXI AÉREO" => kind == MissionBoardKind.TaxiAereo,
            "LINHA REGIONAL" => kind == MissionBoardKind.LinhaRegional,
            "LINHA REGULAR" => kind == MissionBoardKind.LinhaRegular,
            "CARGA / SAE" => kind is MissionBoardKind.Carga or MissionBoardKind.Sae,
            "INSTRUÇÃO" => kind == MissionBoardKind.Instrucao,
            "MÉDICA / ESPECIAL" => kind == MissionBoardKind.Medica,
            _ => true
        };
    }
}
