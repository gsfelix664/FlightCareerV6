using System;
using FlightCareer.Domain.Career;

namespace FlightCareer.Domain.Entities
{
    public enum CareerPhase
    {
        Phase1_FlightSchool = 1,                 // Escola de aviação / aeroclube (Níveis 1-5, PPL)
        Phase2_SAE = 2,                          // Serviços aéreos especializados (Níveis 6-12, CPL)
        Phase3_AirTaxi = 3,                      // Táxi aéreo e carga leve (Níveis 13-20, MLTE/IFR)
        Phase4_HighPerformanceAndMedevac = 4,    // Executiva de alta performance e MEDEVAC (Níveis 21-28, Type rating/RVSM)
        Phase5_Commuter = 5,                     // Regional commuter (Níveis 29-36, ATPL regional)
        Phase6_DomesticAirline = 6,              // Linha aérea doméstica narrowbody (Níveis 37-44, ATPL + Type)
        Phase7_InternationalAirline = 7          // Linha aérea internacional widebody (Níveis 45+, Widebody + ETOPS)
    }

    public static class CareerPhaseExtensions
    {
        public static string GetDisplayName(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).Name;
        }

        public static string GetShortName(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).ShortName;
        }

        public static string GetLicenseDisplay(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).LicenseDisplay;
        }

        public static string GetBadgeColor(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).BadgeColor;
        }

        public static int GetEntryLevel(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).EntryLevel;
        }

        public static double GetMinHours(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).MinHours;
        }

        public static double GetPromotionCost(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).PromotionCost;
        }

        public static string GetOperationalDescription(this CareerPhase phase)
        {
            return CareerPhaseCatalog.Instance.GetPhase(phase).OperationalDescription;
        }

        /// <summary>
        /// Retorna a representação formatada completa para telas: "Fase N — nome · Nível L · licença"
        /// </summary>
        public static string FormatPhaseStatus(this CareerPhase phase, int currentLevel)
        {
            var def = CareerPhaseCatalog.Instance.GetPhase(phase);
            return $"Fase {(int)phase} — {def.Name} · Nível {currentLevel} · {def.LicenseDisplay}";
        }
    }
}
