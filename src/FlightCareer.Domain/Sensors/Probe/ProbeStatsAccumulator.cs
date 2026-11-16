using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FlightCareer.Domain.Sensors.Probe
{
    public sealed class ProbeSlotStats
    {
        public const int MaxDistinctTracked = 1000;

        private readonly HashSet<double> _distinctNumbers = new();
        private readonly HashSet<string> _distinctStrings = new(StringComparer.Ordinal);
        private readonly object _lock = new();

        public string SlotName { get; }
        public bool IsString { get; }

        public long Samples { get; private set; }
        public long Nulls { get; private set; }
        public int DistinctCount { get; private set; }

        public double? Min { get; private set; }
        public double? Max { get; private set; }
        public double? First { get; private set; }
        public double? Last { get; private set; }

        public string? FirstString { get; private set; }
        public string? LastString { get; private set; }

        public ProbeSlotStats(string slotName, bool isString)
        {
            SlotName = slotName ?? throw new ArgumentNullException(nameof(slotName));
            IsString = isString;
        }

        public void RecordNumeric(double value)
        {
            lock (_lock)
            {
                if (double.IsNaN(value))
                {
                    Nulls++;
                    return;
                }

                Samples++;

                if (!First.HasValue)
                {
                    First = value;
                }

                Last = value;

                if (!Min.HasValue || value < Min.Value)
                {
                    Min = value;
                }

                if (!Max.HasValue || value > Max.Value)
                {
                    Max = value;
                }

                if (_distinctNumbers.Count < MaxDistinctTracked)
                {
                    _distinctNumbers.Add(value);
                }
                DistinctCount = _distinctNumbers.Count;
            }
        }

        public void RecordString(string value)
        {
            lock (_lock)
            {
                value ??= string.Empty;
                Samples++;

                FirstString ??= value;
                LastString = value;

                if (_distinctStrings.Count < MaxDistinctTracked)
                {
                    _distinctStrings.Add(value);
                }
                DistinctCount = _distinctStrings.Count;
            }
        }

        public ProbeStatus EvaluateAcceptedStatus()
        {
            lock (_lock)
            {
                if (Samples == 0)
                {
                    return ProbeStatus.AceitaSemAmostras;
                }

                if (DistinctCount <= 1 || (Min.HasValue && Max.HasValue && Min.Value == Max.Value))
                {
                    return ProbeStatus.AceitaConstante;
                }

                return ProbeStatus.AceitaVariando;
            }
        }
    }

    public sealed class ProbeStatsAccumulator
    {
        private readonly ConcurrentDictionary<string, ProbeSlotStats> _stats = new(StringComparer.Ordinal);

        public ProbeSlotStats GetOrAdd(string slotName, bool isString)
        {
            return _stats.GetOrAdd(slotName, name => new ProbeSlotStats(name, isString));
        }

        public ProbeSlotStats? Get(string slotName)
        {
            _stats.TryGetValue(slotName, out var stats);
            return stats;
        }

        public void RecordNumeric(string slotName, double value)
        {
            var stats = GetOrAdd(slotName, isString: false);
            stats.RecordNumeric(value);
        }

        public void RecordString(string slotName, string value)
        {
            var stats = GetOrAdd(slotName, isString: true);
            stats.RecordString(value);
        }

        public IReadOnlyDictionary<string, ProbeSlotStats> GetAll()
        {
            return _stats;
        }
    }
}
