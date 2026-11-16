using System;
using FlightCareer.Domain.Integrity;
using Xunit;

namespace FlightCareer.Tests.Integrity
{
    public class SlewMonitorTests
    {
        private const double DegreesPerMeterAtEquator = 1.0 / 111120.0;

        [Fact]
        public void Slew_NoSoloDentroDoLimite_Permitido()
        {
            var monitor = new SlewMonitor(maxGroundSlewMeters: 1000.0);

            // Início do slew
            var v1 = monitor.Update(slewActive: true, onGround: true, hasTakenOff: false, latDeg: 0.0, lonDeg: 0.0);
            Assert.True(v1.Allowed);
            Assert.False(v1.Invalidated);
            Assert.Equal(0.0, v1.GroundSlewMeters);

            // Deslocamento de 300 m em slew no solo
            double lat300m = 300.0 * DegreesPerMeterAtEquator;
            var v2 = monitor.Update(slewActive: true, onGround: true, hasTakenOff: false, latDeg: lat300m, lonDeg: 0.0);
            Assert.True(v2.Allowed);
            Assert.False(v2.Invalidated);
            Assert.InRange(v2.GroundSlewMeters, 299.0, 301.0);

            // Desativa o slew
            var v3 = monitor.Update(slewActive: false, onGround: true, hasTakenOff: false, latDeg: lat300m, lonDeg: 0.0);
            Assert.True(v3.Allowed);
            Assert.False(v3.Invalidated);
            Assert.InRange(v3.GroundSlewMeters, 299.0, 301.0);
            Assert.True(v3.UsageCompleted);
            Assert.InRange(v3.UsageMeters, 299.0, 301.0);
        }

        [Fact]
        public void Slew_NoSoloAcimaDoLimite_Invalida()
        {
            var monitor = new SlewMonitor(maxGroundSlewMeters: 1000.0);

            // Primeiro uso: 600 metros
            monitor.Update(slewActive: true, onGround: true, hasTakenOff: false, latDeg: 0.0, lonDeg: 0.0);
            double lat600m = 600.0 * DegreesPerMeterAtEquator;
            monitor.Update(slewActive: true, onGround: true, hasTakenOff: false, latDeg: lat600m, lonDeg: 0.0);
            var vEnd1 = monitor.Update(slewActive: false, onGround: true, hasTakenOff: false, latDeg: lat600m, lonDeg: 0.0);

            Assert.True(vEnd1.Allowed);
            Assert.False(vEnd1.Invalidated);
            Assert.InRange(vEnd1.GroundSlewMeters, 599.0, 601.0);
            Assert.True(vEnd1.UsageCompleted);

            // Segundo uso: mais 600 metros (total 1.200 m)
            monitor.Update(slewActive: true, onGround: true, hasTakenOff: false, latDeg: lat600m, lonDeg: 0.0);
            double lat1200m = 1200.0 * DegreesPerMeterAtEquator;
            var vEnd2 = monitor.Update(slewActive: true, onGround: true, hasTakenOff: false, latDeg: lat1200m, lonDeg: 0.0);

            Assert.False(vEnd2.Allowed);
            Assert.True(vEnd2.Invalidated);
            Assert.Contains("slew", vEnd2.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.True(monitor.IsInvalidated);
        }

        [Fact]
        public void Slew_NoAr_Invalida()
        {
            var monitor = new SlewMonitor();

            var verdict = monitor.Update(slewActive: true, onGround: false, hasTakenOff: false, latDeg: -23.0, lonDeg: -46.0);

            Assert.False(verdict.Allowed);
            Assert.True(verdict.Invalidated);
            Assert.Contains("slew", verdict.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.True(monitor.IsInvalidated);
        }

        [Fact]
        public void Slew_DepoisDaDecolagem_NoSolo_Invalida()
        {
            var monitor = new SlewMonitor();

            // Pousou (onGround: true), mas já decolou (hasTakenOff: true) e usou slew
            var verdict = monitor.Update(slewActive: true, onGround: true, hasTakenOff: true, latDeg: -23.0, lonDeg: -46.0);

            Assert.False(verdict.Allowed);
            Assert.True(verdict.Invalidated);
            Assert.Contains("slew", verdict.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.True(monitor.IsInvalidated);
        }

        [Fact]
        public void Slew_Inativo_NaoSomaDistancia()
        {
            var monitor = new SlewMonitor();

            monitor.Update(slewActive: false, onGround: true, hasTakenOff: false, latDeg: 0.0, lonDeg: 0.0);
            double lat2000m = 2000.0 * DegreesPerMeterAtEquator;
            var verdict = monitor.Update(slewActive: false, onGround: true, hasTakenOff: false, latDeg: lat2000m, lonDeg: 0.0);

            Assert.True(verdict.Allowed);
            Assert.False(verdict.Invalidated);
            Assert.Equal(0.0, verdict.GroundSlewMeters);
            Assert.False(verdict.UsageCompleted);
        }

        [Fact]
        public void Freeze_EmVoo_Invalida()
        {
            var monitor = new SlewMonitor();

            // Congelamento de altitude ativo em voo
            var verdict = monitor.CheckFreeze(
                altitudeFreeze: true,
                attitudeFreeze: false,
                latLonFreeze: false,
                onGround: false,
                hasTakenOff: true
            );

            Assert.False(verdict.Allowed);
            Assert.True(verdict.Invalidated);
            Assert.Contains("congelamento", verdict.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Freeze_NoSoloAntesDaDecolagem_Permitido()
        {
            var monitor = new SlewMonitor();

            // Congelamento no solo antes de decolar é permitido/tolerado
            var verdict = monitor.CheckFreeze(
                altitudeFreeze: true,
                attitudeFreeze: true,
                latLonFreeze: true,
                onGround: true,
                hasTakenOff: false
            );

            Assert.True(verdict.Allowed);
            Assert.False(verdict.Invalidated);
            Assert.Null(verdict.Reason);
        }
    }
}
