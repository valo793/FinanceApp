using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Investments;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class InvestmentService(FinanceDbContext dbContext, IAssetPriceService priceService) : IInvestmentService
{
    public async Task<IReadOnlyCollection<InvestmentDto>> ListAsync(Guid userId, Guid? walletId, CancellationToken cancellationToken)
    {
        var query = dbContext.Investments.Where(x => x.UserId == userId && x.IsActive);
        if (walletId.HasValue)
        {
            query = query.Where(x => x.WalletId == walletId.Value);
        }

        var entities = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    public async Task<InvestmentDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Investments.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");
        return Map(entity);
    }

    public async Task<InvestmentDto> CreateAsync(Guid userId, CreateInvestmentRequest request, CancellationToken cancellationToken)
    {
        var entity = new Investment(userId, request.WalletId, request.Name, request.Ticker, request.AssetType,
            request.Quantity, request.AveragePrice, request.CurrentPrice, request.CurrencyCode, request.RiskLevel);
        dbContext.Investments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<InvestmentDto> UpdateAsync(Guid userId, Guid id, UpdateInvestmentRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Investments.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");
        entity.Update(request.Name, request.Ticker, request.AssetType, request.Quantity, request.AveragePrice, request.CurrentPrice, request.RiskLevel, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Investments.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");
        entity.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var investments = await dbContext.Investments
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync(cancellationToken);

        var totalInvested = investments.Sum(x => x.TotalInvested);
        var currentValue = investments.Sum(x => x.CurrentValue);

        return new PortfolioSummaryDto
        {
            TotalInvested = totalInvested,
            CurrentValue = currentValue,
            GainLossPercent = totalInvested == 0 ? 0 : Math.Round((currentValue - totalInvested) / totalInvested * 100, 2),
            ActiveCount = investments.Count
        };
    }

    public async Task SyncPricesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var investments = await dbContext.Investments
            .Where(x => x.UserId == userId && x.IsActive && x.Ticker != null && x.Ticker != "")
            .ToListAsync(cancellationToken);

        var tickers = investments.Select(x => x.Ticker!).Distinct().ToList();
        if (tickers.Count == 0) return;

        var prices = await priceService.GetPricesAsync(tickers, cancellationToken);
        foreach (var investment in investments)
        {
            if (prices.TryGetValue(investment.Ticker!, out var price))
            {
                investment.UpdateCurrentPrice(price);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<InvestmentHistoryPointDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshots = await dbContext.InvestmentSnapshots
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var history = snapshots
            .GroupBy(x => x.SnapshotDate)
            .Select(g => new InvestmentHistoryPointDto
            {
                Date = g.Key,
                Value = g.Sum(x => x.Value)
            })
            .OrderBy(x => x.Date)
            .ToList();

        if (history.Count == 0)
        {
            var summary = await GetPortfolioSummaryAsync(userId, cancellationToken);
            history.Add(new InvestmentHistoryPointDto
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Value = summary.CurrentValue
            });
        }

        return history;
    }

    private static InvestmentDto Map(Investment x) => new()
    {
        Id = x.Id,
        WalletId = x.WalletId,
        Name = x.Name,
        Ticker = x.Ticker,
        AssetType = x.AssetType,
        Quantity = x.Quantity,
        AveragePrice = x.AveragePrice,
        CurrentPrice = x.CurrentPrice,
        CurrencyCode = x.CurrencyCode,
        RiskLevel = x.RiskLevel,
        IsActive = x.IsActive,
        TotalInvested = x.TotalInvested,
        CurrentValue = x.CurrentValue,
        GainLossPercent = x.GainLossPercent,
        LockVersion = x.LockVersion
    };
}
