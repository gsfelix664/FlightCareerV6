using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlightCareer.Domain.Sensors.Probe
{
    public static class ProbeSlotExpander
    {
        public static IReadOnlyList<ProbeSlot> Expand(IEnumerable<ProbeEntry> entries, ProbeOptions? options = null)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            options ??= new ProbeOptions();
            var slots = new List<ProbeSlot>();

            Regex? filterRegex = null;
            if (!string.IsNullOrWhiteSpace(options.Filter))
            {
                filterRegex = new Regex(options.Filter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            foreach (var entry in entries)
            {
                var entrySlots = ExpandEntry(entry, options);
                foreach (var slot in entrySlots)
                {
                    if (filterRegex == null || filterRegex.IsMatch(slot.Name))
                    {
                        slots.Add(slot);
                    }
                }
            }

            return slots;
        }

        private static IEnumerable<ProbeSlot> ExpandEntry(ProbeEntry entry, ProbeOptions options)
        {
            switch (entry.IndexKind)
            {
                case ProbeIndexKind.Engine:
                    for (int i = 1; i <= options.MaxEngine; i++)
                    {
                        yield return new ProbeSlot($"{entry.SimVar}:{i}", entry, i);
                    }
                    break;

                case ProbeIndexKind.Contact:
                    for (int i = 0; i <= options.MaxContact; i++)
                    {
                        yield return new ProbeSlot($"{entry.SimVar}:{i}", entry, i);
                    }
                    break;

                case ProbeIndexKind.Wear:
                    for (int i = 0; i <= 49; i++)
                    {
                        yield return new ProbeSlot($"{entry.SimVar}:{i}", entry, i);
                    }
                    break;

                case ProbeIndexKind.Cover:
                    for (int i = 0; i <= 7; i++)
                    {
                        yield return new ProbeSlot($"{entry.SimVar}:{i}", entry, i);
                    }
                    break;

                case ProbeIndexKind.Generic:
                    yield return new ProbeSlot(entry.SimVar, entry, null);
                    for (int i = 0; i <= options.MaxGeneric; i++)
                    {
                        yield return new ProbeSlot($"{entry.SimVar}:{i}", entry, i);
                    }
                    break;

                case ProbeIndexKind.Multi:
                case ProbeIndexKind.None:
                default:
                    yield return new ProbeSlot(entry.SimVar, entry, null);
                    break;
            }
        }
    }
}
