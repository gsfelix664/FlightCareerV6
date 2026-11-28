using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Fuel
{
    public enum FuelIntegrityEventType
    {
        /// <summary>Combustível ilimitado habilitado nas configurações do simulador.</summary>
        UnlimitedFuelActive,
        /// <summary>Abastecimento em voo detectado (quantidade subiu acima da tolerância).</summary>
        InFlightRefueling,
        /// <summary>Combustível no solo diverge do aplicativo além da tolerância (exige sincronização).</summary>
        GroundMismatch
    }

    public record FuelIntegrityEvent(
        FuelIntegrityEventType Type,
        string Message,
        double SimQuantity,
        double BaselineOrExpectedQuantity,
        double SimulatedTimeSeconds,
        bool RequiresSync = false
    );

    /// <summary>
    /// Monitora a integridade do combustível durante o voo e na preparação em solo.
    /// Detecta UNLIMITED FUEL, abastecimento em voo (trapaça/menu) e divergências no solo.
    /// </summary>
    public class FuelIntegrityMonitor
    {
        private readonly double _totalCapacityGal;
        private readonly double _toleranceGal;
        private double? _minInFlightFuel;
        private bool _wasAirborne;
        private readonly List<FuelIntegrityEvent> _events = new();

        public double TotalCapacityGallons => _totalCapacityGal;
        public double ToleranceGallons => _toleranceGal;

        public FuelIntegrityMonitor(double totalCapacityGallons, double? customToleranceGallons = null)
        {
            _totalCapacityGal = Math.Max(0.0, totalCapacityGallons);

            double defaultTol = Math.Max(0.5, _totalCapacityGal * 0.01);
            _toleranceGal = customToleranceGallons.HasValue && customToleranceGallons.Value > 0
                ? customToleranceGallons.Value
                : defaultTol;
        }

        /// <summary>
        /// Atualiza o monitor com os dados atuais de combustível.
        /// </summary>
        public IReadOnlyList<FuelIntegrityEvent> Update(
            double simFuelTotalGal,
            bool unlimitedFuel,
            bool simOnGround,
            double simulatedTimeSeconds,
            double? appFuelGal = null)
        {
            var newEvents = new List<FuelIntegrityEvent>();

            // 1. Trapaça/opção de combustível ilimitado
            if (unlimitedFuel)
            {
                var evt = new FuelIntegrityEvent(
                    FuelIntegrityEventType.UnlimitedFuelActive,
                    "Combustível ilimitado (UNLIMITED FUEL) está ativado no simulador.",
                    simFuelTotalGal,
                    0.0,
                    simulatedTimeSeconds,
                    RequiresSync: false
                );
                newEvents.Add(evt);
                _events.Add(evt);
            }

            // 2. Comportamento no solo vs voo
            if (simOnGround)
            {
                // Reset da referência de voo enquanto no solo
                _minInFlightFuel = null;
                _wasAirborne = false;

                // Checagem de divergência em solo com o combustível registrado no aplicativo
                if (appFuelGal.HasValue)
                {
                    double diff = Math.Abs(simFuelTotalGal - appFuelGal.Value);
                    if (diff > _toleranceGal)
                    {
                        var evt = new FuelIntegrityEvent(
                            FuelIntegrityEventType.GroundMismatch,
                            $"Combustível no simulador ({simFuelTotalGal:F1} gal) diverge do aplicativo ({appFuelGal.Value:F1} gal) além da tolerância ({_toleranceGal:F2} gal).",
                            simFuelTotalGal,
                            appFuelGal.Value,
                            simulatedTimeSeconds,
                            RequiresSync: true
                        );
                        newEvents.Add(evt);
                        _events.Add(evt);
                    }
                }
            }
            else
            {
                // Em voo (!simOnGround)
                if (!_wasAirborne || _minInFlightFuel == null)
                {
                    _minInFlightFuel = simFuelTotalGal;
                    _wasAirborne = true;
                }
                else
                {
                    if (simFuelTotalGal < _minInFlightFuel.Value)
                    {
                        // Consumo normal: novo mínimo em voo
                        _minInFlightFuel = simFuelTotalGal;
                    }
                    else if (simFuelTotalGal > _minInFlightFuel.Value + _toleranceGal)
                    {
                        // Quantidade subiu acima do mínimo visto + tolerância => abastecimento em voo!
                        var evt = new FuelIntegrityEvent(
                            FuelIntegrityEventType.InFlightRefueling,
                            $"Abastecimento em voo detectado: combustível subiu de {_minInFlightFuel.Value:F1} gal para {simFuelTotalGal:F1} gal (acima da tolerância de {_toleranceGal:F2} gal).",
                            simFuelTotalGal,
                            _minInFlightFuel.Value,
                            simulatedTimeSeconds,
                            RequiresSync: false
                        );
                        newEvents.Add(evt);
                        _events.Add(evt);

                        // Atualiza o piso para evitar disparos repetidos a cada frame sem novo abastecimento
                        _minInFlightFuel = simFuelTotalGal;
                    }
                }
            }

            return newEvents;
        }

        public IReadOnlyList<FuelIntegrityEvent> DrainEvents()
        {
            if (_events.Count == 0) return Array.Empty<FuelIntegrityEvent>();
            var copy = new List<FuelIntegrityEvent>(_events);
            _events.Clear();
            return copy;
        }

        public void Reset()
        {
            _minInFlightFuel = null;
            _wasAirborne = false;
            _events.Clear();
        }
    }
}
