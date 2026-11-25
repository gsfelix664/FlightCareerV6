using System;

namespace FlightCareer.Domain.Entities
{
    public class SimSyncState
    {
        public int Id { get; set; } = 1;
        public string SimVersion { get; set; } = string.Empty;
        public string ListDigest { get; set; } = string.Empty;
        public DateTime? LastListUtc { get; set; }
        public int ListCount { get; set; }
    }
}
