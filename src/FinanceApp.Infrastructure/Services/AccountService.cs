using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Accounts;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Services;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class AccountService(FinanceDbContext dbContext) : IAccountService
{
    public async Task<IReadOnlyCollection<AccountDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(Map)
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var dto = await dbContext.Accounts
            .Where(x => x.UserId == userId && x.Id == id && !x.IsDeleted)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new KeyNotFoundException("Conta não encontrada.");
    }

    public async Task<AccountDto> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var entity = new Account(userId, request.Name, request.AccountType, request.CurrencyCode, request.OpeningBalance, request.IncludeInNetWorth, request.IsManual);
        dbContext.Accounts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map.Compile().Invoke(entity);
    }

    public async Task<AccountDto> UpdateAsync(Guid userId, Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Accounts.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        entity.Update(request.Name, request.AccountType, request.CurrencyCode, request.OpeningBalance, request.IncludeInNetWorth, request.IsManual, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map.Compile().Invoke(entity);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Accounts.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        entity.MarkDeleted(userId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Account, AccountDto>> Map = x => new AccountDto
    {
        Id = x.Id,
        Name = x.Name,
        AccountType = x.AccountType,
        CurrencyCode = x.CurrencyCode,
        OpeningBalance = x.OpeningBalance,
        CurrentBalanceCached = x.CurrentBalanceCached,
        IncludeInNetWorth = x.IncludeInNetWorth,
        IsManual = x.IsManual,
        IsActive = x.IsActive,
        LockVersion = x.LockVersion
    };
}
