using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Reconstructs investment positions from transaction history using
/// weighted average price (preço médio ponderado — Brazilian IR standard).
/// </summary>
public sealed class CustodyService(FinanceDbContext dbContext) : ICustodyService
{
    public async Task RecalculatePositionAsync(Guid userId, Guid investmentId, CancellationToken ct)
    {
        var investment = await dbContext.Investments
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == investmentId, ct)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");

        var transactions = await dbContext.Transactions
            .Where(x => x.UserId == userId
                && x.InvestmentId == investmentId
                && !x.IsDeleted
                && x.Status == TransactionStatuses.Confirmed
                && (x.TransactionType == TransactionTypes.InvestmentBuy
                    || x.TransactionType == TransactionTypes.InvestmentSell))
            .OrderBy(x => x.CompetenceDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);

        decimal quantity = 0;
        decimal averagePrice = 0;

        foreach (var tx in transactions)
        {
            var txQty = tx.InvestmentQuantity ?? 0;
            var txPrice = tx.UnitPrice ?? 0;

            if (tx.TransactionType == TransactionTypes.InvestmentBuy)
            {
                // Preço Médio Ponderado: new avg = (old_qty * old_avg + buy_qty * buy_price) / (old_qty + buy_qty)
                var totalCost = (quantity * averagePrice) + (txQty * txPrice);
                quantity += txQty;
                averagePrice = quantity > 0 ? totalCost / quantity : 0;
            }
            else if (tx.TransactionType == TransactionTypes.InvestmentSell)
            {
                quantity -= txQty;
                // Average price does NOT change on sells (Brazilian standard)
                if (quantity <= 0)
                {
                    quantity = 0;
                    averagePrice = 0;
                }
            }
        }

        investment.UpdatePosition(quantity, Math.Round(averagePrice, 4));
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task RecalculateAllPositionsAsync(Guid userId, CancellationToken ct)
    {
        var investmentIds = await dbContext.Investments
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var id in investmentIds)
        {
            await RecalculatePositionAsync(userId, id, ct);
        }
    }

    public async Task<decimal> GetAccumulatedYieldAsync(Guid userId, Guid investmentId, CancellationToken ct)
    {
        return await dbContext.Transactions
            .Where(x => x.UserId == userId
                && x.InvestmentId == investmentId
                && !x.IsDeleted
                && x.Status == TransactionStatuses.Confirmed
                && x.TransactionType == TransactionTypes.InvestmentYield)
            .SumAsync(x => x.AmountActual ?? x.AmountExpected ?? 0m, ct);
    }

    public async Task<decimal?> GetHistoricalAnnualReturnAsync(Guid userId, Guid investmentId, CancellationToken ct)
    {
        var snapshots = await dbContext.InvestmentSnapshots
            .Where(x => x.UserId == userId && x.InvestmentId == investmentId)
            .OrderBy(x => x.SnapshotDate)
            .ToListAsync(ct);

        if (snapshots.Count < 2)
            return null;

        var first = snapshots.First();
        var last = snapshots.Last();
        var days = last.SnapshotDate.DayNumber - first.SnapshotDate.DayNumber;

        if (days < 30 || first.Value <= 0)
            return null;

        // Annualized return: (Vf / Vi)^(365/days) - 1
        var ratio = (double)(last.Value / first.Value);
        var annualReturn = Math.Pow(ratio, 365.0 / days) - 1.0;

        return (decimal)Math.Round(annualReturn, 6);
    }
}
