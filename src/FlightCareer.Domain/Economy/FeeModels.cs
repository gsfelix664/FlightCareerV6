namespace FlightCareer.Domain.Economy
{
    public record FeeLine(string Code, string Description, double Quantity, double UnitPrice, double Total);

    public record FeeContext
    {
        public int AirportTier { get; init; }
        public string Icao { get; init; } = string.Empty;
        public double MtowLbs { get; init; }
        public int Passengers { get; init; }
        public bool HasMission { get; init; }
        public double ParkedGameHours { get; init; }

        public FeeContext(
            int airportTier,
            string icao,
            double mtowLbs,
            int passengers,
            bool hasMission,
            double parkedGameHours = 0.0)
        {
            AirportTier = airportTier;
            Icao = icao;
            MtowLbs = mtowLbs;
            Passengers = passengers;
            HasMission = hasMission;
            ParkedGameHours = parkedGameHours;
        }
    }
}
