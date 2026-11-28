using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlightCareer.Domain.Career;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Xp;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Career
{
    public class CareerPhaseTests
    {
        [Fact]
        public void Nivel_sobe_apenas_com_xp_sem_exigir_pagamento()
        {
            var pilot = new Pilot
            {
                Id = Guid.NewGuid(),
                Name = "Aluno Piloto",
                CareerPhase = CareerPhase.Phase1_FlightSchool,
                ExperiencePoints = 0
            };

            Assert.Equal(1, pilot.Level);

            // Nível 2 (300 XP)
            pilot.ExperiencePoints = 350;
            Assert.Equal(2, pilot.Level);
            Assert.Equal(CareerPhase.Phase1_FlightSchool, pilot.CareerPhase);

            // Nível 5 (4800 XP)
            pilot.ExperiencePoints = 5000;
            Assert.Equal(5, pilot.Level);
            Assert.Equal(CareerPhase.Phase1_FlightSchool, pilot.CareerPhase);
        }

        [Fact]
        public void Promocao_exige_triade_completa_nivel_horas_e_homologacao()
        {
            var pilot = new Pilot
            {
                Id = Guid.NewGuid(),
                Name = "Piloto Em Formação",
                CareerPhase = CareerPhase.Phase1_FlightSchool,
                TotalHours = 39.0, // Falta 1 hora para a Fase 2 (exige 40h)
                ExperiencePoints = 7500, // Nível 6 (L6 = 300 * 25 = 7500 XP)
                Balance = 50000.0 // Homologação Fase 2 = $35.000
            };

            var nextPhase = CareerPhase.Phase2_SAE;
            int reqLevel = nextPhase.GetEntryLevel(); // 6
            double reqHours = nextPhase.GetMinHours(); // 40
            double cost = nextPhase.GetPromotionCost(); // 35000

            // 1. Nível 6 com 39h -> Bloqueia por horas
            Assert.True(pilot.Level >= reqLevel);
            Assert.False(pilot.TotalHours >= reqHours, "Deve bloquear por horas faltantes");
            Assert.True(pilot.Balance >= cost);

            // 2. Com 40h mas saldo insuficiente -> Bloqueia por homologação
            pilot.TotalHours = 40.0;
            pilot.Balance = 20000.0; // Menor que os $35.000
            Assert.True(pilot.Level >= reqLevel);
            Assert.True(pilot.TotalHours >= reqHours);
            Assert.False(pilot.Balance >= cost, "Deve bloquear por falta de saldo para homologação");

            // 3. Com todos os requisitos preenchidos -> Promove com sucesso
            pilot.Balance = 40000.0;
            bool canPromote = pilot.Level >= reqLevel && pilot.TotalHours >= reqHours && pilot.Balance >= cost;
            Assert.True(canPromote);

            pilot.Balance -= cost;
            pilot.CareerPhase = nextPhase;

            Assert.Equal(CareerPhase.Phase2_SAE, pilot.CareerPhase);
            Assert.Equal(5000.0, pilot.Balance);
        }

        [Fact]
        public void Mapa_subtipo_resolve_fases_conforme_especificacao()
        {
            var catalog = CareerPhaseCatalog.Instance;

            // Fase 1: Escola
            Assert.Equal(CareerPhase.Phase1_FlightSchool, catalog.ResolvePhase("3.4.1"));
            Assert.Equal(CareerPhase.Phase1_FlightSchool, catalog.ResolvePhase("3.4.2"));
            Assert.Equal(CareerPhase.Phase1_FlightSchool, catalog.ResolvePhase("3.4.3"));

            // Fase 2: SAE
            Assert.Equal(CareerPhase.Phase2_SAE, catalog.ResolvePhase("4.1.1"));
            Assert.Equal(CareerPhase.Phase2_SAE, catalog.ResolvePhase("4.2.1"));
            Assert.Equal(CareerPhase.Phase2_SAE, catalog.ResolvePhase("3.2.3"));

            // Fase 3: Táxi Aéreo e Carga Leve
            Assert.Equal(CareerPhase.Phase3_AirTaxi, catalog.ResolvePhase("3.1.1"));
            Assert.Equal(CareerPhase.Phase3_AirTaxi, catalog.ResolvePhase("2.2.1"));
            Assert.Equal(CareerPhase.Phase3_AirTaxi, catalog.ResolvePhase("2.3.1"));
            Assert.Equal(CareerPhase.Phase3_AirTaxi, catalog.ResolvePhase("3.2.1"));
            Assert.Equal(CareerPhase.Phase3_AirTaxi, catalog.ResolvePhase("4.3.2"));

            // Fase 4: Executiva de Alta Performance e MEDEVAC
            Assert.Equal(CareerPhase.Phase4_HighPerformanceAndMedevac, catalog.ResolvePhase("3.3.1"));
            Assert.Equal(CareerPhase.Phase4_HighPerformanceAndMedevac, catalog.ResolvePhase("4.3.1"));
            Assert.Equal(CareerPhase.Phase3_AirTaxi, catalog.ResolvePhase("1.3.1")); // executivo_corporativo (Fase 3 conforme spec E2A)
            Assert.Equal(CareerPhase.Phase4_HighPerformanceAndMedevac, catalog.ResolvePhase("1.3.4"));

            // Fase 5: Regional Commuter
            Assert.Equal(CareerPhase.Phase5_Commuter, catalog.ResolvePhase("2.1.1")); // feeder_inbound

            // Fase 6: Linha Aérea Doméstica
            Assert.Equal(CareerPhase.Phase6_DomesticAirline, catalog.ResolvePhase("1.1.1")); // troncal_domestico
            Assert.Equal(CareerPhase.Phase6_DomesticAirline, catalog.ResolvePhase("2.1.2")); // regional_direto -> troncal_domestico
            Assert.Equal(CareerPhase.Phase6_DomesticAirline, catalog.ResolvePhase("1.2.1")); // carga_paletizada
            Assert.Equal(CareerPhase.Phase6_DomesticAirline, catalog.ResolvePhase("1.1.2")); // regional_internacional

            // Fase 7: Linha Aérea Internacional
            Assert.Equal(CareerPhase.Phase7_InternationalAirline, catalog.ResolvePhase("1.1.3")); // longo_curso

            // Outras fases migradas
            Assert.Equal(CareerPhase.Phase3_AirTaxi, catalog.ResolvePhase("1.3.2")); // crew_parts
            Assert.Equal(CareerPhase.Phase1_FlightSchool, catalog.ResolvePhase("1.3.3")); // ferry_posicionamento
        }

        [Fact]
        public void Missoes_de_fases_anteriores_permanecem_liberadas_e_superiores_bloqueadas()
        {
            var pilotPhase = CareerPhase.Phase3_AirTaxi;

            var mFase1 = new Mission { Title = "Instrução", RequiredPhase = CareerPhase.Phase1_FlightSchool };
            var mFase2 = new Mission { Title = "Logística SAE", RequiredPhase = CareerPhase.Phase2_SAE };
            var mFase3 = new Mission { Title = "Táxi Aéreo", RequiredPhase = CareerPhase.Phase3_AirTaxi };
            var mFase4 = new Mission { Title = "MEDEVAC", RequiredPhase = CareerPhase.Phase4_HighPerformanceAndMedevac };
            var mFase6 = new Mission { Title = "Linha Doméstica", RequiredPhase = CareerPhase.Phase6_DomesticAirline };

            // Fases anteriores e atual liberadas
            Assert.True(mFase1.RequiredPhase <= pilotPhase);
            Assert.True(mFase2.RequiredPhase <= pilotPhase);
            Assert.True(mFase3.RequiredPhase <= pilotPhase);

            // Fases superiores bloqueadas
            Assert.False(mFase4.RequiredPhase <= pilotPhase);
            Assert.False(mFase6.RequiredPhase <= pilotPhase);
        }

        [Fact]
        public void Apos_fase_7_niveis_continuam_subindo_sem_novas_fases()
        {
            var pilot = new Pilot
            {
                Id = Guid.NewGuid(),
                Name = "Comandante Veterano",
                CareerPhase = CareerPhase.Phase7_InternationalAirline,
                TotalHours = 3000.0,
                // L60: 300 * (60 - 1)^2 = 300 * 3481 = 1.044.300 XP
                ExperiencePoints = 1050000
            };

            Assert.Equal(60, pilot.Level);
            Assert.Equal(CareerPhase.Phase7_InternationalAirline, pilot.CareerPhase);

            // Não existe fase além da 7
            Assert.False(Enum.IsDefined(typeof(CareerPhase), (int)pilot.CareerPhase + 1));
            Assert.Equal("Fase 7: Internacional / Long Haul", pilot.CareerPhase.GetShortName());
        }

        [Fact]
        public async Task Migracao_banco_antigo_converte_categorias_1_a_5_para_as_fases_corretas()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotCat4Id = Guid.NewGuid();
            var missionCat5Id = Guid.NewGuid();

            // 1. Cria o banco legado com colunas antigas PilotCategory e RequiredCategory
            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                // Remove colunas novas para simular banco legado
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Pilots DROP COLUMN CareerPhase;");
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Missions DROP COLUMN RequiredPhase;");

                // Insere piloto categoria 4 (deve virar fase 5)
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Pilots (Id, Name, CompanyName, InitialBaseICAO, CurrentAirportICAO, Balance, Difficulty, TotalHours, ExperiencePoints, Rank, PilotCategory, HasPPLicense, HasCPLicense, HasIFRRating, HasMultiEngineRating, HasATPLicense, HasCFIInstructor, CareerClockUtc) " +
                    "VALUES ({0}, 'Piloto Cat 4', 'Regional Linhas', 'SBGR', 'SBGR', 100000, 0, 500, 150000, 'Commercial Pilot', 4, 1, 1, 1, 1, 0, 0, {1})",
                    pilotCat4Id, DateTime.UtcNow.ToString("O"));

                // Insere missão categoria 5 sem SubId (deve virar fase 6)
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Missions (Id, Title, Description, OriginICAO, DestinationICAO, PayloadLbs, PassengerCount, LuggageWeightLbs, CargoWeightLbs, Urgency, RewardMoney, ExperienceGain, DistanceNM, ExpirationTime, IsAccepted, RequiredCategory, MissionCategoryType, SubId) " +
                    "VALUES ({0}, 'Linha Antiga', 'Voo sem SubId', 'SBGR', 'SBRJ', 20000, 150, 4000, 1000, 1.0, 50000, 200, 200, '2026-12-31', 0, 5, 'Linha', NULL)",
                    missionCat5Id);
            }

            // 2. Executa a migração do schema
            await using (var db = new AppDbContext(options))
            {
                await db.EnsureSchemaUpdatedAsync();
                await db.EnsureSchemaUpdatedAsync(); // Testa idempotência

                var pilot = await db.Pilots.FirstOrDefaultAsync(p => p.Id == pilotCat4Id);
                Assert.NotNull(pilot);
                Assert.Equal(CareerPhase.Phase5_Commuter, pilot.CareerPhase);

                var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionCat5Id);
                Assert.NotNull(mission);
                Assert.Equal(CareerPhase.Phase6_DomesticAirline, mission.RequiredPhase);
            }
        }
    }
}
