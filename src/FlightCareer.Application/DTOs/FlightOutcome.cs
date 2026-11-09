namespace FlightCareer.Application.DTOs
{
    public class FlightOutcome
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double DistanceNM { get; set; }
        public int FlightTimeMinutes { get; set; }
        public double FuelUsed { get; set; }
        public double Revenue { get; set; }
        public double FuelCost { get; set; }
        public double LandingFee { get; set; }
        public double NetProfit => Revenue - FuelCost - LandingFee;
        public int XP { get; set; }
        public int CompletedMissions { get; set; }
        public int RemainingMissions { get; set; }
    }
}
