namespace FlightCareer.Domain.Components
{
    public static class HealthWords
    {
        // v <= 0 → Quebrado/Quebrada; v < redBelow → Crítico/Crítica; v < yellowBelow → Atenção;
        // v < goodAbove → Bom/Boa; senão Íntegro/Íntegra
        public static string For(double health, bool feminine = false, double redBelow = 20.0, double yellowBelow = 40.0, double goodAbove = 80.0)
        {
            if (health <= 0.0)
                return feminine ? "Quebrada" : "Quebrado";
            if (health < redBelow)
                return feminine ? "Crítica" : "Crítico";
            if (health < yellowBelow)
                return "Atenção";
            if (health < goodAbove)
                return feminine ? "Boa" : "Bom";
            return feminine ? "Íntegra" : "Íntegro";
        }
    }
}
