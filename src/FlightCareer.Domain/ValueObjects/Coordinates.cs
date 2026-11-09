using System;

namespace FlightCareer.Domain.ValueObjects
{
    public readonly record struct Coordinates(double Latitude, double Longitude)
    {
        public bool IsValid => Math.Abs(Latitude) <= 90.0 && Math.Abs(Longitude) <= 180.0 && (Math.Abs(Latitude) > 0.0001 || Math.Abs(Longitude) > 0.0001);

        public double DistanceTo(Coordinates destination)
        {
            var rlat1 = Math.PI * Latitude / 180.0;
            var rlat2 = Math.PI * destination.Latitude / 180.0;
            var theta = Longitude - destination.Longitude;
            var rtheta = Math.PI * theta / 180.0;
            var dist = Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Cos(rtheta);
            dist = Math.Acos(Math.Min(1.0, Math.Max(-1.0, dist)));
            dist = dist * 180.0 / Math.PI;
            return dist * 60.0; // Milhas Náuticas (NM)
        }

        public override string ToString() => $"{Latitude:F4}, {Longitude:F4}";
    }
}
