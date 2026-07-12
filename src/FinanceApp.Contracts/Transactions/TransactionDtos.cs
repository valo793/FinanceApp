namespace FinanceApp.Contracts.Transactions;

public sealed class TransactionDto
{
    public Guid Id { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? AccountId { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateOnly CompetenceDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public decimal? AmountExpected { get; init; }
    public decimal? AmountActual { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public bool IsFixed { get; init; }
    public long LockVersion { get; init; }
    public Guid? InvestmentId { get; init; }
    public decimal? InvestmentQuantity { get; init; }
    public decimal? UnitPrice { get; init; }
}

public sealed class UpsertTransactionRequest
{
    public required string TransactionType { get; init; }
    public required string Description { get; init; }
    public string Status { get; init; } = "planned";
    public Guid? AccountId { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public Guid? IncomeCategoryId { get; init; }
    public Guid? ExpenseCategoryId { get; init; }
    public DateOnly CompetenceDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public decimal? AmountExpected { get; init; }
    public decimal? AmountActual { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public bool IsFixed { get; init; }
    public long? LockVersion { get; init; }
    public Guid? InvestmentId { get; init; }
    public decimal? InvestmentQuantity { get; init; }
    public decimal? UnitPrice { get; init; }
}
