using System;
using System.Collections.Generic;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Services
{
    public static class DefectText
    {
        private static readonly Dictionary<string, string> Bases = new(StringComparer.OrdinalIgnoreCase)
        {
            [DefectCatalog.EngineFailure] = "O motor parou de funcionar",
            [DefectCatalog.EngineFire] = "O motor pegou fogo",
            [DefectCatalog.EngineDamage] = "O motor sofreu dano interno",
            [DefectCatalog.MagnetoLeft] = "O magneto esquerdo parou de funcionar",
            [DefectCatalog.MagnetoRight] = "O magneto direito parou de funcionar",
            [DefectCatalog.FlapLock] = "Os flaps travaram",
            [DefectCatalog.FlapDamage] = "Os flaps foram danificados por excesso de velocidade",
            [DefectCatalog.Alternator] = "O alternador parou de carregar a bateria",
            [DefectCatalog.Vacuum] = "A bomba de vácuo parou de funcionar",
            [DefectCatalog.Pitot] = "O tubo de pitot entupiu",
            [DefectCatalog.StaticPort] = "A tomada estática entupiu",
            [DefectCatalog.GearDamage] = "O trem de pouso foi danificado por excesso de velocidade",
            [DefectCatalog.GearCollapse] = "O trem de pouso cedeu",
            [DefectCatalog.TireBlowout] = "Um pneu estourou",
            [DefectCatalog.TireFlat] = "Um pneu murchou",
            [DefectCatalog.BrakesWorn] = "Os freios chegaram ao limite de desgaste",
            [DefectCatalog.BrakeFailure] = "Os freios pararam de funcionar",
            [DefectCatalog.Pressurization] = "A pressurização parou de funcionar",
            [DefectCatalog.OilLeak] = "O motor começou a vazar óleo",
            [DefectCatalog.Electrical] = "Houve um curto no sistema elétrico",
            [DefectCatalog.Hydraulic] = "O sistema hidráulico começou a vazar",
            [DefectCatalog.Structural] = "A estrutura ficou trincada",
            [DefectCatalog.Battery] = "A bateria parou de funcionar",
            [DefectCatalog.FlightControl] = "Um comando de voo foi danificado",
            [DefectCatalog.FuelSystem] = "O sistema de combustível parou de funcionar",
            [DefectCatalog.Autopilot] = "O piloto automático parou de funcionar",
        };

        public static string Friendly(AircraftDefect d)
        {
            if (d == null) return string.Empty;

            string momento = GetMomento(d.FlightPhase);

            string baseText;
            if (!string.IsNullOrEmpty(d.Code) && Bases.TryGetValue(d.Code, out var found))
            {
                baseText = found;
            }
            else if (!string.IsNullOrEmpty(d.Code) && DefectCatalog.IsInspection(d.Code))
            {
                baseText = "Inspeção pendente após uma ocorrência";
            }
            else
            {
                baseText = "Defeito registrado";
            }

            return $"{baseText}{momento}.";
        }

        private static string GetMomento(string? flightPhase)
        {
            if (string.IsNullOrWhiteSpace(flightPhase)) return string.Empty;

            var phase = flightPhase.Trim();
            if (phase.StartsWith("VOANDO", StringComparison.OrdinalIgnoreCase))
                return " em voo";
            if (phase.StartsWith("POUSO", StringComparison.OrdinalIgnoreCase))
                return " no pouso";
            if (phase.StartsWith("ACIDENTE", StringComparison.OrdinalIgnoreCase))
                return " no acidente";
            if (phase.StartsWith("PREPARAÇÃO", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("PREPARACAO", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("INICIANDO", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("TÁXI", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("TAXI", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("SOLO", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("FINALIZANDO", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("CANCELANDO", StringComparison.OrdinalIgnoreCase) ||
                phase.StartsWith("VOO CANCELADO", StringComparison.OrdinalIgnoreCase))
                return " em solo";

            return string.Empty;
        }

        public static string Technical(AircraftDefect d)
        {
            if (d == null) return string.Empty;
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(d.OriginLabel)) lines.Add($"Origem: {d.OriginLabel.Trim()}");
            if (!string.IsNullOrWhiteSpace(d.FlightPhase)) lines.Add($"Fase: {d.FlightPhase.Trim()}");
            if (!string.IsNullOrWhiteSpace(d.Cause)) lines.Add($"Causa: {d.Cause.Trim()}");
            if (!string.IsNullOrWhiteSpace(d.Description)) lines.Add(d.Description.Trim());
            return string.Join("\n", lines);
        }
    }
}
