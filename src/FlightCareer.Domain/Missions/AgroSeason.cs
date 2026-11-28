using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Missions
{
    public enum SafraPhase
    {
        Plantio,
        Colheita,
        Entressafra
    }

    /// <summary>
    /// Modelo simplificado de safra agrícola baseado no calendário da soja e milho de verão do Brasil (hemisfério sul),
    /// invertido para o hemisfério norte por deslocamento de 6 meses conforme sinal da latitude e guiado pelo relógio de carreira do piloto.
    /// </summary>
    public static class AgroSeason
    {
        public const string PhaseTagPrefix = "safra:";
        public const string MonthTagPrefix = "safra:mes:";

        /// <summary>
        /// Calendário escrito no hemisfério sul (safra de verão brasileira). Latitude >= 0: mês deslocado 6 meses.
        /// </summary>
        public static int SouthernEquivalentMonth(int month, double latitude)
        {
            if (latitude < 0)
            {
                return month;
            }

            return ((month + 5) % 12) + 1;
        }

        /// <summary>
        /// Fase da safra para o mês equivalente austral: 9-12 Plantio; 1-4 Colheita; 5-8 Entressafra.
        /// </summary>
        public static SafraPhase PhaseFor(int southernMonth)
        {
            if (southernMonth >= 9 && southernMonth <= 12)
            {
                return SafraPhase.Plantio;
            }

            if (southernMonth >= 1 && southernMonth <= 4)
            {
                return SafraPhase.Colheita;
            }

            return SafraPhase.Entressafra;
        }

        /// <summary>
        /// Gera as etiquetas de safra {"safra:<fase>", "safra:mes:<m>"} para a data e latitude fornecidas.
        /// </summary>
        public static IReadOnlySet<string> TagsFor(DateTime careerDate, double latitude)
        {
            int southernMonth = SouthernEquivalentMonth(careerDate.Month, latitude);
            SafraPhase phase = PhaseFor(southernMonth);

            string phaseTag = phase switch
            {
                SafraPhase.Plantio => $"{PhaseTagPrefix}plantio",
                SafraPhase.Colheita => $"{PhaseTagPrefix}colheita",
                _ => $"{PhaseTagPrefix}entressafra"
            };

            string monthTag = $"{MonthTagPrefix}{southernMonth}";

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                phaseTag,
                monthTag
            };
        }

        /// <summary>
        /// Combina as etiquetas de origem com as etiquetas de safra. Se seasonDate for null, devolve a mesma instância de tags.
        /// </summary>
        public static IReadOnlySet<string> WithSeasonTags(IReadOnlySet<string> tags, DateTime? seasonDate, double latitude)
        {
            if (!seasonDate.HasValue)
            {
                return tags;
            }

            var merged = new HashSet<string>(tags ?? (IReadOnlySet<string>)new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            foreach (var seasonTag in TagsFor(seasonDate.Value, latitude))
            {
                merged.Add(seasonTag);
            }

            return merged;
        }

        /// <summary>
        /// Retorna nota contextual da fase da safra com espaço inicial ou string vazia se não houver etiqueta de fase.
        /// </summary>
        public static string Note(IReadOnlySet<string>? tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return "";
            }

            if (tags.Contains($"{PhaseTagPrefix}plantio"))
            {
                return " Época de plantio da safra.";
            }

            if (tags.Contains($"{PhaseTagPrefix}colheita"))
            {
                return " Época de colheita da safra.";
            }

            if (tags.Contains($"{PhaseTagPrefix}entressafra"))
            {
                return " Entressafra: manutenção de máquinas e preparo do solo.";
            }

            return "";
        }
    }
}
