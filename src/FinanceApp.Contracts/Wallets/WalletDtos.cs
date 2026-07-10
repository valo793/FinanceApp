namespace FinanceApp.Contracts.Wallets;

public sealed class WalletDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string WalletType { get; init; } = string.Empty;
    public Guid? BaseAccountId { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public string? RiskProfile { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public long LockVersion { get; init; }
}

public sealed class CreateWalletRequest
{
    public required string Name { get; init; }
    public string WalletType { get; init; } = "investment";
    public string CurrencyCode { get; init; } = "BRL";
    public string? RiskProfile { get; init; }
}

public sealed class UpdateWalletRequest
{
    public required string Name { get; init; }
    public string WalletType { get; init; } = "investment";
    public string CurrencyCode { get; init; } = "BRL";
    public string? RiskProfile { get; init; }
    public bool IsActive { get; init; } = true;
    public required long LockVersion { get; init; }
}
