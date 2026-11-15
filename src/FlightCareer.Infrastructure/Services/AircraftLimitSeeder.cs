using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Infrastructure.Services
{
    /// <summary>
    /// Semeia o catálogo de limites operacionais de aeronaves (AircraftLimitProfiles / AircraftLimits)
    /// com suporte a contexto curto (IDbContextFactory) e upsert idempotente por versão de dados.
    /// </summary>
    public class AircraftLimitSeeder
    {
        private readonly IDbContextFactory<AppDbContext>? _contextFactory;
        private readonly AppDbContext? _context;
        private readonly AircraftLimitCatalogLoader _loader;

        public AircraftLimitSeeder(IDbContextFactory<AppDbContext> contextFactory, AircraftLimitCatalogLoader? loader = null)
        {
            _contextFactory = contextFactory;
            _loader = loader ?? new AircraftLimitCatalogLoader();
        }

        public AircraftLimitSeeder(AppDbContext context, AircraftLimitCatalogLoader? loader = null)
        {
            _context = context;
            _loader = loader ?? new AircraftLimitCatalogLoader();
        }

        public async Task<int> SeedLimitsAsync(Action<string, double>? progressCallback = null)
        {
            if (_contextFactory != null)
            {
                await using var db = await _contextFactory.CreateDbContextAsync();
                return await SeedInternalAsync(db, progressCallback);
            }
            else if (_context != null)
            {
                return await SeedInternalAsync(_context, progressCallback);
            }

            throw new InvalidOperationException("Nenhum DbContext ou IDbContextFactory fornecido para o AircraftLimitSeeder.");
        }

        private async Task<int> SeedInternalAsync(AppDbContext db, Action<string, double>? progressCallback)
        {
            progressCallback?.Invoke("Semeando catálogo de limites operacionais das aeronaves...", 0.15);

            var profiles = _loader.LoadProfiles();
            int appliedCount = 0;

            var existingProfiles = await db.AircraftLimitProfiles
                .Include(p => p.Limits)
                .ToDictionaryAsync(p => p.AircraftTypeModel, StringComparer.OrdinalIgnoreCase);

            foreach (var profile in profiles)
            {
                string simMatchJson = JsonSerializer.Serialize(new
                {
                    title = profile.TitlePatterns,
                    atcModel = profile.AtcModelPatterns
                });

                if (!existingProfiles.TryGetValue(profile.AircraftId, out var existing))
                {
                    var newProfile = new AircraftLimitProfileEntity
                    {
                        Id = Guid.NewGuid(),
                        AircraftTypeModel = profile.AircraftId,
                        Class = profile.Class,
                        DataVersion = profile.DataVersion,
                        SimMatchJson = simMatchJson,
                        Limits = profile.Limits.Select(l => new AircraftLimitEntity
                        {
                            Id = Guid.NewGuid(),
                            Key = l.Key,
                            Value = l.Value,
                            Unit = l.Unit,
                            Source = l.Source,
                            Reference = l.Reference,
                            Notes = l.Notes
                        }).ToList()
                    };

                    db.AircraftLimitProfiles.Add(newProfile);
                    appliedCount++;
                }
                else if (profile.DataVersion > existing.DataVersion)
                {
                    existing.Class = profile.Class;
                    existing.DataVersion = profile.DataVersion;
                    existing.SimMatchJson = simMatchJson;

                    var oldLimits = existing.Limits.ToList();
                    db.AircraftLimits.RemoveRange(oldLimits);

                    foreach (var l in profile.Limits)
                    {
                        db.AircraftLimits.Add(new AircraftLimitEntity
                        {
                            Id = Guid.NewGuid(),
                            ProfileId = existing.Id,
                            Key = l.Key,
                            Value = l.Value,
                            Unit = l.Unit,
                            Source = l.Source,
                            Reference = l.Reference,
                            Notes = l.Notes
                        });
                    }
                    appliedCount++;
                }
            }

            if (appliedCount > 0)
            {
                await db.SaveChangesAsync();
            }

            return appliedCount;
        }
    }
}
