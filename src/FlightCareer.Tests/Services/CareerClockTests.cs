using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightCareer.Application.DTOs;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Services
{
    public class CareerClockTests
    {
        [Fact]
        public void CareerClock_VooAvancaDuracaoMaisSolo()
        {
            var initialClock = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
            double flightDurationHours = 1.5;

            var newClock = CareerClock.AdvanceFlight(initialClock, flightDurationHours);

            // 1.5 h de voo + 0.5 h de solo/turnaround = 2.0 h
            Assert.Equal(initialClock.AddHours(2.0), newClock);
            Assert.Equal(0.5, CareerClock.GroundTurnaroundHours);
        }

        [Fact]
        public async Task FlightService_FimDeVoo_AvancaRelogioDeCarreira()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();
            var initialClock = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var pilot = new Pilot
                {
                    Id = pilotId,
                    Name = "Comandante Teste",
                    CareerClockUtc = initialClock
                };
                var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "Avgas" };
                var aircraft = new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = type.Id,
                    Prefix = "PT-CLK",
                    CurrentAirframeHours = 100.0,
                    FuelAmount = 20.0
                };

                db.Pilots.Add(pilot);
                db.AircraftTypes.Add(type);
                db.PilotAircrafts.Add(aircraft);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var service = new FlightService(db, null!, null!);
                var quality = new FlightQualityService();

                // Voo de 60 minutos (1.0 hora)
                var outcome = await service.ProcessFlightFinishAsync(
                    pilotId: pilotId,
                    aircraftId: aircraftId,
                    originIcao: "SBMT",
                    destinationIcao: "SBAU",
                    startTime: DateTime.Now.AddHours(-1),
                    initialFuel: 20.0,
                    finalFuel: 15.0,
                    distanceNM: 100.0,
                    activeMissions: new List<Mission>(),
                    qualityService: quality,
                    flightDurationMinutes: 60.0);

                Assert.True(outcome.Success);
            }

            await using (var db = new AppDbContext(options))
            {
                var pilot = await db.Pilots.FindAsync(pilotId);
                Assert.NotNull(pilot);

                // 1.0 h de voo + 0.5 h de solo = 1.5 h de avanço
                var expected = initialClock.AddHours(1.5);
                Assert.Equal(expected, pilot.CareerClockUtc);
            }
        }

        [Fact]
        public void CareerClock_ServicoAvancaParada()
        {
            var baseClock = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

            // Inspeção 50h: 2h de parada
            var clockAfter50h = CareerClock.Advance(baseClock, CareerClock.Inspection50hDowntimeHours);
            Assert.Equal(baseClock.AddHours(2.0), clockAfter50h);
            Assert.Equal(2.0, CareerClock.Inspection50hDowntimeHours);

            // Inspeção 100h: 8h de parada
            var clockAfter100h = CareerClock.Advance(baseClock, CareerClock.Inspection100hDowntimeHours);
            Assert.Equal(baseClock.AddHours(8.0), clockAfter100h);
            Assert.Equal(8.0, CareerClock.Inspection100hDowntimeHours);

            // Revisão 500h: 6h de parada
            var clockAfter500h = CareerClock.Advance(baseClock, CareerClock.Inspection500hDowntimeHours);
            Assert.Equal(baseClock.AddHours(6.0), clockAfter500h);
            Assert.Equal(6.0, CareerClock.Inspection500hDowntimeHours);

            // Retífica (Overhaul): 72h de parada
            var clockAfterOverhaul = CareerClock.Advance(baseClock, CareerClock.OverhaulDowntimeHours);
            Assert.Equal(baseClock.AddHours(72.0), clockAfterOverhaul);
            Assert.Equal(72.0, CareerClock.OverhaulDowntimeHours);

            // Horas nulas ou negativas não avançam
            Assert.Equal(baseClock, CareerClock.Advance(baseClock, 0));
            Assert.Equal(baseClock, CareerClock.Advance(baseClock, -5));
        }

        [Fact]
        public void Inspection_CalendarioUsaRelogioDeCarreira()
        {
            var baseClock = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var def = new PartDefinition
            {
                Id = "oil.lubricant",
                Name = "Óleo Lubrificante",
                Group = PartGroup.Oleo,
                CalendarLifeMonths = 3 // Limite de 3 meses de calendário
            };

            var part = new AircraftPart
            {
                PartId = def.Id,
                Health = 95.0,
                HoursSinceReplacement = 10.0,
                CyclesSinceReplacement = 5,
                LastReplacedAtUtc = baseClock
            };

            // Cenário 1: Passaram 100 dias no relógio do Windows, mas o jogador NÃO voou (relógio de carreira estagnado)
            var careerNowSemVoar = baseClock; // 0 dias transcorridos de carreira
            var resultSemVoar = PartInspectionFlags.Evaluate(part, def, careerNowSemVoar);
            Assert.Equal(PartFlag.None, resultSemVoar.Level);
            Assert.Empty(resultSemVoar.Reason);

            // Cenário 2: O relógio de carreira avançou 3 meses (~92 dias de tempo de carreira simulado)
            var careerNow3Meses = baseClock.AddDays(92);
            var resultComCarreira = PartInspectionFlags.Evaluate(part, def, careerNow3Meses);
            Assert.Equal(PartFlag.None, resultComCarreira.Level); // Vida/calendário não marca mais Suspect
            var lifeNote = PartInspectionFlags.LifeNote(part, def, careerNow3Meses);
            Assert.NotNull(lifeNote);
            Assert.Contains("Calendário", lifeNote);
        }

        [Fact]
        public void Seguro_ExpiraPeloRelogioDeCarreira()
        {
            var aircraft = new PilotAircraft
            {
                Id = Guid.NewGuid(),
                Prefix = "PT-SEG",
                CurrentAirframeHours = 200.0
            };

            var baseClock = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

            // Contratação de 30 dias de seguro a partir do relógio de carreira
            aircraft.InsuranceExpirationDate = InsuranceService.CalculateNewExpirationDate(aircraft.InsuranceExpirationDate, baseClock);
            Assert.Equal(baseClock.AddDays(30), aircraft.InsuranceExpirationDate);

            // Cenário 1: Passaram 10 dias de carreira -> Seguro permanece ativo (faltam 20 dias)
            var clock10Dias = baseClock.AddDays(10);
            Assert.True(InsuranceService.IsInsured(aircraft, clock10Dias));

            // Cenário 2: Passaram 100 dias no relógio do Windows, mas apenas 20 dias de carreira -> Seguro ATIVO
            var clock20Dias = baseClock.AddDays(20);
            Assert.True(InsuranceService.IsInsured(aircraft, clock20Dias));

            // Cenário 3: Passaram 31 dias no relógio de carreira -> Seguro EXPIRADO
            var clock31Dias = baseClock.AddDays(31);
            Assert.False(InsuranceService.IsInsured(aircraft, clock31Dias));

            // Cenário 4: Renovação estende +30 dias a partir da validade vigente
            aircraft.InsuranceExpirationDate = InsuranceService.CalculateNewExpirationDate(aircraft.InsuranceExpirationDate, clock20Dias);
            Assert.Equal(baseClock.AddDays(60), aircraft.InsuranceExpirationDate);
            Assert.True(InsuranceService.IsInsured(aircraft, clock31Dias));
        }
    }
}
