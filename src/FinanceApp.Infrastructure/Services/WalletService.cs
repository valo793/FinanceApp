using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Wallets;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class WalletService(FinanceDbContext dbContext) : IWalletService
{
    public async Task<IReadOnlyCollection<WalletDto>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Wallets
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new WalletDto
            {
                Id = x.Id, Name = x.Name, WalletType = x.WalletType,
                BaseAccountId = x.BaseAccountId, CurrencyCode = x.CurrencyCode,
                RiskProfile = x.RiskProfile, IsDefault = x.IsDefault, IsActive = x.IsActive,
                LockVersion = x.LockVersion
            })
            .ToListAsync(cancellationToken);

    public async Task<WalletDto> CreateAsync(Guid userId, CreateWalletRequest request, CancellationToken cancellationToken)
    {
        var entity = new Wallet(userId, request.Name, request.WalletType, request.CurrencyCode, request.RiskProfile);
        dbContext.Wallets.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<WalletDto> UpdateAsync(Guid userId, Guid id, UpdateWalletRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Wallets.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Carteira não encontrada.");
        entity.Update(request.Name, request.WalletType, request.CurrencyCode, request.RiskProfile, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static WalletDto Map(Wallet x) => new()
    {
        Id = x.Id, Name = x.Name, WalletType = x.WalletType,
        BaseAccountId = x.BaseAccountId, CurrencyCode = x.CurrencyCode,
        RiskProfile = x.RiskProfile, IsDefault = x.IsDefault, IsActive = x.IsActive,
        LockVersion = x.LockVersion
    };
}
