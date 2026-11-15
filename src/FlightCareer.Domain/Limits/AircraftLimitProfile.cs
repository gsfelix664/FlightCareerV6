using System.Collections.Generic;

namespace FlightCareer.Domain.Limits
{
    /// <summary>
    /// Limite individual de operação com valor, unidade, fonte de dados e referência.
    /// </summary>
    public sealed record AircraftLimit(
        string Key,
        double Value,
        string Unit,
        LimitSource Source,
        string Reference,
        string? Notes = null
    );

    /// <summary>
    /// Perfil completo de limites operacionais de um modelo de aeronave.
    /// </summary>
    public sealed record AircraftLimitProfile(
        string AircraftId,
        string Class,
        int DataVersion,
        IReadOnlyList<string> TitlePatterns,
        IReadOnlyList<string> AtcModelPatterns,
        IReadOnlyList<AircraftLimit> Limits
    );
}
