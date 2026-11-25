using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Agrega os resultados observados de todos os rastreadores de contratos operacionais
    /// para avaliação de conclusão de missão no domínio sem expandir assinaturas legadas.
    /// </summary>
    public sealed record MissionTrackerResults(
        IReadOnlySet<string>? VisitedWaypoints = null,
        int TouchAndGoCount = 0,
        int SurveyLegsCompleted = 0,
        bool SkydiveDropCompleted = false,
        bool SkydiveThermalShock = false,
        double SkydiveMaxCoolingRate = 0.0,
        MultiStopTracker? MultiStop = null,
        StolLandingTracker? Stol = null,
        CheckFlightTracker? CheckFlight = null,
        FerryRestrictionMonitor? Ferry = null,
        AerialWorkTracker? AerialWork = null
    );
}
