using System;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Rastreia o cumprimento de pousos curtos (STOL) em pista desfavorável/curta:
    /// aproximação íngreme (média dos últimos 300 ft AGL >= MinApproachAngleDeg), toque na pista
    /// e parada completa em distância <= MaxStopDistanceFt.
    /// </summary>
    public sealed class StolLandingTracker
    {
        private StolContractData? _data;
        private bool _wasOnGround = true;
        private double _approachAngleSum;
        private int _approachSamplesCount;
        private bool _hasTouchdownInZone;
        private double _touchdownLat;
        private double _touchdownLon;
        private double _touchdownAvgAngle;
        private double _touchdownCrosswind;
        private double _groundStopSeconds;
        private bool _waitingAirborne;
        private double _airborneSeconds;

        public bool HasActiveContract => _data != null && !string.IsNullOrWhiteSpace(_data.StripIcao);
        public int ValidLandingsCount { get; private set; }
        public int RequiredLandings => _data?.RequiredLandings ?? 0;
        public bool IsCompleted => HasActiveContract && ValidLandingsCount >= RequiredLandings;
        public bool HadCrosswindBonus { get; private set; }
        public StolContractData? ContractData => _data;

        public void Configure(StolContractData? data, bool initialIsOnGround = true)
        {
            _data = data;
            Reset(initialIsOnGround);
        }

        public void Reset(bool initialIsOnGround = true)
        {
            ValidLandingsCount = 0;
            HadCrosswindBonus = false;
            _wasOnGround = initialIsOnGround;
            _approachAngleSum = 0.0;
            _approachSamplesCount = 0;
            _hasTouchdownInZone = false;
            _touchdownLat = 0.0;
            _touchdownLon = 0.0;
            _touchdownAvgAngle = 0.0;
            _touchdownCrosswind = 0.0;
            _groundStopSeconds = 0.0;
            _waitingAirborne = false;
            _airborneSeconds = 0.0;
        }

        public void Update(
            bool isOnGround,
            double lat,
            double lon,
            double altitudeMslFt,
            double groundSpeedKt,
            double verticalSpeedFpm,
            double crosswindKt,
            double simDtSeconds)
        {
            if (_data == null || simDtSeconds <= 0)
            {
                _wasOnGround = isOnGround;
                return;
            }

            double distToStrip = OverflyTracker.CalculateDistanceNM(lat, lon, _data.Lat, _data.Lon);

            // Aguarda estar no ar para habilitar nova corrida de aproximação
            if (_waitingAirborne)
            {
                if (!isOnGround)
                {
                    _airborneSeconds += simDtSeconds;
                    if (_airborneSeconds >= 5.0)
                    {
                        _waitingAirborne = false;
                        _airborneSeconds = 0.0;
                        _approachAngleSum = 0.0;
                        _approachSamplesCount = 0;
                    }
                }
                else
                {
                    _airborneSeconds = 0.0;
                }

                _wasOnGround = isOnGround;
                return;
            }

            // 1. Amostragem do ângulo de aproximação nos últimos 300 ft acima da pista
            if (!isOnGround)
            {
                double aglFt = altitudeMslFt - _data.ElevationFt;
                if (aglFt > 0 && aglFt <= 300.0 && distToStrip <= 1.5)
                {
                    // Ângulo = atan(|VS| / GS) com unidades coerentes:
                    // GS em kt -> ft/min (1 kt = 6076.11549 ft / 60 min = 101.2686 ft/min)
                    double gsFtPerMin = groundSpeedKt * 101.2686;
                    if (gsFtPerMin > 10.0)
                    {
                        double angleRad = Math.Atan2(Math.Abs(verticalSpeedFpm), gsFtPerMin);
                        double angleDeg = angleRad * (180.0 / Math.PI);
                        _approachAngleSum += angleDeg;
                        _approachSamplesCount++;
                    }
                }
                else if (aglFt > 500.0)
                {
                    // Muito alto: reseta acumulador de rampa
                    _approachAngleSum = 0.0;
                    _approachSamplesCount = 0;
                }
            }

            // 2. Transição AR -> SOLO (Toque na pista)
            if (!_wasOnGround && isOnGround)
            {
                if (distToStrip <= 1.5)
                {
                    _hasTouchdownInZone = true;
                    _touchdownLat = lat;
                    _touchdownLon = lon;
                    _touchdownAvgAngle = _approachSamplesCount > 0
                        ? (_approachAngleSum / _approachSamplesCount)
                        : 0.0;
                    _touchdownCrosswind = Math.Abs(crosswindKt);
                    _groundStopSeconds = 0.0;
                }
                else
                {
                    _hasTouchdownInZone = false;
                }
            }

            // 3. Durante a corrida no solo
            if (isOnGround && _hasTouchdownInZone)
            {
                if (groundSpeedKt < 3.0)
                {
                    _groundStopSeconds += simDtSeconds;
                    if (_groundStopSeconds >= 5.0)
                    {
                        // Parada completa atingida: avalia critérios do pouso
                        double stopDistNm = OverflyTracker.CalculateDistanceNM(_touchdownLat, _touchdownLon, lat, lon);
                        double stopDistFt = stopDistNm * 6076.11549;

                        bool distOk = stopDistFt <= _data.MaxStopDistanceFt;
                        bool angleOk = _touchdownAvgAngle >= _data.MinApproachAngleDeg;

                        if (distOk && angleOk)
                        {
                            ValidLandingsCount++;
                            if (_touchdownCrosswind >= _data.CrosswindBonusKt)
                            {
                                HadCrosswindBonus = true;
                            }
                        }

                        _hasTouchdownInZone = false;
                        _waitingAirborne = true;
                        _airborneSeconds = 0.0;
                        _groundStopSeconds = 0.0;
                        _approachAngleSum = 0.0;
                        _approachSamplesCount = 0;
                    }
                }
                else
                {
                    _groundStopSeconds = 0.0;
                }
            }

            // Se arremeter ou decolar antes da parada completa
            if (_wasOnGround && !isOnGround && _hasTouchdownInZone)
            {
                _hasTouchdownInZone = false;
                _approachAngleSum = 0.0;
                _approachSamplesCount = 0;
            }

            _wasOnGround = isOnGround;
        }
    }
}
