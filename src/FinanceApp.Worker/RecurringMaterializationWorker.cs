using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Worker;

public sealed class RecurringMaterializationWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RecurringMaterializationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Recurring worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

                var dueTemplates = await dbContext.RecurringTransactions
                    .Where(x => x.IsActive && !x.IsPaused && x.NextRunDate <= today)
                    .ToListAsync(stoppingToken);

                foreach (var template in dueTemplates)
                {
                    var scheduledFor = template.NextRunDate;
                    var runExists = await dbContext.RecurringTransactionRuns.AnyAsync(
                        x => x.RecurringTransactionId == template.Id && x.ScheduledFor == scheduledFor,
                        stoppingToken);

                    if (runExists)
                        continue;

                    var run = new RecurringTransactionRun(template.Id, scheduledFor, $"{template.Id:N}:{scheduledFor:yyyyMMdd}");
                    dbContext.RecurringTransactionRuns.Add(run);

                    try
                    {
                        var transaction = new Transaction(
                            template.UserId,
                            template.TransactionKind,
                            template.Description,
                            template.AutoConfirm ? TransactionStatuses.Confirmed : TransactionStatuses.Planned,
                            scheduledFor,
                            template.DefaultAmount,
                            template.AutoConfirm ? template.DefaultAmount : null,
                            template.CurrencyCode,
                            template.AccountId,
                            null,
                            template.TransactionKind == TransactionTypes.Income ? template.IncomeCategoryId : null,
                            template.TransactionKind == TransactionTypes.Expense ? template.ExpenseCategoryId : null,
                            scheduledFor,
                            template.AutoConfirm ? DateTimeOffset.UtcNow : null,
                            true,
                            recurringTransactionId: template.Id,
                            originType: "recurring");

                        dbContext.Transactions.Add(transaction);
                        run.MarkSuccess(transaction.Id);
                        template.Advance();

                        // Single atomic save for transaction + run + template advance
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        // Detach the failed entities to avoid saving partial state
                        dbContext.ChangeTracker.Clear();
                        
                        // Re-attach just the run with failure status
                        var failedRun = new RecurringTransactionRun(template.Id, scheduledFor, $"{template.Id:N}:{scheduledFor:yyyyMMdd}");
                        failedRun.MarkFailure(ex.Message);
                        dbContext.RecurringTransactionRuns.Add(failedRun);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        
                        logger.LogError(ex, "Failed to materialize recurring transaction {RecurringTransactionId}", template.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Recurring worker loop failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
