using FinanceApp.Contracts.Recurring;

namespace FinanceApp.Application.Abstractions;

public interface IRecurringService
{
    Task<IReadOnlyCollection<RecurringDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<RecurringDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<RecurringDto> CreateAsync(Guid userId, CreateRecurringRequest request, CancellationToken cancellationToken);
    Task<RecurringDto> UpdateAsync(Guid userId, Guid id, UpdateRecurringRequest request, CancellationToken cancellationToken);
    Task PauseAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task ResumeAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
