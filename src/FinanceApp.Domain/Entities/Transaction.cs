using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public sealed class Transaction : SoftDeletableEntity
{
    public Guid UserId { get; private set; }
    public string TransactionType { get; private set; } = TransactionTypes.Expense;
    public string Status { get; private set; } = TransactionStatuses.Planned;
    public Guid? AccountId { get; private set; }
    public Guid? DestinationAccountId { get; private set; }
    public Guid? WalletId { get; private set; }
    public Guid? IncomeCategoryId { get; private set; }
    public Guid? ExpenseCategoryId { get; private set; }
    public Guid? RecurringTransactionId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public string Priority { get; private set; } = "normal";
    public DateOnly CompetenceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public decimal? AmountExpected { get; private set; }
    public decimal? AmountActual { get; private set; }
    public string CurrencyCode { get; private set; } = "BRL";
    public bool IsFixed { get; private set; }
    public string OriginType { get; private set; } = "manual";

    private Transaction() { }

    public Transaction(
        Guid userId,
        string transactionType,
        string description,
        string status,
        DateOnly competenceDate,
        decimal? amountExpected,
        decimal? amountActual,
        string currencyCode,
        Guid? accountId = null,
        Guid? destinationAccountId = null,
        Guid? incomeCategoryId = null,
        Guid? expenseCategoryId = null,
        DateOnly? dueDate = null,
        DateTimeOffset? paidAt = null,
        bool isFixed = false,
        Guid? recurringTransactionId = null,
        string originType = "manual")
    {
        if (amountExpected is null && amountActual is null)
            throw new InvalidOperationException("A transaction requires amountExpected or amountActual.");

        if (transactionType == TransactionTypes.Transfer && accountId == destinationAccountId)
            throw new InvalidOperationException("Transfer requires different source and destination accounts.");

        UserId = userId;
        TransactionType = transactionType;
        Description = description;
        Status = status;
        CompetenceDate = competenceDate;
        AmountExpected = amountExpected;
        AmountActual = amountActual;
        CurrencyCode = currencyCode;
        AccountId = accountId;
        DestinationAccountId = destinationAccountId;
        IncomeCategoryId = incomeCategoryId;
        ExpenseCategoryId = expenseCategoryId;
        DueDate = dueDate;
        PaidAt = paidAt;
        IsFixed = isFixed;
        RecurringTransactionId = recurringTransactionId;
        OriginType = originType;
    }

    public decimal EffectiveAmount => AmountActual ?? AmountExpected ?? 0m;

    public void Update(
        string transactionType,
        string description,
        string status,
        DateOnly competenceDate,
        decimal? amountExpected,
        decimal? amountActual,
        string currencyCode,
        Guid? accountId,
        Guid? destinationAccountId,
        Guid? incomeCategoryId,
        Guid? expenseCategoryId,
        DateOnly? dueDate,
        DateTimeOffset? paidAt,
        bool isFixed)
    {
        if (amountExpected is null && amountActual is null)
            throw new InvalidOperationException("A transaction requires amountExpected or amountActual.");

        TransactionType = transactionType;
        Description = description;
        Status = status;
        CompetenceDate = competenceDate;
        AmountExpected = amountExpected;
        AmountActual = amountActual;
        CurrencyCode = currencyCode;
        AccountId = accountId;
        DestinationAccountId = destinationAccountId;
        IncomeCategoryId = incomeCategoryId;
        ExpenseCategoryId = expenseCategoryId;
        DueDate = dueDate;
        PaidAt = paidAt;
        IsFixed = isFixed;
        Touch();
    }

    public void Confirm(DateTimeOffset paidAt)
    {
        Status = TransactionStatuses.Confirmed;
        PaidAt = paidAt;
        if (AmountActual is null)
            AmountActual = AmountExpected;

        Touch();
    }

    public decimal SignedAmount()
    {
        var amount = EffectiveAmount;
        return TransactionType switch
        {
            TransactionTypes.Income => amount,
            TransactionTypes.Expense => amount * -1,
            TransactionTypes.InvestmentBuy => amount * -1,
            TransactionTypes.InvestmentSell => amount,
            TransactionTypes.InvestmentYield => amount,
            TransactionTypes.Transfer => 0m,
            _ => amount
        };
    }
}
