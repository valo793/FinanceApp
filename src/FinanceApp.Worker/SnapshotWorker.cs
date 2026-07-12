using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Worker;

public sealed class SnapshotWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SnapshotWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Balance Snapshot worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                var priceService = scope.ServiceProvider.GetRequiredService<IAssetPriceService>();
                var today = DateOnly.FromDateTime(DateTime.Today);

                var users = await dbContext.Users
                    .Where(x => x.Status == "active")
                    .ToListAsync(stoppingToken);

                foreach (var user in users)
                {
                    var accounts = await dbContext.Accounts
                        .Where(x => x.UserId == user.Id && !x.IsDeleted && x.IsActive)
                        .ToListAsync(stoppingToken);

                    foreach (var account in accounts)
                    {
                        var lastSnapshot = await dbContext.BalanceSnapshots
                            .Where(x => x.AccountId == account.Id)
                            .OrderByDescending(x => x.SnapshotDate)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (lastSnapshot is null)
                        {
                            var snapshot = new BalanceSnapshot(user.Id, account.Id, account.CurrentBalanceCached, today);
                            dbContext.BalanceSnapshots.Add(snapshot);
                            await dbContext.SaveChangesAsync(stoppingToken);
                            logger.LogInformation("Created initial balance snapshot for account {AccountId} on {Date}", account.Id, today);
                        }
                        else if (lastSnapshot.SnapshotDate < today)
                        {
                            var transactions = await dbContext.Transactions
                                .Where(t => t.AccountId == account.Id && !t.IsDeleted && t.CompetenceDate > lastSnapshot.SnapshotDate && t.CompetenceDate <= today)
                                .ToListAsync(stoppingToken);

                            var startingValue = lastSnapshot.Balance;
                            var fillDate = lastSnapshot.SnapshotDate.AddDays(1);
                            while (fillDate <= today)
                            {
                                var daySignedSum = transactions
                                    .Where(t => t.CompetenceDate > lastSnapshot.SnapshotDate && t.CompetenceDate <= fillDate)
                                    .Sum(t => t.SignedAmount());
                                
                                var dayBalance = startingValue + daySignedSum;
                                var snapshot = new BalanceSnapshot(user.Id, account.Id, dayBalance, fillDate);
                                dbContext.BalanceSnapshots.Add(snapshot);
                                fillDate = fillDate.AddDays(1);
                            }
                            await dbContext.SaveChangesAsync(stoppingToken);
                            logger.LogInformation("Filled balance snapshot gaps for account {AccountId} up to {Date}", account.Id, today);
                        }
                    }

                    var investments = await dbContext.Investments
                        .Where(x => x.UserId == user.Id && x.IsActive)
                        .ToListAsync(stoppingToken);

                    foreach (var investment in investments)
                    {
                        var lastInvSnapshot = await dbContext.InvestmentSnapshots
                            .Where(x => x.InvestmentId == investment.Id)
                            .OrderByDescending(x => x.SnapshotDate)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (lastInvSnapshot is null)
                        {
                            var snapshot = new InvestmentSnapshot(user.Id, investment.Id, investment.CurrentValue, today);
                            dbContext.InvestmentSnapshots.Add(snapshot);
                            await dbContext.SaveChangesAsync(stoppingToken);
                            logger.LogInformation("Created initial investment snapshot for asset {InvestmentId} on {Date}", investment.Id, today);
                        }
                        else if (lastInvSnapshot.SnapshotDate < today)
                        {
                            var startDate = lastInvSnapshot.SnapshotDate.AddDays(1);
                            Dictionary<DateOnly, decimal> historicalPrices = null;

                            if (!string.IsNullOrWhiteSpace(investment.Ticker))
                            {
                                historicalPrices = await priceService.GetHistoricalPricesAsync(investment.Ticker, startDate, today, stoppingToken);
                            }

                            var fillDate = startDate;
                            var lastKnownPrice = investment.CurrentPrice;

                            while (fillDate <= today)
                            {
                                decimal dayValue = 0m;
                                if (!string.IsNullOrWhiteSpace(investment.Ticker) && historicalPrices != null)
                                {
                                    if (historicalPrices.TryGetValue(fillDate, out var histPrice))
                                    {
                                        lastKnownPrice = histPrice;
                                    }
                                    dayValue = investment.Quantity * lastKnownPrice;
                                }
                                else if (investment.IndexerType != null)
                                {
                                    int daysBack = today.DayNumber - fillDate.DayNumber;
                                    double annualRate = 0.0;
                                    if (investment.IndexerType == "cdi")
                                    {
                                        var ratePercent = investment.IndexerRate ?? 100m;
                                        annualRate = 0.105 * (double)(ratePercent / 100m);
                                    }
                                    else if (investment.IndexerType == "ipca")
                                    {
                                        var addRate = investment.IndexerAdditionalRate ?? 0m;
                                        annualRate = 0.045 + (double)(addRate / 100m);
                                    }
                                    else if (investment.IndexerType == "pre")
                                    {
                                        var ratePercent = investment.IndexerRate ?? 0m;
                                        annualRate = (double)(ratePercent / 100m);
                                    }

                                    var dailyFactor = Math.Pow(1.0 + annualRate, 1.0 / 365.0);
                                    dayValue = investment.CurrentValue / (decimal)Math.Pow(dailyFactor, daysBack);
                                }
                                else
                                {
                                    dayValue = investment.CurrentValue;
                                }

                                var snapshot = new InvestmentSnapshot(user.Id, investment.Id, dayValue, fillDate);
                                dbContext.InvestmentSnapshots.Add(snapshot);
                                fillDate = fillDate.AddDays(1);
                            }
                            await dbContext.SaveChangesAsync(stoppingToken);
                            logger.LogInformation("Filled investment snapshot gaps for asset {InvestmentId} up to {Date}", investment.Id, today);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Balance Snapshot worker loop failed.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
