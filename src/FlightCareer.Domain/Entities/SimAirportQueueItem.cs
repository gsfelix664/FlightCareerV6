using System;

namespace FlightCareer.Domain.Entities
{
    public class SimAirportQueueItem
    {
        public string Ident { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AltitudeM { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
        public int Priority { get; set; }
        public int Attempts { get; set; }
        public DateTime EnqueuedUtc { get; set; }
    }
}
