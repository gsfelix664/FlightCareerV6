using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;

namespace FlightCareer.UI.ViewModels
{
    public class DefectRowViewModel
    {
        public AircraftDefect Defect { get; }
        public string Title => Defect.Title;
        public string SeverityLabel => Defect.SeverityLabel;
        public bool IsGrounding => Defect.IsGrounding;
        public string FriendlyText { get; }
        public string TechnicalText { get; }
        public string RepairButtonText { get; }

        public DefectRowViewModel(AircraftDefect defect, DifficultyEconomy economy)
        {
            Defect = defect;
            FriendlyText = DefectText.Friendly(defect);
            TechnicalText = DefectText.Technical(defect);
            RepairButtonText = $"Reparar · {economy.ApplyCost(defect.RepairCost):C0}";
        }
    }
}
