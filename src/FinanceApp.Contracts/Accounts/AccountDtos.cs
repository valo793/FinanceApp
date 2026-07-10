namespace FinanceApp.Contracts.Accounts;

public sealed class AccountDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = "BRL";
    public decimal OpeningBalance { get; init; }
    public decimal CurrentBalanceCached { get; init; }
    public bool IncludeInNetWorth { get; init; }
    public bool IsManual { get; init; }
    public bool IsActive { get; init; }
    public long LockVersion { get; init; }
}

public class CreateAccountRequest
{
    public required string Name { get; init; }
    public required string AccountType { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public decimal OpeningBalance { get; init; }
    public bool IncludeInNetWorth { get; init; } = true;
    public bool IsManual { get; init; } = true;
}

public sealed class UpdateAccountRequest : CreateAccountRequest
{
    public required long LockVersion { get; init; }
    public bool IsActive { get; init; } = true;
}
