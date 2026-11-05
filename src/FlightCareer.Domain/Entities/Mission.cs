using System;

namespace FlightCareer.Domain.Entities
{
    public class Mission
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OriginICAO { get; set; } = string.Empty;
        public string DestinationICAO { get; set; } = string.Empty;
        public double PayloadLbs { get; set; }
        public int PassengerCount { get; set; }
        public double LuggageWeightLbs { get; set; }
        public double CargoWeightLbs { get; set; }
        public double Urgency { get; set; }
        public double RewardMoney { get; set; }
        public int ExperienceGain { get; set; }
        public double DistanceNM { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsAccepted { get; set; }
        public Guid? PilotId { get; set; }
    }
}
