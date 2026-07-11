using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Contracts.UserPreferences;

namespace FinanceApp.Application.Abstractions;

public interface IUserPreferenceService
{
    Task<UserPreferenceDto> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserPreferenceDto> UpdateAsync(Guid userId, UpdatePreferenceRequest request, CancellationToken cancellationToken);
}
