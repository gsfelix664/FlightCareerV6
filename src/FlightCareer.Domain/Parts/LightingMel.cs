using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public enum LightingMelCategory
    {
        NoGoNight,
        DayOnly
    }

    public sealed record LightingMelItem(
        string PartId,
        string Name,
        LightingMelCategory Category,
        string Message);

    /// <summary>
    /// Avaliação de restrições operacionais e avisos da MEL (Minimum Equipment List)
    /// para peças do grupo de Iluminação com falha.
    /// </summary>
    public static class LightingMel
    {
        public static IReadOnlyList<LightingMelItem> Evaluate(IEnumerable<AircraftPart> parts, PartCatalog catalog)
        {
            if (parts == null || catalog == null)
            {
                return Array.Empty<LightingMelItem>();
            }

            var partsList = parts.ToList();
            if (partsList.Count == 0)
            {
                return Array.Empty<LightingMelItem>();
            }

            bool hasIntactStrobe = partsList.Any(p =>
                p.PartId.Equals("lights.strobe", StringComparison.OrdinalIgnoreCase) && !p.IsBroken);

            var items = new List<LightingMelItem>();

            foreach (var part in partsList)
            {
                if (!part.IsBroken) continue;

                var def = catalog.Get(part.PartId);
                if (def == null || !string.Equals(def.Group, PartGroup.Iluminacao, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                LightingMelCategory category;
                string partIdLower = part.PartId.ToLowerInvariant();

                if (partIdLower == "lights.nav" || partIdLower == "lights.strobe" || partIdLower == "lights.panel")
                {
                    category = LightingMelCategory.NoGoNight;
                }
                else if (partIdLower == "lights.beacon")
                {
                    category = hasIntactStrobe ? LightingMelCategory.DayOnly : LightingMelCategory.NoGoNight;
                }
                else if (partIdLower == "lights.landing" || partIdLower == "lights.taxi")
                {
                    category = LightingMelCategory.DayOnly;
                }
                else
                {
                    category = LightingMelCategory.DayOnly;
                }

                string message = category == LightingMelCategory.NoGoNight
                    ? $"{def.Name} queimada: voo noturno não permitido (aviso)"
                    : $"{def.Name} queimada: permitido apenas voo diurno (aviso)";

                items.Add(new LightingMelItem(part.PartId, def.Name, category, message));
            }

            return items;
        }
    }
}
