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

            // Deve conter exatamente 153 peças (147 originais + 6 peças do grupo Iluminação)
            Assert.Equal(153, Catalog.Parts.Count);
            Assert.InRange(Catalog.Parts.Count, 130, 180);

            // Valida sem lançar exceções (IDs únicos, grupo no vocabulário, LifeHours/LifeCycles, LifeSource e sensores existentes)
            Catalog.Validate(validSensors);

            int perEngineCount = 0;
            int priceScalesCount = 0;
            int calendarLifeCount = 0;

            foreach (var part in Catalog.Parts)
            {
                Assert.False(string.IsNullOrWhiteSpace(part.Id));
                Assert.Contains(".", part.Id);
                Assert.Equal(part.Id.ToLowerInvariant(), part.Id);
                Assert.True(PartGroup.All.Contains(part.Group));
                Assert.False(string.IsNullOrWhiteSpace(part.LifeSource));
                Assert.True(part.LifeSource.StartsWith("estimativa — revisar") || part.LifeSource.StartsWith("calibração do usuário"),
                    $"Peça '{part.Id}' tem LifeSource inesperado: '{part.LifeSource}'");
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

                // F2.1b Rodada 3: vida útil calendárica
                if (part.CalendarLifeMonths.HasValue)
                {
                    Assert.True(part.CalendarLifeMonths.Value > 0, $"Peça '{part.Id}' tem CalendarLifeMonths inválido ({part.CalendarLifeMonths.Value}).");
                    calendarLifeCount++;
                }

                if (part.PerEngine) perEngineCount++;
                if (part.PriceScalesWithAircraft) priceScalesCount++;
            }

            // Garante que as novas propriedades perEngine, priceScalesWithAircraft e calendarLifeMonths foram calibradas
            Assert.True(perEngineCount > 0, "Nenhuma peça configurada com PerEngine.");
            Assert.True(priceScalesCount > 0, "Nenhuma peça configurada com PriceScalesWithAircraft.");
            Assert.True(calendarLifeCount >= 10, "Menos de 10 peças com envelhecimento calendárico (CalendarLifeMonths).");
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
            Assert.Contains(parts, p => p.Id == "fuselage.doors_seals");
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

                // Toda aeronave deve ter pelo menos: asa, empenagem, trem, pneus, freios, elétrico e óleo
                Assert.True(parts.Any(p => p.Group == PartGroup.Asa), $"Aeronave '{profile.AircraftId}' sem peças de Asa.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Empenagem), $"Aeronave '{profile.AircraftId}' sem peças de Empenagem.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Trem), $"Aeronave '{profile.AircraftId}' sem peças de Trem.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Pneus), $"Aeronave '{profile.AircraftId}' sem peças de Pneus.");
                Assert.True(parts.Any(p => p.Group == PartGroup.Freios), $"Aeronave '{profile.AircraftId}' sem peças de Freios.");
                if (profile.Features.Contains(AircraftFeatures.NoElectrical))
                {
                    Assert.False(parts.Any(p => p.Group == PartGroup.Eletrico), $"Aeronave '{profile.AircraftId}' possui peças de Elétrico indevidamente.");
                }
                else
                {
                    Assert.True(parts.Any(p => p.Group == PartGroup.Eletrico), $"Aeronave '{profile.AircraftId}' sem peças de Elétrico.");
                }
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

        [Fact]
        public void PartGroups_AllGroupsHaveParts()
        {
            Assert.Equal(23, PartGroup.All.Count);

            foreach (var group in PartGroup.All)
            {
                var count = Catalog.Parts.Count(p => string.Equals(p.Group, group, StringComparison.OrdinalIgnoreCase));
                Assert.True(count > 0, $"Grupo '{group}' não possui peças cadastradas no catálogo.");
            }
        }

        [Fact]
        public void CalendarLife_StationaryAgingPartsConfigured()
        {
            // Peças de envelhecimento estacionário do Adendo 4
            var mainBattery = Catalog.Find("electrical.battery");
            Assert.NotNull(mainBattery);
            Assert.Equal(24, mainBattery.CalendarLifeMonths);

            var mainTire = Catalog.Find("tires.main");
            Assert.NotNull(mainTire);
            Assert.Equal(48, mainTire.CalendarLifeMonths);

            var hydLines = Catalog.Find("hydraulic.lines_fittings");
            Assert.NotNull(hydLines);
            Assert.Equal(60, hydLines.CalendarLifeMonths);

            var crewO2 = Catalog.Find("oxygen.crew_cylinder");
            Assert.NotNull(crewO2);
            Assert.Equal(60, crewO2.CalendarLifeMonths);

            var fireBottles = Catalog.Find("fire.engine_extinguisher_bottles");
            Assert.NotNull(fireBottles);
            Assert.Equal(60, fireBottles.CalendarLifeMonths);

            var elt = Catalog.Find("avionics.flight_recorders_elt");
            Assert.NotNull(elt);
            Assert.Equal(24, elt.CalendarLifeMonths);

            var oil = Catalog.Find("oil.lubricant");
            Assert.NotNull(oil);
            Assert.Equal(12, oil.CalendarLifeMonths);
        }

        [Fact]
        public void Adendo4Features_CoherenceRulesEnforced()
        {
            // 1. Galley sem passengerCabin deve falhar
            var invalidGalley = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.Galley
            };
            var ex1 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidGalley, "TestGalleyNoCabin"));
            Assert.Contains("galley requer passengerCabin", ex1.Message);

            // 2. Lavatory sem passengerCabin deve falhar
            var invalidLavatory = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.Lavatory
            };
            var ex2 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidLavatory, "TestLavatoryNoCabin"));
            Assert.Contains("lavatory requer passengerCabin", ex2.Message);

            // 3. Floats e Skis simultaneamente deve falhar
            var invalidFloatsSkis = new[]
            {
                AircraftFeatures.Turboprop,
                AircraftFeatures.MultiEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.ConstantSpeedProp,
                AircraftFeatures.Floats,
                AircraftFeatures.Skis
            };
            var ex3 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidFloatsSkis, "TestFloatsAndSkis"));
            Assert.Contains("floats (flutuadores) e skis (esquis)", ex3.Message);

            // 4. IceDetector em pistão deve falhar
            var invalidIceDetectorPiston = new[]
            {
                AircraftFeatures.Piston,
                AircraftFeatures.SingleEngine,
                AircraftFeatures.FixedGear,
                AircraftFeatures.FixedPitchProp,
                AircraftFeatures.Carbureted,
                AircraftFeatures.IceDetector
            };
            var ex4 = Assert.Throws<InvalidOperationException>(() => AircraftFeatures.ValidateCoherence(invalidIceDetectorPiston, "TestIceDetectorPiston"));
            Assert.Contains("iceDetector", ex4.Message);
        }

        [Fact]
        public void SpecificAircraft_Adendo4FeaturesVerified()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles().ToDictionary(p => p.AircraftId, StringComparer.OrdinalIgnoreCase);

            // 1. DHC-6-300 Twin Otter capturado possui skis, passengerCabin, oxygen e wipers
            Assert.True(profiles.ContainsKey("DHC-6-300 Twin"));
            var dhc6Features = profiles["DHC-6-300 Twin"].Features;
            Assert.Contains(AircraftFeatures.Skis, dhc6Features);
            Assert.Contains(AircraftFeatures.PassengerCabin, dhc6Features);
            Assert.Contains(AircraftFeatures.Oxygen, dhc6Features);
            Assert.Contains(AircraftFeatures.Wipers, dhc6Features);

            // 2. 747-8i possui passengerCabin, galley, lavatory, oxygen, fireProtection, wipers e iceDetector
            Assert.True(profiles.ContainsKey("747-8i"));
            var b748Features = profiles["747-8i"].Features;
            Assert.Contains(AircraftFeatures.PassengerCabin, b748Features);
            Assert.Contains(AircraftFeatures.Galley, b748Features);
            Assert.Contains(AircraftFeatures.Lavatory, b748Features);
            Assert.Contains(AircraftFeatures.Oxygen, b748Features);
            Assert.Contains(AircraftFeatures.FireProtection, b748Features);
            Assert.Contains(AircraftFeatures.Wipers, b748Features);
            Assert.Contains(AircraftFeatures.IceDetector, b748Features);

            // 3. C152 é treinador básico de 2 lugares: não possui nenhuma das features de cabine/transporte/específicos
            Assert.True(profiles.ContainsKey("Cessna C152"));
            var c152Features = profiles["Cessna C152"].Features;
            Assert.DoesNotContain(AircraftFeatures.PassengerCabin, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Galley, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Lavatory, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Oxygen, c152Features);
            Assert.DoesNotContain(AircraftFeatures.FireProtection, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Floats, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Skis, c152Features);
            Assert.DoesNotContain(AircraftFeatures.Wipers, c152Features);
            Assert.DoesNotContain(AircraftFeatures.IceDetector, c152Features);
        }

        [Fact]
        public void PartsFor_Adendo4_CabinOxygenFireEmergencyPartsFilterAppropriately()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles().ToDictionary(p => p.AircraftId, StringComparer.OrdinalIgnoreCase);

            var c152Parts = Catalog.PartsFor(profiles["Cessna C152"].Features);
            var dhc6Parts = Catalog.PartsFor(profiles["DHC-6-300 Twin"].Features);
            var b748Parts = Catalog.PartsFor(profiles["747-8i"].Features);

            // C152: extintor portátil comum a todos, mas sem cabine comercial, oxigênio, detecção de fogo fixa, escorregadeiras ou esquis
            Assert.Contains(c152Parts, p => p.Id == "fire.cabin_extinguishers");
            Assert.DoesNotContain(c152Parts, p => p.Id == "cabin.passenger_seats");
            Assert.DoesNotContain(c152Parts, p => p.Id == "cabin.galley");
            Assert.DoesNotContain(c152Parts, p => p.Id == "cabin.lavatory");
            Assert.DoesNotContain(c152Parts, p => p.Id == "oxygen.crew_cylinder");
            Assert.DoesNotContain(c152Parts, p => p.Id == "fire.engine_detection");
            Assert.DoesNotContain(c152Parts, p => p.Id == "emergency.evacuation_slides");
            Assert.DoesNotContain(c152Parts, p => p.Id == "gear.skis");

            // DHC-6: esquis, assentos de passageiros, oxigênio, limpadores
            Assert.Contains(dhc6Parts, p => p.Id == "gear.skis");
            Assert.Contains(dhc6Parts, p => p.Id == "cabin.passenger_seats");
            Assert.Contains(dhc6Parts, p => p.Id == "oxygen.crew_cylinder");
            Assert.Contains(dhc6Parts, p => p.Id == "emergency.windshield_wipers");
            Assert.DoesNotContain(dhc6Parts, p => p.Id == "cabin.galley");
            Assert.DoesNotContain(dhc6Parts, p => p.Id == "emergency.evacuation_slides");

            // 747-8i: assentos, galley, lavatório, IFE, escorregadeiras, garrafas de fogo de motor/APU, detector de gelo
            Assert.Contains(b748Parts, p => p.Id == "cabin.passenger_seats");
            Assert.Contains(b748Parts, p => p.Id == "cabin.galley");
            Assert.Contains(b748Parts, p => p.Id == "cabin.lavatory");
            Assert.Contains(b748Parts, p => p.Id == "cabin.in_flight_entertainment");
            Assert.Contains(b748Parts, p => p.Id == "emergency.evacuation_slides");
            Assert.Contains(b748Parts, p => p.Id == "emergency.life_vests_rafts");
            Assert.Contains(b748Parts, p => p.Id == "fire.engine_detection");
            Assert.Contains(b748Parts, p => p.Id == "fire.engine_extinguisher_bottles");
            Assert.Contains(b748Parts, p => p.Id == "fire.apu_extinguisher");
            Assert.Contains(b748Parts, p => p.Id == "emergency.ice_detector");
            Assert.DoesNotContain(b748Parts, p => p.Id == "gear.skis");
        }

        [Fact]
        public void Catalogo_SemExternalLighting()
        {
            Assert.Null(Catalog.Find("electrical.external_lighting"));
            Assert.DoesNotContain(Catalog.Parts, p => p.Id == "electrical.external_lighting");
        }

        [Fact]
        public void Catalogo_NoElectrical_SemPecasEletricas()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            var cubProfile = profiles.First(p => p.AircraftId.Equals("Piper J-3 Cub", StringComparison.OrdinalIgnoreCase));
            var capProfile = profiles.First(p => p.AircraftId.Equals("CAP-4", StringComparison.OrdinalIgnoreCase));

            foreach (var profile in new[] { cubProfile, capProfile })
            {
                var parts = Catalog.PartsFor(profile.Features);

                // Nenhuma peça do grupo Elétrico
                Assert.DoesNotContain(parts, p => p.Group == PartGroup.Eletrico);

                // Nenhuma peça do grupo Iluminação
                Assert.DoesNotContain(parts, p => p.Group == PartGroup.Iluminacao);

                // No grupo Aviônicos, apenas analog_flight_instruments é permitida
                var avionicsParts = parts.Where(p => p.Group == PartGroup.Avionicos).ToList();
                Assert.All(avionicsParts, p => Assert.Equal("avionics.analog_flight_instruments", p.Id));
                Assert.Contains(parts, p => p.Id == "avionics.analog_flight_instruments");
            }
        }

        [Fact]
        public void Catalogo_NoFlaps_SemFlap()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            var cubProfile = profiles.First(p => p.AircraftId.Equals("Piper J-3 Cub", StringComparison.OrdinalIgnoreCase));
            var capProfile = profiles.First(p => p.AircraftId.Equals("CAP-4", StringComparison.OrdinalIgnoreCase));

            foreach (var profile in new[] { cubProfile, capProfile })
            {
                var parts = Catalog.PartsFor(profile.Features);
                Assert.DoesNotContain(parts, p => p.Id == "airframe.flaps");
            }
        }

        [Fact]
        public void Catalogo_Motor_CalibracaoDoUsuario()
        {
            var cyl = Catalog.Get("engine.cylinders")!;
            Assert.Equal(250, cyl.LifeHours);
            Assert.Null(cyl.LifeCycles);
            Assert.Equal(1600m, cyl.PartPrice);
            Assert.Equal(8.0, cyl.LaborHours);
            Assert.Equal(new[] { 4.0, 14.0 }, cyl.DowntimeHours);
            Assert.Equal("calibração do usuário 2026-11-17", cyl.LifeSource);

            var starter = Catalog.Get("engine.starter")!;
            Assert.Equal(200, starter.LifeHours);
            Assert.Equal(150, starter.LifeCycles);
            Assert.Equal(320m, starter.PartPrice);
            Assert.Equal(1.5, starter.LaborHours);
            Assert.Equal(new[] { 2.0, 8.0 }, starter.DowntimeHours);
            Assert.Equal("calibração do usuário 2026-11-17", starter.LifeSource);

            var fan = Catalog.Get("engine.fan")!;
            Assert.Equal(600, fan.LifeHours);
            Assert.Equal(400, fan.LifeCycles);
            Assert.Equal(16000m, fan.PartPrice);
            Assert.Equal(8.0, fan.LaborHours);
            Assert.Equal(new[] { 6.0, 18.0 }, fan.DowntimeHours);
            Assert.Equal("calibração do usuário 2026-11-17", fan.LifeSource);

            var rev = Catalog.Get("engine.thrust_reverser")!;
            Assert.Equal(0.0, rev.LifeHours);
            Assert.Equal(300, rev.LifeCycles);
            Assert.Equal(7800m, rev.PartPrice);
            Assert.Equal(5.0, rev.LaborHours);
            Assert.Equal(new[] { 4.0, 12.0 }, rev.DowntimeHours);
            Assert.Equal("calibração do usuário 2026-11-17", rev.LifeSource);
        }
    }
}
