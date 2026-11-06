using CommunityToolkit.Mvvm.ComponentModel;
using FlightCareer.Domain.Entities;
using System;

namespace FlightCareer.UI.ViewModels
{
    public partial class MissionDisplayItem : ObservableObject
    {
        public Mission Mission { get; }

        public Guid Id => Mission.Id;
        public string Title => Mission.Title;
        public string Description => Mission.Description;
        public string OriginICAO => Mission.OriginICAO;
        public string DestinationICAO => Mission.DestinationICAO;
        public double DistanceNM => Mission.DistanceNM;
        public double PayloadLbs => Mission.PayloadLbs;
        public int PassengerCount => Mission.PassengerCount;
        public double LuggageWeightLbs => Mission.LuggageWeightLbs;
        public double CargoWeightLbs => Mission.CargoWeightLbs;
        public double RewardMoney => Mission.RewardMoney;
        public int ExperienceGain => Mission.ExperienceGain;
        public PilotCategory RequiredCategory => Mission.RequiredCategory;
        public string MissionCategoryType => Mission.MissionCategoryType;
        public DateTime ExpirationTime => Mission.ExpirationTime;

        [ObservableProperty] private string _destinationName = string.Empty;
        [ObservableProperty] private string _destinationCity = string.Empty;
        [ObservableProperty] private string _destinationCountry = string.Empty;
        [ObservableProperty] private double _destLat;
        [ObservableProperty] private double _destLon;
        [ObservableProperty] private int _destTier = 3;

        [ObservableProperty] private bool _isSelectedForBatch;
        [ObservableProperty] private bool _isOverweight;
        [ObservableProperty] private bool _isEligibleForPilot = true;
        [ObservableProperty] private string _ineligibilityReason = string.Empty;

        public event Action<MissionDisplayItem, bool>? BatchSelectionChanged;

        partial void OnIsSelectedForBatchChanged(bool value)
        {
            BatchSelectionChanged?.Invoke(this, value);
        }

        // Tag Curta para Badge da Categoria
        public string CategoryShortTag => MissionCategoryType switch
        {
            var t when t.Contains("Instrução") || t.Contains("Treinamento") => "INSTRUÇÃO",
            var t when t.Contains("Táxi") || t.Contains("Charter") => "TÁXI AÉREO",
            var t when t.Contains("Linha Regional") => "REGIONAL",
            var t when t.Contains("Linha Regular") || t.Contains("Heavy") => "REGULAR",
            var t when t.Contains("SAE") => "SAE",
            var t when t.Contains("Carga") => "CARGA",
            var t when t.Contains("Saúde") || t.Contains("Remoção") => "MÉDICA",
            _ => "GERAL"
        };

        // Indicadores de Performance & Lucratividade
        public double ProfitPerNM => DistanceNM > 0 ? Math.Round(RewardMoney / DistanceNM, 1) : 0;
        public string ProfitPerNmDisplay => $"${ProfitPerNM:F1}/NM";

        // Badges Cromáticos Semânticos (Dark Mode Avionics)
        public string CategoryColor => MissionCategoryType switch
        {
            var t when t.Contains("Instrução") || t.Contains("Treinamento") => "#E040FB", // Magenta GPS / IFR
            var t when t.Contains("Táxi") || t.Contains("Charter") => "#00E676",          // Verde Lucro / VFR
            var t when t.Contains("Linha Regional") => "#00E5FF",                          // Ciano Aviônica
            var t when t.Contains("Linha Regular") || t.Contains("Heavy") => "#00B0FF",    // Ciano / Azul Elétrico
            var t when t.Contains("SAE") || t.Contains("Carga") => "#FFB300",              // Âmbar / Alerta
            var t when t.Contains("Saúde") || t.Contains("Remoção") => "#FF5252",          // Vermelho Aviso
            _ => "#8B9BAE"
        };

        public string CategoryBadgeBackground => MissionCategoryType switch
        {
            var t when t.Contains("Instrução") || t.Contains("Treinamento") => "#2B123A",
            var t when t.Contains("Táxi") || t.Contains("Charter") => "#0D2B1E",
            var t when t.Contains("Linha Regional") => "#0D2A3A",
            var t when t.Contains("Linha Regular") || t.Contains("Heavy") => "#0E2338",
            var t when t.Contains("SAE") || t.Contains("Carga") => "#2D220D",
            var t when t.Contains("Saúde") || t.Contains("Remoção") => "#2E1214",
            _ => "#141C24"
        };

