using TokenShield.Domain.Common;
using TokenShield.Domain.Enums;

namespace TokenShield.Domain.Entities;

public class AiModel : BaseEntity
{
    public Guid ProviderId { get; set; }
    public ModelProvider Provider { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;
    public ModelTier Tier { get; set; }
    
    public decimal InputTokenPricePerMillion { get; set; }
    public decimal OutputTokenPricePerMillion { get; set; }
    public int ContextWindow { get; set; }
    public bool IsActive { get; set; }
}
