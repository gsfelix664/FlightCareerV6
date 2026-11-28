namespace FlightCareer.Domain.Connection;

/// <summary>
/// Vigia da ligação com o simulador. Quando o MSFS é fechado à força (sem mensagem Quit),
/// o SimConnect não dispara OnRecvQuit; este vigia rastreia mensagens recebidas e
/// detecta silêncio excessivo para acionar a desconexão e reconexão automática.
/// </summary>
public sealed class SimLinkWatchdog
{
    private DateTime _lastMessageUtc;

    public SimLinkWatchdog(TimeSpan silenceTimeout)
    {
        if (silenceTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(silenceTimeout), "O timeout de silêncio deve ser estritamente positivo.");

        SilenceTimeout = silenceTimeout;
    }

    public TimeSpan SilenceTimeout { get; }
    public bool IsArmed { get; private set; }

    public void MarkConnected(DateTime nowUtc)
    {
        IsArmed = true;
        _lastMessageUtc = nowUtc;
    }

    public void MarkMessage(DateTime nowUtc)
    {
        if (!IsArmed) return;
        _lastMessageUtc = nowUtc;
    }

    public void MarkDisconnected()
    {
        IsArmed = false;
    }

    public bool IsStale(DateTime nowUtc)
    {
        return IsArmed && (nowUtc - _lastMessageUtc) > SilenceTimeout;
    }

    public TimeSpan SilenceFor(DateTime nowUtc)
    {
        if (!IsArmed) return TimeSpan.Zero;
        return nowUtc - _lastMessageUtc;
    }
}
