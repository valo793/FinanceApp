using System.Text.Json;
using FinanceApp.Application.Abstractions;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;

namespace FinanceApp.Infrastructure.Services;

public sealed class AuditService(FinanceDbContext dbContext) : IAuditService
{
    public async Task WriteAsync(Guid? userId, string actionCode, string resourceType, Guid? resourceId, string result, string severity, object context, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var log = new AuditLog(userId, actionCode, resourceType, resourceId, result, severity, correlationId, JsonSerializer.Serialize(context));
        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
