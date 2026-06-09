using TokenShield.Domain.Common;

namespace TokenShield.Domain.Entities;

public class ApiKey : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    public Guid ClientApplicationId { get; set; }
    public ClientApplication ClientApplication { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    public string KeyHash { get; set; } = null!;
    public string Prefix { get; set; } = null!; // e.g. ts_live_ or ts_dev_
    
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
}
