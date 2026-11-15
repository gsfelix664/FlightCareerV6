using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Lista fechada de métricas de evento e estresse operacional suportadas pelo catálogo de regras de desgaste.
    /// Define as chaves padronizadas de métricas e como cada uma é calculada a partir dos sensores e telemetria do simulador.
    /// </summary>
    public static class WearRuleMetrics
    {
        /// <summary>
        /// Ciclos de partida de motor a pistão. Derivado da transição de rotação de partida para combustão estabilizada.
        /// </summary>
        public const string EngineStartCycle = "engineStartCycle";

        /// <summary>
        /// Segundos contínuos de engajamento do motor de arranque (starter duty cycle).
        /// </summary>
        public const string StarterSeconds = "starterSeconds";

        /// <summary>
        /// Ciclos de aquecimento de velas aquecedoras em motores diesel antes da partida.
        /// </summary>
        public const string GlowPlugCycles = "glowPlugCycles";

        /// <summary>
        /// Ciclos de partida de turbina / turboélice (starter-generator ou ATS acelerando N1 até ignição).
        /// </summary>
        public const string TurbineStartCycle = "turbineStartCycle";

        /// <summary>
        /// Minutos acumulados com acelerador em alta potência (>70%) enquanto a temperatura do óleo do motor está abaixo da faixa verde/nominal.
        /// </summary>
        public const string HighPowerColdOilMinutes = "highPowerColdOilMinutes";

        /// <summary>
        /// Ciclos de transientes bruscos de manete de potência ou torque (ex.: avanço ou recuo rápido >50% em menos de 1 s).
        /// </summary>
        public const string RapidPowerTransientCycles = "rapidPowerTransientCycles";

        /// <summary>
        /// Picos de sobretensão por acionamento do motor de arranque com a chave mestre de aviônicos ligada (AVIONICS MASTER SWITCH == 1 na subida de combustão/starter).
        /// </summary>
        public const string AvionicsMasterStartSpikes = "avionicsMasterStartSpikes";

        /// <summary>
        /// Minutos acumulados com a tensão da bateria abaixo do limite crítico (&lt;22V em 24V ou &lt;11V em 12V) sem gerador ou GPU alimentando o barramento.
        /// </summary>
        public const string BatteryDeepDischargeMinutes = "batteryDeepDischargeMinutes";

        /// <summary>
        /// Minutos acumulados com corrente total consumida excedendo a capacidade nominal combinada de alternadores/geradores.
        /// </summary>
        public const string ElectricalOverloadMinutes = "electricalOverloadMinutes";

        /// <summary>
        /// Energia cinética total dissipada pelos freios nas frenagens (MJ), calculada por massa da aeronave e variação de velocidade ao frear.
        /// </summary>
        public const string BrakeEnergyMkJ = "brakeEnergyMkJ";

        /// <summary>
        /// Ciclos de inflamento e desinflamento das botas pneumáticas de degelo da asa (STRUCTURAL DEICE SWITCH 0-&gt;1).
        /// </summary>
        public const string DeiceCycles = "deiceCycles";

        /// <summary>
        /// Ciclos de acionamento do sistema térmico de degelo elétrico das pás da hélice (PROP DEICE SWITCH 0-&gt;1).
        /// </summary>
        public const string PropDeiceCycles = "propDeiceCycles";

        /// <summary>
        /// Minutos acumulados de operação contínua de bomba auxiliar/elétrica de combustível com quantidade do tanque associado inferior ao mínimo utilizável (&lt;0.5 gal).
        /// </summary>
        public const string DryRunPumpMinutes = "dryRunPumpMinutes";

        /// <summary>
        /// Ciclos de voo completos (decolagem até pouso, monitorado pela transição de SIM ON GROUND).
        /// </summary>
        public const string FlightCycles = "flightCycles";

        /// <summary>
        /// Ciclos de retração e extensão do trem de pouso (GEAR HANDLE POSITION 0-&gt;1 ou 1-&gt;0).
        /// </summary>
        public const string GearCycles = "gearCycles";

        /// <summary>
        /// Minutos de exposição e acúmulo de gelo na célula (STRUCTURAL ICE PCT &gt; 0).
        /// </summary>
        public const string IcingExposureMinutes = "icingExposureMinutes";

        /// <summary>
        /// Ciclos de pouso / toque das rodas no solo.
        /// </summary>
        public const string LandingCycles = "landingCycles";

        /// <summary>
        /// Minutos acumulados em voo com pressão hidráulica do sistema abaixo da pressão de operação nominal.
        /// </summary>
        public const string LowHydraulicPressureMinutes = "lowHydraulicPressureMinutes";

        /// <summary>
        /// Ciclos de pressurização e despressurização da cabine (diferencial delta-P subindo acima de 1.0 psf e retornando a zero no pouso).
        /// </summary>
        public const string PressurizationCycles = "pressurizationCycles";

        /// <summary>
        /// Variação abrupta de pressão diferencial da cabine em caso de descompressão rápida não programada.
        /// </summary>
        public const string RapidDecompressionDeltaPsf = "rapidDecompressionDeltaPsf";

        /// <summary>
        /// Velocidade de solo (GS em nós) durante curvas fechadas no táxi com deflexão da roda dianteira (GEAR STEER ANGLE PCT).
        /// </summary>
        public const string TaxiTurnSpeedKnots = "taxiTurnSpeedKnots";

        /// <summary>
        /// Ângulo absoluto de inclinação lateral (bank em graus) no momento exato do toque das rodas no solo (avalia risco de wingtip strike).
        /// </summary>
        public const string TouchdownBankDegrees = "touchdownBankDegrees";

        /// <summary>
        /// Velocidade vertical de descida no toque (pés por minuto - fpm), medida pelo sensor de toque rápido.
        /// </summary>
        public const string TouchdownFpm = "touchdownFpm";

        /// <summary>
        /// Velocidade lateral de deriva no toque (pés por segundo - fps), medindo pouso caranguejado com vento cruzado desalinhado.
        /// </summary>
        public const string TouchdownLateralFps = "touchdownLateralFps";

        /// <summary>
        /// Ângulo de arfagem de nariz para cima (pitch em graus) no toque, medindo risco de batida de cauda (tailstrike).
        /// </summary>
        public const string TouchdownPitchDegrees = "touchdownPitchDegrees";

        /// <summary>
        /// Ciclos de comando e compensação em atuadores e parafusos de trim (trim jackscrew).
        /// </summary>
        public const string TrimCycles = "trimCycles";

        /// <summary>
        /// Ciclos de aceleração G fora dos limites operacionais com o piloto automático engajado (AUTOPILOT MASTER == 1 sob turbulência severa).
        /// </summary>
        public const string TurbulenceGExceedanceCycles = "turbulenceGExceedanceCycles";

        /// <summary>
        /// Conjunto com todas as métricas válidas no vocabulário fechado.
        /// </summary>
        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            EngineStartCycle,
            StarterSeconds,
            GlowPlugCycles,
            TurbineStartCycle,
            HighPowerColdOilMinutes,
            RapidPowerTransientCycles,
            AvionicsMasterStartSpikes,
            BatteryDeepDischargeMinutes,
            ElectricalOverloadMinutes,
            BrakeEnergyMkJ,
            DeiceCycles,
            PropDeiceCycles,
            DryRunPumpMinutes,
            FlightCycles,
            GearCycles,
            IcingExposureMinutes,
            LandingCycles,
            LowHydraulicPressureMinutes,
            PressurizationCycles,
            RapidDecompressionDeltaPsf,
            TaxiTurnSpeedKnots,
            TouchdownBankDegrees,
            TouchdownFpm,
            TouchdownLateralFps,
            TouchdownPitchDegrees,
            TrimCycles,
            TurbulenceGExceedanceCycles
        };
    }
}
