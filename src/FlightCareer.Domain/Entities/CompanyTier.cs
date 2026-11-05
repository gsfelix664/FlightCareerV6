namespace FlightCareer.Domain.Entities
{
    public enum CompanyTier
    {
        Tier1_FlightSchool = 1,  // CIAC / Aeroclube - Instrução de Alunos (PP/PC/INVA)
        Tier2_SAE = 2,           // Serviços Aéreos Especializados & Pequenas Cargas
        Tier3_AirTaxi = 3,       // Táxi Aéreo / Charter & Carga Fechada
        Tier4_Regional = 4,      // Linhas Regionais & Sub-regulares
        Tier5_MajorAirline = 5   // Grande Linha Aérea Regular
    }

    public static class CompanyTierExtensions
    {
        public static string GetDisplayName(this CompanyTier tier)
        {
            return tier switch
            {
                CompanyTier.Tier1_FlightSchool => "Tier 1: CIAC / Escola de Aviação",
                CompanyTier.Tier2_SAE => "Tier 2: Serviços Especializados (SAE)",
                CompanyTier.Tier3_AirTaxi => "Tier 3: Táxi Aéreo & Charter",
                CompanyTier.Tier4_Regional => "Tier 4: Linha Aérea Regional",
                CompanyTier.Tier5_MajorAirline => "Tier 5: Linha Aérea Regular",
                _ => "Tier Desconhecido"
            };
        }

        public static string GetShortName(this CompanyTier tier)
        {
            return tier switch
            {
                CompanyTier.Tier1_FlightSchool => "CIAC / Escola",
                CompanyTier.Tier2_SAE => "SAE & Carga",
                CompanyTier.Tier3_AirTaxi => "Táxi Aéreo",
                CompanyTier.Tier4_Regional => "Linha Regional",
                CompanyTier.Tier5_MajorAirline => "Linha Regular",
                _ => "Outro"
            };
        }

        public static string GetBadgeColor(this CompanyTier tier)
        {
            return tier switch
            {
                CompanyTier.Tier1_FlightSchool => "#3B82F6", // Azul
                CompanyTier.Tier2_SAE => "#10B981",          // Verde Esmeralda
                CompanyTier.Tier3_AirTaxi => "#F59E0B",      // Laranja / Âmbar
                CompanyTier.Tier4_Regional => "#8B5CF6",     // Roxo
                CompanyTier.Tier5_MajorAirline => "#EC4899", // Rosa / Magenta
                _ => "#6B7280"
            };
        }

        public static double GetPromotionCost(this CompanyTier currentTier)
        {
            return currentTier switch
            {
                CompanyTier.Tier1_FlightSchool => 35000.0,    // Homologação SAE (ANAC / FAA)
                CompanyTier.Tier2_SAE => 120000.0,            // Certificação Táxi Aéreo / SGSO / DTS
                CompanyTier.Tier3_AirTaxi => 500000.0,        // Certificação Linha Regional / DOV / Handling
                CompanyTier.Tier4_Regional => 2000000.0,      // Linha Regular / Slot Hubs / IATA
                _ => 0.0
            };
        }

        public static double GetRequiredHours(this CompanyTier currentTier)
        {
            return currentTier switch
            {
                CompanyTier.Tier1_FlightSchool => 40.0,   // PP formado
                CompanyTier.Tier2_SAE => 150.0,           // Comercial
                CompanyTier.Tier3_AirTaxi => 500.0,       // IFR + Experiência
                CompanyTier.Tier4_Regional => 1500.0,     // PLA
                _ => 0.0
            };
        }
    }
}
