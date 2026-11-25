using System.Collections.Generic;
using FlightCareer.Domain.Airports.Sim;
using Xunit;

namespace FlightCareer.Tests.Airports.Sim
{
    public class SimAirportDiffTests
    {
        [Fact]
        public void Diff_ClassificaNovoMudouSumiuIgual()
        {
            var sbokAntigo = new SimAirportListItem("SBOK", "SB", -10.5000, -55.0000, 200.0);
            var sbokNovo = new SimAirportListItem("SBOK", "SB", -3.3600, -39.8300, 50.0);
            var sbsp = new SimAirportListItem("SBSP", "SB", -23.6261, -46.6564, 802.0);
            var sbxx = new SimAirportListItem("SBXX", "SB", -15.0000, -47.0000, 1000.0);

            var fpSbokAntigo = SimAirportFingerprint.Of(sbokAntigo);
            var fpSbsp = SimAirportFingerprint.Of(sbsp);

            var simList = new List<SimAirportListItem>
            {
                sbokNovo,
                sbsp,
                sbxx
            };

            var knownList = new List<KnownAirport>
            {
                new("SBOK", fpSbokAntigo, true),
                new("SBSP", fpSbsp, true),
                new("SBZZ", "fp-sbzz", true)
            };

            var result = SimAirportDiff.Compute(simList, knownList);

            Assert.Single(result.New);
            Assert.Equal("SBXX", result.New[0].Ident);

            Assert.Single(result.Changed);
            Assert.Equal("SBOK", result.Changed[0].Ident);

            Assert.Single(result.Missing);
            Assert.Equal("SBZZ", result.Missing[0]);

            Assert.Equal(1, result.Unchanged);
            Assert.Equal(0, result.Duplicates);
        }

        [Fact]
        public void Diff_IdentRepetido_ContaDuplicado()
        {
            var item1 = new SimAirportListItem("SBSP", "SB", -23.6261, -46.6564, 802.0);
            var item2 = new SimAirportListItem("sbsp", "SB", -23.6261, -46.6564, 802.0);

            var result = SimAirportDiff.Compute(new[] { item1, item2 }, new List<KnownAirport>());

            Assert.Single(result.New);
            Assert.Equal(1, result.Duplicates);
        }

        [Fact]
        public void Diff_ForaDoSimulador_NaoViraMissing()
        {
            var known = new List<KnownAirport>
            {
                new("SBBW", "fp", false) // Já estava IsInSimulator == false
            };

            var result = SimAirportDiff.Compute(new List<SimAirportListItem>(), known);

            Assert.Empty(result.Missing);
        }
    }
}
