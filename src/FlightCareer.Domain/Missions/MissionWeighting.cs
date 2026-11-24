using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    public static class MissionWeighting
    {
        public static double OriginWeight(MissionDefinition m, int tier, IReadOnlySet<string> tags)
        {
            if (!m.Enabled) return 0.0;

            string tierKey = tier.ToString();
            if (!m.BaseByTier.TryGetValue(tierKey, out double baseWeight) || baseWeight <= 0.0)
            {
                return 0.0;
            }

            double multProduct = 1.0;
            if (m.OriginProfiles != null && m.OriginProfiles.Count > 0 && tags != null && tags.Count > 0)
            {
                foreach (var (profileKey, mult) in m.OriginProfiles)
                {
                    if (AirportProfileCatalog.MatchesAny(profileKey, tags))
                    {
                        multProduct *= mult;
                    }
                }
            }

            multProduct = Math.Min(12.0, multProduct);
            double weight = baseWeight * multProduct;

            // Base militar pura (tem 'militar' e NÃO tem 'militar_misto')
            if (tags != null && tags.Contains("militar") && !tags.Contains("militar_misto") && m.CivilAtMilitaryBase)
            {
                weight *= 0.2;
            }

            return weight;
        }

        public static double DestinationWeight(MissionDefinition m, IReadOnlySet<string> destTags)
        {
            double multProduct = 1.0;
            if (m.Destination?.Profiles != null && m.Destination.Profiles.Count > 0 && destTags != null && destTags.Count > 0)
            {
                foreach (var (profileKey, mult) in m.Destination.Profiles)
                {
                    if (AirportProfileCatalog.MatchesAny(profileKey, destTags))
                    {
                        multProduct *= mult;
                    }
                }
            }

            multProduct = Math.Min(12.0, multProduct);
            return Math.Max(1.0, multProduct);
        }
    }
}
