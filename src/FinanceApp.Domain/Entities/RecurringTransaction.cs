using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class RecurringTransaction : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TransactionKind { get; private set; } = "expense";
    public Guid AccountId { get; private set; }
    public Guid? IncomeCategoryId { get; private set; }
    public Guid? ExpenseCategoryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Frequency { get; private set; } = "monthly";
    public int IntervalValue { get; private set; } = 1;
    public short? DayOfMonth { get; private set; }
    public short? DayOfWeek { get; private set; }
    public bool UseLastBusinessDay { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly NextRunDate { get; private set; }
    public decimal DefaultAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "BRL";
    public bool AutoConfirm { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsActive { get; private set; } = true;

    private RecurringTransaction() { }

    public RecurringTransaction(Guid userId, Guid accountId, string transactionKind, string description, string frequency, DateOnly startDate, DateOnly nextRunDate, decimal defaultAmount, string currencyCode, Guid? incomeCategoryId = null, Guid? expenseCategoryId = null)
    {
        UserId = userId;
        AccountId = accountId;
        TransactionKind = transactionKind;
        Description = description;
        Frequency = frequency;
        StartDate = startDate;
        NextRunDate = nextRunDate;
        DefaultAmount = defaultAmount;
        CurrencyCode = currencyCode;
        IncomeCategoryId = incomeCategoryId;
        ExpenseCategoryId = expenseCategoryId;
    }

    public bool ShouldRunOn(DateOnly date) =>
        IsActive && !IsPaused && NextRunDate <= date && (EndDate is null || date <= EndDate);

    public void Pause()
    {
        IsPaused = true;
        Touch();
    }

    public void Resume()
    {
        IsPaused = false;
        Touch();
    }

    public void Advance()
    {
        NextRunDate = Frequency switch
        {
            "weekly" => NextRunDate.AddDays(7 * IntervalValue),
            "biweekly" => NextRunDate.AddDays(14 * IntervalValue),
            "monthly" => NextRunDate.AddMonths(1 * IntervalValue),
            "yearly" => NextRunDate.AddYears(1 * IntervalValue),
            _ => NextRunDate.AddMonths(1)
        };
        Touch();
    }
}
