using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlightCareer.Domain.Parts;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartCatalogTests
    {
        private static readonly PartCatalog Catalog = PartCatalog.LoadDefault();

        private static HashSet<string> LoadValidSensors()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "sensor_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "sensor_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", "sensor_registry.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "sensor_registry.json")
            };

            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException("sensor_registry.json não encontrado.");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var el in doc.RootElement.GetProperty("sensors").EnumerateArray())
            {
                set.Add(el.GetProperty("key").GetString()!);
            }
            return set;
        }

        [Fact]
        public void Catalog_IdsUniqueAndValid()
        {
            var validSensors = LoadValidSensors();

            // Deve conter entre 35 e 60 peças conforme a especificação
            Assert.InRange(Catalog.Parts.Count, 35, 60);

            // Valida sem lançar exceções (IDs únicos, grupo no vocabulário, LifeHours/LifeCycles, LifeSource e sensores existentes)
            Catalog.Validate(validSensors);

            foreach (var part in Catalog.Parts)
            {
                Assert.False(string.IsNullOrWhiteSpace(part.Id));
                Assert.Contains(".", part.Id);
                Assert.Equal(part.Id.ToLowerInvariant(), part.Id);
                Assert.True(PartGroup.All.Contains(part.Group));
                Assert.False(string.IsNullOrWhiteSpace(part.LifeSource));
                Assert.True(part.LifeHours > 0 || (part.LifeCycles.HasValue && part.LifeCycles.Value > 0));
                Assert.True(part.PartPrice >= 0);
                Assert.True(part.LaborHours >= 0);
            }
        }

        [Fact]
        public void Features_AllAircraftFilesCoherent()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            // Todas as 46 aeronaves do catálogo
            Assert.Equal(46, profiles.Count);

            foreach (var p in profiles)
            {
                Assert.NotNull(p.Features);
                Assert.NotEmpty(p.Features);

                // Validação de vocabulário fechado e coerência
                AircraftFeatures.ValidateCoherence(p.Features, p.AircraftId);
            }
        }

        [Fact]
        public void PartsFor_CarburetedPiston_HasCarburetorAndMagnetos_NoHotSection()
        {
            var features = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted
            };

            var parts = Catalog.PartsFor(features);

            Assert.Contains(parts, p => p.Id == "engine.carburetor");
            Assert.Contains(parts, p => p.Id == "ignition.magneto_left");
            Assert.Contains(parts, p => p.Id == "ignition.magneto_right");
            Assert.DoesNotContain(parts, p => p.Group == PartGroup.SecaoQuente);
            Assert.DoesNotContain(parts, p => p.Id == "engine.fuel_injectors");
        }

        [Fact]
        public void PartsFor_Jet_NoMagnetoNoPropeller()
        {
            var features = new[]
            {
                AircraftFeatures.Jet,
                AircraftFeatures.MultiEngine,
                AircraftFeatures.RetractableGear,
                AircraftFeatures.Pressurized,
                AircraftFeatures.Apu,
                AircraftFeatures.HydraulicGear
            };

            var parts = Catalog.PartsFor(features);

            Assert.Contains(parts, p => p.Group == PartGroup.SecaoQuente);
            Assert.Contains(parts, p => p.Id == "hotsection.combustor");
            Assert.Contains(parts, p => p.Id == "electrical.apu_generator");
            Assert.Contains(parts, p => p.Id == "pressurization.outflow_valve");
            Assert.DoesNotContain(parts, p => p.Group == PartGroup.Ignicao);
            Assert.DoesNotContain(parts, p => p.Group == PartGroup.Helice);
            Assert.DoesNotContain(parts, p => p.Id == "engine.carburetor");
        }

        [Fact]
        public void PartsFor_Diesel_NoMagnetoNoCarburetor()
        {
            var features = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.Diesel,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.ConstantSpeedProp,
                AircraftFeatures.FuelInjected,
                AircraftFeatures.Turbocharged
            };

            var parts = Catalog.PartsFor(features);

            Assert.Contains(parts, p => p.Id == "engine.coolant_system");
            Assert.Contains(parts, p => p.Id == "engine.turbocharger");
            Assert.Contains(parts, p => p.Id == "ignition.glow_plugs");
            Assert.DoesNotContain(parts, p => p.Id == "ignition.magneto_left");
            Assert.DoesNotContain(parts, p => p.Id == "ignition.magneto_right");
            Assert.DoesNotContain(parts, p => p.Id == "engine.carburetor");
            Assert.DoesNotContain(parts, p => p.Group == PartGroup.SecaoQuente);
        }

        [Fact]
        public void PartsFor_RetractableGear_HasGearActuator()
        {
            var features = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.RetractableGear,
                AircraftFeatures.ConstantSpeedProp,
                AircraftFeatures.FuelInjected
            };

            var parts = Catalog.PartsFor(features);

            Assert.Contains(parts, p => p.Id == "gear.actuator");
        }

        [Fact]
        public void PartsFor_FixedGear_NoGearActuator()
        {
            var features = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted
            };

            var parts = Catalog.PartsFor(features);

            Assert.DoesNotContain(parts, p => p.Id == "gear.actuator");
        }

        [Fact]
        public void PartsFor_Pressurized_HasPressurizationParts()
        {
            var features = new[]
            {
                AircraftFeatures.Turboprop,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.RetractableGear,
                AircraftFeatures.ConstantSpeedProp,
                AircraftFeatures.Pressurized,
                AircraftFeatures.HydraulicGear
            };

            var parts = Catalog.PartsFor(features);

            Assert.Contains(parts, p => p.Group == PartGroup.Pressurizacao);
            Assert.Contains(parts, p => p.Id == "pressurization.outflow_valve");
            Assert.Contains(parts, p => p.Id == "pressurization.door_seals");
            Assert.Contains(parts, p => p.Group == PartGroup.Hidraulico);
        }

        [Fact]
        public void EveryCatalogAircraft_HasAtLeastCorePartsForItsClass()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            foreach (var profile in profiles)
            {
                var parts = Catalog.PartsFor(profile.Features);

                // Toda aeronave deve ter pelo menos: célula, trem, pneus, freios, elétrico e óleo
                Assert.True(parts.Any(p => p.Group == PartGroup.Celula), $"Aeronave '{profile.AircraftId}' sem peças de Célula.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Trem), $"Aeronave '{profile.AircraftId}' sem peças de Trem.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Pneus), $"Aeronave '{profile.AircraftId}' sem peças de Pneus.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Freios), $"Aeronave '{profile.AircraftId}' sem peças de Freios.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Eletrico), $"Aeronave '{profile.AircraftId}' sem peças de Elétrico.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Oleo), $"Aeronave '{profile.AircraftId}' sem peças de Óleo.");

                // Conforme a classe:
                if (profile.Features.Contains(AircraftFeatures.Piston))
                {
                    Assert.True(parts.Any(p => p.Group == PartGroup.Motor), $"Pistão '{profile.AircraftId}' sem peças de Motor.");
                    Assert.True(parts.Any(p => p.Group == PartGroup.Ignicao), $"Pistão '{profile.AircraftId}' sem peças de Ignição.");
                    Assert.True(parts.Any(p => p.Group == PartGroup.Helice), $"Pistão '{profile.AircraftId}' sem peças de Hélice.");
                }
                else if (profile.Features.Contains(AircraftFeatures.Turboprop))
                {
                    Assert.True(parts.Any(p => p.Group == PartGroup.SecaoQuente), $"Turboélice '{profile.AircraftId}' sem peças de Seção quente.");
                    Assert.True(parts.Any(p => p.Group == PartGroup.Helice), $"Turboélice '{profile.AircraftId}' sem peças de Hélice.");
                }
                else if (profile.Features.Contains(AircraftFeatures.Jet))
                {
                    Assert.True(parts.Any(p => p.Group == PartGroup.SecaoQuente), $"Jato '{profile.AircraftId}' sem peças de Seção quente.");
                    Assert.False(parts.Any(p => p.Group == PartGroup.Helice), $"Jato '{profile.AircraftId}' possui peças de Hélice indevidamente.");
                }
            }
        }
    }
}
