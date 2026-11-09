using FlightCareer.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace FlightCareer.Application.Interfaces
{
    public interface IMsfsScenerySyncService
    {
        string? LocateMsfsDatabasePath();
        Task<bool> NeedsSyncAsync();
        Task<MsfsSyncResult> SyncAirportsAsync(Action<string, double>? progressCallback = null);
    }
}
