using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;

namespace FlightCareer.Infrastructure.Services
{
    /// <summary>
    /// Copia o estado de peças em memória para as entidades correspondentes no banco de dados.
    /// Não chama SaveChangesAsync (a transação e a gravação são de responsabilidade do chamador).
    /// </summary>
    public static class PartStateWriter
    {
        public static async Task CopyToDbAsync(AppDbContext ctx, IEnumerable<AircraftPart> parts)
        {
            if (ctx == null || parts == null) return;

            var partsList = parts.ToList();
            if (partsList.Count == 0) return;

            var ids = partsList.Select(p => p.Id).ToHashSet();
            var dbParts = await ctx.AircraftParts
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            var partMap = partsList.ToDictionary(p => p.Id);

            foreach (var dbPart in dbParts)
            {
                if (partMap.TryGetValue(dbPart.Id, out var memPart))
                {
                    dbPart.Health = memPart.Health;
                    dbPart.HoursSinceReplacement = memPart.HoursSinceReplacement;
                    dbPart.CyclesSinceReplacement = memPart.CyclesSinceReplacement;
                    dbPart.IsBroken = memPart.IsBroken;
                    dbPart.BrokenAtUtc = memPart.BrokenAtUtc;
                }
            }
        }
    }
}
