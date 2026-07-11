using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceApp.Application.Abstractions;

public interface IAssetPriceService
{
    Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers, CancellationToken cancellationToken);
}
