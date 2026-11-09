using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Enums;
using System.Collections.Generic;

namespace FlightCareer.Application.Interfaces
{
    public class RunwayViabilityResult
    {
        public bool IsViable { get; set; }
        public double RequiredRunwayFt { get; set; }
        public double AvailableRunwayFt { get; set; }
        public double SafetyFactor { get; set; }
        public double ElevationMultiplier { get; set; }
        public double SurfaceMultiplier { get; set; }
        public string BestRunwaySurface { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public interface IRunwayViabilityCalculator
    {
        RunwayViabilityResult CalculateViability(
            AircraftType aircraftType, 
            Airport airport, 
            IEnumerable<Runway>? runways, 
            PilotCategory category, 
            bool isWetOrContaminated = false);
    }
}
