namespace FinanceApp.Domain.Entities;

public sealed class RecurringTransactionRun
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RecurringTransactionId { get; private set; }
    public DateOnly ScheduledFor { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public string Status { get; private set; } = "pending";
    public Guid? GeneratedTransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public int AttemptCount { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private RecurringTransactionRun() { }

    public RecurringTransactionRun(Guid recurringTransactionId, DateOnly scheduledFor, string idempotencyKey)
    {
        RecurringTransactionId = recurringTransactionId;
        ScheduledFor = scheduledFor;
        IdempotencyKey = idempotencyKey;
    }

    public void MarkSuccess(Guid transactionId)
    {
        Status = "completed";
        GeneratedTransactionId = transactionId;
        ExecutedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailure(string reason)
    {
        Status = "failed";
        FailureReason = reason;
        AttemptCount++;
        ExecutedAt = DateTimeOffset.UtcNow;
    }
}
