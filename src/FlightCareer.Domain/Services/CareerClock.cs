using System;

namespace FlightCareer.Domain.Services
{
    /// <summary>
    /// Relógio de carreira da aviação: avança exclusivamente com tempo voado (horas de célula)
    /// acrescido de tempo de solo (turnaround) e paradas de oficina (manutenções, inspeções e reparos no Hangar).
    /// Nunca avança com o simulador ou aplicativo fechados, nem pelo relógio do sistema operacional (Windows),
    /// garantindo que jogadores que passam dias ou semanas longe do simulador não sejam penalizados.
    /// </summary>
    public static class CareerClock
    {
        /// <summary>
        /// Tempo padrão de solo/turnaround após cada voo (0,5 h = 30 minutos).
        /// Cobre desembarque de passageiros/carga, inspeção pós-voo, documentação e preparo.
        /// </summary>
        public const double GroundTurnaroundHours = 0.5;

        /// <summary>
        /// Parada estimada para a troca de óleo (a cada 12 h): 2,0 h.
        /// Cobre troca de óleo e filtro, dreno e conferência de limalhas no bujão magnético.
        /// </summary>
        public const double Inspection50hDowntimeHours = 2.0;

        /// <summary>
        /// Parada estimada para a inspeção anual (a cada 25 h): 8,0 h.
        /// Cobre inspeção geral de célula, superfícies de comando, cabos, fiações, compressão e filtros.
        /// </summary>
        public const double Inspection100hDowntimeHours = 8.0;

        /// <summary>
        /// Parada estimada para a revisão de ignição (a cada 125 h): 6,0 h.
        /// Cobre revisão completa de magnetos, platinados, condensadores, fiação de alta tensão e velas.
        /// </summary>
        public const double Inspection500hDowntimeHours = 6.0;

        /// <summary>
        /// Parada estimada para retífica geral do motor (overhaul de TBO): 72,0 h (3 dias de oficina).
        /// Cobre desmontagem total, medições dimensionais, usinagem, troca de componentes e teste em bancada.
        /// </summary>
        public const double OverhaulDowntimeHours = 72.0;

        /// <summary>
        /// Parada padrão estimada para troca ou reparo de componentes ou panes sem downtime específico: 2,0 h.
        /// </summary>
        public const double DefaultRepairDowntimeHours = 2.0;

        /// <summary>
        /// Avança o relógio de carreira em um determinado número de horas.
        /// Horas negativas ou zeradas não alteram o relógio.
        /// </summary>
        public static DateTime Advance(DateTime clock, double hours)
        {
            if (hours <= 0.0) return clock;
            return clock.AddHours(hours);
        }

        /// <summary>
        /// Avança o relógio de carreira ao finalizar ou cancelar um voo após sua ativação.
        /// Soma a duração simulada voada na célula com o tempo padrão de solo/turnaround (0,5 h).
        /// </summary>
        public static DateTime AdvanceFlight(DateTime clock, double flightHours)
        {
            double totalHours = Math.Max(0.0, flightHours) + GroundTurnaroundHours;
            return Advance(clock, totalHours);
        }
    }
}
