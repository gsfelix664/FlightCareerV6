using System.Collections.Generic;
using FlightCareer.Domain.Xp;
using Xunit;

namespace FlightCareer.Tests.Xp
{
    public class XpCalculatorTests
    {
        [Fact]
        public void Compute_PlanExample_40Min_60Nm_1Leg_Instrucao_SmoothLanding_Returns185Xp()
        {
            // Exemplo da spec: 40 min no ar, 60 NM, 1 perna → base 160;
            // instrução (1,10), realismo 1,0, toque 200 fpm/1,2 G (×1,05), sem bônus
            // → 160 × 1,10 × 1,05 = 184,8 → 185.
            var inputs = new XpFlightInputs(
                AirborneMinutes: 40,
                DistanceNm: 60,
                LegsCompleted: 1,
                MissionFamilyId: "instrucao",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 200,
                WorstTouchdownG: 1.2,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            // MRealism: sem aceleracao (+0.05) + crash det (+0.05) + autoCoord off (+0.05) + realism 100 (+0.05) = 1.20
            // Mas para o teste bater exatamente com o exemplo de "realismo 1.0", podemos definir um cenário neutro:
            var customRealismRules = new XpRules
            {
                Realism = new XpRealismRules(
                    BaseValue: 1.0,
                    NoTimeAccelerationBonus: 0.0,
                    CrashDetectionBonus: 0.0,
                    AutoCoordinationOffBonus: 0.0,
                    Realism100Bonus: 0.0,
                    AutoStartPenalty: -0.10,
                    Min: 0.8,
                    Max: 1.2
                )
            };

            var breakdown = XpCalculator.Compute(inputs, customRealismRules);

            Assert.Equal(160.0, breakdown.Base);
            Assert.Equal(1.10, breakdown.MType);
            Assert.Equal(1.00, breakdown.MRealism);
            Assert.Equal(1.05, breakdown.MQuality);
            Assert.Equal(0.0, breakdown.Bonuses);
            Assert.Equal(185, breakdown.Total);
        }

        [Fact]
        public void Compute_WorstTouchdownInTgl_ViolentTouchdownAppliesPenalty()
        {
            // TGL: toques em 90 fpm, 500 fpm e 150 fpm → o pior (500 fpm) vale → penalidade severa −0,30
            var inputs = new XpFlightInputs(
                AirborneMinutes: 30,
                DistanceNm: 20,
                LegsCompleted: 3,
                MissionFamilyId: "instrucao_basica",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 500, // Pior toque registrado
                WorstTouchdownG: 1.8,
                TouchdownCount: 3,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            var breakdown = XpCalculator.Compute(inputs);
            // MQuality = 1.0 - 0.30 = 0.70
            Assert.Equal(0.70, breakdown.MQuality, 2);
        }

        [Fact]
        public void Compute_ShortUnpavedRunwayAtHighElevation_GrantsAllBonuses()
        {
            // Pista curta de terra a 5.000 ft → +40 (curta) + 30 (não pavimentada) + 30 (combo) + 25 (elevação)
            // Total bônus = 125
            var rwy = new RunwayFacts(AvailableFt: 2200, MinRequiredFt: 2000, Unpaved: true, ElevationFt: 5000);
            var landing = new EnvFacts(
                CrosswindKt: 5,
                IsJet: false,
                GustSpreadKt: 2,
                DensityAltitudeFt: 5200,
                Night: false,
                VisibilityM: 9999,
                InCloud: false
            );

            var inputs = new XpFlightInputs(
                AirborneMinutes: 20,
                DistanceNm: 30,
                LegsCompleted: 1,
                MissionFamilyId: "agro_bush",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 120,
                WorstTouchdownG: 1.1,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: rwy,
                Landing: landing,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            var breakdown = XpCalculator.Compute(inputs);
            // Bônus de pista = 40 (curta: 2200 <= 1.25*2000=2500) + 30 (unpaved) + 30 (combo bush: curta + terra + elev > 4000) = 100
            // Bônus de ambiente = 25 (elev > 4000 ft)
            // Total bônus = 125
            Assert.Equal(125.0, breakdown.Bonuses);
        }

        [Fact]
        public void Compute_OffRunway_ZeroesLandingBonusesAndPenalizesQuality()
        {
            var rwy = new RunwayFacts(AvailableFt: 2200, MinRequiredFt: 2000, Unpaved: true, ElevationFt: 5000);
            var landing = new EnvFacts(
                CrosswindKt: 18,
                IsJet: false,
                GustSpreadKt: 12,
                DensityAltitudeFt: 6500,
                Night: false,
                VisibilityM: 2000,
                InCloud: true
            );

            var inputs = new XpFlightInputs(
                AirborneMinutes: 30,
                DistanceNm: 40,
                LegsCompleted: 1,
                MissionFamilyId: "carga_expressa",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 200,
                WorstTouchdownG: 1.2,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: true, // Fora da pista!
                Origin: null,
                Destination: rwy,
                Landing: landing,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            var breakdown = XpCalculator.Compute(inputs);

            // Bônus do destino e ambiente devem ser 0
            Assert.Equal(0.0, breakdown.Bonuses);

            // MQuality = 1.0 + 0.05 (toque firme) - 0.40 (off runway) = 0.65
            Assert.Equal(0.65, breakdown.MQuality, 2);
        }

        [Fact]
        public void Compute_MultiplierCapsAndFloors_AreRespected()
        {
            // Muitas infrações para testar piso de MQuality (0.50)
            var inputsLow = new XpFlightInputs(
                AirborneMinutes: 20,
                DistanceNm: 10,
                LegsCompleted: 1,
                MissionFamilyId: "carga_expressa",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: true,
                CrashDetectionOn: false,
                AutoCoordinationOn: true,
                RealismPct: 50,
                AutoStartUsed: true, // -0.10
                WorstTouchdownFpm: 800, // -0.30
                WorstTouchdownG: 3.5,
                TouchdownCount: 1,
                VneOrStallInfractions: 5, // -0.75
                GExcessInfractionsWithPax: 4, // -0.60
                LightsOrSpeedInfractions: 4, // -0.20
                OffRunwayOrExcursion: true, // -0.40
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            var breakdownLow = XpCalculator.Compute(inputsLow);
            Assert.Equal(0.90, breakdownLow.MRealism); // 1.0 - 0.10 da partida automática
            Assert.Equal(0.50, breakdownLow.MQuality); // Piso MQuality

            // Testar piso de MRealism com penalidade excessiva
            var rulesFloorRealism = new XpRules { Realism = new XpRealismRules(AutoStartPenalty: -0.50, Min: 0.80, Max: 1.20) };
            var breakdownFloor = XpCalculator.Compute(inputsLow, rulesFloorRealism);
            Assert.Equal(0.80, breakdownFloor.MRealism); // Piso 0.80 respeitado após clamp

            // Máxima qualidade para testar teto (1.15)
            var inputsHigh = new XpFlightInputs(
                AirborneMinutes: 20,
                DistanceNm: 10,
                LegsCompleted: 1,
                MissionFamilyId: "medevac",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 80, // Manteiga (+0.15)
                WorstTouchdownG: 1.05,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            var breakdownHigh = XpCalculator.Compute(inputsHigh);
            Assert.Equal(1.20, breakdownHigh.MRealism); // Teto MRealism
            Assert.Equal(1.15, breakdownHigh.MQuality); // Teto MQuality
            Assert.Equal(1.50, breakdownHigh.MType);    // medevac
        }

        [Fact]
        public void Compute_UnknownFamily_UsesFallback1_0()
        {
            var inputs = new XpFlightInputs(
                AirborneMinutes: 10,
                DistanceNm: 10,
                LegsCompleted: 1,
                MissionFamilyId: "familia_desconhecida_xyz",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 150,
                WorstTouchdownG: 1.2,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            var breakdown = XpCalculator.Compute(inputs);
            Assert.Equal(1.00, breakdown.MType);
        }

        [Fact]
        public void Compute_FerryOrNoMission_Uses0_80()
        {
            var inputs = new XpFlightInputs(
                AirborneMinutes: 10,
                DistanceNm: 10,
                LegsCompleted: 1,
                MissionFamilyId: null,
                IsFerryOrNoMission: true,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 150,
                WorstTouchdownG: 1.2,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: null,
                EteMinutes: null,
                IsUrgentMission: false
            );

            var breakdown = XpCalculator.Compute(inputs);
            Assert.Equal(0.80, breakdown.MType);
        }

        [Theory]
        [InlineData(25, 30, false, 30)] // 25/30 = 83% <= 90% normal -> +30
        [InlineData(25, 30, true, 60)]  // 25/30 = 83% <= 90% urgente -> +60
        [InlineData(31, 30, false, 15)] // 31/30 = 103% (90-115%) -> +15
        [InlineData(40, 30, false, 0)]  // 40/30 = 133% > 115% -> 0
        public void Compute_TimelinessBonus_CalculatesCorrectly(double blockMin, double eteMin, bool urgent, double expectedBonus)
        {
            var inputs = new XpFlightInputs(
                AirborneMinutes: 20,
                DistanceNm: 20,
                LegsCompleted: 1,
                MissionFamilyId: "carga_expressa",
                IsFerryOrNoMission: false,
                UsedTimeAcceleration: false,
                CrashDetectionOn: true,
                AutoCoordinationOn: false,
                RealismPct: 100,
                AutoStartUsed: false,
                WorstTouchdownFpm: 150,
                WorstTouchdownG: 1.2,
                TouchdownCount: 1,
                VneOrStallInfractions: 0,
                GExcessInfractionsWithPax: 0,
                LightsOrSpeedInfractions: 0,
                OffRunwayOrExcursion: false,
                Origin: null,
                Destination: null,
                Landing: null,
                BlockMinutes: blockMin,
                EteMinutes: eteMin,
                IsUrgentMission: urgent
            );

            var breakdown = XpCalculator.Compute(inputs);
            Assert.Equal(expectedBonus, breakdown.Bonuses);
        }
    }
}
