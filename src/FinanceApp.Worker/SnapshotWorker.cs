using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                var today = DateOnly.FromDateTime(DateTime.Today);

                // Load all active users
                var users = await dbContext.Users
                    .Where(x => x.Status == "active")
                    .ToListAsync(stoppingToken);

                foreach (var user in users)
                {
                    // Load all active accounts for this user
                    var accounts = await dbContext.Accounts
                        .Where(x => x.UserId == user.Id && !x.IsDeleted && x.IsActive)
                        .ToListAsync(stoppingToken);

                    foreach (var account in accounts)
                    {
                        // Check the most recent snapshot for this account
                        var lastSnapshot = await dbContext.BalanceSnapshots
                            .Where(x => x.AccountId == account.Id)
                            .OrderByDescending(x => x.SnapshotDate)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (lastSnapshot is null)
                        {
                            // No snapshots at all: create one for today
                            var snapshot = new BalanceSnapshot(user.Id, account.Id, account.CurrentBalanceCached, today);
                            dbContext.BalanceSnapshots.Add(snapshot);
                            await dbContext.SaveChangesAsync(stoppingToken);
                            logger.LogInformation("Created initial balance snapshot for account {AccountId} on {Date}", account.Id, today);
                        }
                        else if (lastSnapshot.SnapshotDate < today)
                        {
                            // Fill gaps between the last snapshot and today
                            var fillDate = lastSnapshot.SnapshotDate.AddDays(1);
                            while (fillDate <= today)
                            {
                                var snapshot = new BalanceSnapshot(user.Id, account.Id, account.CurrentBalanceCached, fillDate);
                                dbContext.BalanceSnapshots.Add(snapshot);
                                fillDate = fillDate.AddDays(1);
                            }
                            await dbContext.SaveChangesAsync(stoppingToken);
                            logger.LogInformation("Filled balance snapshot gaps for account {AccountId} up to {Date}", account.Id, today);
                        }
                    }

                    // Load all active investments for this user
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
                            var fillDate = lastInvSnapshot.SnapshotDate.AddDays(1);
                            while (fillDate <= today)
                            {
                                var snapshot = new InvestmentSnapshot(user.Id, investment.Id, investment.CurrentValue, fillDate);
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

            // Run check every 1 hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
