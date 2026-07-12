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
            .Where(x => x.UserId == userId && x.IsActive && !x.IsWatchlist)
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

        // 1. Initial balance snapshot on/before 'from'
        var closestBalanceDate = await dbContext.BalanceSnapshots
            .Where(x => x.UserId == userId && x.SnapshotDate <= from)
            .Select(x => (DateOnly?)x.SnapshotDate)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var initialBalance = closestBalanceDate.HasValue
            ? await dbContext.BalanceSnapshots
                .Where(x => x.UserId == userId && x.SnapshotDate == closestBalanceDate.Value)
                .SumAsync(x => x.Balance, cancellationToken)
            : currentBalance;

        // 2. Initial investment value snapshot on/before 'from'
        var closestInvestmentDate = await dbContext.InvestmentSnapshots
            .Where(x => x.UserId == userId && x.SnapshotDate <= from)
            .Select(x => (DateOnly?)x.SnapshotDate)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var initialInvestmentValue = closestInvestmentDate.HasValue
            ? await dbContext.InvestmentSnapshots
                .Where(x => x.UserId == userId && x.SnapshotDate == closestInvestmentDate.Value && dbContext.Investments.Any(i => i.Id == x.InvestmentId && !i.IsWatchlist))
                .SumAsync(x => x.Value, cancellationToken)
            : investedNetWorth;

        var initialNetWorth = initialBalance + initialInvestmentValue;
        var finalNetWorth = currentBalance + investedNetWorth;

        // 3. Investment yields confirmed during the period
        var monthYields = transactions
            .Where(x => x.TransactionType == TransactionTypes.InvestmentYield && x.Status == TransactionStatuses.Confirmed)
            .Sum(x => x.EffectiveAmount);

        // 4. Calculate market price variation:
        // Final Net Worth = Initial Net Worth + Income - Expenses + Yields + Market Variation
        // So Market Variation = Final Net Worth - Initial Net Worth - Income + Expenses - Yields
        var marketVariation = finalNetWorth - initialNetWorth - monthIncome + monthExpenses - monthYields;

        var waterfallPoints = new List<WaterfallPointDto>
        {
            new() { Label = "Inicial", Value = initialNetWorth, Type = "start" },
            new() { Label = "Receitas", Value = monthIncome, Type = "increase" },
            new() { Label = "Despesas", Value = monthExpenses, Type = "decrease" },
            new() { Label = "Rendimentos", Value = monthYields, Type = "increase" }
        };

        if (marketVariation >= 0)
        {
            waterfallPoints.Add(new() { Label = "Var. Mercado", Value = marketVariation, Type = "increase" });
        }
        else
        {
            waterfallPoints.Add(new() { Label = "Var. Mercado", Value = Math.Abs(marketVariation), Type = "decrease" });
        }

        waterfallPoints.Add(new() { Label = "Final", Value = finalNetWorth, Type = "end" });

        return new DashboardOverviewDto
        {
            CurrentBalance = currentBalance,
            ProjectedBalance = projectedBalance,
            MonthIncome = monthIncome,
            MonthExpenses = monthExpenses,
            NetResult = monthIncome - monthExpenses,
            InvestedNetWorth = investedNetWorth,
            PendingRecurrences = transactions.Count(x => x.Status == TransactionStatuses.Planned),
            CriticalAlerts = 0,
            ExpenseByCategory = expenseByCategory,
            CashflowSeries = series,
            NetWorthSeries = netWorthSeries,
            WaterfallSeries = waterfallPoints
        };
    }
}
