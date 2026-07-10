namespace FinanceApp.Application.Abstractions;

public interface IAuditService
{
    Task WriteAsync(Guid? userId, string actionCode, string resourceType, Guid? resourceId, string result, string severity, object context, CancellationToken cancellationToken);
}
