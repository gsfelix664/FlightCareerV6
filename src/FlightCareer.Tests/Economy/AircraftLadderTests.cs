using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Simulation;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    /// <summary>
    /// A escada dos bots não pode depender da ordem do banco (chave Guid): antes o degrau H às vezes caía
    /// no Citation Longitude ($28 mi) em vez do CJ4 e o Médio ficava parado na classe G.
    /// </summary>
    public class AircraftLadderTests
    {
        private static readonly string[] Models =
        {
            "Cessna Citation Longitude", "Cessna Citation CJ4", "Beechcraft King Air", "Beechcraft King Air C90GTx",
            "Beechcraft King Air 250", "C208B", "Asobo TBM 930", "Beechcraft Baron G58", "Diamond DA62",
            "C172SP G1000", "Cessna C152", "A320neo", "737 Max 8", "A330-200", "Airbus A330-300", "Boeing 747-400"
        };

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(42)]
        public void Escada_escolhe_os_mesmos_modelos_em_qualquer_ordem(int seed)
        {
            var rnd = new Random(seed);
            var types = Models.Select(m => new AircraftType { Id = Guid.NewGuid(), Model = m })
                .OrderBy(_ => rnd.Next()).ToList();

            var ladder = CareerProgressionSimulator.BuildAircraftLadder(types).Select(s => s.Type.Model).ToList();

            Assert.Equal(new List<string>
            {
                "Cessna C152", "C172SP G1000", "Beechcraft Baron G58", "C208B",
                "Beechcraft King Air 250", "Cessna Citation CJ4", "A320neo", "A330-200"
            }, ladder);
        }
    }
}
