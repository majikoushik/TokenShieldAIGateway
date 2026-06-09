using TokenShield.Domain.Common;

namespace TokenShield.Domain.Entities;

public class ModelProvider : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    public string ApiUrl { get; set; } = null!;
    public string ApiKeySecretRef { get; set; } = null!; // Reference to key vault secret
    public bool IsActive { get; set; }
    
    // Navigation properties
    public ICollection<AiModel> Models { get; set; } = new List<AiModel>();
}
