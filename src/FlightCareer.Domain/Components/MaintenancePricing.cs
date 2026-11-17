using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Valores regulamentares e custos base de serviços periódicos de manutenção (Fase 3 e ECON-SIM).
    /// Centraliza os preços de inspeções e overhaul com lógica pura compartilhada entre UI, simuladores e testes.
    /// </summary>
    public static class MaintenancePricing
    {
        public const double Cost50hInspection = 850.0;
        public const double Cost100hInspection = 2500.0;
        public const double Cost500hInspection = 1450.0;
        public const double MinOverhaulCost = 25000.0;
        public const double OverhaulRate = 0.20;
        public const double CostToggleConfiguration = 1200.0;

        /// <summary>
        /// Calcula o custo da retífica geral (overhaul de TBO do motor):
        /// 20% do preço de compra da aeronave, com piso mínimo de $25.000.
        /// </summary>
        public static double CalculateOverhaulCost(double purchasePrice)
        {
            return Math.Max(MinOverhaulCost, Math.Round(purchasePrice * OverhaulRate, 2));
        }
    }
}
