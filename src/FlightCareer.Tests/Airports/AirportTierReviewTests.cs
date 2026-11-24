using System;
using System.Collections.Generic;
using FlightCareer.Domain.Airports;
using Xunit;

namespace FlightCareer.Tests.Airports
{
    /// <summary>Ajustes da revisão TIER-A: teto da estrutura, corte do tier 1 e base militar.</summary>
    public class AirportTierReviewTests
    {
        private static readonly AirportTierRules Rules = AirportTierRules.CreateDefault();
        private static IReadOnlySet<string> Tags(params string[] t) => new HashSet<string>(t, StringComparer.OrdinalIgnoreCase);

        // Base aérea com muito ILS, procedimentos e pista longa, sem etiqueta: antes virava tier 1
        private static AirportTierInput BaseAerea(bool militar, params string[] tags) => new(
            Tags(tags), IlsEnds: 4, Approaches: 30, Gates: 40, Jetways: 0, CargoParking: 0, HasTower: true,
            LongestRunwayFt: 12000, LongestRunwayPaved: true, CsvType: "large_airport", ScheduledService: false, IsMilitary: militar);

        [Fact]
        public void Estrutura_TemTeto()
        {
            var s = AirportTierClassifier.Score(BaseAerea(false), Rules);
            Assert.Equal(Rules.Structure.MaxTotal, s.StructureScore);
        }

        [Fact]
        public void BaseMilitarSemLinha_NoMaximoTier2()
        {
            Assert.Equal(2, AirportTierClassifier.Classify(BaseAerea(true, "militar", "militar:caca"), Rules));
        }

        [Fact]
        public void BaseMilitarComLinha_PodeSerTier1()
        {
            Assert.Equal(1, AirportTierClassifier.Classify(BaseAerea(true, "linha:hub"), Rules));
        }

        [Fact]
        public void MegaHub_SoComAEtiquetaEPista_Tier1()
        {
            var input = new AirportTierInput(Tags("linha:mega_hub"), 0, 0, 0, 0, 0, false, 12000, true, null, null);
            Assert.Equal(1, AirportTierClassifier.Classify(input, Rules));
        }
    }
}
