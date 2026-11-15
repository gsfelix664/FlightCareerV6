using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Política pura de enforcement para alavanca de flaps com pane persistente.
    /// Reescreve no máximo 1×/s e só depois de 0,5 s fora da posição. A pane nativa de flap NÃO suprime a correção: o barulho do motor do flap fica como indicativo ao piloto (decisão do usuário, 2026-11-15).
    /// </summary>
    public static class FlapEnforcementPolicy
    {
        public static readonly TimeSpan MismatchConfirmationTime = TimeSpan.FromSeconds(0.5);
        public static readonly TimeSpan FlapCorrectionThrottle = TimeSpan.FromSeconds(1.0);
        public const double PositionTolerance = 0.1;

        public static bool ShouldWrite(
            double currentIndex,
            double lockedIndex,
            DateTime? mismatchSince,
            DateTime? lastWrite,
            DateTime now)
        {
            if (Math.Abs(currentIndex - lockedIndex) <= PositionTolerance) return false;
            if (!mismatchSince.HasValue || now - mismatchSince.Value < MismatchConfirmationTime) return false;
            if (lastWrite.HasValue && now - lastWrite.Value < FlapCorrectionThrottle) return false;
            return true;
        }
    }
}
