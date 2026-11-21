using System.Collections.Generic;

namespace FlightCareer.Domain.Sensors.Probe
{
    public enum ProbeIndexKind
    {
        None,
        Engine,
        Contact,
        Wear,
        Cover,
        Generic,
        Multi
    }

    public sealed record ProbeEntry(
        string SimVar,
        string Unit,
        bool IsString,
        ProbeIndexKind IndexKind,
        string Source,
        string Category,
        bool? Settable);

    public sealed record ProbeSlot(
        string Name,
        ProbeEntry Entry,
        int? Index); // Name = "SIMVAR" ou "SIMVAR:i"

    public sealed record ProbeOptions(
        int MaxEngine = 4,
        int MaxContact = 5,
        int MaxGeneric = 4,
        string? Filter = null,
        int BlockSize = 200);

    public enum ProbeStatus
    {
        SemResposta,
        Rejeitada,
        Erro,
        AceitaSemAmostras,
        AceitaConstante,
        AceitaVariando
    }

    public sealed record ProbeBlock(
        int Index,
        IReadOnlyList<ProbeSlot> Slots);
}
