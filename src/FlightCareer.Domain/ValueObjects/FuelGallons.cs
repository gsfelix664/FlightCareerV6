using System;

namespace FlightCareer.Domain.ValueObjects
{
    public readonly record struct FuelGallons(double Value)
    {
        public const double GallonsToLitersRatio = 3.785411784;
        public const double AvgasLbsPerGallon = 6.0;
        public const double JetALbsPerGallon = 6.7;

        public double ToLiters() => Math.Round(Value * GallonsToLitersRatio, 2);
        public static FuelGallons FromLiters(double liters) => new(Math.Round(liters / GallonsToLitersRatio, 2));

        public double ToWeightLbs(string fuelType = "Avgas")
            => fuelType.ToUpperInvariant().Contains("JET") ? Value * JetALbsPerGallon : Value * AvgasLbsPerGallon;

        public static implicit operator double(FuelGallons f) => f.Value;
        public static implicit operator FuelGallons(double d) => new(d);

        public static FuelGallons operator +(FuelGallons a, FuelGallons b) => new(a.Value + b.Value);
        public static FuelGallons operator -(FuelGallons a, FuelGallons b) => new(Math.Max(0, a.Value - b.Value));

        public override string ToString() => $"{Value:F1} gal";
    }
}
