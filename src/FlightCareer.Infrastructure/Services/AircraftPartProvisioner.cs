using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Infrastructure.Services
{
    /// <summary>
    /// Garante que toda aeronave do hangar tenha suas peças individuais instaladas.
    /// Avalia as features técnicas do modelo da aeronave e provisiona as peças aplicáveis a 100% de saúde.
    /// Idempotente: só cria peças faltantes e nunca remove peças existentes.
    /// </summary>
    public sealed class AircraftPartProvisioner
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly PartCatalog _catalog;

        public AircraftPartProvisioner(PartCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// Provisiona as peças faltantes para todas as aeronaves cadastradas no hangar.
        /// </summary>
        /// <returns>Quantidade total de peças criadas.</returns>
        public async Task<int> EnsurePartsAsync(IAppDbContext db)
        {
            var aircraft = await db.PilotAircrafts
                .Include(a => a.Pilot)
                .Include(a => a.AircraftType)
                .Include(a => a.Parts)
                .ToListAsync();

            if (aircraft.Count == 0) return 0;

            var profiles = await db.AircraftLimitProfiles
                .ToDictionaryAsync(p => p.AircraftTypeModel, StringComparer.OrdinalIgnoreCase);

            int createdCount = 0;

            foreach (var a in aircraft)
            {
                string? model = a.AircraftType?.Model;
                if (string.IsNullOrWhiteSpace(model) || !profiles.TryGetValue(model, out var profile))
                {
                    Debug.WriteLine($"[Peças] Aeronave '{model ?? "Desconhecida"}' (ID {a.Id}) sem perfil de limites/features cadastrado; provisionamento de peças ignorado.");
                    continue;
                }

                List<string> features;
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(profile.FeaturesJson, JsonOpts) ?? new List<string>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Peças] Erro ao desserializar FeaturesJson para perfil '{model}': {ex.Message}");
                    features = new List<string>();
                }

                var applicableParts = _catalog.PartsFor(features);
                var existingParts = a.Parts
                    .Select(p => (p.PartId.ToLowerInvariant(), p.EngineIndex))
                    .ToHashSet();

                int engineCount = Math.Max(1, profile.EngineCount);
                var missingParts = new List<AircraftPart>();

                foreach (var partDef in applicableParts)
                {
                    if (partDef.PerEngine)
                    {
                        for (int engineIndex = 1; engineIndex <= engineCount; engineIndex++)
                        {
                            if (!existingParts.Contains((partDef.Id.ToLowerInvariant(), engineIndex)))
                            {
                                missingParts.Add(new AircraftPart
                                {
                                    Id = Guid.NewGuid(),
                                    PilotAircraftId = a.Id,
                                    PartId = partDef.Id,
                                    EngineIndex = engineIndex,
                                    Health = 100.0,
                                    HoursSinceReplacement = 0.0,
                                    CyclesSinceReplacement = 0,
                                    LastReplacedAtUtc = a.Pilot?.CareerClockUtc ?? DateTime.UtcNow,
                                    LastReplacedAirframeHours = a.CurrentAirframeHours,
                                    Flag = PartFlag.None,
                                    FlaggedBy = PartFlagSource.None,
                                    FlaggedAtUtc = null,
                                    IsBroken = false,
                                    BrokenAtUtc = null
                                });
                            }
                        }
                    }
                    else
                    {
                        if (!existingParts.Contains((partDef.Id.ToLowerInvariant(), 0)))
                        {
                            missingParts.Add(new AircraftPart
                            {
                                Id = Guid.NewGuid(),
                                PilotAircraftId = a.Id,
                                PartId = partDef.Id,
                                EngineIndex = 0,
                                Health = 100.0,
                                HoursSinceReplacement = 0.0,
                                CyclesSinceReplacement = 0,
                                LastReplacedAtUtc = a.Pilot?.CareerClockUtc ?? DateTime.UtcNow,
                                LastReplacedAirframeHours = a.CurrentAirframeHours,
                                Flag = PartFlag.None,
                                FlaggedBy = PartFlagSource.None,
                                FlaggedAtUtc = null,
                                IsBroken = false,
                                BrokenAtUtc = null
                            });
                        }
                    }
                }

                if (missingParts.Count == 0) continue;

                db.AircraftParts.AddRange(missingParts);
                createdCount += missingParts.Count;
            }

            if (createdCount > 0)
            {
                await db.SaveChangesAsync();
            }

            return createdCount;
        }
    }
}
