using System;
using System.Collections.Generic;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Gerencia o conhecimento da saúde das peças revelado por walkaround ou inspeções de oficina,
    /// e achados de defeitos graves da inspeção anual regulatória.
    /// </summary>
    public static class PartKnowledge
    {
        /// <summary>
        /// Grava o valor exato da saúde e as horas da aeronave nas peças dentro do escopo da inspeção/walkaround.
        /// </summary>
        public static void Reveal(
            IEnumerable<AircraftPart> parts,
            Func<PartDefinition, bool> inScope,
            PartCatalog catalog,
            double airframeHours)
        {
            if (parts == null || inScope == null || catalog == null) return;

            foreach (var part in parts)
            {
                var def = catalog.Get(part.PartId);
                if (def == null || !inScope(def)) continue;

                part.KnownHealth = part.Health;
                part.KnownAtAirframeHours = airframeHours;
            }
        }

        /// <summary>
        /// Registra nas peças quebradas ou em faixa vermelha (< 20%) o apontamento da inspeção anual regulatória.
        /// Peças em bom estado (verde/amarelo) têm o apontamento limpo.
        /// </summary>
        public static void MarkAnnualFindings(
            IEnumerable<AircraftPart> parts,
            PartCatalog catalog,
            PartBandSettings? bands,
            double airframeHours)
        {
            if (parts == null) return;
            bands ??= new PartBandSettings();

            foreach (var part in parts)
            {
                bool isRedOrBroken = part.IsBroken || part.Health < bands.RedBelowPct;
                if (isRedOrBroken)
                {
                    part.FoundRedAtAnnualAirframeHours = airframeHours;
                }
                else
                {
                    part.FoundRedAtAnnualAirframeHours = null;
                }
            }
        }
    }
}
