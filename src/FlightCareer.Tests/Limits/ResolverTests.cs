using System.Collections.Generic;
using FlightCareer.Domain.Limits;
using Xunit;

namespace FlightCareer.Tests.Limits
{
    public class ResolverTests
    {
        [Fact]
        public void Resolver_PicksHighestPrioritySource()
        {
            var profile = new AircraftLimitProfile(
                "Cessna C152",
                "Piston",
                1,
                new[] { "Cessna C152" },
                new[] { "C152" },
                new[]
                {
                    new AircraftLimit("rpmRedline", 2600, "rpm", LimitSource.SimVar, "SimVar capture"),
                    new AircraftLimit("rpmRedline", 2550, "rpm", LimitSource.POH, "POH Cessna 152"),
                    new AircraftLimit("rpmRedline", 2700, "rpm", LimitSource.ClassDefault, "Default")
                }
            );

            var resolver = new AircraftLimitResolver(new[] { profile }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            var limit = resolver.Get(profile, "Piston", "rpmRedline");
            Assert.NotNull(limit);
            Assert.Equal(2550, limit.Value);
            Assert.Equal(LimitSource.POH, limit.Source);
        }

        [Fact]
        public void Resolver_FallsBackToClassDefault()
        {
            var profile = new AircraftLimitProfile(
                "Cessna C152",
                "Piston",
                1,
                new[] { "Cessna C152" },
                new[] { "C152" },
                new[]
                {
                    new AircraftLimit("vne", 149, "knots", LimitSource.SimVar, "SimVar")
                }
            );

            var classDefaults = new Dictionary<string, IReadOnlyList<AircraftLimit>>
            {
                ["Piston"] = new[]
                {
                    new AircraftLimit("chtMax", 260, "celsius", LimitSource.ClassDefault, "Class default piston")
                }
            };

            var resolver = new AircraftLimitResolver(new[] { profile }, classDefaults);

            var limit = resolver.Get(profile, "Piston", "chtMax");
            Assert.NotNull(limit);
            Assert.Equal(260, limit.Value);
            Assert.Equal("celsius", limit.Unit);
            Assert.Equal(LimitSource.ClassDefault, limit.Source);
        }

        [Fact]
        public void Resolver_UnknownAircraft_UsesClass()
        {
            var classDefaults = new Dictionary<string, IReadOnlyList<AircraftLimit>>
            {
                ["Piston"] = new[]
                {
                    new AircraftLimit("chtMax", 260, "celsius", LimitSource.ClassDefault, "Class default piston")
                }
            };

            var resolver = new AircraftLimitResolver(new List<AircraftLimitProfile>(), classDefaults);

            var profile = resolver.FindProfile("Avião X", null);
            Assert.Null(profile);

            var limit = resolver.Get(null, "Piston", "chtMax");
            Assert.NotNull(limit);
            Assert.Equal(260, limit.Value);
            Assert.Equal(LimitSource.ClassDefault, limit.Source);
        }

        [Fact]
        public void Resolver_MatchesByTitleThenAtcModel()
        {
            var profile1 = new AircraftLimitProfile(
                "Cessna C152",
                "Piston",
                1,
                new[] { "Cessna 152", "C152 Asobo" },
                new[] { "C152" },
                new List<AircraftLimit>()
            );

            var profile2 = new AircraftLimitProfile(
                "Diamond DA40NG",
                "Piston",
                1,
                new[] { "DA40-NG" },
                new[] { "DA40" },
                new List<AircraftLimit>()
            );

            var resolver = new AircraftLimitResolver(new[] { profile1, profile2 }, new Dictionary<string, IReadOnlyList<AircraftLimit>>());

            // Match por título (case-insensitive)
            var match1 = resolver.FindProfile("microsoft cessna 152 aerobat", null);
            Assert.NotNull(match1);
            Assert.Equal("Cessna C152", match1.AircraftId);

            // Match por ATC Model quando o título não casa
            var match2 = resolver.FindProfile("Aeronave sem titulo claro", "ATCCOM.AC_MODEL DA40.0.text");
            Assert.NotNull(match2);
            Assert.Equal("Diamond DA40NG", match2.AircraftId);

            // Se nenhum casar, retorna null
            var noMatch = resolver.FindProfile("Desconhecido", "INEXISTENTE");
            Assert.Null(noMatch);
        }
    }
}
