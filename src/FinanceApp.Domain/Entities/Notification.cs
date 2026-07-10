namespace FinanceApp.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; } = string.Empty;
    public string Severity { get; private set; } = "info";
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification() { }

    public Notification(Guid userId, string notificationType, string severity, string title, string message, string? referenceType = null, Guid? referenceId = null)
    {
        UserId = userId;
        NotificationType = notificationType;
        Severity = severity;
        Title = title;
        Message = message;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
    }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
    }
}
