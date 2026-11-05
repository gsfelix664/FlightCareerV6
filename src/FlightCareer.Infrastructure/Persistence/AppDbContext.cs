using FlightCareer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<Pilot> Pilots { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<AircraftType> AircraftTypes { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Runway> Runways { get; set; }
        public DbSet<PilotAircraft> PilotAircrafts { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pilot>()
                .HasMany(p => p.Flights)
                .WithOne(f => f.Pilot)
                .HasForeignKey(f => f.PilotId);

            modelBuilder.Entity<Pilot>()
                .HasMany(p => p.Hangar)
                .WithOne(pa => pa.Pilot)
                .HasForeignKey(pa => pa.PilotId);

            modelBuilder.Entity<Airport>()
                .HasIndex(a => a.ICAO)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
