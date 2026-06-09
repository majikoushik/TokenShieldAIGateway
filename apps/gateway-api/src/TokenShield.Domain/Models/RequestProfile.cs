namespace TokenShield.Domain.Models;

public class RequestProfile
{
    public string TaskType { get; set; } = null!;
    public string RiskLevel { get; set; } = null!;
    public int InputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public bool RequiresReasoning { get; set; }
    public bool RequiresStructuredOutput { get; set; }
    public bool ContainsPii { get; set; }
    public int ComplexityScore { get; set; }
    public string? Department { get; set; }
    public string? Environment { get; set; }
}
