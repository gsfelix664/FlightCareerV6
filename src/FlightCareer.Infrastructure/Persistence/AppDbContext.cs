using FlightCareer.Domain.Entities;
using FlightCareer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FlightCareer.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public DbSet<Pilot> Pilots { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<AircraftType> AircraftTypes { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Runway> Runways { get; set; }
        public DbSet<PilotAircraft> PilotAircrafts { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }
        public DbSet<MarketListing> MarketListings { get; set; }

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

            modelBuilder.Entity<Pilot>()
                .Ignore(p => p.CompanyTier);

            modelBuilder.Entity<Mission>()
                .Ignore(m => m.RequiredTier);

            modelBuilder.Entity<MarketListing>(entity =>
            {
                entity.HasOne(m => m.AircraftType)
                    .WithMany()
                    .HasForeignKey(m => m.AircraftTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => m.IsSold);
                entity.HasIndex(m => m.IsPlayerOwned);
            });

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
                "ALTER TABLE Pilots ADD COLUMN PilotCategory INTEGER DEFAULT 1;",
                "UPDATE Pilots SET PilotCategory = CompanyTier WHERE CompanyTier IS NOT NULL AND (PilotCategory IS NULL OR PilotCategory = 1);",
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
                "ALTER TABLE PilotAircrafts ADD COLUMN AvionicsCondition REAL DEFAULT 100.0;",
                "ALTER TABLE PilotAircrafts ADD COLUMN HoursUntilNext500hInspection REAL DEFAULT 500.0;",
                "ALTER TABLE PilotAircrafts ADD COLUMN MagnetosCondition REAL DEFAULT 100.0;",
                "ALTER TABLE PilotAircrafts ADD COLUMN OilCondition REAL DEFAULT 100.0;",
                "ALTER TABLE PilotAircraft ADD COLUMN HoursUntilNext500hInspection REAL DEFAULT 500.0;",
                "ALTER TABLE PilotAircraft ADD COLUMN MagnetosCondition REAL DEFAULT 100.0;",
                "ALTER TABLE PilotAircraft ADD COLUMN OilCondition REAL DEFAULT 100.0;",

                "ALTER TABLE Missions ADD COLUMN RequiredTier INTEGER DEFAULT 1;",
                "ALTER TABLE Missions ADD COLUMN RequiredCategory INTEGER DEFAULT 1;",
                "UPDATE Missions SET RequiredCategory = RequiredTier WHERE RequiredTier IS NOT NULL AND (RequiredCategory IS NULL OR RequiredCategory = 1);",
                "ALTER TABLE Missions ADD COLUMN MissionCategoryType TEXT DEFAULT 'Instrucao';",

                // Cenário MSFS 2024 e Divergências de ICAO
                "ALTER TABLE Airports ADD COLUMN IsInSimulator INTEGER DEFAULT 0;",
                "ALTER TABLE Airports ADD COLUMN MsfsIdent TEXT DEFAULT '';",
                "ALTER TABLE Airports ADD COLUMN ElevationFt INTEGER DEFAULT 0;",
                "CREATE INDEX IF NOT EXISTS IX_Airports_IsInSimulator ON Airports(IsInSimulator);",
                "CREATE INDEX IF NOT EXISTS IX_Airports_MsfsIdent ON Airports(MsfsIdent);",
                "ALTER TABLE Missions ADD COLUMN OriginMsfsIdent TEXT DEFAULT '';",
                "ALTER TABLE Missions ADD COLUMN DestinationMsfsIdent TEXT DEFAULT '';",

                // Normalização de Dados Aeronáuticos e Correção de Bases Existentes
                "UPDATE Airports SET Country = 'BR' WHERE (ICAO LIKE 'SB%' OR ICAO LIKE 'SS%' OR ICAO LIKE 'SW%' OR ICAO LIKE 'SD%' OR ICAO LIKE 'SJ%' OR ICAO LIKE 'SN%' OR ICAO LIKE 'SI%') AND (Country IN ('SB','SS','SW','SJ','SD','SI','SN') OR Country = '' OR Country IS NULL);",
                "UPDATE Airports SET City = 'Ribeirão Branco', Country = 'BR' WHERE ICAO = 'SBWM' AND (City IS NULL OR City = '' OR City = 'SB');",
                "UPDATE Airports SET City = TRIM(SUBSTR(Name, 14)) WHERE (City IS NULL OR City = '' OR City IN ('SB','SS','SW','SJ','SD','SI','SN')) AND Name LIKE 'Aeroporto de %' AND length(Name) > 13;",
                "UPDATE Airports SET City = TRIM(SUBSTR(Name, 13)) WHERE (City IS NULL OR City = '' OR City IN ('SB','SS','SW','SJ','SD','SI','SN')) AND (Name LIKE 'Aeródromo de %' OR Name LIKE 'Aerodromo de %') AND length(Name) > 12;",
                "UPDATE Airports SET City = TRIM(SUBSTR(Name, 24)) WHERE (City IS NULL OR City = '' OR City IN ('SB','SS','SW','SJ','SD','SI','SN')) AND Name LIKE 'Aeroporto Municipal de %' AND length(Name) > 23;",

                // Mercado Vivo (MarketListing)
                @"CREATE TABLE IF NOT EXISTS MarketListings (
                    Id TEXT PRIMARY KEY,
                    AircraftTypeId TEXT NOT NULL,
                    Prefix TEXT NOT NULL,
                    EngineCondition REAL NOT NULL,
                    TiresCondition REAL NOT NULL,
                    AirframeCondition REAL NOT NULL,
                    AvionicsCondition REAL NOT NULL,
                    AirframeHours REAL NOT NULL,
                    YearOfManufacture INTEGER NOT NULL,
                    TboHours REAL NOT NULL,
                    TsmoHours REAL NOT NULL,
                    StructuralIncidentHistory TEXT NOT NULL,
                    FuelAmount REAL NOT NULL,
                    BaseMarketValue REAL NOT NULL,
                    AskingPrice REAL NOT NULL,
                    OriginalAskingPrice REAL NOT NULL,
                    PriceDropsCount INTEGER NOT NULL,
                    ListedAtUtc TEXT NOT NULL,
                    LastPriceDropAtUtc TEXT NULL,
                    IsSold INTEGER NOT NULL,
                    SoldAtUtc TEXT NULL,
                    IsPlayerOwned INTEGER NOT NULL,
                    PlayerAircraftId TEXT NULL,
                    PilotId TEXT NULL,
                    CategoryBadge TEXT NOT NULL,
                    FOREIGN KEY (AircraftTypeId) REFERENCES AircraftTypes(Id)
                );",
                "CREATE INDEX IF NOT EXISTS IX_MarketListings_IsSold ON MarketListings(IsSold);",
                "CREATE INDEX IF NOT EXISTS IX_MarketListings_IsPlayerOwned ON MarketListings(IsPlayerOwned);"
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

            // Higienização e extração inteligente de municípios via AviationDataNormalizer para aeroportos com City vazio
            try
            {
                using var selectCmd = connection.CreateCommand();
                selectCmd.CommandText = "SELECT Id, Name, ICAO, Country FROM Airports WHERE City IS NULL OR City = '' OR City IN ('SB','SS','SW','SJ','SD','SI','SN');";

                var pendingUpdates = new System.Collections.Generic.List<(string Id, string City, string Country)>();
                using (var reader = await selectCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetString(0);
                        var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        var icao = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        var country = reader.IsDBNull(3) ? "" : reader.GetString(3);

                        if (FlightCareer.Infrastructure.Services.AviationDataNormalizer.TryExtractCityFromName(name, out var extractedCity))
                        {
                            var resolvedCountry = FlightCareer.Infrastructure.Services.AviationDataNormalizer.ResolveCountryFromIcao(icao, country);
                            pendingUpdates.Add((id, extractedCity, resolvedCountry));
                        }
                    }
                }

                if (pendingUpdates.Count > 0)
                {
                    using var updateCmd = connection.CreateCommand();
                    using var trans = connection.BeginTransaction();
                    updateCmd.Transaction = trans;
                    updateCmd.CommandText = "UPDATE Airports SET City = @city, Country = @country WHERE Id = @id;";
                    var pCity = updateCmd.CreateParameter(); pCity.ParameterName = "@city"; updateCmd.Parameters.Add(pCity);
                    var pCountry = updateCmd.CreateParameter(); pCountry.ParameterName = "@country"; updateCmd.Parameters.Add(pCountry);
                    var pId = updateCmd.CreateParameter(); pId.ParameterName = "@id"; updateCmd.Parameters.Add(pId);

                    foreach (var (id, city, country) in pendingUpdates)
                    {
                        pCity.Value = city;
                        pCountry.Value = country;
                        pId.Value = id;
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                    await trans.CommitAsync();
                }
            }
            catch
            {
                // Ignorar em caso de primeira inicialização antes da criação da tabela Airports
            }
        }
    }
}
