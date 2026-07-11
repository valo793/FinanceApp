using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Projections;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class ProjectionService(FinanceDbContext dbContext) : IProjectionService
{
    public async Task<IReadOnlyCollection<ProjectionPointDto>> GetProjectionAsync(Guid userId, int months, CancellationToken cancellationToken)
    {
        if (months <= 0) months = 6;
        if (months > 24) months = 24; // Cap at 2 years

        var today = DateOnly.FromDateTime(DateTime.Today);
        var endDate = today.AddMonths(months);

        // 1. Get current starting balance (sum of all active account balances)
        var startingBalance = await dbContext.Accounts
            .Where(x => x.UserId == userId && !x.IsDeleted && x.IsActive)
            .SumAsync(x => x.CurrentBalanceCached, cancellationToken);

        // 2. Load all existing future planned transactions
        var plannedTransactions = await dbContext.Transactions
            .Where(x => x.UserId == userId && !x.IsDeleted && x.Status == TransactionStatuses.Planned && x.CompetenceDate >= today && x.CompetenceDate <= endDate)
            .ToListAsync(cancellationToken);

        // 3. Load all active recurring templates
        var recurringTemplates = await dbContext.RecurringTransactions
            .Where(x => x.UserId == userId && x.IsActive && !x.IsPaused)
            .ToListAsync(cancellationToken);

        // 4. Create a list to collect all future cash flows
        var cashFlows = new List<(DateOnly Date, decimal SignedAmount)>();

        // Add existing planned transactions
        foreach (var t in plannedTransactions)
        {
            cashFlows.Add((t.CompetenceDate, t.SignedAmount()));
        }

        // Project future runs of recurring templates, avoiding double counting with already materialized ones
        foreach (var temp in recurringTemplates)
        {
            var existingMax = plannedTransactions
                .Where(t => t.RecurringTransactionId == temp.Id)
                .Select(t => (DateOnly?)t.CompetenceDate)
                .DefaultIfEmpty()
                .Max();

            var simDate = temp.NextRunDate;
            if (existingMax.HasValue)
            {
                // Advance simulation start date until it is past the already materialized planned transactions
                while (simDate <= existingMax.Value)
                {
                    simDate = AdvanceDate(simDate, temp.Frequency, temp.IntervalValue);
                }
            }

            // Simulate runs up to the end of the projection window
            while (simDate <= endDate)
            {
                if (simDate >= today && (temp.EndDate is null || simDate <= temp.EndDate))
                {
                    decimal signedAmount = temp.TransactionKind == "income" ? temp.DefaultAmount : temp.DefaultAmount * -1;
                    cashFlows.Add((simDate, signedAmount));
                }
                simDate = AdvanceDate(simDate, temp.Frequency, temp.IntervalValue);
            }
        }

        // 5. Group cash flows by date
        var cashFlowsByDate = cashFlows
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SignedAmount));

        // 6. Generate daily points
        var points = new List<ProjectionPointDto>();
        var currentBalance = startingBalance;
        var currentDate = today;

        while (currentDate <= endDate)
        {
            decimal income = 0;
            decimal expense = 0;

            if (cashFlowsByDate.TryGetValue(currentDate, out var signedSum))
            {
                currentBalance += signedSum;
                if (signedSum > 0) income = signedSum;
                else expense = Math.Abs(signedSum);
            }

            points.Add(new ProjectionPointDto
            {
                Date = currentDate,
                ProjectedBalance = currentBalance,
                ProjectedIncome = income,
                ProjectedExpense = expense
            });

            currentDate = currentDate.AddDays(1);
        }

        return points;
    }

    private static DateOnly AdvanceDate(DateOnly date, string frequency, int intervalValue)
    {
        return frequency switch
        {
            "weekly" => date.AddDays(7 * intervalValue),
            "biweekly" => date.AddDays(14 * intervalValue),
            "monthly" => date.AddMonths(1 * intervalValue),
            "yearly" => date.AddYears(1 * intervalValue),
            _ => date.AddMonths(1 * intervalValue)
        };
    }
}
