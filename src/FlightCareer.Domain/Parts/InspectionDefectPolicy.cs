using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Parts
{
    public static class InspectionDefectPolicy
    {
        // Inspeções de toque/manobra severa interditam a aeronave imediatamente.
        // As demais ficam diferidas até a tarefa EXCEDENCIAS tirar falsos positivos (decisão 2026-11-26).
        private static readonly HashSet<string> TouchdownCodes = new(StringComparer.Ordinal)
        {
            "HARD_LANDING_INSPECTION",
            "TAILSTRIKE_INSPECTION",
            "WINGTIP_STRIKE_INSPECTION",
            "CROSSWIND_LANDING_LOAD_INSPECTION",
            "NOSEWHEEL_STEERING_INSPECTION"
        };

        public static bool IsGrounding(PartWearInspectionEvent e) =>
            e.IsGrounding && TouchdownCodes.Contains(e.DefectCode);

        public static string Title(PartWearInspectionEvent e)
        {
            string desc = !string.IsNullOrWhiteSpace(e.Description) ? e.Description : e.DefectCode;
            return $"Inspeção: {desc}";
        }
    }
}
