namespace FinanceApp.Domain.Common;

public abstract class SoftDeletableEntity : BaseEntity
{
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public Guid? DeletedByUserId { get; protected set; }

    public void MarkDeleted(Guid? deletedByUserId = null)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedByUserId = deletedByUserId;
        Touch();
    }
}
