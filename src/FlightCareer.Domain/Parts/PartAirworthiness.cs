using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Avalia a aeronavegabilidade física das peças da aeronave, identificando peças quebradas
    /// cujo catálogo determina interdição obrigatória do voo (<see cref="PartDefinition.BlocksFlightWhenBroken"/>).
    /// </summary>
    public static class PartAirworthiness
    {
        /// <summary>
        /// Retorna a lista de peças instaladas quebradas que bloqueiam o voo.
        /// Peças sem definição no catálogo são ignoradas.
        /// </summary>
        public static IReadOnlyList<AircraftPart> GroundingParts(IEnumerable<AircraftPart> parts, PartCatalog catalog)
        {
            if (parts == null || catalog == null)
            {
                return Array.Empty<AircraftPart>();
            }

            var result = new List<AircraftPart>();
            foreach (var part in parts)
            {
                if (!part.IsBroken) continue;
                var def = catalog.Get(part.PartId);
                if (def != null && def.BlocksFlightWhenBroken)
                {
                    result.Add(part);
                }
            }

            return result;
        }
    }
}
