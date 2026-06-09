namespace TokenShield.Domain.Entities;

public class AiRequestLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CorrelationId { get; set; }
    public string RequestId { get; set; } = null!;
    
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid ApiKeyId { get; set; }
    
    public string PromptHash { get; set; } = null!;
    public string ResponseHash { get; set; } = null!;
    
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    
    public string SelectedProvider { get; set; } = null!;
    public string SelectedModel { get; set; } = null!;
    public string SelectedTier { get; set; } = null!;
    
    public string? MatchedRuleName { get; set; }
    public bool FallbackUsed { get; set; }
    public string BudgetStatus { get; set; } = null!;
    public string RequestStatus { get; set; } = null!; // Success, Failed, Blocked
    public int LatencyMs { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
