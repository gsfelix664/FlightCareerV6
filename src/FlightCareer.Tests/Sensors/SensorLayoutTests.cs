using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Sensors
{
    public class SensorLayoutTests
    {
        private static SensorRegistry LoadRealRegistry()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "sensor_registry.json");
            return SensorRegistry.LoadFromFile(path);
        }

        [Fact]
        public void Build_OneEngine_HasOneSlotPerSensor()
        {
            var registry = LoadRealRegistry();
            Assert.Equal(177, registry.Sensors.Count);

            var layout = SensorLayout.Build(registry.Sensors, engineCount: 1);

            Assert.Single(layout.Blocks);
            Assert.Equal(177, layout.Blocks[0].Slots.Count);
            Assert.Equal(177, layout.Blocks.Sum(b => b.Slots.Count));
        }

        [Fact]
        public void Build_FourEngines_SplitsIntoBlocksOf256()
        {
            var registry = LoadRealRegistry();
            var layout = SensorLayout.Build(registry.Sensors, engineCount: 4, blockSize: 256);

            // 99 sensores globais + 78 sensores PerEngine * 4 = 99 + 312 = 411 slots
            Assert.Equal(411, layout.Blocks.Sum(b => b.Slots.Count));
            Assert.Equal(2, layout.Blocks.Count);

            Assert.Equal(0, layout.Blocks[0].Index);
            Assert.Equal(256, layout.Blocks[0].Slots.Count);

            Assert.Equal(1, layout.Blocks[1].Index);
            Assert.Equal(155, layout.Blocks[1].Slots.Count);
        }

        [Fact]
        public void Build_PerEngine_ExpandsIndices()
        {
            var sensors = new List<SensorDefinition>
            {
                new("eng-rpm", "GENERAL ENG RPM", "rpm", "engine", PerEngine: true),
                new("fuel-qty", "FUEL TOTAL QUANTITY", "gallons", "fuel", PerEngine: false)
            };

            var layout = SensorLayout.Build(sensors, engineCount: 2);

            Assert.Single(layout.Blocks);
            var slots = layout.Blocks[0].Slots;
            Assert.Equal(3, slots.Count);

            Assert.Equal("eng-rpm:1", slots[0].Key);
            Assert.Equal("GENERAL ENG RPM:1", slots[0].SimVar);
            Assert.Equal("rpm", slots[0].Unit);

            Assert.Equal("eng-rpm:2", slots[1].Key);
            Assert.Equal("GENERAL ENG RPM:2", slots[1].SimVar);
            Assert.Equal("rpm", slots[1].Unit);

            Assert.Equal("fuel-qty", slots[2].Key);
            Assert.Equal("FUEL TOTAL QUANTITY", slots[2].SimVar);
            Assert.Equal("gallons", slots[2].Unit);
        }

        [Fact]
        public void Build_EngineCountOutOfRange_IsClamped()
        {
            var sensors = new List<SensorDefinition>
            {
                new("eng-rpm", "GENERAL ENG RPM", "rpm", "engine", PerEngine: true)
            };

            // Menor que 1 deve ser clampado para 1
            var layoutZero = SensorLayout.Build(sensors, engineCount: 0);
            Assert.Single(layoutZero.Blocks[0].Slots);
            Assert.Equal("eng-rpm:1", layoutZero.Blocks[0].Slots[0].Key);

            var layoutNegative = SensorLayout.Build(sensors, engineCount: -5);
            Assert.Single(layoutNegative.Blocks[0].Slots);
            Assert.Equal("eng-rpm:1", layoutNegative.Blocks[0].Slots[0].Key);

            // Maior que 16 deve ser clampado para 16
            var layoutHuge = SensorLayout.Build(sensors, engineCount: 99);
            Assert.Equal(16, layoutHuge.Blocks[0].Slots.Count);
            Assert.Equal("eng-rpm:16", layoutHuge.Blocks[0].Slots[15].Key);
        }

        [Fact]
        public void Without_RemovesRejectedAndReblocks()
        {
            var sensors = new List<SensorDefinition>
            {
                new("s1", "SIMVAR 1", "number", "test", false),
                new("s2", "SIMVAR 2", "number", "test", false),
                new("s3", "SIMVAR 3", "number", "test", false),
                new("s4", "SIMVAR 4", "number", "test", false)
            };

            // Bloco de tamanho 2 -> inicialmente 2 blocos (s1,s2) e (s3,s4)
            var layout = SensorLayout.Build(sensors, engineCount: 1, blockSize: 2);
            Assert.Equal(2, layout.Blocks.Count);

            // Remove SIMVAR 2
            var pruned = layout.Without(new[] { "SIMVAR 2" });

            // Deve sobrar s1, s3, s4 -> 3 slots -> bloco 0 com (s1, s3), bloco 1 com (s4)
            Assert.Equal(2, pruned.Blocks.Count);
            Assert.Equal(2, pruned.Blocks[0].Slots.Count);
            Assert.Equal("s1", pruned.Blocks[0].Slots[0].Key);
            Assert.Equal("s3", pruned.Blocks[0].Slots[1].Key);

            Assert.Single(pruned.Blocks[1].Slots);
            Assert.Equal("s4", pruned.Blocks[1].Slots[0].Key);
            Assert.Equal(1, pruned.Blocks[1].Index);
        }

        [Fact]
        public void Map_ReturnsValuesByKey_AndIgnoresExtraPositions()
        {
            var sensors = new List<SensorDefinition>
            {
                new("k1", "V1", "number", "grp", false),
                new("k2", "V2", "number", "grp", false)
            };

            var layout = SensorLayout.Build(sensors, engineCount: 1, blockSize: 2);
            var values = new double[] { 10.5, 20.25, 999.99 }; // 3 valores para 2 slots

            var mapped = layout.Map(blockIndex: 0, values);

            Assert.Equal(2, mapped.Count);
            Assert.Equal(10.5, mapped["k1"]);
            Assert.Equal(20.25, mapped["k2"]);
            Assert.False(mapped.ContainsKey("k3"));
        }
    }
}
