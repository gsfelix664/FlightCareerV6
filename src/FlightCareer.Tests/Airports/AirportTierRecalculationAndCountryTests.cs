using System;
using System.IO;
using System.Threading.Tasks;
using FlightCareer.Domain.Airports;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Airports
{
    public class AirportTierRecalculationAndCountryTests
    {
        [Fact]
        public async Task Pais_CsvMesmaPosicao_SubstituiPais_EPosicaoDivergenteNaoSubstitui()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
            }

            // Criar CSV temporário com 2 aeroportos:
            // 1. ZSPD: Xangai Pudong (região ZS, csv CN, lat 31.14, lon 121.80) -> mesma posição
            // 2. SBOK: Reaproveitado no MSFS em Amontada/CE (-3.36, -39.83), mas no CSV em MT (-10.5, -55.0) -> > 10 NM
            string tempDir = Path.Combine(Path.GetTempPath(), "FC_Test_Country_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string csvFile = Path.Combine(tempDir, "airports.csv");
                await File.WriteAllTextAsync(csvFile,
@"ident,type,name,elevation_ft,continent,iso_country,iso_region,municipality,scheduled_service,gps_code,iata_code,local_code,home_link,wikipedia_link,keywords,latitude_deg,longitude_deg
ZSPD,large_airport,Shanghai Pudong International Airport,13,AS,CN,CN-31,Shanghai,yes,ZSPD,PVG,ZSPD,,,PVG,31.1434,121.8052
SBOK,small_airport,Fazenda Antiga MT,800,SA,BR,BR-MT,Querência,no,SBOK,,SBOK,,,, -10.5000,-55.0000
");

                // Inserir os 2 aeroportos no banco simulando o Little Navmap inicial (antes do enriquecimento)
                await using (var db = new AppDbContext(options))
                {
                    db.Airports.AddRange(
                        new Airport
                        {
                            Id = Guid.NewGuid(),
                            ICAO = "ZSPD",
                            Name = "Shanghai Pudong MSFS",
                            Country = "ZS", // Inicialmente veio com a região ICAO
                            City = "",
                            Latitude = 31.1440,
                            Longitude = 121.8060,
                            ElevationFt = 13,
                            Tier = 2,
                            IsInSimulator = true
                        },
                        new Airport
                        {
                            Id = Guid.NewGuid(),
                            ICAO = "SBOK",
                            Name = "Amontada MSFS",
                            Country = "BR",
                            City = "Amontada",
                            Latitude = -3.3600, // Amontada/CE
                            Longitude = -39.8300,
                            ElevationFt = 50,
                            Tier = 3,
                            IsInSimulator = true
                        }
                    );
                    await db.SaveChangesAsync();
                }

                // Chamar o enriquecimento apontando para a pasta temporária
                await using (var db = new AppDbContext(options))
                {
                    // Usar reflection para definir _csvPath ou simular via conexão SQLite idêntica ao DataSeeder
                    var seeder = new DataSeeder(db);
                    var csvPathField = typeof(DataSeeder).GetField("_csvPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    csvPathField?.SetValue(seeder, tempDir);

                    var enrichMethod = typeof(DataSeeder).GetMethod("EnrichFromCsvAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(enrichMethod);

                    var task = (Task)enrichMethod!.Invoke(seeder, new object?[] { null })!;
                    await task;
                }

                // Verificar os resultados
                await using (var db = new AppDbContext(options))
                {
                    var zspd = await db.Airports.FirstOrDefaultAsync(a => a.ICAO == "ZSPD");
                    Assert.NotNull(zspd);
                    // Como a posição bateu (< 10 NM), Country DEVE virar "CN" (iso_country do CSV)
                    Assert.Equal("CN", zspd!.Country);
                    Assert.Equal("large_airport", zspd.CsvType);
                    Assert.True(zspd.ScheduledService);

                    var sbok = await db.Airports.FirstOrDefaultAsync(a => a.ICAO == "SBOK");
                    Assert.NotNull(sbok);
                    // Posição a > 10 NM: não deve ser contaminado com município/elevação do CSV antigo
                    Assert.Equal("Amontada", sbok!.City);
                    Assert.Equal(50, sbok.ElevationFt);
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public async Task RecalculoTier_VersaoRegrasMudou_RecalculaTodos()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
            }

            var currentRules = AirportTierRules.Default;
            int oldVersion = currentRules.Version - 1; // Versão 1

            // Inserir aeroportos com TierRulesVersion = 1:
            // 1. Aeroporto A: Sinais de Tier 1 (12.000 ft pavimentada, 4 ILS, 30 aproximações, 50 gates, torre),
            //    mas com Tier gravado como 2 e versão 1
            // 2. Aeroporto B: Já na versão atual (versão 2)
            Guid idA = Guid.NewGuid();
            Guid idB = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                db.Airports.AddRange(
                    new Airport
                    {
                        Id = idA,
                        ICAO = "KJFK",
                        Name = "John F Kennedy",
                        Country = "US",
                        Latitude = 40.6398,
                        Longitude = -73.7789,
                        ElevationFt = 13,
                        Tier = 2, // Desatualizado: sinais dão Tier 1
                        TierScore = 40,
                        IlsEnds = 4,
                        Approaches = 25,
                        Gates = 50,
                        CargoParking = 10,
                        HasTower = true,
                        LongestRunwayFt = 14511,
                        LongestRunwayPaved = true,
                        CsvType = "large_airport",
                        ScheduledService = true,
                        IsMilitary = false,
                        TierRulesVersion = oldVersion // Versão antiga
                    },
                    new Airport
                    {
                        Id = idB,
                        ICAO = "SDAO",
                        Name = "Fazenda Pouso Redondo",
                        Country = "BR",
                        Latitude = -12.0,
                        Longitude = -54.0,
                        ElevationFt = 1000,
                        Tier = 4,
                        TierScore = 0,
                        LongestRunwayFt = 2000,
                        LongestRunwayPaved = false,
                        TierRulesVersion = currentRules.Version // Já atualizado
                    }
                );
                await db.SaveChangesAsync();
            }

            // Executar recálculo via EnsureTiersUpToDateAsync
            await using (var db = new AppDbContext(options))
            {
                var seeder = new DataSeeder(db);
                int changed = await seeder.EnsureTiersUpToDateAsync();

                Assert.True(changed >= 1, $"Esperava que pelo menos 1 aeroporto mudasse de tier, mudaram {changed}.");
            }

            // Conferir que o Aeroporto A virou Tier 1 e TierRulesVersion = 2
            await using (var db = new AppDbContext(options))
            {
                var jfk = await db.Airports.FirstOrDefaultAsync(a => a.ICAO == "KJFK");
                Assert.NotNull(jfk);
                Assert.Equal(1, jfk!.Tier);
                Assert.True(jfk.TierScore >= 120);
                Assert.Equal(currentRules.Version, jfk.TierRulesVersion);

                var sdao = await db.Airports.FirstOrDefaultAsync(a => a.ICAO == "SDAO");
                Assert.NotNull(sdao);
                Assert.Equal(4, sdao!.Tier);
                Assert.Equal(currentRules.Version, sdao.TierRulesVersion);
            }
        }
    }
}
