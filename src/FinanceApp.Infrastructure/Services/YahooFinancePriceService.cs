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
    private static string NormalizeTicker(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker)) return string.Empty;
        var clean = ticker.Trim().ToUpperInvariant();
        
        if (clean.Contains(".")) return clean;

        if (System.Text.RegularExpressions.Regex.IsMatch(clean, @"^[A-Z]{4}[0-9]{1,2}F?$"))
        {
            return clean + ".SA";
        }
        
        return clean;
    }

    public async Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (tickers == null) return result;

        var originalTickers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tickers)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            var normalized = NormalizeTicker(t);
            originalTickers[normalized] = t.Trim();
        }

        if (originalTickers.Count == 0) return result;

        // Fetch prices in parallel using the chart endpoint
        var tasks = originalTickers.Keys.Select(async normalizedTicker =>
        {
            var price = await GetSinglePriceAsync(normalizedTicker, cancellationToken);
            return (normalizedTicker, price);
        });

        var taskResults = await Task.WhenAll(tasks);
        foreach (var (normalizedTicker, price) in taskResults)
        {
            if (price.HasValue)
            {
                if (originalTickers.TryGetValue(normalizedTicker, out var originalTicker))
                {
                    result[originalTicker] = price.Value;
                }
                else
                {
                    result[normalizedTicker] = price.Value;
                }
            }
        }

        return result;
    }

    private async Task<decimal?> GetSinglePriceAsync(string ticker, CancellationToken cancellationToken)
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=1d";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<YahooChartResponse>(cancellationToken: cancellationToken);
            var meta = data?.Chart?.Result?.FirstOrDefault()?.Meta;
            if (meta != null)
            {
                return (decimal)meta.RegularMarketPrice;
            }
        }
        catch
        {
            // Fail silently
        }
        return null;
    }

    public async Task<TickerValidationResultDto?> ValidateTickerAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = NormalizeTicker(ticker);
        if (string.IsNullOrWhiteSpace(normalized)) return new TickerValidationResultDto { IsValid = false };

        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(normalized)}?interval=1d&range=1d";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new TickerValidationResultDto { IsValid = false };
            }

            var data = await response.Content.ReadFromJsonAsync<YahooChartResponse>(cancellationToken: cancellationToken);
            var meta = data?.Chart?.Result?.FirstOrDefault()?.Meta;
            if (meta != null)
            {
                var mappedType = "stock";
                var quoteType = meta.InstrumentType?.ToUpperInvariant() ?? "EQUITY";
                if (quoteType == "MUTUALFUND" || quoteType == "ETF")
                {
                    mappedType = "fund";
                }
                else if (quoteType == "CRYPTOCURRENCY")
                {
                    mappedType = "crypto";
                }

                if (normalized.EndsWith("11.SA") || normalized.EndsWith("11"))
                {
                    mappedType = "fii";
                }

                return new TickerValidationResultDto
                {
                    IsValid = true,
                    Name = meta.LongName ?? meta.ShortName ?? meta.Symbol,
                    CurrentPrice = (decimal)meta.RegularMarketPrice,
                    CurrencyCode = meta.Currency ?? "BRL",
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

        var normalized = NormalizeTicker(ticker);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(normalized)}?period1={period1}&period2={period2}&interval=1d";

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

    public async Task<IReadOnlyCollection<CandlestickPointDto>> GetHistoricalCandlesticksAsync(string ticker, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var result = new List<CandlestickPointDto>();
        if (string.IsNullOrWhiteSpace(ticker)) return result;

        var fromDateTime = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toDateTime = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var period1 = fromDateTime.ToUnixTimeSeconds();
        var period2 = toDateTime.ToUnixTimeSeconds();

        var normalized = NormalizeTicker(ticker);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(normalized)}?period1={period1}&period2={period2}&interval=1d";

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
            if (chartResult?.Timestamp != null && chartResult.Indicators?.Quote?.FirstOrDefault() != null)
            {
                var quote = chartResult.Indicators.Quote[0];
                var timestamps = chartResult.Timestamp;
                var opens = quote.Open;
                var highs = quote.High;
                var lows = quote.Low;
                var closes = quote.Close;
                var volumes = quote.Volume;

                for (int i = 0; i < timestamps.Count; i++)
                {
                    var o = opens != null && opens.Count > i ? opens[i] : null;
                    var h = highs != null && highs.Count > i ? highs[i] : null;
                    var l = lows != null && lows.Count > i ? lows[i] : null;
                    var c = closes != null && closes.Count > i ? closes[i] : null;
                    var v = volumes != null && volumes.Count > i ? volumes[i] : null;

                    if (o.HasValue && h.HasValue && l.HasValue && c.HasValue)
                    {
                        var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime);
                        result.Add(new CandlestickPointDto
                        {
                            Date = date,
                            Open = (decimal)o.Value,
                            High = (decimal)h.Value,
                            Low = (decimal)l.Value,
                            Close = (decimal)c.Value,
                            Volume = v ?? 0
                        });
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
        [JsonPropertyName("meta")]
        public YahooChartMeta? Meta { get; set; }

        [JsonPropertyName("timestamp")]
        public List<long>? Timestamp { get; set; }

        [JsonPropertyName("indicators")]
        public YahooChartIndicators? Indicators { get; set; }
    }

    private sealed class YahooChartMeta
    {
        [JsonPropertyName("regularMarketPrice")]
        public double RegularMarketPrice { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("instrumentType")]
        public string? InstrumentType { get; set; }

        [JsonPropertyName("longName")]
        public string? LongName { get; set; }

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }
    }

    private sealed class YahooChartIndicators
    {
        [JsonPropertyName("quote")]
        public List<YahooChartQuote>? Quote { get; set; }
    }

    private sealed class YahooChartQuote
    {
        [JsonPropertyName("open")]
        public List<double?>? Open { get; set; }

        [JsonPropertyName("high")]
        public List<double?>? High { get; set; }

        [JsonPropertyName("low")]
        public List<double?>? Low { get; set; }

        [JsonPropertyName("close")]
        public List<double?>? Close { get; set; }

        [JsonPropertyName("volume")]
        public List<long?>? Volume { get; set; }
    }
}
