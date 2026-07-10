using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class Wallet : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string WalletType { get; private set; } = "investment";
    public Guid? BaseAccountId { get; private set; }
    public string CurrencyCode { get; private set; } = "BRL";
    public string? RiskProfile { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Wallet() { }

    public Wallet(Guid userId, string name, string walletType, string currencyCode, string? riskProfile = null)
    {
        UserId = userId;
        Name = name;
        WalletType = walletType;
        CurrencyCode = currencyCode;
        RiskProfile = riskProfile;
    }

    public void Update(string name, string walletType, string currencyCode, string? riskProfile, bool isActive)
    {
        Name = name;
        WalletType = walletType;
        CurrencyCode = currencyCode;
        RiskProfile = riskProfile;
        IsActive = isActive;
        Touch();
    }
}
