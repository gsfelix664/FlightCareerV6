using System;

namespace FlightCareer.Domain.Entities
{
    public class Flight
    {
        public Guid Id { get; set; }
        public string OriginICAO { get; set; } = string.Empty;
        public string DestinationICAO { get; set; } = string.Empty;
        public double DurationHours { get; set; }
        public double DistanceNM { get; set; }
        public DateTime Date { get; set; }
        public double Revenue { get; set; }
        public int XP { get; set; }
        public Guid PilotId { get; set; }
        public Pilot? Pilot { get; set; }
        public string AircraftModel { get; set; } = string.Empty;
    }
}
