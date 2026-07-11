using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Contracts.Projections;

namespace FinanceApp.Application.Abstractions;

public interface IProjectionService
{
    Task<IReadOnlyCollection<ProjectionPointDto>> GetProjectionAsync(Guid userId, int months, CancellationToken cancellationToken);
}
