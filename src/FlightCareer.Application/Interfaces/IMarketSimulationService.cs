using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Application.Interfaces
{
    public interface IMarketSimulationService
    {
        Task EnsureMarketInitializedAsync();
        Task ProcessSimulationTicksAsync();
        Task<List<MarketListing>> GetActiveListingsAsync();
        Task<List<MarketListing>> GetRecentlySoldListingsAsync(int count = 5);
        Task<List<MarketListing>> GetPlayerActiveListingsAsync(string pilotName);
        Task<(bool Success, string Message, PilotAircraft? Aircraft)> BuyMarketListingAsync(Guid marketListingId, string pilotName);
        Task<(bool Success, string Message, MarketListing? Listing)> ListPlayerAircraftAsync(Guid playerAircraftId, string pilotName, double askingPrice);
        Task<(bool Success, string Message)> CancelPlayerListingAsync(Guid marketListingId, string pilotName);
        double CalculateBaseMarketValue(AircraftType type, double engineCond, double tiresCond, double airframeCond, double avionicsCond, int year, double airframeHours);
    }
}
