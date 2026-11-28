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
        Airframe = 8,     // Célula / estrutura
        Instruments = 9   // Instrumentos (vácuo, pitot, estática)
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

    /// <summary>
    /// Efeito que uma pane aplica no simulador. Só existem eventos de falha no SimConnect para
    /// motor, elétrico, vácuo, pitot, estática, hidráulico e freios; os demais efeitos são feitos
    /// forçando chaves/comandos (magneto, alternador, flaps) ou ficam apenas no app (None).
    /// </summary>
    public enum FailureEffect
    {
        None = 0,              // Só alerta e dano no componente
        EngineFailure,         // TOGGLE_ENGINE1_FAILURE
        EngineFire,            // ENG ON FIRE:1 (ciclo de fogo progressivo)
        OilLeak,               // GENERAL ENG OIL LEAKED PERCENT:1 (EffectValue = %/min)
        MagnetoLeft,           // Chave mantida fora do magneto esquerdo
        MagnetoRight,          // Chave mantida fora do magneto direito
        AlternatorOff,         // Chave ALT mantida desligada
        ElectricalFailure,     // TOGGLE_ELECTRICAL_FAILURE
        VacuumFailure,         // TOGGLE_VACUUM_FAILURE
        PitotBlockage,         // TOGGLE_PITOT_BLOCKAGE
        StaticBlockage,        // TOGGLE_STATIC_PORT_BLOCKAGE
        FlapJam,               // Alavanca de flap mantida na posição
        BrakeFailureOneSide,   // TOGGLE_LEFT/RIGHT_BRAKE_FAILURE (lado sorteado)
        BrakeFailureTotal,     // TOGGLE_TOTAL_BRAKE_FAILURE
        HydraulicFailure       // TOGGLE_HYDRAULIC_FAILURE
    }
}
