using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Parâmetros operacionais e limites do sistema elétrico de uma aeronave (F2.4b-CAL).
    /// </summary>
    public sealed record ElectricalConfig
    {
        /// <summary>
        /// Tensão nominal do sistema com alternador carregando (28 ou 14 V).
        /// </summary>
        public double? SystemVolts { get; init; }

        /// <summary>
        /// Índice global do circuito que reflete a chave de aviônicos (CIRCUIT ON:n).
        /// </summary>
        public int? AvionicsCircuitIndex { get; init; }

        /// <summary>
        /// Índice do barramento que alimenta os aviônicos (ELECTRICAL BUS AMPS:n).
        /// </summary>
        public int? AvionicsBusAmpsIndex { get; init; }

        /// <summary>
        /// Índice do barramento principal para medição de tensão (ELECTRICAL BUS VOLTAGE:n).
        /// </summary>
        public int? MainBusIndex { get; init; }

        /// <summary>
        /// Capacidade nominal máxima de saída do alternador/gerador em ampères (amperes do POH).
        /// </summary>
        public double? AlternatorRatedAmps { get; init; }

        /// <summary>
        /// Limiar de tensão para descarga profunda da bateria (&lt;22V em sistema 28V ou &lt;11V em 14V).
        /// </summary>
        public double? DeepDischargeVolts { get; init; }

        /// <summary>
        /// Faixas cumulativas de minutos de descarga profunda da bateria (thresholds de wear_rules.json).
        /// </summary>
        public IReadOnlyList<double> BatteryDeepDischargeThresholds { get; init; } = new[] { 10.0, 25.0, 45.0 };

        /// <summary>
        /// Faixas cumulativas de minutos de sobrecarga de carga elétrica (thresholds de wear_rules.json).
        /// </summary>
        public IReadOnlyList<double> ElectricalOverloadThresholds { get; init; } = new[] { 5.0, 15.0, 30.0 };
    }
}
