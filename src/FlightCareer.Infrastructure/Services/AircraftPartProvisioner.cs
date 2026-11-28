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
    /// Idempotente: cria peças faltantes e remove peças obsoletas ou que não se aplicam mais à aeronave.
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
        /// Provisiona as peças faltantes e remove as que deixaram de se aplicar para todas as aeronaves cadastradas no hangar.
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
            int removedCount = 0;

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
                    // Sem features confiáveis, a limpeza apagaria peças reais: pula esta aeronave.
                    Debug.WriteLine($"[Peças] Erro ao desserializar FeaturesJson para perfil '{model}': {ex.Message}. Provisionamento e limpeza ignorados.");
                    continue;
                }

                var applicableParts = _catalog.PartsFor(features);
                var applicablePartIds = applicableParts
                    .Select(p => p.Id)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // 1. Limpeza de peças que não existem mais no catálogo ou não se aplicam mais às features da aeronave
                var partsToRemove = a.Parts
                    .Where(p => _catalog.Find(p.PartId) == null || !applicablePartIds.Contains(p.PartId))
                    .ToList();

                if (partsToRemove.Count > 0)
                {
                    Debug.WriteLine($"[Peças] Aeronave '{model}' (ID {a.Id}): removendo {partsToRemove.Count} peça(s) que deixaram de se aplicar ou saíram do catálogo.");
                    db.AircraftParts.RemoveRange(partsToRemove);
                    foreach (var p in partsToRemove)
                    {
                        a.Parts.Remove(p);
                    }
                    removedCount += partsToRemove.Count;
                }

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

            if (createdCount > 0 || removedCount > 0)
            {
                await db.SaveChangesAsync();
            }

            return createdCount;
        }
    }
}
