using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Grupos da UI para organização das peças no Hangar e na manutenção.
    /// </summary>
    public static class PartGroup
    {
        public const string Motor = "Motor";
        public const string Oleo = "Óleo";
        public const string Ignicao = "Ignição";
        public const string SecaoQuente = "Seção quente";
        public const string Helice = "Hélice";
        public const string Eletrico = "Elétrico";
        public const string Instrumentos = "Instrumentos";
        public const string Combustivel = "Combustível";
        public const string Trem = "Trem";
        public const string Pneus = "Pneus";
        public const string Freios = "Freios";
        public const string Celula = "Célula";
        public const string Hidraulico = "Hidráulico";
        public const string Pressurizacao = "Pressurização";
        public const string Empenagem = "Empenagem";
        public const string Asa = "Asa";
        public const string Fuselagem = "Fuselagem";
        public const string Avionicos = "Aviônicos";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Motor,
            Oleo,
            Ignicao,
            SecaoQuente,
            Helice,
            Eletrico,
            Instrumentos,
            Combustivel,
            Trem,
            Pneus,
            Freios,
            Celula,
            Hidraulico,
            Pressurizacao,
            Empenagem,
            Asa,
            Fuselagem,
            Avionicos
        };
    }
}
