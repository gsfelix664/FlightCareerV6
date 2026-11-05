using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.Infrastructure.Repositories
{
    public class FlightRepository : IFlightRepository
    {
        private readonly AppDbContext _context;

        public FlightRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Flight>> GetByPilotIdAsync(Guid pilotId)
        {
            return await _context.Flights
                .Where(f => f.PilotId == pilotId)
                .OrderByDescending(f => f.Date)
                .ToListAsync();
        }

        public async Task AddAsync(Flight flight)
        {
            await _context.Flights.AddAsync(flight);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
