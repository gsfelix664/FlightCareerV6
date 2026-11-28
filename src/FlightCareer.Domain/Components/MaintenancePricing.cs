using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Valores regulamentares e custos base de serviços periódicos de manutenção (Fase 3 e ECON-SIM).
    /// Centraliza os preços de inspeções e overhaul com lógica pura compartilhada entre UI, simuladores e testes.
    /// </summary>
    public static class MaintenancePricing
    {
        public const double Cost50hInspection = 212.50;
        public const double Cost100hInspection = 625.0;
        public const double Cost500hInspection = 362.50;
        public const double MinOverhaulCost = 6250.0;
        public const double OverhaulRate = 0.05;
        /// <summary>Fator fixo de compressão temporal da carreira: o tempo da vida real é 4× o do jogo (PARADA-REAL).</summary>
        public const double CompressionFactor = 4.0;
        /// <summary>Converte horas de voo do jogo no prazo equivalente da vida real (× 4).</summary>
        public static double RealHours(double gameHours) => gameHours * CompressionFactor;
        /// <summary>Converte ciclos do jogo nos ciclos equivalentes da vida real (× 4).</summary>
        public static double RealCycles(double gameCycles) => gameCycles * CompressionFactor;

        /// <summary>Intervalos e TBO comprimidos ÷ 4 (decisão do usuário 2026-11-17/19).</summary>
        public const double OilChangeIntervalHours = 12.0;
        public const double AnnualInspectionIntervalHours = 25.0;
        public const double IgnitionRevisionIntervalHours = 125.0;
        public const double PistonTboHours = 500.0;
        public const double TurbineTboHours = 900.0;

        /// <summary>TBO do motor pela classe (pistão 500 h; turboélice e jato 900 h).</summary>
        public static double TboHoursFor(EngineClass engineClass) => engineClass == EngineClass.Piston ? PistonTboHours : TurbineTboHours;

        public const double CostToggleConfiguration = 1200.0;

        /// <summary>
        /// Calcula o custo da retífica geral (overhaul de TBO do motor):
        /// 5% do preço de compra da aeronave, com piso mínimo de $6.250 (compressão ÷ 4, CALIBRA-F4).
        /// </summary>
        public static double CalculateOverhaulCost(double purchasePrice)
        {
            return Math.Max(MinOverhaulCost, Math.Round(purchasePrice * OverhaulRate, 2));
        }
    }
}
