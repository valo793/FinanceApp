using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Contracts.Investments;

namespace FinanceApp.Application.Abstractions;

public interface IInvestmentService
{
    Task<IReadOnlyCollection<InvestmentDto>> ListAsync(Guid userId, Guid? walletId, CancellationToken cancellationToken);
    Task<InvestmentDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<InvestmentDto> CreateAsync(Guid userId, CreateInvestmentRequest request, CancellationToken cancellationToken);
    Task<InvestmentDto> UpdateAsync(Guid userId, Guid id, UpdateInvestmentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId, CancellationToken cancellationToken);
    Task SyncPricesAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InvestmentHistoryPointDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken);
}
