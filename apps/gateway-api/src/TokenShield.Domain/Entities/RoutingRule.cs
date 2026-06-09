using TokenShield.Domain.Common;
using TokenShield.Domain.Enums;

namespace TokenShield.Domain.Entities;

public class RoutingRule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    
    public string ConditionsJson { get; set; } = null!; // JSONB type in DB
    public RoutingActionType Action { get; set; }
    public ModelTier? TargetTier { get; set; }
    public bool IsActive { get; set; }
}
