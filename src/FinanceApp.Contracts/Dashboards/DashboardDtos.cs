namespace FinanceApp.Contracts.Dashboards;

public sealed class DashboardOverviewDto
{
    public decimal CurrentBalance { get; init; }
    public decimal ProjectedBalance { get; init; }
    public decimal MonthIncome { get; init; }
    public decimal MonthExpenses { get; init; }
    public decimal NetResult { get; init; }
    public decimal InvestedNetWorth { get; init; }
    public int PendingRecurrences { get; init; }
    public int CriticalAlerts { get; init; }
    public IReadOnlyCollection<CategoryAmountDto> ExpenseByCategory { get; init; } = [];
    public IReadOnlyCollection<TimeSeriesPointDto> CashflowSeries { get; init; } = [];
    public IReadOnlyCollection<NetWorthPointDto> NetWorthSeries { get; init; } = [];
}

public sealed class NetWorthPointDto
{
    public DateOnly Date { get; init; }
    public decimal Balance { get; init; }
}

public sealed class CategoryAmountDto
{
    public required string Name { get; init; }
    public decimal Amount { get; init; }
    public decimal Percentage { get; init; }
    public decimal? BudgetLimit { get; init; }
    public decimal BudgetPercentage => (BudgetLimit.HasValue && BudgetLimit.Value > 0) ? Math.Min(Math.Round(Amount / BudgetLimit.Value * 100, 1), 100) : 0;
}

public sealed class TimeSeriesPointDto
{
    public DateOnly Date { get; init; }
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
    public decimal Balance { get; init; }
}
