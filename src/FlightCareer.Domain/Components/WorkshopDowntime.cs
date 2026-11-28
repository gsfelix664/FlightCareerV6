using System;
using System.Globalization;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Regras e formatação de tempo de parada na oficina em tempo de jogo (OFICINA-TEMPO-JOGO).
    /// O atendimento sai pronto na hora real; o tempo de carreira avança e envelhece o quadro de missões.
    /// </summary>
    public static class WorkshopDowntime
    {
        public const double ExpressCostMultiplier = 1.5;

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        /// <summary>
        /// Aplica os parâmetros do serviço conforme o modo (Normal ou Expresso).
        /// Serviço expresso: metade das horas de carreira e +50% no preço (fator 1,5).
        /// Normal: horas mantidas e custo padrão (fator 1,0).
        /// </summary>
        public static (double gameHours, double costFactor) Apply(double gameHours, bool express)
        {
            if (express)
            {
                return (gameHours / 2.0, ExpressCostMultiplier);
            }
            return (gameHours, 1.0);
        }

        /// <summary>
        /// Formata as horas no padrão "0.##" em cultura pt-BR seguido de " h".
        /// Ex: 72 -> "72 h", 0.5 -> "0,5 h", 0.25 -> "0,25 h", 8 -> "8 h".
        /// </summary>
        public static string FormatHours(double hours)
        {
            return $"{hours.ToString("0.##", PtBr)} h";
        }

        /// <summary>
        /// Retorna o resumo de conclusão do serviço da oficina, com as horas de carreira gastas,
        /// a data atualizada da carreira e o impacto nos contratos do quadro de missões.
        /// </summary>
        public static string CompletionSummary(double gameHours, DateTime careerClock, int expiredContracts)
        {
            string formattedHours = FormatHours(gameHours);
            string formattedDate = careerClock.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            string contractsNotice = expiredContracts switch
            {
                0 => "Nenhum contrato do quadro venceu nesse tempo.",
                1 => "1 contrato do quadro venceu nesse tempo.",
                _ => $"{expiredContracts} contratos do quadro venceram nesse tempo."
            };

            return $"A oficina levou {formattedHours} de carreira. Dia da carreira: {formattedDate}.\n{contractsNotice}";
        }
    }
}
