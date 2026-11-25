using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class AgroSeasonTests
    {
        [Fact]
        public void SouthernEquivalentMonth_SouthernLatitude_Unchanged()
        {
            int month = AgroSeason.SouthernEquivalentMonth(10, -15.0);
            Assert.Equal(10, month);
        }

        [Fact]
        public void SouthernEquivalentMonth_NorthernLatitude_ShiftsSixMonths()
        {
            int month4 = AgroSeason.SouthernEquivalentMonth(4, 40.0);
            Assert.Equal(10, month4);

            int month12 = AgroSeason.SouthernEquivalentMonth(12, 0.0);
            Assert.Equal(6, month12);
        }

        [Fact]
        public void PhaseFor_Boundaries()
        {
            Assert.Equal(SafraPhase.Plantio, AgroSeason.PhaseFor(9));
            Assert.Equal(SafraPhase.Plantio, AgroSeason.PhaseFor(12));
            Assert.Equal(SafraPhase.Colheita, AgroSeason.PhaseFor(1));
            Assert.Equal(SafraPhase.Colheita, AgroSeason.PhaseFor(4));
            Assert.Equal(SafraPhase.Entressafra, AgroSeason.PhaseFor(5));
            Assert.Equal(SafraPhase.Entressafra, AgroSeason.PhaseFor(8));
        }

        [Fact]
        public void TagsFor_October_South_ReturnsPlantioAndMonth()
        {
            var date = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);
            var tags = AgroSeason.TagsFor(date, -22.0);

            Assert.Contains("safra:plantio", tags);
            Assert.Contains("safra:mes:10", tags);
            Assert.Equal(2, tags.Count);
        }

        [Fact]
        public void WithSeasonTags_NullDate_ReturnsSameInstance()
        {
            var original = new HashSet<string> { "agro", "fazenda" };
            var result = AgroSeason.WithSeasonTags(original, null, -15.0);

            Assert.True(ReferenceEquals(original, result));
        }

        [Fact]
        public void WithSeasonTags_KeepsOriginalTags()
        {
            var original = new HashSet<string> { "agro", "fazenda" };
            var date = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);
            var result = AgroSeason.WithSeasonTags(original, date, -15.0);

            Assert.False(ReferenceEquals(original, result));
            Assert.Contains("agro", result);
            Assert.Contains("fazenda", result);
            Assert.Contains("safra:plantio", result);
            Assert.Contains("safra:mes:10", result);
        }

        [Fact]
        public void Note_NoPhaseTag_Empty()
        {
            var tags = new HashSet<string> { "agro", "fazenda" };
            Assert.Equal("", AgroSeason.Note(tags));
            Assert.Equal("", AgroSeason.Note(null));
        }

        [Fact]
        public void Note_Colheita_StartsWithSpace()
        {
            var tags = new HashSet<string> { "safra:colheita", "agro" };
            string note = AgroSeason.Note(tags);

            Assert.Equal(" Época de colheita da safra.", note);
            Assert.StartsWith(" ", note);
        }

        [Fact]
        public void OriginWeight_SeasonAppliedAfterCap()
        {
            var m = new MissionDefinition
            {
                Id = "teste_safra",
                Enabled = true,
                BaseByTier = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["3"] = 10.0
                },
                OriginProfiles = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["a"] = 4.0,
                    ["b"] = 4.0
                },
                Season = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["safra:plantio"] = 1.5,
                    ["safra:entressafra"] = 0.6
                }
            };

            var tagsPlantio = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "safra:plantio" };
            double weightPlantio = MissionWeighting.OriginWeight(m, 3, tagsPlantio);
            Assert.Equal(180.0, weightPlantio, 3);

            var tagsSemSafra = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b" };
            double weightSemSafra = MissionWeighting.OriginWeight(m, 3, tagsSemSafra);
            Assert.Equal(120.0, weightSemSafra, 3);

            var tagsEntressafra = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "safra:entressafra" };
            double weightEntressafra = MissionWeighting.OriginWeight(m, 3, tagsEntressafra);
            Assert.Equal(72.0, weightEntressafra, 3);
        }

        [Fact]
        public void Pick_InsumosAgro_EntressafraCanPickRevisao()
        {
            var catalog = CargoKindCatalog.Default;
            var originTagsEntressafra = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "agro", "safra:entressafra" };
            var originTagsSemSafra = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "agro" };

            bool entressafraPickedRevisao = false;
            for (int seed = 0; seed < 500; seed++)
            {
                var pick = catalog.Pick("insumos_agro", originTagsEntressafra, null, seed);
                if (pick?.KindId == "revisao_entressafra")
                {
                    entressafraPickedRevisao = true;
                    break;
                }
            }
            Assert.True(entressafraPickedRevisao, "Com safra:entressafra, revisao_entressafra deveria ser sorteada em pelo menos um seed entre 0 e 499.");

            bool semSafraPickedRevisao = false;
            for (int seed = 0; seed < 500; seed++)
            {
                var pick = catalog.Pick("insumos_agro", originTagsSemSafra, null, seed);
                if (pick?.KindId == "revisao_entressafra")
                {
                    semSafraPickedRevisao = true;
                    break;
                }
            }
            Assert.False(semSafraPickedRevisao, "Sem tag de safra:entressafra, revisao_entressafra possui peso 0 e nunca deve ser sorteada.");
        }

        [Fact]
        public void Pick_InsumosAgro_PlantioFavorsSementes()
        {
            var catalog = CargoKindCatalog.Default;
            var tagsPlantio = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "agro", "safra:plantio" };
            var tagsEntressafra = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "agro", "safra:entressafra" };

            int sementesPlantio = 0;
            int sementesEntressafra = 0;

            for (int seed = 0; seed < 500; seed++)
            {
                var pickPlantio = catalog.Pick("insumos_agro", tagsPlantio, null, seed);
                if (pickPlantio?.KindId == "sementes_certificadas")
                {
                    sementesPlantio++;
                }

                var pickEntressafra = catalog.Pick("insumos_agro", tagsEntressafra, null, seed);
                if (pickEntressafra?.KindId == "sementes_certificadas")
                {
                    sementesEntressafra++;
                }
            }

            Assert.True(sementesPlantio > sementesEntressafra,
                $"Esperava que sementes_certificadas aparecesse mais no plantio do que na entressafra (Plantio: {sementesPlantio}, Entressafra: {sementesEntressafra})");
        }

        [Fact]
        public void GenerateBoard_WithSeasonDate_AgroOriginShowsSafraNote()
        {
            var origin = new Airport
            {
                ICAO = "SWAG",
                Latitude = -15.5,
                Longitude = -55.0,
                Tier = 3,
                IsInSimulator = true
            };
            var runway = new Runway { AirportIdent = "SWAG", LengthFt = 4500, Surface = "ASPH" };
            var dest = new Airport
            {
                ICAO = "SWFA",
                Latitude = -16.0,
                Longitude = -55.5,
                Tier = 3,
                IsInSimulator = true
            };
            var candidates = new List<AirportCandidate>
            {
                new(dest, new Runway { AirportIdent = "SWFA", LengthFt = 4000, Surface = "GRAS" })
            };

            var originTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "agro" };
            IReadOnlySet<string> GetDestTags(string icao) => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fazenda" };

            var octoberDate = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

            var boardComSafra = MissionBoardCore.GenerateBoard(
                origin,
                candidates,
                originTags,
                GetDestTags,
                MissionCatalog.Shared,
                new Random(42),
                octoberDate,
                targetCount: 20,
                originRunway: runway,
                seasonDate: octoberDate);

            var agroMission = boardComSafra.FirstOrDefault(m => m.SubId == "insumos_agro");
            Assert.NotNull(agroMission);
            Assert.Contains("Época de plantio", agroMission.Description);

            var boardSemSafra = MissionBoardCore.GenerateBoard(
                origin,
                candidates,
                originTags,
                GetDestTags,
                MissionCatalog.Shared,
                new Random(42),
                octoberDate,
                targetCount: 20,
                originRunway: runway,
                seasonDate: null);

            Assert.True(boardSemSafra.All(m => !m.Description.Contains("Época de")),
                "Sem seasonDate, nenhuma descrição deve conter marcador de safra.");
        }

        [Fact]
        public void GenerateBoard_SeasonShiftsAgroShare()
        {
            var origin = new Airport
            {
                ICAO = "SWAG",
                Latitude = -15.5,
                Longitude = -55.0,
                Tier = 3,
                IsInSimulator = true
            };
            var runway = new Runway { AirportIdent = "SWAG", LengthFt = 4500, Surface = "ASPH" };
            var dest = new Airport
            {
                ICAO = "SWFA",
                Latitude = -16.0,
                Longitude = -55.5,
                Tier = 3,
                IsInSimulator = true
            };
            var candidates = new List<AirportCandidate>
            {
                new(dest, new Runway { AirportIdent = "SWFA", LengthFt = 4000, Surface = "GRAS" })
            };

            var originTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "agro" };
            IReadOnlySet<string> GetDestTags(string icao) => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fazenda" };

            var octoberDate = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc); // Plantio
            var juneDate = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);       // Entressafra

            int agroCountPlantio = 0;
            int agroCountEntressafra = 0;

            for (int s = 1; s <= 20; s++)
            {
                var boardOct = MissionBoardCore.GenerateBoard(
                    origin,
                    candidates,
                    originTags,
                    GetDestTags,
                    MissionCatalog.Shared,
                    new Random(s * 101),
                    octoberDate,
                    targetCount: 15,
                    originRunway: runway,
                    seasonDate: octoberDate);

                agroCountPlantio += boardOct.Count(m => m.SubId == "insumos_agro");

                var boardJun = MissionBoardCore.GenerateBoard(
                    origin,
                    candidates,
                    originTags,
                    GetDestTags,
                    MissionCatalog.Shared,
                    new Random(s * 101),
                    juneDate,
                    targetCount: 15,
                    originRunway: runway,
                    seasonDate: juneDate);

                agroCountEntressafra += boardJun.Count(m => m.SubId == "insumos_agro");
            }

            Assert.True(agroCountPlantio > agroCountEntressafra,
                $"Esperava mais missões de insumos_agro em outubro ({agroCountPlantio}) do que em junho ({agroCountEntressafra}) devido ao peso da safra.");
        }
    }
}
