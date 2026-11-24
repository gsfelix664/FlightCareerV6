using System;
using System.Collections.Generic;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class MultiStopTrackerTests
    {
        private static MultiStopContractData CreateTestData()
        {
            return new MultiStopContractData
            {
                Stops = new List<StopPoint>
                {
                    new() { Icao = "SBDN", Lat = -22.1969, Lon = -49.9261, ElevationFt = 1476 },
                    new() { Icao = "SDCO", Lat = -23.4808, Lon = -47.4897, ElevationFt = 2077 }
                },
                RequireNoRefuel = true
            };
        }

        [Fact]
        public void EscalasNaOrdem_ComParadaCompleta_CompletaComSucesso()
        {
            var tracker = new MultiStopTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // 1. Decolagem da origem
            tracker.Update(isOnGround: false, -23.4356, -46.4731, groundSpeedKt: 90.0, fuelTotalGallons: 50.0, simDtSeconds: 10.0);
            Assert.Equal(0, tracker.CompletedStopsCount);
            Assert.False(tracker.AllStopsCompleted);

            // 2. Aproximação e toque na escala 1 (SBDN)
            tracker.Update(isOnGround: true, -22.1969, -49.9261, groundSpeedKt: 2.0, fuelTotalGallons: 40.0, simDtSeconds: 5.5);
            Assert.Equal(1, tracker.CompletedStopsCount);
            Assert.False(tracker.AllStopsCompleted);

            // 3. Decolagem da escala 1 (no ar >= 5 s)
            tracker.Update(isOnGround: false, -22.1969, -49.9261, groundSpeedKt: 95.0, fuelTotalGallons: 38.0, simDtSeconds: 6.0);

            // 4. Aproximação e parada completa na escala 2 (SDCO)
            tracker.Update(isOnGround: true, -23.4808, -47.4897, groundSpeedKt: 1.5, fuelTotalGallons: 30.0, simDtSeconds: 5.2);
            Assert.Equal(2, tracker.CompletedStopsCount);
            Assert.True(tracker.AllStopsCompleted);
            Assert.False(tracker.RefuelViolationDetected);
            Assert.Null(tracker.FailureReason);
        }

        [Fact]
        public void EscalaPulada_NaoCompleta_InformaEscalaNaoVisitada()
        {
            var tracker = new MultiStopTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // Decolagem da origem
            tracker.Update(isOnGround: false, -23.4356, -46.4731, 100.0, 50.0, 10.0);

            // Pousa no destino final sem visitar SBDN nem SDCO
            tracker.Update(isOnGround: true, -23.1822, -46.9431, 1.0, 35.0, 6.0);

            Assert.False(tracker.AllStopsCompleted);
            Assert.Equal(0, tracker.CompletedStopsCount);
            Assert.NotNull(tracker.FailureReason);
            Assert.Contains("SBDN", tracker.FailureReason);
        }

        [Fact]
        public void OrdemTrocada_ParadaNaSegundaEscalaAntesDaPrimeira_GeraFalha()
        {
            var tracker = new MultiStopTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // Decola da origem
            tracker.Update(isOnGround: false, -23.4356, -46.4731, 100.0, 50.0, 10.0);

            // Pousa e para em SDCO (escala 2) antes de passar por SBDN (escala 1)
            tracker.Update(isOnGround: true, -23.4808, -47.4897, 1.0, 40.0, 6.0);

            Assert.True(tracker.OrderViolationDetected);
            Assert.False(tracker.AllStopsCompleted);
            Assert.Contains("SDCO", tracker.FailureReason);
            Assert.Contains("SBDN", tracker.FailureReason);
        }

        [Fact]
        public void ToqueSemParar_RolagemRapida_NaoContaEscala()
        {
            var tracker = new MultiStopTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            tracker.Update(isOnGround: false, -23.4356, -46.4731, 100.0, 50.0, 10.0);

            // Toque em SBDN mas velocidade permanece > 3 kt (ex.: 20 kt corrida rápida de arremetida)
            tracker.Update(isOnGround: true, -22.1969, -49.9261, 20.0, 42.0, 6.0);
            tracker.Update(isOnGround: false, -22.1969, -49.9261, 80.0, 41.0, 6.0);

            Assert.Equal(0, tracker.CompletedStopsCount);
        }

        [Fact]
        public void CombustivelSubindo5Gal_EntreEscalas_GeraFalhaDeReabastecimento()
        {
            var tracker = new MultiStopTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // Decola com 50 gal
            tracker.Update(isOnGround: false, -23.4356, -46.4731, 100.0, 50.0, 10.0);

            // Consumiu até 35 gal
            tracker.Update(isOnGround: false, -22.5, -48.0, 100.0, 35.0, 10.0);

            // Reabasteceu 5 gal (subiu para 40 gal > 35 + max(1, 0.7) = 36)
            tracker.Update(isOnGround: true, -22.1969, -49.9261, 1.0, 41.0, 6.0);

            Assert.True(tracker.RefuelViolationDetected);
            Assert.Contains("Reabasteceu", tracker.FailureReason);
        }

        [Fact]
        public void CombustivelSubindoRuido_0Ponto5Gal_NaoReprova()
        {
            var tracker = new MultiStopTracker();
            var data = CreateTestData();
            tracker.Configure(data, initialIsOnGround: true);

            // Decola com 50 gal
            tracker.Update(isOnGround: false, -23.4356, -46.4731, 100.0, 50.0, 10.0);

            // Consumiu até 40 gal
            tracker.Update(isOnGround: false, -22.5, -48.0, 100.0, 40.0, 10.0);

            // Ruído de leitura de 0.5 gal (sobe para 40.5 gal <= 40 + 1.0)
            tracker.Update(isOnGround: true, -22.1969, -49.9261, 1.0, 40.5, 6.0);

            Assert.False(tracker.RefuelViolationDetected);
        }
    }
}
