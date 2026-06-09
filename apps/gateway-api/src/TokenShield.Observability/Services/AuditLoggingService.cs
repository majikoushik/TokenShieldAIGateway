using System.Text.Json;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Domain.Entities;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Observability.Services;

public class AuditLoggingService : IAuditLoggingService
{
    private readonly TokenShieldDbContext _dbContext;

    public AuditLoggingService(TokenShieldDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogActionAsync(Guid? tenantId, string actionName, string entityName, Guid entityId, string actorEmail, object details)
    {
        var detailsJson = details is string str ? str : JsonSerializer.Serialize(details);

        var auditLog = new AuditLog
        {
            TenantId = tenantId,
            ActionName = actionName,
            EntityName = entityName,
            EntityId = entityId,
            ActorEmail = actorEmail,
            DetailsJson = detailsJson,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();
    }
}
