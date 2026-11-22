using System.Collections.Generic;

namespace FlightCareer.Domain.Xp
{
    public sealed record XpFlightInputs(
        double AirborneMinutes,
        double DistanceNm,
        int LegsCompleted,                  // pernas = decolagem + pouso concluídos
        string? MissionFamilyId,
        bool IsFerryOrNoMission,
        bool UsedTimeAcceleration,
        bool CrashDetectionOn,
        bool AutoCoordinationOn,
        double RealismPct,
        bool AutoStartUsed,
        double WorstTouchdownFpm,
        double WorstTouchdownG,
        int TouchdownCount,
        int VneOrStallInfractions,
        int GExcessInfractionsWithPax,
        int LightsOrSpeedInfractions,
        bool OffRunwayOrExcursion,
        RunwayFacts? Origin,
        RunwayFacts? Destination,
        EnvFacts? Landing,
        double? BlockMinutes,
        double? EteMinutes,
        bool IsUrgentMission);

    public sealed record RunwayFacts(
        double AvailableFt,
        double MinRequiredFt,
        bool Unpaved,
        double ElevationFt);

    public sealed record EnvFacts(
        double CrosswindKt,
        bool IsJet,
        double GustSpreadKt,
        double DensityAltitudeFt,
        bool Night,
        double VisibilityM,
        bool InCloud);

    public sealed record XpBreakdown(
        double Base,
        double MType,
        double MRealism,
        double MQuality,
        double Bonuses,
        int Total,
        IReadOnlyList<string> Lines);
}
