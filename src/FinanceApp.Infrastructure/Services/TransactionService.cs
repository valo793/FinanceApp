using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Common;
using FinanceApp.Contracts.Transactions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Services;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class TransactionService(FinanceDbContext dbContext) : ITransactionService
{
    public async Task<PagedResult<TransactionDto>> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions.Where(x => x.UserId == userId && !x.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CompetenceDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Map)
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TransactionDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .Where(x => x.UserId == userId && x.Id == id && !x.IsDeleted)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);

        return transaction ?? throw new KeyNotFoundException("Lançamento não encontrado.");
    }

    public async Task<TransactionDto> CreateAsync(Guid userId, UpsertTransactionRequest request, CancellationToken cancellationToken)
    {
        var entity = new Transaction(
            userId,
            request.TransactionType,
            request.Description,
            request.Status,
            request.CompetenceDate,
            request.AmountExpected,
            request.AmountActual,
            request.CurrencyCode,
            request.AccountId,
            request.DestinationAccountId,
            request.IncomeCategoryId,
            request.ExpenseCategoryId,
            request.DueDate,
            request.PaidAt,
            request.IsFixed);

        dbContext.Transactions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateBalancesAsync(userId, cancellationToken);
        return Map.Compile().Invoke(entity);
    }

    public async Task<TransactionDto> UpdateAsync(Guid userId, Guid id, UpsertTransactionRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Transactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Lançamento não encontrado.");

        entity.Update(
            request.TransactionType,
            request.Description,
            request.Status,
            request.CompetenceDate,
            request.AmountExpected,
            request.AmountActual,
            request.CurrencyCode,
            request.AccountId,
            request.DestinationAccountId,
            request.IncomeCategoryId,
            request.ExpenseCategoryId,
            request.DueDate,
            request.PaidAt,
            request.IsFixed);

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateBalancesAsync(userId, cancellationToken);
        return Map.Compile().Invoke(entity);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Transactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Lançamento não encontrado.");

        entity.MarkDeleted(userId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateBalancesAsync(userId, cancellationToken);
    }

    public async Task ConfirmExpenseAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Transactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Lançamento não encontrado.");

        entity.Confirm(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateBalancesAsync(userId, cancellationToken);
    }

    private async Task RecalculateBalancesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts.Where(x => x.UserId == userId && !x.IsDeleted).ToListAsync(cancellationToken);
        var transactions = await dbContext.Transactions.Where(x => x.UserId == userId && !x.IsDeleted).ToListAsync(cancellationToken);

        foreach (var account in accounts)
        {
            var delta = BalanceCalculator.CalculateConfirmedDelta(transactions, account.Id);
            account.RecalculateBalance(delta);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Transaction, TransactionDto>> Map = x => new TransactionDto
    {
        Id = x.Id,
        TransactionType = x.TransactionType,
        Status = x.Status,
        AccountId = x.AccountId,
        DestinationAccountId = x.DestinationAccountId,
        Description = x.Description,
        CompetenceDate = x.CompetenceDate,
        DueDate = x.DueDate,
        PaidAt = x.PaidAt,
        AmountExpected = x.AmountExpected,
        AmountActual = x.AmountActual,
        CurrencyCode = x.CurrencyCode,
        IsFixed = x.IsFixed,
        LockVersion = x.LockVersion
    };
}
