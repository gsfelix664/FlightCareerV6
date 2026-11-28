using System;

namespace FlightCareer.Domain.Parts
{
    public enum PartBand
    {
        Green,
        Yellow,
        Red
    }

    public sealed record PartBandSettings(
        double YellowBelowPct = 40.0,
        double RedBelowPct = 20.0,
        double LowHealthWearMaxMultiplier = 2.0);

    public static class PartHealthBands
    {
        public static PartBand Classify(double health, PartBandSettings? s = null)
        {
            s ??= new PartBandSettings();
            if (health < s.RedBelowPct) return PartBand.Red;
            if (health < s.YellowBelowPct) return PartBand.Yellow;
            return PartBand.Green;
        }

        // 1 em h ≥ Yellow; 1 + (Max − 1) · ((Yellow − h) / Yellow)² abaixo (h limitado a [0, 100])
        public static double LowHealthWearMultiplier(double health, PartBandSettings? s = null)
        {
            s ??= new PartBandSettings();
            if (health >= s.YellowBelowPct) return 1.0;
            if (s.YellowBelowPct <= 0.0) return 1.0;

            double h = Math.Clamp(health, 0.0, 100.0);
            double fraction = (s.YellowBelowPct - h) / s.YellowBelowPct;
            return 1.0 + (s.LowHealthWearMaxMultiplier - 1.0) * (fraction * fraction);
        }
    }
}
