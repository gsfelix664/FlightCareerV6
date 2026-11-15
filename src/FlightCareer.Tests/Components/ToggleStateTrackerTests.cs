using FlightCareer.Domain.Components;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class ToggleStateTrackerTests
    {
        [Fact]
        public void Desconectar_EnviaUmaVez()
        {
            var tracker = new ToggleStateTracker(defaultState: true);

            bool send = tracker.RequestState(desired: false);

            Assert.True(send);
            Assert.False(tracker.AppliedState);
            Assert.False(tracker.DesiredState);
        }

        [Fact]
        public void PedirDeNovo_NaoReenvia()
        {
            var tracker = new ToggleStateTracker(defaultState: true);
            tracker.RequestState(desired: false);

            bool sendAgain = tracker.RequestState(desired: false);

            Assert.False(sendAgain);
            Assert.False(tracker.AppliedState);
        }

        [Fact]
        public void Reconectar_Envia()
        {
            var tracker = new ToggleStateTracker(defaultState: true);
            tracker.RequestState(desired: false);

            bool reconnect = tracker.RequestState(desired: true);

            Assert.True(reconnect);
            Assert.True(tracker.AppliedState);
            Assert.True(tracker.DesiredState);
        }

        [Fact]
        public void DepoisDeRecarga_DesejadoDesconectado_VoltaAEnviarUmaVez()
        {
            var tracker = new ToggleStateTracker(defaultState: true);
            tracker.RequestState(desired: false);

            // Simulador recarrega a aeronave: o simulador volta ao defaultState (true)
            bool reapply = tracker.OnSimReloaded();

            Assert.True(reapply);
            Assert.False(tracker.AppliedState);

            // Uma chamada subsequente sem recarga não reenvia
            bool reapplyAgain = tracker.RequestState(desired: false);
            Assert.False(reapplyAgain);
        }

        [Fact]
        public void DepoisDeRecarga_DesejadoConectado_NaoEnvia()
        {
            var tracker = new ToggleStateTracker(defaultState: true);
            // Estado desejado permanece conectado (padrão)
            bool reapply = tracker.OnSimReloaded();

            Assert.False(reapply);
            Assert.True(tracker.AppliedState);
        }

        [Fact]
        public void Reset_RestauraEstadoPadrao()
        {
            var tracker = new ToggleStateTracker(defaultState: true);
            tracker.RequestState(desired: false);

            tracker.Reset();

            Assert.True(tracker.AppliedState);
            Assert.True(tracker.DesiredState);
        }
    }
}
