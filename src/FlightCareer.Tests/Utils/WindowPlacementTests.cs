using FlightCareer.Domain.Utils;
using Xunit;

namespace FlightCareer.Tests.Utils
{
    public class WindowPlacementTests
    {
        [Fact]
        public void ClampToArea_DentroDaTela_NaoMuda()
        {
            var window = new WindowRect(100, 100, 300, 200);
            var area = new WindowRect(0, 0, 1920, 1080);

            var clamped = WindowPlacement.ClampToArea(window, area);

            Assert.Equal(100, clamped.Left);
            Assert.Equal(100, clamped.Top);
            Assert.Equal(300, clamped.Width);
            Assert.Equal(200, clamped.Height);
        }

        [Fact]
        public void ClampToArea_ForaADireita_TrazDeVolta()
        {
            var window = new WindowRect(2500, 100, 300, 200);
            var area = new WindowRect(0, 0, 1920, 1080);

            var clamped = WindowPlacement.ClampToArea(window, area);

            Assert.Equal(1920 - 60, clamped.Left);
            Assert.Equal(100, clamped.Top);
        }

        [Fact]
        public void ClampToArea_ForaAEsquerda_DeixaMinVisivel()
        {
            var window = new WindowRect(-1000, 100, 300, 200);
            var area = new WindowRect(0, 0, 1920, 1080);

            var clamped = WindowPlacement.ClampToArea(window, area);

            Assert.Equal(-300 + 60, clamped.Left);
            Assert.Equal(100, clamped.Top);
        }

        [Fact]
        public void ClampToArea_AcimaDoTopo_TopNaArea()
        {
            var window = new WindowRect(100, -50, 300, 200);
            var area = new WindowRect(0, 0, 1920, 1080);

            var clamped = WindowPlacement.ClampToArea(window, area);

            Assert.Equal(100, clamped.Left);
            Assert.Equal(0, clamped.Top);
        }

        [Fact]
        public void ClampToArea_AbaixoDaBase_DeixaBarraVisivel()
        {
            var window = new WindowRect(100, 1100, 300, 200);
            var area = new WindowRect(0, 0, 1920, 1080);

            var clamped = WindowPlacement.ClampToArea(window, area);

            Assert.Equal(100, clamped.Left);
            Assert.Equal(1080 - 40, clamped.Top);
        }

        [Fact]
        public void ClampToArea_MonitorAEsquerda_AreaNegativa()
        {
            var window = new WindowRect(-1500, 100, 300, 200);
            var area = new WindowRect(-1920, 0, 3840, 1080);

            var clamped = WindowPlacement.ClampToArea(window, area);

            Assert.Equal(-1500, clamped.Left);
            Assert.Equal(100, clamped.Top);
        }
    }
}
