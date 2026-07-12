using System;

namespace FinanceApp.Contracts.Projections;

public sealed class ProjectionPointDto
{
    public required DateOnly Date { get; init; }
    public required decimal ProjectedBalance { get; init; }
    public required decimal ProjectedIncome { get; init; }
    public required decimal ProjectedExpense { get; init; }
    public required decimal ProjectedInvestments { get; init; }
}
