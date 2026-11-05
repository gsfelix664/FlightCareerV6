using System;

namespace FlightCareer.Domain.Entities
{
    public enum TransactionType
    {
        Income,     // Entradas (Recompensas de voo)
        Expense,    // Saídas (Manutenção, Combustível, Taxas)
        Transfer    // Transferências manuais (se houver)
    }

    public class FinancialTransaction
    {
        public Guid Id { get; set; }
        public Guid PilotId { get; set; }
        public Pilot? Pilot { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Category { get; set; } = string.Empty; // Ex: "Missão", "Combustível", "Reparo"
        public string? RelatedMissionId { get; set; } // Opcional
    }
}
