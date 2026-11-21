using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using System.Globalization;
using FlightCareer.Domain.Limits;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Limits
{
    public class CatalogTests
    {
        private readonly AircraftLimitCatalogLoader _loader = new();

        [Fact]
        public void Catalog_EveryCsvModelHasFile()
        {
            var dataDir = _loader.ResolveAircraftDataDirectory();
            var csvPath = Path.Combine(dataDir, "..", "..", "CSV", "aircrafts.csv");
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(AppContext.BaseDirectory, "CSV", "aircrafts.csv");
            }

            Assert.True(File.Exists(csvPath), $"aircrafts.csv não encontrado em {csvPath}");

            var csvModels = new List<string>();
            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    csvModels.Add(csv.GetField("Modelo")!);
                }
            }

            var jsonFiles = Directory.GetFiles(dataDir, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("_", StringComparison.Ordinal))
                .ToList();

            var loadedProfiles = _loader.LoadProfiles();
            var loadedIds = loadedProfiles.Select(p => p.AircraftId).ToList();

            // Todo modelo do aircrafts.csv tem JSON correspondente
            foreach (var model in csvModels)
            {
                Assert.Contains(model, loadedIds);
            }

            // E vice-versa: todo JSON (não _) corresponde a um modelo do CSV
            foreach (var id in loadedIds)
            {
                Assert.Contains(id, csvModels);
            }

            Assert.Equal(csvModels.Count, jsonFiles.Count);
        }

        [Fact]
        public void Catalog_AllFilesValid()
        {
            var profiles = _loader.LoadProfiles();
            Assert.NotEmpty(profiles);

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var profile in profiles)
            {
                Assert.False(string.IsNullOrWhiteSpace(profile.AircraftId), "AircraftId não pode ser vazio");
                Assert.True(seenIds.Add(profile.AircraftId), $"Id repetido: {profile.AircraftId}");
                Assert.False(string.IsNullOrWhiteSpace(profile.Class), "Class não pode ser vazia");

                foreach (var limit in profile.Limits)
                {
                    Assert.True(LimitKeys.IsValid(limit.Key), $"Chave inválida: '{limit.Key}' em {profile.AircraftId}");
                    string expectedUnit = LimitKeys.UnitOf(limit.Key);
                    Assert.Equal(expectedUnit, limit.Unit);

                    if (limit.Source == LimitSource.POH)
                    {
                        Assert.False(string.IsNullOrWhiteSpace(limit.Reference), $"Limite POH '{limit.Key}' em {profile.AircraftId} sem referência");
                    }
                }
            }

            // Defaults de classe válidos
            var classDefaults = _loader.LoadClassDefaults();
            Assert.NotEmpty(classDefaults);
            foreach (var (cls, limits) in classDefaults)
            {
                Assert.NotEmpty(limits);
                foreach (var l in limits)
                {
                    Assert.True(LimitKeys.IsValid(l.Key));
                    Assert.Equal(LimitKeys.UnitOf(l.Key), l.Unit);
                }
            }
        }

        [Fact]
        public void Catalog_C152_HasSpikeValues()
        {
            var profiles = _loader.LoadProfiles();
            var c152 = profiles.FirstOrDefault(p => p.AircraftId == "Cessna C152");
            Assert.NotNull(c152);

            var vne = c152.Limits.FirstOrDefault(l => l.Key == "vne");
            Assert.NotNull(vne);
            Assert.Equal(149.0, vne.Value);
            Assert.Equal("knots", vne.Unit);

            var vfe1 = c152.Limits.FirstOrDefault(l => l.Key == "vfe:1");
            Assert.NotNull(vfe1);
            Assert.Equal(110.0, vfe1.Value);
            Assert.Equal("knots", vfe1.Unit);

            var vfe2 = c152.Limits.FirstOrDefault(l => l.Key == "vfe:2");
            Assert.NotNull(vfe2);
            Assert.Equal(85.0, vfe2.Value);
            Assert.Equal("knots", vfe2.Unit);

            var chtMax = c152.Limits.FirstOrDefault(l => l.Key == "chtMax");
            Assert.NotNull(chtMax);
            Assert.Equal(260.2, chtMax.Value, 1); // captura: 260,18 °C (500 °F)
            Assert.Equal("celsius", chtMax.Unit);
        }
    }
}
