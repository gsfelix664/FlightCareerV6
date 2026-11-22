using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Regras e conversão de tempo de parada na oficina em tempo de relógio real (PARADA-REAL).
    /// Lógica pura compartilhada entre UI, persistência e testes.
    /// </summary>
    public static class WorkshopDowntime
    {
        public const int MinRealMinutes = 1;
        public const int MaxRealMinutes = 20;
        public const double ExpressCostMultiplier = 1.5;

        /// <summary>
        /// Converte horas de jogo da parada em minutos reais do relógio do computador:
        /// clamp(round(2 × √horasDeJogo), 1, 20).
        /// Se horasDeJogo <= 0, retorna 0 (sem parada).
        /// </summary>
        public static int RealMinutesFor(double gameHours)
        {
            if (gameHours <= 0.0) return 0;
            int minutes = (int)Math.Round(2.0 * Math.Sqrt(gameHours), MidpointRounding.AwayFromZero);
            return Math.Clamp(minutes, MinRealMinutes, MaxRealMinutes);
        }

        /// <summary>
        /// Aplica os parâmetros do serviço conforme o modo (Normal ou Expresso).
        /// Serviço expresso: +50 % no preço (custo × 1,5), metade das horas de jogo e metade dos minutos reais.
        /// </summary>
        public static (double gameHours, int realMinutes, double costFactor) Apply(double gameHours, bool express)
        {
            if (express)
            {
                double gHours = gameHours / 2.0;
                int rMinutes = RealMinutesFor(gHours);
                return (gHours, rMinutes, ExpressCostMultiplier);
            }
            return (gameHours, RealMinutesFor(gameHours), 1.0);
        }

        /// <summary>
        /// Confere se a aeronave ainda está em manutenção na oficina no instante informado.
        /// </summary>
        public static bool IsInWorkshop(DateTime? untilUtc, DateTime nowUtc)
        {
            return untilUtc.HasValue && untilUtc.Value > nowUtc;
        }

        /// <summary>
        /// Retorna o tempo restante de parada na oficina. Devolve TimeSpan.Zero se já liberada.
        /// </summary>
        public static TimeSpan Remaining(DateTime? untilUtc, DateTime nowUtc)
        {
            if (!IsInWorkshop(untilUtc, nowUtc)) return TimeSpan.Zero;
            return untilUtc!.Value - nowUtc;
        }

        /// <summary>
        /// Calcula o novo prazo de liberação da oficina (UTC).
        /// Serviço sobre parada em andamento usa o maior prazo (não soma).
        /// </summary>
        public static DateTime CalculateNewUntilUtc(DateTime? currentUntilUtc, DateTime nowUtc, int realMinutes)
        {
            var targetUntil = nowUtc.AddMinutes(realMinutes);
            if (currentUntilUtc.HasValue && currentUntilUtc.Value > targetUntil)
            {
                return currentUntilUtc.Value;
            }
            return targetUntil;
        }
    }
}
