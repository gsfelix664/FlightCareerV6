using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Services
{
    /// <summary>
    /// Amostra instantânea de parâmetros de voo para avaliação da qualidade e conforto.
    /// PitchUpDeg: positivo com o nariz para cima (no MSFS, PLANE PITCH DEGREES é negativo para cima).
    /// </summary>
    public record FlightQualitySample(
        bool OnGround,
        double GForce,
        double BankDeg,
        double PitchUpDeg,
        double? LoadFactorYDot = null,
        double? BetaDeg = null,
        double? WindVerticalFps = null,
        bool? MotionPaused = null)
    {
        public static FlightQualitySample FromTelemetry(SimTelemetry data) =>
            new(
                OnGround: data.IsOnGround,
                GForce: data.GForce,
                BankDeg: data.PlaneBank,
                PitchUpDeg: -data.PlanePitch,
                LoadFactorYDot: null,
                BetaDeg: null,
                WindVerticalFps: null,
                MotionPaused: null
            );
    }
}
