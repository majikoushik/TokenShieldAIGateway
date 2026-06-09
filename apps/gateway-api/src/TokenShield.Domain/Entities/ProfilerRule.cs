using System;
using TokenShield.Domain.Common;

namespace TokenShield.Domain.Entities;

public class ProfilerRule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string TargetTaskType { get; set; } = string.Empty;
    
    // Stored as JSON arrays
    public string PhrasesJson { get; set; } = "[]";
    public string RegexPatternsJson { get; set; } = "[]";
    
    public double Confidence { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}
