using System;

namespace FlightCareer.Domain.Xp
{
    /// <summary>
    /// Rastreador de infrações operacionais em voo (estol, luzes na decolagem/toque, 250 kt abaixo de 10.000 ft, Vne).
    /// Especificação do plano xp-proficiencia.md § 6.4 e tarefa XP-PONTAS.
    /// </summary>
    public sealed class FlightInfractionTracker
    {
        private readonly bool _isInstruction;
        private bool _prevStallWarning;
        private bool _prevSpeed250;
        private bool _prevOverspeedVne;

        private int _stallInfractions;
        private int _speed250Infractions;
        private int _overspeedVneInfractions;
        private int _takeoffLightsInfractions;
        private bool _lastTouchdownLightsOff;
        private int _touchdownCount;

        public FlightInfractionTracker(bool isInstruction = false)
        {
            _isInstruction = isInstruction;
        }

        /// <summary>
        /// Processa pacote periódico de telemetria para verificação de bordas de subida contínuas.
        /// </summary>
        public void OnTelemetryTick(
            bool isAirborne,
            bool stallWarning,
            double airspeedKnots,
            double altitudeFeet,
            bool hasOverspeedVne)
        {
            // 1. Estol: borda de subida de STALL WARNING no ar (exceto em missão de instrução)
            if (isAirborne && stallWarning && !_prevStallWarning)
            {
                if (!_isInstruction)
                {
                    _stallInfractions++;
                }
            }
            _prevStallWarning = stallWarning;

            // 2. Velocidade: borda de subida de IAS > 250 kt com altitude < 10.000 ft
            bool speed250Condition = airspeedKnots > 250.0 && altitudeFeet < 10000.0;
            if (speed250Condition && !_prevSpeed250)
            {
                _speed250Infractions++;
            }
            _prevSpeed250 = speed250Condition;

            // 3. Vne: borda de subida do alerta de overspeed estrutural
            if (hasOverspeedVne && !_prevOverspeedVne)
            {
                _overspeedVneInfractions++;
            }
            _prevOverspeedVne = hasOverspeedVne;
        }

        /// <summary>
        /// Registra estado dos faróis no momento da decolagem.
        /// Farol de pouso e de táxi desligados simultaneamente configuram infração (+1).
        /// </summary>
        public void OnTakeoff(bool landingLightOn, bool taxiLightOn)
        {
            if (!landingLightOn && !taxiLightOn)
            {
                _takeoffLightsInfractions++;
            }
        }

        /// <summary>
        /// Registra estado dos faróis no momento do toque de pouso.
        /// Mantém o estado da última perna/toque final.
        /// </summary>
        public void OnTouchdown(bool landingLightOn, bool taxiLightOn, bool isBounce = false)
        {
            if (isBounce) return;
            _touchdownCount++;
            _lastTouchdownLightsOff = (!landingLightOn && !taxiLightOn);
        }

        public int StallInfractions => _stallInfractions;
        public int OverspeedVneInfractions => _overspeedVneInfractions;
        public int VneOrStallInfractions => _overspeedVneInfractions + _stallInfractions;

        public int Overspeed250Infractions => _speed250Infractions;
        public int TakeoffLightsInfractions => _takeoffLightsInfractions;
        public int FinalTouchdownLightsInfractions => (_touchdownCount > 0 && _lastTouchdownLightsOff) ? 1 : 0;
        public int LightsInfractions => _takeoffLightsInfractions + FinalTouchdownLightsInfractions;
        public int LightsOrSpeedInfractions => LightsInfractions + _speed250Infractions;
    }
}
