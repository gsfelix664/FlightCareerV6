using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Sensors
{
    /// <summary>
    /// Snapshot dos dados estáticos e de contexto da aeronave e do voo obtidos do MSFS.
    /// </summary>
    /// <param name="Numbers">Valores numéricos de contexto (engine count, flaps, etc.).</param>
    /// <param name="Strings">Strings de identificação (TITLE, ATC MODEL, ATC TYPE).</param>
    /// <param name="Utc">Horário UTC da leitura.</param>
    public sealed record ContextSnapshot(
        IReadOnlyDictionary<string, double> Numbers,
        IReadOnlyDictionary<string, string> Strings,
        DateTime Utc);
}
