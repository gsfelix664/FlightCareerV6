using System.Collections.Generic;

namespace FlightCareer.Domain.Services
{
    public enum HubType
    {
        Instruction,
        Cargo,
        Executive,
        CommercialPax,
        Maintenance,
        Feeder,
        Agro,
        Health
    }

    public interface IHubDataService
    {
        bool IsInstructionHub(string icao);
        bool IsCargoHub(string icao);
        bool IsExecutiveHub(string icao);
        bool IsCommercialPaxHub(string icao);
        bool IsMaintenanceHub(string icao);
        bool IsFeederHub(string icao);
        bool IsAgroHub(string icao);
        bool IsHealthHub(string icao);

        bool IsHubOfType(string icao, HubType type);
        List<HubType> GetHubTypesForAirport(string icao);

        IReadOnlyList<string> GetHubs(HubType type, string? country = null);
    }
}
