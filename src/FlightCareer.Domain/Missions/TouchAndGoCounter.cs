using System;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Conta toques-e-arremetidas (TGL) válidos durante um voo monitorado.
    /// TGL = toque (não quique, duração no solo >= 1.0 s) com rolagem (GS máxima > 20 kt),
    /// sem parada completa (GS nunca caiu abaixo de 5.0 kt), seguido de nova decolagem.
    /// A decolagem inicial do voo é ignorada.
    /// </summary>
    public sealed class TouchAndGoCounter
    {
        public const double MinRollDurationSeconds = 1.0;
        public const double MinGroundSpeedForRollKt = 20.0;
        public const double FullStopSpeedThresholdKt = 5.0;

        private int _count;
        private bool _wasOnGround;
        private bool _hasTakenOffOnce;
        private double _touchdownSimTime;
        private double _maxGsDuringRoll;
        private bool _fullStopOccurred;

        public int Count => _count;

        public static bool IsValidTouchAndGo(double rollDurationSeconds, double maxGroundSpeedKt, bool fullStopOccurred)
        {
            bool isBounce = rollDurationSeconds < MinRollDurationSeconds;
            bool reachedSpeed = maxGroundSpeedKt > MinGroundSpeedForRollKt;
            return !isBounce && !fullStopOccurred && reachedSpeed;
        }

        public void Reset(bool initialIsOnGround)
        {
            _count = 0;
            _wasOnGround = initialIsOnGround;
            // Se já iniciou o monitoramento no ar, considera a decolagem inicial já realizada.
            // Se iniciou no solo, a primeira decolagem é a decolagem inicial (não é TGL).
            _hasTakenOffOnce = !initialIsOnGround;
            _touchdownSimTime = 0.0;
            _maxGsDuringRoll = 0.0;
            _fullStopOccurred = false;
        }

        public void Update(bool isOnGround, double groundSpeedKt, double simSeconds)
        {
            if (!_wasOnGround && isOnGround)
            {
                // Transição AR -> SOLO (toque na pista)
                _touchdownSimTime = simSeconds;
                _maxGsDuringRoll = groundSpeedKt;
                _fullStopOccurred = groundSpeedKt < 5.0;
            }
            else if (_wasOnGround && isOnGround)
            {
                // Durante a corrida no solo
                if (groundSpeedKt > _maxGsDuringRoll)
                {
                    _maxGsDuringRoll = groundSpeedKt;
                }
                if (groundSpeedKt < 5.0)
                {
                    _fullStopOccurred = true;
                }
            }
            else if (_wasOnGround && !isOnGround)
            {
                // Transição SOLO -> AR (decolagem)
                if (!_hasTakenOffOnce)
                {
                    // Decolagem inicial do voo
                    _hasTakenOffOnce = true;
                }
                else
                {
                    double rollDuration = simSeconds - _touchdownSimTime;
                    if (IsValidTouchAndGo(rollDuration, _maxGsDuringRoll, _fullStopOccurred))
                    {
                        _count++;
                    }
                }
            }

            _wasOnGround = isOnGround;
        }
    }
}
