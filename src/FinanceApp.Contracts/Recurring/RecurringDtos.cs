namespace FinanceApp.Contracts.Recurring;

public sealed class RecurringDto
{
    public Guid Id { get; init; }
    public string TransactionKind { get; init; } = string.Empty;
    public Guid AccountId { get; init; }
    public Guid? IncomeCategoryId { get; init; }
    public Guid? ExpenseCategoryId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public int IntervalValue { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly NextRunDate { get; init; }
    public decimal DefaultAmount { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public bool AutoConfirm { get; init; }
    public bool IsPaused { get; init; }
    public bool IsActive { get; init; }
    public long LockVersion { get; init; }
}

public class CreateRecurringRequest
{
    public required string TransactionKind { get; init; }
    public required Guid AccountId { get; init; }
    public Guid? IncomeCategoryId { get; init; }
    public Guid? ExpenseCategoryId { get; init; }
    public required string Description { get; init; }
    public string Frequency { get; init; } = "monthly";
    public int IntervalValue { get; init; } = 1;
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required decimal DefaultAmount { get; init; }
    public string CurrencyCode { get; init; } = "BRL";
    public bool AutoConfirm { get; init; }
}

public sealed class UpdateRecurringRequest : CreateRecurringRequest
{
    public required long LockVersion { get; init; }
}
