using System;
using System.Collections.Generic;
using FlightCareer.Domain.Airports;
using Xunit;

namespace FlightCareer.Tests.Airports
{
    public class AirportTierClassifierTests
    {
        private readonly AirportTierRules _rules = AirportTierRules.Default;

        [Fact]
        public void AirportTierClassifier_MegaHub_Tier1()
        {
            var input = new AirportTierInput(
                Tags: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "linha:mega_hub" },
                IlsEnds: 0,
                Approaches: 0,
                Gates: 0,
                Jetways: 0,
                CargoParking: 0,
                HasTower: false,
                LongestRunwayFt: 12000,
                LongestRunwayPaved: true,
                CsvType: null,
                ScheduledService: null
            );

            var score = AirportTierClassifier.Score(input, _rules);
            Assert.Equal(1, score.Tier);
            Assert.True(score.TotalScore >= 90);
        }

        [Fact]
        public void AirportTierClassifier_PonteAereaPistaCurta_Tier1()
        {
            // SBRJ: Santos Dumont (linha:ponte_aerea, linha:hub, ILS 1, 36 procedimentos, 8 gates, 8 pontes, torre, 4.338 ft pavimentada)
            var input = new AirportTierInput(
                Tags: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "linha:ponte_aerea", "linha:hub" },
                IlsEnds: 1,
                Approaches: 36,
                Gates: 8,
                Jetways: 8,
                CargoParking: 0,
                HasTower: true,
                LongestRunwayFt: 4338,
                LongestRunwayPaved: true,
                CsvType: "medium_airport",
                ScheduledService: true
            );

            var score = AirportTierClassifier.Score(input, _rules);
            Assert.Equal(1, score.Tier);
            Assert.True(score.TotalScore >= 90);
        }

        [Fact]
        public void AirportTierClassifier_AeroportoExecutivo_Tier2()
        {
            // SBMT: Campo de Marte (executivo, instrucao, saude, mro; 0 ILS, 0 gates, 32 vagas GA, torre, 5.120 ft pavimentada, sem linha)
            var input = new AirportTierInput(
                Tags: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "executivo", "instrucao", "saude", "mro" },
                IlsEnds: 0,
                Approaches: 0,
                Gates: 0,
                Jetways: 0,
                CargoParking: 0,
                HasTower: true,
                LongestRunwayFt: 5120,
                LongestRunwayPaved: true,
                CsvType: "medium_airport",
                ScheduledService: false
            );

            var score = AirportTierClassifier.Score(input, _rules);
            Assert.Equal(2, score.Tier);
            Assert.InRange(score.TotalScore, 45, 89);
        }

        [Fact]
        public void AirportTierClassifier_Fazenda_Tier4()
        {
            // Sem etiquetas, 3.896 ft de terra, sem torre
            var input = new AirportTierInput(
                Tags: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                IlsEnds: 0,
                Approaches: 0,
                Gates: 0,
                Jetways: 0,
                CargoParking: 0,
                HasTower: false,
                LongestRunwayFt: 3896,
                LongestRunwayPaved: false,
                CsvType: "small_airport",
                ScheduledService: false
            );

            var score = AirportTierClassifier.Score(input, _rules);
            Assert.Equal(4, score.Tier);
            Assert.True(score.TotalScore < 45);
        }

        [Fact]
        public void AirportTierClassifier_SemPavimento_NoMaximoTier3()
        {
            // Etiquetas fortes mas pista de terra
            var input = new AirportTierInput(
                Tags: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "linha:mega_hub" },
                IlsEnds: 4,
                Approaches: 20,
                Gates: 40,
                Jetways: 10,
                CargoParking: 10,
                HasTower: true,
                LongestRunwayFt: 12000,
                LongestRunwayPaved: false,
                CsvType: "large_airport",
                ScheduledService: true
            );

            var score = AirportTierClassifier.Score(input, _rules);
            Assert.True(score.Tier <= 4 && score.Tier >= 3, $"Esperava Tier 3 para pista não pavimentada com score alto, obteve {score.Tier}");
            Assert.Equal(3, score.Tier);
        }

        [Fact]
        public void AirportTierClassifier_IgnoraEtiquetasHeuristicas()
        {
            var baseTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "executivo" };
            var inputBase = new AirportTierInput(
                Tags: baseTags,
                IlsEnds: 0,
                Approaches: 4,
                Gates: 0,
                Jetways: 0,
                CargoParking: 0,
                HasTower: true,
                LongestRunwayFt: 4000,
                LongestRunwayPaved: true,
                CsvType: null,
                ScheduledService: null
            );

            var tagsComHeuristicas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "executivo", "isolado", "interior_distante", "fazenda", "altitude", "turistico_poi"
            };
            var inputHeuristico = inputBase with { Tags = tagsComHeuristicas };

            var scoreBase = AirportTierClassifier.Score(inputBase, _rules);
            var scoreHeuristico = AirportTierClassifier.Score(inputHeuristico, _rules);

            Assert.Equal(scoreBase.TotalScore, scoreHeuristico.TotalScore);
            Assert.Equal(scoreBase.TagScore, scoreHeuristico.TagScore);
            Assert.Equal(scoreBase.Tier, scoreHeuristico.Tier);
        }
    }
}
