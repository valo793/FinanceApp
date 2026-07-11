using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Worker;

public sealed class AssetPriceSyncWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<AssetPriceSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Asset Price Sync worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting stock price synchronization...");
                using var scope = serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                var priceService = scope.ServiceProvider.GetRequiredService<IAssetPriceService>();

                var investments = await dbContext.Investments
                    .Where(x => x.IsActive && x.Ticker != null && x.Ticker != "")
                    .ToListAsync(stoppingToken);

                var tickers = investments.Select(x => x.Ticker!).Distinct().ToList();

                if (tickers.Count > 0)
                {
                    logger.LogInformation("Syncing prices for tickers: {Tickers}", string.Join(", ", tickers));
                    var prices = await priceService.GetPricesAsync(tickers, stoppingToken);

                    foreach (var investment in investments)
                    {
                        if (prices.TryGetValue(investment.Ticker!, out var price))
                        {
                            investment.UpdateCurrentPrice(price);
                            logger.LogInformation("Updated price of {Ticker} ({Name}) to R$ {Price:N2}", investment.Ticker, investment.Name, price);
                        }
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Finished stock price synchronization successfully.");
                }
                else
                {
                    logger.LogInformation("No active investments with ticker found to sync.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Asset Price Sync worker loop failed.");
            }

            // Sync every 4 hours
            await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
        }
    }
}
