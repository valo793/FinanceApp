using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;

namespace FinanceApp.Infrastructure.Services;

public sealed class YahooFinancePriceService(HttpClient httpClient) : IAssetPriceService
{
    public async Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var tickerList = tickers.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList();

        if (tickerList.Count == 0) return result;

        var symbols = string.Join(",", tickerList);
        var url = $"https://query1.finance2.yahoo.com/v7/finance/quote?symbols={Uri.EscapeDataString(symbols)}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            var data = await response.Content.ReadFromJsonAsync<YahooQuoteResponse>(cancellationToken: cancellationToken);
            if (data?.QuoteResponse?.Result != null)
            {
                foreach (var item in data.QuoteResponse.Result)
                {
                    if (item.Symbol != null)
                    {
                        result[item.Symbol] = (decimal)item.RegularMarketPrice;
                    }
                }
            }
        }
        catch
        {
            // Fail silently
        }

        return result;
    }

    private sealed class YahooQuoteResponse
    {
        [JsonPropertyName("quoteResponse")]
        public YahooQuoteResultWrapper? QuoteResponse { get; set; }
    }

    private sealed class YahooQuoteResultWrapper
    {
        [JsonPropertyName("result")]
        public List<YahooQuoteItem>? Result { get; set; }
    }

    private sealed class YahooQuoteItem
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("regularMarketPrice")]
        public double RegularMarketPrice { get; set; }
    }
}
