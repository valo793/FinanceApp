using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Contracts.Investments;

namespace FinanceApp.Application.Abstractions;

public interface IAssetPriceService
{
    Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers, CancellationToken cancellationToken);
    Task<TickerValidationResultDto?> ValidateTickerAsync(string ticker, CancellationToken cancellationToken);
    Task<Dictionary<DateOnly, decimal>> GetHistoricalPricesAsync(string ticker, DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CandlestickPointDto>> GetHistoricalCandlesticksAsync(string ticker, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
