using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Fuel;
using FlightCareer.Infrastructure.Replay;
using Xunit;

namespace FlightCareer.Tests.Fuel
{
    public class FuelDomainTests
    {
        [Fact]
        public void Accumulator_SomaIncrementos_DoisMotores()
        {
            var acc = new FuelUsageAccumulator();

            // Tick 1: partida dos motores
            acc.Update(new[] { 0.0, 0.0 });
            Assert.Equal(0.0, acc.TotalLbs);

            // Tick 2: motor 1 consumiu 5 lb, motor 2 consumiu 3 lb
            acc.Update(new[] { 5.0, 3.0 });
            Assert.Equal(8.0, acc.TotalLbs);

            // Tick 3: motor 1 consumiu mais 2 lb (total 7), motor 2 mais 1 lb (total 4)
            acc.Update(new[] { 7.0, 4.0 });
            Assert.Equal(11.0, acc.TotalLbs);

            // Conversão para galões (ex: AVGAS 6.0 lb/gal)
            double gallons = acc.TotalGallons(6.0);
            Assert.Equal(11.0 / 6.0, gallons, 4);
        }

        [Fact]
        public void Accumulator_ReinicioDoMotor_ContinuaSomando()
        {
            var acc = new FuelUsageAccumulator();

            // Sequência exata da especificação: 10 → 12 → 0,5 → 1 = 13 lb
            acc.Update(new[] { 10.0 });
            Assert.Equal(10.0, acc.TotalLbs);

            acc.Update(new[] { 12.0 });
            Assert.Equal(12.0, acc.TotalLbs);

            // Motor reiniciou em voo ou solo: contador caiu de 12 para 0.5 lb
            acc.Update(new[] { 0.5 });
            Assert.Equal(12.5, acc.TotalLbs);

            acc.Update(new[] { 1.0 });
            Assert.Equal(13.0, acc.TotalLbs);

            var diags = acc.DrainDiagnostics();
            Assert.Contains(diags, d => d.Contains("Reinício detectado no motor 1"));
        }

        [Fact]
        public void Accumulator_IgnoraNaN()
        {
            var acc = new FuelUsageAccumulator();

            acc.Update(new[] { 5.0 });
            Assert.Equal(5.0, acc.TotalLbs);

            // Envia NaN, Infinity e negativo
            acc.Update(new[] { double.NaN });
            acc.Update(new[] { double.PositiveInfinity });
            acc.Update(new[] { -2.0 });

            Assert.Equal(5.0, acc.TotalLbs);

            // Envia incremento válido subsequente
            acc.Update(new[] { 7.0 });
            Assert.Equal(7.0, acc.TotalLbs);

            var diags = acc.DrainDiagnostics();
            Assert.True(diags.Count >= 3, "Deveria registrar diagnósticos para os valores descartados.");
        }

        [Fact]
        public void Integrity_UnlimitedFuel_Detecta()
        {
            var monitor = new FuelIntegrityMonitor(totalCapacityGallons: 26.0);

            var events = monitor.Update(simFuelTotalGal: 20.0, unlimitedFuel: true, simOnGround: true, simulatedTimeSeconds: 10.0);

            Assert.Single(events);
            Assert.Equal(FuelIntegrityEventType.UnlimitedFuelActive, events[0].Type);
            Assert.False(events[0].RequiresSync);
        }

        [Fact]
        public void Integrity_QuantidadeSobeEmVoo_DetectaAbastecimento()
        {
            var monitor = new FuelIntegrityMonitor(totalCapacityGallons: 26.0, customToleranceGallons: 0.5);

            // No ar, consumo normal
            monitor.Update(simFuelTotalGal: 20.0, unlimitedFuel: false, simOnGround: false, simulatedTimeSeconds: 100.0);
            monitor.Update(simFuelTotalGal: 19.5, unlimitedFuel: false, simOnGround: false, simulatedTimeSeconds: 160.0);
            monitor.Update(simFuelTotalGal: 19.0, unlimitedFuel: false, simOnGround: false, simulatedTimeSeconds: 220.0);

            // Subida de combustível pelo menu do simulador (19.0 -> 22.0 = subiu 3.0 gal > tolerância 0.5 gal)
            var events = monitor.Update(simFuelTotalGal: 22.0, unlimitedFuel: false, simOnGround: false, simulatedTimeSeconds: 250.0);

            Assert.Single(events);
            Assert.Equal(FuelIntegrityEventType.InFlightRefueling, events[0].Type);
            Assert.Equal(22.0, events[0].SimQuantity);
            Assert.Equal(19.0, events[0].BaselineOrExpectedQuantity);
        }

