using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Xp;
using Xunit;

namespace FlightCareer.Tests.Xp
{
    public class XpEstimatorAndInfractionTests
    {
        [Fact]
        public void Estimate_60Nm_100Kt_Instrucao_Returns182()
        {
            // Spec: 60 NM a 100 kt, instrução → ≈ (2 × 36 + 30 + 50) × 1,10 + 15 = 182
            int xp = XpCalculator.Estimate(
                distanceNm: 60.0,
                cruiseSpeedKts: 100.0,
                missionFamilyOrSubId: "instrucao",
                origin: null,
                destination: null,
                legs: 1
            );

            Assert.Equal(182, xp);
        }

        [Fact]
        public void Estimate_WithDestinationUnpavedRunway_AddsRunwayBonus()
        {
            // 60 NM a 100 kt com destino não pavimentado: 182 + 30 = 212
            var destRunway = new RunwayFacts(AvailableFt: 3500, MinRequiredFt: 1500, Unpaved: true, ElevationFt: 500);

            int xp = XpCalculator.Estimate(
                distanceNm: 60.0,
                cruiseSpeedKts: 100.0,
                missionFamilyOrSubId: "instrucao",
                origin: null,
                destination: destRunway,
                legs: 1
            );

            Assert.Equal(212, xp);
        }

        [Fact]
        public void FlightInfractionTracker_Stall_EdgeTriggered_IgnoredInInstruction()
        {
            var normalTracker = new FlightInfractionTracker(isInstruction: false);

            // No solo: aviso de estol não conta
            normalTracker.OnTelemetryTick(isAirborne: false, stallWarning: true, airspeedKnots: 40, altitudeFeet: 500, hasOverspeedVne: false);
            Assert.Equal(0, normalTracker.StallInfractions);

            // Borda de subida no ar
            normalTracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 50, altitudeFeet: 1500, hasOverspeedVne: false);
            normalTracker.OnTelemetryTick(isAirborne: true, stallWarning: true, airspeedKnots: 45, altitudeFeet: 1500, hasOverspeedVne: false);
            Assert.Equal(1, normalTracker.StallInfractions);

            // Continua em estol: não incrementa novamente sem borda
            normalTracker.OnTelemetryTick(isAirborne: true, stallWarning: true, airspeedKnots: 44, altitudeFeet: 1400, hasOverspeedVne: false);
            Assert.Equal(1, normalTracker.StallInfractions);

            // Recupera e entra de novo: segunda borda
            normalTracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 65, altitudeFeet: 1200, hasOverspeedVne: false);
            normalTracker.OnTelemetryTick(isAirborne: true, stallWarning: true, airspeedKnots: 43, altitudeFeet: 1100, hasOverspeedVne: false);
            Assert.Equal(2, normalTracker.StallInfractions);

            // Em voo de instrução: estol faz parte do treinamento, não conta
            var instructionTracker = new FlightInfractionTracker(isInstruction: true);
            instructionTracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 50, altitudeFeet: 2000, hasOverspeedVne: false);
            instructionTracker.OnTelemetryTick(isAirborne: true, stallWarning: true, airspeedKnots: 40, altitudeFeet: 2000, hasOverspeedVne: false);
            Assert.Equal(0, instructionTracker.StallInfractions);
            Assert.Equal(0, instructionTracker.VneOrStallInfractions);
        }

        [Fact]
        public void FlightInfractionTracker_Speed250Below10k_EdgeTriggeredOnly()
        {
            var tracker = new FlightInfractionTracker();

            // Abaixo de 10.000 ft, acelerando além de 250 kt: borda
            tracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 245, altitudeFeet: 8000, hasOverspeedVne: false);
            Assert.Equal(0, tracker.Overspeed250Infractions);

            tracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 255, altitudeFeet: 8000, hasOverspeedVne: false);
            Assert.Equal(1, tracker.Overspeed250Infractions);

            // Mantém > 250 kt: sem novo incremento
            tracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 260, altitudeFeet: 8500, hasOverspeedVne: false);
            Assert.Equal(1, tracker.Overspeed250Infractions);

            // Acima de 10.000 ft: permitido voar acima de 250 kt
            tracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 240, altitudeFeet: 12000, hasOverspeedVne: false);
            tracker.OnTelemetryTick(isAirborne: true, stallWarning: false, airspeedKnots: 280, altitudeFeet: 12000, hasOverspeedVne: false);
            Assert.Equal(1, tracker.Overspeed250Infractions);
        }

        [Fact]
        public void FlightInfractionTracker_Lights_TakeoffAndTouchdown()
        {
            var tracker = new FlightInfractionTracker();

            // Decolagem sem luzes (landing e taxi desligadas): infração +1
            tracker.OnTakeoff(landingLightOn: false, taxiLightOn: false);
            Assert.Equal(1, tracker.TakeoffLightsInfractions);
            Assert.Equal(1, tracker.LightsInfractions);

            // Decolagem com ao menos farol de táxi: OK
            var trackerGoodTakeoff = new FlightInfractionTracker();
            trackerGoodTakeoff.OnTakeoff(landingLightOn: false, taxiLightOn: true);
            Assert.Equal(0, trackerGoodTakeoff.TakeoffLightsInfractions);

            // Toque intermediário (TGL) com luzes apagadas, mas liga antes do toque final
            trackerGoodTakeoff.OnTouchdown(landingLightOn: false, taxiLightOn: false, isBounce: false);
            // Liga luzes no toque final
            trackerGoodTakeoff.OnTouchdown(landingLightOn: true, taxiLightOn: false, isBounce: false);
            Assert.Equal(0, trackerGoodTakeoff.FinalTouchdownLightsInfractions);
            Assert.Equal(0, trackerGoodTakeoff.LightsInfractions);

            // Toque final com luzes apagadas: infração no toque final
            tracker.OnTouchdown(landingLightOn: false, taxiLightOn: false, isBounce: false);
            Assert.Equal(1, tracker.FinalTouchdownLightsInfractions);
            Assert.Equal(2, tracker.LightsInfractions); // 1 na decolagem + 1 no toque final
        }

        [Fact]
        public void RunwaySurface_IsUnpaved_ClassifiesCorrectly()
        {
            Assert.False(RunwaySurface.IsUnpaved("ASPHALT"));
            Assert.False(RunwaySurface.IsUnpaved("ASPH"));
            Assert.False(RunwaySurface.IsUnpaved("CONCRETE"));
            Assert.False(RunwaySurface.IsUnpaved("CONC"));
            Assert.False(RunwaySurface.IsUnpaved("HARD"));
            Assert.False(RunwaySurface.IsUnpaved("BITUMINOUS"));
            Assert.False(RunwaySurface.IsUnpaved("PEM"));
            Assert.False(RunwaySurface.IsUnpaved("ASP-G"));

            Assert.True(RunwaySurface.IsUnpaved("GRASS"));
            Assert.True(RunwaySurface.IsUnpaved("DIRT"));
            Assert.True(RunwaySurface.IsUnpaved("GRAVEL"));
            Assert.True(RunwaySurface.IsUnpaved("TURF"));

            Assert.False(RunwaySurface.IsUnpaved(null));
            Assert.False(RunwaySurface.IsUnpaved(""));
        }
    }
}
