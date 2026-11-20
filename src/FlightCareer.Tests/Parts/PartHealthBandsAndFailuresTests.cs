using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartHealthBandsAndFailuresTests
    {
        private sealed class FixedRandom : IRandomSource
        {
            private readonly double _val;
            public FixedRandom(double val) => _val = val;
            public double NextDouble() => _val;
        }

        private static readonly PartCatalog PartsCatalog = PartCatalog.LoadDefault();
        private static readonly ComponentCatalog CompCatalog = ComponentCatalog.LoadDefault();
        private static readonly WearRuleCatalog WearRules = WearRuleCatalog.LoadDefault();

        [Fact]
        public void Bands_Classify_Limites()
        {
            var s = new PartBandSettings(40.0, 20.0, 2.0);
            Assert.Equal(PartBand.Green, PartHealthBands.Classify(40.0, s));
            Assert.Equal(PartBand.Yellow, PartHealthBands.Classify(39.9, s));
            Assert.Equal(PartBand.Yellow, PartHealthBands.Classify(20.0, s));
            Assert.Equal(PartBand.Red, PartHealthBands.Classify(19.9, s));
            Assert.Equal(PartBand.Red, PartHealthBands.Classify(0.0, s));
        }

        [Fact]
        public void LowHealthWear_Curva()
        {
            var s = new PartBandSettings(40.0, 20.0, 2.0);
            Assert.Equal(1.0, PartHealthBands.LowHealthWearMultiplier(100.0, s), 6);
            Assert.Equal(1.0, PartHealthBands.LowHealthWearMultiplier(40.0, s), 6);
            Assert.Equal(1.25, PartHealthBands.LowHealthWearMultiplier(20.0, s), 6);
            Assert.Equal(2.0, PartHealthBands.LowHealthWearMultiplier(0.0, s), 6);
        }

        [Fact]
        public void PartWear_PecaAbaixoDe40_GastaMaisRapido()
        {
            var bands = new PartBandSettings(40.0, 20.0, 2.0);
            var engine = new PartWearEngine(PartsCatalog, WearRules, new FixedRandom(0.5), bands);

            var part80 = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 80.0
            };

            var part30 = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 30.0
            };

            var emptyFrame = new FlightCareer.Domain.Sensors.SensorFrame(new Dictionary<string, double>(), DateTime.UtcNow, 1.0);

            engine.ApplyTick(new[] { part80 }, emptyFrame, 1.0, Array.Empty<string>(), null, null);
            engine.ApplyTick(new[] { part30 }, emptyFrame, 1.0, Array.Empty<string>(), null, null);

            double wear80 = 80.0 - part80.Health;
            double wear30 = 30.0 - part30.Health;

            // Esperado: 1 + ((40 - 30) / 40)^2 = 1 + (0.25)^2 = 1.0625
            double ratio = wear30 / wear80;
            Assert.Equal(1.0625, ratio, 5);
        }

        [Fact]
        public void PartFailure_AcimaDe20_NuncaFalha()
        {
            var pneu = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "tires.main",
                Health = 20.1,
                IsBroken = false
            };

            var rngZero = new FixedRandom(0.0);
            var failureEngine = new PartFailureEngine(CompCatalog, PartsCatalog, EngineClass.Piston, rngZero);

            for (int i = 0; i < 10000; i++)
            {
                var failures = failureEngine.Evaluate(new[] { pneu }, 0.01, _ => true, Array.Empty<ComponentKind>());
                Assert.Empty(failures);
            }
        }

        [Fact]
        public void PartFailure_Em0_TaxaDe2PorHora()
        {
            var compDef = CompCatalog.Get(ComponentKind.Tires);
            double prob = FailureEngine.FailureProbability(0.0, 1.0, CompCatalog.Hazard, compDef.HazardScale);
            double expected = 1.0 - Math.Exp(-2.0 * compDef.HazardScale);
            Assert.Equal(expected, prob, 6);
        }

        [Fact]
        public void PartFailure_UmaPorComponentePorVoo()
        {
            var pneuEsq = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "tires.main",
                Health = 5.0,
                IsBroken = false
            };
            var pneuDir = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "tires.main",
                Health = 5.0,
                IsBroken = false
            };

            var rngFail = new FixedRandom(0.0);
            var engine = new PartFailureEngine(CompCatalog, PartsCatalog, EngineClass.Piston, rngFail);

            var failures = engine.Evaluate(new[] { pneuEsq, pneuDir }, 0.1, _ => true, Array.Empty<ComponentKind>());
            Assert.Single(failures);
            Assert.Equal(ComponentKind.Tires, failures[0].Kind);

            // Próximo tick no mesmo voo: não falha outro pneu
            var failures2 = engine.Evaluate(new[] { pneuEsq, pneuDir }, 0.1, _ => true, engine.FailedThisFlight);
            Assert.Empty(failures2);
        }

        [Fact]
        public void PartFailure_IgnoraMinorEQuebradas()
        {
            var lampadaQueimavel = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "lights.landing",
                Health = 5.0,
                IsBroken = false
            };
            var pecaQuebrada = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "tires.main",
                Health = 0.0,
                IsBroken = true
            };

            var rngFail = new FixedRandom(0.0);
            var engine = new PartFailureEngine(CompCatalog, PartsCatalog, EngineClass.Piston, rngFail);

            var failures = engine.Evaluate(new[] { lampadaQueimavel, pecaQuebrada }, 1.0, _ => true, Array.Empty<ComponentKind>());
            Assert.Empty(failures);
        }

        [Fact]
        public void PartFailure_UsaPecaNaoBarra()
        {
            var pneuRuim = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "tires.main",
                Health = 10.0,
                IsBroken = false
            };
            var pneuBom = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "tires.main",
                Health = 100.0,
                IsBroken = false
            };

            // Barra agregada do componente = 0.7 * 10 + 0.3 * 55 = 23.5% (acima do limiar de 20%)
            double aggregate = PartComponentBridge.AggregateHealth(new[] { pneuRuim, pneuBom }, PartsCatalog);
            Assert.True(aggregate > 20.0);

            var rngFail = new FixedRandom(0.0);
            var engine = new PartFailureEngine(CompCatalog, PartsCatalog, EngineClass.Piston, rngFail);

            var failures = engine.Evaluate(new[] { pneuRuim, pneuBom }, 0.5, _ => true, Array.Empty<ComponentKind>());
            Assert.Single(failures);
            Assert.Equal(pneuRuim.Id, failures[0].Part.Id);
            Assert.Equal(10.0, failures[0].HealthAtFailure);
        }

        [Fact]
        public void Planner_NaoTrocaVerdeNemAmarelaSozinho()
        {
            var p70 = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 70.0 };
            var p35 = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 35.0 };
            var p15 = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 15.0 };
            var pQuebrada = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 50.0, IsBroken = true };

            var recommended = PartReplacementPlanner.SelectRecommended(
                new[] { p70, p35, p15, pQuebrada },
                PartsCatalog,
                new PartBandSettings(40.0, 20.0, 2.0));

            Assert.Equal(2, recommended.Count);
            Assert.Contains(p15, recommended);
            Assert.Contains(pQuebrada, recommended);
        }

        [Fact]
        public void Planner_Plan_PrecificaSoAsSelecionadas()
        {
            var p35 = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 35.0 };
            var p15 = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 15.0 };

            var plan = PartReplacementPlanner.Plan(new[] { p35, p15 }, PartsCatalog, "A", DateTime.UtcNow);

            Assert.Equal(2, plan.Items.Count);
            Assert.Contains("Faixa amarela: 35 %", plan.Items[0].Reason);
            Assert.Contains("Faixa vermelha: 15 %", plan.Items[1].Reason);
            Assert.True(plan.TotalCost > 0);
        }

        [Fact]
        public void Flags_SoPelaSaude()
        {
            var def = PartsCatalog.Get("oil.lubricant")!;
            var p50 = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "oil.lubricant",
                Health = 50.0,
                HoursSinceReplacement = def.LifeHours + 10.0 // Vida vencida
            };
            var p30 = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "oil.lubricant",
                Health = 30.0
            };
            var p10 = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "oil.lubricant",
                Health = 10.0
            };

            var eval50 = PartInspectionFlags.Evaluate(p50, def, DateTime.UtcNow);
            Assert.Equal(PartFlag.None, eval50.Level);
            string? lifeNote = PartInspectionFlags.LifeNote(p50, def, DateTime.UtcNow);
            Assert.NotNull(lifeNote);
            Assert.Contains("Vida vencida", lifeNote);

            var eval30 = PartInspectionFlags.Evaluate(p30, def, DateTime.UtcNow);
            Assert.Equal(PartFlag.Suspect, eval30.Level);
            Assert.Contains("Faixa amarela", eval30.Reason);

            var eval10 = PartInspectionFlags.Evaluate(p10, def, DateTime.UtcNow);
            Assert.Equal(PartFlag.Broken, eval10.Level);
            Assert.Contains("Faixa vermelha", eval10.Reason);
        }

        [Fact]
        public void AnnualInspection_NaoTrocaOleo_MarcaAchados()
        {
            var pVerde = new AircraftPart { Id = Guid.NewGuid(), PartId = "oil.lubricant", Health = 80.0 };
            var pAmarela = new AircraftPart { Id = Guid.NewGuid(), PartId = "brakes.pads_discs", Health = 35.0 };
            var pVermelha = new AircraftPart { Id = Guid.NewGuid(), PartId = "tires.main", Health = 15.0 };
            var pQuebrada = new AircraftPart { Id = Guid.NewGuid(), PartId = "engine.cylinders", Health = 50.0, IsBroken = true };

            var all = new[] { pVerde, pAmarela, pVermelha, pQuebrada };
            var bands = new PartBandSettings(40.0, 20.0, 2.0);

            // Anual revela todas as peças
            PartKnowledge.Reveal(all, _ => true, PartsCatalog, airframeHours: 250.0);
            foreach (var p in all)
            {
                Assert.Equal(p.Health, p.KnownHealth);
                Assert.Equal(250.0, p.KnownAtAirframeHours);
            }

            // Marca achados de defeito na anual
            PartKnowledge.MarkAnnualFindings(all, PartsCatalog, bands, airframeHours: 250.0);
            Assert.Null(pVerde.FoundRedAtAnnualAirframeHours);
            Assert.Null(pAmarela.FoundRedAtAnnualAirframeHours);
            Assert.Equal(250.0, pVermelha.FoundRedAtAnnualAirframeHours);
            Assert.Equal(250.0, pQuebrada.FoundRedAtAnnualAirframeHours);

            // Troca da peça vermelha limpa a marca e restaura KnownHealth a 100%
            PartReplacementPlanner.ApplyToPart(pVermelha, airframeHours: 251.0, careerNow: DateTime.UtcNow);
            Assert.Null(pVermelha.FoundRedAtAnnualAirframeHours);
            Assert.Equal(100.0, pVermelha.KnownHealth);
            Assert.Equal(251.0, pVermelha.KnownAtAirframeHours);
            Assert.Equal(100.0, pVermelha.Health);
        }

        [Fact]
        public void Insurance_Premio_2v5PorCentoAoAno()
        {
            // 120_000 * (0.025 / 12) = 250.00
            double premiumExpensive = InsuranceService.CalculateMonthlyPremium(120_000.0);
            Assert.Equal(250.0, premiumExpensive, 2);

            // 12_000 * (0.025 / 12) = 25.00 -> clamp no piso de 50.00
            double premiumCheap = InsuranceService.CalculateMonthlyPremium(12_000.0);
            Assert.Equal(50.0, premiumCheap, 2);
        }

        [Fact]
        public void Insurance_ClaimNegado_ComAchadoDaAnualNaoCorrigido()
        {
            var plane = new PilotAircraft
            {
                CurrentAirframeHours = 200.0,
                Parts = new List<AircraftPart>
                {
                    new AircraftPart { PartId = "engine.cylinders", Health = 15.0, FoundRedAtAnnualAirframeHours = 190.0 }
                }
            };

            // Peça com achado de defeito na anual não corrigida nega a cobertura
            Assert.True(InsuranceService.ClaimDenied(plane));

            // Se a peça for substituída / reparada, FoundRedAtAnnualAirframeHours vira null
            plane.Parts[0].FoundRedAtAnnualAirframeHours = null;
            Assert.False(InsuranceService.ClaimDenied(plane));
        }

        [Fact]
        public async Task FlightService_TboVencido_NaoInterdita()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var c172 = new AircraftType { Id = Guid.NewGuid(), Model = "C172", Type = "Geral", FuelType = "Avgas", PurchasePrice = 100000 };
                db.AircraftTypes.Add(c172);
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Piloto", Balance = 50000, CurrentAirportICAO = "SBSP" });
                db.PilotAircrafts.Add(new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = c172.Id,
                    Prefix = "PT-TEST",
                    EngineTboRemainingHours = 0.5, // vai zerar com voo de 1h
                    HoursUntilNext100hInspection = 50.0, // ainda tem 49h
                    HoursUntilNext50hInspection = 50.0,
                    CurrentLocationICAO = "SBSP",
                    IsInterdicted = false,
                    CurrentAirframeHours = 1000.0
                });
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var service = new FlightService(db, null!, null!);
                await service.ProcessFlightFinishAsync(pilotId, aircraftId, "SBSP", "SBRJ", DateTime.Now, 30, 20, 100, new List<Mission>(), new FlightQualityService(), flightDurationMinutes: 60);
            }

            await using (var db = new AppDbContext(options))
            {
                var aircraft = await db.PilotAircrafts.FindAsync(aircraftId);
                Assert.NotNull(aircraft);
                Assert.Equal(0.0, aircraft.EngineTboRemainingHours);
                Assert.False(aircraft.IsInterdicted, "TBO vencido não interdita a aeronave no FlightService");
            }
        }

        [Fact]
        public async Task FlightService_AnualVencida_Interdita()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            var pilotId = Guid.NewGuid();
            var aircraftId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var c172 = new AircraftType { Id = Guid.NewGuid(), Model = "C172", Type = "Geral", FuelType = "Avgas", PurchasePrice = 100000 };
                db.AircraftTypes.Add(c172);
                db.Pilots.Add(new Pilot { Id = pilotId, Name = "Piloto", Balance = 50000, CurrentAirportICAO = "SBSP" });
                db.PilotAircrafts.Add(new PilotAircraft
                {
                    Id = aircraftId,
                    PilotId = pilotId,
                    AircraftTypeId = c172.Id,
                    Prefix = "PT-TEST",
                    EngineTboRemainingHours = 500.0,
                    HoursUntilNext100hInspection = 0.5, // vai zerar com voo de 1h
                    HoursUntilNext50hInspection = 50.0,
                    CurrentLocationICAO = "SBSP",
                    IsInterdicted = false,
                    CurrentAirframeHours = 1000.0
                });
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var service = new FlightService(db, null!, null!);
                await service.ProcessFlightFinishAsync(pilotId, aircraftId, "SBSP", "SBRJ", DateTime.Now, 30, 20, 100, new List<Mission>(), new FlightQualityService(), flightDurationMinutes: 60);
            }

            await using (var db = new AppDbContext(options))
            {
                var aircraft = await db.PilotAircrafts.FindAsync(aircraftId);
                Assert.NotNull(aircraft);
                Assert.Equal(0.0, aircraft.HoursUntilNext100hInspection);
                Assert.True(aircraft.IsInterdicted, "Inspeção Anual vencida interdita a aeronave no FlightService");
                Assert.Contains("Anual", aircraft.InterdictionReason);
            }
        }
    }
}
