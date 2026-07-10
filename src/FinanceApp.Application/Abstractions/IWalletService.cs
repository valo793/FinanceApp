using FinanceApp.Contracts.Wallets;

namespace FinanceApp.Application.Abstractions;

public interface IWalletService
{
    Task<IReadOnlyCollection<WalletDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<WalletDto> CreateAsync(Guid userId, CreateWalletRequest request, CancellationToken cancellationToken);
    Task<WalletDto> UpdateAsync(Guid userId, Guid id, UpdateWalletRequest request, CancellationToken cancellationToken);
}
