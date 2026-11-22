using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Xp
{
    /// <summary>
    /// Calculador de experiência de voo baseado em esforço × habilidade + bônus fixos.
    /// Especificação do plano xp-proficiencia.md § 6.
    /// </summary>
    public static class XpCalculator
    {
        public static XpBreakdown Compute(XpFlightInputs i, XpRules? rules = null)
        {
            var r = rules ?? XpRules.Default;
            var lines = new List<string>();

            // 1. XP Base (Esforço)
            double baseAirborne = Math.Max(0.0, i.AirborneMinutes) * r.Base.AirborneRate;
            double baseDistance = Math.Max(0.0, i.DistanceNm) * r.Base.DistanceRate;
            double baseLegs = Math.Max(0, i.LegsCompleted) * r.Base.LegBonus;
            double baseXP = baseAirborne + baseDistance + baseLegs;

            lines.Add($"XP Base: {baseXP:F0} ({i.AirborneMinutes:F0} min no ar, {i.DistanceNm:F0} NM, {i.LegsCompleted} perna(s))");

            // 2. M_Tipo (Complexidade da Missão)
            double mType = ResolveTypeMultiplier(i, r, out string typeLabel);
            lines.Add($"Mult. Tipo ({typeLabel}): {mType:F2}x");

            // 3. M_Realismo (Auxílios e Realismo do Simulador)
            double mRealismDelta = 0.0;
            if (!i.UsedTimeAcceleration) mRealismDelta += r.Realism.NoTimeAccelerationBonus;
            if (i.CrashDetectionOn) mRealismDelta += r.Realism.CrashDetectionBonus;
            if (!i.AutoCoordinationOn) mRealismDelta += r.Realism.AutoCoordinationOffBonus;
            if (i.RealismPct >= 100.0) mRealismDelta += r.Realism.Realism100Bonus;
            if (i.AutoStartUsed) mRealismDelta += r.Realism.AutoStartPenalty;

            double mRealism = Math.Clamp(r.Realism.BaseValue + mRealismDelta, r.Realism.Min, r.Realism.Max);
            lines.Add($"Mult. Realismo: {mRealism:F2}x");

            // 4. M_Qualidade (Toque e Infrações Operacionais)
            double mQualityDelta = 0.0;
            if (i.TouchdownCount > 0)
            {
                if (i.WorstTouchdownFpm < r.Quality.ButterFpmMax && i.WorstTouchdownG < r.Quality.ButterGMax)
                {
                    mQualityDelta += r.Quality.ButterBonus;
                }
                else if (i.WorstTouchdownFpm <= r.Quality.FirmFpmMax && i.WorstTouchdownG < r.Quality.FirmGMax)
                {
                    mQualityDelta += r.Quality.FirmBonus;
                }
                else if (i.WorstTouchdownFpm > r.Quality.SevereFpmThreshold || i.WorstTouchdownG > r.Quality.SevereGThreshold)
                {
                    mQualityDelta += r.Quality.SeverePenalty;
                }
                else if (i.WorstTouchdownFpm > r.Quality.FirmFpmMax && i.WorstTouchdownFpm <= r.Quality.HardFpmMax)
                {
                    mQualityDelta += r.Quality.HardPenalty;
                }
            }

            if (i.VneOrStallInfractions > 0)
            {
                mQualityDelta -= i.VneOrStallInfractions * r.Quality.VneOrStallPenalty;
                lines.Add($"- Vne / Estol ({i.VneOrStallInfractions}x): -{i.VneOrStallInfractions * r.Quality.VneOrStallPenalty:F2}");
            }

            if (i.GExcessInfractionsWithPax > 0)
            {
                mQualityDelta -= i.GExcessInfractionsWithPax * r.Quality.GExcessPaxPenalty;
                lines.Add($"- Força G excessiva c/ PAX ({i.GExcessInfractionsWithPax}x): -{i.GExcessInfractionsWithPax * r.Quality.GExcessPaxPenalty:F2}");
            }

            if (i.LightsOrSpeedInfractions > 0)
            {
                mQualityDelta -= i.LightsOrSpeedInfractions * r.Quality.LightsOrSpeedPenalty;
                lines.Add($"- Luzes / Vel. > 250 kt ({i.LightsOrSpeedInfractions}x): -{i.LightsOrSpeedInfractions * r.Quality.LightsOrSpeedPenalty:F2}");
            }

            if (i.OffRunwayOrExcursion)
            {
                mQualityDelta -= r.Quality.OffRunwayPenalty;
                lines.Add($"- Pouso fora de pista / excursão: -{r.Quality.OffRunwayPenalty:F2}");
            }

            double mQuality = Math.Clamp(r.Quality.BaseValue + mQualityDelta, r.Quality.Min, r.Quality.Max);
            lines.Add($"Mult. Qualidade: {mQuality:F2}x");

            // 5. Bônus Fixos Aditivos
            double totalBonuses = 0.0;

            // 5.1 Pista (maior entre Origem e Destino). Fora da pista anula bônus de pista do destino.
            double originRunwayBonus = i.Origin != null ? ComputeRunwayBonus(i.Origin, r, out string originDetail) : 0.0;
            double destRunwayBonus = (!i.OffRunwayOrExcursion && i.Destination != null) ? ComputeRunwayBonus(i.Destination, r, out string destDetail) : 0.0;

            double bestRunwayBonus = Math.Max(originRunwayBonus, destRunwayBonus);
            if (bestRunwayBonus > 0)
            {
                totalBonuses += bestRunwayBonus;
                string chosenDetail = bestRunwayBonus == destRunwayBonus ? "Destino" : "Origem";
                lines.Add($"+{bestRunwayBonus:F0} Bônus de Pista ({chosenDetail})");
            }

            // 5.2 Ambiente no Pouso (anulado se fora de pista)
            if (!i.OffRunwayOrExcursion && i.Landing != null)
            {
                var l = i.Landing;
                double crosswindLimit = l.IsJet ? r.Bonuses.CrosswindThresholdJetKt : r.Bonuses.CrosswindThresholdSingleOrTurbopropKt;
                if (l.CrosswindKt > crosswindLimit)
                {
                    totalBonuses += r.Bonuses.CrosswindBonus;
                    lines.Add($"+{r.Bonuses.CrosswindBonus:F0} Vento Cruzado ({l.CrosswindKt:F0} kt)");
                }

                if (l.GustSpreadKt > r.Bonuses.GustSpreadThresholdKt)
                {
                    totalBonuses += r.Bonuses.GustSpreadBonus;
                    lines.Add($"+{r.Bonuses.GustSpreadBonus:F0} Rajadas ({l.GustSpreadKt:F0} kt)");
                }

                double destElevation = i.Destination?.ElevationFt ?? 0.0;
                if (destElevation > r.Bonuses.HighElevationThresholdFt || l.DensityAltitudeFt > r.Bonuses.HighDensityAltitudeThresholdFt)
                {
                    totalBonuses += r.Bonuses.HighElevationOrDensityAltBonus;
                    lines.Add($"+{r.Bonuses.HighElevationOrDensityAltBonus:F0} Altitude/Densidade Elevada");
                }

                if (l.Night)
                {
                    totalBonuses += r.Bonuses.NightBonus;
                    lines.Add($"+{r.Bonuses.NightBonus:F0} Operação Noturna");
                }

                if (l.VisibilityM < r.Bonuses.ImcMaxVisibilityMeters || l.InCloud)
                {
                    totalBonuses += r.Bonuses.ImcBonus;
                    lines.Add($"+{r.Bonuses.ImcBonus:F0} Condições IMC");
                }
            }

            // 5.3 Prazo
            if (i.BlockMinutes.HasValue && i.EteMinutes.HasValue && i.EteMinutes.Value > 0)
            {
                double ratio = i.BlockMinutes.Value / i.EteMinutes.Value;
                if (ratio <= r.Bonuses.OnTimeRatioMax)
                {
                    double bonus = i.IsUrgentMission ? r.Bonuses.OnTimeBonusUrgent : r.Bonuses.OnTimeBonusNormal;
                    totalBonuses += bonus;
                    lines.Add($"+{bonus:F0} Prazo Cumprido (≤ 90% ETE)");
                }
                else if (ratio <= r.Bonuses.AcceptableTimeRatioMax)
                {
                    totalBonuses += r.Bonuses.AcceptableTimeBonus;
                    lines.Add($"+{r.Bonuses.AcceptableTimeBonus:F0} Prazo Aceitável (90–115% ETE)");
                }
            }

            int total = (int)Math.Round(baseXP * mType * mRealism * mQuality + totalBonuses);
            lines.Add($"Total: {total} XP");

            return new XpBreakdown(baseXP, mType, mRealism, mQuality, totalBonuses, total, lines);
        }

        private static double ResolveTypeMultiplier(XpFlightInputs i, XpRules r, out string label)
        {
            if (i.IsFerryOrNoMission)
            {
                label = "ferry/sem missão";
                return r.TypeMultipliers.GetValueOrDefault("ferryOrNoMission", 0.80);
            }

            string family = i.MissionFamilyId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(family))
            {
                label = "padrão";
                return r.TypeMultipliers.GetValueOrDefault("fallback", 1.00);
            }

            if (r.TypeMultipliers.TryGetValue(family, out double mult))
            {
                label = family;
                return mult;
            }

            label = family;
            return r.TypeMultipliers.GetValueOrDefault("fallback", 1.00);
        }

        private static double ComputeRunwayBonus(RunwayFacts rf, XpRules r, out string detail)
        {
            double bonus = 0.0;
            bool isShort = rf.MinRequiredFt > 0 &&
                           rf.AvailableFt >= rf.MinRequiredFt * r.Bonuses.ShortRunwayMinRatio &&
                           rf.AvailableFt <= rf.MinRequiredFt * r.Bonuses.ShortRunwayMaxRatio;

            bool isUnpaved = rf.Unpaved;
            bool isBushCombo = isShort && isUnpaved && rf.ElevationFt > r.Bonuses.BushComboMinElevationFt;

            if (isShort) bonus += r.Bonuses.ShortRunwayBonus;
            if (isUnpaved) bonus += r.Bonuses.UnpavedBonus;
            if (isBushCombo) bonus += r.Bonuses.BushComboBonus;

            detail = $"Short={isShort}, Unpaved={isUnpaved}, Combo={isBushCombo}";
            return bonus;
        }

        /// <summary>
        /// Estima o XP previsto para uma missão exibida no quadro (XP-PONTAS).
        /// Considera minutos no ar (distância / velocidade de cruzeiro), pernas (1 ou N+1 para circuito/TGL),
        /// M_Tipo da família, realismo 1.0, qualidade 1.0, bônus de pista e bônus de prazo no prazo (+15).
        /// </summary>
        public static int Estimate(
            double distanceNm,
            double cruiseSpeedKts,
            string? missionFamilyOrSubId = null,
            RunwayFacts? origin = null,
            RunwayFacts? destination = null,
            int legs = 1,
            XpRules? rules = null)
        {
            var r = rules ?? XpRules.Default;
            double speed = cruiseSpeedKts > 0 ? cruiseSpeedKts : 100.0;
            double distance = Math.Max(0.0, distanceNm);
            double airborneMinutes = (distance / speed) * 60.0;

            // 1. XP Base
            double baseAirborne = airborneMinutes * r.Base.AirborneRate;
            double baseDistance = distance * r.Base.DistanceRate;
            double baseLegs = Math.Max(1, legs) * r.Base.LegBonus;
            double baseXP = baseAirborne + baseDistance + baseLegs;

            // 2. M_Tipo
            double mType = ResolveTypeMultiplierFromFamilyOrSubId(missionFamilyOrSubId, r);

            // 3 & 4. M_Realismo e M_Qualidade = 1.00
            double mRealism = 1.0;
            double mQuality = 1.0;

            // 5. Bônus de Pista (origem ou destino)
            double originRunwayBonus = origin != null ? ComputeRunwayBonus(origin, r, out _) : 0.0;
            double destRunwayBonus = destination != null ? ComputeRunwayBonus(destination, r, out _) : 0.0;
            double bestRunwayBonus = Math.Max(originRunwayBonus, destRunwayBonus);

            // 6. Prazo "no prazo" (+15)
            double onTimeBonus = r.Bonuses.AcceptableTimeBonus;

            double totalBonuses = bestRunwayBonus + onTimeBonus;
            return (int)Math.Round(baseXP * mType * mRealism * mQuality + totalBonuses);
        }

        private static double ResolveTypeMultiplierFromFamilyOrSubId(string? familyOrSubId, XpRules r)
        {
            if (string.IsNullOrWhiteSpace(familyOrSubId))
                return r.TypeMultipliers.GetValueOrDefault("fallback", 1.00);

            if (r.TypeMultipliers.TryGetValue(familyOrSubId, out double directMult))
                return directMult;

            var resolvedFamily = FlightCareer.Domain.Economy.MissionPayTable.Shared.Resolve(familyOrSubId);
            if (resolvedFamily != null && r.TypeMultipliers.TryGetValue(resolvedFamily.Id, out double mappedMult))
                return mappedMult;

            return r.TypeMultipliers.GetValueOrDefault("fallback", 1.00);
        }
    }
}
