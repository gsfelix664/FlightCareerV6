using System;
using FlightCareer.Domain.Utils;

namespace FlightCareer.Domain.Integrity
{
    /// <summary>
    /// Veredito retornado pela avaliação periódica de slew e integridade.
    /// </summary>
    public record SlewVerdict(
        bool Allowed,
        double GroundSlewMeters,
        bool Invalidated,
        string? Reason = null)
    {
        public bool UsageCompleted { get; init; }
        public double UsageMeters { get; init; }
    }

    /// <summary>
    /// Monitora o uso do modo slew e congelamentos durante o voo da carreira.
    /// No solo antes da decolagem, permite reposicionamento curto (<= maxGroundSlewMeters).
    /// No ar, após a decolagem ou quando exceder o limite no solo, invalida o voo.
    /// </summary>
    public class SlewMonitor
    {
        private readonly double _maxGroundSlewMeters;
        private double _totalGroundSlewMeters;
        private double _currentUsageMeters;
        private bool _wasSlewActive;
        private double? _lastLat;
        private double? _lastLon;
        private bool _isInvalidated;
        private string? _invalidationReason;

        public double TotalGroundSlewMeters => _totalGroundSlewMeters;
        public double MaxGroundSlewMeters => _maxGroundSlewMeters;
        public bool IsInvalidated => _isInvalidated;
        public string? InvalidationReason => _invalidationReason;

        public SlewMonitor(double maxGroundSlewMeters = 1000.0)
        {
            _maxGroundSlewMeters = Math.Max(0.0, maxGroundSlewMeters);
        }

        /// <summary>
        /// Atualiza o rastreamento de deslocamento por slew.
        /// </summary>
        public SlewVerdict Update(bool slewActive, bool onGround, bool hasTakenOff, double latDeg, double lonDeg)
        {
            if (_isInvalidated)
            {
                return new SlewVerdict(
                    Allowed: false,
                    GroundSlewMeters: _totalGroundSlewMeters,
                    Invalidated: true,
                    Reason: _invalidationReason
                );
            }

            bool usageCompleted = false;
            double completedMeters = 0.0;

            if (slewActive)
            {
                if (!onGround)
                {
                    _isInvalidated = true;
                    _invalidationReason = "slew";
                    return new SlewVerdict(
                        Allowed: false,
                        GroundSlewMeters: _totalGroundSlewMeters,
                        Invalidated: true,
                        Reason: _invalidationReason
                    );
                }

                if (hasTakenOff)
                {
                    _isInvalidated = true;
                    _invalidationReason = "slew";
                    return new SlewVerdict(
                        Allowed: false,
                        GroundSlewMeters: _totalGroundSlewMeters,
                        Invalidated: true,
                        Reason: _invalidationReason
                    );
                }

                // No solo e antes da decolagem
                if (!_wasSlewActive)
                {
                    _wasSlewActive = true;
                    _lastLat = latDeg;
                    _lastLon = lonDeg;
                    _currentUsageMeters = 0.0;
                }
                else
                {
                    if (_lastLat.HasValue && _lastLon.HasValue)
                    {
                        double dist = LocationUtils.CalculateDistanceMeters(_lastLat.Value, _lastLon.Value, latDeg, lonDeg);
                        _totalGroundSlewMeters += dist;
                        _currentUsageMeters += dist;
                    }

                    _lastLat = latDeg;
                    _lastLon = lonDeg;
                }

                if (_totalGroundSlewMeters > _maxGroundSlewMeters)
                {
                    _isInvalidated = true;
                    _invalidationReason = "slew";
                    return new SlewVerdict(
                        Allowed: false,
                        GroundSlewMeters: _totalGroundSlewMeters,
                        Invalidated: true,
                        Reason: _invalidationReason
                    );
                }

                return new SlewVerdict(
                    Allowed: true,
                    GroundSlewMeters: _totalGroundSlewMeters,
                    Invalidated: false,
                    Reason: null
                );
            }
            else
            {
                // Slew inativo
                if (_wasSlewActive)
                {
                    _wasSlewActive = false;
                    _lastLat = null;
                    _lastLon = null;

                    if (_currentUsageMeters > 0.0)
                    {
                        usageCompleted = true;
                        completedMeters = _currentUsageMeters;
                        _currentUsageMeters = 0.0;
                    }
                }

                return new SlewVerdict(
                    Allowed: true,
                    GroundSlewMeters: _totalGroundSlewMeters,
                    Invalidated: false,
                    Reason: null
                )
                {
                    UsageCompleted = usageCompleted,
                    UsageMeters = completedMeters
                };
            }
        }

        /// <summary>
        /// Avalia se há congelamento de altitude, atitude ou coordenadas ativado.
        /// No solo antes da decolagem é tolerado; no ar ou após a decolagem invalida o voo.
        /// </summary>
        public SlewVerdict CheckFreeze(bool altitudeFreeze, bool attitudeFreeze, bool latLonFreeze, bool onGround, bool hasTakenOff)
        {
            if (_isInvalidated)
            {
                return new SlewVerdict(
                    Allowed: false,
                    GroundSlewMeters: _totalGroundSlewMeters,
                    Invalidated: true,
                    Reason: _invalidationReason
                );
            }

            bool isFreezeActive = altitudeFreeze || attitudeFreeze || latLonFreeze;
            if (isFreezeActive && (!onGround || hasTakenOff))
            {
                _isInvalidated = true;
                _invalidationReason = "congelamento";
                return new SlewVerdict(
                    Allowed: false,
                    GroundSlewMeters: _totalGroundSlewMeters,
                    Invalidated: true,
                    Reason: _invalidationReason
                );
            }

            return new SlewVerdict(
                Allowed: true,
                GroundSlewMeters: _totalGroundSlewMeters,
                Invalidated: false,
                Reason: null
            );
        }

        /// <summary>
        /// Sobrecarga unificada que avalia simultaneamente slew e flags de congelamento.
        /// </summary>
        public SlewVerdict Update(
            bool slewActive,
            bool onGround,
            bool hasTakenOff,
            double latDeg,
            double lonDeg,
            bool altitudeFreeze,
            bool attitudeFreeze,
            bool latLonFreeze)
        {
            var slewResult = Update(slewActive, onGround, hasTakenOff, latDeg, lonDeg);
            if (slewResult.Invalidated) return slewResult;

            var freezeResult = CheckFreeze(altitudeFreeze, attitudeFreeze, latLonFreeze, onGround, hasTakenOff);
            if (freezeResult.Invalidated) return freezeResult;

            return slewResult;
        }
    }
}
