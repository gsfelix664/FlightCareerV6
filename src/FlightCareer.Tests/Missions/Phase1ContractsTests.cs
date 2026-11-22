using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Career;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Domain.Xp;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class Phase1ContractsTests
    {
        [Fact]
        public void MissionCompletion_Panoramico_ComTodosPontosEPousoNaOrigem_Concluida()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "3.5.1",
                Title = "Voo Panorâmico",
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                OverflyWaypoints = "SBMT;SDCO"
            };

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SBMT", "SDCO" };
            var result = MissionCompletion.Evaluate(mission, "SBGR", visited, 0);

            Assert.True(result.IsCompleted);
            Assert.Null(result.Reason);
        }

        [Fact]
        public void MissionCompletion_Panoramico_FaltandoUmPonto_NaoConcluida()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "3.5.1",
                Title = "Voo Panorâmico",
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                OverflyWaypoints = "SBMT;SDCO"
            };

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SBMT" };
            var result = MissionCompletion.Evaluate(mission, "SBGR", visited, 0);

            Assert.False(result.IsCompleted);
            Assert.NotNull(result.Reason);
            Assert.Contains("SDCO", result.Reason);
        }

        [Fact]
        public void MissionCompletion_Panoramico_PousoEmOutroAeroporto_NaoConcluida()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "3.5.1",
                Title = "Voo Panorâmico",
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                OverflyWaypoints = "SBMT;SDCO"
            };

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SBMT", "SDCO" };
            var result = MissionCompletion.Evaluate(mission, "SBSP", visited, 0);

            Assert.False(result.IsCompleted);
            Assert.Contains("SBSP", result.Reason);
        }

        [Fact]
        public void MissionCompletion_CircuitoTgl_4Exigidos4FeitosEPousoNaOrigem_Concluida()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "3.5.2",
                Title = "Circuito de Tráfego",
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                RequiredTouchAndGoes = 4
            };

            var result = MissionCompletion.Evaluate(mission, "SBGR", null, 4);

            Assert.True(result.IsCompleted);
            Assert.Null(result.Reason);
        }

        [Fact]
        public void MissionCompletion_CircuitoTgl_4Exigidos3Feitos_NaoConcluida()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "3.5.2",
                Title = "Circuito de Tráfego",
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                RequiredTouchAndGoes = 4
            };

            var result = MissionCompletion.Evaluate(mission, "SBGR", null, 3);

            Assert.False(result.IsCompleted);
            Assert.Contains("3 de 4", result.Reason);
        }

        [Fact]
        public void TouchAndGoCounter_ToqueRolo40ktDecolagem_ContaComoTgl()
        {
            var counter = new TouchAndGoCounter();
            counter.Reset(initialIsOnGround: true);

            // 1. Decolagem inicial (não é TGL)
            counter.Update(isOnGround: true, groundSpeedKt: 0.0, simSeconds: 0.0);
            counter.Update(isOnGround: true, groundSpeedKt: 50.0, simSeconds: 15.0);
            counter.Update(isOnGround: false, groundSpeedKt: 65.0, simSeconds: 20.0);
            Assert.Equal(0, counter.Count);

            // 2. Voo
            counter.Update(isOnGround: false, groundSpeedKt: 90.0, simSeconds: 60.0);

            // 3. Toque para TGL: toca a 55 kt em t=100s, rola mantendo > 20 kt, decola a 60 kt em t=105s (duração 5s)
            counter.Update(isOnGround: true, groundSpeedKt: 55.0, simSeconds: 100.0);
            counter.Update(isOnGround: true, groundSpeedKt: 40.0, simSeconds: 102.0);
            counter.Update(isOnGround: true, groundSpeedKt: 50.0, simSeconds: 104.0);
            counter.Update(isOnGround: false, groundSpeedKt: 60.0, simSeconds: 105.0);

            Assert.Equal(1, counter.Count);
        }

        [Fact]
        public void TouchAndGoCounter_QuiqueVoltaAoArMenosDe1s_NaoConta()
        {
            var counter = new TouchAndGoCounter();
            counter.Reset(initialIsOnGround: false); // já no ar

            // Toque rápido (bounce): toca em t=50.0 e sai em t=50.5 (0.5 s)
            counter.Update(isOnGround: false, groundSpeedKt: 65.0, simSeconds: 49.0);
            counter.Update(isOnGround: true, groundSpeedKt: 55.0, simSeconds: 50.0);
            counter.Update(isOnGround: false, groundSpeedKt: 52.0, simSeconds: 50.5);

            Assert.Equal(0, counter.Count);
        }

        [Fact]
        public void TouchAndGoCounter_ParadaCompleta_NaoContaComoTgl()
        {
            var counter = new TouchAndGoCounter();
            counter.Reset(initialIsOnGround: false); // no ar

            // Toque seguido de parada completa (GS cai para 2 kt)
            counter.Update(isOnGround: true, groundSpeedKt: 55.0, simSeconds: 100.0);
            counter.Update(isOnGround: true, groundSpeedKt: 2.0, simSeconds: 110.0);
            counter.Update(isOnGround: true, groundSpeedKt: 50.0, simSeconds: 130.0);
            counter.Update(isOnGround: false, groundSpeedKt: 60.0, simSeconds: 135.0);

            // Pouso completo com nova decolagem não é TGL
            Assert.Equal(0, counter.Count);
        }

        [Fact]
        public void OverflyTracker_DistanciaEAltitude_MarcacaoCorreta()
        {
            var tracker = new OverflyTracker();
            // Ponto de teste: Elevação 2.000 ft, localizado em Lat 0.0, Lon 0.0
            var wp = new OverflyPoint("TEST1", 0.0, 0.0, 2000.0);
            tracker.ConfigureWaypoints(new[] { wp });

            // 1. Passar a 1.5 NM e 2.000 ft acima da elevação (altitude 4.000 ft, max permitido 5.000 ft) -> visitado
            double lat15Nm = 1.5 / 60.0; // aprox 1.5 NM ao norte
            tracker.Update(lat15Nm, 0.0, 4000.0);
            Assert.True(tracker.IsPointVisited("TEST1"));

            // 2. Novo ponto para testar distância excessiva (3 NM)
            var tracker2 = new OverflyTracker();
            var wp2 = new OverflyPoint("TEST2", 0.0, 0.0, 2000.0);
            tracker2.ConfigureWaypoints(new[] { wp2 });
            double lat3Nm = 3.0 / 60.0;
            tracker2.Update(lat3Nm, 0.0, 3000.0);
            Assert.False(tracker2.IsPointVisited("TEST2"));

            // 3. Novo ponto para testar altitude excessiva (5.000 ft acima da elevação = 7.000 ft, teto 5.000 ft)
            var tracker3 = new OverflyTracker();
            var wp3 = new OverflyPoint("TEST3", 0.0, 0.0, 2000.0);
            tracker3.ConfigureWaypoints(new[] { wp3 });
            double lat1Nm = 1.0 / 60.0;
            tracker3.Update(lat1Nm, 0.0, 7500.0); // 5.500 ft acima da elevação
            Assert.False(tracker3.IsPointVisited("TEST3"));
        }

        [Fact]
        public void Generator_ContratosFase1_Aeroclube_GeradosCorretamente()
        {
            var origin = new Airport
            {
                ICAO = "SBDN",
                Name = "Aeroporto Presidente Prudente",
                City = "Presidente Prudente",
                Country = "BR",
                Tier = 2,
                Latitude = -22.175,
                Longitude = -51.424,
                ElevationFt = 1476
            };

            var neighborAirports = new List<Airport>
            {
                origin,
                new() { ICAO = "SDRS", Name = "Aeródromo Dracena", City = "Dracena", Country = "BR", Tier = 3, Latitude = -21.468, Longitude = -51.530, ElevationFt = 1350 },
                new() { ICAO = "SDAN", Name = "Aeródromo Álvares Machado", City = "Álvares Machado", Country = "BR", Tier = 4, Latitude = -22.090, Longitude = -51.480, ElevationFt = 1450 },
                new() { ICAO = "SNOK", Name = "Aeródromo Regente Feijó", City = "Regente Feijó", Country = "BR", Tier = 4, Latitude = -22.220, Longitude = -51.300, ElevationFt = 1460 },
                new() { ICAO = "SBLS", Name = "Aeroporto Lins", City = "Lins", Country = "BR", Tier = 3, Latitude = -21.660, Longitude = -49.730, ElevationFt = 1550 }
            };

            var cat = MissionRules.Rules[2].Categories.FirstOrDefault(c => c.Id == "3.5");
            Assert.NotNull(cat);
            Assert.Equal("Aeroclube", cat.Name);

            var subPanoramico = cat.SubCategories.First(s => s.Id == "3.5.1");
            var subTgl = cat.SubCategories.First(s => s.Id == "3.5.2");
            var subTranslado = cat.SubCategories.First(s => s.Id == "3.5.3");

            var rng = new Random(42);
            var now = DateTime.UtcNow;

            // 1. Gerar Panorâmico
            var missionsPano = MissionGenerator.GenerateMissionsInMemory(
                origin, neighborAirports, null, rng, now);
            // Verifica resolução das fases no catálogo
            Assert.Equal(CareerPhase.Phase1_FlightSchool, CareerPhaseCatalog.Instance.ResolvePhase("3.5.1"));
            Assert.Equal(CareerPhase.Phase1_FlightSchool, CareerPhaseCatalog.Instance.ResolvePhase("3.5.2"));
            Assert.Equal(CareerPhase.Phase1_FlightSchool, CareerPhaseCatalog.Instance.ResolvePhase("3.5.3"));

            // 2. Multiplicadores de XP
            Assert.Equal(1.15, XpRules.Default.TypeMultipliers["panoramico"]);
            Assert.Equal(0.80, XpRules.Default.TypeMultipliers["translado"]);
            Assert.Equal(1.10, XpRules.Default.TypeMultipliers["instrucao_basica"]);

            // 3. Famílias de pagamento
            Assert.Equal("panoramico", MissionPayTable.Shared.Resolve("3.5.1")?.Id);
            Assert.Equal("instrucao_basica", MissionPayTable.Shared.Resolve("3.5.2")?.Id);
            Assert.Equal("translado", MissionPayTable.Shared.Resolve("3.5.3")?.Id);
        }

        [Fact]
        public async Task Persistence_LegacyDb_GanhaNovasColunasIdempotente()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"fc_legacy_fases2a_{Guid.NewGuid():N}.db");
            try
            {
                var connStr = $"Data Source={dbPath}";

                // 1. Cria banco legado sem RequiredTouchAndGoes e sem OverflyWaypoints
                await using (var conn = new SqliteConnection(connStr))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE Missions (
                            Id TEXT PRIMARY KEY,
                            Title TEXT,
                            Description TEXT,
                            OriginICAO TEXT,
                            DestinationICAO TEXT,
                            PayloadLbs REAL,
                            PassengerCount INTEGER,
                            LuggageWeightLbs REAL,
                            CargoWeightLbs REAL,
                            Urgency REAL,
                            RewardMoney REAL,
                            ExperienceGain INTEGER,
                            DistanceNM REAL,
                            ExpirationTime TEXT,
                            IsAccepted INTEGER,
                            PilotId TEXT,
                            MissionCategoryType TEXT
                        );
                    ";
                    await cmd.ExecuteNonQueryAsync();
                }

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connStr)
                    .Options;

                // 2. Executa migração idempotente duas vezes
                await using (var db = new AppDbContext(options))
                {
                    await db.EnsureSchemaUpdatedAsync();
                    await db.EnsureSchemaUpdatedAsync();

                    var mission = new Mission
                    {
                        Id = Guid.NewGuid(),
                        Title = "Teste Panoramico Legado",
                        OriginICAO = "SBGR",
                        DestinationICAO = "SBGR",
                        RequiredTouchAndGoes = 5,
                        OverflyWaypoints = "SBMT;SDCO"
                    };

                    db.Missions.Add(mission);
                    await db.SaveChangesAsync();
                }

                // 3. Lê de volta e confere
                await using (var db = new AppDbContext(options))
                {
                    var saved = await db.Missions.FirstAsync();
                    Assert.Equal(5, saved.RequiredTouchAndGoes);
                    Assert.Equal("SBMT;SDCO", saved.OverflyWaypoints);
                }
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    try { File.Delete(dbPath); } catch { }
                }
            }
        }
    }
}
