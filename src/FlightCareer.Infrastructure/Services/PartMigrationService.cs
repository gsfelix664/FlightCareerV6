using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Infrastructure.Services
{
    public sealed record MigrationSummary(
        int AircraftMigrated,
        int PartsCreated,
        int PartsAdjusted,
        int ObsoletePartsRemoved,
        IReadOnlyList<string> Details);

    /// <summary>
    /// Serviço de migração e alinhamento de peças para aeronaves existentes e compras de mercado (F2.3).
    /// </summary>
    public sealed class PartMigrationService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly HashSet<string> RemovedCatalogIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "engine.wastegate_controller",
            "pressurization.door_seals",
            "hydraulic.reservoir_lines"
        };

        private readonly PartCatalog _partCatalog;
        private readonly PartComponentMapping _mapping;
        private readonly IRandomSource _random;

        public PartMigrationService(
            PartCatalog partCatalog,
            PartComponentMapping? mapping = null,
            IRandomSource? random = null)
        {
            _partCatalog = partCatalog ?? throw new ArgumentNullException(nameof(partCatalog));
            _mapping = mapping ?? PartComponentMapping.CreateDefault();
            _random = random ?? new SystemRandomSource();
        }

        /// <summary>
        /// Provisiona as peças de uma aeronave recém-adquirida no mercado (nova ou usada).
        /// Avião novo recebe peças a 100%; usado recebe peças coerentes com as condições anunciadas.
        /// </summary>
        public void ProvisionPurchasedAircraft(
            PilotAircraft aircraft,
            IReadOnlyList<string> features,
            int engineCount,
            double engineCondition = 100.0,
            double tiresCondition = 100.0,
            double airframeCondition = 100.0,
            double avionicsCondition = 100.0,
            double? tsmoHours = null)
        {
            if (aircraft == null) throw new ArgumentNullException(nameof(aircraft));

            int engines = Math.Max(1, engineCount);
            var applicableDefs = _partCatalog.PartsFor(features);
            aircraft.Parts ??= new List<AircraftPart>();
            aircraft.Parts.Clear();

            bool isBrandNew = aircraft.CurrentAirframeHours <= 0.05 &&
                              engineCondition >= 99.5 &&
                              airframeCondition >= 99.5;

            var rnd = new SystemRandomSource(aircraft.Id.GetHashCode());

            foreach (var def in applicableDefs)
            {
                int count = def.PerEngine ? engines : 1;
                for (int engIdx = (def.PerEngine ? 1 : 0); engIdx <= (def.PerEngine ? engines : 0); engIdx++)
                {
                    double health = 100.0;
                    double hoursSince = 0.0;
                    int cyclesSince = 0;

                    if (!isBrandNew)
                    {
                        double baseHealth = GetBaseHealthForGroup(def.Group, engineCondition, tiresCondition, airframeCondition, avionicsCondition);
                        double delta = (rnd.NextDouble() * 6.0) - 3.0; // Variação pequena de +-3%
                        health = Math.Clamp(Math.Round(baseHealth + delta, 1), 10.0, 100.0);

                        // Horas: clamp((100 - saúde) / 30 * vidaHoras, 0, horas de célula)
                        double rawHours = def.LifeHours > 0 ? ((100.0 - health) / 30.0) * def.LifeHours : 0.0;
                        hoursSince = Math.Clamp(rawHours, 0.0, aircraft.CurrentAirframeHours);

                        // Peças do motor coerentes com o TSMO (nunca mais horas que o TSMO)
                        bool isEnginePart = IsEngineGroup(def.Group);
                        if (isEnginePart && tsmoHours.HasValue)
                        {
                            hoursSince = Math.Min(hoursSince, tsmoHours.Value);
                        }

                        // Ciclos: clamp((100 - saúde) / 30 * vidaCiclos, 0, pousos estimados)
                        int estimatedLandings = aircraft.LandingsCount > 0 
                            ? aircraft.LandingsCount 
                            : (int)Math.Round(aircraft.CurrentAirframeHours);

                        double rawCycles = def.LifeCycles.HasValue ? ((100.0 - health) / 30.0) * def.LifeCycles.Value : 0.0;
                        cyclesSince = def.LifeCycles.HasValue ? (int)Math.Clamp(Math.Round(rawCycles), 0, estimatedLandings) : 0;
                    }

                    aircraft.Parts.Add(new AircraftPart
                    {
                        Id = Guid.NewGuid(),
                        PilotAircraftId = aircraft.Id,
                        PartId = def.Id,
                        EngineIndex = engIdx,
                        Health = health,
                        KnownHealth = health, // O comprador fez a inspeção pré-compra
                        HoursSinceReplacement = hoursSince,
                        CyclesSinceReplacement = cyclesSince,
                        LastReplacedAtUtc = aircraft.Pilot?.CareerClockUtc ?? DateTime.UtcNow,
                        LastReplacedAirframeHours = Math.Max(0.0, aircraft.CurrentAirframeHours - hoursSince),
                        Flag = PartFlag.None,
                        FlaggedBy = PartFlagSource.None,
                        FlaggedAtUtc = null,
                        IsBroken = false,
                        BrokenAtUtc = null,
                        LastShopServiceAtUtc = null, // Compra NÃO preenche serviço de oficina
                        LastShopServiceAirframeHours = null,
                        LastShopServiceCycles = null
                    });
                }
            }

            aircraft.PartsMigratedAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Migra as peças de uma aeronave existente no banco.
        /// Idempotente: se já migrada (PartsMigratedAtUtc != null), não repete a migração.
        /// Remove peças obsoletas (que saíram do catálogo ou deixaram de se aplicar) se nunca foram trocadas/marcadas.
        /// </summary>
        public async Task<MigrationSummary> MigrateAircraftAsync(IAppDbContext db, PilotAircraft aircraft)
        {
            var details = new List<string>();
            if (aircraft.PartsMigratedAtUtc.HasValue)
            {
                details.Add($"Aeronave {aircraft.Prefix} já migrada anteriormente em {aircraft.PartsMigratedAtUtc}.");
                return new MigrationSummary(0, 0, 0, 0, details);
            }

            string model = aircraft.AircraftType?.Model ?? string.Empty;
            var profile = await db.AircraftLimitProfiles
                .FirstOrDefaultAsync(p => p.AircraftTypeModel.ToLower() == model.ToLower());

            List<string> features = new();
            int engineCount = 1;
            if (profile != null)
            {
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(profile.FeaturesJson, JsonOpts) ?? new();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Migração] Falha ao deserializar FeaturesJson para perfil {profile.AircraftTypeModel}: {ex.Message}");
                }
                engineCount = Math.Max(1, profile.EngineCount);
            }

            var applicableDefs = _partCatalog.PartsFor(features);
            var applicableIds = applicableDefs.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            int obsoleteRemoved = 0;
            int partsCreated = 0;
            int partsAdjusted = 0;

            // 1. Remover peças obsoletas que saíram do catálogo ou não se aplicam mais (se virgens)
            var partsToRemove = new List<AircraftPart>();
            foreach (var part in aircraft.Parts)
            {
                bool isObsolete = RemovedCatalogIds.Contains(part.PartId) || !applicableIds.Contains(part.PartId);
                if (isObsolete)
                {
                    bool isUntouched = part.HoursSinceReplacement <= 0.001 &&
                                       part.Flag == PartFlag.None &&
                                       !part.IsBroken;

                    if (isUntouched)
                    {
                        partsToRemove.Add(part);
                        obsoleteRemoved++;
                        details.Add($"Removida peça obsoleta/não aplicável '{part.PartId}' (Index {part.EngineIndex}) da aeronave {aircraft.Prefix}.");
                    }
                    else
                    {
                        details.Add($"Peça obsoleta '{part.PartId}' mantida pois já possui histórico de uso/marcação.");
                    }
                }
            }

            if (partsToRemove.Count > 0)
            {
                foreach (var p in partsToRemove)
                {
                    aircraft.Parts.Remove(p);
                    db.AircraftParts.Remove(p);
                }
            }

            // 2. Criação ou ajuste de peças aplicáveis para herdar o desgaste das barras de componentes
            var existingMap = aircraft.Parts
                .ToDictionary(p => (p.PartId.ToLowerInvariant(), p.EngineIndex));

            var rnd = new SystemRandomSource(aircraft.Id.GetHashCode());

            foreach (var def in applicableDefs)
            {
                int maxEngIdx = def.PerEngine ? engineCount : 0;
                int minEngIdx = def.PerEngine ? 1 : 0;

                for (int engIdx = minEngIdx; engIdx <= maxEngIdx; engIdx++)
                {
                    var key = (def.Id.ToLowerInvariant(), engIdx);

                    double baseHealth = GetBaseHealthFromComponents(aircraft, def.Group);
                    double delta = (rnd.NextDouble() * 6.0) - 3.0; // +-3%
                    double estimatedHealth = Math.Clamp(Math.Round(baseHealth + delta, 1), 10.0, 100.0);

                    double deficit = Math.Max(0.0, 100.0 - estimatedHealth);
                    double estHours = def.LifeHours > 0 ? (deficit / 70.0) * def.LifeHours : 0.0;
                    estHours = Math.Min(aircraft.CurrentAirframeHours, Math.Max(0.0, estHours));
                    int estCycles = def.LifeCycles.HasValue ? (int)Math.Round((deficit / 70.0) * def.LifeCycles.Value) : 0;
                    estCycles = Math.Min(aircraft.LandingsCount, Math.Max(0, estCycles));

                    bool found = existingMap.TryGetValue(key, out var existingPart);
                    if (!found && engIdx == 1 && existingMap.TryGetValue((def.Id.ToLowerInvariant(), 0), out existingPart))
                    {
                        found = true;
                        existingPart.EngineIndex = 1;
                    }

                    if (found && existingPart != null)
                    {
                        // Se a peça já existia porém estava a 100% num avião antigo que está desgastado
                        if (existingPart.Health >= 99.9 && aircraft.CurrentAirframeHours > 5.0 && baseHealth < 95.0)
                        {
                            existingPart.Health = estimatedHealth;
                            existingPart.HoursSinceReplacement = estHours;
                            existingPart.CyclesSinceReplacement = estCycles;
                            existingPart.LastReplacedAirframeHours = Math.Max(0.0, aircraft.CurrentAirframeHours - estHours);
                            partsAdjusted++;
                        }
                    }
                    else
                    {
                        var newPart = new AircraftPart
                        {
                            Id = Guid.NewGuid(),
                            PilotAircraftId = aircraft.Id,
                            PartId = def.Id,
                            EngineIndex = engIdx,
                            Health = estimatedHealth,
                            HoursSinceReplacement = estHours,
                            CyclesSinceReplacement = estCycles,
                            LastReplacedAtUtc = aircraft.Pilot?.CareerClockUtc ?? DateTime.UtcNow,
                            LastReplacedAirframeHours = Math.Max(0.0, aircraft.CurrentAirframeHours - estHours),
                            Flag = PartFlag.None,
                            FlaggedBy = PartFlagSource.None,
                            FlaggedAtUtc = null,
                            IsBroken = false,
                            BrokenAtUtc = null,
                            LastShopServiceAtUtc = null, // Migração NÃO preenche serviço de oficina
                            LastShopServiceAirframeHours = null,
                            LastShopServiceCycles = null
                        };
                        aircraft.Parts.Add(newPart);
                        db.AircraftParts.Add(newPart);
                        partsCreated++;
                    }
                }
            }

            aircraft.PartsMigratedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            details.Add($"Aeronave {aircraft.Prefix} migrada com sucesso. Criadas: {partsCreated}, Ajustadas: {partsAdjusted}, Removidas: {obsoleteRemoved}.");
            return new MigrationSummary(1, partsCreated, partsAdjusted, obsoleteRemoved, details);
        }

        /// <summary>
        /// Migra todas as aeronaves cadastradas no banco que ainda não foram migradas (PartsMigratedAtUtc == null).
        /// Executado na inicialização da aplicação (App.xaml.cs) logo após o provisionamento de peças.
        /// </summary>
        public async Task<int> MigrateAllPendingAsync(IAppDbContext db)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));

            var unmigratedAircraft = await db.PilotAircrafts
                .Include(a => a.AircraftType)
                .Include(a => a.Components)
                .Include(a => a.Parts)
                .Where(a => a.PartsMigratedAtUtc == null)
                .ToListAsync();

            if (unmigratedAircraft.Count == 0) return 0;

            int migratedCount = 0;
            foreach (var aircraft in unmigratedAircraft)
            {
                var summary = await MigrateAircraftAsync(db, aircraft);
                if (summary.AircraftMigrated > 0)
                {
                    migratedCount++;
                }
            }

            return migratedCount;
        }

        private double GetBaseHealthFromComponents(PilotAircraft aircraft, string group)
        {
            var kind = _mapping.ResolveComponentKind(group);
            if (kind.HasValue && aircraft.Components != null)
            {
                var comp = aircraft.Components.FirstOrDefault(c => c.Kind == kind.Value);
                if (comp != null) return comp.Health;
            }

            // Fallback para colunas legadas
            return GetBaseHealthForGroup(group, aircraft.EngineCondition, aircraft.TiresCondition, aircraft.AirframeCondition, aircraft.AvionicsCondition);
        }

        private static double GetBaseHealthForGroup(
            string group,
            double engineCondition,
            double tiresCondition,
            double airframeCondition,
            double avionicsCondition)
        {
            if (string.Equals(group, PartGroup.Motor, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, PartGroup.Helice, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, PartGroup.Combustivel, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, PartGroup.SecaoQuente, StringComparison.OrdinalIgnoreCase))
            {
                return engineCondition;
            }

            if (string.Equals(group, PartGroup.Pneus, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, PartGroup.Freios, StringComparison.OrdinalIgnoreCase))
            {
                return tiresCondition;
            }

            if (string.Equals(group, PartGroup.Eletrico, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, PartGroup.Avionicos, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, PartGroup.Instrumentos, StringComparison.OrdinalIgnoreCase))
            {
                return avionicsCondition;
            }

            return airframeCondition;
        }

        private bool IsEngineGroup(string? group)
        {
            if (string.IsNullOrWhiteSpace(group)) return false;
            var kind = _mapping.ResolveComponentKind(group);
            return kind is ComponentKind.Engine or ComponentKind.HotSection or ComponentKind.Oil or ComponentKind.Ignition;
        }
    }
}
