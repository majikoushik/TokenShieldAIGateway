namespace TokenShield.Application.Common.Interfaces;

public interface IAuditLoggingService
{
    Task LogActionAsync(Guid? tenantId, string actionName, string entityName, Guid entityId, string actorEmail, object details);
}
