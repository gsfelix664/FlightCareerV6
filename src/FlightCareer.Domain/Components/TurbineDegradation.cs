using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Perdas percentuais de eficiência e empuxo de um motor a turbina (0 a 100%).
    /// </summary>
    public sealed record TurbineLosses(
        double N1LossPct,
        double FuelEfficiencyLossPct,
        double IttCoolingLossPct,
        double ThrustEfficiencyLossPct)
    {
        public static readonly TurbineLosses None = new(0.0, 0.0, 0.0, 0.0);

        public bool IsZero => Math.Abs(N1LossPct) < 1e-4 &&
                              Math.Abs(FuelEfficiencyLossPct) < 1e-4 &&
                              Math.Abs(IttCoolingLossPct) < 1e-4 &&
                              Math.Abs(ThrustEfficiencyLossPct) < 1e-4;

        public bool HasSignificantDifference(TurbineLosses other, double toleranceStep)
        {
            double tol = Math.Max(1e-4, toleranceStep);
            return Math.Abs(N1LossPct - other.N1LossPct) >= tol ||
                   Math.Abs(FuelEfficiencyLossPct - other.FuelEfficiencyLossPct) >= tol ||
                   Math.Abs(IttCoolingLossPct - other.IttCoolingLossPct) >= tol ||
                   Math.Abs(ThrustEfficiencyLossPct - other.ThrustEfficiencyLossPct) >= tol;
        }
    }

    /// <summary>
    /// Configuração de calibração das perdas de degradação da turbina (lida de component_catalog.json).
    /// </summary>
    public sealed class TurbineDegradationConfig
    {
        public double StartHealthPct { get; set; } = 60.0;
        public double MaxN1LossPct { get; set; } = 5.0;
        public double MaxFuelEfficiencyLossPct { get; set; } = 10.0;
        public double MaxIttCoolingLossPct { get; set; } = 15.0;
        public double MaxThrustEfficiencyLossPct { get; set; } = 8.0;
        public double WriteToleranceStepPct { get; set; } = 0.5;
        public string? Source { get; set; }
    }

    /// <summary>
    /// Cálculo puro e determinístico de perdas de desempenho para motores a turbina.
    /// </summary>
    public static class TurbineDegradation
    {
        /// <summary>
        /// Calcula as perdas de turbina a partir da saúde de Motor e Seção Quente.
        /// </summary>
        public static TurbineLosses Compute(double engineHealthPct, double hotSectionHealthPct, TurbineDegradationConfig? cfg)
        {
            if (cfg == null || cfg.StartHealthPct <= 0.0)
            {
                return TurbineLosses.None;
            }

            double fEngine = ComputeFactor(engineHealthPct, cfg.StartHealthPct);
            double fHotSection = ComputeFactor(hotSectionHealthPct, cfg.StartHealthPct);
            double worstHealth = Math.Min(engineHealthPct, hotSectionHealthPct);
            double fWorst = ComputeFactor(worstHealth, cfg.StartHealthPct);

            double rawN1 = cfg.MaxN1LossPct * fEngine;
            double rawFuel = cfg.MaxFuelEfficiencyLossPct * fWorst;
            double rawItt = cfg.MaxIttCoolingLossPct * fHotSection;
            double rawThrust = cfg.MaxThrustEfficiencyLossPct * fEngine;

            double step = cfg.WriteToleranceStepPct;
            return new TurbineLosses(
                RoundToStep(rawN1, step),
                RoundToStep(rawFuel, step),
                RoundToStep(rawItt, step),
                RoundToStep(rawThrust, step));
        }

        /// <summary>
        /// Determina se uma nova escrita deve ser enviada ao simulador.
        /// </summary>
        public static bool ShouldWrite(
            TurbineLosses target,
            TurbineLosses? lastWritten,
            TurbineLosses? currentSimValues,
            TimeSpan elapsedSinceLastWrite,
            double toleranceStep,
            TimeSpan minInterval)
        {
            // Proteção de tráfego de rede/mensagens
            if (elapsedSinceLastWrite < minInterval)
            {
                return false;
            }

            // Primeira escrita ou o alvo mudou significativamente
            if (lastWritten == null || target.HasSignificantDifference(lastWritten, toleranceStep))
            {
                return true;
            }

            // O simulador divergiu do alvo (ex.: sim zerou ou foi recarregado)
            if (currentSimValues != null && target.HasSignificantDifference(currentSimValues, toleranceStep))
            {
                return true;
            }

            return false;
        }

        private static double ComputeFactor(double healthPct, double startHealthPct)
        {
            if (healthPct >= startHealthPct) return 0.0;
            return Math.Clamp((startHealthPct - healthPct) / startHealthPct, 0.0, 1.0);
        }

        private static double RoundToStep(double value, double step)
        {
            if (step <= 0.0) return value;
            double rounded = Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
            return Math.Round(rounded, 6);
        }
    }
}
