using System;

namespace FlightCareer.Domain.Entities
{
    /// <summary>
    /// Origem da pane: sorteada/injetada pelo addon ou gerada pelo próprio simulador e detectada por SimVar.
    /// </summary>
    public enum DefectOrigin
    {
        Addon = 0,
        Native = 1
    }

    /// <summary>
    /// Defeito persistente de uma aeronave. Só sai pela oficina (IsResolved = true no Hangar).
    /// </summary>
    public class AircraftDefect
    {
        public Guid Id { get; set; }
        public Guid PilotAircraftId { get; set; }

        /// <summary>Código do catálogo (ver DefectCatalog), ex.: MAGNETO_LEFT, FLAP_LOCK.</summary>
        public string Code { get; set; } = string.Empty;
        public DefectOrigin Origin { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
        public string FlightPhase { get; set; } = string.Empty;

        /// <summary>Item inoperante que impede iniciar missões (NO-GO).</summary>
        public bool IsGrounding { get; set; }
        public double RepairCost { get; set; }

        /// <summary>Parâmetro específico da pane (ex.: índice em que o flap travou, % de dano do motor).</summary>
        public double Parameter { get; set; }

        public DateTime DetectedAtUtc { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }

        public string OriginLabel => Origin == DefectOrigin.Native ? "SIMULADOR" : "ADDON";
        public string SeverityLabel => IsGrounding ? "NO-GO" : "DIFERIDA";
    }
}
