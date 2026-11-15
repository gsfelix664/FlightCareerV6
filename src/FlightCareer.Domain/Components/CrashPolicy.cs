namespace FlightCareer.Domain.Components
{
    public enum CrashDecision
    {
        Ignore,
        TotalLoss,
        AlertOnly
    }

    /// <summary>
    /// Política pura de decisão para eventos de crash reportados pelo simulador.
    /// Define se o crash vira perda total com encerramento de voo, apenas alerta ou se deve ser ignorado.
    /// </summary>
    public static class CrashPolicy
    {
        public static CrashDecision Decide(bool isMonitoring, bool isFlightActive, bool alreadyHandled)
        {
            if (alreadyHandled) return CrashDecision.Ignore;
            if (isMonitoring && isFlightActive) return CrashDecision.TotalLoss;
            return CrashDecision.AlertOnly;
        }
    }
}
