using System;

namespace FlightCareer.Domain.Xp
{
    /// <summary>
    /// Prazo do bônus de XP (XpCalculator › 5.3) calculado antes do voo, para o quadro de missões.
    /// O ETE de referência é o mesmo do fim do voo: distância direta ÷ cruzeiro + folga fixa (15 min).
    /// </summary>
    public sealed record XpDeadlineInfo(
        double ReferenceEteMinutes,
        double OnTimeLimitMinutes,
        double AcceptableLimitMinutes,
        double OnTimeBonusXp,
        double AcceptableBonusXp);

    public static class XpDeadline
    {
        /// <summary>
        /// Calcula os limites de tempo de voo para os bônus de prazo.
        /// Retorna null em voo local (origem = destino) ou sem distância, em que o ETE seria só a folga.
        /// </summary>
        public static XpDeadlineInfo? Compute(double distanceNm, double cruiseSpeedKts, bool isUrgent, bool isLocalFlight, XpRules? rules = null)
        {
            if (isLocalFlight || distanceNm <= 0)
                return null;

            var b = (rules ?? XpRules.Default).Bonuses;
            double cruise = cruiseSpeedKts > 0 ? cruiseSpeedKts : 100.0;
            double ete = distanceNm / cruise * 60.0 + b.EteFixedBufferMinutes;

            return new XpDeadlineInfo(
                ReferenceEteMinutes: ete,
                OnTimeLimitMinutes: ete * b.OnTimeRatioMax,
                AcceptableLimitMinutes: ete * b.AcceptableTimeRatioMax,
                OnTimeBonusXp: isUrgent ? b.OnTimeBonusUrgent : b.OnTimeBonusNormal,
                AcceptableBonusXp: b.AcceptableTimeBonus);
        }

        /// <summary>Formata minutos como "1h 05m" ou "42 min".</summary>
        public static string FormatMinutes(double minutes)
        {
            int total = (int)Math.Floor(Math.Max(0, minutes));
            int h = total / 60, m = total % 60;
            return h > 0 ? $"{h}h {m:D2}m" : $"{m} min";
        }
    }
}
