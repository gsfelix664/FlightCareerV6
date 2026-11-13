using System;
using FlightCareer.Domain.Components;

namespace FlightCareer.Domain.Entities
{
    /// <summary>
    /// Barra de saúde individual de um componente de uma aeronave do hangar.
    /// Health é o valor REAL (0-100); o jogador só vê uma faixa até inspecionar (ver HealthVisibility).
    /// </summary>
    public class AircraftComponent
    {
        public Guid Id { get; set; }
        public Guid PilotAircraftId { get; set; }
        public PilotAircraft? PilotAircraft { get; set; }

        public ComponentKind Kind { get; set; }

        /// <summary>Saúde real do componente (0-100%).</summary>
        public double Health { get; set; } = 100.0;

        /// <summary>Horas de operação desde o último overhaul/troca.</summary>
        public double HoursSinceOverhaul { get; set; }

        /// <summary>Ciclos (pousos/partidas) desde o último overhaul/troca.</summary>
        public int CyclesSinceOverhaul { get; set; }

        /// <summary>Horas de célula no momento da última inspeção deste componente.</summary>
        public double LastInspectionAirframeHours { get; set; }

        /// <summary>Meia-largura (em %) da faixa exibida logo após a última inspeção (0 = valor exato).</summary>
        public double InspectionPrecision { get; set; }

        /// <summary>Deslocamento aleatório estável (-0.6 a 0.6) do centro da faixa exibida, sorteado a cada inspeção.</summary>
        public double InspectionBias { get; set; }
    }
}
