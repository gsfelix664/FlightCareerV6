using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Sensors.Probe
{
    public static class ProbeBlockPlanner
    {
        public const int DefaultBlockSize = 200;

        public static IReadOnlyList<ProbeBlock> Plan(
            IEnumerable<ProbeSlot> slots,
            IReadOnlySet<string>? rejectedSlotNames = null,
            IReadOnlySet<string>? acceptedSlotNames = null,
            int blockSize = DefaultBlockSize)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            int validBlockSize = blockSize > 0 ? blockSize : DefaultBlockSize;

            var numericAcceptedSlots = slots
                .Where(s => !s.Entry.IsString)
                .Where(s => acceptedSlotNames == null || acceptedSlotNames.Contains(s.Name))
                .Where(s => rejectedSlotNames == null || !rejectedSlotNames.Contains(s.Name))
                .ToList();

            var blocks = new List<ProbeBlock>();
            int blockIndex = 0;

            for (int i = 0; i < numericAcceptedSlots.Count; i += validBlockSize)
            {
                int count = Math.Min(validBlockSize, numericAcceptedSlots.Count - i);
                var chunk = new List<ProbeSlot>(count);
                for (int j = 0; j < count; j++)
                {
                    chunk.Add(numericAcceptedSlots[i + j]);
                }

                blocks.Add(new ProbeBlock(blockIndex++, chunk));
            }

            return blocks;
        }
    }
}
