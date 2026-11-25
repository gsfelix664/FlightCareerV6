using System.Collections.Generic;
using FlightCareer.Domain.Airports.Sim;
using Xunit;

namespace FlightCareer.Tests.Airports.Sim
{
    public class SimAirportFingerprintTests
    {
        [Fact]
        public void Fingerprint_MesmaPosicaoArredondada_Igual()
        {
            var item1 = new SimAirportListItem("SBSP", "SB", -23.62611, -46.65639, 802.2);
            var item2 = new SimAirportListItem(" sbsp ", "sb", -23.62614, -46.65641, 802.4);

            var fp1 = SimAirportFingerprint.Of(item1);
            var fp2 = SimAirportFingerprint.Of(item2);

            Assert.Equal(fp1, fp2);
        }

        [Fact]
        public void Fingerprint_MudaAltitude_Diferente()
        {
            var item1 = new SimAirportListItem("SBSP", "SB", -23.6261, -46.6564, 802.2);
            var item2 = new SimAirportListItem("SBSP", "SB", -23.6261, -46.6564, 803.1);

            var fp1 = SimAirportFingerprint.Of(item1);
            var fp2 = SimAirportFingerprint.Of(item2);

            Assert.NotEqual(fp1, fp2);
        }

        [Fact]
        public void Digest_OrdemDiferente_Igual()
        {
            var a = new SimAirportListItem("SBSP", "SB", -23.6261, -46.6564, 802.0);
            var b = new SimAirportListItem("SBGR", "SB", -23.4356, -46.4731, 750.0);

            var list1 = new List<SimAirportListItem> { a, b };
            var list2 = new List<SimAirportListItem> { b, a };

            var digest1 = SimAirportFingerprint.Digest(list1);
            var digest2 = SimAirportFingerprint.Digest(list2);

            Assert.Equal(digest1, digest2);
            Assert.False(string.IsNullOrWhiteSpace(digest1));
        }
    }
}
