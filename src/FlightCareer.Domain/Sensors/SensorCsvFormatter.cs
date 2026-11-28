using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FlightCareer.Domain.Sensors
{
    /// <summary>
    /// Formatador determinístico para exportação de leituras de telemetria em formato CSV.
    /// Garante conformidade com RFC 4180, cultura invariante e sanitização de nomes de arquivos.
    /// </summary>
    public static class SensorCsvFormatter
    {
        private static readonly HashSet<char> WindowsInvalidFileNameChars = new(new[]
        {
            '\"', '<', '>', '|', ':', '*', '?', '\\', '/'
        });

        /// <summary>
        /// Gera a linha de cabeçalho do arquivo CSV: utc,sim_rate,&lt;keys...&gt;,marker
        /// </summary>
        public static string Header(IReadOnlyList<string> keys)
        {
            var sb = new StringBuilder();
            sb.Append("utc,sim_rate");

            if (keys != null && keys.Count > 0)
            {
                foreach (var key in keys)
                {
                    sb.Append(',');
                    sb.Append(key);
                }
            }

            sb.Append(",marker");
            return sb.ToString();
        }

        /// <summary>
        /// Formata uma linha de dados do frame no padrão CSV com cultura invariante e formato 'R' para números.
        /// Chaves ausentes, NaN ou infinitos resultam em células vazias.
        /// </summary>
        public static string Row(DateTime utc, SensorFrame frame, IReadOnlyList<string> keys, string? marker)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            var sb = new StringBuilder();
            sb.Append(utc.ToString("O", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(FormatNumber(frame.SimulationRate));

            if (keys != null && keys.Count > 0)
            {
                foreach (var key in keys)
                {
                    sb.Append(',');
                    if (frame.Values != null && frame.Values.TryGetValue(key, out double val))
                    {
                        sb.Append(FormatNumber(val));
                    }
                }
            }

            sb.Append(',');
            sb.Append(EscapeMarker(marker));

            return sb.ToString();
        }

        /// <summary>
        /// Escapa um texto de marcador para conformidade com RFC 4180.
        /// Aspas duplas são duplicadas e o texto é envolvido em aspas se contiver vírgula, quebra de linha ou aspas.
        /// </summary>
        public static string EscapeMarker(string? marker)
        {
            if (string.IsNullOrEmpty(marker))
                return string.Empty;

            if (marker.Contains('"') || marker.Contains(',') || marker.Contains('\r') || marker.Contains('\n'))
            {
                return $"\"{marker.Replace("\"", "\"\"")}\"";
            }

            return marker;
        }

        /// <summary>
        /// Sanitiza um título de aeronave para uso seguro como nome de arquivo no Windows.
        /// Substitui espaços por '_', remove caracteres inválidos do Windows, limita a 60 caracteres.
        /// Retorna 'UNKNOWN' se o resultado for vazio ou nulo.
        /// </summary>
        public static string SafeFileName(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "UNKNOWN";

            // Troca espaços por '_'
            string withUnderscores = title.Replace(' ', '_');

            // Remove caracteres inválidos do Windows e caracteres de controle (< 32)
            var sb = new StringBuilder(withUnderscores.Length);
            foreach (char c in withUnderscores)
            {
                if (c >= 32 && !WindowsInvalidFileNameChars.Contains(c))
                {
                    sb.Append(c);
                }
            }

            string sanitized = sb.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(sanitized))
                return "UNKNOWN";

            if (sanitized.Length > 60)
            {
                sanitized = sanitized.Substring(0, 60);
            }

            return sanitized;
        }

        private static string FormatNumber(double val)
        {
            if (double.IsNaN(val) || double.IsInfinity(val))
                return string.Empty;

            return val.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
