using FlightCareer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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

        public async Task EnsureSchemaUpdatedAsync()
        {
            var connection = Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();

            string[] migrationStatements = new[]
            {
                "ALTER TABLE Pilots ADD COLUMN CompanyTier INTEGER DEFAULT 1;",
                "ALTER TABLE Pilots ADD COLUMN HasPPLicense INTEGER DEFAULT 1;",
                "ALTER TABLE Pilots ADD COLUMN HasCPLicense INTEGER DEFAULT 0;",
                "ALTER TABLE Pilots ADD COLUMN HasIFRRating INTEGER DEFAULT 0;",
                "ALTER TABLE Pilots ADD COLUMN HasMultiEngineRating INTEGER DEFAULT 0;",
                "ALTER TABLE Pilots ADD COLUMN HasATPLicense INTEGER DEFAULT 0;",
                "ALTER TABLE Pilots ADD COLUMN HasCFIInstructor INTEGER DEFAULT 0;",

                "ALTER TABLE PilotAircrafts ADD COLUMN EngineTboRemainingHours REAL DEFAULT 1800;",
                "ALTER TABLE PilotAircrafts ADD COLUMN HoursUntilNext50hInspection REAL DEFAULT 50;",
                "ALTER TABLE PilotAircrafts ADD COLUMN HoursUntilNext100hInspection REAL DEFAULT 100;",
                "ALTER TABLE PilotAircrafts ADD COLUMN IsInterdicted INTEGER DEFAULT 0;",
                "ALTER TABLE PilotAircrafts ADD COLUMN InterdictionReason TEXT DEFAULT '';",
                "ALTER TABLE PilotAircrafts ADD COLUMN Configuration TEXT DEFAULT 'Passageiro';",

                "ALTER TABLE Missions ADD COLUMN RequiredTier INTEGER DEFAULT 1;",
                "ALTER TABLE Missions ADD COLUMN MissionCategoryType TEXT DEFAULT 'Instrucao';"
            };

            foreach (var stmt in migrationStatements)
            {
                try
                {
                    command.CommandText = stmt;
                    await command.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Coluna já existente (ignorar)
                }
            }
        }
    }
}
