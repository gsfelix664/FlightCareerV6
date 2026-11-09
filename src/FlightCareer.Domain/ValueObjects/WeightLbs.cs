using System;

namespace FlightCareer.Domain.ValueObjects
{
    public readonly record struct WeightLbs(double Value)
    {
        public const double LbsToKgRatio = 0.45359237;

        public double ToKg() => Math.Round(Value * LbsToKgRatio, 2);
        public static WeightLbs FromKg(double kg) => new(Math.Round(kg / LbsToKgRatio, 2));

        public static implicit operator double(WeightLbs w) => w.Value;
        public static implicit operator WeightLbs(double d) => new(d);

        public static WeightLbs operator +(WeightLbs a, WeightLbs b) => new(a.Value + b.Value);
        public static WeightLbs operator -(WeightLbs a, WeightLbs b) => new(Math.Max(0, a.Value - b.Value));

        public override string ToString() => $"{Value:N0} lbs";
    }
}
