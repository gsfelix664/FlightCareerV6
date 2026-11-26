using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class HudContractItemTests
    {
        [Fact]
        public void HudContractItem_From_Pending()
        {
            var line = new ContractObjectiveLine("TGL 1/2", ContractObjectiveState.Pending);
            var item = HudContractItem.From(line);

            Assert.Equal("TGL 1/2", item.Text);
            Assert.Equal("#FBBF24", item.Color);
        }

        [Fact]
        public void HudContractItem_From_Done()
        {
            var line = new ContractObjectiveLine("TGL 2/2", ContractObjectiveState.Done);
            var item = HudContractItem.From(line);

            Assert.Equal("TGL 2/2 ✓", item.Text);
            Assert.Equal("#00E676", item.Color);
        }

        [Fact]
        public void HudContractItem_From_Failed()
        {
            var line = new ContractObjectiveLine("Altitude máx", ContractObjectiveState.Failed);
            var item = HudContractItem.From(line);

            Assert.Equal("✖ Altitude máx", item.Text);
            Assert.Equal("#FF5252", item.Color);
        }
    }
}