        [Fact]
        public void Integrity_RuidoPequeno_NaoDetecta()
        {
            var monitor = new FuelIntegrityMonitor(totalCapacityGallons: 26.0, customToleranceGallons: 0.5);

            // No ar
            monitor.Update(simFuelTotalGal: 20.0, unlimitedFuel: false, simOnGround: false, simulatedTimeSeconds: 100.0);

            // Oscilação de onda/sloshing no tanque de 0.2 gal (<= tolerância de 0.5 gal)
            var events = monitor.Update(simFuelTotalGal: 20.2, unlimitedFuel: false, simOnGround: false, simulatedTimeSeconds: 110.0);

            Assert.Empty(events);
        }

        [Fact]
        public void Integrity_NoSoloDiferenteDoApp_PedeSincronizacao()
        {
            var monitor = new FuelIntegrityMonitor(totalCapacityGallons: 26.0, customToleranceGallons: 0.5);

            // Simulação com combustível do sim MENOR que o do app (ex: sim 15.0 gal, app 20.0 gal)
            var eventsAbaixo = monitor.Update(simFuelTotalGal: 15.0, unlimitedFuel: false, simOnGround: true, simulatedTimeSeconds: 10.0, appFuelGal: 20.0);
            Assert.Single(eventsAbaixo);
            Assert.Equal(FuelIntegrityEventType.GroundMismatch, eventsAbaixo[0].Type);
            Assert.True(eventsAbaixo[0].RequiresSync);

            // Simulação com combustível do sim MAIOR que o do app (ex: sim 25.0 gal, app 20.0 gal)
            var eventsAcima = monitor.Update(simFuelTotalGal: 25.0, unlimitedFuel: false, simOnGround: true, simulatedTimeSeconds: 20.0, appFuelGal: 20.0);
            Assert.Single(eventsAcima);
            Assert.Equal(FuelIntegrityEventType.GroundMismatch, eventsAcima[0].Type);
            Assert.True(eventsAcima[0].RequiresSync);
        }

        [Fact]
        public void WritePlan_DistribuiPelaCapacidade_SemPassar()
        {
            // 2 asas 13/13 gal (total 26 gal)
            var tanks = new[]
            {
                new FuelTank("FUELSYSTEM TANK QUANTITY:1", 13.0),
                new FuelTank("FUELSYSTEM TANK QUANTITY:2", 13.0)
            };

            // Alvo 20 gal -> 10/10 gal, excedente 0
            var plan20 = FuelTankWritePlan.Distribute(20.0, tanks);
            Assert.Equal(20.0, plan20.TotalAllocatedGallons);
            Assert.Equal(0.0, plan20.ExcessGallons);
            Assert.Equal(2, plan20.Allocations.Count);
            Assert.Equal(10.0, plan20.Allocations[0].Gallons);
            Assert.Equal(10.0, plan20.Allocations[1].Gallons);

            // Alvo 30 gal -> passa da capacidade (26 gal) -> 13/13 gal, excedente 4.0 gal
            var plan30 = FuelTankWritePlan.Distribute(30.0, tanks);
            Assert.Equal(26.0, plan30.TotalAllocatedGallons);
            Assert.Equal(4.0, plan30.ExcessGallons);
            Assert.Equal(13.0, plan30.Allocations[0].Gallons);
            Assert.Equal(13.0, plan30.Allocations[1].Gallons);
        }

        [Fact]
        public void WritePlan_SistemaLegado_UsaNomesDosTanques()
        {
            var legacyTanks = new[]
            {
                new FuelTank("FUEL TANK LEFT MAIN QUANTITY", 15.0),
                new FuelTank("FUEL TANK RIGHT MAIN QUANTITY", 15.0),
                new FuelTank("FUEL TANK CENTER QUANTITY", 30.0)
            };

            // Capacidade total: 60 gal. Alvo: 40 gal.
            // Proporções: 15/60 = 25%, 15/60 = 25%, 30/60 = 50%.
            // Esperado: Left = 10 gal, Right = 10 gal, Center = 20 gal.
            var plan = FuelTankWritePlan.Distribute(40.0, legacyTanks);

            Assert.Equal(40.0, plan.TotalAllocatedGallons);
            Assert.Equal(0.0, plan.ExcessGallons);
            Assert.Equal("FUEL TANK LEFT MAIN QUANTITY", plan.Allocations[0].SimVar);
            Assert.Equal(10.0, plan.Allocations[0].Gallons);
            Assert.Equal("FUEL TANK RIGHT MAIN QUANTITY", plan.Allocations[1].SimVar);
            Assert.Equal(10.0, plan.Allocations[1].Gallons);
            Assert.Equal("FUEL TANK CENTER QUANTITY", plan.Allocations[2].SimVar);
            Assert.Equal(20.0, plan.Allocations[2].Gallons);
        }

