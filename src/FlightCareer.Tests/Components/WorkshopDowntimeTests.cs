using System;
using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class WorkshopDowntimeTests
    {
        [Theory]
        [InlineData(2.0, 3)]     // 2 h -> 3 min
        [InlineData(8.0, 6)]     // 8 h -> 6 min
        [InlineData(72.0, 17)]   // 72 h -> 17 min
        [InlineData(150.0, 20)]  // 150 h -> 20 min (teto)
        [InlineData(0.1, 1)]     // 0,1 h -> 1 min (piso)
        [InlineData(0.0, 0)]     // 0 h -> sem parada
        [InlineData(-1.0, 0)]
        public void RealMinutesFor_CalculaConformeRegraComPisoETeto(double gameHours, int expectedMinutes)
        {
            int minutes = WorkshopDowntime.RealMinutesFor(gameHours);
            Assert.Equal(expectedMinutes, minutes);
        }

        [Fact]
        public void Apply_ServicoExpresso_ReduzMetadeHorasEMinutosECustaMais50Pct()
        {
            // 72 h jogo -> 36 h jogo, 12 min reais, custo x 1,5
            var (gHours, rMinutes, costFactor) = WorkshopDowntime.Apply(72.0, express: true);

            Assert.Equal(36.0, gHours);
            Assert.Equal(12, rMinutes);
            Assert.Equal(1.5, costFactor);
        }

        [Fact]
        public void Apply_ServicoNormal_MantemHorasEMinutosECustoPadrao()
        {
            var (gHours, rMinutes, costFactor) = WorkshopDowntime.Apply(72.0, express: false);

            Assert.Equal(72.0, gHours);
            Assert.Equal(17, rMinutes);
            Assert.Equal(1.0, costFactor);
        }

        [Fact]
        public void IsInWorkshop_ERemaining_ComRelogioInjetado()
        {
            var baseTime = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
            var untilTime = baseTime.AddMinutes(15);

            // Antes da liberação
            Assert.True(WorkshopDowntime.IsInWorkshop(untilTime, baseTime));
            Assert.True(WorkshopDowntime.IsInWorkshop(untilTime, baseTime.AddMinutes(14)));
            var remaining = WorkshopDowntime.Remaining(untilTime, baseTime);
            Assert.Equal(TimeSpan.FromMinutes(15), remaining);

            // No momento exato ou depois
            Assert.False(WorkshopDowntime.IsInWorkshop(untilTime, untilTime));
            Assert.False(WorkshopDowntime.IsInWorkshop(untilTime, untilTime.AddSeconds(1)));
            Assert.Equal(TimeSpan.Zero, WorkshopDowntime.Remaining(untilTime, untilTime));
            Assert.Equal(TimeSpan.Zero, WorkshopDowntime.Remaining(untilTime, untilTime.AddMinutes(5)));

            // Sem prazo
            Assert.False(WorkshopDowntime.IsInWorkshop(null, baseTime));
            Assert.Equal(TimeSpan.Zero, WorkshopDowntime.Remaining(null, baseTime));
        }

        [Fact]
        public void CalculateNewUntilUtc_ServicoSobreParadaUsaAMaior()
        {
            var now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

            // Caso 1: sem parada prévia
            var firstUntil = WorkshopDowntime.CalculateNewUntilUtc(null, now, 10);
            Assert.Equal(now.AddMinutes(10), firstUntil);

            // Caso 2: nova parada menor que a parada restante (mantém a maior)
            var nowPlus3 = now.AddMinutes(3); // 7 min restantes
            var smallerUntil = WorkshopDowntime.CalculateNewUntilUtc(firstUntil, nowPlus3, 5); // nowPlus3 + 5 = now + 8 min (< now + 10 min)
            Assert.Equal(firstUntil, smallerUntil);

            // Caso 3: nova parada maior que a restante (estende)
            var largerUntil = WorkshopDowntime.CalculateNewUntilUtc(firstUntil, nowPlus3, 15); // nowPlus3 + 15 = now + 18 min (> now + 10 min)
            Assert.Equal(nowPlus3.AddMinutes(15), largerUntil);
        }

        [Fact]
        public void MaintenancePricing_RealHours_SempreMultiplicaPorQuatro()
        {
            Assert.Equal(48.0, MaintenancePricing.RealHours(MaintenancePricing.OilChangeIntervalHours)); // 12 -> 48
            Assert.Equal(100.0, MaintenancePricing.RealHours(MaintenancePricing.AnnualInspectionIntervalHours)); // 25 -> 100
            Assert.Equal(500.0, MaintenancePricing.RealHours(MaintenancePricing.IgnitionRevisionIntervalHours)); // 125 -> 500
            Assert.Equal(2000.0, MaintenancePricing.RealHours(MaintenancePricing.PistonTboHours)); // 500 -> 2.000
            Assert.Equal(3600.0, MaintenancePricing.RealHours(MaintenancePricing.TurbineTboHours)); // 900 -> 3.600
        }

        [Fact]
        public void MaintenancePricing_RealCycles_MultiplicaPorQuatro()
        {
            Assert.Equal(400.0, MaintenancePricing.RealCycles(100.0));
        }

        [Fact]
        public void PilotAircraft_NaOficina_CalculaRestanteECorreto()
        {
            var now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
            var aircraft = new FlightCareer.Domain.Entities.PilotAircraft
            {
                Prefix = "PT-TST",
                WorkshopUntilUtc = now.AddMinutes(5),
                WorkshopDescription = "Troca de óleo"
            };

            Assert.True(WorkshopDowntime.IsInWorkshop(aircraft.WorkshopUntilUtc, now));
            Assert.Equal(TimeSpan.FromMinutes(5), WorkshopDowntime.Remaining(aircraft.WorkshopUntilUtc, now));

            // Após término
            Assert.False(WorkshopDowntime.IsInWorkshop(aircraft.WorkshopUntilUtc, now.AddMinutes(5).AddSeconds(1)));
        }
    }
}
