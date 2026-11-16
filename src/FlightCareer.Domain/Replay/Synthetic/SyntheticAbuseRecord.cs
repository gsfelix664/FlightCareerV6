namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Registro detalhado de um abuso ou falha injetada no voo sintético.
    /// Usado pelo oráculo para verificar o gabarito.
    /// </summary>
    /// <param name="Type">Tipo do abuso injetado.</param>
    /// <param name="StartSeconds">Momento de início do abuso em segundos de simulação.</param>
    /// <param name="DurationSeconds">Duração planejada do abuso em segundos de simulação.</param>
    /// <param name="Intensity">Fator de intensidade relativa do abuso (ex.: 0.10 = +10% sobre o redline).</param>
    /// <param name="EngineIndex">Índice do motor afetado (1..N, ou 0 se global/não-específico).</param>
    /// <param name="TargetRuleId">Identificador esperado da regra de desgaste afetada.</param>
    /// <param name="TargetPartId">Identificador esperado da peça ou família afetada.</param>
    /// <param name="Notes">Observações ou justificativas do abuso.</param>
    public sealed record SyntheticAbuseRecord(
        SyntheticAbuseType Type,
        double StartSeconds,
        double DurationSeconds,
        double Intensity,
        int EngineIndex = 1,
        string? TargetRuleId = null,
        string? TargetPartId = null,
        string? Notes = null);
}
