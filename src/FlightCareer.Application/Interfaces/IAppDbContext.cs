using FlightCareer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace FlightCareer.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Pilot> Pilots { get; }
        DbSet<Flight> Flights { get; }
        DbSet<AircraftType> AircraftTypes { get; }
        DbSet<Airport> Airports { get; }
        DbSet<Runway> Runways { get; }
        DbSet<PilotAircraft> PilotAircrafts { get; }
        DbSet<Mission> Missions { get; }
        DbSet<FinancialTransaction> FinancialTransactions { get; }
        DbSet<MarketListing> MarketListings { get; }

        DatabaseFacade Database { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
