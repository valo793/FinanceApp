using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Dashboards;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class DashboardService(FinanceDbContext dbContext) : IDashboardService
{
    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .Where(x => x.UserId == userId && !x.IsDeleted && x.IsActive)
            .ToListAsync(cancellationToken);

        var transactions = await dbContext.Transactions
            .Where(x => x.UserId == userId && !x.IsDeleted && x.CompetenceDate >= from && x.CompetenceDate <= to)
            .ToListAsync(cancellationToken);

        var expenseCategories = await dbContext.ExpenseCategories
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        var investments = await dbContext.Investments
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync(cancellationToken);

        var currentBalance = accounts.Sum(x => x.CurrentBalanceCached);
        var monthIncome = transactions.Where(x => x.TransactionType == TransactionTypes.Income && x.Status == TransactionStatuses.Confirmed).Sum(x => x.EffectiveAmount);
        var monthExpenses = transactions.Where(x => x.TransactionType == TransactionTypes.Expense && x.Status == TransactionStatuses.Confirmed).Sum(x => x.EffectiveAmount);

        var plannedIncome = transactions.Where(x => x.TransactionType == TransactionTypes.Income && x.Status == TransactionStatuses.Planned).Sum(x => x.EffectiveAmount);
        var plannedExpenses = transactions.Where(x => x.TransactionType == TransactionTypes.Expense && x.Status == TransactionStatuses.Planned).Sum(x => x.EffectiveAmount);
        var projectedBalance = currentBalance + plannedIncome - plannedExpenses;
        var investedNetWorth = investments.Sum(x => x.Quantity * x.CurrentPrice);

        var totalExpenseAmount = transactions.Where(x => x.TransactionType == TransactionTypes.Expense).Sum(x => x.EffectiveAmount);
        var expenseByCategory = transactions
            .Where(x => x.TransactionType == TransactionTypes.Expense)
            .GroupBy(x => x.ExpenseCategoryId)
            .Select(g =>
            {
                var category = g.Key.HasValue && expenseCategories.TryGetValue(g.Key.Value, out var cat) ? cat : null;
                var categoryName = category?.Name ?? "Sem categoria";
                var budgetLimit = category?.MonthlyBudgetLimit;
                var amount = g.Sum(x => x.EffectiveAmount);
                return new CategoryAmountDto
                {
                    Name = categoryName,
                    Amount = amount,
                    Percentage = totalExpenseAmount == 0 ? 0 : Math.Round(amount / totalExpenseAmount * 100, 1),
                    BudgetLimit = budgetLimit
                };
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var series = transactions
            .GroupBy(x => x.CompetenceDate)
            .OrderBy(x => x.Key)
            .Select(g => new TimeSeriesPointDto
            {
                Date = g.Key,
                Income = g.Where(x => x.TransactionType == TransactionTypes.Income).Sum(x => x.EffectiveAmount),
                Expense = g.Where(x => x.TransactionType == TransactionTypes.Expense).Sum(x => x.EffectiveAmount),
                Balance = g.Where(x => x.TransactionType == TransactionTypes.Income).Sum(x => x.EffectiveAmount)
                        - g.Where(x => x.TransactionType == TransactionTypes.Expense).Sum(x => x.EffectiveAmount)
            })
            .ToList();

        // Load historical balance snapshots for net worth series
        var snapshots = await dbContext.BalanceSnapshots
            .Where(x => x.UserId == userId && x.SnapshotDate >= from && x.SnapshotDate <= to)
            .ToListAsync(cancellationToken);

        var netWorthSeries = snapshots
            .GroupBy(x => x.SnapshotDate)
            .Select(g => new NetWorthPointDto
            {
                Date = g.Key,
                Balance = g.Sum(x => x.Balance)
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Fallback for new users or empty snapshots
        if (netWorthSeries.Count == 0)
        {
            netWorthSeries.Add(new NetWorthPointDto
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Balance = currentBalance
            });
        }

        return new DashboardOverviewDto
        {
            CurrentBalance = currentBalance,
            ProjectedBalance = projectedBalance,
            MonthIncome = monthIncome,
            MonthExpenses = monthExpenses,
            NetResult = monthIncome - monthExpenses,
            InvestedNetWorth = investedNetWorth,
            PendingRecurrences = await dbContext.RecurringTransactions.CountAsync(x => x.UserId == userId && x.IsActive && !x.IsPaused, cancellationToken),
            CriticalAlerts = 0,
            ExpenseByCategory = expenseByCategory,
            CashflowSeries = series,
            NetWorthSeries = netWorthSeries
        };
    }
}
