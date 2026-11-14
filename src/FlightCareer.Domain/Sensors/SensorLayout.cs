using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Sensors
{
    /// <summary>
    /// Slot individual mapeado em um bloco de dados do SimConnect.
    /// </summary>
    /// <param name="Key">Chave do sensor (com :N se por motor).</param>
    /// <param name="SimVar">Nome da SimVar no MSFS (com :N se por motor).</param>
    /// <param name="Unit">Unidade de medida no SDK.</param>
    public sealed record SensorSlot(string Key, string SimVar, string Unit);

    /// <summary>
    /// Bloco sequencial de slots limitado a uma capacidade máxima (padrão 256).
    /// </summary>
    /// <param name="Index">Índice sequencial do bloco (0-based).</param>
    /// <param name="Slots">Lista de slots contidos no bloco.</param>
    public sealed record SensorBlock(int Index, IReadOnlyList<SensorSlot> Slots);

    /// <summary>
    /// Layout determinístico de leitura particionado em blocos de até blockSize doubles.
    /// </summary>
    public sealed class SensorLayout
    {
        public int BlockSize { get; }
        public IReadOnlyList<SensorBlock> Blocks { get; }

        public SensorLayout(IReadOnlyList<SensorBlock> blocks, int blockSize = 256)
        {
            Blocks = blocks ?? Array.Empty<SensorBlock>();
            BlockSize = blockSize > 0 ? blockSize : 256;
        }

        /// <summary>
        /// Monta o layout a partir da lista de sensores e da contagem de motores.
        /// Expande sensores PerEngine em :1..:engineCount.
        /// </summary>
        public static SensorLayout Build(IReadOnlyList<SensorDefinition> sensors, int engineCount, int blockSize = 256)
        {
            if (sensors == null || sensors.Count == 0)
            {
                return new SensorLayout(Array.Empty<SensorBlock>(), blockSize);
            }

            int validBlockSize = blockSize > 0 ? blockSize : 256;
            int clampedEngines = Math.Clamp(engineCount, 1, 16);

            var flatSlots = new List<SensorSlot>();

            foreach (var s in sensors)
            {
                if (s.PerEngine)
                {
                    for (int eng = 1; eng <= clampedEngines; eng++)
                    {
                        flatSlots.Add(new SensorSlot(
                            Key: $"{s.Key}:{eng}",
                            SimVar: $"{s.SimVar}:{eng}",
                            Unit: s.Unit));
                    }
                }
                else
                {
                    flatSlots.Add(new SensorSlot(
                        Key: s.Key,
                        SimVar: s.SimVar,
                        Unit: s.Unit));
                }
            }

            return CreateFromSlots(flatSlots, validBlockSize);
        }

        /// <summary>
        /// Cria uma nova versão do layout removendo SimVars rejeitadas (com índice se aplicável)
        /// e re-particionando os slots restantes em blocos.
        /// </summary>
        public SensorLayout Without(IEnumerable<string> simVarsComIndice)
        {
            if (simVarsComIndice == null)
                return this;

            var rejectedSet = new HashSet<string>(simVarsComIndice, StringComparer.OrdinalIgnoreCase);
            if (rejectedSet.Count == 0)
                return this;

            var remainingSlots = Blocks
                .SelectMany(b => b.Slots)
                .Where(slot => !rejectedSet.Contains(slot.SimVar))
                .ToList();

            return CreateFromSlots(remainingSlots, BlockSize);
        }

        /// <summary>
        /// Converte o array recebido do bloco no índice indicado em um dicionário chave -> valor.
        /// Ignora valores extras além da quantidade de slots do bloco.
        /// </summary>
        public IReadOnlyDictionary<string, double> Map(int blockIndex, double[] values)
        {
            if (blockIndex < 0 || blockIndex >= Blocks.Count || values == null || values.Length == 0)
            {
                return new Dictionary<string, double>();
            }

            var block = Blocks[blockIndex];
            int count = Math.Min(block.Slots.Count, values.Length);
            var result = new Dictionary<string, double>(count, StringComparer.Ordinal);

            for (int i = 0; i < count; i++)
            {
                result[block.Slots[i].Key] = values[i];
            }

            return result;
        }

        private static SensorLayout CreateFromSlots(IReadOnlyList<SensorSlot> flatSlots, int blockSize)
        {
            var blocks = new List<SensorBlock>();
            int blockIndex = 0;

            for (int i = 0; i < flatSlots.Count; i += blockSize)
            {
                int count = Math.Min(blockSize, flatSlots.Count - i);
                var chunk = new List<SensorSlot>(count);
                for (int j = 0; j < count; j++)
                {
                    chunk.Add(flatSlots[i + j]);
                }

                blocks.Add(new SensorBlock(blockIndex++, chunk));
            }

            return new SensorLayout(blocks, blockSize);
        }
    }
}
