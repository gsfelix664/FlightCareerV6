using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FlightCareer.Domain.Sensors.Probe
{
    public sealed record ProbeReportRow(
        string Slot,
        string SimVar,
        int? Index,
        string Unit,
        bool IsString,
        string Source,
        string Category,
        bool? Settable,
        ProbeStatus Status,
        string? Exception,
        long Samples,
        int Distinct,
        double? Min,
        double? Max,
        string? First,
        string? Last);

    public sealed class ProbeValidationResult
    {
        public bool ReceivedResponse { get; set; }
        public bool IsRejected { get; set; }
        public string? ExceptionMessage { get; set; }

        public static ProbeValidationResult Accepted() => new() { ReceivedResponse = true, IsRejected = false };
        public static ProbeValidationResult Rejected(string? exception = null) => new() { ReceivedResponse = true, IsRejected = true, ExceptionMessage = exception ?? "NAME_UNRECOGNIZED" };
        public static ProbeValidationResult Error(string exception) => new() { ReceivedResponse = true, IsRejected = false, ExceptionMessage = exception };
        public static ProbeValidationResult NoResponse() => new() { ReceivedResponse = false, IsRejected = false };
    }

    public static class ProbeReportBuilder
    {
        public static ProbeStatus DetermineStatus(
            ProbeValidationResult? validation,
            ProbeSlotStats? stats)
        {
            if (validation != null)
            {
                if (validation.IsRejected)
                {
                    return ProbeStatus.Rejeitada;
                }

                if (!string.IsNullOrWhiteSpace(validation.ExceptionMessage) && !validation.ReceivedResponse)
                {
                    return ProbeStatus.Erro;
                }

                if (!string.IsNullOrWhiteSpace(validation.ExceptionMessage) && validation.ExceptionMessage != "NAME_UNRECOGNIZED")
                {
                    // Erro registrado
                    return ProbeStatus.Erro;
                }

                if (!validation.ReceivedResponse)
                {
                    return ProbeStatus.SemResposta;
                }
            }

            if (stats == null || stats.Samples == 0)
            {
                return ProbeStatus.AceitaSemAmostras;
            }

            return stats.EvaluateAcceptedStatus();
        }

        public static IReadOnlyList<ProbeReportRow> BuildRows(
            IEnumerable<ProbeSlot> slots,
            IReadOnlyDictionary<string, ProbeValidationResult>? validationResults,
            ProbeStatsAccumulator? accumulator)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            var rows = new List<ProbeReportRow>();

            foreach (var slot in slots)
            {
                ProbeValidationResult? validation = null;
                validationResults?.TryGetValue(slot.Name, out validation);

                var stats = accumulator?.Get(slot.Name);

                ProbeStatus status = DetermineStatus(validation, stats);

                string? exception = validation?.ExceptionMessage;

                long samples = stats?.Samples ?? 0;
                int distinct = stats?.DistinctCount ?? 0;
                double? min = stats?.Min;
                double? max = stats?.Max;

                string? firstStr = null;
                string? lastStr = null;

                if (slot.Entry.IsString)
                {
                    firstStr = stats?.FirstString;
                    lastStr = stats?.LastString;
                }
                else
                {
                    firstStr = stats?.First?.ToString("G", CultureInfo.InvariantCulture);
                    lastStr = stats?.Last?.ToString("G", CultureInfo.InvariantCulture);
                }

                rows.Add(new ProbeReportRow(
                    Slot: slot.Name,
                    SimVar: slot.Entry.SimVar,
                    Index: slot.Index,
                    Unit: slot.Entry.Unit,
                    IsString: slot.Entry.IsString,
                    Source: slot.Entry.Source,
                    Category: slot.Entry.Category,
                    Settable: slot.Entry.Settable,
                    Status: status,
                    Exception: exception,
                    Samples: samples,
                    Distinct: distinct,
                    Min: min,
                    Max: max,
                    First: firstStr,
                    Last: lastStr));
            }

            return rows;
        }

        public static string GenerateReportCsv(IReadOnlyList<ProbeReportRow> rows)
        {
            var sb = new StringBuilder();
            // cabeçalho: slot,simvar,index,unit,isString,source,category,settable,status,exception,samples,distinct,min,max,first,last
            sb.AppendLine("slot,simvar,index,unit,isString,source,category,settable,status,exception,samples,distinct,min,max,first,last");

            foreach (var r in rows)
            {
                var fields = new string[]
                {
                    r.Slot,
                    r.SimVar,
                    r.Index.HasValue ? r.Index.Value.ToString() : string.Empty,
                    r.Unit,
                    r.IsString ? "true" : "false",
                    r.Source,
                    r.Category,
                    r.Settable.HasValue ? (r.Settable.Value ? "true" : "false") : string.Empty,
                    r.Status.ToString(),
                    r.Exception ?? string.Empty,
                    r.Samples.ToString(),
                    r.Distinct.ToString(),
                    r.Min.HasValue ? r.Min.Value.ToString("G", CultureInfo.InvariantCulture) : string.Empty,
                    r.Max.HasValue ? r.Max.Value.ToString("G", CultureInfo.InvariantCulture) : string.Empty,
                    r.First ?? string.Empty,
                    r.Last ?? string.Empty
                };

                sb.AppendLine(ProbeCsvWriter.FormatCsvRow(fields));
            }

            return sb.ToString();
        }

        public static string GenerateSummaryText(
            IReadOnlyList<ProbeReportRow> rows,
            string? aircraftTitle,
            TimeSpan duration)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== RELATÓRIO DE SONDAGEM DE SIMVARS (SimVarProbe) ===");
            sb.AppendLine($"Aeronave: {aircraftTitle ?? "(desconhecida/sem simulação)"}");
            sb.AppendLine($"Duração: {duration:hh\\:mm\\:ss}");
            sb.AppendLine($"Total de variáveis avaliadas: {rows.Count}");
            sb.AppendLine();

            var statusCounts = rows
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            sb.AppendLine("Totais por status:");
            foreach (ProbeStatus status in Enum.GetValues<ProbeStatus>())
            {
                int count = statusCounts.TryGetValue(status, out int c) ? c : 0;
                sb.AppendLine($"  - {status}: {count}");
            }
            sb.AppendLine();

            var rejected = rows.Where(r => r.Status == ProbeStatus.Rejeitada).ToList();
            sb.AppendLine($"SimVars Rejeitadas ({rejected.Count}):");
            if (rejected.Count == 0)
            {
                sb.AppendLine("  (nenhuma)");
            }
            else
            {
                foreach (var r in rejected)
                {
                    sb.AppendLine($"  - {r.Slot} ({r.Exception ?? "NAME_UNRECOGNIZED"})");
                }
            }

            var errors = rows.Where(r => r.Status == ProbeStatus.Erro).ToList();
            if (errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"SimVars com Erro ({errors.Count}):");
                foreach (var r in errors)
                {
                    sb.AppendLine($"  - {r.Slot} ({r.Exception})");
                }
            }

            var noResponse = rows.Where(r => r.Status == ProbeStatus.SemResposta).ToList();
            if (noResponse.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"SimVars Sem Resposta ({noResponse.Count}):");
                foreach (var r in noResponse)
                {
                    sb.AppendLine($"  - {r.Slot}");
                }
            }

            return sb.ToString();
        }
    }
}
