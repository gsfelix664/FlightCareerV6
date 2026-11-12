namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Componentes (barras de saúde) individuais de uma aeronave.
    /// Ignição se aplica a motores a pistão; Seção Quente a turboélices e jatos.
    /// </summary>
    public enum ComponentKind
    {
        Engine = 0,       // Motor (núcleo)
        Oil = 1,          // Óleo e lubrificação
        Ignition = 2,     // Ignição / magnetos (pistão)
        HotSection = 3,   // Seção quente (turbina)
        Electrical = 4,   // Elétrico (alternador/gerador e bateria)
        LandingGear = 5,  // Trem de pouso
        Tires = 6,        // Pneus
        Brakes = 7,       // Freios
        Airframe = 8      // Célula / estrutura
    }

    public enum EngineClass
    {
        Piston = 0,
        Turboprop = 1,
        Jet = 2
    }

    /// <summary>Faixa de operação de um parâmetro em relação aos limites da aeronave.</summary>
    public enum StressBand
    {
        Green = 0,
        Yellow = 1,
        Red = 2
    }
}
