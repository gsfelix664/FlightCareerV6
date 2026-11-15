using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Entities
{
    /// <summary>
    /// Perfil de limites operacionais de uma aeronave persistido no banco de dados.
    /// </summary>
    public class AircraftLimitProfileEntity
    {
        public Guid Id { get; set; }

        /// <summary>Modelo de aeronave (chave única correspondente a AircraftType.Model).</summary>
        public string AircraftTypeModel { get; set; } = string.Empty;

        /// <summary>Classe de propulsão/operação (ex.: Piston, Turboprop, Jet).</summary>
        public string Class { get; set; } = string.Empty;

        /// <summary>Versão dos dados do perfil para controle de atualização idempotente.</summary>
        public int DataVersion { get; set; }

        /// <summary>JSON com os padrões de Title e AtcModel para casamento com o simulador.</summary>
        public string SimMatchJson { get; set; } = string.Empty;

        /// <summary>JSON com a lista de características técnicas (features) da aeronave.</summary>
        public string FeaturesJson { get; set; } = "[]";

        public List<AircraftLimitEntity> Limits { get; set; } = new();
    }
}
