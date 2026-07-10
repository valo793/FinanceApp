using FinanceApp.Contracts.Dashboards;

namespace FinanceApp.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