        private static string GetFixturePath(string relativePath)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "Flights", relativePath),
                Path.Combine(Directory.GetCurrentDirectory(), "Fixtures", "Flights", relativePath),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "Flights", relativePath),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Tests", "Fixtures", "Flights", relativePath)
            };

            var match = candidates.FirstOrDefault(File.Exists);
            if (match != null) return Path.GetFullPath(match);

            throw new FileNotFoundException($"Fixture '{relativePath}' não encontrada nos caminhos buscados:\n" + string.Join("\n", candidates));
        }

        [Fact]
        public void Replay_CombustivelConsumido_CoerenteComInicialMenosFinal()
        {
            string fixturePath = GetFixturePath("c152-sbmt-snej-20261115.csv.gz");
            var frames = SensorLogReader.ReadFrames(fixturePath);

            Assert.NotEmpty(frames);

            var accumulator = new FuelUsageAccumulator();
            double? firstGallons = null;
            double? lastGallons = null;
            double? initialWeightLbs = null;

            foreach (var frame in frames)
            {
                if (frame.Values.TryGetValue("fuel-total-quantity", out double gal))
                {
                    firstGallons ??= gal;
                    lastGallons = gal;
                }

                if (frame.Values.TryGetValue("fuel-total-quantity-weight", out double weightLbs))
                {
                    initialWeightLbs ??= weightLbs;
                    // Simula o contador de combustível usado acumulado pelo motor a partir da perda de peso dos tanques
                    double fuelUsedSinceStartLbs = Math.Max(0.0, initialWeightLbs.Value - weightLbs);
                    accumulator.Update(new[] { fuelUsedSinceStartLbs });
                }
            }

            Assert.NotNull(firstGallons);
            Assert.NotNull(lastGallons);

            double diffGallons = Math.Max(0.0, firstGallons.Value - lastGallons.Value);
            double accumulatorGallons = accumulator.TotalGallons(6.0); // AVGAS ~6.0 lb/gal

            Assert.True(diffGallons > 0, "O voo na fixture deveria ter consumido combustível.");

            // Coerente dentro de ±10% entre acumulador e (inicial - final)
            double discrepancy = Math.Abs(accumulatorGallons - diffGallons);
            double maxAllowedDiscrepancy = Math.Max(0.01, diffGallons * 0.10);

            Assert.True(discrepancy <= maxAllowedDiscrepancy,
                $"Divergência de consumo ({discrepancy:F4} gal) excedeu 10% do consumo ({diffGallons:F4} gal). Acumulador: {accumulatorGallons:F4} gal.");
        }

        [Fact]
        public void Accumulator_Reset_DoisVoosSeguidos_ContaApenasSegundoVoo()
        {
            var acc = new FuelUsageAccumulator();

            // Voo 1: consome 15 lb
            acc.Update(new[] { 0.0 });
            acc.Update(new[] { 10.0 });
            acc.Update(new[] { 15.0 });
            Assert.Equal(15.0, acc.TotalLbs);

            // Fim do voo 1 / Início do voo 2: reset do acumulador
            acc.Reset();
            Assert.Equal(0.0, acc.TotalLbs);

            // Voo 2: simula novo motor iniciando em 0 lb e consumindo 6 lb
            acc.Update(new[] { 0.0 });
            acc.Update(new[] { 6.0 });
            Assert.Equal(6.0, acc.TotalLbs);

            // Garante que os 15 lb do primeiro voo não foram cobrados
            double galVoo2 = acc.TotalGallons(6.0);
            Assert.Equal(1.0, galVoo2, 4);
        }

        [Fact]
        public void SyncCoordinator_ValorInicialDentroDaTolerancia_ConfirmaImediatamente()
        {
            var coord = new FuelSyncCoordinator();
            // Alvo 20 gal, sim 20.2 gal, tolerância 0.5 gal, no solo
            coord.Start(targetGallons: 20.0, currentSimGallons: 20.2, tolerance: 0.5, isOnGround: true);

            Assert.Equal(FuelSyncStatus.Confirmed, coord.Status);
            Assert.False(coord.NeedsSimWrite);
            Assert.Equal(0, coord.AttemptCount);
        }

        [Fact]
        public void SyncCoordinator_DivergenciaForaDoSolo_FalhaImediatamente()
        {
            var coord = new FuelSyncCoordinator();
            // Alvo 20 gal, sim 15.0 gal, no ar
            coord.Start(targetGallons: 20.0, currentSimGallons: 15.0, tolerance: 0.5, isOnGround: false);

            Assert.Equal(FuelSyncStatus.Failed, coord.Status);
            Assert.False(coord.NeedsSimWrite);
        }

        [Fact]
        public void SyncCoordinator_DivergenciaNoSolo_EscreveEConfirmaNoProximoTick()
        {
            var currentTime = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var coord = new FuelSyncCoordinator(() => currentTime);

            // Início no solo com divergência (alvo 20 gal, sim 10 gal)
            coord.Start(targetGallons: 20.0, currentSimGallons: 10.0, tolerance: 0.5, isOnGround: true);

            Assert.Equal(FuelSyncStatus.Pending, coord.Status);
            Assert.True(coord.NeedsSimWrite);
            Assert.Equal(1, coord.AttemptCount);

            // Dispara escrita no sim
            coord.NotifyWriteDispatched();
            Assert.False(coord.NeedsSimWrite);

            // Próximo tick de telemetria após 1 segundo: leitura atualizou para 20.1 gal
            currentTime = currentTime.AddSeconds(1);
            coord.Update(currentSimGallons: 20.1, isOnGround: true);

            Assert.Equal(FuelSyncStatus.Confirmed, coord.Status);
            Assert.False(coord.NeedsSimWrite);
        }

        [Fact]
        public void SyncCoordinator_Timeout5s_DisparaNovaTentativaAteTresVezes_E_Falha()
        {
            var currentTime = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var coord = new FuelSyncCoordinator(() => currentTime);

            coord.Start(targetGallons: 20.0, currentSimGallons: 10.0, tolerance: 0.5, isOnGround: true);
            Assert.Equal(1, coord.AttemptCount);
            Assert.True(coord.NeedsSimWrite);
            coord.NotifyWriteDispatched();

            // Passam 4.9 segundos sem o simulador atualizar: continua pendente, sem reescrever
            currentTime = currentTime.AddSeconds(4.9);
            coord.Update(currentSimGallons: 10.0, isOnGround: true);
            Assert.Equal(FuelSyncStatus.Pending, coord.Status);
            Assert.False(coord.NeedsSimWrite);
            Assert.Equal(1, coord.AttemptCount);

            // Passam mais 0.2 segundos (total 5.1s): dispara tentativa 2
            currentTime = currentTime.AddSeconds(0.2);
            coord.Update(currentSimGallons: 10.0, isOnGround: true);
            Assert.Equal(FuelSyncStatus.Pending, coord.Status);
            Assert.True(coord.NeedsSimWrite);
            Assert.Equal(2, coord.AttemptCount);
            coord.NotifyWriteDispatched();

            // Passam mais 5.1 segundos: dispara tentativa 3
            currentTime = currentTime.AddSeconds(5.1);
            coord.Update(currentSimGallons: 10.0, isOnGround: true);
            Assert.Equal(FuelSyncStatus.Pending, coord.Status);
            Assert.True(coord.NeedsSimWrite);
            Assert.Equal(3, coord.AttemptCount);
            coord.NotifyWriteDispatched();

            // Passam mais 5.1 segundos sem convergir (total > 15s): esgota as 3 tentativas e falha
            currentTime = currentTime.AddSeconds(5.1);
            coord.Update(currentSimGallons: 10.0, isOnGround: true);
            Assert.Equal(FuelSyncStatus.Failed, coord.Status);
            Assert.False(coord.NeedsSimWrite);
        }

        [Fact]
        public void SyncCoordinator_DecolaDuranteSincronizacaoPendente_Falha()
        {
            var currentTime = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var coord = new FuelSyncCoordinator(() => currentTime);

            coord.Start(targetGallons: 20.0, currentSimGallons: 10.0, tolerance: 0.5, isOnGround: true);
            coord.NotifyWriteDispatched();

            // Aeronave decola antes da conferência concluir
            currentTime = currentTime.AddSeconds(2);
            coord.Update(currentSimGallons: 10.0, isOnGround: false);

            Assert.Equal(FuelSyncStatus.Failed, coord.Status);
        }
    }
}
