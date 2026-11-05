using CommunityToolkit.Mvvm.ComponentModel;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.UI.ViewModels
{
    public partial class FinanceViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private Guid _pilotId;

        [ObservableProperty] private ObservableCollection<FinancialTransaction> _transactions = new();
        [ObservableProperty] private double _totalIncome;
        [ObservableProperty] private double _totalExpenses;
        [ObservableProperty] private double _netProfit;

        public FinanceViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync(Guid pilotId)
        {
            _pilotId = pilotId;
            await LoadTransactionsAsync();
        }

        public async Task LoadTransactionsAsync()
        {
            var transactions = await _context.FinancialTransactions
                .Where(t => t.PilotId == _pilotId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            Transactions = new ObservableCollection<FinancialTransaction>(transactions);
            
            TotalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            TotalExpenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            NetProfit = TotalIncome - TotalExpenses;
        }
    }
}
