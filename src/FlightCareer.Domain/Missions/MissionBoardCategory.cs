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
            switch (id)
            {
                // Instrução: aeroclube/escola e circuito de TGL
                case "3.5.2": return MissionBoardKind.Instrucao;
                // Passageiros sob demanda (executivo, VIP, panorâmico, equipes)
                case "1.3.1": case "1.3.4": case "1.7.1": case "1.7.4":
                case "2.7.1": case "2.7.4": case "3.5.1": case "4.1.2": case "4.1.3":
                    return MissionBoardKind.TaxiAereo;
                case "1.7.5": case "1.7.7": return MissionBoardKind.LinhaRegular;
                case "1.3.2": case "1.7.2": case "1.7.6": case "1.7.8": case "2.4.1": case "2.7.2":
                case "3.7.1": case "4.1.1": case "4.2.3": case "4.3.2":
                    return MissionBoardKind.Carga;
                case "1.7.3": case "2.7.3": case "3.7.2": case "4.3.1": return MissionBoardKind.Medica;
                // Translados/ferry e governo ficam em "geral"
                case "1.3.3": case "2.4.2": case "3.5.3": case "4.2.1": case "4.2.2":
                    return MissionBoardKind.Geral;
            }

            if (id.StartsWith("3.4.", StringComparison.Ordinal)) return MissionBoardKind.Instrucao;
            if (id.StartsWith("1.1.", StringComparison.Ordinal)) return MissionBoardKind.LinhaRegular;
            if (id.StartsWith("2.1.", StringComparison.Ordinal)) return MissionBoardKind.LinhaRegional;
            if (id.StartsWith("2.2.", StringComparison.Ordinal) || id.StartsWith("3.1.", StringComparison.Ordinal)) return MissionBoardKind.TaxiAereo;
            if (id.StartsWith("1.2.", StringComparison.Ordinal) || id.StartsWith("2.3.", StringComparison.Ordinal) || id.StartsWith("3.2.", StringComparison.Ordinal)) return MissionBoardKind.Carga;
            if (id.StartsWith("3.3.", StringComparison.Ordinal)) return MissionBoardKind.Medica;
            if (id.StartsWith("3.6.", StringComparison.Ordinal)) return MissionBoardKind.Sae;
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
