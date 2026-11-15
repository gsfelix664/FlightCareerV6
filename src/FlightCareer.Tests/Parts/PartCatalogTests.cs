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

            // Deve conter entre 130 e 180 peças conforme a especificação F2.1b (Rodada 2)
            Assert.InRange(Catalog.Parts.Count, 130, 180);

            // Valida sem lançar exceções (IDs únicos, grupo no vocabulário, LifeHours/LifeCycles, LifeSource e sensores existentes)
            Catalog.Validate(validSensors);

            int perEngineCount = 0;
            int priceScalesCount = 0;

            foreach (var part in Catalog.Parts)
            {
                Assert.False(string.IsNullOrWhiteSpace(part.Id));
                Assert.Contains(".", part.Id);
                Assert.Equal(part.Id.ToLowerInvariant(), part.Id);
                Assert.True(PartGroup.All.Contains(part.Group));
                Assert.False(string.IsNullOrWhiteSpace(part.LifeSource));
                Assert.StartsWith("estimativa — revisar", part.LifeSource);
                Assert.True(part.LifeHours > 0 || (part.LifeCycles.HasValue && part.LifeCycles.Value > 0));
                Assert.True(part.PartPrice >= 0);
                Assert.True(part.LaborHours >= 0);

                // F2.1b: validação de faixas de custo de reparo em % e tempo de hangar
                Assert.NotNull(part.RepairCostPctOfAircraft);
                Assert.Equal(2, part.RepairCostPctOfAircraft.Count);
                Assert.True(part.RepairCostPctOfAircraft[0] >= 0);
                Assert.True(part.RepairCostPctOfAircraft[0] <= part.RepairCostPctOfAircraft[1]);

                Assert.NotNull(part.DowntimeHours);
                Assert.Equal(2, part.DowntimeHours.Count);
                Assert.True(part.DowntimeHours[0] >= 0);
                Assert.True(part.DowntimeHours[0] <= part.DowntimeHours[1]);

                if (part.PerEngine) perEngineCount++;
                if (part.PriceScalesWithAircraft) priceScalesCount++;
            }

            // Garante que as novas propriedades perEngine e priceScalesWithAircraft foram calibradas
            Assert.True(perEngineCount > 0, "Nenhuma peça configurada com PerEngine.");
            Assert.True(priceScalesCount > 0, "Nenhuma peça configurada com PriceScalesWithAircraft.");
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

                // Validação de contagem de motores
                Assert.True(p.EngineCount >= 1);
                if (p.Features.Contains(AircraftFeatures.SingleEngine))
                {
                    Assert.Equal(1, p.EngineCount);
                }
                else if (p.Features.Contains(AircraftFeatures.MultiEngine))
                {
                    Assert.True(p.EngineCount >= 2);
                }
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
            Assert.Contains(parts, p => p.Id == "oil.lubricant");
            Assert.Contains(parts, p => p.Id == "oil.filter");
            Assert.Contains(parts, p => p.Id == "oil.pump");
            Assert.Contains(parts, p => p.Id == "fuel.engine_pump");
            Assert.Contains(parts, p => p.Id == "fuel.filter_gascolator");
            Assert.Contains(parts, p => p.Id == "electrical.alternator");
            Assert.Contains(parts, p => p.Id == "engine.starter");

            Assert.DoesNotContain(parts, p => p.Group == PartGroup.SecaoQuente);
            Assert.DoesNotContain(parts, p => p.Id == "engine.fuel_injectors");
            Assert.DoesNotContain(parts, p => p.Id == "engine.compressor");
            Assert.DoesNotContain(parts, p => p.Id == "oil.turbine_oil");
            Assert.DoesNotContain(parts, p => p.Id == "electrical.starter_generator");
            Assert.DoesNotContain(parts, p => p.Id == "fuel.fcu");
        }

        [Fact]
        public void PartsFor_Jet_NoMagnetoNoPropeller_HasTurbineAndFanParts()
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
            Assert.Contains(parts, p => p.Id == "hotsection.turbine_blades");
            Assert.Contains(parts, p => p.Id == "hotsection.igniters");
            Assert.Contains(parts, p => p.Id == "engine.compressor");
            Assert.Contains(parts, p => p.Id == "engine.fan");
            Assert.Contains(parts, p => p.Id == "engine.thrust_reverser");
            Assert.Contains(parts, p => p.Id == "electrical.starter_generator");
            Assert.Contains(parts, p => p.Id == "fuel.fcu");
            Assert.Contains(parts, p => p.Id == "oil.turbine_oil");
            Assert.Contains(parts, p => p.Id == "oil.turbine_filter");
            Assert.Contains(parts, p => p.Id == "oil.turbine_pump");
            Assert.Contains(parts, p => p.Id == "electrical.apu_generator");
            Assert.Contains(parts, p => p.Id == "pressurization.outflow_valve");

            // Não deve conter peças exclusivas de pistão nem hélice
            Assert.DoesNotContain(parts, p => p.Group == PartGroup.Ignicao);
            Assert.DoesNotContain(parts, p => p.Group == PartGroup.Helice);
            Assert.DoesNotContain(parts, p => p.Id == "engine.carburetor");
            Assert.DoesNotContain(parts, p => p.Id == "engine.starter");
            Assert.DoesNotContain(parts, p => p.Id == "electrical.alternator");
            Assert.DoesNotContain(parts, p => p.Id == "oil.lubricant");
            Assert.DoesNotContain(parts, p => p.Id == "oil.filter");
            Assert.DoesNotContain(parts, p => p.Id == "oil.pump");
            Assert.DoesNotContain(parts, p => p.Id == "fuel.engine_pump");
            Assert.DoesNotContain(parts, p => p.Id == "fuel.filter_gascolator");
            Assert.DoesNotContain(parts, p => p.Id == "engine.reduction_gearbox");
        }

        [Fact]
        public void PartsFor_Turboprop_HasCompressorAndReductionGearbox_NoFanNoReverser()
        {
            var features = new[]
            {
                AircraftFeatures.Turboprop,
                AircraftFeatures.MultiEngine,
                AircraftFeatures.RetractableGear,
                AircraftFeatures.ConstantSpeedProp,
                AircraftFeatures.Pressurized,
                AircraftFeatures.HydraulicGear
            };

            var parts = Catalog.PartsFor(features);

            Assert.Contains(parts, p => p.Group == PartGroup.SecaoQuente);
            Assert.Contains(parts, p => p.Group == PartGroup.Helice);
            Assert.Contains(parts, p => p.Id == "engine.compressor");
            Assert.Contains(parts, p => p.Id == "engine.reduction_gearbox");
            Assert.Contains(parts, p => p.Id == "electrical.starter_generator");
            Assert.Contains(parts, p => p.Id == "fuel.fcu");
            Assert.Contains(parts, p => p.Id == "oil.turbine_oil");
            Assert.Contains(parts, p => p.Id == "propeller.blades");
            Assert.Contains(parts, p => p.Id == "propeller.governor");

            // Não deve conter itens exclusivos de jato ou de pistão
            Assert.DoesNotContain(parts, p => p.Id == "engine.fan");
            Assert.DoesNotContain(parts, p => p.Id == "engine.thrust_reverser");
            Assert.DoesNotContain(parts, p => p.Id == "engine.starter");
            Assert.DoesNotContain(parts, p => p.Id == "electrical.alternator");
            Assert.DoesNotContain(parts, p => p.Id == "ignition.magneto_left");
            Assert.DoesNotContain(parts, p => p.Id == "oil.lubricant");
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

        [Fact]
        public void FeatureCoherence_ExclusiveAndPrerequisiteRules_Enforced()
        {
            // deIceBoots e bleedAntiIce exclusivos
            var exclusiveDeice = new[]
            {
                AircraftFeatures.Turboprop,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.ConstantSpeedProp,
                AircraftFeatures.DeIceBoots,
                AircraftFeatures.BleedAntiIce
            };
            var ex1 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(exclusiveDeice, "TestExclusiveDeice"));
            Assert.Contains("deIceBoots e bleedAntiIce", ex1.Message);

            // slats apenas em jet ou turboprop (pistão com slats deve falhar)
            var invalidSlatsPiston = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.Slats
            };
            var ex2 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidSlatsPiston, "TestPistonSlats"));
            Assert.Contains("slats", ex2.Message);

            // thsTrim apenas em jet ou turboprop
            var invalidThsPiston = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.ThsTrim
            };
            var ex3 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidThsPiston, "TestPistonThs"));
            Assert.Contains("thsTrim", ex3.Message);

            // tailwheel não tem noseWheelSteering
            var invalidTailwheelSteering = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.Tailwheel,
                AircraftFeatures.NoseWheelSteering
            };
            var ex4 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidTailwheelSteering, "TestTailwheelSteering"));
            Assert.Contains("tailwheel", ex4.Message);

            // ptu em monomotor a pistão deve falhar
            var invalidPtuPiston = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.Ptu
            };
            var ex5 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidPtuPiston, "TestPistonPtu"));
            Assert.Contains("ptu", ex5.Message);

            // rat em pistão deve falhar
            var invalidRatPiston = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.Rat
            };
            var ex6 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidRatPiston, "TestPistonRat"));
            Assert.Contains("rat", ex6.Message);

            // propDeIce em jato deve falhar
            var invalidPropDeiceJet = new[]
            {
                AircraftFeatures.Jet,
                AircraftFeatures.MultiEngine,
                AircraftFeatures.RetractableGear,
                AircraftFeatures.Pressurized,
                AircraftFeatures.PropDeIce
            };
            var ex7 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidPropDeiceJet, "TestJetPropDeIce"));
            Assert.Contains("propDeIce", ex7.Message);

            // bleedAir em pistão não-pressurizado deve falhar
            var invalidBleedPiston = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.BleedAir
            };
            var ex8 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidBleedPiston, "TestPistonBleedAir"));
            Assert.Contains("bleedAir", ex8.Message);
        }

        [Fact]
        public void SpecificAircraft_MeetF21bFeatureRequirements()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles().ToDictionary(p => p.AircraftId, StringComparer.OrdinalIgnoreCase);

            // 1. C152 não tem FMS, slats, spoilers, radar, PTU, RAT, propDeIce, FADEC ou bleedAir
            Assert.True(profiles.ContainsKey("Cessna C152"));
            var c152Features = profiles["Cessna C152"].Features;
            Assert.DoesNotContain(AircraftFeatures.Fms, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Slats, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Spoilers, c152Features);
            Assert.DoesNotContain(AircraftFeatures.WeatherRadar, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Ptu, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Rat, c152Features);
            Assert.DoesNotContain(AircraftFeatures.PropDeIce, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Fadec, c152Features);
            Assert.DoesNotContain(AircraftFeatures.BleedAir, c152Features);

            // 2. 747-8i tem slats, spoilers, FMS, radar, antiskid, anteparas de pressão (pressurized), RAT, FADEC, bleedAir
            Assert.True(profiles.ContainsKey("747-8i"));
            var b748Features = profiles["747-8i"].Features;
            Assert.Contains(AircraftFeatures.Slats, b748Features);
            Assert.Contains(AircraftFeatures.Spoilers, b748Features);
            Assert.Contains(AircraftFeatures.Fms, b748Features);
            Assert.Contains(AircraftFeatures.WeatherRadar, b748Features);
            Assert.Contains(AircraftFeatures.Antiskid, b748Features);
            Assert.Contains(AircraftFeatures.Pressurized, b748Features);
            Assert.Contains(AircraftFeatures.Rat, b748Features);
            Assert.Contains(AircraftFeatures.Fadec, b748Features);
            Assert.Contains(AircraftFeatures.BleedAir, b748Features);

            // 3. A320neo tem PTU, RAT, FADEC, bleedAir
            Assert.True(profiles.ContainsKey("A320neo"));
            var a320Features = profiles["A320neo"].Features;
            Assert.Contains(AircraftFeatures.Ptu, a320Features);
            Assert.Contains(AircraftFeatures.Rat, a320Features);
            Assert.Contains(AircraftFeatures.Fadec, a320Features);
            Assert.Contains(AircraftFeatures.BleedAir, a320Features);

            // 4. 737 Max 8 tem PTU, FADEC, bleedAir, mas NÃO possui RAT
            Assert.True(profiles.ContainsKey("737 Max 8"));
            var b738Features = profiles["737 Max 8"].Features;
            Assert.Contains(AircraftFeatures.Ptu, b738Features);
            Assert.Contains(AircraftFeatures.Fadec, b738Features);
            Assert.Contains(AircraftFeatures.BleedAir, b738Features);
            Assert.DoesNotContain(AircraftFeatures.Rat, b738Features);

            // 5. King Air tem botas de degelo (deIceBoots), propDeIce e bleedAir
            Assert.True(profiles.ContainsKey("Beechcraft King Air"));
            var kingAirFeatures = profiles["Beechcraft King Air"].Features;
            Assert.Contains(AircraftFeatures.DeIceBoots, kingAirFeatures);
            Assert.Contains(AircraftFeatures.PropDeIce, kingAirFeatures);
            Assert.Contains(AircraftFeatures.BleedAir, kingAirFeatures);

            // 6. Diamond DA62 tem FADEC e propDeIce, sem bleedAir
            Assert.True(profiles.ContainsKey("Diamond DA62"));
            var da62Features = profiles["Diamond DA62"].Features;
            Assert.Contains(AircraftFeatures.Fadec, da62Features);
            Assert.Contains(AircraftFeatures.PropDeIce, da62Features);
            Assert.DoesNotContain(AircraftFeatures.BleedAir, da62Features);
        }

        [Fact]
        public void PartsFor_ExpandedCatalog_FeaturesFilterAppropriateParts()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles().ToDictionary(p => p.AircraftId, StringComparer.OrdinalIgnoreCase);

            var c152Parts = Catalog.PartsFor(profiles["Cessna C152"].Features);
            var b748Parts = Catalog.PartsFor(profiles["747-8i"].Features);
            var a320Parts = Catalog.PartsFor(profiles["A320neo"].Features);
            var kingAirParts = Catalog.PartsFor(profiles["Beechcraft King Air"].Features);

            // C152 tem instrumentos analógicos e empenagem básica, sem FMS, slats, radar, PTU, RAT ou FADEC
            Assert.Contains(c152Parts, p => p.Id == "avionics.analog_flight_instruments");
            Assert.Contains(c152Parts, p => p.Id == "empennage.elevator");
            Assert.Contains(c152Parts, p => p.Id == "empennage.rudder");
            Assert.DoesNotContain(c152Parts, p => p.Id == "avionics.fms_computer");
            Assert.DoesNotContain(c152Parts, p => p.Id == "wing.slats");
            Assert.DoesNotContain(c152Parts, p => p.Id == "wing.spoilers");
            Assert.DoesNotContain(c152Parts, p => p.Id == "avionics.weather_radar");
            Assert.DoesNotContain(c152Parts, p => p.Id == "hydraulic.ptu");
            Assert.DoesNotContain(c152Parts, p => p.Id == "electrical.rat");
            Assert.DoesNotContain(c152Parts, p => p.Id == "engine.fadec_eec");
            Assert.DoesNotContain(c152Parts, p => p.Id == "pneumatic.bleed_valves");

            // 747-8i tem FMS, slats, spoilers, radar, antiskid, anteparas de pressão, RAT, FADEC, bleed valves e packs
            Assert.Contains(b748Parts, p => p.Id == "avionics.fms_computer");
            Assert.Contains(b748Parts, p => p.Id == "avionics.glass_displays");
            Assert.Contains(b748Parts, p => p.Id == "wing.slats");
            Assert.Contains(b748Parts, p => p.Id == "wing.spoilers");
            Assert.Contains(b748Parts, p => p.Id == "avionics.weather_radar");
            Assert.Contains(b748Parts, p => p.Id == "gear.antiskid_system");
            Assert.Contains(b748Parts, p => p.Id == "fuselage.pressure_bulkheads");
            Assert.Contains(b748Parts, p => p.Id == "electrical.rat");
            Assert.Contains(b748Parts, p => p.Id == "engine.fadec_eec");
            Assert.Contains(b748Parts, p => p.Id == "pneumatic.bleed_valves");
            Assert.Contains(b748Parts, p => p.Id == "pneumatic.air_cycle_machine");
            Assert.Contains(b748Parts, p => p.Id == "hotsection.fuel_nozzles");
            Assert.Contains(b748Parts, p => p.Id == "hotsection.ngv");
            Assert.DoesNotContain(b748Parts, p => p.Id == "avionics.analog_flight_instruments");

            // A320neo tem PTU, RAT, FADEC e bleed valves
            Assert.Contains(a320Parts, p => p.Id == "hydraulic.ptu");
            Assert.Contains(a320Parts, p => p.Id == "electrical.rat");
            Assert.Contains(a320Parts, p => p.Id == "engine.fadec_eec");
            Assert.Contains(a320Parts, p => p.Id == "pneumatic.bleed_valves");

            // King Air tem degelo de hélice e válvulas de sangria
            Assert.Contains(kingAirParts, p => p.Id == "propeller.deice_boots");
            Assert.Contains(kingAirParts, p => p.Id == "pneumatic.bleed_valves");
        }
    }
}
