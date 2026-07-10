namespace FinanceApp.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? UserId { get; private set; }
    public string ActionCode { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public Guid? ResourceId { get; private set; }
    public string Result { get; private set; } = "success";
    public string Severity { get; private set; } = "info";
    public string CorrelationId { get; private set; } = string.Empty;
    public string ContextJson { get; private set; } = "{}";

    private AuditLog() { }

    public AuditLog(Guid? userId, string actionCode, string resourceType, Guid? resourceId, string result, string severity, string correlationId, string contextJson)
    {
        UserId = userId;
        ActionCode = actionCode;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Result = result;
        Severity = severity;
        CorrelationId = correlationId;
        ContextJson = contextJson;
    }
}
