using System;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Rastreia o estado desejado vs. aplicado para comandos toggle do simulador sem SimVar confiável
    /// (como ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE).
    /// No início e após recarga da aeronave/voo, o simulador assume o estado padrão (normalmente conectado = true).
    /// </summary>
    public sealed class ToggleStateTracker
    {
        private readonly bool _defaultState;
        private bool _appliedState;
        private bool _desiredState;

        public ToggleStateTracker(bool defaultState = true)
        {
            _defaultState = defaultState;
            _appliedState = defaultState;
            _desiredState = defaultState;
        }

        public bool AppliedState => _appliedState;
        public bool DesiredState => _desiredState;

        /// <summary>
        /// Atualiza o estado desejado. Retorna true se um evento toggle deve ser enviado ao simulador.
        /// </summary>
        public bool RequestState(bool desired)
        {
            _desiredState = desired;
            if (_appliedState != _desiredState)
            {
                _appliedState = _desiredState;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Notifica que a aeronave ou o voo foi recarregado no simulador (o sim volta para o estado padrão).
        /// Retorna true se o estado desejado atual exige reenviar o toggle.
        /// </summary>
        public bool OnSimReloaded()
        {
            _appliedState = _defaultState;
            if (_appliedState != _desiredState)
            {
                _appliedState = _desiredState;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Restaura o estado para o padrão (ex.: na desconexão do SimConnect).
        /// </summary>
        public void Reset()
        {
            _appliedState = _defaultState;
            _desiredState = _defaultState;
        }
    }
}
