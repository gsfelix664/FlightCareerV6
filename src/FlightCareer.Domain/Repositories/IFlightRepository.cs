using FlightCareer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightCareer.Domain.Repositories
{
    public interface IFlightRepository
    {
        Task<IEnumerable<Flight>> GetByPilotIdAsync(Guid pilotId);
        Task AddAsync(Flight flight);
        Task SaveChangesAsync();
    }
}
