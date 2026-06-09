namespace TokenShield.Domain.Models;

public class ProfileSignal
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Source { get; init; } = string.Empty;
}

public class RequestProfile
{
    public string TaskType { get; set; } = null!;
    public double TaskTypeConfidence { get; set; }
    public string RiskLevel { get; set; } = null!;
    public double RiskConfidence { get; set; }
    public int InputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public bool RequiresReasoning { get; set; }
    public bool RequiresStructuredOutput { get; set; }
    public bool ContainsPii { get; set; }
    public string DataSensitivity { get; set; } = "unknown";
    public double SensitivityConfidence { get; set; }
    public int ComplexityScore { get; set; }
    public string ComplexityBand { get; set; } = "medium";
    public string? Department { get; set; }
    public string? Environment { get; set; }
    public string ClassificationMethod { get; set; } = "unknown";
    public IReadOnlyList<ProfileSignal> Signals { get; set; } = [];
    public IReadOnlyList<string> Warnings { get; set; } = [];
}
