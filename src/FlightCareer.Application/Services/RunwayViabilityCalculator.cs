using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Enums;
using FlightCareer.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Application.Services
{
    public class RunwayViabilityCalculator : IRunwayViabilityCalculator
    {
        public RunwayViabilityResult CalculateViability(
            AircraftType aircraftType, 
            Airport airport, 
            IEnumerable<Runway>? runways, 
            PilotCategory category, 
            bool isWetOrContaminated = false)
        {
            var result = new RunwayViabilityResult();

            int minRunwayBase = aircraftType.MinRunwayLength > 0 ? aircraftType.MinRunwayLength : 1500;

            // 1. Fator de Segurança Operacional:
            // 1.25x para VFR Geral / Instrução (Cat 1/2)
            // 1.67x para RBAC 135 / Táxi Aéreo / Charter / Linha Aérea (Cat 3/4/5)
            double safetyFactor = (category == PilotCategory.Category1_FlightSchool || category == PilotCategory.Category2_SAE)
                ? 1.25
                : 1.67;

            // 2. Acréscimo por Elevação (Densidade do Ar):
            // ~10% a cada 1.000 pés de elevação acima do nível do mar
            double elevationFt = Math.Max(0, airport.ElevationFt);
            double elevationMultiplier = 1.0 + (elevationFt / 1000.0 * 0.10);

            // 3. Avaliação das Pistas Disponíveis no Aeródromo
            var runwayList = runways?.ToList() ?? new List<Runway>();
            
            // Se o aeroporto não possui cadastro detalhado de pista individual,
            // assume estimativa baseada no Tier
            int maxAvailableRunway = 0;
            string bestSurface = "Asphalt";
            bool hasHardRunway = false;

            if (runwayList.Any())
            {
                var bestRunway = runwayList.OrderByDescending(r => r.LengthFt).First();
                maxAvailableRunway = bestRunway.LengthFt;
                bestSurface = bestRunway.Surface;
                hasHardRunway = runwayList.Any(r => r.SurfaceType == RunwaySurface.Asphalt || 
                                                    r.SurfaceType == RunwaySurface.Concrete || 
                                                    r.Surface.ToUpperInvariant().Contains("BIT") ||
                                                    r.Surface.ToUpperInvariant().Contains("HARD"));
            }
            else
            {
                // Fallback por Tier
                maxAvailableRunway = airport.Tier switch
                {
                    1 => 10000,
                    2 => 6500,
                    3 => 3500,
                    4 => 1800,
                    _ => 2000
                };
                hasHardRunway = airport.Tier <= 3;
                bestSurface = airport.Tier <= 3 ? "Asphalt" : "Grass";
            }

            // 4. Penalidade de Superfície:
            // Pistas não pavimentadas (grama molhada, terra fofa, cascalho): +20% a +35% de pista requerida
            double surfaceMultiplier = 1.0;
            bool isUnpaved = !hasHardRunway || bestSurface.Equals("Grass", StringComparison.OrdinalIgnoreCase) || 
                             bestSurface.Equals("Dirt", StringComparison.OrdinalIgnoreCase) ||
                             bestSurface.Equals("Gravel", StringComparison.OrdinalIgnoreCase);

            if (isUnpaved)
            {
                surfaceMultiplier = isWetOrContaminated ? 1.35 : 1.25;
            }
            else if (isWetOrContaminated)
            {
                surfaceMultiplier = 1.15;
            }

            // 5. Cálculo do Comprimento de Pista Requerido:
            // RequiredRunway = MinRunwayBase * (1 + (ElevationFt / 1000) * 0.10) * SafetyFactor * SurfaceMultiplier
            double requiredRunway = minRunwayBase * elevationMultiplier * safetyFactor * surfaceMultiplier;

            result.RequiredRunwayFt = Math.Round(requiredRunway, 0);
            result.AvailableRunwayFt = maxAvailableRunway;
            result.SafetyFactor = safetyFactor;
            result.ElevationMultiplier = Math.Round(elevationMultiplier, 2);
            result.SurfaceMultiplier = Math.Round(surfaceMultiplier, 2);
            result.BestRunwaySurface = bestSurface;

            // 6. Restrição de Tipo/Porte (Evitar Absurdos Operacionais: ex. Boeing 737 em fazenda)
            bool requiresHardSurface = category >= PilotCategory.Category4_Regional || 
                                      aircraftType.MaxPayloadLbs > 8000 || 
                                      aircraftType.Type.Contains("Jet", StringComparison.OrdinalIgnoreCase);

            if (requiresHardSurface && isUnpaved)
            {
                result.IsViable = false;
                result.Reason = $"Operação não homologada: {aircraftType.Model} exige pista pavimentada rígida (Asfalto/Concreto). O aeródromo {airport.ICAO} possui apenas superfície {bestSurface}.";
                return result;
            }

            if (maxAvailableRunway < requiredRunway)
            {
                result.IsViable = false;
                result.Reason = $"Pista insuficiente em {airport.ICAO}: Requerido {result.RequiredRunwayFt:N0} FT (Base: {minRunwayBase} FT, Elev: {airport.ElevationFt} FT, Fator: {safetyFactor:F2}x, Sup: {surfaceMultiplier:F2}x) vs Disponível {maxAvailableRunway:N0} FT.";
                return result;
            }

            result.IsViable = true;
            result.Reason = $"Viabilidade física confirmada: Pista disponível de {maxAvailableRunway:N0} FT atende à exigência operacional de {result.RequiredRunwayFt:N0} FT.";
            return result;
        }
    }
}
