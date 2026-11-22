using System.Globalization;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class OverflyWaypointCodecTests
    {
        [Fact]
        public void Parse_LegacyIcaoList_ParsedAsAirports()
        {
            var raw = "SBJD; SBSP,  SBGR";
            var items = OverflyWaypointCodec.Parse(raw);

            Assert.Equal(3, items.Count);
            Assert.True(items[0].IsAirport);
            Assert.Equal("SBJD", items[0].Ident);
            Assert.True(items[1].IsAirport);
            Assert.Equal("SBSP", items[1].Ident);
            Assert.True(items[2].IsAirport);
            Assert.Equal("SBGR", items[2].Ident);
        }

        [Fact]
        public void Parse_CoordinatePoint_WithoutRadiusAndAltitude_UsesDefaults()
        {
            var raw = "P:Farol de Santa Marta@-28.601,-48.815";
            var items = OverflyWaypointCodec.Parse(raw);

            Assert.Single(items);
            var p = items[0];
            Assert.False(p.IsAirport);
            Assert.Equal("Farol de Santa Marta", p.Ident);
            Assert.Equal(-28.601, p.Lat!.Value, 3);
            Assert.Equal(-48.815, p.Lon!.Value, 3);
            Assert.Equal(2.0, p.RadiusNm);
            Assert.Equal(3000.0, p.MaxAglFt);
        }

        [Fact]
        public void Parse_CoordinatePoint_WithRadiusAndAltitude_CustomValues()
        {
            var raw = "P:Área de instrução N@-23.1234,-46.5678,3.5,6000";
            var items = OverflyWaypointCodec.Parse(raw);

            Assert.Single(items);
            var p = items[0];
            Assert.False(p.IsAirport);
            Assert.Equal("Área de instrução N", p.Ident);
            Assert.Equal(-23.1234, p.Lat!.Value, 4);
            Assert.Equal(-46.5678, p.Lon!.Value, 4);
            Assert.Equal(3.5, p.RadiusNm);
            Assert.Equal(6000.0, p.MaxAglFt);
        }

        [Fact]
        public void Parse_MixedList_AirportsAndPoints()
        {
            var raw = "SBJD; P:Cristo Redentor@-22.9519,-43.2105,1.5,2500 ; SBRJ";
            var items = OverflyWaypointCodec.Parse(raw);

            Assert.Equal(3, items.Count);
            Assert.True(items[0].IsAirport);
            Assert.Equal("SBJD", items[0].Ident);

            Assert.False(items[1].IsAirport);
            Assert.Equal("Cristo Redentor", items[1].Ident);
            Assert.Equal(-22.9519, items[1].Lat!.Value, 4);
            Assert.Equal(-43.2105, items[1].Lon!.Value, 4);
            Assert.Equal(1.5, items[1].RadiusNm);
            Assert.Equal(2500.0, items[1].MaxAglFt);

            Assert.True(items[2].IsAirport);
            Assert.Equal("SBRJ", items[2].Ident);
        }

        [Fact]
        public void FormatAndParse_Roundtrip_MatchesIdentAndValues()
        {
            var origCulture = CultureInfo.CurrentCulture;
            try
            {
                // Garante que mesmo sob cultura pt-BR com vírgula decimal não quebra
                CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

                var item1 = new OverflyWaypointItem("SBSP", true);
                var item2 = new OverflyWaypointItem("Cachoeira das Andorinhas", false, -20.12345, -44.98765, 1.8, 4500);

                var formatted = OverflyWaypointCodec.Format(new[] { item1, item2 });
                Assert.Contains("SBSP", formatted);
                Assert.Contains("P:Cachoeira das Andorinhas@-20.12345,-44.98765,1.8,4500", formatted);

                var parsed = OverflyWaypointCodec.Parse(formatted);
                Assert.Equal(2, parsed.Count);
                Assert.Equal("SBSP", parsed[0].Ident);
                Assert.Equal("Cachoeira das Andorinhas", parsed[1].Ident);
                Assert.Equal(-20.12345, parsed[1].Lat!.Value, 5);
                Assert.Equal(-44.98765, parsed[1].Lon!.Value, 5);
                Assert.Equal(1.8, parsed[1].RadiusNm);
                Assert.Equal(4500.0, parsed[1].MaxAglFt);
            }
            finally
            {
                CultureInfo.CurrentCulture = origCulture;
            }
        }
    
        [Fact]
        public void Nome_com_separadores_e_higienizado_e_volta_igual()
        {
            string wp = OverflyWaypointCodec.FormatCoordinatePoint("Ponte A; B @ C, D", -22.5, -43.1, 1.5, 3000);
            var items = OverflyWaypointCodec.Parse(wp);
            Assert.Single(items);
            Assert.Equal("Ponte A  B   C  D", items[0].Ident);
            Assert.Equal(-22.5, items[0].Lat);
            Assert.Equal(1.5, items[0].RadiusNm);
        }
    }
}
