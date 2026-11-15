using System;

namespace FlightCareer.Domain.Entities
{
    /// <summary>
    /// Indicador visual de suspeita na peça (exclamação amarela no Hangar/Walkaround).
    /// </summary>
    public enum PartFlag
    {
        None = 0,
        Suspect = 1
    }

    /// <summary>
    /// Origem da marcação de suspeita na peça.
    /// </summary>
    public enum PartFlagSource
    {
        None = 0,
        Walkaround = 1,
        Inspection = 2
    }

    /// <summary>
    /// Peça individual instalada em uma aeronave do jogador com saúde oculta (0-100%).
    /// </summary>
    public class AircraftPart
    {
        public Guid Id { get; set; }

        public Guid PilotAircraftId { get; set; }
        public PilotAircraft? PilotAircraft { get; set; }

        /// <summary>Identificador da peça no catálogo (ex.: 'engine.cylinders').</summary>
        public string PartId { get; set; } = string.Empty;

        /// <summary>Índice do motor para peças instaladas por motor (1..N); 0 para peças da célula/gerais.</summary>
        public int EngineIndex { get; set; } = 0;

        /// <summary>Saúde oculta da peça de 0 a 100%. Nunca exibida numericamente ao piloto.</summary>
        public double Health { get; set; } = 100.0;

        /// <summary>Horas de voo acumuladas desde a última substituição/instalação.</summary>
        public double HoursSinceReplacement { get; set; }

        /// <summary>Ciclos/pousos acumulados desde a última substituição/instalação.</summary>
        public int CyclesSinceReplacement { get; set; }

        /// <summary>Data UTC da última substituição.</summary>
        public DateTime LastReplacedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Horas totais de célula da aeronave no momento da última substituição.</summary>
        public double LastReplacedAirframeHours { get; set; }

        /// <summary>Sinalização de suspeita (None ou Suspect com exclamação amarela).</summary>
        public PartFlag Flag { get; set; } = PartFlag.None;

        /// <summary>Fonte que descobriu a suspeita (Walkaround ou Revisão/Inspeção).</summary>
        public PartFlagSource FlaggedBy { get; set; } = PartFlagSource.None;

        /// <summary>Data UTC em que a peça foi marcada como suspeita.</summary>
        public DateTime? FlaggedAtUtc { get; set; }

        /// <summary>Indica se a peça falhou completamente (BROKEN / vermelha / pane).</summary>
        public bool IsBroken { get; set; }

        /// <summary>Data UTC em que a falha da peça ocorreu.</summary>
        public DateTime? BrokenAtUtc { get; set; }

        /// <summary>Data UTC do último serviço real de oficina/hangar nesta peça. Null para peça original de fábrica/migrada.</summary>
        public DateTime? LastShopServiceAtUtc { get; set; }

        /// <summary>Horas totais de célula da aeronave no momento do último serviço de oficina nesta peça.</summary>
        public double? LastShopServiceAirframeHours { get; set; }

        /// <summary>Ciclos totais da aeronave no momento do último serviço de oficina nesta peça.</summary>
        public int? LastShopServiceCycles { get; set; }
    }
}
