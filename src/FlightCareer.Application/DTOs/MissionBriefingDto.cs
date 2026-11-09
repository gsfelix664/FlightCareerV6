using System;
using System.Collections.Generic;

namespace FlightCareer.Application.DTOs
{
    public class MissionBriefingDto
    {
        public Guid MissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginICAO { get; set; } = string.Empty;
        public string DestinationICAO { get; set; } = string.Empty;
        public double DistanceNM { get; set; }
        public int PassengerCount { get; set; }
        public double PayloadLbs { get; set; }
        public double RewardMoney { get; set; }
        public int ExperienceGain { get; set; }
        public string MissionCategoryType { get; set; } = string.Empty;
    }

    public class FlightTelemetryDto
    {
        public DateTime Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AltitudeFeet { get; set; }
        public double HeadingDegrees { get; set; }
        public double GroundSpeedKnots { get; set; }
        public double VerticalSpeedFpm { get; set; }
        public double PitchDegrees { get; set; }
        public double BankDegrees { get; set; }
        public double GForce { get; set; }
        public bool IsOnGround { get; set; }
        public double FuelRemainingGallons { get; set; }
        public string FlightPhase { get; set; } = string.Empty;
    }
}
