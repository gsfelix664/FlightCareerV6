using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Agrupa as excedências por peça que o <see cref="PartWearEngine"/> devolve a cada tick (uma por peça, enquanto a
    /// faixa estiver ativa) em duas linhas de blackbox por ocorrência: início e fim da faixa, com duração, pico,
    /// dano somado e número de peças. Sem isso, uma subida a pleno gerava 12 linhas por segundo.
    /// Chave da ocorrência: regra + faixa/motor (<see cref="PartWearExceedance.Notes"/>); trocar de faixa encerra uma
    /// ocorrência e abre outra.
    /// </summary>
    public sealed class ExceedanceLogAggregator
    {
        private sealed class Episode
        {
            public required string RuleId;
            public required string Notes;
            public double StartSimSeconds;
            public double Peak;
            public double Limit;
            public double DamagePct;
            public readonly HashSet<string> Parts = new(StringComparer.OrdinalIgnoreCase);
        }

        private readonly Dictionary<string, Episode> _open = new(StringComparer.Ordinal);

        public int OpenCount => _open.Count;

        /// <summary>Processa as excedências de um tick (lista vazia = nenhuma faixa ativa) e devolve as linhas a gravar.</summary>
        public IReadOnlyList<string> Update(IReadOnlyList<PartWearExceedance>? tick, double simSeconds)
        {
            var lines = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var ex in tick ?? Array.Empty<PartWearExceedance>())
            {
                string key = ex.RuleId + "|" + ex.Notes;
                seen.Add(key);
                if (!_open.TryGetValue(key, out var ep))
                {
                    ep = new Episode
                    {
                        RuleId = ex.RuleId,
                        Notes = ex.Notes,
                        StartSimSeconds = simSeconds,
                        Peak = ex.PeakValue,
                        Limit = ex.LimitValue
                    };
                    _open[key] = ep;
                    lines.Add(string.Format(CultureInfo.InvariantCulture,
                        "INICIO {0} - {1} - valor {2:F1} (limite {3:F1})", ep.RuleId, ep.Notes, ex.PeakValue, ex.LimitValue));
                }

                ep.Parts.Add(ex.TargetId);
                ep.Peak = Math.Max(ep.Peak, ex.PeakValue);
                ep.DamagePct += Math.Max(0.0, ex.TotalDamagePct);
            }

            foreach (var key in _open.Keys.Where(k => !seen.Contains(k)).ToList())
            {
                lines.Add(Close(_open[key], simSeconds));
                _open.Remove(key);
            }

            return lines;
        }

        /// <summary>Encerra as ocorrências abertas (fim do voo, cancelamento, perda total).</summary>
        public IReadOnlyList<string> Flush(double simSeconds)
        {
            var lines = _open.Values.Select(ep => Close(ep, simSeconds)).ToList();
            _open.Clear();
            return lines;
        }

        public void Reset() => _open.Clear();

        private static string Close(Episode ep, double simSeconds) =>
            string.Format(CultureInfo.InvariantCulture,
                "FIM {0} - {1} - {2:F0} s, pico {3:F1} (limite {4:F1}), dano {5:F2}% em {6} peca(s): {7}",
                ep.RuleId, ep.Notes, Math.Max(0.0, simSeconds - ep.StartSimSeconds), ep.Peak, ep.Limit, ep.DamagePct,
                ep.Parts.Count, string.Join(", ", ep.Parts.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)));
    }
}
