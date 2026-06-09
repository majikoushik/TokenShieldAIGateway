namespace TokenShield.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? TenantId { get; set; }
    public string ActionName { get; set; } = null!; // e.g. Create, Update, Delete, Revoke
    public string EntityName { get; set; } = null!; // e.g. ApiKey, BudgetLimit
    public Guid EntityId { get; set; }
    
    public string ActorEmail { get; set; } = null!;
    public string DetailsJson { get; set; } = null!; // JSONB type in DB
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
