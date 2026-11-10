using CommunityToolkit.Mvvm.ComponentModel;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Services;
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

        // Dicionário de Códigos ICAO que divergiram entre a base DECEA/AISWEB e a base do MSFS (Asobo/NavBlue)
        public static readonly System.Collections.Generic.Dictionary<string, string> MsfsIcaoAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "SBPG", "SSZW" }, // Ponta Grossa - PR (Comandante Antonio Amilton Beraldo)
            { "SBCA", "SSKW" }, // Cascavel - PR (Adalberto Mendes da Silva)
            { "SBTD", "SSTW" }, // Toledo - PR (Luiz dal Canalle Filho)
            { "SBPO", "SSPB" }, // Pato Branco - PR (Juvenal Cardoso)
            { "SBPF", "SSPF" }, // Passo Fundo - RS (Lauro Kurtz)
            { "SBNM", "SSAN" }  // Santo Ângelo - RS (Sepé Tiaraju)
        };

        public string? MsfsAliasIcao
        {
            get
            {
                if (!string.IsNullOrEmpty(Mission.DestinationMsfsIdent) && !string.Equals(DestinationICAO, Mission.DestinationMsfsIdent, StringComparison.OrdinalIgnoreCase))
                    return Mission.DestinationMsfsIdent;

                return MsfsIcaoAliases.TryGetValue(DestinationICAO, out var alias) ? alias : null;
            }
        }

        public bool HasMsfsAlias => !string.IsNullOrEmpty(MsfsAliasIcao);
        public string MsfsBadgeText => HasMsfsAlias ? $"MSFS: {MsfsAliasIcao}" : string.Empty;
        public string DestinationIcaoWithMsfsNotice => HasMsfsAlias ? $"{DestinationICAO} (MSFS: {MsfsAliasIcao})" : DestinationICAO;
        public string DestinationTooltip
        {
            get
            {
                string info;
                if (string.IsNullOrWhiteSpace(DestinationCity) || string.Equals(DestinationCity, "Destino", StringComparison.OrdinalIgnoreCase))
                {
                    info = DestinationName;
                }
                else if (string.Equals(DestinationName, DestinationCity, StringComparison.OrdinalIgnoreCase))
                {
                    info = DestinationName;
                }
                else
                {
                    info = $"{DestinationName} - {DestinationCity}";
                }

                return HasMsfsAlias 
                    ? $"{info}\n💡 MSFS 2024: No simulador, este aeroporto utiliza o código '{MsfsAliasIcao}'."
                    : info;
            }
        }

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DestinationTooltip))]
        private string _destinationName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DestinationTooltip))]
        private string _destinationCity = string.Empty;
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

        // Badges Cromáticos Semânticos (Cores apenas para exceções/destaques; neutro sutil para padrão)
        public string SemanticBorderBrush => MissionCategoryType switch
        {
            var t when t.Contains("VIP") => "#F59E0B",
            var t when t.Contains("Saúde") || t.Contains("Remoção") || t.Contains("Médica") => "#EF4444",
            var t when t.Contains("Instrução") || t.Contains("Treinamento") => "#A855F7",
            var t when t.Contains("SAE") || t.Contains("Agro") => "#00E5FF",
            _ => "#2A3749" // Neutro sutil
        };

        public string SemanticBadgeBackground => MissionCategoryType switch
        {
            var t when t.Contains("VIP") => "#2E1E05",
            var t when t.Contains("Saúde") || t.Contains("Remoção") || t.Contains("Médica") => "#2E1214",
            var t when t.Contains("Instrução") || t.Contains("Treinamento") => "#251233",
            var t when t.Contains("SAE") || t.Contains("Agro") => "#0B2730",
            _ => "#161F2A" // Fundo neutro discreto
        };

        public string SemanticTextColor => MissionCategoryType switch
        {
            var t when t.Contains("VIP") => "#F59E0B",
            var t when t.Contains("Saúde") || t.Contains("Remoção") || t.Contains("Médica") => "#EF4444",
            var t when t.Contains("Instrução") || t.Contains("Treinamento") => "#C084FC",
            var t when t.Contains("SAE") || t.Contains("Agro") => "#00E5FF",
            _ => "#94A3B8" // Neutro suave
        };

        public string CategoryColor => SemanticTextColor;
        public string CategoryBadgeBackground => SemanticBadgeBackground;

        public string RequiredCategoryDisplay => RequiredCategory.GetShortName();
        public string RequiredCategoryTooltip => RequiredCategory.GetDisplayName();

        // Metadados Formatados para a Tabela e Cards Operacionais
        public string PayloadFormatted => PassengerCount > 0 
            ? $"{PassengerCount} PAX • {PayloadLbs:N0} lbs" 
            : $"{PayloadLbs:N0} lbs Carga";

        public string MinRunwayDisplay => DestTier switch
        {
            1 => "3.000m+",
            2 => "1.800m",
            3 => "1.200m",
            _ => "800m"
        };

        public string XpDisplay => $"+{ExperienceGain} XP";
        public string FlightRulesDisplay => WeatherCategory == "IFR" ? "IFR" : "VFR";
        public string FlightRulesColor => WeatherCategory == "IFR" ? "#FF5252" : "#00E676";

        public string CargoPaxDetailChip => PassengerCount > 0 
            ? $"👥 {PassengerCount} Passageiro(s) • {LuggageWeightLbs:N0} lbs bagagem" 
            : $"📦 {CargoWeightLbs:N0} lbs Carga Útil";

        // Estimativas de Pré-Despacho (calculadas com a aeronave ativa)
        [ObservableProperty] private string _eteFormatted = "--";
        [ObservableProperty] private double _estimatedFuelGallons;
        [ObservableProperty] private double _estimatedFuelLbs;

        // Proa / Rumo Geográfico (Initial Bearing)
        [ObservableProperty] private double _headingDegrees;
        [ObservableProperty] private string _headingCardinal = string.Empty;
        [ObservableProperty] private string _headingDisplay = "---";

        // Meteorologia Real (NOAA AviationWeather.gov)
        [ObservableProperty] private string _weatherCategory = "---";
        [ObservableProperty] private string _weatherColor = "#8B9BAE";
        [ObservableProperty] private string _weatherIcon = "🌐";
        [ObservableProperty] private string _weatherSummary = "Buscando transmissão METAR da NOAA...";
        [ObservableProperty] private string _rawMetar = string.Empty;
        [ObservableProperty] private bool _isRealMetarLoaded;
        [ObservableProperty] private bool _isNearestStationMetar;
        [ObservableProperty] private string _weatherStationIcao = string.Empty;

        public string WeatherDisplay => $"{WeatherIcon} {WeatherCategory}";
        public string WeatherBadgeBg => WeatherCategory switch
        {
            "LIFR" => "#2E122A",
            "IFR" => "#2E1214",
            "MVFR" => "#2E1E05",
            "VFR" => "#0D2B1E",
            _ => "#161F2A"
        };
        public string WeatherBadgeBorder => WeatherCategory switch
        {
            "LIFR" => "#5A1E54",
            "IFR" => "#5A1E24",
            "MVFR" => "#593D0F",
            "VFR" => "#1B4D3E",
            _ => "#263545"
        };

        public string WeatherTooltip
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RawMetar))
                    return $"Consultando transmissão METAR oficial da NOAA para {DestinationICAO}...";

                if (IsNearestStationMetar)
                    return $"📡 METAR REAL VIA {WeatherStationIcao} (Estação mais próxima do destino):\n{RawMetar}";

                return $"📡 METAR REAL ({DestinationICAO}):\n{RawMetar}";
            }
        }

        public void ApplyRealMetar(FlightCareer.UI.Services.MetarInfo metar)
        {
            WeatherCategory = metar.FlightCategory;
            WeatherColor = metar.CategoryColor;
            WeatherIcon = metar.CategoryIcon;
            RawMetar = metar.RawMetar;
            WeatherSummary = metar.FormattedSummary;
            IsRealMetarLoaded = true;
            IsNearestStationMetar = metar.IsNearestStation;
            WeatherStationIcao = metar.IsNearestStation ? (metar.NearestStationIcao ?? metar.Icao) : metar.Icao;

            OnPropertyChanged(nameof(WeatherDisplay));
            OnPropertyChanged(nameof(WeatherBadgeBg));
            OnPropertyChanged(nameof(WeatherBadgeBorder));
            OnPropertyChanged(nameof(WeatherTooltip));
            OnPropertyChanged(nameof(FlightRulesDisplay));
            OnPropertyChanged(nameof(FlightRulesColor));
        }

        // Expiração do Contrato
        public string ExpirationDisplay
        {
            get
            {
                var remaining = ExpirationTime - DateTime.Now;
                if (remaining.TotalMinutes <= 0) return "EXPIRADO";
                if (remaining.TotalHours < 1) return $"{(int)remaining.TotalMinutes}m rest.";
                if (remaining.TotalHours < 24) return $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
                int days = (int)remaining.TotalDays;
                int hours = remaining.Hours;
                return $"{days}d {hours:D2}h";
            }
        }

        public string ExpirationColor
        {
            get
            {
                var remaining = ExpirationTime - DateTime.Now;
                if (remaining.TotalMinutes <= 0 || remaining.TotalHours < 2) return "#FF5252";
                if (remaining.TotalHours < 6) return "#FFB300";
                return "#94A3B8";
            }
        }

        public string ExpirationBadgeBg => ExpirationColor == "#FF5252" ? "#2E1214" : (ExpirationColor == "#FFB300" ? "#2E1E05" : "#161F2A");
        public string ExpirationBadgeBorder => ExpirationColor == "#FF5252" ? "#5A1E24" : (ExpirationColor == "#FFB300" ? "#593D0F" : "#263545");
        public string ExpirationTooltip => $"Validade do contrato até {ExpirationTime:dd/MM/yyyy HH:mm}";

        public MissionDisplayItem(Mission mission, Airport? destAirport, PilotAircraft? activeAircraft, PilotCategory pilotCategory, Airport? originAirport = null)
        {
            Mission = mission;

            if (destAirport != null)
            {
                DestinationName = !string.IsNullOrWhiteSpace(destAirport.Name) ? destAirport.Name : mission.DestinationICAO;
                DestinationCountry = AviationDataNormalizer.ResolveCountryFromIcao(destAirport.ICAO, destAirport.Country);
                DestLat = destAirport.Latitude;
                DestLon = destAirport.Longitude;
                DestTier = destAirport.Tier;

                if (IsValidCityName(destAirport.City, DestinationCountry))
                {
                    DestinationCity = AviationDataNormalizer.CapitalizeTitle(destAirport.City.Trim());
                }
                else if (AviationDataNormalizer.TryExtractCityFromName(destAirport.Name, out var extractedCity) && !string.IsNullOrWhiteSpace(extractedCity))
                {
                    DestinationCity = extractedCity;
                }
                else if (!string.IsNullOrWhiteSpace(destAirport.Name) && !AviationDataNormalizer.IsFirCode(destAirport.Name) && destAirport.Name.Trim().Length > 2)
                {
                    DestinationCity = AviationDataNormalizer.CapitalizeTitle(destAirport.Name.Trim());
                }
                else
                {
                    DestinationCity = "Destino";
                }
            }
            else
            {
                DestinationName = mission.DestinationICAO;
                DestinationCity = "Destino";
            }

            // Cálculo de Proa / Rumo (Heading Geográfico)
            if (originAirport != null && destAirport != null)
            {
                if (string.Equals(originAirport.ICAO, destAirport.ICAO, StringComparison.OrdinalIgnoreCase))
                {
                    HeadingDegrees = 0;
                    HeadingCardinal = "LOC";
                    HeadingDisplay = "LOCAL";
                }
                else
                {
                    double bearing = CalculateBearing(originAirport.Latitude, originAirport.Longitude, destAirport.Latitude, destAirport.Longitude);
                    HeadingDegrees = Math.Round(bearing);
                    HeadingCardinal = DegreesToCardinal(bearing);
                    HeadingDisplay = $"{HeadingDegrees:000}° {HeadingCardinal}";
                }
            }
            else
            {
                HeadingDisplay = "---";
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
        }

        private static bool IsValidCityName(string? city, string? country)
        {
            if (string.IsNullOrWhiteSpace(city))
                return false;

            string trimmed = city.Trim();

            // Siglas de 2 caracteres (códigos de FIR como "SB", "SS", "K4" ou códigos ISO de país como "BR", "US") não são cidades válidas
            if (trimmed.Length <= 2)
                return false;

            if (AviationDataNormalizer.IsFirCode(trimmed))
                return false;

            if (!string.IsNullOrWhiteSpace(country) && string.Equals(trimmed, country.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double phi1 = lat1 * Math.PI / 180.0;
            double phi2 = lat2 * Math.PI / 180.0;
            double deltaLambda = (lon2 - lon1) * Math.PI / 180.0;

            double y = Math.Sin(deltaLambda) * Math.Cos(phi2);
            double x = Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(deltaLambda);
            double theta = Math.Atan2(y, x);
            double bearing = (theta * 180.0 / Math.PI + 360.0) % 360.0;
            return bearing;
        }

        private static string DegreesToCardinal(double degrees)
        {
            string[] cardinals = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            int index = (int)Math.Round((degrees % 360.0) / 22.5) % 16;
            return cardinals[index];
        }
    }
}
