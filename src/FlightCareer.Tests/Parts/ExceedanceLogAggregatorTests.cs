using System.Collections.Generic;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class ExceedanceLogAggregatorTests
    {
        private static PartWearExceedance Ex(string part, double peak, double damage = 0.0, string notes = "Faixa 'caution' atingida (Motor 1)") =>
            new("piston_rpm_overspeed", part, "Addon", peak, 2550.0, 0.0, 1.0, damage, notes);

        [Fact]
        public void FaixaContinua_DozePecasPorTick_SoInicioEFim()
        {
            var agg = new ExceedanceLogAggregator();
            var parts = new[] { "engine.cylinders:1", "engine.valves:1", "oil.pump:1" };
            var lines = new List<string>();

            for (int s = 0; s < 100; s++)
            {
                var tick = new List<PartWearExceedance>();
                foreach (var p in parts) tick.Add(Ex(p, 2474.0 + s * 0.1, damage: 0.01));
                lines.AddRange(agg.Update(tick, s));
            }
            lines.AddRange(agg.Update(new List<PartWearExceedance>(), 100));

            Assert.Equal(2, lines.Count);
            Assert.StartsWith("INICIO piston_rpm_overspeed", lines[0]);
            Assert.StartsWith("FIM piston_rpm_overspeed", lines[1]);
            Assert.Contains("100 s", lines[1]);
            Assert.Contains("pico 2483.9", lines[1]);
            Assert.Contains("dano 3.00%", lines[1]);
            Assert.Contains("3 peca(s)", lines[1]);
        }

        [Fact]
        public void TrocaDeFaixa_FechaUmaEAbreOutra()
        {
            var agg = new ExceedanceLogAggregator();
            agg.Update(new List<PartWearExceedance> { Ex("engine.cylinders:1", 2500) }, 0);
            var lines = agg.Update(new List<PartWearExceedance> { Ex("engine.cylinders:1", 2560, notes: "Faixa 'warning' atingida (Motor 1)") }, 5);

            Assert.Equal(2, lines.Count);
            Assert.Contains(lines, l => l.StartsWith("INICIO") && l.Contains("'warning'"));
            Assert.Contains(lines, l => l.StartsWith("FIM") && l.Contains("'caution'") && l.Contains("5 s"));
        }

        [Fact]
        public void Flush_EncerraAbertas()
        {
            var agg = new ExceedanceLogAggregator();
            agg.Update(new List<PartWearExceedance> { Ex("engine.cylinders:1", 2500) }, 10);

            var lines = agg.Flush(40);

            Assert.Single(lines);
            Assert.Contains("30 s", lines[0]);
            Assert.Equal(0, agg.OpenCount);
            Assert.Empty(agg.Flush(50));
        }

        [Fact]
        public void TickVazioSemAbertas_NaoGravaNada()
        {
            var agg = new ExceedanceLogAggregator();
            Assert.Empty(agg.Update(new List<PartWearExceedance>(), 1));
            Assert.Empty(agg.Update(null, 2));
        }
    }
}
