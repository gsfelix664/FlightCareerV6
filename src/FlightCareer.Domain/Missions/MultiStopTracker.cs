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
        private double _rollDurationSeconds;
        private double _maxGsDuringRoll;
        private bool _fullStopOccurredDuringRoll;
        private double _minDistTargetDuringRoll = double.MaxValue;
        private string? _futureStopVisitedDuringRoll;

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
            _rollDurationSeconds = 0.0;
            _maxGsDuringRoll = 0.0;
            _fullStopOccurredDuringRoll = false;
            _minDistTargetDuringRoll = double.MaxValue;
            _futureStopVisitedDuringRoll = null;
        }

        public void Update(bool isOnGround, double lat, double lon, double groundSpeedKt, double fuelTotalGallons, double simDtSeconds)
        {
            if (_data == null || _data.Stops.Count == 0 || simDtSeconds <= 0)
            {
                _wasOnGround = isOnGround;
                return;
            }

            // 1. Rastreio de toque, rolagem e decolagem
            double distTarget = double.MaxValue;
            if (_currentStopIndex < _data.Stops.Count)
            {
                var target = _data.Stops[_currentStopIndex];
                distTarget = OverflyTracker.CalculateDistanceNM(lat, lon, target.Lat, target.Lon);
            }

            if (!_wasOnGround && isOnGround)
            {
                // Transição AR -> SOLO (toque na pista)
                _rollDurationSeconds = simDtSeconds;
                _maxGsDuringRoll = groundSpeedKt;
                _fullStopOccurredDuringRoll = groundSpeedKt < TouchAndGoCounter.FullStopSpeedThresholdKt;
                _minDistTargetDuringRoll = distTarget;
                _futureStopVisitedDuringRoll = null;

                for (int f = _currentStopIndex + 1; f < _data.Stops.Count; f++)
                {
                    var fStop = _data.Stops[f];
                    if (OverflyTracker.CalculateDistanceNM(lat, lon, fStop.Lat, fStop.Lon) <= 1.5)
                    {
                        _futureStopVisitedDuringRoll = fStop.Icao;
                        break;
                    }
                }
            }
            else if (_wasOnGround && isOnGround)
            {
                // Durante a corrida no solo
                _rollDurationSeconds += simDtSeconds;
                if (groundSpeedKt > _maxGsDuringRoll)
                {
                    _maxGsDuringRoll = groundSpeedKt;
                }
                if (groundSpeedKt < TouchAndGoCounter.FullStopSpeedThresholdKt)
                {
                    _fullStopOccurredDuringRoll = true;
                }
                if (distTarget < _minDistTargetDuringRoll)
                {
                    _minDistTargetDuringRoll = distTarget;
                }
                for (int f = _currentStopIndex + 1; f < _data.Stops.Count; f++)
                {
                    var fStop = _data.Stops[f];
                    if (OverflyTracker.CalculateDistanceNM(lat, lon, fStop.Lat, fStop.Lon) <= 1.5)
                    {
                        _futureStopVisitedDuringRoll ??= fStop.Icao;
                    }
                }
            }
            else if (_wasOnGround && !isOnGround)
            {
                // Transição SOLO -> AR (decolagem)
                if (!_hasTakenOffOnce)
                {
                    _hasTakenOffOnce = true;
                    if (_minFuelSinceTakeoff < 0 && fuelTotalGallons > 0)
                    {
                        _minFuelSinceTakeoff = fuelTotalGallons;
                    }
                }
                else
                {
                    bool isTglMode = string.Equals(_data.StopMode, "touch_and_go", StringComparison.OrdinalIgnoreCase);
                    if (isTglMode && !_waitingAirborne && _currentStopIndex < _data.Stops.Count)
                    {
                        bool validTgl = TouchAndGoCounter.IsValidTouchAndGo(_rollDurationSeconds, _maxGsDuringRoll, _fullStopOccurredDuringRoll);
                        if (validTgl)
                        {
                            if (_futureStopVisitedDuringRoll != null)
                            {
                                _orderViolation = true;
                                _failureReason ??= $"Escala {_futureStopVisitedDuringRoll} visitada antes de {_data.Stops[_currentStopIndex].Icao}.";
                            }
                            else if (_minDistTargetDuringRoll <= 1.5)
                            {
                                _currentStopIndex++;
                                _groundStopSeconds = 0.0;
                                _waitingAirborne = true;
                                _airborneSeconds = 0.0;
                            }
                        }
                    }
                }

                _rollDurationSeconds = 0.0;
                _maxGsDuringRoll = 0.0;
                _fullStopOccurredDuringRoll = false;
                _minDistTargetDuringRoll = double.MaxValue;
                _futureStopVisitedDuringRoll = null;
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
                distTarget = OverflyTracker.CalculateDistanceNM(lat, lon, targetStop.Lat, targetStop.Lon);

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
