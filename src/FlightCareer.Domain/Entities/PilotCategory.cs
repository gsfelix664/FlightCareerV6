namespace FlightCareer.Domain.Entities
{
    public enum PilotCategory
    {
        Category1_FlightSchool = 1,  // CIAC / Aeroclube - Instrução de Alunos (PP/PC/INVA)
        Category2_SAE = 2,           // Serviços Aéreos Especializados & Pequenas Cargas
        Category3_AirTaxi = 3,       // Táxi Aéreo / Charter & Carga Fechada
        Category4_Regional = 4,      // Linhas Regionais & Sub-regulares
        Category5_MajorAirline = 5   // Grande Linha Aérea Regular
    }

    // Enum mantido para compatibilidade com código existente
    public enum CompanyTier
    {
        Tier1_FlightSchool = 1,
        Tier2_SAE = 2,
        Tier3_AirTaxi = 3,
        Tier4_Regional = 4,
        Tier5_MajorAirline = 5
    }

    public static class PilotCategoryExtensions
    {
        public static string GetDisplayName(this PilotCategory category)
        {
            return category switch
            {
                PilotCategory.Category1_FlightSchool => "Categoria 1: CIAC / Escola de Aviação",
                PilotCategory.Category2_SAE => "Categoria 2: Serviços Especializados (SAE)",
                PilotCategory.Category3_AirTaxi => "Categoria 3: Táxi Aéreo & Charter",
                PilotCategory.Category4_Regional => "Categoria 4: Linha Aérea Regional",
                PilotCategory.Category5_MajorAirline => "Categoria 5: Linha Aérea Regular",
                _ => "Categoria Desconhecida"
            };
        }

        public static string GetShortName(this PilotCategory category)
        {
            return category switch
            {
                PilotCategory.Category1_FlightSchool => "Cat 1: CIAC / Escola",
                PilotCategory.Category2_SAE => "Cat 2: SAE & Carga",
                PilotCategory.Category3_AirTaxi => "Cat 3: Táxi Aéreo",
                PilotCategory.Category4_Regional => "Cat 4: Linha Regional",
                PilotCategory.Category5_MajorAirline => "Cat 5: Linha Regular",
                _ => "Outro"
            };
        }

        public static string GetBadgeColor(this PilotCategory category)
        {
            return category switch
            {
                PilotCategory.Category1_FlightSchool => "#3B82F6", // Azul
                PilotCategory.Category2_SAE => "#10B981",          // Verde Esmeralda
                PilotCategory.Category3_AirTaxi => "#F59E0B",      // Laranja / Âmbar
                PilotCategory.Category4_Regional => "#8B5CF6",     // Roxo
                PilotCategory.Category5_MajorAirline => "#EC4899", // Rosa / Magenta
                _ => "#6B7280"
            };
        }

        public static double GetPromotionCost(this PilotCategory currentCategory)
        {
            return currentCategory switch
            {
                PilotCategory.Category1_FlightSchool => 35000.0,    // Homologação SAE (ANAC / FAA)
                PilotCategory.Category2_SAE => 120000.0,            // Certificação Táxi Aéreo / SGSO / DTS
                PilotCategory.Category3_AirTaxi => 500000.0,        // Certificação Linha Regional / DOV / Handling
                PilotCategory.Category4_Regional => 2000000.0,      // Linha Regular / Slot Hubs / IATA
                _ => 0.0
            };
        }

        public static double GetRequiredHours(this PilotCategory currentCategory)
        {
            return currentCategory switch
            {
                PilotCategory.Category1_FlightSchool => 40.0,   // PP formado
                PilotCategory.Category2_SAE => 150.0,           // Comercial
                PilotCategory.Category3_AirTaxi => 500.0,       // IFR + Experiência
                PilotCategory.Category4_Regional => 1500.0,     // PLA
                _ => 0.0
            };
        }

        public static int GetRequiredXP(this PilotCategory currentCategory)
        {
            return currentCategory switch
            {
                PilotCategory.Category1_FlightSchool => 2500,     // Formação Básica e Prática
                PilotCategory.Category2_SAE => 12000,             // Proficiência Comercial
                PilotCategory.Category3_AirTaxi => 45000,         // Experiência em Rotas e Gestão
                PilotCategory.Category4_Regional => 150000,       // Alta Performance e Precisão IFR
                _ => 0
            };
        }

        // Extensões para manter compatibilidade com CompanyTier
        public static string GetDisplayName(this CompanyTier tier) => ((PilotCategory)(int)tier).GetDisplayName();
        public static string GetShortName(this CompanyTier tier) => ((PilotCategory)(int)tier).GetShortName();
        public static string GetBadgeColor(this CompanyTier tier) => ((PilotCategory)(int)tier).GetBadgeColor();
        public static double GetPromotionCost(this CompanyTier tier) => ((PilotCategory)(int)tier).GetPromotionCost();
        public static double GetRequiredHours(this CompanyTier tier) => ((PilotCategory)(int)tier).GetRequiredHours();
        public static int GetRequiredXP(this CompanyTier tier) => ((PilotCategory)(int)tier).GetRequiredXP();
    }
}
