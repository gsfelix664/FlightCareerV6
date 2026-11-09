using System;

namespace FlightCareer.Domain.ValueObjects
{
    public readonly record struct Money(double Amount, string Currency = "USD")
    {
        public static implicit operator double(Money m) => m.Amount;
        public static implicit operator Money(double d) => new(d);

        public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount, a.Currency);
        public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount, a.Currency);
        public static Money operator *(Money a, double multiplier) => new(a.Amount * multiplier, a.Currency);

        public override string ToString() => $"{Amount:C2}";
    }
}
