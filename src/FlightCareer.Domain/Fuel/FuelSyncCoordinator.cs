using System;

namespace FlightCareer.Domain.Fuel
{
    public enum FuelSyncStatus
    {
        Idle,
        Pending,
        Confirmed,
        Failed
    }

    /// <summary>
    /// Coordena o processo de sincronização de combustível entre o aplicativo e os tanques do simulador.
    /// Opera com conferência assíncrona após novas leituras do sim, limite de tentativas (máx 3),
    /// janela de conferência (~5 s por tentativa), exigência de solo e relógio injetável.
    /// </summary>
    public class FuelSyncCoordinator
    {
        public const int MaxAttempts = 3;
        public const double AttemptTimeoutSeconds = 5.0;

        private readonly Func<DateTime> _clock;

        public FuelSyncStatus Status { get; private set; } = FuelSyncStatus.Idle;
        public double TargetGallons { get; private set; }
        public double Tolerance { get; private set; }
        public int AttemptCount { get; private set; }
        public DateTime CurrentAttemptStartTime { get; private set; }
        public bool NeedsSimWrite { get; private set; }

        public FuelSyncCoordinator(Func<DateTime>? clock = null)
        {
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        /// <summary>
        /// Inicia a sincronização de combustível.
        /// Se o combustível no simulador já estiver dentro da tolerância, confirma imediatamente sem necessidade de escrita.
        /// Se divergir fora do solo, falha imediatamente.
        /// Se divergir no solo, entra em Pending e sinaliza NeedsSimWrite na tentativa 1.
        /// </summary>
        public void Start(double targetGallons, double currentSimGallons, double tolerance, bool isOnGround)
        {
            TargetGallons = targetGallons;
            Tolerance = Math.Max(0.1, tolerance);
            AttemptCount = 0;
            NeedsSimWrite = false;

            if (Math.Abs(currentSimGallons - targetGallons) <= Tolerance)
            {
                Status = FuelSyncStatus.Confirmed;
                return;
            }

            if (!isOnGround)
            {
                Status = FuelSyncStatus.Failed;
                return;
            }

            Status = FuelSyncStatus.Pending;
            AttemptCount = 1;
            CurrentAttemptStartTime = _clock();
            NeedsSimWrite = true;
        }

        /// <summary>
        /// Notifica que a escrita nos tanques do simulador foi disparada pelo chamador.
        /// </summary>
        public void NotifyWriteDispatched()
        {
            NeedsSimWrite = false;
        }

        /// <summary>
        /// Atualiza o estado da sincronização a cada tick de telemetria recebido do simulador.
        /// </summary>
        public void Update(double currentSimGallons, bool isOnGround)
        {
            if (Status != FuelSyncStatus.Pending) return;

            // Se a aeronave não estiver mais no solo durante a sincronização pendente, falha
            if (!isOnGround)
            {
                Status = FuelSyncStatus.Failed;
                NeedsSimWrite = false;
                return;
            }

            // Confere se o valor convergiu para o alvo dentro da tolerância
            if (Math.Abs(currentSimGallons - TargetGallons) <= Tolerance)
            {
                Status = FuelSyncStatus.Confirmed;
                NeedsSimWrite = false;
                return;
            }

            var now = _clock();
            var elapsedSeconds = (now - CurrentAttemptStartTime).TotalSeconds;

            if (elapsedSeconds >= AttemptTimeoutSeconds)
            {
                if (AttemptCount < MaxAttempts)
                {
                    // Nova tentativa de gravação
                    AttemptCount++;
                    CurrentAttemptStartTime = now;
                    NeedsSimWrite = true;
                }
                else
                {
                    // Esgotaram as tentativas máximas sem conferência bem-sucedida
                    Status = FuelSyncStatus.Failed;
                    NeedsSimWrite = false;
                }
            }
        }

        /// <summary>
        /// Reseta o coordenador para o estado inicial Idle.
        /// </summary>
        public void Reset()
        {
            Status = FuelSyncStatus.Idle;
            TargetGallons = 0;
            Tolerance = 0;
            AttemptCount = 0;
            NeedsSimWrite = false;
        }
    }
}
