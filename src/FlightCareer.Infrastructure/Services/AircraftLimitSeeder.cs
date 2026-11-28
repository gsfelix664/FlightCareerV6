using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
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
                string featuresJson = JsonSerializer.Serialize(profile.Features ?? (IReadOnlyList<string>)Array.Empty<string>());

                if (!existingProfiles.TryGetValue(profile.AircraftId, out var existing))
                {
                    var newProfile = new AircraftLimitProfileEntity
                    {
                        Id = Guid.NewGuid(),
                        AircraftTypeModel = profile.AircraftId,
                        Class = profile.Class,
                        DataVersion = profile.DataVersion,
                        SimMatchJson = simMatchJson,
                        FeaturesJson = featuresJson,
                        EngineCount = profile.EngineCount,
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
                    existing.FeaturesJson = featuresJson;
                    existing.EngineCount = profile.EngineCount;

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

            // Sincroniza HasConvertibleCabin em AircraftTypes para bancos existentes ou novos perfis
            var resolver = new AircraftLimitResolver(profiles, new Dictionary<string, IReadOnlyList<AircraftLimit>>());
            var aircraftTypes = await db.AircraftTypes.ToListAsync();
            foreach (var at in aircraftTypes)
            {
                var matched = resolver.FindById(at.Model) ?? resolver.FindProfile(at.Model, at.Model);
                bool hasConvertible = matched != null && matched.Features.Contains(AircraftFeatures.ConvertibleCabin, StringComparer.OrdinalIgnoreCase);
                if (at.HasConvertibleCabin != hasConvertible)
                {
                    at.HasConvertibleCabin = hasConvertible;
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
