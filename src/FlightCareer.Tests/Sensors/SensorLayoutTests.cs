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
        public void Build_AlwaysIncludesSimulationRateFirst()
        {
            var layout = SensorLayout.Build(Array.Empty<SensorDefinition>(), engineCount: 1);

            Assert.Single(layout.Blocks);
            var firstSlot = layout.Blocks[0].Slots[0];
            Assert.Equal("ctx-simulation-rate", firstSlot.Key);
            Assert.Equal("SIMULATION RATE", firstSlot.SimVar);
            Assert.Equal("number", firstSlot.Unit);
        }

        [Fact]
        public void Build_OneEngine_HasOneSlotPerSensor()
        {
            var registry = LoadRealRegistry();
            Assert.Equal(177, registry.Sensors.Count);

            var layout = SensorLayout.Build(registry.Sensors, engineCount: 1);

            Assert.Single(layout.Blocks);
            Assert.Equal(178, layout.Blocks[0].Slots.Count);
            Assert.Equal(178, layout.Blocks.Sum(b => b.Slots.Count));
        }

        [Fact]
        public void Build_FourEngines_SplitsIntoBlocksOf256()
        {
            var registry = LoadRealRegistry();
            var layout = SensorLayout.Build(registry.Sensors, engineCount: 4, blockSize: 256);

            // 1 reservado (SIMULATION RATE) + 99 sensores globais + 78 sensores PerEngine * 4 = 1 + 99 + 312 = 412 slots
            Assert.Equal(412, layout.Blocks.Sum(b => b.Slots.Count));
            Assert.Equal(2, layout.Blocks.Count);

            Assert.Equal(0, layout.Blocks[0].Index);
            Assert.Equal(256, layout.Blocks[0].Slots.Count);

            Assert.Equal(1, layout.Blocks[1].Index);
            Assert.Equal(156, layout.Blocks[1].Slots.Count);
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
            Assert.Equal(4, slots.Count);

            Assert.Equal("ctx-simulation-rate", slots[0].Key);
            Assert.Equal("SIMULATION RATE", slots[0].SimVar);
            Assert.Equal("number", slots[0].Unit);

            Assert.Equal("eng-rpm:1", slots[1].Key);
            Assert.Equal("GENERAL ENG RPM:1", slots[1].SimVar);
            Assert.Equal("rpm", slots[1].Unit);

            Assert.Equal("eng-rpm:2", slots[2].Key);
            Assert.Equal("GENERAL ENG RPM:2", slots[2].SimVar);
            Assert.Equal("rpm", slots[2].Unit);

            Assert.Equal("fuel-qty", slots[3].Key);
            Assert.Equal("FUEL TOTAL QUANTITY", slots[3].SimVar);
            Assert.Equal("gallons", slots[3].Unit);
        }

        [Fact]
        public void Build_EngineCountOutOfRange_IsClamped()
        {
            var sensors = new List<SensorDefinition>
            {
                new("eng-rpm", "GENERAL ENG RPM", "rpm", "engine", PerEngine: true)
            };

            // Menor que 1 deve ser clampado para 1 (1 reservado + 1 perEngine = 2 slots)
            var layoutZero = SensorLayout.Build(sensors, engineCount: 0);
            Assert.Equal(2, layoutZero.Blocks[0].Slots.Count);
            Assert.Equal("ctx-simulation-rate", layoutZero.Blocks[0].Slots[0].Key);
            Assert.Equal("eng-rpm:1", layoutZero.Blocks[0].Slots[1].Key);

            var layoutNegative = SensorLayout.Build(sensors, engineCount: -5);
            Assert.Equal(2, layoutNegative.Blocks[0].Slots.Count);
            Assert.Equal("ctx-simulation-rate", layoutNegative.Blocks[0].Slots[0].Key);
            Assert.Equal("eng-rpm:1", layoutNegative.Blocks[0].Slots[1].Key);

            // Maior que 16 deve ser clampado para 16 (1 reservado + 16 perEngine = 17 slots)
            var layoutHuge = SensorLayout.Build(sensors, engineCount: 99);
            Assert.Equal(17, layoutHuge.Blocks[0].Slots.Count);
            Assert.Equal("ctx-simulation-rate", layoutHuge.Blocks[0].Slots[0].Key);
            Assert.Equal("eng-rpm:16", layoutHuge.Blocks[0].Slots[16].Key);
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

            // Com o slot reservado ctx-simulation-rate + 4 sensores = 5 slots.
            // Bloco de tamanho 2 -> inicialmente 3 blocos: (ctx, s1), (s2, s3), (s4)
            var layout = SensorLayout.Build(sensors, engineCount: 1, blockSize: 2);
            Assert.Equal(3, layout.Blocks.Count);

            // Remove SIMVAR 2
            var pruned = layout.Without(new[] { "SIMVAR 2" });

            // Deve sobrar ctx, s1, s3, s4 -> 4 slots -> bloco 0 com (ctx, s1), bloco 1 com (s3, s4)
            Assert.Equal(2, pruned.Blocks.Count);
            Assert.Equal(2, pruned.Blocks[0].Slots.Count);
            Assert.Equal("ctx-simulation-rate", pruned.Blocks[0].Slots[0].Key);
            Assert.Equal("s1", pruned.Blocks[0].Slots[1].Key);

            Assert.Equal(2, pruned.Blocks[1].Slots.Count);
            Assert.Equal("s3", pruned.Blocks[1].Slots[0].Key);
            Assert.Equal("s4", pruned.Blocks[1].Slots[1].Key);
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

            // ctx + 2 sensores = 3 slots no bloco (pois blockSize = 3)
            var layout = SensorLayout.Build(sensors, engineCount: 1, blockSize: 3);
            var values = new double[] { 1.0, 10.5, 20.25, 999.99 }; // 4 valores para 3 slots

            var mapped = layout.Map(blockIndex: 0, values);

            Assert.Equal(3, mapped.Count);
            Assert.Equal(1.0, mapped["ctx-simulation-rate"]);
            Assert.Equal(10.5, mapped["k1"]);
            Assert.Equal(20.25, mapped["k2"]);
            Assert.False(mapped.ContainsKey("k3"));
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct SensorBlockDataSpike
        {
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 256)]
            public double[] Values;
        }

        [Fact]
        public void SensorBlockData_MarshalSize_Is2048Bytes()
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf<SensorBlockDataSpike>();
            Assert.Equal(2048, size);
        }
    }
}
