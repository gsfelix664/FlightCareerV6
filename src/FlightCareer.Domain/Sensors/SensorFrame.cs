using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Sensors
{
    /// <summary>
    /// Pacote de leitura instantânea dos sensores do simulador.
    /// </summary>
    /// <param name="Values">Mapa chave -> valor numérico.</param>
    /// <param name="ReceivedUtc">Horário UTC do recebimento.</param>
    /// <param name="SimulationRate">Taxa de simulação ativa (1.0 = tempo real).</param>
    public sealed record SensorFrame(IReadOnlyDictionary<string, double> Values, DateTime ReceivedUtc, double SimulationRate);
}
