using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Domain.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.UI.ViewModels
{
    public partial class CreatePilotViewModel : ObservableObject
    {
        private readonly IPilotRepository _pilotRepository;
        private readonly Infrastructure.Persistence.AppDbContext _context;

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _companyName = string.Empty;
        [ObservableProperty] private string _initialBaseICAO = string.Empty;
        [ObservableProperty] private DifficultyMode _selectedDifficulty = DifficultyMode.Easy;

        private readonly Infrastructure.Services.PartMigrationService _partProvisioner;

        public CreatePilotViewModel(IPilotRepository pilotRepository, Infrastructure.Persistence.AppDbContext context,
            Infrastructure.Services.PartMigrationService partProvisioner)
        {
            _pilotRepository = pilotRepository;
            _context = context;
            _partProvisioner = partProvisioner;
            CreateCommand = new AsyncRelayCommand(CreatePilotAsync);
        }

        public IAsyncRelayCommand CreateCommand { get; }
        public Action? RequestClose { get; set; }

        private async Task CreatePilotAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                System.Windows.MessageBox.Show("Por favor, informe o Nome do Piloto.", "Campo Obrigatório", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                System.Windows.MessageBox.Show("Por favor, informe o Nome da Companhia.", "Campo Obrigatório", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(InitialBaseICAO) || InitialBaseICAO.Trim().Length < 3)
            {
                System.Windows.MessageBox.Show("Por favor, informe um código ICAO válido para a base inicial (ex: SBMT, SBGR, etc.).", "Campo Obrigatório", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            string icaoCheck = InitialBaseICAO.Trim().ToUpper();
            var airportCheck = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == icaoCheck);
            if (airportCheck == null)
            {
                System.Windows.MessageBox.Show($"O aeroporto com código ICAO '{icaoCheck}' não foi encontrado no banco de dados.", "Aeroporto Inválido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            double initialBalance = CareerStart.GetInitialBalance(SelectedDifficulty);

            var pilot = new Pilot
            {
                Id = Guid.NewGuid(),
                Name = Name,
                CompanyName = CompanyName,
                InitialBaseICAO = InitialBaseICAO.ToUpper(),
                CurrentAirportICAO = InitialBaseICAO.ToUpper(),
                Balance = initialBalance,
                Difficulty = SelectedDifficulty,
                Rank = "Student Pilot",
                CareerPhase = CareerPhase.Phase1_FlightSchool,
                HasPPLicense = true
            };

            // Buscar aeroporto para determinar o prefixo pelo país
            var airport = await _context.Airports.FirstOrDefaultAsync(a => a.ICAO == pilot.InitialBaseICAO);
            string country = airport?.Country ?? "BR";
            string generatedPrefix = GeneratePrefixByCountry(country);

            var c152 = await _context.AircraftTypes.FirstOrDefaultAsync(a => a.Model.Contains("152"));
            if (c152 != null)
            {
                var startingAircraft = new PilotAircraft
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    AircraftTypeId = c152.Id,
                    PurchaseDate = DateTime.Now,
                    CurrentAirframeHours = CareerStart.DefaultStartingAirframeHours, // C152 padrão começa com horas
                    Prefix = generatedPrefix,
                    EngineCondition = 100,
                    TiresCondition = 100,
                    AirframeCondition = 100,
                    FuelAmount = c152.FuelTankGallons * CareerStart.DefaultStartingFuelFraction, // Começa com meio tanque
                    CurrentLocationICAO = pilot.InitialBaseICAO,
                    EngineTboRemainingHours = CareerStart.DefaultStartingTboRemainingHours,
                    HoursUntilNext50hInspection = CareerStart.DefaultStarting50hHours,
                    HoursUntilNext100hInspection = CareerStart.DefaultStarting100hHours,
                    HoursUntilNext500hInspection = CareerStart.DefaultStarting500hHours,
                    IsInterdicted = false,
                    InterdictionReason = string.Empty,
                    Configuration = "Passageiro"
                };
                // Peças já na criação (como na compra): sem elas o 1º voo usa o desgaste antigo e o Hangar mostra %
                await ProvisionStartingPartsAsync(startingAircraft, c152.Model);
                pilot.Hangar.Add(startingAircraft);
                pilot.ActiveAircraftId = startingAircraft.Id;
            }

            await _pilotRepository.AddAsync(pilot);
            await _pilotRepository.SaveChangesAsync();

            RequestClose?.Invoke();
        }

        private async Task ProvisionStartingPartsAsync(PilotAircraft aircraft, string model)
        {
            var profile = await _context.AircraftLimitProfiles
                .FirstOrDefaultAsync(p => p.AircraftTypeModel.ToLower() == model.ToLower());
            if (profile == null)
            {
                // A abertura do app (AircraftPartProvisioner) ainda cria as peças depois
                System.Diagnostics.Debug.WriteLine($"[Peças] Sem perfil de limites para '{model}': avião inicial criado sem peças.");
                return;
            }

            var features = new System.Collections.Generic.List<string>();
            try
            {
                features = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(profile.FeaturesJson) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Peças] Falha ao ler FeaturesJson de '{model}': {ex.Message}");
            }

            _partProvisioner.ProvisionPurchasedAircraft(aircraft, features, profile.EngineCount,
                aircraft.EngineCondition, aircraft.TiresCondition, aircraft.AirframeCondition, aircraft.AvionicsCondition);
        }

        private string GeneratePrefixByCountry(string countryCode)
        {
            var rng = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string suffix = new string(Enumerable.Repeat(chars, 3).Select(s => s[rng.Next(s.Length)]).ToArray());

            return countryCode.ToUpper() switch
            {
                "BR" or "BRAZIL" => (rng.Next(0, 2) == 0 ? "PT-" : "PR-") + suffix,
                "US" or "UNITED STATES" => "N" + rng.Next(100, 999) + new string(Enumerable.Repeat(chars, 2).Select(s => s[rng.Next(s.Length)]).ToArray()),
                "FR" or "FRANCE" => "F-" + suffix,
                "GB" or "UNITED KINGDOM" => "G-" + suffix,
                _ => "PP-" + suffix // Fallback Brasil
            };
        }
    }
}
