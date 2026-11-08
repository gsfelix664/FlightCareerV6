using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FlightCareer.Domain.Services;

namespace FlightCareer.Infrastructure.Services
{
    public class HubDataService : IHubDataService
    {
        private readonly HashSet<string> _instructionHubs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _cargoHubs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _executiveHubs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _commercialPaxHubs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _maintenanceHubs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _feederHubs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _agroHubs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _healthHubs = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Dictionary<HubType, List<string>>> _regionalHubs = new(StringComparer.OrdinalIgnoreCase);

        public HubDataService()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "hubs_data.json");
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "hubs_data.json");
                }
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FlightCareer.Infrastructure", "Data", "hubs_data.json");
                }

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[HubDataService] Arquivo hubs_data.json não encontrado em: {filePath}");
                    return;
                }

                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("HubProfiles", out var profilesElem))
                    return;

                foreach (var regionProp in profilesElem.EnumerateObject())
                {
                    var regionKey = regionProp.Name;
                    if (!_regionalHubs.ContainsKey(regionKey))
                    {
                        _regionalHubs[regionKey] = new Dictionary<HubType, List<string>>();
                    }

                    var regionObj = regionProp.Value;

                    ProcessCategory(regionKey, regionObj, "InstructionHubs", HubType.Instruction, _instructionHubs);
                    ProcessCategory(regionKey, regionObj, "CargoHubs", HubType.Cargo, _cargoHubs);
                    ProcessCategory(regionKey, regionObj, "ExecutiveHubs", HubType.Executive, _executiveHubs);
                    ProcessCategory(regionKey, regionObj, "CommercialPaxHubs", HubType.CommercialPax, _commercialPaxHubs);
                    ProcessCategory(regionKey, regionObj, "MaintenanceHubs", HubType.Maintenance, _maintenanceHubs);
                    ProcessCategory(regionKey, regionObj, "FeederHubs", HubType.Feeder, _feederHubs);
                    ProcessCategory(regionKey, regionObj, "AgroHubs", HubType.Agro, _agroHubs);
                    ProcessCategory(regionKey, regionObj, "HealthHubs", HubType.Health, _healthHubs);
                }

                System.Diagnostics.Debug.WriteLine($"[HubDataService] Carregados com sucesso: {_instructionHubs.Count} Instruction, {_cargoHubs.Count} Cargo, {_executiveHubs.Count} Exec, {_commercialPaxHubs.Count} Pax, {_maintenanceHubs.Count} Maint, {_feederHubs.Count} Feeder, {_agroHubs.Count} Agro, {_healthHubs.Count} Health hubs.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HubDataService] Erro ao carregar hubs_data.json: {ex.Message}");
            }
        }

        private void ProcessCategory(string regionKey, JsonElement regionObj, string propertyName, HubType hubType, HashSet<string> globalSet)
        {
            if (regionObj.TryGetProperty(propertyName, out var arrayElem) && arrayElem.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in arrayElem.EnumerateArray())
                {
                    var icao = item.GetString()?.Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(icao))
                    {
                        list.Add(icao);
                        globalSet.Add(icao);
                    }
                }
                _regionalHubs[regionKey][hubType] = list;
            }
        }

        public bool IsInstructionHub(string icao) => !string.IsNullOrEmpty(icao) && _instructionHubs.Contains(icao);
        public bool IsCargoHub(string icao) => !string.IsNullOrEmpty(icao) && _cargoHubs.Contains(icao);
        public bool IsExecutiveHub(string icao) => !string.IsNullOrEmpty(icao) && _executiveHubs.Contains(icao);
        public bool IsCommercialPaxHub(string icao) => !string.IsNullOrEmpty(icao) && _commercialPaxHubs.Contains(icao);
        public bool IsMaintenanceHub(string icao) => !string.IsNullOrEmpty(icao) && _maintenanceHubs.Contains(icao);
        public bool IsFeederHub(string icao) => !string.IsNullOrEmpty(icao) && _feederHubs.Contains(icao);
        public bool IsAgroHub(string icao) => !string.IsNullOrEmpty(icao) && _agroHubs.Contains(icao);
        public bool IsHealthHub(string icao) => !string.IsNullOrEmpty(icao) && _healthHubs.Contains(icao);

        public bool IsHubOfType(string icao, HubType type) => type switch
        {
            HubType.Instruction => IsInstructionHub(icao),
            HubType.Cargo => IsCargoHub(icao),
            HubType.Executive => IsExecutiveHub(icao),
            HubType.CommercialPax => IsCommercialPaxHub(icao),
            HubType.Maintenance => IsMaintenanceHub(icao),
            HubType.Feeder => IsFeederHub(icao),
            HubType.Agro => IsAgroHub(icao),
            HubType.Health => IsHealthHub(icao),
            _ => false
        };

        public List<HubType> GetHubTypesForAirport(string icao)
        {
            var types = new List<HubType>();
            if (string.IsNullOrEmpty(icao)) return types;

            if (IsInstructionHub(icao)) types.Add(HubType.Instruction);
            if (IsCargoHub(icao)) types.Add(HubType.Cargo);
            if (IsExecutiveHub(icao)) types.Add(HubType.Executive);
            if (IsCommercialPaxHub(icao)) types.Add(HubType.CommercialPax);
            if (IsMaintenanceHub(icao)) types.Add(HubType.Maintenance);
            if (IsFeederHub(icao)) types.Add(HubType.Feeder);
            if (IsAgroHub(icao)) types.Add(HubType.Agro);
            if (IsHealthHub(icao)) types.Add(HubType.Health);

            return types;
        }

        public IReadOnlyList<string> GetHubs(HubType type, string? country = null)
        {
            if (!string.IsNullOrEmpty(country) && _regionalHubs.TryGetValue(country, out var typesDict))
            {
                if (typesDict.TryGetValue(type, out var list))
                    return list;
            }

            // Fallback para GLOBAL se não achar o país específico
            if (_regionalHubs.TryGetValue("GLOBAL", out var globalDict))
            {
                if (globalDict.TryGetValue(type, out var globalList))
                    return globalList;
            }

            // Retorna lista consolidada
            return type switch
            {
                HubType.Instruction => new List<string>(_instructionHubs),
                HubType.Cargo => new List<string>(_cargoHubs),
                HubType.Executive => new List<string>(_executiveHubs),
                HubType.CommercialPax => new List<string>(_commercialPaxHubs),
                HubType.Maintenance => new List<string>(_maintenanceHubs),
                HubType.Feeder => new List<string>(_feederHubs),
                HubType.Agro => new List<string>(_agroHubs),
                HubType.Health => new List<string>(_healthHubs),
                _ => Array.Empty<string>()
            };
        }
    }
}
