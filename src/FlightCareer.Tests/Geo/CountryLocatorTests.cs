using System;
using System.Threading.Tasks;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Geo;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Geo
{
    public class CountryLocatorTests
    {
        [Fact]
        public void CountryLocator_PontosConhecidos()
        {
            var locator = CountryLocator.Shared;

            // São Paulo (-23,55; -46,63) → BR
            Assert.Equal("BR", locator.Locate(-23.55, -46.63));

            // Ufa (54,56; 55,87) → RU
            Assert.Equal("RU", locator.Locate(54.56, 55.87));

            // Bethel (60,78; -161,84) → US
            Assert.Equal("US", locator.Locate(60.78, -161.84));

            // Lisboa (38,77; -9,13) → PT
            Assert.Equal("PT", locator.Locate(38.77, -9.13));

            // Meio do Atlântico (0; -30) → null
            Assert.Null(locator.Locate(0.0, -30.0));
        }

        [Fact]
        public void CountryLocator_Buraco()
        {
            var locator = CountryLocator.Shared;

            // Maseru (-29,31; 27,48) → LS (Lesoto fica dentro da África do Sul; não pode dar ZA)
            string? result = locator.Locate(-29.31, 27.48);
            Assert.Equal("LS", result);
        }

        [Fact]
        public void CountryLocator_FrancaSemIsoA2()
        {
            var locator = CountryLocator.Shared;

            // Paris (48,85; 2,35) → FR
            Assert.Equal("FR", locator.Locate(48.85, 2.35));

            // Oslo (59,91; 10,75) → NO
            Assert.Equal("NO", locator.Locate(59.91, 10.75));
        }

        [Fact]
        public async Task DataSeeder_PaisInvalido_ResolvidoPorContornoOuVizinho()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
            }

            Guid idOhio = Guid.NewGuid();
            Guid idBrValido = Guid.NewGuid();
            Guid idAgua = Guid.NewGuid();
            Guid idOceano = Guid.NewGuid();
            Guid idCaiena = Guid.NewGuid();

            // 1. Aeroporto com Country = "K4" em Ohio -> US (resolvido por contorno)
            // 2. Aeroporto válido no Brasil (SBRJ: -22.9104, -43.1631, BR)
            // 3. Aeroporto na água a ~10 NM de SBRJ (-23.0804, -43.1631: dist 10.2 NM, mar fora do contorno) -> BR (resolvido por vizinho)
            // 4. Aeroporto isolado no oceano (0.0, -30.0) -> vazio

            await using (var db = new AppDbContext(options))
            {
                db.Airports.AddRange(
                    new Airport
                    {
                        Id = idOhio,
                        ICAO = "KOSU",
                        Name = "Ohio State University Airport",
                        Country = "K4",
                        Latitude = 40.0798,
                        Longitude = -83.0731,
                        ElevationFt = 905,
                        Tier = 3,
                        IsInSimulator = true
                    },
                    new Airport
                    {
                        Id = idBrValido,
                        ICAO = "SBRJ",
                        Name = "Santos Dumont",
                        Country = "BR",
                        CsvType = "medium_airport",
                        Latitude = -22.9104,
                        Longitude = -43.1631,
                        ElevationFt = 9,
                        Tier = 1,
                        IsInSimulator = true
                    },
                    new Airport
                    {
                        Id = idAgua,
                        ICAO = "AGUA1",
                        Name = "Plataforma Marítima RJ",
                        Country = "32",
                        Latitude = -23.0804,
                        Longitude = -43.1631,
                        ElevationFt = 0,
                        Tier = 4,
                        IsInSimulator = true
                    },
                    new Airport
                    {
                        Id = idOceano,
                        ICAO = "OCEAN",
                        Name = "Plataforma Isolada no Atlântico",
                        Country = "2S",
                        Latitude = 0.0,
                        Longitude = -30.0,
                        ElevationFt = 0,
                        Tier = 4,
                        IsInSimulator = true
                    },
                    new Airport
                    {
                        Id = idCaiena,
                        ICAO = "SOCA",
                        Name = "Caiena (país do OurAirports: GF)",
                        Country = "GF",
                        CsvType = "medium_airport",
                        Latitude = 4.8198,
                        Longitude = -52.3604,
                        Tier = 2,
                        IsInSimulator = true
                    }
                );
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var seeder = new DataSeeder(db);
                var (byBorder, byNeighbor, empty) = await seeder.ResolveMissingCountriesAsync();

                Assert.Equal(1, byBorder);
                Assert.Equal(1, byNeighbor);
                Assert.Equal(1, empty);
            }

            await using (var db = new AppDbContext(options))
            {
                var ohio = await db.Airports.FindAsync(idOhio);
                Assert.NotNull(ohio);
                Assert.Equal("US", ohio!.Country);

                var agua = await db.Airports.FindAsync(idAgua);
                Assert.NotNull(agua);
                Assert.Equal("BR", agua!.Country);

                var oceano = await db.Airports.FindAsync(idOceano);
                Assert.NotNull(oceano);
                Assert.Equal("", oceano!.Country);

                // país do csv com a posição batendo não é trocado pelo contorno (Natural Earth junta GF com FR)
                var caiena = await db.Airports.FindAsync(idCaiena);
                Assert.Equal("GF", caiena!.Country);
            }
        }
    }
}
