using System;

namespace FlightCareer.Domain.Fuel
{
    public enum FuelTruckDeliveryStatus
    {
        Idle,
        Armed,
        Completed,
        Expired
    }

    /// <summary>
    /// Monitora a entrega de combustível realizada pelo caminhão do simulador no solo.
    /// Opera com lógica pura e relógio injetável:
    /// - Arm(simGallons, now): entra em Armed ao receber REQUEST_FUEL_KEY no solo.
    /// - Update(simGallons, onGround, now): acompanha o abastecimento; descida de nível é ignorada;
    ///   quando o combustível sobe mais que a tolerância e estabiliza por 10 s, conclui (Completed)
    ///   com DeliveredGallons = máx - base; se decolar ou passar 10 min sem subida, expira (Expired).
    /// - ForceComplete(simGallons): conclui imediatamente se houve subida além da tolerância, ou expira.
    /// </summary>
    public class FuelTruckDelivery
    {
        public const double DefaultTolerance = 0.5;
        public const double StabilityWindowSeconds = 10.0;
        public const double TimeoutMinutes = 10.0;

        private readonly Func<DateTime> _clock;

        public FuelTruckDeliveryStatus Status { get; private set; } = FuelTruckDeliveryStatus.Idle;
        public double BaseGallons { get; private set; }
        public double MaxGallons { get; private set; }
        public double DeliveredGallons { get; private set; }
        public double Tolerance { get; private set; }
        public DateTime ArmTime { get; private set; }
        public DateTime? LastRiseTime { get; private set; }

        public FuelTruckDelivery(double tolerance = DefaultTolerance, Func<DateTime>? clock = null)
        {
            Tolerance = Math.Max(0.1, tolerance);
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public void Arm(double simGallons) => Arm(simGallons, _clock());

        public void Arm(double simGallons, DateTime now)
        {
            BaseGallons = simGallons;
            MaxGallons = simGallons;
            DeliveredGallons = 0.0;
            ArmTime = now;
            LastRiseTime = null;
            Status = FuelTruckDeliveryStatus.Armed;
        }

        public void Update(double simGallons, bool onGround) => Update(simGallons, onGround, _clock());

        public void Update(double simGallons, bool onGround, DateTime now)
        {
            if (Status != FuelTruckDeliveryStatus.Armed) return;

            // Decolagem invalida/expira a entrega do caminhão
            if (!onGround)
            {
                Status = FuelTruckDeliveryStatus.Expired;
                DeliveredGallons = 0.0;
                return;
            }

            // Descidas são ignoradas; detecta novas subidas
            if (simGallons > MaxGallons)
            {
                MaxGallons = simGallons;
                if (MaxGallons - BaseGallons > Tolerance)
                {
                    LastRiseTime = now;
                }
            }

            // Se subiu além da tolerância e estabilizou por pelo menos 10 segundos
            if (MaxGallons - BaseGallons > Tolerance && LastRiseTime.HasValue)
            {
                if ((now - LastRiseTime.Value).TotalSeconds >= StabilityWindowSeconds)
                {
                    DeliveredGallons = MaxGallons - BaseGallons;
                    Status = FuelTruckDeliveryStatus.Completed;
                    return;
                }
            }
            else
            {
                // Sem subida acima da tolerância por 10 minutos desde o arme
                if ((now - ArmTime).TotalMinutes >= TimeoutMinutes)
                {
                    Status = FuelTruckDeliveryStatus.Expired;
                    DeliveredGallons = 0.0;
                    return;
                }
            }
        }

        public void ForceComplete(double simGallons)
        {
            if (Status != FuelTruckDeliveryStatus.Armed) return;

            if (simGallons > MaxGallons)
            {
                MaxGallons = simGallons;
            }

            if (MaxGallons - BaseGallons > Tolerance)
            {
                DeliveredGallons = MaxGallons - BaseGallons;
                Status = FuelTruckDeliveryStatus.Completed;
            }
            else
            {
                Status = FuelTruckDeliveryStatus.Expired;
                DeliveredGallons = 0.0;
            }
        }

        public void Reset()
        {
            Status = FuelTruckDeliveryStatus.Idle;
            BaseGallons = 0.0;
            MaxGallons = 0.0;
            DeliveredGallons = 0.0;
            ArmTime = DateTime.MinValue;
            LastRiseTime = null;
        }
    }
}
