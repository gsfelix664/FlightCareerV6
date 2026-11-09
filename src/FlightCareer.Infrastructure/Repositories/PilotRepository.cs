using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightCareer.Infrastructure.Repositories
{
    public class PilotRepository : IPilotRepository
    {
        private readonly Persistence.AppDbContext _context;

        public PilotRepository(Persistence.AppDbContext context)
        {
            _context = context;
        }

        public async Task<Pilot?> GetByIdAsync(Guid id)
        {
            return await _context.Pilots
                .Include(p => p.Hangar)
                .ThenInclude(h => h.AircraftType)
                .Include(p => p.Flights)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Pilot>> GetAllAsync()
        {
            return await _context.Pilots
                .Include(p => p.Hangar)
                .ThenInclude(h => h.AircraftType)
                .Include(p => p.Flights)
                .ToListAsync();
        }

        public async Task AddAsync(Pilot pilot)
        {
            await _context.Pilots.AddAsync(pilot);
        }

        public async Task UpdateAsync(Pilot pilot)
        {
            _context.Pilots.Update(pilot);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Pilot pilot)
        {
            _context.Pilots.Remove(pilot);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
