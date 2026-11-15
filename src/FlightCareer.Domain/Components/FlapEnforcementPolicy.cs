using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Política pura de enforcement para alavanca de flaps com pane persistente.
    /// Evita "briga" contínua com a pane nativa de flaps do simulador e reescrita durante movimento normal da alavanca.
    /// </summary>
    public static class FlapEnforcementPolicy
    {
        public static readonly TimeSpan MismatchConfirmationTime = TimeSpan.FromSeconds(0.5);
        public static readonly TimeSpan FlapCorrectionThrottle = TimeSpan.FromSeconds(1.0);
        public const double PositionTolerance = 0.1;

        public static bool ShouldWrite(
            double currentIndex,
            double lockedIndex,
            bool nativeFlapFailed,
            DateTime? mismatchSince,
            DateTime? lastWrite,
            DateTime now)
        {
            if (nativeFlapFailed) return false;
            if (Math.Abs(currentIndex - lockedIndex) <= PositionTolerance) return false;
            if (!mismatchSince.HasValue || now - mismatchSince.Value < MismatchConfirmationTime) return false;
            if (lastWrite.HasValue && now - lastWrite.Value < FlapCorrectionThrottle) return false;
            return true;
        }
    }
}
