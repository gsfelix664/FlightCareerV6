using System;
using System.Threading.Tasks;

namespace FlightCareer.Domain.Services
{
    public class MsfsSyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalMsfsAirports { get; set; }
        public int TotalValidatedInSim { get; set; }
        public int DivergencesIdentified { get; set; }
        public int NonSimulatorExcluded { get; set; }
    }

    public interface IMsfsScenerySyncService
    {
        string? LocateMsfsDatabasePath();
        Task<bool> NeedsSyncAsync();
        Task<MsfsSyncResult> SyncAirportsAsync(Action<string, double>? progressCallback = null);
    }
}
