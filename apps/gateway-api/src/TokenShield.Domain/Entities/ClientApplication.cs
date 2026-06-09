using TokenShield.Domain.Common;

namespace TokenShield.Domain.Entities;

public class ClientApplication : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    
    // Navigation properties
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
