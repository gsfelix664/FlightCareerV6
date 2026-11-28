using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Estatísticas consolidadas de execuções de fuzz para uma aeronave específica.
    /// </summary>
    public sealed class FuzzAircraftSummary
    {
        private readonly Dictionary<string, int> _violationsByCategory = new(StringComparer.OrdinalIgnoreCase);

        public string AircraftId { get; }
        public int TotalRuns { get; private set; }
        public int Passed { get; private set; }
        public int MissingLimits { get; private set; }
        public int Failed { get; private set; }

        public IReadOnlyDictionary<string, int> ViolationsByCategory => _violationsByCategory;

        public string TopViolationCategory
        {
            get
            {
                if (_violationsByCategory.Count == 0) return "-";
                return _violationsByCategory.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
            }
        }

        public FuzzAircraftSummary(string aircraftId)
        {
            AircraftId = aircraftId ?? throw new ArgumentNullException(nameof(aircraftId));
        }

        public void RecordRun(IReadOnlyList<string> violations)
        {
            TotalRuns++;

            if (violations == null || violations.Count == 0)
            {
                Passed++;
                return;
            }

            var actualViolations = violations.Where(v => !v.StartsWith("limite ausente:", StringComparison.OrdinalIgnoreCase)).ToList();
            var missingLimits = violations.Where(v => v.StartsWith("limite ausente:", StringComparison.OrdinalIgnoreCase)).ToList();

            if (actualViolations.Count > 0)
            {
                Failed++;
                foreach (var v in actualViolations)
                {
                    string category = v.Split(':')[0].Trim();
                    _violationsByCategory[category] = _violationsByCategory.GetValueOrDefault(category, 0) + 1;
                }
            }
            else if (missingLimits.Count > 0)
            {
                MissingLimits++;
            }
            else
            {
                Passed++;
            }
        }
    }

    /// <summary>
    /// Agregação pura e ordenação de resultados de Fuzz Testing em voos sintéticos.
    /// </summary>
    public sealed class FuzzRunSummary
    {
        private readonly Dictionary<string, FuzzAircraftSummary> _summaries = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _missingLimitsByType = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _violationsByCategory = new(StringComparer.OrdinalIgnoreCase);

        public int TotalRuns => _summaries.Values.Sum(s => s.TotalRuns);
        public int TotalPassed => _summaries.Values.Sum(s => s.Passed);
        public int TotalMissingLimits => _summaries.Values.Sum(s => s.MissingLimits);
        public int TotalFailed => _summaries.Values.Sum(s => s.Failed);

        public IReadOnlyDictionary<string, int> MissingLimitsByType => _missingLimitsByType;
        public IReadOnlyDictionary<string, int> ViolationsByCategory => _violationsByCategory;

        public FuzzAircraftSummary GetOrCreateAircraftSummary(string aircraftId)
        {
            if (string.IsNullOrWhiteSpace(aircraftId))
                throw new ArgumentException("ID da aeronave não pode ser nulo ou vazio.", nameof(aircraftId));

            if (!_summaries.TryGetValue(aircraftId, out var summary))
            {
                summary = new FuzzAircraftSummary(aircraftId);
                _summaries[aircraftId] = summary;
            }

            return summary;
        }

        public FuzzAircraftSummary? GetAircraftSummary(string aircraftId)
        {
            return _summaries.GetValueOrDefault(aircraftId);
        }

        public void RecordRun(string aircraftId, IReadOnlyList<string> violations)
        {
            var summary = GetOrCreateAircraftSummary(aircraftId);
            summary.RecordRun(violations);

            if (violations != null)
            {
                foreach (var v in violations)
                {
                    if (v.StartsWith("limite ausente:", StringComparison.OrdinalIgnoreCase))
                    {
                        _missingLimitsByType[v] = _missingLimitsByType.GetValueOrDefault(v, 0) + 1;
                    }
                    else
                    {
                        string category = v.Split(':')[0].Trim();
                        _violationsByCategory[category] = _violationsByCategory.GetValueOrDefault(category, 0) + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Retorna os resumos por aeronave ordenados com falhas primeiro, seguidos por limites ausentes e ID da aeronave.
        /// </summary>
        public IReadOnlyList<FuzzAircraftSummary> GetOrderedAircraftSummaries()
        {
            return _summaries.Values
                .OrderByDescending(s => s.Failed)
                .ThenByDescending(s => s.MissingLimits)
                .ThenBy(s => s.AircraftId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
