using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Converte o tempo real entre dois pacotes de telemetria em tempo simulado (× SIMULATION RATE).
    ///
    /// A proteção contra saltos (pausa, travamento, alt-tab) é feita no tempo REAL, antes de multiplicar
    /// pelo sim rate. Limitar depois da multiplicação descartava parte do desgaste e das horas em
    /// simulação acelerada (ex.: pacote de 5 s a 8× = 40 s simulados, cortados para 18 s).
    /// </summary>
    public static class SimTimeStep
    {
        /// <summary>Maior intervalo real aceito entre dois pacotes. Acima disto é pausa/travamento.</summary>
        public const double MaxWallSeconds = 10.0;

        /// <summary>Maior aceleração considerada (proteção contra leitura inválida de SIMULATION RATE).</summary>
        public const double MaxSimRate = 128.0;

        /// <summary>Maior passo simulado que um tick pode representar, em horas.</summary>
        public const double MaxSimHoursPerTick = MaxWallSeconds * MaxSimRate / 3600.0;

        /// <summary>Segundos simulados correspondentes a <paramref name="wallSeconds"/> de tempo real.</summary>
        public static double ToSimSeconds(double wallSeconds, double simRate)
        {
            if (double.IsNaN(wallSeconds) || wallSeconds <= 0) return 0.0;
            double wall = Math.Min(wallSeconds, MaxWallSeconds);
            double rate = double.IsNaN(simRate) || simRate <= 0 ? 1.0 : Math.Min(simRate, MaxSimRate);
            return wall * rate;
        }

        /// <summary>Horas simuladas correspondentes a <paramref name="wallSeconds"/> de tempo real.</summary>
        public static double ToSimHours(double wallSeconds, double simRate) => ToSimSeconds(wallSeconds, simRate) / 3600.0;
    }
}
