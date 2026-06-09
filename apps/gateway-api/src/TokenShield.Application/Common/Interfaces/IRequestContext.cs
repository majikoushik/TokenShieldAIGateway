namespace TokenShield.Application.Common.Interfaces;

public interface IRequestContext
{
    Guid TenantId { get; set; }
    Guid ClientApplicationId { get; set; }
    Guid ApiKeyId { get; set; }
    Guid CorrelationId { get; set; }
}
