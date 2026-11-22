using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Services;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    public class DifficultyEconomyTests
    {
        [Fact]
        public void DifficultyEconomy_ValoresAprovados()
        {
            var easy = DifficultyEconomy.For(DifficultyMode.Easy);
            Assert.Equal(1.25, easy.PayMultiplier);
            Assert.Equal(0.80, easy.CostMultiplier);
            Assert.Equal(1.00, easy.FailureMultiplier);
            Assert.Equal("Fácil", DifficultyEconomy.Label(DifficultyMode.Easy));

            var medium = DifficultyEconomy.For(DifficultyMode.Medium);
            Assert.Equal(1.00, medium.PayMultiplier);
            Assert.Equal(1.00, medium.CostMultiplier);
            Assert.Equal(1.00, medium.FailureMultiplier);
            Assert.Equal("Médio", DifficultyEconomy.Label(DifficultyMode.Medium));

            var hard = DifficultyEconomy.For(DifficultyMode.Hard);
            Assert.Equal(0.85, hard.PayMultiplier);
            Assert.Equal(1.15, hard.CostMultiplier);
            Assert.Equal(1.25, hard.FailureMultiplier);
            Assert.Equal("Difícil", DifficultyEconomy.Label(DifficultyMode.Hard));

            var unknown = DifficultyEconomy.For((DifficultyMode)999);
            Assert.Equal(1.00, unknown.PayMultiplier);
            Assert.Equal(1.00, unknown.CostMultiplier);
            Assert.Equal(1.00, unknown.FailureMultiplier);
            Assert.Equal("Médio", DifficultyEconomy.Label((DifficultyMode)999));
        }

        [Fact]
        public void CareerStart_EasyUmMilhao()
        {
            Assert.Equal(1_000_000.0, CareerStart.EasyInitialBalance);
            Assert.Equal(1_000_000.0, CareerStart.GetInitialBalance(DifficultyMode.Easy));
        }

        [Fact]
        public void CalculateReward_TabelaPadrao_IgualAoCodigoAntigo()
        {
            var testSubIds = new[]
            {
                "3.4.1", "3.4.2", "4.3.1", "3.3.1", "4.3.2", "3.2.1",
                "4.1.1", "3.2.2", "3.1.1", "2.2.1", "1.3.1", "2.3.1",
                "2.1.1", "1.1.1", "1.2.1", "9.9.9"
            };

            foreach (var subId in testSubIds)
            {
                foreach (int pax in new[] { 0, 6 })
                {
                    double oldVal = CalculateRewardOld(120, 500, 1.3, subId, pax, 1, 4, new Random(42));
                    // Tabela embutida (escala 1,0): a calibração de 2026-11-22 muda só as escalas do JSON
                    double newVal = MissionGenerator.CalculateReward(120, 500, 1.3, subId, pax, 1, 4, new Random(42), 1.0, MissionPayTable.Defaults);
                    Assert.Equal(oldVal, newVal);
                }
            }
        }

        [Fact]
        public void CalculateReward_PayMultiplier_Proporcional()
        {
            double valNormal = MissionGenerator.CalculateReward(120, 500, 1.3, "2.1.1", 4, 1, 3, new Random(42), 1.0);
            double valHard = MissionGenerator.CalculateReward(120, 500, 1.3, "2.1.1", 4, 1, 3, new Random(42), 0.85);

            Assert.Equal(valNormal * 0.85, valHard, 2);
        }

        [Fact]
        public void CalculateReward_EscalaDaFamilia()
        {
            var defaults = MissionPayTable.Defaults;
            var customizedFamilies = defaults.Families.Select(f =>
                f.Id == "linha"
                    ? new MissionPayFamily(f.Id, f.Prefixes, f.BaseFee, f.RatePerMile, f.RatePerPaxMile, f.PaxAtLeastOne, f.RatePerPayloadLb, 0.5)
                    : f
            ).ToList();

            var customTable = new MissionPayTable(customizedFamilies);

            // Linha regular (1.1.1): scale 0.5 -> metade do valor nominal
            double valNominalLinha = MissionGenerator.CalculateReward(200, 1000, 1.0, "1.1.1", 10, 1, 1, new Random(42), payTable: defaults);
            double valCustomLinha = MissionGenerator.CalculateReward(200, 1000, 1.0, "1.1.1", 10, 1, 1, new Random(42), payTable: customTable);
            Assert.Equal(valNominalLinha * 0.5, valCustomLinha, 2);

            // Táxi aéreo (3.1.1): scale 1.0 -> mesmo valor
            double valNominalTaxi = MissionGenerator.CalculateReward(120, 500, 1.0, "3.1.1", 3, 2, 3, new Random(42), payTable: defaults);
            double valCustomTaxi = MissionGenerator.CalculateReward(120, 500, 1.0, "3.1.1", 3, 2, 3, new Random(42), payTable: customTable);
            Assert.Equal(valNominalTaxi, valCustomTaxi, 2);
        }

        [Fact]
        public void MissionPayTable_JsonIgualAosPadroes()
        {
            var loaded = MissionPayTable.LoadDefault();
            var defaults = MissionPayTable.Defaults;

            Assert.Equal(defaults.Families.Count, loaded.Families.Count);

            for (int i = 0; i < defaults.Families.Count; i++)
            {
                var def = defaults.Families[i];
                var load = loaded.Families[i];

                Assert.Equal(def.Id, load.Id);
                Assert.Equal(def.Prefixes, load.Prefixes);
                Assert.Equal(def.BaseFee, load.BaseFee);
                Assert.Equal(def.RatePerMile, load.RatePerMile);
                Assert.Equal(def.RatePerPaxMile, load.RatePerPaxMile);
                Assert.Equal(def.PaxAtLeastOne, load.PaxAtLeastOne);
                Assert.Equal(def.RatePerPayloadLb, load.RatePerPayloadLb);
                // Escala calibrada no JSON (economia-estabilidade.md › 4.4): só precisa ser positiva
                Assert.True(load.Scale > 0);
            }
        }

        [Fact]
        public void MissionPayTable_ResolveOrdem()
        {
            var table = MissionPayTable.LoadDefault();

            Assert.Equal("medevac", table.Resolve("4.3.1").Id);
            Assert.Equal("aog", table.Resolve("4.3.2").Id);
            Assert.Equal("agro_bush", table.Resolve("4.1.1").Id);
            Assert.Equal("aog", table.Resolve("3.2.1").Id);
            Assert.Equal("agro_municipal", table.Resolve("3.2.2").Id);
            Assert.Equal("instrucao_basica", table.Resolve("3.4.1").Id);
            Assert.Equal("instrucao", table.Resolve("3.4.2").Id);
            Assert.Equal("fallback", table.Resolve("9.9").Id);
        }

        [Fact]
        public void PartFailureEngine_HazardMultiplier_DobraLambda()
        {
            var compCatalog = ComponentCatalog.LoadDefault();
            var partCatalog = PartCatalog.LoadDefault();
            var mapping = PartComponentMapping.CreateDefault();

            var engine1 = new PartFailureEngine(compCatalog, partCatalog, EngineClass.Piston, new SystemRandomSource(42), mapping)
            {
                HazardMultiplier = 1.0
            };

            var engine2 = new PartFailureEngine(compCatalog, partCatalog, EngineClass.Piston, new SystemRandomSource(42), mapping)
            {
                HazardMultiplier = 2.0
            };

            // Peça do motor desgastada (10% de saúde, bem abaixo do threshold 20%)
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 10.0
            };

            double dt = 0.5; // meia hora
            double p1 = engine1.CalculateProbability(part, dt);
            double p2 = engine2.CalculateProbability(part, dt);

            Assert.True(p1 > 0.0);
            Assert.True(p2 > 0.0);

            // p1 = 1 - e^(-lambda*dt) => 1 - p1 = e^(-lambda*dt)
            // p2 = 1 - e^(-2*lambda*dt) = 1 - (e^(-lambda*dt))^2 = 1 - (1 - p1)^2
            double expectedP2 = 1.0 - Math.Pow(1.0 - p1, 2.0);
            Assert.Equal(expectedP2, p2, 6);
        }

        private static double CalculateRewardOld(
            double distance,
            double payload,
            double urgency,
            string subId,
            int paxCount,
            int originTier,
            int destTier,
            Random random)
        {
            double baseFee;
            double ratePerMile;
            double ratePerPayloadLb;

            if (subId.StartsWith("3.4"))
            {
                baseFee = subId == "3.4.1" ? 220.0 : 350.0;
                ratePerMile = 6.5;
                ratePerPayloadLb = 0.10;
            }
            else if (subId.StartsWith("3.3") || subId == "4.3.1")
            {
                baseFee = 2400.0;
                ratePerMile = 14.0;
                ratePerPayloadLb = 0.25;
            }
            else if (subId == "4.3.2" || subId == "3.2.1")
            {
                baseFee = 950.0;
                ratePerMile = 8.0;
                ratePerPayloadLb = 0.35;
            }
            else if (subId.StartsWith("4."))
            {
                baseFee = 380.0;
                ratePerMile = 4.5;
                ratePerPayloadLb = 0.18;
            }
            else if (subId.StartsWith("3.2"))
            {
                baseFee = 450.0;
                ratePerMile = 5.0;
                ratePerPayloadLb = 0.15;
            }
            else if (subId.StartsWith("3.1") || subId.StartsWith("2.2") || subId.StartsWith("1.3"))
            {
                baseFee = 950.0;
                ratePerMile = 7.5 + (Math.Max(1, paxCount) * 1.5);
                ratePerPayloadLb = 0.12;
            }
            else if (subId.StartsWith("2.3"))
            {
                baseFee = 500.0;
                ratePerMile = 4.0;
                ratePerPayloadLb = 0.22;
            }
            else if (subId.StartsWith("2.1"))
            {
                baseFee = 1800.0;
                ratePerMile = 6.0 + (paxCount * 0.45);
                ratePerPayloadLb = 0.08;
            }
            else if (subId.StartsWith("1.1") || subId.StartsWith("1.2"))
            {
                baseFee = 5500.0;
                ratePerMile = 8.0 + (paxCount * 0.30);
                ratePerPayloadLb = 0.05;
            }
            else
            {
                baseFee = 400.0;
                ratePerMile = 5.5;
                ratePerPayloadLb = 0.10;
            }

            double urgencyMultiplier = Math.Max(1.0, urgency);
            double grossBase = (baseFee + (distance * ratePerMile) + (payload * ratePerPayloadLb)) * urgencyMultiplier;

            double originFactor = originTier switch
            {
                1 => 1.20,
                2 => 1.08,
                3 => 1.00,
                4 => 0.95,
                _ => 1.00
            };

            double bushBonus = (destTier == 4) ? 1.25 : 1.00;
            double marketSpread = 0.88 + ((random ?? new Random()).NextDouble() * 0.26);
            double finalReward = grossBase * originFactor * bushBonus * marketSpread;

            return Math.Round(finalReward, 2);
        }

        [Fact]
        public void StableHash_ValorFixoEntreExecucoes()
        {
            // FNV-1a 32 bits de "SBSP" (valor fixo: string.GetHashCode muda a cada processo)
            uint expected = 2166136261;
            foreach (char c in "SBSP") { expected ^= c; expected *= 16777619; }
            Assert.Equal(unchecked((int)expected), FlightCareer.Application.Simulation.CareerProgressionSimulator.StableHash("SBSP"));
            Assert.NotEqual(FlightCareer.Application.Simulation.CareerProgressionSimulator.StableHash("SBSP"),
                FlightCareer.Application.Simulation.CareerProgressionSimulator.StableHash("SBGR"));
        }

        [Fact]
        public void DeriveBotSeed_NaoDependeDaCaixaDaPolitica()
        {
            int a = FlightCareer.Application.Simulation.CareerProgressionSimulator.DeriveBotSeed(1, DifficultyMode.Medium, "Cauteloso", 2);
            int b = FlightCareer.Application.Simulation.CareerProgressionSimulator.DeriveBotSeed(1, DifficultyMode.Medium, "cauteloso", 2);
            Assert.Equal(a, b);
        }

        [Fact]
        public void MissionPayTable_Shared_MesmaInstancia()
        {
            Assert.Same(MissionPayTable.Shared, MissionPayTable.Shared);
        }
    }
}
