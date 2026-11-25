using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Career;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class Phase2ContractTests
    {
        [Fact]
        public void SurveyLegTracker_PernaRetaDentroDaTolerancia_ContaComoConcluida()
        {
            var tracker = new SurveyLegTracker();
            var data = new SurveyContractData
            {
                CenterLatitude = -23.0,
                CenterLongitude = -46.0,
                WidthNM = 3.0,
                LengthNM = 3.0,
                OrientationDeg = 0.0, // Pernas no sentido Sul -> Norte
                RequiredLegs = 3,
                TargetAltitudeFt = 3500.0,
                TargetSpeedKt = 90.0
            };
            tracker.Configure(data);

            // Inicia ao sul da área retangular (Length 3 NM -> de -1.5 a +1.5 NM, ou latitude -23.025 a -22.975)
            // 1 grau de latitude = 60 NM, então 1.5 NM = 0.025 graus
            // Entra por lat -23.03 e cruza até -22.97
            for (double lat = -23.030; lat <= -22.970; lat += 0.002)
            {
                tracker.Update(lat, -46.0, 3500.0, 90.0, 0.0);
            }
            // Sai da área
            tracker.Update(-22.960, -46.0, 3500.0, 90.0, 0.0);

            Assert.Equal(1, tracker.CompletedLegs);
        }

        [Fact]
        public void SurveyLegTracker_ForaDeAltitudePorMaisDe20Porcento_NaoConta()
        {
            var tracker = new SurveyLegTracker();
            var data = new SurveyContractData
            {
                CenterLatitude = -23.0,
                CenterLongitude = -46.0,
                WidthNM = 3.0,
                LengthNM = 3.0,
                OrientationDeg = 0.0,
                RequiredLegs = 3,
                TargetAltitudeFt = 3500.0,
                TargetSpeedKt = 90.0
            };
            tracker.Configure(data);

            int step = 0;
            for (double lat = -23.030; lat <= -22.970; lat += 0.002)
            {
                step++;
                // Em 40% das amostras a altitude varia além de ±150 ft (ex: 3800 ft)
                double alt = (step % 2 == 0) ? 3800.0 : 3500.0;
                tracker.Update(lat, -46.0, alt, 90.0, 0.0);
            }
            tracker.Update(-22.960, -46.0, 3500.0, 90.0, 0.0);

            Assert.Equal(0, tracker.CompletedLegs);
        }

        [Fact]
        public void SurveyLegTracker_RumoForaDe15Graus_NaoConta()
        {
            var tracker = new SurveyLegTracker();
            var data = new SurveyContractData
            {
                CenterLatitude = -23.0,
                CenterLongitude = -46.0,
                WidthNM = 3.0,
                LengthNM = 3.0,
                OrientationDeg = 0.0,
                RequiredLegs = 3,
                TargetAltitudeFt = 3500.0,
                TargetSpeedKt = 90.0
            };
            tracker.Configure(data);

            for (double lat = -23.030; lat <= -22.970; lat += 0.002)
            {
                // Rumo 030° (diferença de 30° contra o alvo 0°, limite é ±15°)
                tracker.Update(lat, -46.0, 3500.0, 90.0, 30.0);
            }
            tracker.Update(-22.960, -46.0, 3500.0, 90.0, 30.0);

            Assert.Equal(0, tracker.CompletedLegs);
        }

        [Fact]
        public void SurveyLegTracker_NPernasExigidas_CompletaContrato()
        {
            var tracker = new SurveyLegTracker();
            var data = new SurveyContractData
            {
                CenterLatitude = -23.0,
                CenterLongitude = -46.0,
                WidthNM = 3.0,
                LengthNM = 3.0,
                OrientationDeg = 0.0,
                RequiredLegs = 3,
                TargetAltitudeFt = 3500.0,
                TargetSpeedKt = 90.0
            };
            tracker.Configure(data);

            for (int leg = 0; leg < 3; leg++)
            {
                for (double lat = -23.030; lat <= -22.970; lat += 0.002)
                {
                    tracker.Update(lat, -46.0 + (leg * 0.01), 3500.0, 90.0, 0.0);
                }
                tracker.Update(-22.960, -46.0 + (leg * 0.01), 3500.0, 90.0, 0.0);
            }

            Assert.Equal(3, tracker.CompletedLegs);
            Assert.True(tracker.IsCompleted);
        }

        [Fact]
        public void SkydiveDropTracker_Subida30SegundosEstavelNaZona_SaltoFeito()
        {
            var tracker = new SkydiveDropTracker();
            var data = new SkydiveContractData
            {
                DropZoneLatitude = -23.0,
                DropZoneLongitude = -46.0,
                DropZoneElevationFt = 2000.0,
                TargetAltitudeAglFt = 10000.0, // Alvo MSL = 12.000 ft
                MaxDropSpeedKt = 90.0
            };
            tracker.Configure(data);

            // A 1 NM da zona, altitude 12.050 ft MSL (dentro de ±200 ft), velocidade 80 kt
            for (int sec = 0; sec < 30; sec++)
            {
                tracker.Update(-23.01, -46.0, 12050.0, 80.0, 10.0, 1.0);
            }

            Assert.True(tracker.DropCompleted);
        }

        [Fact]
        public void SkydiveDropTracker_Apenas20Segundos_NaoCompletaSalto()
        {
            var tracker = new SkydiveDropTracker();
            var data = new SkydiveContractData
            {
                DropZoneLatitude = -23.0,
                DropZoneLongitude = -46.0,
                DropZoneElevationFt = 2000.0,
                TargetAltitudeAglFt = 10000.0,
                MaxDropSpeedKt = 90.0
            };
            tracker.Configure(data);

            for (int sec = 0; sec < 20; sec++)
            {
                tracker.Update(-23.01, -46.0, 12050.0, 80.0, 10.0, 1.0);
            }

            Assert.False(tracker.DropCompleted);
        }

        [Fact]
        public void SkydiveDropTracker_ForaDaZonaDeSalto_NaoConta()
        {
            var tracker = new SkydiveDropTracker();
            var data = new SkydiveContractData
            {
                DropZoneLatitude = -23.0,
                DropZoneLongitude = -46.0,
                DropZoneElevationFt = 2000.0,
                TargetAltitudeAglFt = 10000.0,
                MaxDropSpeedKt = 90.0
            };
            tracker.Configure(data);

            // A 3.5 NM de distância (limite é 2.0 NM)
            for (int sec = 0; sec < 40; sec++)
            {
                tracker.Update(-23.06, -46.0, 12000.0, 80.0, 10.0, 1.0);
            }

            Assert.False(tracker.DropCompleted);
        }

        [Fact]
        public void MissionCompletion_SurveyLegs_ConcluidoOuNaoComMotivo()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                SubId = "3.6.1",
                ContractData = JsonSerializer.Serialize(new SurveyContractData { RequiredLegs = 4 })
            };

            var incomplete = MissionCompletion.Evaluate(mission, "SBGR", surveyLegsCompleted: 2);
            Assert.False(incomplete.IsCompleted);
            Assert.Contains("2 de 4 pernas", incomplete.Reason);

            var complete = MissionCompletion.Evaluate(mission, "SBGR", surveyLegsCompleted: 4);
            Assert.True(complete.IsCompleted);
            Assert.Null(complete.Reason);
        }

        [Fact]
        public void MissionCompletion_Skydive_SaltoNaoFeito_ReprovaComMotivo()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                SubId = "3.6.2",
                ContractData = JsonSerializer.Serialize(new SkydiveContractData { TargetAltitudeAglFt = 10000.0 })
            };

            var incomplete = MissionCompletion.Evaluate(mission, "SBGR", skydiveDropCompleted: false);
            Assert.False(incomplete.IsCompleted);
            Assert.Contains("não estabilizou na altitude de salto", incomplete.Reason);
        }

        [Fact]
        public void MissionCompletion_Skydive_ChoqueTermicoGeraAvisoSemReprovar()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                OriginICAO = "SBGR",
                DestinationICAO = "SBGR",
                SubId = "3.6.2"
            };

            var result = MissionCompletion.Evaluate(
                mission, "SBGR",
                skydiveDropCompleted: true,
                skydiveThermalShock: true,
                skydiveMaxCoolingRate: 34.5);

            Assert.True(result.IsCompleted);
            Assert.Null(result.Reason);
            Assert.NotNull(result.Warning);
            Assert.Contains("34", result.Warning);
            Assert.Contains("Choque térmico na descida", result.Warning);
        }

        [Fact]
        public void Gerador_ContratosFase2_ParametrosCoerentes()
        {
            var origin = new Airport
            {
                ICAO = "SDRR",
                Latitude = -23.1,
                Longitude = -46.5,
                ElevationFt = 2100,
                Tier = 3,
                Country = "BR"
            };

            var subSurvey = new MissionDefinition { Id = "3.6.1", Name = "Levantamento", MinExpHours = 2, MaxExpHours = 6, Urgency = 1.0 };
            var subSkydive = new MissionDefinition { Id = "3.6.2", Name = "Paraquedismo", MinExpHours = 2, MaxExpHours = 6, Urgency = 1.0 };

            var rng = new Random(42);
            var survey = MissionGenerator.CreateSurveyMissionSync(origin, subSurvey, rng, DateTime.UtcNow);
            Assert.Equal("3.6.1", survey.SubId);
            Assert.Equal(CareerPhase.Phase2_SAE, survey.RequiredPhase);
            Assert.NotNull(survey.ContractData);
            var surveyData = JsonSerializer.Deserialize<SurveyContractData>(survey.ContractData);
            Assert.NotNull(surveyData);
            Assert.InRange(surveyData.RequiredLegs, 3, 5);
            Assert.InRange(surveyData.TargetAltitudeFt, 3100, 6000);

            var skydive = MissionGenerator.CreateSkydiveMissionSync(origin, subSkydive, rng, DateTime.UtcNow, 25.0);
            Assert.Equal("3.6.2", skydive.SubId);
            Assert.Equal(CareerPhase.Phase2_SAE, skydive.RequiredPhase);
            Assert.InRange(skydive.PassengerCount, 4, 10);
            Assert.NotNull(skydive.ContractData);
            var skydiveData = JsonSerializer.Deserialize<SkydiveContractData>(skydive.ContractData);
            Assert.NotNull(skydiveData);
            Assert.InRange(skydiveData.TargetAltitudeAglFt, 10000, 12000);
        }

        [Fact]
        public async Task BancoAntigo_MigracaoContractData_Sucesso()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                await db.EnsureSchemaUpdatedAsync();

                var mission = new Mission
                {
                    Id = Guid.NewGuid(),
                    Title = "Aerofoto Teste",
                    OriginICAO = "SBGR",
                    DestinationICAO = "SBGR",
                    RewardMoney = 1500,
                    SubId = "3.6.1",
                    ContractData = "{\"RequiredLegs\":3}"
                };
                db.Missions.Add(mission);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var loaded = await db.Missions.FirstAsync();
                Assert.Equal("{\"RequiredLegs\":3}", loaded.ContractData);
            }
        }

        [Fact]
        public async Task FlightService_ParaquedistasNaoContamComoPaxNoPouso()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                var type = new AircraftType { Id = Guid.NewGuid(), Model = "C208B", Type = "Geral", FuelType = "JetA" };
                var pilot = new Pilot { Id = pilotId, Name = "Piloto Salto", Balance = 10000 };
                var aircraft = new PilotAircraft { Id = aircraftId, PilotId = pilotId, AircraftTypeId = type.Id, Prefix = "PT-SKD", FuelAmount = 50.0 };
                var airport = new Airport { ICAO = "SBGR", Latitude = -23.4, Longitude = -46.4, ElevationFt = 2461, Tier = 1, Country = "BR" };

                var mission = new Mission
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilotId,
                    IsAccepted = true,
                    Title = "Salto 8 Pax",
                    OriginICAO = "SBGR",
                    DestinationICAO = "SBGR",
                    RewardMoney = 2000,
                    SubId = "3.6.2",
                    PassengerCount = 8
                };

                db.Airports.Add(airport);
                db.Pilots.Add(pilot);
                db.AircraftTypes.Add(type);
                db.PilotAircrafts.Add(aircraft);
                db.Missions.Add(mission);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var service = new FlightService(db, null!, null!);
                var activeMissions = await db.Missions.ToListAsync();

                var outcome = await service.ProcessFlightFinishAsync(
                    pilotId: pilotId,
                    aircraftId: aircraftId,
                    originIcao: "SBGR",
                    destinationIcao: "SBGR",
                    startTime: DateTime.Now.AddMinutes(-40),
                    initialFuel: 50.0,
                    finalFuel: 40.0,
                    distanceNM: 25.0,
                    activeMissions: activeMissions,
                    qualityService: new FlightQualityService(),
                    flightDurationMinutes: 35.0,
                    skydiveDropCompleted: true);

                Assert.True(outcome.Success);
                Assert.Equal(1, outcome.CompletedMissions);

                // Conferir transações financeiras geradas: não deve haver "Taxa de Embarque" (PAX_FEE)
                var paxFeeTx = await db.FinancialTransactions.FirstOrDefaultAsync(t => t.Category == "Taxa de Embarque");
                Assert.Null(paxFeeTx);
            }
        }

        [Fact]
        public async Task FlightService_ContratoNaoCumprido_DevolveMotivoNoResultado()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();

            await using var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var type = new AircraftType { Id = Guid.NewGuid(), Model = "C152", Type = "Geral", FuelType = "AvGas" };
            db.Airports.Add(new Airport { ICAO = "SBJS", Latitude = -7.34, Longitude = -41.14, ElevationFt = 1036, Tier = 4, Country = "BR" });
            db.Pilots.Add(new Pilot { Id = pilotId, Name = "Piloto TGL", Balance = 10000 });
            db.AircraftTypes.Add(type);
            db.PilotAircrafts.Add(new PilotAircraft { Id = aircraftId, PilotId = pilotId, AircraftTypeId = type.Id, Prefix = "PR-TGL", FuelAmount = 20.0 });
            db.Missions.Add(new Mission
            {
                Id = Guid.NewGuid(),
                PilotId = pilotId,
                IsAccepted = true,
                Title = "Instrução com TGL",
                OriginICAO = "SBJS",
                DestinationICAO = "SBJS",
                RewardMoney = 1200,
                SubId = "instrucao_local",
                RequiredTouchAndGoes = 3
            });
            await db.SaveChangesAsync();

            var service = new FlightService(db, null!, null!);
            var outcome = await service.ProcessFlightFinishAsync(
                pilotId: pilotId,
                aircraftId: aircraftId,
                originIcao: "SBJS",
                destinationIcao: "SBJS",
                startTime: DateTime.Now.AddMinutes(-20),
                initialFuel: 20.0,
                finalFuel: 15.0,
                distanceNM: 15.0,
                activeMissions: await db.Missions.ToListAsync(),
                qualityService: new FlightQualityService(),
                flightDurationMinutes: 20.0,
                touchAndGoCount: 0);

            Assert.True(outcome.Success);
            Assert.Equal(0, outcome.CompletedMissions);
            Assert.Equal(0, outcome.Revenue);
            var reason = Assert.Single(outcome.IncompleteReasons);
            Assert.Contains("0 de 3 TGL", reason);
        }
    }
}
