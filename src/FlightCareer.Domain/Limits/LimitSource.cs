namespace FlightCareer.Domain.Limits
{
    /// <summary>
    /// Origem do limite operacional da aeronave. Menor valor numérico indica maior prioridade.
    /// </summary>
    public enum LimitSource
    {
        POH = 1,
        SimVar = 2,
        Cfg = 3,
        NativeOnset = 4,
        ClassDefault = 5
    }
}
