using System;
using System.IO;
using FlightCareer.Domain.Economy;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    /// <summary>O multiplicador de preço de peças por classe vem só do economy_targets.json (antes era duplicado num switch).</summary>
    public class AircraftEconomyClassMultiplierTests
    {
        [Theory]
        [InlineData("A", 0.75)]
        [InlineData("E", 1.5)]
        [InlineData("H", 2.5)]
        [InlineData("J", 5.5)]
        [InlineData("a", 0.75)]
        [InlineData("Z", 1.0)]
        [InlineData("", 1.0)]
        public void Multiplicador_vem_do_json_real(string classe, double esperado)
        {
            Assert.Equal(esperado, AircraftEconomyClass.GetPartPriceMultiplier(classe), 6);
        }

        [Fact]
        public void Json_real_tem_todas_as_classes_da_reserva()
        {
            var loaded = AircraftEconomyClass.LoadPartPriceMultipliers();
            foreach (var (classe, valor) in AircraftEconomyClass.DefaultPartPriceMultipliers)
                Assert.Equal(valor, loaded[classe], 6);
        }

        [Fact]
        public void Mudanca_no_json_muda_o_multiplicador()
        {
            string dir = Path.Combine(Path.GetTempPath(), "fc-econ-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "economy_targets.json"),
                    "{ \"classes\": { \"A\": { \"partPriceMultiplier\": 0.9 }, \"B\": { \"className\": \"sem multiplicador\" } } }");
                var loaded = AircraftEconomyClass.LoadPartPriceMultipliers(dir);
                Assert.Equal(0.9, loaded["A"], 6);
                Assert.Equal(1.25, loaded["B"], 6); // ausente no arquivo → reserva
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
