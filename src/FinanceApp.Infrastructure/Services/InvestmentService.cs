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

public sealed class InvestmentService(FinanceDbContext dbContext, IAssetPriceService priceService, ICustodyService custodyService) : IInvestmentService
{
    public async Task<IReadOnlyCollection<InvestmentDto>> ListAsync(Guid userId, Guid? walletId, bool? isWatchlist, CancellationToken cancellationToken)
    {
        var query = dbContext.Investments.Where(x => x.UserId == userId && x.IsActive);
        if (walletId.HasValue)
        {
            query = query.Where(x => x.WalletId == walletId.Value);
        }
        if (isWatchlist.HasValue)
        {
            query = query.Where(x => x.IsWatchlist == isWatchlist.Value);
        }

        var entities = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        
        var dtos = new List<InvestmentDto>();
        foreach (var entity in entities)
        {
            var yield = await custodyService.GetAccumulatedYieldAsync(userId, entity.Id, cancellationToken);
            dtos.Add(Map(entity, yield));
        }
        return dtos;
    }

    public async Task<InvestmentDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Investments.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");
        
        var yield = await custodyService.GetAccumulatedYieldAsync(userId, entity.Id, cancellationToken);
        return Map(entity, yield);
    }

    public async Task<InvestmentDto> CreateAsync(Guid userId, CreateInvestmentRequest request, CancellationToken cancellationToken)
    {
        var entity = new Investment(userId, request.WalletId, request.Name, request.Ticker, request.AssetType,
            request.Quantity, request.AveragePrice, request.CurrentPrice, request.CurrencyCode, request.RiskLevel,
            request.IndexerType, request.IndexerRate, request.IndexerAdditionalRate, request.IsWatchlist,
            request.AccountId, request.PurchaseDate, request.IsPinned);
        dbContext.Investments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var yield = await custodyService.GetAccumulatedYieldAsync(userId, entity.Id, cancellationToken);
        return Map(entity, yield);
    }

    public async Task<InvestmentDto> UpdateAsync(Guid userId, Guid id, UpdateInvestmentRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Investments.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");
        entity.Update(request.Name, request.Ticker, request.AssetType, request.Quantity, request.AveragePrice, request.CurrentPrice, request.RiskLevel, request.IsActive,
            request.IndexerType, request.IndexerRate, request.IndexerAdditionalRate, request.IsWatchlist,
            request.AccountId, request.PurchaseDate, request.IsPinned);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var yield = await custodyService.GetAccumulatedYieldAsync(userId, entity.Id, cancellationToken);
        return Map(entity, yield);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Investments.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");
        entity.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<InvestmentDto> TogglePinAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Investments.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");

        bool newPinnedState = !entity.IsPinned;

        if (newPinnedState)
        {
            // Unpin all other investments in the same watchlist mode for this user
            var others = await dbContext.Investments
                .Where(x => x.UserId == userId && x.IsWatchlist == entity.IsWatchlist && x.Id != id && x.IsPinned)
                .ToListAsync(cancellationToken);
            
            foreach (var other in others)
            {
                other.SetPinned(false);
            }
        }

        entity.SetPinned(newPinnedState);
        await dbContext.SaveChangesAsync(cancellationToken);

        var yield = await custodyService.GetAccumulatedYieldAsync(userId, entity.Id, cancellationToken);
        return Map(entity, yield);
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var investments = await dbContext.Investments
            .Where(x => x.UserId == userId && x.IsActive && !x.IsWatchlist)
            .ToListAsync(cancellationToken);

        var totalInvested = investments.Sum(x => x.TotalInvested);
        var currentValue = investments.Sum(x => x.CurrentValue);

        decimal totalYield = 0;
        foreach (var inv in investments)
        {
            totalYield += await custodyService.GetAccumulatedYieldAsync(userId, inv.Id, cancellationToken);
        }

        var gainLoss = currentValue - totalInvested;
        var totalReturn = gainLoss + totalYield;

        return new PortfolioSummaryDto
        {
            TotalInvested = totalInvested,
            CurrentValue = currentValue,
            GainLossPercent = totalInvested == 0 ? 0 : Math.Round((currentValue - totalInvested) / totalInvested * 100, 2),
            ActiveCount = investments.Count,
            TotalYield = totalYield,
            TotalReturn = totalReturn
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

    public async Task<IReadOnlyCollection<InvestmentHistoryPointDto>> GetHistoryAsync(Guid userId, string? category, Guid? investmentId, CancellationToken cancellationToken)
    {
        var query = dbContext.InvestmentSnapshots
            .Where(x => x.UserId == userId && dbContext.Investments.Any(i => i.Id == x.InvestmentId && !i.IsWatchlist));

        if (investmentId.HasValue)
        {
            query = query.Where(x => x.InvestmentId == investmentId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => dbContext.Investments.Any(i => i.Id == x.InvestmentId && i.AssetType == category));
        }

        var snapshots = await query.ToListAsync(cancellationToken);

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

    public async Task<IReadOnlyCollection<CandlestickPointDto>> GetInvestmentCandlesticksAsync(Guid userId, Guid id, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var investment = await dbContext.Investments
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");

        if (string.IsNullOrWhiteSpace(investment.Ticker))
        {
            // No ticker: generate synthetic historical points based on purchase price, current price, and creation date
            var points = new List<CandlestickPointDto>();
            // Use PurchaseDate if available, otherwise fallback to CreatedAt minus 30 days so there's always a nice visual history
            var startDate = investment.PurchaseDate ?? DateOnly.FromDateTime(investment.CreatedAt.DateTime).AddDays(-30);
            
            // Start generating from 'from' or 'startDate', whichever is later, but not in the future
            var current = from > startDate ? from : startDate;
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (current > today)
            {
                current = today.AddDays(-30); // Fallback to last 30 days if start date is in the future
            }

            var totalDays = today.DayNumber - current.DayNumber;
            if (totalDays <= 0) totalDays = 1;

            double startPrice = (double)investment.AveragePrice;
            if (startPrice <= 0) startPrice = (double)investment.CurrentPrice;
            double endPrice = (double)investment.CurrentPrice;
            if (endPrice <= 0) endPrice = startPrice;

            // Determine growth rate based on indexer (matching SnapshotWorker logic)
            double annualRate = 0.0;
            if (investment.IndexerType != null)
            {
                if (investment.IndexerType.Equals("cdi", StringComparison.OrdinalIgnoreCase))
                {
                    var ratePercent = investment.IndexerRate ?? 100m;
                    annualRate = 0.105 * (double)(ratePercent / 100m);
                }
                else if (investment.IndexerType.Equals("ipca", StringComparison.OrdinalIgnoreCase))
                {
                    var addRate = investment.IndexerAdditionalRate ?? 0m;
                    annualRate = 0.045 + (double)(addRate / 100m);
                }
                else if (investment.IndexerType.Equals("pre", StringComparison.OrdinalIgnoreCase))
                {
                    var ratePercent = investment.IndexerRate ?? 0m;
                    annualRate = (double)(ratePercent / 100m);
                }
            }

            double dailyFactor = Math.Pow(1.0 + annualRate, 1.0 / 365.0);

            for (int i = 0; current <= today; i++, current = current.AddDays(1))
            {
                int daysBack = today.DayNumber - current.DayNumber;
                
                double price;
                double prevPrice;

                if (investment.IndexerType != null)
                {
                    // Calculate exact compounding price backwards from CurrentPrice (matching SnapshotWorker)
                    price = endPrice / Math.Pow(dailyFactor, daysBack);
                    prevPrice = endPrice / Math.Pow(dailyFactor, daysBack + 1);
                }
                else
                {
                    // Non-indexed: linear interpolation with no noise
                    price = totalDays == 1 
                        ? startPrice 
                        : startPrice + (endPrice - startPrice) * ((double)i / totalDays);

                    prevPrice = totalDays == 1 
                        ? startPrice 
                        : startPrice + (endPrice - startPrice) * ((double)Math.Max(0, i - 1) / totalDays);
                }

                points.Add(new CandlestickPointDto
                {
                    Date = current,
                    Open = (decimal)prevPrice,
                    High = (decimal)Math.Max(prevPrice, price),
                    Low = (decimal)Math.Min(prevPrice, price),
                    Close = (decimal)price,
                    Volume = 0
                });
            }

            return points;
        }

        return await priceService.GetHistoricalCandlesticksAsync(investment.Ticker, from, to, cancellationToken);
    }

    private static InvestmentDto Map(Investment x, decimal accumulatedYield) => new()
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
        IndexerType = x.IndexerType,
        IndexerRate = x.IndexerRate,
        IndexerAdditionalRate = x.IndexerAdditionalRate,
        IsWatchlist = x.IsWatchlist,
        AccountId = x.AccountId,
        PurchaseDate = x.PurchaseDate,
        IsPinned = x.IsPinned,
        TotalInvested = x.TotalInvested,
        CurrentValue = x.CurrentValue,
        GainLossPercent = x.GainLossPercent,
        LockVersion = x.LockVersion,
        AccumulatedYield = accumulatedYield,
        TotalReturn = (x.CurrentValue - x.TotalInvested) + accumulatedYield
    };
}
