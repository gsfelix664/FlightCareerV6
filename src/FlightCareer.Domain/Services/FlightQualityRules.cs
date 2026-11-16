using System;

namespace FlightCareer.Domain.Services
{
    /// <summary>
    /// Limites operacionais de conforto e pesos de penalidade por segundo simulado.
    /// Origem dos limites:
    /// - G (0,4 a 1,6): envelope de conforto de passageiros da FAA/IATA.
    /// - Bank (30°): inclinação padrão de curva suave de passageiro.
    /// - Pitch (nariz para cima 20°, para baixo 15°): conforto de cabine de passageiros.
    /// - Jerk (YDOT 2,0 /s): solavanco vertical excessivo (desconforto e enjoo).
    /// - Slip (Beta 5°): ângulo de derrapagem / voo descoordenado.
    /// - Vento vertical (5 ft/s): corrente vertical atmosférica indicativa de turbulência.
    /// - Fração em turbulência (0,25): atenua penalidade de G/solavanco quando induzido pela atmosfera.
    /// - Pesos: 0,1 ponto/s (G, bank, pitch pax); 0,05 ponto/s (G carga, solavanco/slip pax); 0,025 (solavanco carga).
    /// </summary>
    public record FlightQualityRules(
        double MinGForce = 0.4,
        double MaxGForce = 1.6,
        double MaxBankDeg = 30.0,
        double PitchUpMaxDeg = 20.0,
        double PitchDownMaxDeg = 15.0,
        double JerkThreshold = 2.0,
        double SlipThresholdDeg = 5.0,
        double WindVerticalThresholdFps = 5.0,
        double TurbulenceFactor = 0.25,
        double PaxGWeight = 0.1,
        double PaxBankWeight = 0.1,
        double PaxPitchWeight = 0.1,
        double PaxJerkWeight = 0.05,
        double PaxSlipWeight = 0.05,
        double CargoGWeight = 0.05,
        double CargoJerkWeight = 0.025
    );
}
