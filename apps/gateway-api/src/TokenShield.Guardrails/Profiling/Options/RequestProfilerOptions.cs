using System.Collections.Generic;

namespace TokenShield.Guardrails.Profiling.Options;

public class RequestProfilerOptions
{
    public const string SectionName = "RequestProfiler";

    public string Mode { get; set; } = "Mvp";
    public bool EnableProductionProfiler { get; set; } = false;
    public bool EnableSemanticClassifier { get; set; } = false;
    public bool EnableLlmClassifier { get; set; } = false;
    public double LowConfidenceThreshold { get; set; } = 0.6;
    public double HighConfidenceThreshold { get; set; } = 0.85;
    public string DefaultTaskType { get; set; } = "general";
    public string DefaultRiskLevel { get; set; } = "medium";
    public bool UseMetadataAsPrimarySignal { get; set; } = true;
    public bool ValidateMetadataAgainstPrompt { get; set; } = true;
    public bool EnableSensitivityDetection { get; set; } = true;
    public bool EnableProfileSignals { get; set; } = true;

    public List<TaskClassificationRule> TaskClassificationRules { get; set; } = new();
}

public class TaskClassificationRule
{
    public string TaskType { get; set; } = "general";
    public List<string> Phrases { get; set; } = new();
    public List<string> RegexPatterns { get; set; } = new();
    public double Confidence { get; set; } = 0.75;
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}
