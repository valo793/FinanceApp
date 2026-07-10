using FinanceApp.Contracts.Common;
using FinanceApp.Contracts.Transactions;

namespace FinanceApp.Application.Abstractions;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<TransactionDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<TransactionDto> CreateAsync(Guid userId, UpsertTransactionRequest request, CancellationToken cancellationToken);
    Task<TransactionDto> UpdateAsync(Guid userId, Guid id, UpsertTransactionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task ConfirmExpenseAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
