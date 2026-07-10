using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class Account : SoftDeletableEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string AccountType { get; private set; } = "checking";
    public string? InstitutionName { get; private set; }
    public string CurrencyCode { get; private set; } = "BRL";
    public decimal OpeningBalance { get; private set; }
    public decimal CurrentBalanceCached { get; private set; }
    public bool IncludeInNetWorth { get; private set; } = true;
    public bool IsManual { get; private set; } = true;
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Account() { }

    public Account(Guid userId, string name, string accountType, string currencyCode, decimal openingBalance, bool includeInNetWorth, bool isManual, string? institutionName = null, string? color = null, string? icon = null)
    {
        UserId = userId;
        Name = name;
        AccountType = accountType;
        CurrencyCode = currencyCode;
        OpeningBalance = openingBalance;
        CurrentBalanceCached = openingBalance;
        IncludeInNetWorth = includeInNetWorth;
        IsManual = isManual;
        InstitutionName = institutionName;
        Color = color;
        Icon = icon;
    }

    public void Update(string name, string accountType, string currencyCode, decimal openingBalance, bool includeInNetWorth, bool isManual, bool isActive, string? institutionName = null, string? color = null, string? icon = null)
    {
        Name = name;
        AccountType = accountType;
        CurrencyCode = currencyCode;
        OpeningBalance = openingBalance;
        IncludeInNetWorth = includeInNetWorth;
        IsManual = isManual;
        IsActive = isActive;
        InstitutionName = institutionName;
        Color = color;
        Icon = icon;
        Touch();
    }

    public void RecalculateBalance(decimal confirmedDelta)
    {
        CurrentBalanceCached = OpeningBalance + confirmedDelta;
        Touch();
    }
}
