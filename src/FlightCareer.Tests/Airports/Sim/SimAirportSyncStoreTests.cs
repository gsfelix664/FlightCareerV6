using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightCareer.Domain.Airports.Sim;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Airports.Sim
{
    public class SimAirportSyncStoreTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly TestDbContextFactory _factory;

        public SimAirportSyncStoreTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
            _factory = new TestDbContextFactory(_options);

            using var db = new AppDbContext(_options);
            db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;
            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
            public AppDbContext CreateDbContext() => new(_options);
            public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(new AppDbContext(_options));
        }

        [Fact]
        public async Task ApplyList_PrimeiraVez_EnfileiraTodos()
        {
            var store = new SimAirportSyncStore(_factory);
            var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

            var items = new List<SimAirportListItem>
            {
                new("SBSP", "SB", -23.6261, -46.6564, 802.0),
                new("SBGR", "SB", -23.4356, -46.4731, 750.0)
            };

            var result = await store.ApplyListAsync(items, "1.37.19.0", now);

            Assert.False(result.NoChange);
            Assert.Equal(2, result.Enqueued);
            Assert.Equal(0, result.MarkedMissing);
            Assert.False(result.MissingSkippedSuspicious);
            Assert.False(result.VersionChanged);

            var state = await store.GetStateAsync();
            Assert.Equal("1.37.19.0", state.SimVersion);
            Assert.Equal(2, state.ListCount);
            Assert.Equal(now, state.LastListUtc);

            Assert.Equal(2, await store.PendingCountAsync());
        }

        [Fact]
        public async Task ApplyList_MesmoDigestEVersao_NoChange()
        {
            var store = new SimAirportSyncStore(_factory);
            var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

            var items = new List<SimAirportListItem>
            {
                new("SBSP", "SB", -23.6261, -46.6564, 802.0)
            };

            await store.ApplyListAsync(items, "1.37.19.0", now);

            var later = now.AddMinutes(10);
            var result = await store.ApplyListAsync(items, "1.37.19.0", later);

            Assert.True(result.NoChange);
            Assert.Equal(0, result.Enqueued);

            var state = await store.GetStateAsync();
            Assert.Equal(later, state.LastListUtc);
        }

        [Fact]
        public async Task ApplyList_Ausente_MarcaForaSemApagar()
        {
            await using (var db = new AppDbContext(_options))
            {
                var sbsp = new Airport
                {
                    Id = Guid.NewGuid(),
                    ICAO = "SBSP",
                    Name = "Congonhas",
                    IsInSimulator = true
                };
                db.Airports.Add(sbsp);
                db.Runways.Add(new Runway
                {
                    Id = Guid.NewGuid(),
                    AirportIdent = "SBSP",
                    LengthFt = 6000,
                    WidthFt = 150,
                    Surface = "Asphalt"
                });
                await db.SaveChangesAsync();
            }

            var store = new SimAirportSyncStore(_factory);
            var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

            // Lista vem vazia (ou sem SBSP), mas para não ser suspeita (list.Count >= 0.5 * inSimCount)
            // Se temos 1 aeroporto no banco, 0.5 * 1 = 0.5. Se a lista tiver 1 aeroporto novo (ex: SBGR), list.Count = 1 >= 0.5
            var items = new List<SimAirportListItem>
            {
                new("SBGR", "SB", -23.4356, -46.4731, 750.0)
            };

            var result = await store.ApplyListAsync(items, "1.37.19.0", now);

            Assert.Equal(1, result.MarkedMissing);
            Assert.False(result.MissingSkippedSuspicious);

            await using (var db = new AppDbContext(_options))
            {
                var sbsp = await db.Airports.FirstOrDefaultAsync(a => a.ICAO == "SBSP");
                Assert.NotNull(sbsp);
                Assert.False(sbsp!.IsInSimulator);

                // Pistas continuam no banco
                var runways = await db.Runways.Where(r => r.AirportIdent == "SBSP").ToListAsync();
                Assert.Single(runways);
            }
        }

        [Fact]
        public async Task ApplyList_ListaSuspeita_NaoMarcaAusentes()
        {
            await using (var db = new AppDbContext(_options))
            {
                // Inserir 10 aeroportos in simulator
                for (int i = 0; i < 10; i++)
                {
                    db.Airports.Add(new Airport
                    {
                        Id = Guid.NewGuid(),
                        ICAO = $"SB0{i}",
                        IsInSimulator = true
                    });
                }
                await db.SaveChangesAsync();
            }

            var store = new SimAirportSyncStore(_factory);
            var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

            // Lista traz apenas 2 aeroportos (< 0.5 * 10 = 5)
            var items = new List<SimAirportListItem>
            {
                new("SB01", "SB", -23.0, -46.0, 700.0),
                new("SB02", "SB", -23.1, -46.1, 700.0)
            };

            var result = await store.ApplyListAsync(items, "1.37.19.0", now);

            Assert.True(result.MissingSkippedSuspicious);
            Assert.Equal(0, result.MarkedMissing);

            // Nenhum foi desmarcado
            await using (var db = new AppDbContext(_options))
            {
                int inSim = await db.Airports.CountAsync(a => a.IsInSimulator);
                Assert.Equal(10, inSim);
            }
        }

        [Fact]
        public async Task ApplyList_VersaoMudou_EnfileiraIguaisComPrioridade0()
        {
            var store = new SimAirportSyncStore(_factory);
            var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

            var items = new List<SimAirportListItem>
            {
                new("SBSP", "SB", -23.6261, -46.6564, 802.0)
            };

            // Primeira aplicação com versão 1.0
            await store.ApplyListAsync(items, "1.0", now);

            // Processar detalhe para o banco ter SBSP com fingerprint igual
            var detail = new SimAirportDetail(
                "SBSP", "SB", "Congonhas", -23.6261, -46.6564, 802.0, 4, 10, -23.6, -46.6,
                new List<SimRunwayData> { new(1940, 45, 4, "", "") });

            var batch = await store.TakeBatchAsync(10);
            await store.ApplyDetailsAsync(new[] { (batch[0], (SimAirportDetail?)detail) }, now);
            Assert.Equal(0, await store.PendingCountAsync());

            // Agora aplica com versão 2.0 (versão mudou!)
            var later = now.AddDays(1);
            var result = await store.ApplyListAsync(items, "2.0", later);

            Assert.True(result.VersionChanged);
            Assert.Equal(1, result.Enqueued);

            var queued = await store.TakeBatchAsync(10);
            Assert.Single(queued);
            Assert.Equal("SBSP", queued[0].Ident);
            Assert.Equal(0, queued[0].Priority); // Reconferência com prioridade 0
        }

        [Fact]
        public async Task TakeBatch_OrdenaPorPrioridadeEDistancia()
        {
            var store = new SimAirportSyncStore(_factory);
            var now = DateTime.UtcNow;

            await using (var db = new AppDbContext(_options))
            {
                db.SimAirportQueue.AddRange(
                    // P0, perto de (-23.0, -46.0)
                    new SimAirportQueueItem { Ident = "SB01", Latitude = -23.1, Longitude = -46.1, Priority = 0, EnqueuedUtc = now },
                    // P1, longe
                    new SimAirportQueueItem { Ident = "SB02", Latitude = -10.0, Longitude = -50.0, Priority = 1, EnqueuedUtc = now },
                    // P1, perto
                    new SimAirportQueueItem { Ident = "SB03", Latitude = -23.05, Longitude = -46.05, Priority = 1, EnqueuedUtc = now },
                    // P2, longe
                    new SimAirportQueueItem { Ident = "SB04", Latitude = 0.0, Longitude = 0.0, Priority = 2, EnqueuedUtc = now }
                );
                await db.SaveChangesAsync();
            }

            var batch = await store.TakeBatchAsync(4, nearLat: -23.0, nearLon: -46.0);

            Assert.Equal(4, batch.Count);
            // Prioridade mais alta primeiro (P2: SB04)
            Assert.Equal("SB04", batch[0].Ident);
            // Entre os P1, o mais próximo primeiro: SB03 antes de SB02
            Assert.Equal("SB03", batch[1].Ident);
            Assert.Equal("SB02", batch[2].Ident);
            // P0 por último
            Assert.Equal("SB01", batch[3].Ident);
        }

        [Fact]
        public async Task ApplyDetails_InsereAtualizaEExclui()
        {
            var store = new SimAirportSyncStore(_factory);
            var now = DateTime.UtcNow;

            await using (var db = new AppDbContext(_options))
            {
                // SBSP já existe
                db.Airports.Add(new Airport
                {
                    Id = Guid.NewGuid(),
                    ICAO = "SBSP",
                    Name = "Congonhas",
                    Source = "Csv",
                    IsInSimulator = true
                });
                db.Runways.Add(new Runway
                {
                    Id = Guid.NewGuid(),
                    AirportIdent = "SBSP",
                    LengthFt = 4000,
                    WidthFt = 100,
                    Surface = "Gravel"
                });

                // SBEX existe mas novo detalhe terá pista curta -> deve ser excluído
                db.Airports.Add(new Airport
                {
                    Id = Guid.NewGuid(),
                    ICAO = "SBEX",
                    Name = "Excluido",
                    IsInSimulator = true
                });

                // Fila
                db.SimAirportQueue.AddRange(
                    new SimAirportQueueItem { Ident = "SBSP", Fingerprint = "fp-sbsp", Priority = 1, EnqueuedUtc = now },
                    new SimAirportQueueItem { Ident = "SBGR", Fingerprint = "fp-sbgr", Priority = 1, EnqueuedUtc = now },
                    new SimAirportQueueItem { Ident = "SBEX", Fingerprint = "fp-sbex", Priority = 1, EnqueuedUtc = now }
                );
                await db.SaveChangesAsync();
            }

            var queueItems = await store.TakeBatchAsync(10);
            var qSbsp = queueItems.First(q => q.Ident == "SBSP");
            var qSbgr = queueItems.First(q => q.Ident == "SBGR");
            var qSbex = queueItems.First(q => q.Ident == "SBEX");

            var dSbsp = new SimAirportDetail(
                "SBSP", "SB", "Congonhas Novo", -23.6261, -46.6564, 802.0, 5, 12, -23.6, -46.6,
                new List<SimRunwayData> { new(1940, 45, 4, "", "") }); // 1940m = 6365ft

            var dSbgr = new SimAirportDetail(
                "SBGR", "SB", "Guarulhos", -23.4356, -46.4731, 750.0, 10, 20, -23.4, -46.4,
                new List<SimRunwayData> { new(3700, 45, 0, "", "") });

            // SBEX com pista < 1000 ft (200m = 656 ft) -> Builder retorna null
            var dSbex = new SimAirportDetail(
                "SBEX", "SB", "Excluido", -20.0, -40.0, 100.0, 0, 0, 0, 0,
                new List<SimRunwayData> { new(200, 20, 4, "", "") });

            var batch = new List<(SimAirportQueueItem Item, SimAirportDetail? Detail)>
            {
                (qSbsp, dSbsp),
                (qSbgr, dSbgr),
                (qSbex, dSbex)
            };

            var result = await store.ApplyDetailsAsync(batch, now);

            Assert.Equal(1, result.Inserted); // SBGR
            Assert.Equal(1, result.Updated);  // SBSP
            Assert.Equal(1, result.Excluded); // SBEX
            Assert.Equal(0, result.Failed);
            Assert.Equal(0, result.Dropped);

            await using (var db = new AppDbContext(_options))
            {
                var sbsp = await db.Airports.FirstAsync(a => a.ICAO == "SBSP");
                Assert.Equal("Congonhas Novo", sbsp.Name);
                Assert.Equal("Sim", sbsp.Source);
                Assert.True(sbsp.IsInSimulator);

                // Pistas antigas substituídas
                var sbspRunways = await db.Runways.Where(r => r.AirportIdent == "SBSP").ToListAsync();
                Assert.Single(sbspRunways);
                Assert.Equal("Asphalt", sbspRunways[0].Surface);
                Assert.Equal(6365, sbspRunways[0].LengthFt);

                var sbgr = await db.Airports.FirstAsync(a => a.ICAO == "SBGR");
                Assert.Equal("Sim", sbgr.Source);
                Assert.True(sbgr.IsInSimulator);

                var sbex = await db.Airports.FirstAsync(a => a.ICAO == "SBEX");
                Assert.False(sbex.IsInSimulator); // Marcado fora do simulador

                // Todos foram removidos da fila
                Assert.Equal(0, await db.SimAirportQueue.CountAsync());
            }
        }

        [Fact]
        public async Task ApplyDetails_FalhaTresVezes_Descarta()
        {
            var store = new SimAirportSyncStore(_factory);
            var now = DateTime.UtcNow;

            await using (var db = new AppDbContext(_options))
            {
                db.SimAirportQueue.Add(new SimAirportQueueItem
                {
                    Ident = "SBER",
                    Attempts = 2,
                    Priority = 1,
                    EnqueuedUtc = now
                });
                await db.SaveChangesAsync();
            }

            var batch = await store.TakeBatchAsync(10);
            Assert.Single(batch);

            // Submete com Detail = null (falha)
            var result = await store.ApplyDetailsAsync(new[] { (batch[0], (SimAirportDetail?)null) }, now);

            Assert.Equal(1, result.Failed);
            Assert.Equal(1, result.Dropped);

            Assert.Equal(0, await store.PendingCountAsync());
        }

        [Fact]
        public async Task EnqueueRotationAsync_PegaMaisAntigosPrimeiro()
        {
            var store = new SimAirportSyncStore(_factory);
            var now = DateTime.UtcNow;

            await using (var db = new AppDbContext(_options))
            {
                db.Airports.AddRange(
                    new Airport { Id = Guid.NewGuid(), ICAO = "SB01", IsInSimulator = true, SimCheckedUtc = now.AddDays(-2), Latitude = -23.0, Longitude = -46.0, ElevationFt = 1000 },
                    new Airport { Id = Guid.NewGuid(), ICAO = "SB02", IsInSimulator = true, SimCheckedUtc = null, Latitude = -23.0, Longitude = -46.0, ElevationFt = 1000 },
                    new Airport { Id = Guid.NewGuid(), ICAO = "SB03", IsInSimulator = true, SimCheckedUtc = now.AddDays(-10), Latitude = -23.0, Longitude = -46.0, ElevationFt = 1000 },
                    new Airport { Id = Guid.NewGuid(), ICAO = "SB04", IsInSimulator = false, Latitude = -23.0, Longitude = -46.0 } // Não está in sim
                );
                await db.SaveChangesAsync();
            }

            int enqueued = await store.EnqueueRotationAsync(2, now);
            Assert.Equal(2, enqueued);

            var batch = await store.TakeBatchAsync(10);
            Assert.Equal(2, batch.Count);
            // Primeiro os com SimCheckedUtc nulo (SB02), depois o mais antigo (-10 dias: SB03)
            Assert.Equal("SB02", batch[0].Ident);
            Assert.Equal("SB03", batch[1].Ident);
        }
    }
}
