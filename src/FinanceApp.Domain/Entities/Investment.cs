using System;
using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class Investment : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid WalletId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Ticker { get; private set; }
    public string AssetType { get; private set; } = "stock";
    public decimal Quantity { get; private set; }
    public decimal AveragePrice { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "BRL";
    public string RiskLevel { get; private set; } = "moderate";
    public bool IsActive { get; private set; } = true;
    public bool ProjectionEnabled { get; private set; } = true;

    public decimal TotalInvested => Quantity * AveragePrice;
    public decimal CurrentValue => Quantity * CurrentPrice;
    public decimal GainLossPercent => TotalInvested == 0 ? 0 : Math.Round((CurrentValue - TotalInvested) / TotalInvested * 100, 2);

    private Investment() { }

    public Investment(Guid userId, Guid walletId, string name, string? ticker, string assetType, decimal quantity, decimal averagePrice, decimal currentPrice, string currencyCode, string riskLevel)
    {
        UserId = userId;
        WalletId = walletId;
        Name = name;
        Ticker = ticker;
        AssetType = assetType;
        Quantity = quantity;
        AveragePrice = averagePrice;
        CurrentPrice = currentPrice;
        CurrencyCode = currencyCode;
        RiskLevel = riskLevel;
    }

    public void Update(string name, string? ticker, string assetType, decimal quantity, decimal averagePrice, decimal currentPrice, string riskLevel, bool isActive)
    {
        Name = name;
        Ticker = ticker;
        AssetType = assetType;
        Quantity = quantity;
        AveragePrice = averagePrice;
        CurrentPrice = currentPrice;
        RiskLevel = riskLevel;
        IsActive = isActive;
        Touch();
    }

    public void UpdateCurrentPrice(decimal currentPrice)
    {
        CurrentPrice = currentPrice;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
