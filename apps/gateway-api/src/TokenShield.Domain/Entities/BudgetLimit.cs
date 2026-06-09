using TokenShield.Domain.Common;
using TokenShield.Domain.Enums;

namespace TokenShield.Domain.Entities;

public class BudgetLimit : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    public BudgetScope Scope { get; set; }
    public Guid? TargetId { get; set; } // ID of target application, key, or model depending on scope
    
    public decimal MonthlyLimit { get; set; }
    public decimal WarningThresholdPercent { get; set; }
    public decimal CurrentSpend { get; set; }
    
    public DateTime LastResetAtUtc { get; set; }
    public BudgetActionType Action { get; set; }
}
