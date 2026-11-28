using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Rastreia os passos sequenciais de um voo de teste pós-manutenção:
    /// Passo 1: Atingir altitude de teste (+/- tol)
    /// Passo 2: Manter potência máxima contínua por PowerHoldSeconds
    /// Passo 3: Manter cada proa cardinal (N, E, S, W) por HeadingHoldSeconds dentro da faixa de altitude
    /// Passo 4: Retorno e pouso na origem (avaliado no MissionCompletion)
    /// </summary>
    public sealed class CheckFlightTracker
    {
        private CheckFlightContractData? _data;
        private bool _step1Reached;
        private bool _step2PowerChecked;
        private double _powerHoldTimer;

        private bool _heldN;
        private bool _heldE;
        private bool _heldS;
        private bool _heldW;

        private double _headingTimerN;
        private double _headingTimerE;
        private double _headingTimerS;
        private double _headingTimerW;

        public bool HasActiveContract => _data != null && _data.TestAltitudeFt > 0;
        public bool Step1Reached => _step1Reached;
        public bool Step2PowerChecked => _step2PowerChecked;
        public bool Step3HeadingsChecked => _heldN && _heldE && _heldS && _heldW;
        public bool IsCompleted => Step1Reached && Step2PowerChecked && Step3HeadingsChecked;

        public bool HeldN => _heldN;
        public bool HeldE => _heldE;
        public bool HeldS => _heldS;
        public bool HeldW => _heldW;

        public CheckFlightContractData? ContractData => _data;

        public void Configure(CheckFlightContractData? data)
        {
            _data = data;
            Reset();
        }

        public void Reset()
        {
            _step1Reached = false;
            _step2PowerChecked = false;
            _powerHoldTimer = 0.0;
            _heldN = false;
            _heldE = false;
            _heldS = false;
            _heldW = false;
            _headingTimerN = 0.0;
            _headingTimerE = 0.0;
            _headingTimerS = 0.0;
            _headingTimerW = 0.0;
        }

        public void Update(
            bool isOnGround,
            double altitudeMslFt,
            double planeHeadingDeg,
            double engineRpm,
            double maxRpm,
            double engineN1,
            double engineType,
            double simDtSeconds)
        {
            if (_data == null || simDtSeconds <= 0 || isOnGround) return;

            bool inAltitudeBand = Math.Abs(altitudeMslFt - _data.TestAltitudeFt) <= _data.AltitudeToleranceFt;

            // Passo 1: Atingir altitude de teste
            if (!_step1Reached && inAltitudeBand)
            {
                _step1Reached = true;
            }

            // Passo 2: Potência máxima por PowerHoldSeconds
            if (_step1Reached && !_step2PowerChecked)
            {
                bool isTurbine = engineType != 0.0; // 0 = PISTON no SimConnect
                bool maxPowerActive = isTurbine
                    ? engineN1 >= 90.0
                    : (maxRpm > 0 ? (engineRpm >= 0.95 * maxRpm) : (engineRpm >= 2400.0));

                if (maxPowerActive)
                {
                    _powerHoldTimer += simDtSeconds;
                    if (_powerHoldTimer >= _data.PowerHoldSeconds)
                    {
                        _step2PowerChecked = true;
                    }
                }
                else
                {
                    _powerHoldTimer = 0.0;
                }
            }

            // Passo 3: Roteiro cardinal (N, E, S, W) dentro da faixa de altitude
            if (_step2PowerChecked && !Step3HeadingsChecked)
            {
                if (inAltitudeBand)
                {
                    // Rumo Norte (0° ou 360°)
                    if (!_heldN)
                    {
                        if (AngleDiff(planeHeadingDeg, 0.0) <= _data.HeadingToleranceDeg)
                        {
                            _headingTimerN += simDtSeconds;
                            if (_headingTimerN >= _data.HeadingHoldSeconds) _heldN = true;
                        }
                        else
                        {
                            _headingTimerN = 0.0;
                        }
                    }

                    // Rumo Leste (90°)
                    if (!_heldE)
                    {
                        if (AngleDiff(planeHeadingDeg, 90.0) <= _data.HeadingToleranceDeg)
                        {
                            _headingTimerE += simDtSeconds;
                            if (_headingTimerE >= _data.HeadingHoldSeconds) _heldE = true;
                        }
                        else
                        {
                            _headingTimerE = 0.0;
                        }
                    }

                    // Rumo Sul (180°)
                    if (!_heldS)
                    {
                        if (AngleDiff(planeHeadingDeg, 180.0) <= _data.HeadingToleranceDeg)
                        {
                            _headingTimerS += simDtSeconds;
                            if (_headingTimerS >= _data.HeadingHoldSeconds) _heldS = true;
                        }
                        else
                        {
                            _headingTimerS = 0.0;
                        }
                    }

                    // Rumo Oeste (270°)
                    if (!_heldW)
                    {
                        if (AngleDiff(planeHeadingDeg, 270.0) <= _data.HeadingToleranceDeg)
                        {
                            _headingTimerW += simDtSeconds;
                            if (_headingTimerW >= _data.HeadingHoldSeconds) _heldW = true;
                        }
                        else
                        {
                            _headingTimerW = 0.0;
                        }
                    }
                }
                else
                {
                    // Fora da faixa de altitude, os temporizadores de rumo zeram
                    _headingTimerN = 0.0;
                    _headingTimerE = 0.0;
                    _headingTimerS = 0.0;
                    _headingTimerW = 0.0;
                }
            }
        }

        public string? GetFailureReason()
        {
            if (IsCompleted) return null;
            if (_data == null) return "Contrato de voo de teste inválido.";

            if (!_step1Reached)
            {
                return $"Passo 1 não cumprido: não atingiu a altitude de teste ({_data.TestAltitudeFt:N0} ft ± {_data.AltitudeToleranceFt:F0} ft).";
            }

            if (!_step2PowerChecked)
            {
                return $"Passo 2 não cumprido: não manteve potência máxima contínua por {_data.PowerHoldSeconds:F0} s.";
            }

            if (!Step3HeadingsChecked)
            {
                var missing = new List<string>();
                if (!_heldN) missing.Add("N (360°)");
                if (!_heldE) missing.Add("E (090°)");
                if (!_heldS) missing.Add("S (180°)");
                if (!_heldW) missing.Add("W (270°)");
                return $"Passo 3 não cumprido: rumo(s) não mantido(s) por {_data.HeadingHoldSeconds:F0} s na altitude: {string.Join(", ", missing)}.";
            }

            return "Voo de teste pós-manutenção incompleto.";
        }

        public static double AngleDiff(double a, double b)
        {
            double diff = Math.Abs((a - b + 180.0) % 360.0 - 180.0);
            return diff;
        }
    }
}
