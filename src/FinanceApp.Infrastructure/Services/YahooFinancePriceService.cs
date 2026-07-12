using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Investments;

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

    public async Task<TickerValidationResultDto?> ValidateTickerAsync(string ticker, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticker)) return new TickerValidationResultDto { IsValid = false };

        var url = $"https://query1.finance2.yahoo.com/v7/finance/quote?symbols={Uri.EscapeDataString(ticker.Trim().ToUpper())}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new TickerValidationResultDto { IsValid = false };
            }

            var data = await response.Content.ReadFromJsonAsync<YahooValidateResponse>(cancellationToken: cancellationToken);
            var item = data?.QuoteResponse?.Result?.FirstOrDefault();
            if (item != null)
            {
                var mappedType = "stock";
                var quoteType = item.QuoteType?.ToUpperInvariant() ?? "EQUITY";
                if (quoteType == "MUTUALFUND" || quoteType == "ETF")
                {
                    mappedType = "fund";
                }
                else if (quoteType == "CRYPTOCURRENCY")
                {
                    mappedType = "crypto";
                }

                var cleanedTicker = ticker.Trim().ToUpperInvariant();
                if (cleanedTicker.EndsWith("11.SA") || cleanedTicker.EndsWith("11"))
                {
                    mappedType = "fii";
                }

                return new TickerValidationResultDto
                {
                    IsValid = true,
                    Name = item.LongName ?? item.ShortName ?? item.Symbol,
                    CurrentPrice = (decimal)item.RegularMarketPrice,
                    CurrencyCode = item.Currency ?? "BRL",
                    AssetType = mappedType
                };
            }
        }
        catch
        {
            // Fail silently
        }

        return new TickerValidationResultDto { IsValid = false };
    }

    public async Task<Dictionary<DateOnly, decimal>> GetHistoricalPricesAsync(string ticker, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var result = new Dictionary<DateOnly, decimal>();
        if (string.IsNullOrWhiteSpace(ticker)) return result;

        var fromDateTime = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toDateTime = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var period1 = fromDateTime.ToUnixTimeSeconds();
        var period2 = toDateTime.ToUnixTimeSeconds();

        var url = $"https://query1.finance2.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker.Trim().ToUpper())}?period1={period1}&period2={period2}&interval=1d";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            var data = await response.Content.ReadFromJsonAsync<YahooChartResponse>(cancellationToken: cancellationToken);
            var chartResult = data?.Chart?.Result?.FirstOrDefault();
            if (chartResult?.Timestamp != null && chartResult.Indicators?.Quote?.FirstOrDefault()?.Close != null)
            {
                var timestamps = chartResult.Timestamp;
                var closes = chartResult.Indicators.Quote[0].Close!;

                for (int i = 0; i < Math.Min(timestamps.Count, closes.Count); i++)
                {
                    var closeValue = closes[i];
                    if (closeValue.HasValue)
                    {
                        var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime);
                        result[date] = (decimal)closeValue.Value;
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

    private sealed class YahooValidateResponse
    {
        [JsonPropertyName("quoteResponse")]
        public YahooValidateResultWrapper? QuoteResponse { get; set; }
    }

    private sealed class YahooValidateResultWrapper
    {
        [JsonPropertyName("result")]
        public List<YahooValidateItem>? Result { get; set; }
    }

    private sealed class YahooValidateItem
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("regularMarketPrice")]
        public double RegularMarketPrice { get; set; }

        [JsonPropertyName("longName")]
        public string? LongName { get; set; }

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("quoteType")]
        public string? QuoteType { get; set; }
    }

    private sealed class YahooChartResponse
    {
        [JsonPropertyName("chart")]
        public YahooChartResultWrapper? Chart { get; set; }
    }

    private sealed class YahooChartResultWrapper
    {
        [JsonPropertyName("result")]
        public List<YahooChartResultItem>? Result { get; set; }
    }

    private sealed class YahooChartResultItem
    {
        [JsonPropertyName("timestamp")]
        public List<long>? Timestamp { get; set; }

        [JsonPropertyName("indicators")]
        public YahooChartIndicators? Indicators { get; set; }
    }

    private sealed class YahooChartIndicators
    {
        [JsonPropertyName("quote")]
        public List<YahooChartQuote>? Quote { get; set; }
    }

    private sealed class YahooChartQuote
    {
        [JsonPropertyName("close")]
        public List<double?>? Close { get; set; }
    }
}
