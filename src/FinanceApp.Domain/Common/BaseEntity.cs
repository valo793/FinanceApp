namespace FinanceApp.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public long LockVersion { get; protected set; } = 1;

    public void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        LockVersion++;
    }
}
