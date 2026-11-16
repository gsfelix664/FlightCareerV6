using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Fuel
{
    /// <summary>
    /// Acumula o consumo real de combustível em libras a partir da telemetria
    /// de GENERAL ENG FUEL USED SINCE START:n por motor.
    /// Lida com reinício de motores (contador zera), valores inválidos e múltiplos motores.
    /// </summary>
    public class FuelUsageAccumulator
    {
        private readonly Dictionary<int, double> _accumulatedByEngine = new();
        private readonly Dictionary<int, double> _lastSeenByEngine = new();
        private readonly List<string> _diagnostics = new();

        /// <summary>
        /// Consumo total acumulado somando todos os motores em libras.
        /// </summary>
        public double TotalLbs
        {
            get
            {
                double total = 0.0;
                foreach (var lbs in _accumulatedByEngine.Values)
                {
                    total += lbs;
                }
                return total;
            }
        }

        /// <summary>
        /// Consumo total acumulado convertido em galões usando o peso específico do combustível.
        /// </summary>
        public double TotalGallons(double weightPerGallon)
        {
            if (weightPerGallon <= 0 || double.IsNaN(weightPerGallon) || double.IsInfinity(weightPerGallon))
            {
                _diagnostics.Add($"[FuelUsageAccumulator] Peso de combustível por galão inválido ({weightPerGallon}). Retornando 0 galões.");
                return 0.0;
            }

            return TotalLbs / weightPerGallon;
        }

        /// <summary>
        /// Atualiza o acumulador com os valores de GENERAL ENG FUEL USED SINCE START em libras para cada motor.
        /// </summary>
        public void Update(IReadOnlyList<double>? usedSinceStartLbsPorMotor)
        {
            if (usedSinceStartLbsPorMotor == null || usedSinceStartLbsPorMotor.Count == 0) return;

            for (int i = 0; i < usedSinceStartLbsPorMotor.Count; i++)
            {
                int engineIndex = i + 1;
                double current = usedSinceStartLbsPorMotor[i];

                if (double.IsNaN(current) || double.IsInfinity(current) || current < 0)
                {
                    _diagnostics.Add($"[FuelUsageAccumulator] Valor inválido descartado para motor {engineIndex}: {current}");
                    continue;
                }

                if (!_lastSeenByEngine.TryGetValue(engineIndex, out double lastSeen))
                {
                    // Primeira leitura válida do motor
                    _accumulatedByEngine[engineIndex] = current;
                    _lastSeenByEngine[engineIndex] = current;
                }
                else
                {
                    if (current >= lastSeen)
                    {
                        double delta = current - lastSeen;
                        _accumulatedByEngine[engineIndex] += delta;
                        _lastSeenByEngine[engineIndex] = current;
                    }
                    else
                    {
                        // Reinício do motor: o contador do simulador zerou e recomeçou a contar
                        double delta = current;
                        _accumulatedByEngine[engineIndex] += delta;
                        _lastSeenByEngine[engineIndex] = current;
                        _diagnostics.Add($"[FuelUsageAccumulator] Reinício detectado no motor {engineIndex}: contador caiu de {lastSeen:F2} para {current:F2} lb.");
                    }
                }
            }
        }

        /// <summary>
        /// Drena e limpa a lista de diagnósticos acumulados.
        /// </summary>
        public IReadOnlyList<string> DrainDiagnostics()
        {
            if (_diagnostics.Count == 0) return Array.Empty<string>();
            var copy = new List<string>(_diagnostics);
            _diagnostics.Clear();
            return copy;
        }
    }
}
