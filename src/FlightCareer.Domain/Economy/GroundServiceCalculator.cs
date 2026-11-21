using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Economy
{
    public class GroundServiceCalculator
    {
        private readonly AirportFeeCatalog _catalog;

        public GroundServiceCalculator(AirportFeeCatalog? catalog = null)
        {
            _catalog = catalog ?? AirportFeeCatalog.Default;
        }

        public IReadOnlyList<FeeLine> CalculateFees(FeeContext context)
        {
            return Calculate(context, _catalog);
        }

        public static IReadOnlyList<FeeLine> Calculate(FeeContext context, AirportFeeCatalog? catalog = null)
        {
            catalog ??= AirportFeeCatalog.Default;

            // Tier 4 (pistas remotas/rurais) ou tiers inválidos não cobram nenhuma taxa
            if (context.AirportTier <= 0 || context.AirportTier >= 4)
            {
                return Array.Empty<FeeLine>();
            }

            var lines = new List<FeeLine>();
            var sizeClass = catalog.Classify(context.MtowLbs);

            // 1. LANDING — Taxa de pouso baseada em base fixa + taxa por lb MTOW
            if (catalog.LandingTiers.TryGetValue(context.AirportTier, out var landingConfig))
            {
                double landingTotal = Math.Round(landingConfig.BaseFee + (context.MtowLbs * landingConfig.PerLbFee), 2);
                if (landingTotal > 0)
                {
                    lines.Add(new FeeLine(
                        "LANDING",
                        $"Taxa de Pouso em {context.Icao} (Tier {context.AirportTier}, MTOW: {context.MtowLbs:N0} lbs)",
                        1.0,
                        landingTotal,
                        landingTotal
                    ));
                }
            }

            // 2. PAX_FEE — Taxa de embarque por passageiro (somente com missões e passageiros > 0)
            if (context.HasMission && context.Passengers > 0)
            {
                if (catalog.PassengerFeeTiers.TryGetValue(context.AirportTier, out double paxRate) && paxRate > 0)
                {
                    double paxTotal = Math.Round(context.Passengers * paxRate, 2);
                    if (paxTotal > 0)
                    {
                        lines.Add(new FeeLine(
                            "PAX_FEE",
                            $"Taxa de Embarque em {context.Icao} ({context.Passengers} pax)",
                            context.Passengers,
                            paxRate,
                            paxTotal
                        ));
                    }
                }
            }

            // 3. HANDLING — Rampa para médio/pesado em tier 1 e 2 com missão
            if (context.HasMission && sizeClass != AircraftSizeClass.Light && (context.AirportTier == 1 || context.AirportTier == 2))
            {
                if (catalog.HandlingTiers.TryGetValue(context.AirportTier, out var handlingConfig))
                {
                    double rate = sizeClass == AircraftSizeClass.Heavy ? handlingConfig.Heavy : handlingConfig.Medium;
                    double handlingTotal = Math.Round(rate, 2);
                    if (handlingTotal > 0)
                    {
                        string sizeDesc = sizeClass == AircraftSizeClass.Heavy ? "Porte Pesado" : "Porte Médio";
                        lines.Add(new FeeLine(
                            "HANDLING",
                            $"Handling de Rampa em {context.Icao} ({sizeDesc})",
                            1.0,
                            handlingTotal,
                            handlingTotal
                        ));
                    }
                }
            }

            // 4. NAV_TERMINAL — Tarifa de navegação por 1.000 lb MTOW para médio/pesado em tier 1–3
            if (sizeClass != AircraftSizeClass.Light && (context.AirportTier >= 1 && context.AirportTier <= 3))
            {
                var navConfig = catalog.NavTerminal;
                string sizeKey = sizeClass == AircraftSizeClass.Heavy ? "heavy" : "medium";
                bool tierApplies = navConfig.ApplicableTiers == null || navConfig.ApplicableTiers.Contains(context.AirportTier);
                bool sizeApplies = navConfig.ApplicableSizeClasses == null || navConfig.ApplicableSizeClasses.Contains(sizeKey);

                if (tierApplies && sizeApplies)
                {
                    double thousandLbs = context.MtowLbs / 1000.0;
                    double navTotal = Math.Round(thousandLbs * navConfig.FeePerThousandLbs, 2);
                    if (navTotal > 0)
                    {
                        lines.Add(new FeeLine(
                            "NAV_TERMINAL",
                            $"Tarifa de Navegação/Terminal em {context.Icao} ({context.MtowLbs:N0} lbs)",
                            thousandLbs,
                            navConfig.FeePerThousandLbs,
                            navTotal
                        ));
                    }
                }
            }

            // 5. PARKING — Estacionamento acima da franquia por tier × porte (calculado e testado; medido no sim na F2)
            var parkingConfig = catalog.Parking;
            if (context.ParkedGameHours > parkingConfig.FreeHours && (context.AirportTier >= 1 && context.AirportTier <= 3))
            {
                if (parkingConfig.Tiers != null && parkingConfig.Tiers.TryGetValue(context.AirportTier, out var rates))
                {
                    double hourlyRate = sizeClass switch
                    {
                        AircraftSizeClass.Light => rates.Light,
                        AircraftSizeClass.Medium => rates.Medium,
                        AircraftSizeClass.Heavy => rates.Heavy,
                        _ => 0.0
                    };

                    double billableHours = context.ParkedGameHours - parkingConfig.FreeHours;
                    double parkingTotal = Math.Round(billableHours * hourlyRate, 2);
                    if (parkingTotal > 0)
                    {
                        lines.Add(new FeeLine(
                            "PARKING",
                            $"Estacionamento em {context.Icao} ({billableHours:F1} h faturadas além de {parkingConfig.FreeHours:F0} h franquia)",
                            billableHours,
                            hourlyRate,
                            parkingTotal
                        ));
                    }
                }
            }

            return lines;
        }
    }
}