        // Estimativas de Pré-Despacho (calculadas com a aeronave ativa)
        [ObservableProperty] private string _eteFormatted = "--";
        [ObservableProperty] private double _estimatedFuelGallons;
        [ObservableProperty] private double _estimatedFuelLbs;

        // Meteorologia Simulada Determinística (METAR Simplificado)
        [ObservableProperty] private string _weatherCategory = "VFR";
        [ObservableProperty] private string _weatherColor = "#10B981";
        [ObservableProperty] private string _weatherIcon = "☀️";
        [ObservableProperty] private string _weatherSummary = "Céu Claro • Vis > 10 km • Vento Calmo";

        public MissionDisplayItem(Mission mission, Airport? destAirport, PilotAircraft? activeAircraft, PilotCategory pilotCategory)
        {
            Mission = mission;

            if (destAirport != null)
            {
                DestinationName = destAirport.Name;
                DestinationCity = string.IsNullOrWhiteSpace(destAirport.City) ? destAirport.Country : destAirport.City;
                DestinationCountry = destAirport.Country;
                DestLat = destAirport.Latitude;
                DestLon = destAirport.Longitude;
                DestTier = destAirport.Tier;
            }
            else
            {
                DestinationName = mission.DestinationICAO;
                DestinationCity = "Destino";
            }

            // Checagem de Homologação
            if (mission.RequiredCategory > pilotCategory)
            {
                IsEligibleForPilot = false;
                IneligibilityReason = $"Exige {mission.RequiredCategory.GetDisplayName()}";
            }

            // Cálculos com Aeronave Ativa
            if (activeAircraft != null)
            {
                var type = activeAircraft.AircraftType;
                double speed = type?.CruiseSpeedKts > 0 ? type.CruiseSpeedKts : 110.0;
                double gph = type?.FuelConsumptionGPH > 0 ? type.FuelConsumptionGPH : 8.0;
                double maxPayload = type?.MaxPayloadLbs > 0 ? type.MaxPayloadLbs : 500.0;

                IsOverweight = mission.PayloadLbs > maxPayload;

                double hours = Math.Max(0.1, mission.DistanceNM / speed);
                int totalMinutes = (int)Math.Round(hours * 60.0);
                int h = totalMinutes / 60;
                int m = totalMinutes % 60;
                EteFormatted = h > 0 ? $"{h}h {m:D2}m" : $"{m} min";

                EstimatedFuelGallons = Math.Round(hours * gph, 1);
                EstimatedFuelLbs = Math.Round(EstimatedFuelGallons * 6.0, 0); // 1 gal Avgas ~ 6 lbs
            }

            // Gerar Condições Meteorológicas (METAR Simplificado determinístico pelo ICAO e dia do ano)
            GenerateWeather(mission.DestinationICAO);
        }

        private void GenerateWeather(string icao)
        {
            int seed = Math.Abs(icao.GetHashCode() ^ (DateTime.Today.Year * 1000 + DateTime.Today.DayOfYear));
            var rng = new Random(seed);
            int roll = rng.Next(100);

            if (roll < 72)
            {
                // VFR (72% dos casos)
                WeatherCategory = "VFR";
                WeatherColor = "#00E676"; // Verde Lucro / VFR
                WeatherIcon = "☀️";
                int windDir = rng.Next(1, 36) * 10;
                int windSpd = rng.Next(3, 12);
                WeatherSummary = $"CÉU CLARO • Vento {windDir:D3}/{windSpd:D2}kt • Vis > 10 km • Teto Ilimitado";
            }
            else if (roll < 90)
            {
                // MVFR (18% dos casos)
                WeatherCategory = "MVFR";
                WeatherColor = "#FFB300"; // Âmbar / Alerta
                WeatherIcon = "⛅";
                int windDir = rng.Next(1, 36) * 10;
                int windSpd = rng.Next(8, 16);
                int ceiling = rng.Next(15, 30) * 100;
                WeatherSummary = $"PARCIALMENTE NUBLADO • Vento {windDir:D3}/{windSpd:D2}kt • Vis 6-8 km • Teto {ceiling:N0}ft";
            }
            else
            {
                // IFR (10% dos casos)
                WeatherCategory = "IFR";
                WeatherColor = "#FF5252"; // Vermelho Aviso / IFR
                WeatherIcon = "🌧️";
                int windDir = rng.Next(1, 36) * 10;
                int windSpd = rng.Next(14, 25);
                int ceiling = rng.Next(5, 10) * 100;
                WeatherSummary = $"CHUVA LEVE / NÉVOA • Vento {windDir:D3}/{windSpd:D2}G{windSpd+6}kt • Vis 3-4 km • Teto {ceiling:N0}ft";
            }
        }
    }
}
