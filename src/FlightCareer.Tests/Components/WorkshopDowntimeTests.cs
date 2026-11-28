using System;
using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class WorkshopDowntimeTests
    {
        [Fact]
        public void Apply_ServicoNormal_MantemHorasECustoPadrao()
        {
            var (gameHours, costFactor) = WorkshopDowntime.Apply(72.0, express: false);

            Assert.Equal(72.0, gameHours);
            Assert.Equal(1.0, costFactor);
        }

        [Fact]
        public void Apply_ServicoExpresso_ReduzMetadeHorasECustaMais50Pct()
        {
            var (gameHours72, costFactor72) = WorkshopDowntime.Apply(72.0, express: true);
            Assert.Equal(36.0, gameHours72);
            Assert.Equal(1.5, costFactor72);

            var (gameHoursHalf, costFactorHalf) = WorkshopDowntime.Apply(0.5, express: true);
            Assert.Equal(0.25, gameHoursHalf);
            Assert.Equal(1.5, costFactorHalf);
        }

        [Theory]
        [InlineData(72.0, "72 h")]
        [InlineData(0.5, "0,5 h")]
        [InlineData(0.25, "0,25 h")]
        [InlineData(8.0, "8 h")]
        public void FormatHours_FormataEmPtBr(double hours, string expected)
        {
            Assert.Equal(expected, WorkshopDowntime.FormatHours(hours));
        }

        [Fact]
        public void CompletionSummary_ComZeroUmEMultiplosVencidos()
        {
            var careerClock = new DateTime(2026, 11, 27, 15, 0, 0, DateTimeKind.Utc);

            Assert.Equal(
                "A oficina levou 8 h de carreira. Dia da carreira: 27/11/2026.\nNenhum contrato do quadro venceu nesse tempo.",
                WorkshopDowntime.CompletionSummary(8.0, careerClock, 0));

            Assert.Equal(
                "A oficina levou 0,5 h de carreira. Dia da carreira: 27/11/2026.\n1 contrato do quadro venceu nesse tempo.",
                WorkshopDowntime.CompletionSummary(0.5, careerClock, 1));

            Assert.Equal(
                "A oficina levou 72 h de carreira. Dia da carreira: 27/11/2026.\n3 contratos do quadro venceram nesse tempo.",
                WorkshopDowntime.CompletionSummary(72.0, careerClock, 3));
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
    }
}
