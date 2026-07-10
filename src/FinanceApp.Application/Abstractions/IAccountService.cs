using FinanceApp.Contracts.Accounts;

namespace FinanceApp.Application.Abstractions;

public interface IAccountService
{
    Task<IReadOnlyCollection<AccountDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<AccountDto> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken);
    Task<AccountDto> UpdateAsync(Guid userId, Guid id, UpdateAccountRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
