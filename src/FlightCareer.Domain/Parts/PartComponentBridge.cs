using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    public sealed class PartAggregationSettings
    {
        public double MinWeight { get; init; } = 0.7;
        public double AverageWeight { get; init; } = 0.3;
    }

    public sealed class PartComponentMapping
    {
        public Dictionary<string, string> GroupMappings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public PartAggregationSettings Aggregation { get; init; } = new();

        public ComponentKind? ResolveComponentKind(string group)
        {
            if (string.IsNullOrWhiteSpace(group)) return null;
            if (GroupMappings.TryGetValue(group, out var kindStr) &&
                Enum.TryParse<ComponentKind>(kindStr, true, out var kind))
            {
                return kind;
            }
            return null;
        }

        public static PartComponentMapping CreateDefault()
        {
            return new PartComponentMapping
            {
                GroupMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [PartGroup.Motor] = nameof(ComponentKind.Engine),
                    [PartGroup.Helice] = nameof(ComponentKind.Engine),
                    [PartGroup.Combustivel] = nameof(ComponentKind.Engine),
                    [PartGroup.Oleo] = nameof(ComponentKind.Oil),
                    [PartGroup.Ignicao] = nameof(ComponentKind.Ignition),
                    [PartGroup.SecaoQuente] = nameof(ComponentKind.HotSection),
                    [PartGroup.Eletrico] = nameof(ComponentKind.Electrical),
                    [PartGroup.Avionicos] = nameof(ComponentKind.Electrical),
                    [PartGroup.Instrumentos] = nameof(ComponentKind.Instruments),
                    [PartGroup.Trem] = nameof(ComponentKind.LandingGear),
                    [PartGroup.Pneus] = nameof(ComponentKind.Tires),
                    [PartGroup.Freios] = nameof(ComponentKind.Brakes),
                    [PartGroup.Asa] = nameof(ComponentKind.Airframe),
                    [PartGroup.Fuselagem] = nameof(ComponentKind.Airframe),
                    [PartGroup.Empenagem] = nameof(ComponentKind.Airframe),
                    [PartGroup.Hidraulico] = nameof(ComponentKind.Airframe),
                    [PartGroup.Pressurizacao] = nameof(ComponentKind.Airframe),
                    [PartGroup.Pneumatico] = nameof(ComponentKind.Airframe),
                    [PartGroup.Cabine] = nameof(ComponentKind.Airframe),
                    [PartGroup.Oxigenio] = nameof(ComponentKind.Airframe),
                    [PartGroup.Fogo] = nameof(ComponentKind.Airframe),
                    [PartGroup.Emergencia] = nameof(ComponentKind.Airframe)
                },
                Aggregation = new PartAggregationSettings
                {
                    MinWeight = 0.7,
                    AverageWeight = 0.3
                }
            };
        }
    }

    /// <summary>
    /// Ponte entre as peças individuais (fonte da verdade) e as 9 barras de componente legadas.
    /// As barras viram derivadas do agregado de saúde das peças do seu grupo (mínimo ponderado).
    /// </summary>
    public static class PartComponentBridge
    {
        /// <summary>
        /// Determina se uma aeronave possui o sistema de peças individuais ativado (ao menos 1 peça instalada).
        /// Quando verdadeiro, o motor de desgaste legado (WearEngine/FailureEngine) não deve desgastar a aeronave.
        /// </summary>
        public static bool HasParts(PilotAircraft aircraft)
        {
            return aircraft.Parts != null && aircraft.Parts.Count > 0;
        }

        /// <summary>
        /// Calcula a saúde agregada (mínimo ponderado) de uma coleção de peças pertencentes a um componente.
        /// </summary>
        public static double AggregateHealth(IReadOnlyList<AircraftPart> parts, PartAggregationSettings? settings = null)
        {
            if (parts == null || parts.Count == 0) return 100.0;
            settings ??= new PartAggregationSettings();

            double min = parts.Min(p => p.Health);
            double avg = parts.Average(p => p.Health);

            double totalWeight = settings.MinWeight + settings.AverageWeight;
            if (totalWeight <= 0) totalWeight = 1.0;

            double weighted = ((min * settings.MinWeight) + (avg * settings.AverageWeight)) / totalWeight;
            return Math.Clamp(Math.Round(weighted, 2), 0.0, 100.0);
        }

        /// <summary>
        /// Atualiza as barras de componente (AircraftComponent) e as colunas legadas de PilotAircraft
        /// a partir do estado atual das peças individuais instaladas.
        /// </summary>
        public static int SyncComponentsFromParts(
            PilotAircraft aircraft,
            IEnumerable<AircraftComponent> components,
            IEnumerable<AircraftPart> parts,
            PartCatalog partCatalog,
            PartComponentMapping? mapping = null)
        {
            if (aircraft == null || components == null || parts == null || partCatalog == null)
            {
                return 0;
            }

            mapping ??= PartComponentMapping.CreateDefault();
            var partsList = parts.ToList();
            if (partsList.Count == 0) return 0;

            var compList = components.ToList();
            int updated = 0;

            // Mapeia cada peça para o ComponentKind correspondente ao seu grupo no catálogo
            var partsByKind = new Dictionary<ComponentKind, List<AircraftPart>>();
            foreach (var part in partsList)
            {
                var def = partCatalog.Get(part.PartId);
                if (def == null) continue;

                var kind = mapping.ResolveComponentKind(def.Group);
                if (kind.HasValue)
                {
                    if (!partsByKind.TryGetValue(kind.Value, out var list))
                    {
                        list = new List<AircraftPart>();
                        partsByKind[kind.Value] = list;
                    }
                    list.Add(part);
                }
            }

            foreach (var comp in compList)
            {
                if (partsByKind.TryGetValue(comp.Kind, out var matchingParts) && matchingParts.Count > 0)
                {
                    double newHealth = AggregateHealth(matchingParts, mapping.Aggregation);
                    if (Math.Abs(comp.Health - newHealth) > 0.001)
                    {
                        comp.Health = newHealth;
                        updated++;
                    }
                }
            }

            // Sincroniza também as colunas antigas da aeronave para garantir que UI/Mercado/HUD reflitam as peças
            LegacyConditionBridge.WriteToLegacy(aircraft, compList);

            return updated;
        }

        /// <summary>
        /// Reconcilia as peças a partir das barras de componente caso alterações tenham sido feitas
        /// fora do voo (ex.: reparo ou revisão no Hangar, perda total ou dano manual).
        /// - Se barra > agregado + 0,5: peças do grupo com Health < barra sobem para o valor da barra;
        ///   se a barra for >= 99,5, limpa também IsBroken e BrokenAtUtc.
        /// - Se barra < agregado - 0,5: peças do grupo sofrem clamp para não ultrapassarem a barra.
        /// </summary>
        public static int ReconcilePartsFromComponents(
            IEnumerable<AircraftComponent> components,
            IEnumerable<AircraftPart> parts,
            PartCatalog partCatalog,
            PartComponentMapping? mapping = null)
        {
            if (components == null || parts == null || partCatalog == null) return 0;
            mapping ??= PartComponentMapping.CreateDefault();

            var partsList = parts.ToList();
            var compList = components.ToList();
            if (partsList.Count == 0 || compList.Count == 0) return 0;

            var partsByKind = new Dictionary<ComponentKind, List<AircraftPart>>();
            foreach (var part in partsList)
            {
                var def = partCatalog.Get(part.PartId);
                if (def == null) continue;

                var kind = mapping.ResolveComponentKind(def.Group);
                if (kind.HasValue)
                {
                    if (!partsByKind.TryGetValue(kind.Value, out var list))
                    {
                        list = new List<AircraftPart>();
                        partsByKind[kind.Value] = list;
                    }
                    list.Add(part);
                }
            }

            int modified = 0;
            foreach (var comp in compList)
            {
                if (!partsByKind.TryGetValue(comp.Kind, out var groupParts) || groupParts.Count == 0)
                    continue;

                double aggregate = AggregateHealth(groupParts, mapping.Aggregation);

                if (comp.Health > aggregate + 0.5)
                {
                    foreach (var p in groupParts)
                    {
                        if (p.Health < comp.Health)
                        {
                            p.Health = comp.Health;
                            modified++;
                        }

                        if (comp.Health >= 99.5 && p.IsBroken)
                        {
                            p.IsBroken = false;
                            p.BrokenAtUtc = null;
                            modified++;
                        }
                    }
                }
                else if (comp.Health < aggregate - 0.5)
                {
                    foreach (var p in groupParts)
                    {
                        if (p.Health > comp.Health)
                        {
                            p.Health = comp.Health;
                            modified++;
                        }
                    }
                }
            }

            return modified;
        }
    }
}
