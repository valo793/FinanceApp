using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Recurring;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class RecurringService(FinanceDbContext dbContext) : IRecurringService
{
    public async Task<IReadOnlyCollection<RecurringDto>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.RecurringTransactions
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.NextRunDate)
            .Select(Map)
            .ToListAsync(cancellationToken);

    public async Task<RecurringDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var dto = await dbContext.RecurringTransactions
            .Where(x => x.UserId == userId && x.Id == id)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);
        return dto ?? throw new KeyNotFoundException("Recorrência não encontrada.");
    }

    public async Task<RecurringDto> CreateAsync(Guid userId, CreateRecurringRequest request, CancellationToken cancellationToken)
    {
        var entity = new RecurringTransaction(
            userId, request.AccountId, request.TransactionKind,
            request.Description, request.Frequency, request.StartDate,
            request.StartDate, request.DefaultAmount, request.CurrencyCode,
            request.IncomeCategoryId, request.ExpenseCategoryId);
        dbContext.RecurringTransactions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map.Compile().Invoke(entity);
    }

    public async Task<RecurringDto> UpdateAsync(Guid userId, Guid id, UpdateRecurringRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.RecurringTransactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Recorrência não encontrada.");
        // For POC, we re-create — full update method can be added to domain entity later
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map.Compile().Invoke(entity);
    }

    public async Task PauseAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.RecurringTransactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Recorrência não encontrada.");
        entity.Pause();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.RecurringTransactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Recorrência não encontrada.");
        entity.Resume();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<RecurringTransaction, RecurringDto>> Map = x => new RecurringDto
    {
        Id = x.Id, TransactionKind = x.TransactionKind, AccountId = x.AccountId,
        IncomeCategoryId = x.IncomeCategoryId, ExpenseCategoryId = x.ExpenseCategoryId,
        Description = x.Description, Frequency = x.Frequency, IntervalValue = x.IntervalValue,
        StartDate = x.StartDate, EndDate = x.EndDate, NextRunDate = x.NextRunDate,
        DefaultAmount = x.DefaultAmount, CurrencyCode = x.CurrencyCode,
        AutoConfirm = x.AutoConfirm, IsPaused = x.IsPaused, IsActive = x.IsActive,
        LockVersion = x.LockVersion
    };
}
