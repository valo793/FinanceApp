using System;

namespace FinanceApp.Contracts.Investments;

public sealed class InvestmentDto
{
    public Guid Id { get; init; }
    public Guid WalletId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Ticker { get; init; }
    public string AssetType { get; init; } = "stock";
    public decimal Quantity { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public string RiskLevel { get; init; } = "moderate";
    public bool IsActive { get; init; }
    public string? IndexerType { get; init; }
    public decimal? IndexerRate { get; init; }
    public decimal? IndexerAdditionalRate { get; init; }
    public bool IsWatchlist { get; init; }
    public Guid? AccountId { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public bool IsPinned { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal GainLossPercent { get; init; }
    public long LockVersion { get; init; }
    public decimal AccumulatedYield { get; init; }
    public decimal TotalReturn { get; init; }
}

public sealed class CreateInvestmentRequest
{
    public Guid WalletId { get; init; }
    public required string Name { get; init; }
    public string? Ticker { get; init; }
    public string AssetType { get; init; } = "stock";
    public decimal Quantity { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public string RiskLevel { get; init; } = "moderate";
    public string? IndexerType { get; init; }
    public decimal? IndexerRate { get; init; }
    public decimal? IndexerAdditionalRate { get; init; }
    public bool IsWatchlist { get; init; }
    public Guid? AccountId { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public bool IsPinned { get; init; } = false;
}

public sealed class UpdateInvestmentRequest
{
    public required string Name { get; init; }
    public string? Ticker { get; init; }
    public string AssetType { get; init; } = "stock";
    public decimal Quantity { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public string RiskLevel { get; init; } = "moderate";
    public bool IsActive { get; init; } = true;
    public string? IndexerType { get; init; }
    public decimal? IndexerRate { get; init; }
    public decimal? IndexerAdditionalRate { get; init; }
    public bool IsWatchlist { get; init; }
    public Guid? AccountId { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public bool IsPinned { get; init; }
    public required long LockVersion { get; init; }
}

public sealed class PortfolioSummaryDto
{
    public decimal TotalInvested { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal GainLossPercent { get; init; }
    public int ActiveCount { get; init; }
    public decimal TotalYield { get; init; }
    public decimal TotalReturn { get; init; }
}

public sealed class InvestmentHistoryPointDto
{
    public DateOnly Date { get; init; }
    public decimal Value { get; init; }
}

public sealed class CandlestickPointDto
{
    public required DateOnly Date { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }
}
