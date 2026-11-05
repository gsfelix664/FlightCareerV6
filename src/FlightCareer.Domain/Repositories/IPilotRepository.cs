using FlightCareer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightCareer.Domain.Repositories
{
    public interface IPilotRepository
    {
        Task<Pilot?> GetByIdAsync(Guid id);
        Task<IEnumerable<Pilot>> GetAllAsync();
        Task AddAsync(Pilot pilot);
        Task UpdateAsync(Pilot pilot);
        Task DeleteAsync(Pilot pilot);
        Task SaveChangesAsync();
    }
}
