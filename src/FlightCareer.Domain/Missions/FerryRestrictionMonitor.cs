using System;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Monitora restrições técnicas operacionais em voos com Special Flight Permit:
    /// - "max_altitude": teto operacional estrito (falha se altitude > MaxAltitudeFt + 200 por > 10 s acumulados);
    /// - "flaps_fixed": flaps devem permanecer na posição da decolagem (falha se variação > FlapTolerancePct).
    /// </summary>
    public sealed class FerryRestrictionMonitor
    {
        private FerryPermitContractData? _data;
        private bool _hasTakenOff;
        private bool _wasOnGround = true;
        private double _initialFlapsPercent;
        private double _altitudeExceedanceSeconds;
        private bool _hasViolation;
        private string? _failureReason;

        public bool HasActiveContract => _data != null && !string.IsNullOrWhiteSpace(_data.Restriction);
        public bool HasViolation => _hasViolation;
        public string? FailureReason => _failureReason;
        public double AltitudeExceedanceSeconds => _altitudeExceedanceSeconds;
        public FerryPermitContractData? ContractData => _data;

        public void Configure(FerryPermitContractData? data, bool initialIsOnGround = true)
        {
            _data = data;
            Reset(initialIsOnGround);
        }

        public void Reset(bool initialIsOnGround = true)
        {
            _hasTakenOff = !initialIsOnGround;
            _wasOnGround = initialIsOnGround;
            _initialFlapsPercent = 0.0;
            _altitudeExceedanceSeconds = 0.0;
            _hasViolation = false;
            _failureReason = null;
        }

        public void Update(bool isOnGround, double altitudeMslFt, double flapsPercent, double simDtSeconds)
        {
            if (_data == null || simDtSeconds <= 0)
            {
                _wasOnGround = isOnGround;
                return;
            }

            // Registra flaps na decolagem
            if (_wasOnGround && !isOnGround)
            {
                if (!_hasTakenOff)
                {
                    _initialFlapsPercent = flapsPercent;
                    _hasTakenOff = true;
                }
            }

            if (!_hasViolation && !isOnGround)
            {
                if (string.Equals(_data.Restriction, "max_altitude", StringComparison.OrdinalIgnoreCase))
                {
                    double ceilingWithMargin = _data.MaxAltitudeFt + 200.0;
                    if (altitudeMslFt > ceilingWithMargin)
                    {
                        _altitudeExceedanceSeconds += simDtSeconds;
                        if (_altitudeExceedanceSeconds > 10.0)
                        {
                            _hasViolation = true;
                            _failureReason = $"Restrição técnica violada: teto de {_data.MaxAltitudeFt:N0} ft MSL excedido por mais de 10 s ({_altitudeExceedanceSeconds:F1} s acumulados).";
                        }
                    }
                }
                else if (string.Equals(_data.Restriction, "flaps_fixed", StringComparison.OrdinalIgnoreCase))
                {
                    if (_hasTakenOff)
                    {
                        double diff = Math.Abs(flapsPercent - _initialFlapsPercent);
                        if (diff > _data.FlapTolerancePct)
                        {
                            _hasViolation = true;
                            _failureReason = $"Restrição técnica violada: flaps alterados de {_initialFlapsPercent:F0}% para {flapsPercent:F0}% (tolerância: ±{_data.FlapTolerancePct:F0}%).";
                        }
                    }
                }
            }

            _wasOnGround = isOnGround;
        }
    }
}
