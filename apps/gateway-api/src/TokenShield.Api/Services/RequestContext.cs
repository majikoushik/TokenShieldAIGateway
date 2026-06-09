using TokenShield.Application.Common.Interfaces;

namespace TokenShield.Api.Services;

public class RequestContext : IRequestContext
{
    public Guid TenantId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public Guid CorrelationId { get; set; }
}
