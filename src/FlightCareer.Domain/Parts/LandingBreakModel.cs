using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public sealed record LandingBreak(AircraftPart Part, double Chance, string Reason);

    /// <summary>
    /// Modelo probabilístico de quebra de trem de pouso e pneu em pouso duro ou fora de pista.
    /// </summary>
    public static class LandingBreakModel
    {
        private static readonly string[] GearPartOrder = { "gear.main_struts", "gear.nose_strut", "gear.tailwheel_assembly" };
        private static readonly string[] TirePartOrder = { "tires.main", "tires.nose" };

        /// <summary>Chance base de quebra do trem conforme a razão de descida no toque (fpm).</summary>
        public static double GearBaseChance(double fpm, LandingSettings s)
        {
            double absFpm = Math.Abs(fpm);
            if (absFpm < s.BreakMinFpm) return 0.0;
            if (absFpm < s.BreakSevereFpm)
            {
                double t = (absFpm - s.BreakMinFpm) / (s.BreakSevereFpm - s.BreakMinFpm);
                return s.GearBreakChanceAtMin + t * (s.GearBreakChanceAtSevere - s.GearBreakChanceAtMin);
            }
            if (absFpm <= s.BreakMaxFpm)
            {
                double t = (absFpm - s.BreakSevereFpm) / (s.BreakMaxFpm - s.BreakSevereFpm);
                return s.GearBreakChanceSevereStart + t * (s.GearBreakChanceAtMax - s.GearBreakChanceSevereStart);
            }
            return s.GearBreakChanceAtMax;
        }

        /// <summary>Chance final de quebra para uma peça (trem ou pneu) considerando fatores agravantes e desgaste.</summary>
        public static double ChanceFor(AircraftPart part, bool isTire, double fpm, double? bankDeg, bool offRunway, LandingSettings s)
        {
            double baseChance = GearBaseChance(fpm, s);
            if (baseChance <= 0.0) return 0.0;

            double chance = baseChance;
            if (isTire) chance *= s.TireBreakFactor;

            if (bankDeg.HasValue && Math.Abs(bankDeg.Value) >= s.BreakBankDeg)
            {
                chance *= s.BreakBankMultiplier;
            }

            if (offRunway)
            {
                chance *= s.OffRunwayBreakMultiplier;
            }

            double wearFactor = 1.0 + (s.WornBreakMultiplierMax - 1.0) * (1.0 - Math.Clamp(part.Health, 0.0, 100.0) / 100.0);
            chance *= wearFactor;

            return Math.Clamp(chance, 0.0, s.BreakChanceCap);
        }

        /// <summary>Sorteia no máximo uma peça de trem e uma de pneu elegíveis para quebra no pouso.</summary>
        public static IReadOnlyList<LandingBreak> Roll(
            IReadOnlyList<AircraftPart> parts,
            double fpm,
            double? bankDeg,
            bool offRunway,
            LandingSettings s,
            IRandomSource random)
        {
            var result = new List<LandingBreak>();

            TryRollCandidate(parts, GearPartOrder, isTire: false, fpm, bankDeg, offRunway, s, random, result);
            TryRollCandidate(parts, TirePartOrder, isTire: true, fpm, bankDeg, offRunway, s, random, result);

            return result;
        }

        private static void TryRollCandidate(
            IReadOnlyList<AircraftPart> parts,
            string[] order,
            bool isTire,
            double fpm,
            double? bankDeg,
            bool offRunway,
            LandingSettings s,
            IRandomSource random,
            List<LandingBreak> result)
        {
            AircraftPart? candidate = null;
            foreach (var id in order)
            {
                var p = parts.FirstOrDefault(x => x.PartId == id && !x.IsBroken);
                if (p != null)
                {
                    candidate = p;
                    break;
                }
            }

            if (candidate == null) return;

            double chance = ChanceFor(candidate, isTire, fpm, bankDeg, offRunway, s);
            if (chance > 0.0 && random.NextDouble() < chance)
            {
                result.Add(new LandingBreak(candidate, chance, FormatReason(fpm, bankDeg, offRunway, s)));
            }
        }

        private static string FormatReason(double fpm, double? bankDeg, bool offRunway, LandingSettings s)
        {
            var details = new List<string> { $"{Math.Abs(fpm):F0} fpm" };
            if (bankDeg.HasValue && Math.Abs(bankDeg.Value) >= s.BreakBankDeg)
            {
                details.Add($"{Math.Abs(bankDeg.Value):F0}° de inclinação");
            }
            if (offRunway)
            {
                details.Add("fora de pista");
            }
            return $"Pouso duro ({string.Join(", ", details)})";
        }
    }
}
