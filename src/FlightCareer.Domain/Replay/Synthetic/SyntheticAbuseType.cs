namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Tipos de abusos operacionais e anomalias de dados injetáveis em voos sintéticos.
    /// </summary>
    public enum SyntheticAbuseType
    {
        None = 0,

        // Abusos de motor e sistemas contínuos
        RpmOverspeed,
        ChtOverheat,
        OilTempOverheat,
        OilPressureLow,
        ThermalShock,
        EgtHigh,

        // Abusos aerodinâmicos e estruturais
        FlapOverSpeed,
        VneExceeded,
        PositiveGExceeded,
        NegativeGExceeded,
        HardLanding,

        // Abusos de partidas e elétricos
        LongStart,
        AvionicsMasterOnStart,
        BatteryDischarging,
        ElectricalOverload,
        GearCycles,

        // Anomalias e falhas de dados de sensores
        DataMissingSensor,
        DataNanValues,
        DataFrameGap,
        DataSimPause,
        DataCancelledMidFlight
    }
}
