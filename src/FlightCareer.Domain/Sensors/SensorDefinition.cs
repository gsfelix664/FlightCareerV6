namespace FlightCareer.Domain.Sensors
{
    /// <summary>
    /// Definição de sensor / SimVar catalogada para contexto ou telemetria contínua.
    /// </summary>
    /// <param name="Key">Identificador curto e estável em kebab-case.</param>
    /// <param name="SimVar">Nome exato da SimVar no MSFS SDK, sem o sufixo :N.</param>
    /// <param name="Unit">Unidade de medida no SDK, em minúsculas.</param>
    /// <param name="Group">Grupo/sistema (context, piston, oil, turbine, etc.).</param>
    /// <param name="PerEngine">Verdadeiro quando a variável possui índice por motor (:N no inventário).</param>
    /// <param name="CalibrationOnly">Indica que o sensor só é requisitado e lido no modo de calibração de aeronave.</param>
    public sealed record SensorDefinition(string Key, string SimVar, string Unit, string Group, bool PerEngine, bool CalibrationOnly = false);
}
