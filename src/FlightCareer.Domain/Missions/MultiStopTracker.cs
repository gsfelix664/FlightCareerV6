using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Rastreia o cumprimento sequencial de escalas intermediárias com parada completa (GS < 3 kt por >= 5 s)
    /// e monitora a integridade de combustível (sem reabastecimento além da margem de ruído entre escalas).
    /// </summary>
    public sealed class MultiStopTracker
    {
        private MultiStopContractData? _data;
        private int _currentStopIndex;
        private bool _waitingAirborne;
        private double _groundStopSeconds;
        private double _airborneSeconds;
        private bool _hasTakenOffOnce;
        private bool _wasOnGround = true;
        private double _minFuelSinceTakeoff = -1.0;
        private bool _refuelViolation;
        private bool _orderViolation;
        private string? _failureReason;

        public bool HasActiveContract => _data != null && _data.Stops.Count > 0;
        public int CompletedStopsCount => _currentStopIndex;
        public int TotalStopsCount => _data?.Stops.Count ?? 0;
        public bool AllStopsCompleted => HasActiveContract && _currentStopIndex >= TotalStopsCount && !_orderViolation;
        public bool RefuelViolationDetected => _refuelViolation;
        public bool OrderViolationDetected => _orderViolation;
        public string? FailureReason => _failureReason ?? (_currentStopIndex < TotalStopsCount && _data != null
            ? $"Escala {_data.Stops[_currentStopIndex].Icao} não visitada."
            : null);
        public string? NextStopIcao => (_data != null && _currentStopIndex < _data.Stops.Count)
            ? _data.Stops[_currentStopIndex].Icao
            : null;
        public MultiStopContractData? ContractData => _data;

        public void Configure(MultiStopContractData? data, bool initialIsOnGround = true)
        {
            _data = data;
            Reset(initialIsOnGround);
        }

        public void Reset(bool initialIsOnGround = true)
        {
            _currentStopIndex = 0;
            _waitingAirborne = false;
            _groundStopSeconds = 0.0;
            _airborneSeconds = 0.0;
            _wasOnGround = initialIsOnGround;
            _hasTakenOffOnce = !initialIsOnGround;
            _minFuelSinceTakeoff = -1.0;
            _refuelViolation = false;
            _orderViolation = false;
            _failureReason = null;
        }

        public void Update(bool isOnGround, double lat, double lon, double groundSpeedKt, double fuelTotalGallons, double simDtSeconds)
        {
            if (_data == null || _data.Stops.Count == 0 || simDtSeconds <= 0)
            {
                _wasOnGround = isOnGround;
                return;
            }

            // 1. Detecção da primeira decolagem para início da conferência de reabastecimento
            if (_wasOnGround && !isOnGround)
            {
                _hasTakenOffOnce = true;
                if (_minFuelSinceTakeoff < 0 && fuelTotalGallons > 0)
                {
                    _minFuelSinceTakeoff = fuelTotalGallons;
                }
            }

            // 2. Monitoramento de combustível (RequireNoRefuel)
            if (_data.RequireNoRefuel && _hasTakenOffOnce && fuelTotalGallons > 0)
            {
                if (_minFuelSinceTakeoff < 0)
                {
                    _minFuelSinceTakeoff = fuelTotalGallons;
                }
                else
                {
                    double allowedIncrease = Math.Max(1.0, 0.02 * _minFuelSinceTakeoff);
                    if (fuelTotalGallons > _minFuelSinceTakeoff + allowedIncrease)
                    {
                        _refuelViolation = true;
                        _failureReason ??= "Reabasteceu entre escalas.";
                    }
                    else if (fuelTotalGallons < _minFuelSinceTakeoff)
                    {
                        _minFuelSinceTakeoff = fuelTotalGallons;
                    }
                }
            }

            // 3. Aguardar estar no ar por >= 5 s antes de permitir contar a próxima escala
            if (_waitingAirborne)
            {
                if (!isOnGround)
                {
                    _airborneSeconds += simDtSeconds;
                    if (_airborneSeconds >= 5.0)
                    {
                        _waitingAirborne = false;
                        _airborneSeconds = 0.0;
                    }
                }
                else
                {
                    _airborneSeconds = 0.0;
                }

                _wasOnGround = isOnGround;
                return;
            }

            // 4. Se ainda há escalas a cumprir
            if (_currentStopIndex < _data.Stops.Count)
            {
                var targetStop = _data.Stops[_currentStopIndex];
                double distTarget = OverflyTracker.CalculateDistanceNM(lat, lon, targetStop.Lat, targetStop.Lon);

                // Checagem de escala fora de ordem: se o piloto parou em uma escala posterior
                for (int futureIdx = _currentStopIndex + 1; futureIdx < _data.Stops.Count; futureIdx++)
                {
                    var futureStop = _data.Stops[futureIdx];
                    double distFuture = OverflyTracker.CalculateDistanceNM(lat, lon, futureStop.Lat, futureStop.Lon);
                    if (isOnGround && distFuture <= 1.5 && groundSpeedKt < 3.0)
                    {
                        // Está no aeródromo posterior e parou antes da escala esperada
                        _orderViolation = true;
                        _failureReason ??= $"Escala {futureStop.Icao} visitada antes de {targetStop.Icao}.";
                        _wasOnGround = isOnGround;
                        return;
                    }
                }

                if (isOnGround && distTarget <= 1.5 && groundSpeedKt < 3.0)
                {
                    _groundStopSeconds += simDtSeconds;
                    if (_groundStopSeconds >= 5.0)
                    {
                        // Escala cumprida com parada completa!
                        _currentStopIndex++;
                        _groundStopSeconds = 0.0;
                        _waitingAirborne = true;
                        _airborneSeconds = 0.0;
                    }
                }
                else
                {
                    _groundStopSeconds = 0.0;
                }
            }

            _wasOnGround = isOnGround;
        }
    }
}
