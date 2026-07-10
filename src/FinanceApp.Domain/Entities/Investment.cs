using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class Investment : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid WalletId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Ticker { get; private set; }
    public string CurrencyCode { get; private set; } = "BRL";
    public string RiskLevel { get; private set; } = "moderate";
    public bool IsActive { get; private set; } = true;
    public bool ProjectionEnabled { get; private set; } = true;

    private Investment() { }

    public Investment(Guid userId, Guid walletId, string name, string currencyCode, string riskLevel)
    {
        UserId = userId;
        WalletId = walletId;
        Name = name;
        CurrencyCode = currencyCode;
        RiskLevel = riskLevel;
    }
}
