namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Ciclo entre uma decolagem e o toque que a encerra. Depois do toque, voltar a voar por mais tempo que um quique
    /// (toque e arremetida, arremetida no solo, nova perna) abre um ciclo novo: o próximo toque é outro pouso.
    /// Perna (XP) é só o pouso que termina em parada completa; toque e arremetida conta pouso, mas não perna.
    /// Lógica pura: o tempo vem de quem chama (segundos simulados).
    /// </summary>
    public sealed class LandingCycleTracker
    {
        public const double FullStopSpeedKt = 5.0;
        public const double MinFlyingSpeedKt = 30.0;

        private readonly double _bounceMaxAirborneSec;
        private double? _airborneSince;
        private bool _legCounted;

        public LandingCycleTracker(double bounceMaxAirborneSec) => _bounceMaxAirborneSec = bounceMaxAirborneSec;

        /// <summary>Já houve pouso no ciclo atual (contatos seguintes são quique).</summary>
        public bool HasTouchedDown { get; private set; }

        /// <summary>Maior razão de descida registrada no ciclo atual.</summary>
        public double TouchdownFpm { get; private set; }

        /// <summary>Pousos que terminaram em parada completa.</summary>
        public int CompletedLegs { get; private set; }

        /// <summary>O avião parou depois do último pouso: a próxima saída do chão é uma decolagem nova.</summary>
        public bool StoppedSinceTouchdown => _legCounted;

        public void Reset()
        {
            CompletedLegs = 0;
            StartCycle();
        }

        /// <summary>Primeiro contato do ciclo.</summary>
        public void RegisterLanding(double fpm)
        {
            HasTouchedDown = true;
            TouchdownFpm = fpm;
            _airborneSince = null;
        }

        /// <summary>Contato seguinte no mesmo ciclo: só conta se for mais forte que o anterior.</summary>
        public bool TryRegisterBounce(double fpm)
        {
            if (!HasTouchedDown || fpm <= TouchdownFpm) return false;
            TouchdownFpm = fpm;
            return true;
        }

        /// <summary>
        /// Acompanha o estado depois do pouso. Devolve true quando o avião voltou a voar e um ciclo novo começou;
        /// <paramref name="afterFullStop"/> indica se ele tinha parado antes (decolagem nova, não toque e arremetida).
        /// </summary>
        public bool Update(bool onGround, double groundSpeedKt, double simSeconds, out bool afterFullStop)
        {
            afterFullStop = false;
            if (!HasTouchedDown) return false;

            if (onGround)
            {
                _airborneSince = null;
                if (!_legCounted && groundSpeedKt < FullStopSpeedKt)
                {
                    _legCounted = true;
                    CompletedLegs++;
                }
                return false;
            }

            if (groundSpeedKt <= MinFlyingSpeedKt) return false;

            _airborneSince ??= simSeconds;
            if (simSeconds - _airborneSince.Value < _bounceMaxAirborneSec) return false;

            afterFullStop = _legCounted;
            StartCycle();
            return true;
        }

        private void StartCycle()
        {
            HasTouchedDown = false;
            TouchdownFpm = 0;
            _airborneSince = null;
            _legCounted = false;
        }
    }
}
