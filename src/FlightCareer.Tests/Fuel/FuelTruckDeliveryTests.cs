using System;
using FlightCareer.Domain.Fuel;
using Xunit;

namespace FlightCareer.Tests.Fuel
{
    public class FuelTruckDeliveryTests
    {
        [Fact]
        public void FuelTruckDelivery_Sobe20GalEEstabiliza10s_Completa20()
        {
            var t0 = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5);

            delivery.Arm(10.0, t0);
            Assert.Equal(FuelTruckDeliveryStatus.Armed, delivery.Status);
            Assert.Equal(10.0, delivery.BaseGallons);

            // Subida para 30 galões (+20 gal)
            var t1 = t0.AddSeconds(5);
            delivery.Update(30.0, true, t1);
            Assert.Equal(FuelTruckDeliveryStatus.Armed, delivery.Status);
            Assert.Equal(30.0, delivery.MaxGallons);

            // Estabilizado por 5 s (ainda dentro da janela de 10 s)
            delivery.Update(30.0, true, t1.AddSeconds(5));
            Assert.Equal(FuelTruckDeliveryStatus.Armed, delivery.Status);

            // Estabilizado por 10 s desde a última subida
            delivery.Update(30.0, true, t1.AddSeconds(10));
            Assert.Equal(FuelTruckDeliveryStatus.Completed, delivery.Status);
            Assert.Equal(20.0, delivery.DeliveredGallons);
        }

        [Fact]
        public void FuelTruckDelivery_RuidoAbaixoDaTolerancia_NaoCompleta()
        {
            var t0 = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5);

            delivery.Arm(10.0, t0);

            // Ruído de 0.2 gal (menor que tolerância 0.5)
            delivery.Update(10.2, true, t0.AddSeconds(15));
            Assert.Equal(FuelTruckDeliveryStatus.Armed, delivery.Status);
            Assert.Equal(0.0, delivery.DeliveredGallons);
        }

        [Fact]
        public void FuelTruckDelivery_Decolagem_Expira()
        {
            var t0 = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5);

            delivery.Arm(10.0, t0);
            delivery.Update(10.0, false, t0.AddSeconds(1));

            Assert.Equal(FuelTruckDeliveryStatus.Expired, delivery.Status);
            Assert.Equal(0.0, delivery.DeliveredGallons);
        }

        [Fact]
        public void FuelTruckDelivery_Timeout10Min_Expira()
        {
            var t0 = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5);

            delivery.Arm(10.0, t0);

            // Passou 10 minutos sem subida de combustível
            delivery.Update(10.0, true, t0.AddMinutes(10));

            Assert.Equal(FuelTruckDeliveryStatus.Expired, delivery.Status);
            Assert.Equal(0.0, delivery.DeliveredGallons);
        }

        [Fact]
        public void FuelTruckDelivery_Descida_Ignorada()
        {
            var t0 = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5);

            delivery.Arm(10.0, t0);

            // Motor consumiu um pouco ou ruído negativo
            delivery.Update(9.0, true, t0.AddSeconds(2));
            Assert.Equal(FuelTruckDeliveryStatus.Armed, delivery.Status);
            Assert.Equal(10.0, delivery.MaxGallons);

            // Caminhão sobe para 30 gal
            var tRise = t0.AddSeconds(5);
            delivery.Update(30.0, true, tRise);

            // Estabiliza por 10 s
            delivery.Update(30.0, true, tRise.AddSeconds(10));
            Assert.Equal(FuelTruckDeliveryStatus.Completed, delivery.Status);
            Assert.Equal(20.0, delivery.DeliveredGallons);
        }

        [Fact]
        public void FuelTruckDelivery_ForceCompleteComSubida_Completa()
        {
            var t0 = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5);

            delivery.Arm(10.0, t0);
            delivery.Update(25.0, true, t0.AddSeconds(2));

            // Usuário inicia o voo antes dos 10 s de estabilidade
            delivery.ForceComplete(25.0);

            Assert.Equal(FuelTruckDeliveryStatus.Completed, delivery.Status);
            Assert.Equal(15.0, delivery.DeliveredGallons);
        }

        [Fact]
        public void FuelTruckDelivery_ForceCompleteSemSubida_Expira()
        {
            var t0 = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5);

            delivery.Arm(10.0, t0);
            delivery.ForceComplete(10.2);

            Assert.Equal(FuelTruckDeliveryStatus.Expired, delivery.Status);
            Assert.Equal(0.0, delivery.DeliveredGallons);
        }

        [Fact]
        public void FuelTruckDelivery_RelogioInjetavelESobrecargas_Funciona()
        {
            var time = new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc);
            var delivery = new FuelTruckDelivery(tolerance: 0.5, clock: () => time);

            delivery.Arm(50.0);
            Assert.Equal(FuelTruckDeliveryStatus.Armed, delivery.Status);

            time = time.AddSeconds(2);
            delivery.Update(70.0, true);

            time = time.AddSeconds(10);
            delivery.Update(70.0, true);

            Assert.Equal(FuelTruckDeliveryStatus.Completed, delivery.Status);
            Assert.Equal(20.0, delivery.DeliveredGallons);

            delivery.Reset();
            Assert.Equal(FuelTruckDeliveryStatus.Idle, delivery.Status);
            Assert.Equal(0.0, delivery.DeliveredGallons);
        }
    }
}
